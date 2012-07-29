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
        public object Processor
        {
            get;
            set;
        }

        public ProcessorNode(object processor)
        {
            Processor = processor;
        }

        public override IEnumerable<Stream> Evaluate(BuildInstance instance, IEnumerable<Stream> inputs)
        {
            dynamic p = Processor;
            return p.Run(instance, inputs);
        }
    }
}
