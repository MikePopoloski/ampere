using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    public enum NodeType
    {
        StringList,
        Processor
    }

    public class BuildNode
    {
        public BuildNode Input
        {
            get;
            set;
        }

        public BuildNode Output
        {
            get;
            set;
        }

        public string[] Strings
        {
            get;
            set;
        }

        public object Processor
        {
            get;
            set;
        }

        public NodeType NodeType
        {
            get;
            set;
        }

        public BuildNode(object processor)
        {
            NodeType = NodeType.Processor;
            Processor = processor;
        }

        public BuildNode(params string[] inputs)
        {
            NodeType = NodeType.StringList;
            Strings = inputs;
        }

        public BuildNode Using(object processor)
        {
            return Input = new BuildNode(processor) { Output = this };
        }

        public BuildNode From(params string[] inputs)
        {
            return Input = new BuildNode(inputs) { Output = this };
        }
    }
}
