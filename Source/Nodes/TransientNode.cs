using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
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

        public BuildNode From(string input, params string[] inputs)
        {
            var node = new InputNode(input, inputs) { OutputNode = this };
            InputNode = node;

            return node;
        }
    }
}
