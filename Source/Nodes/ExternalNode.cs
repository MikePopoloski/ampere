using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ampere
{
    class ExternalNode : RunNode
    {
        List<Func<ArgProvider, string>> argProviders = new List<Func<ArgProvider, string>>();
        List<Func<ArgProvider, string>> resultProviders = new List<Func<ArgProvider, string>>();
        string fileName;
        string argumentFormat;
        RunOptions options;

        public ExternalNode(string fileName, string arguments, RunOptions options)
        {
            this.fileName = Environment.ExpandEnvironmentVariables(fileName);
            this.argumentFormat = arguments;
            this.options = options;
        }

        public override IEnumerable<object> Evaluate(BuildInstance instance, IEnumerable<object> inputs)
        {
            if (!File.Exists(fileName))
            {
                instance.Log.Error("Could not find external program '{0}'. (line {1})", fileName, LineNumber);
                return null;
            }

            if (resultProviders.Count == 0)
            {
                instance.Log.Error("Running an external tool requires at least one Result specifier. (line {0})", LineNumber);
                return null;
            }

            // perform argument replacement
            var argProvider = new ArgProvider(instance, inputs, LineNumber);
            string currentArguments = instance.Match.Result(argumentFormat);
            currentArguments = string.Format(currentArguments, argProviders.Select(p => p(argProvider)).ToArray());

            var startInfo = new ProcessStartInfo(fileName, currentArguments);
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = (options & RunOptions.RedirectOutput) != 0;
            startInfo.RedirectStandardError = (options & RunOptions.RedirectError) != 0;

            var process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (o, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    instance.Log.Info(e.Data);
            };

            process.ErrorDataReceived += (o, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    instance.Log.Error(e.Data);
            };

            process.Start();

            if (startInfo.RedirectStandardOutput)
                process.BeginOutputReadLine();

            if (startInfo.RedirectStandardError)
                process.BeginErrorReadLine();

            process.WaitForExit();

            if ((options & RunOptions.DontCheckResultCode) == 0 && process.ExitCode != 0)
            {
                instance.Log.Error("Running tool '{0}' failed with result code {1}. (line {2})", fileName, process.ExitCode, LineNumber);
                return null;
            }

            var results = new List<Stream>();
            foreach (var output in resultProviders.Select(p => p(argProvider)))
            {
                string path = instance.Match.Result(output);
                results.Add(File.OpenRead(path));
            }

            return results;
        }

        public override RunNode Arg(Func<ArgProvider, string> resolver)
        {
            argProviders.Add(resolver);
            return this;
        }

        public override RunNode Result(Func<ArgProvider, string> resolver)
        {
            resultProviders.Add(resolver);
            return this;
        }
    }
}
