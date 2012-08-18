using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

// command line usage
[assembly: AssemblyUsage("Usage: amp build-script [options]")]
[assembly: AssemblyInformationalVersion("1.0")]

namespace Ampere
{
    class Options : CommandLineOptionsBase
    {
        [ValueList(typeof(List<string>), MaximumElements = 1)]
        public IList<string> Items { get; set; }

        [Option("p", "plugins", HelpText = "The path to the directory containing plugin assemblies. If not set, the build script directory will be used.")]
        public string PluginDirectory { get; set; }

        public string BuildScript
        {
            get { return Items.Count > 0 ? Items[0] : null; }
        }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, e => HelpText.DefaultParsingErrorsHandler(this, e));
        }
    }
}
