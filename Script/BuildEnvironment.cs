using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    /// <summary>
    /// Allows the build script to interact with the host environment.
    /// </summary>
    public class BuildEnvironment
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }

        public string ResolveInput(string name)
        {
            throw new NotImplementedException();
        }

        public string ResolveOutput(string name)
        {
            throw new NotImplementedException();
        }
    }
}
