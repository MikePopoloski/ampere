using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ampere
{
    class OutputMatch
    {
        public OutputNode Node;
        public Match Match;

        public OutputMatch(OutputNode node, Match match)
        {
            Node = node;
            Match = match;
        }
    }

    class OutputNode : BuildNode
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

        public OutputNode(string pattern, int priority, string[] byproducts)
        {
            Pattern = pattern;
            Priority = priority;
            Byproducts = byproducts;
        }

        public OutputMatch Match(string name)
        {
            // allow non-regex wildcard strings to be used
            var current = Pattern;
            if (!Pattern.StartsWith("/") || !Pattern.EndsWith("/"))
                current = "^" + Regex.Escape(Pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";

            var match = Regex.Match(name, current);
            if (!match.Success)
                return null;

            return new OutputMatch(this, match);
        }
    }
}
