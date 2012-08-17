using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
    class BuildHistory
    {
        string path;
        ConcurrentDictionary<string, HistoryEntry> history;

        public BuildHistory(string historyPath)
        {
            path = historyPath;
            if (File.Exists(historyPath))
                history = JsonConvert.DeserializeObject<ConcurrentDictionary<string, HistoryEntry>>(File.ReadAllText(historyPath));
            else
                history = new ConcurrentDictionary<string, HistoryEntry>();
        }

        public void Save()
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(history));
        }

        public void BuildSucceeded(BuildInstance instance)
        {
        }

        public void BuildFailed(BuildInstance instance)
        {
        }

        public bool ShouldBuild(BuildInstance instance)
        {
            // do comparisons in order from cheapest to most expensive to try to early out when a change is obvious
            // check 1: see if we have history for this output
            HistoryEntry entry;
            if (!history.TryGetValue(instance.Output.ToLower(), out entry))
                return true;

            // check 2: make sure the byproducts match
            var byproductSet = instance.Byproducts.Select(b => b.ToLower()).ToSet();
            if (!byproductSet.SetEquals(entry.Byproducts))
                return true;

            // check 3: compare number and type of pipeline stages
            var node = instance.Pipeline;
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
            node = instance.Pipeline;
            foreach (var stage in entry.StageHashes)
            {
                node = node.InputNode;
                if (stage != node.Hash())
                    return true;
            }

            // check 5: changes in inputs
            if (instance.Env.InputChangeDetection != ChangeDetection.None)
            {
                for (int i = 0; i < instance.Inputs.Length; i++)
                {
                    if (CheckChanged(instance.Env.InputChangeDetection, new FileInfo(instance.Env.ResolveInput(instance.Inputs[i])), entry.InputCache[i]))
                        return true;
                }
            }

            // check 6: changes in outputs
            if (instance.Env.OutputChangeDetection != ChangeDetection.None)
            {
                if (CheckChanged(instance.Env.OutputChangeDetection, new FileInfo(instance.Env.ResolveOutput(instance.Output)), entry.OutputCache[0]))
                    return true;

                for (int i = 0; i < instance.Byproducts.Length; i++)
                {
                    if (CheckChanged(instance.Env.OutputChangeDetection, new FileInfo(instance.Env.ResolveOutput(instance.Byproducts[i])), entry.OutputCache[i + 1]))
                        return true;
                }
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
