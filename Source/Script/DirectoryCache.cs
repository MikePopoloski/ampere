using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    public class DirectoryCache
    {
        BuildContext context;
        ILookup<string, string> files;

        public DirectoryCache(BuildContext context, string directory)
        {
            this.context = context;
            files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                .Select(f => f.Remove(0, directory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .ToLookup(f => Path.GetFileName(f), StringComparer.CurrentCultureIgnoreCase);
        }

        public string GetPath(string name)
        {
            var set = files[name];
            int count = set.Count();

            if (count > 1)
                context.Log.ErrorFormat("More than one content file matches name '{0}' when using a flatten resolve (files: {1})", name, string.Join(", ", set));
            if (count != 1)
                return null;

            return set.First();
        }
    }
}
