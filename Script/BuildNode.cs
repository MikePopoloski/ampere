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
        public BuildNode InputNode
        {
            get;
            set;
        }

        public BuildNode OutputNode
        {
            get;
            set;
        }

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
    }
}
