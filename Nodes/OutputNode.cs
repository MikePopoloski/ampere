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

        public IEnumerable<string> Byproducts
        {
            get;
            set;
        }

        public Match MatchResults
        {
            get;
            private set;
        }

        public OutputNode(string pattern, int priority, string[] byproducts)
        {
            Pattern = pattern;
            Priority = priority;
            Byproducts = byproducts;
        }

        public OutputNode Match(string name)
        {
            // allow non-regex wildcard strings to be used
            var current = Pattern;
            if (!Pattern.StartsWith("/") || !Pattern.EndsWith("/"))
                current = "^" + Regex.Escape(Pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";

            MatchResults = Regex.Match(name, current);
            return this;
        }

        public override IEnumerable<Stream> Evaluate(BuildContext context, IEnumerable<Stream> inputs)
        {
            // figure out the final names of each output
            var outputs = new List<string>();
            outputs.Add(MatchResults.Value);
            outputs.AddRange(Byproducts.Select(b => MatchResults.Result(b)));
            
            // make sure we have enough inputs to satisfy each output
            var inputArray = inputs.ToArray();
            if (inputArray.Length != outputs.Count)
            {
                context.Log.ErrorFormat("Number of inputs does not match number of outputs for '{0}' (line {1}).", MatchResults.Value, LineNumber);
                return null;
            }

            // match each input to an output name
            for (int i = 0; i < inputArray.Length; i++)
            {
                var outputPath = context.Env.ResolveOutput(outputs[i]);
                if (string.IsNullOrEmpty(outputPath))
                {
                    context.Log.ErrorFormat("Could not resolve output '{0}' (line {1}).", outputs[i], LineNumber);
                    return null;
                }

                 // if we have a filestream, we can do a straight file copy because we know it hasn't been changed
                var stream = inputArray[i];
                var file = stream as FileStream;
                if (file != null)
                    File.Copy(file.Name, outputPath, true);
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
