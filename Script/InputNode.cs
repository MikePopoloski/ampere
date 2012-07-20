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

        public override object Evaluate(BuildContext context)
        {
            var root = (OutputNode)GetTopNode();

            // resolve input names into full paths; if any fail, an error occurs
            var paths = new List<string>();
            foreach (var input in inputs)
            {
                var path = context.Env.ResolveInput(root.MatchResults.Result(input));
                if (string.IsNullOrEmpty(path))
                {
                    BuildContext.Log.ErrorFormat("Could not resolve input '{0}' (line {1}).", input, LineNumber);
                    return null;
                }

                paths.Add(path);
            }

            // as an optimization, determine whether the next node in the pipeline requires the data to be on disk
            // if that's the case, don't bother reading in a filestream
            if (OutputNode.RequiresInputOnDisk)
                return paths.ToArray();

            // otherwise, read in filestreams
            return paths.Select(p => File.OpenRead(p)).ToArray();
        }
    }
}
