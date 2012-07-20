using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    public abstract class BuildNode
    {
        protected int LineNumber
        {
            get;
            private set;
        }

        protected BuildNode()
        {
            var stack = new StackTrace(2, true);
            LineNumber = stack.GetFrame(0).GetFileLineNumber();
        }

        public BuildNode Using(object processor)
        {
            return new ProcessorNode(processor);
        }

        public InputBuildNode From(string input, params string[] inputs)
        {
            return new InputBuildNode(input, inputs);
        }
    }
}
