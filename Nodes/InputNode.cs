using System;
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

        public InputNode(string input, string[] additional)
        {
            inputs.Add(input);
            inputs.AddRange(additional);
        }

        public override IEnumerable<Stream> Evaluate(BuildContext context, IEnumerable<Stream> unused)
        {
            var root = (OutputNode)GetTopNode();

            // resolve input names into full paths; if any fail, an error occurs
            var paths = new List<string>();
            foreach (var input in inputs)
            {
                var fullName = root.MatchResults.Result(input);
                var path = context.Env.ResolveInput(fullName);
                if (string.IsNullOrEmpty(path))
                {
                    context.Log.ErrorFormat("Could not resolve input '{0}' (line {1}).", fullName, LineNumber);
                    return null;
                }

                paths.Add(path);
            }

            // otherwise, open up the filestreams
            return paths.Select(p => File.OpenRead(p));
        }
    }
}
