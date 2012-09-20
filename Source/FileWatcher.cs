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

        public void Add(string path, string filter)
        {
            if (Directory.Exists(path))
                watchers.Add(new FileSystemWatcher(path, filter));
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
        }
    }
}
