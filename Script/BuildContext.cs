using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ampere
{
    /// <summary>
    /// The top-level context for the build script.
    /// </summary>
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

        public BuildNode Build(string pattern, params string[] additional)
        {
            return new OutputNode(pattern, additional);
        }

        public Task Begin(string name)
        {
        }
    }
}
