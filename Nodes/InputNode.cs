﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    class InputNode : BuildNode
    {
        List<string> inputs = new List<string>();

        public string[] Inputs
        {
            get { return inputs.ToArray(); }
        }

        public InputNode(string input, string[] additional)
        {
            inputs.Add(input);
            inputs.AddRange(additional);
        }

        public override IEnumerable<Stream> Evaluate(BuildInstance instance, IEnumerable<Stream> unused)
        {
            // resolve input names into full paths; if any fail, an error occurs
            var paths = new List<string>();
            foreach (var input in inputs)
            {
                var fullName = instance.Match.Result(input);
                var path = instance.Env.ResolveInput(fullName);
                if (string.IsNullOrEmpty(path))
                {
                    instance.Log(LogLevel.Error, "Could not resolve input '{0}' (line {1}).", fullName, LineNumber);
                    return null;
                }

                paths.Add(path);
            }

            // otherwise, open up the filestreams
            return paths.Select(p => File.OpenRead(p));
        }
    }
}
