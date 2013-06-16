using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    [Flags]
    public enum RunOptions
    {
        None = 0,
        RedirectOutput = 1,
        RedirectError = 2,
        DontCheckResultCode = 4
    }

    /// <summary>
    /// Contains operators that are viable for various intermediate stages of a pipeline.
    /// </summary>
    public abstract class TransientNode : BuildNode
    {
        public TransientNode Using(Func<BuildInstance, IEnumerable<object>, IEnumerable<object>> processor)
        {
            var node = new ProcessorNode(processor) { OutputNode = this };
            InputNode = node;

            return node;
        }

        public TransientNode Using(Func<BuildInstance, IEnumerable<object>, object> processor)
        {
            var node = new ProcessorNode(processor) { OutputNode = this };
            InputNode = node;

            return node;
        }

        public TransientNode Using(Func<BuildInstance, Stream, Stream> processor)
        {
            var node = new ProcessorNode(processor) { OutputNode = this };
            InputNode = node;

            return node;
        }

        public RunNode Run(string fileName, string arguments, RunOptions options)
        {
            var node = new ExternalNode(fileName, arguments, options) { OutputNode = this };
            InputNode = node;

            return node;
        }

        public RunNode Run(string fileName, string arguments)
        {
            var node = new ExternalNode(fileName, arguments, RunOptions.RedirectError) { OutputNode = this };
            InputNode = node;

            return node;
        }

        public BuildNode From(string input, params string[] inputs)
        {
            var node = new InputNode(input, inputs) { OutputNode = this };
            InputNode = node;

            return node;
        }
    }
}
