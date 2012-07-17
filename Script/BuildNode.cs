using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    public abstract class BuildNode
    {
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
