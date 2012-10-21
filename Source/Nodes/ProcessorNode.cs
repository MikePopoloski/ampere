using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    class ProcessorNode : TransientNode
    {
        public Func<BuildInstance, IEnumerable<object>, IEnumerable<object>> Processor
        {
            get;
            set;
        }

        public ProcessorNode(Func<BuildInstance, IEnumerable<object>, IEnumerable<object>> processor)
        {
            Processor = processor;
        }

        public ProcessorNode(Func<BuildInstance, IEnumerable<object>, object> processor)
            : this((i, s) => new[] { processor(i, s) })
        {
        }

        public override IEnumerable<object> Evaluate(BuildInstance instance, IEnumerable<object> inputs)
        {
            try
            {
                return Processor(instance, inputs);
            }
            catch (Exception e)
            {
                instance.Log(LogLevel.Error, e.ToString());
                return null;
            }
        }
    }
}
