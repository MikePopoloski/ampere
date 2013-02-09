using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    public static class Resolvers
    {
        public static Func<string, string> Flatten(DirectoryCache cache)
        {
            return new Func<string, string>(input =>
            {
                return cache.GetPath(input);
            });
        }

        public static Func<string, string> PassThrough()
        {
            return new Func<string, string>(input =>
            {
                return input;
            });
        }

        public static Func<string, string> Namespace()
        {
            return new Func<string, string>(input =>
            {
                return Path.GetFileNameWithoutExtension(input).Replace('.', '/') + Path.GetExtension(input);
            });
        }
    }
}
