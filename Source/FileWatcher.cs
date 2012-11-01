using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    class FileWatcher
    {
        List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        Dictionary<string, HashSet<string>> paths = new Dictionary<string, HashSet<string>>();

        public void Add(string path, string filter)
        {
            HashSet<string> filters;
            if (!paths.TryGetValue(path, out filters))
            {
                filters = new HashSet<string>();
                paths.Add(path, filters);
            }

            if (filters.Contains(filter))
                return;

            if (!Directory.Exists(path))
                return;

            watchers.Add(new FileSystemWatcher(path, filter));
            filters.Add(filter);
        }

        public void Wait()
        {
            var tasks = watchers.Select(w =>
                Task.Run(() =>
                    w.WaitForChanged(WatcherChangeTypes.All)
            )).ToArray();

            Task.WaitAny(tasks);
        }

        public void Clear()
        {
            foreach (var watcher in watchers)
                watcher.Dispose();

            watchers.Clear();
            paths.Clear();
        }
    }
}
