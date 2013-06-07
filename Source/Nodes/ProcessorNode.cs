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

        public ProcessorNode(Func<BuildInstance, Stream, Stream> processor)
        {
            Processor = (instance, inputs) =>
            {
                var stream = inputs.FirstOrDefault() as Stream;
                if (stream == null)
                {
                    instance.Log.Error("Inputs to processor must be a single Stream object.");
                    return null;
                }

                return new[] { processor(instance, stream) };
            };
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
                instance.Log.Error(e.ToString());
                return null;
            }
        }
    }
}
