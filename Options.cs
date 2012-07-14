using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

// command line usage
[assembly: AssemblyUsage("Usage: amp [build-script] [plugin-directory]")]
[assembly: AssemblyInformationalVersion("1.0")]

namespace Ampere
{
    class Options : CommandLineOptionsBase
    {
        [Option("b", "build-script", Required=true,  HelpText = "The path to the build script to run. If not set, the compiler will search for a .cs file that matches the name of the current directory.")]
        public string BuildScript { get; set; }

        [Option("p", "plugins", HelpText = "The path to the directory containing plugin assemblies. If not set, the build script directory will be used.")]
        public string PluginDirectory { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, e => HelpText.DefaultParsingErrorsHandler(this, e));
        }
    }
}
