using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ampere
{
    class OutputNode : TransientNode
    {
        public string Pattern
        {
            get;
            set;
        }

        public int Priority
        {
            get;
            set;
        }

        public string[] Byproducts
        {
            get;
            set;
        }

        public OutputNode(string pattern, int priority, string[] byproducts)
        {
            Pattern = pattern;
            Priority = priority;
            Byproducts = byproducts;
        }

        public Match Match(string name)
        {
            // allow non-regex wildcard strings to be used
            var current = Pattern;
            if (!Pattern.StartsWith("/") || !Pattern.EndsWith("/"))
                current = "^" + Regex.Escape(Pattern).Replace(@"\*", "(.*)").Replace(@"\?", "(.)") + "$";

            return Regex.Match(name, current);
        }

        public bool ResolveNames(BuildInstance instance)
        {
            var paths = new List<string>();
            foreach (var output in Byproducts.Select(b => instance.Match.Result(b)))
            {
                var outputPath = instance.Env.ResolveOutput(output);
                if (string.IsNullOrEmpty(outputPath))
                {
                    instance.Log(LogLevel.Error, "Could not resolve output '{0}' (line {1}).", output, LineNumber);
                    return false;
                }

                paths.Add(outputPath);
            }

            instance.OutputPath = instance.Env.ResolveOutput(instance.OutputName);
            instance.Byproducts = paths.ToArray();

            return !string.IsNullOrEmpty(instance.OutputPath);
        }

        public override IEnumerable<object> Evaluate(BuildInstance instance, IEnumerable<object> inputs)
        {
            // figure out the final names of each output
            var outputs = new List<string>();
            outputs.Add(instance.OutputPath);
            outputs.AddRange(instance.Byproducts);

            // make sure we have enough inputs to satisfy each output
            var inputArray = inputs.ToArray();
            if (inputArray.Length != outputs.Count)
            {
                instance.Log(LogLevel.Error, "Number of inputs does not match number of outputs for '{0}' (line {1}).", instance.OutputName, LineNumber);
                return null;
            }

            // match each input to an output name
            for (int i = 0; i < inputArray.Length; i++)
            {
                // if we have a filestream, we can do a straight file copy because we know it hasn't been changed
                var outputPath = outputs[i];
                var stream = inputArray[i] as Stream;
                var file = stream as FileStream;
                if (file != null)
                    File.Copy(file.Name, outputPath, true);
                else if (stream == null)
                {
                    instance.Log(LogLevel.Error, "Inputs to Build() node must all be of type stream ('{0}' on line {1}).", instance.OutputName, LineNumber);
                    return null;
                }
                else
                {
                    // otherwise, write to file
                    stream.Seek(0, SeekOrigin.Begin);
                    using (var outputStream = File.OpenWrite(outputPath))
                        stream.CopyTo(outputStream);
                }

                stream.Close();
            }

            return Enumerable.Empty<Stream>();
        }
    }
}
