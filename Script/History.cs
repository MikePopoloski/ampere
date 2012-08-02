using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    class FileEntry
    {
        public long Length;
        public DateTime Timestamp;
        public string Hash;
    }

    class HistoryEntry
    {
        public HashSet<string> Byproducts;
        public List<Type> StageTypes;
        public List<FileEntry> OutputCache;
        public List<FileEntry> InputCache;
        public List<string> StageHashes;
    }

    /// <summary>
    /// Maintains a history of built files and detects whether a new build is necessary.
    /// </summary>
    class History
    {
        ConcurrentDictionary<string, HistoryEntry> history = new ConcurrentDictionary<string, HistoryEntry>();

        public History()
        {
        }

        public bool ShouldBuild(string output, string[] byproducts, string[] inputs, BuildNode pipeline, BuildEnvironment env)
        {
            // do comparisons in order from cheapest to most expensive to try to early out when a change is obvious
            // check 1: see if we have history for this output
            HistoryEntry entry;
            if (!history.TryGetValue(output.ToLower(), out entry))
                return true;

            // check 2: make sure the byproducts match
            var byproductSet = byproducts.Select(b => b.ToLower()).ToSet();
            if (!byproductSet.SetEquals(entry.Byproducts))
                return true;

            // check 3: compare number and type of pipeline stages
            var node = pipeline;
            for (int i = 0; i < entry.StageTypes.Count; i++)
            {
                if (node == null || node.GetType() != entry.StageTypes[i])
                    return true;

                node = node.InputNode;
            }

            // any nodes left over mean that the pipeline was changed
            if (node != null)
                return true;

            // check 4: check for pipeline processor changes
            node = pipeline;
            foreach (var stage in entry.StageHashes)
            {
                node = node.InputNode;
                if (stage != node.Hash())
                    return true;
            }

            // check 5: changes in outputs
            if (env.OutputChangeDetection != ChangeDetection.None)
            {
                if (CheckChanged(env.OutputChangeDetection, new FileInfo(env.ResolveOutput(output)), entry.OutputCache[0]))
                    return true;

                for (int i = 0; i < byproducts.Length; i++)
                {
                    if (CheckChanged(env.OutputChangeDetection, new FileInfo(env.ResolveOutput(byproducts[i])), entry.OutputCache[i + 1]))
                        return true;
                }
            }

            // check 6: changes in inputs
            if (env.InputChangeDetection != ChangeDetection.None)
            {

            }

            // at this point, we can safely say that the entire pipeline is the same. no need to do a build
            return false;
        }

        bool CheckChanged(ChangeDetection detection, FileInfo file, FileEntry entry)
        {
            if (!file.Exists)
                return true;

            if ((detection & ChangeDetection.Length) != 0 && file.Length != entry.Length)
                return true;

            if ((detection & ChangeDetection.Timestamp) != 0 && file.LastWriteTimeUtc != entry.Timestamp)
                return true;

            if ((detection & ChangeDetection.Hash) != 0 && HashFile(file) != entry.Hash)
                return true;

            return false;
        }

        string HashFile(FileInfo file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = file.OpenRead())
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
            }
        }
    }
}
