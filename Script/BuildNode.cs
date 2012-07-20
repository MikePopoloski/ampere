﻿using System;
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

        public virtual bool RequiresInputOnDisk
        {
            get { return false; }
        }

        protected int LineNumber
        {
            get;
            private set;
        }

        protected BuildNode()
        {
            var stack = new StackTrace(3, true);
            LineNumber = stack.GetFrame(0).GetFileLineNumber();
        }

        public BuildNode GetBottomNode()
        {
            var node = this;
            while (node.InputNode != null)
                node = node.InputNode;

            return node;
        }

        public BuildNode GetTopNode()
        {
            var node = this;
            while (node.OutputNode != null)
                node = node.OutputNode;

            return node;
        }

        public abstract object Evaluate(BuildContext context);
    }
}
