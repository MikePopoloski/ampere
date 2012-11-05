using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public int LineNumber
        {
            get;
            private set;
        }

        protected BuildNode()
        {
            var stack = new StackTrace(2, true);
            LineNumber = stack.GetFrame(0).GetFileLineNumber();
        }

        public BuildNode GetBottomNode()
        {
            var node = this;
            while (node.InputNode != null)
                node = node.InputNode;

            return node;
        }

        public abstract IEnumerable<object> Evaluate(BuildInstance instance, IEnumerable<object> inputs);

        public virtual string Hash()
        {
            return "";
        }
    }
}
