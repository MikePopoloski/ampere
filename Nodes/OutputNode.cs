using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ampere
{
    class OutputNode : TransientNode
    {
        public string Pattern
        {
            get;
            set;
        }

        public int Priority
        {
            get;
            set;
        }

        public IEnumerable<string> Byproducts
        {
            get;
            set;
        }

        public Match MatchResults
        {
            get;
            private set;
        }

        public OutputNode(string pattern, int priority, string[] byproducts)
        {
            Pattern = pattern;
            Priority = priority;
            Byproducts = byproducts;
        }

        public OutputNode Match(string name)
        {
            // allow non-regex wildcard strings to be used
            var current = Pattern;
            if (!Pattern.StartsWith("/") || !Pattern.EndsWith("/"))
                current = "^" + Regex.Escape(Pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";

            MatchResults = Regex.Match(name, current);
            return this;
        }

        public override object Evaluate(BuildContext context)
        {
            throw new NotImplementedException();
        }
    }
}
