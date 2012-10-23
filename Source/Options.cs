using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

// command line usage
[assembly: AssemblyUsage("Usage: amp [build-script] [plugin-directory] [options]")]
[assembly: AssemblyInformationalVersion("1.0")]

namespace Ampere
{
    class Options : CommandLineOptionsBase
    {
        [Option("l", "loglevel", HelpText = "Indicates the level of logging output, from 0 to 3. Higher values show more information.")]
        public int LogLevel
        {
            get;
            set;
        }

        [ValueList(typeof(List<string>), MaximumElements = 1)]
        public IList<string> Items { get; set; }

        public string BuildScript
        {
            get { return Items.Count > 0 ? Items[0] : null; }
        }

        public Options()
        {
            // default log level is to show everything
            LogLevel = 3;
        }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, e => HelpText.DefaultParsingErrorsHandler(this, e));
        }
    }
}
