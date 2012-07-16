using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    public class BuildContext
    {
        public BuildEnvironment Env
        {
            get;
            private set;
        }

        public BuildContext()
        {
            Env = new BuildEnvironment();
        }

        public BuildNode BuildRule(string outputPattern)
        {
            return new BuildNode(outputPattern);
        }

        public void Build(string name)
        {
        }
    }
}
