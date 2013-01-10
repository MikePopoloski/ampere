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
    class ExternalNode : TransientNode
    {
        const string KnownReplacements = @"\$\(Output(\[\d\])?\)|\$\(Input(\[\d\])\)|\$\(Name\)|\$\(TempName\)|\$\(TempDir\)|\$\d";

        string fileName;
        string arguments;
        string[] outputs;
        RunOptions options;

        public ExternalNode(string fileName, string arguments, RunOptions options, string[] outputs)
        {
            this.fileName = Environment.ExpandEnvironmentVariables(fileName);
            this.arguments = arguments;
            this.outputs = outputs;
            this.options = options;
        }

        public override IEnumerable<object> Evaluate(BuildInstance instance, IEnumerable<object> inputs)
        {
            if (!File.Exists(fileName))
            {
                instance.Log(LogLevel.Error, "Could not find external program '{0}'. (line {1})", fileName, LineNumber);
                return null;
            }

            // perform argument replacement
            var inputArray = inputs.ToArray();
            string currentArguments = Regex.Replace(arguments, KnownReplacements, m => Replacer(instance, inputArray, m));

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
                    instance.Log(LogLevel.Info, e.Data);
            };

            process.ErrorDataReceived += (o, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    instance.Log(LogLevel.Error, e.Data);
            };

            process.Start();

            if (startInfo.RedirectStandardOutput)
                process.BeginOutputReadLine();

            if (startInfo.RedirectStandardError)
                process.BeginErrorReadLine();

            process.WaitForExit();

            if ((options & RunOptions.DontCheckResultCode) == 0 && process.ExitCode != 0)
            {
                instance.Log(LogLevel.Error, "Running tool '{0}' failed with result code {1}. (line {2})", fileName, process.ExitCode, LineNumber);
                return null;
            }

            var results = new List<Stream>();
            foreach (var output in outputs)
            {
                string path = Regex.Replace(output, KnownReplacements, m => Replacer(instance, inputArray, m));
                results.Add(File.OpenRead(path));
            }

            return results;
        }

        string Replacer(BuildInstance instance, object[] inputs, Match match)
        {
            if (match.Value.Length >= 3)
            {
                char type = match.Value[2];
                if (type == 'N')
                    return instance.OutputName;

                if (match.Value.Contains("TempName"))
                    return instance.Env.ResolveTemp(instance.OutputName);

                if (match.Value.Contains("TempDir"))
                    return instance.Env.TempPath;

                if (type == 'I')
                {
                    int index = GetIndex(match);
                    if (index < 0 || index >= inputs.Length)
                    {
                        instance.Log(LogLevel.Error, "Index of Input ({0}) given to Run() is outside the bounds of available inputs ({1}). ('{2}' on line {3})", index, inputs.Length, instance.OutputName, LineNumber);
                        return "<Error>";
                    }

                    var stream = inputs[index] as Stream;
                    if (stream == null)
                    {
                        instance.Log(LogLevel.Error, "Input to Run() node must be of type stream ('{0}' on line {1}).", instance.OutputName, LineNumber);
                        return "<Error>";
                    }

                    var file = stream as FileStream;
                    if (file != null)
                        return file.Name;

                    // otherwise, we need to write to a temporary file
                    var path = Path.GetTempFileName();
                    using (var tempFile = File.Create(path))
                        stream.CopyTo(tempFile);

                    return path;
                }

                if (type == 'O')
                {
                    if (match.Captures.Count > 2)
                    {
                        int index = GetIndex(match) - 1;
                        if (index >= 0)
                            return instance.Byproducts[index];
                    }

                    return instance.OutputPath;
                }
            }

            // this is a regex replace string for output name matches
            return instance.Match.Result(match.Value);
        }

        static int GetIndex(Match match)
        {
            return Convert.ToInt32(match.Groups[2].Value[1].ToString());
        }
    }
}
