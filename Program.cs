using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Ampere
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (!CommandLineParser.Default.ParseArguments(args, options))
                return;

            // if we weren't given a build script, try to find one in the current directory
            if (string.IsNullOrEmpty(options.BuildScript) || !File.Exists(options.BuildScript))
            {
                var file = Path.GetDirectoryName(Directory.GetCurrentDirectory()) + ".cs";
                if (File.Exists(file))
                    options.BuildScript = file;
                else if (File.Exists("build.cs"))
                    options.BuildScript = "build.cs";
                else
                {
                    Console.WriteLine("Could not find or open build script.");
                    return;
                }
            }

            
        }
    }
}
