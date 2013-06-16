using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ampere
{
    public class ArgProvider
    {
        static int GlobalId = 100;

        BuildInstance instance;
        Dictionary<int, string> inputCache = new Dictionary<int, string>();
        Dictionary<int, int> idCache = new Dictionary<int, int>();
        object[] inputs;
        int lineNumber;

        public string Name
        {
            get { return instance.OutputName; }
        }

        public string TempName
        {
            get { return instance.Env.ResolveTemp(instance.OutputName); }
        }

        public string TempDir
        {
            get { return instance.Env.TempPath; }
        }

        internal ArgProvider(BuildInstance instance, IEnumerable<object> inputs, int line)
        {
            this.instance = instance;
            this.inputs = inputs.ToArray();
            this.lineNumber = line;
        }

        public string Input(int index = 0)
        {
            string path;
            if (inputCache.TryGetValue(index, out path))
                return path;

            if (index < 0 || index >= inputs.Length)
            {
                instance.Log.Error("Index of Input ({0}) given to Run() is outside the bounds of available inputs ({1}). ('{2}' on line {3})", index, inputs.Length, instance.OutputName, lineNumber);
                return "<Error>";
            }

            var stream = inputs[index] as Stream;
            if (stream == null)
            {
                instance.Log.Error("Input to Run() node must be of type stream ('{0}' on line {1}).", instance.OutputName, lineNumber);
                return "<Error>";
            }

            var file = stream as FileStream;
            if (file != null)
                return file.Name;

            // otherwise, we need to write to a temporary file
            path = Path.GetTempFileName();
            using (var tempFile = File.Create(path))
                stream.CopyTo(tempFile);

            inputCache[index] = path;
            return path;
        }

        public string Output(int index = 0)
        {
            if (index > 0)
                return instance.Byproducts[index - 1];
            return instance.OutputPath;
        }

        public int TempId(int index = 0)
        {
            int id;
            if (idCache.TryGetValue(index, out id))
                return id;

            id = Interlocked.Increment(ref GlobalId);
            idCache[index] = id;

            return id;
        }
    }

    public abstract class RunNode : TransientNode
    {
        public abstract RunNode Arg(Func<ArgProvider, string> resolver);
        public abstract RunNode Result(Func<ArgProvider, string> resolver);
    }
}
