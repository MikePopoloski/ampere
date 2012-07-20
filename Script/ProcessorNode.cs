using System;
using System.Collections.Generic;
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

        public override object Evaluate(BuildContext context)
        {
            throw new NotImplementedException();
        }
    }
}
