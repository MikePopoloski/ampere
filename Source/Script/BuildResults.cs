using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    [Serializable]
    class BuildResults
    {
        public bool ShouldRunAgain
        {
            get;
            set;
        }

        public IList<string> ProbedPaths
        {
            get;
            set;
        }
    }
}
