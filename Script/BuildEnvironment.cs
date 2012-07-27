using System;
using System.Collections.Generic;
using System.IO;
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
        BuildContext context;

        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string TempPath { get; set; }

        public Func<string, string> InputResolver { get; set; }
        public Func<string, string> OutputResolver { get; set; }

        public BuildEnvironment(BuildContext context)
        {
            this.context = context;

            TempPath = Path.GetTempPath();
        }

        public string ResolveInput(string name)
        {
            if (!Directory.Exists(InputPath))
            {
                context.Log.ErrorFormat("Current input path '{0}' does not exist.", InputPath);
                return null;
            }

            if (InputResolver == null)
            {
                context.Log.ErrorFormat("No input resolver function set.");
                return null;
            }

            var input = InputResolver(name);
            if (string.IsNullOrEmpty(input))
                return null;

            return Path.Combine(InputPath, input);
        }

        public string ResolveOutput(string name)
        {
            if (!Directory.Exists(OutputPath))
            {
                context.Log.ErrorFormat("Current output path '{0}' does not exist.", OutputPath);
                return null;
            }

            if (OutputResolver == null)
            {
                context.Log.ErrorFormat("No output resolver function set.");
                return null;
            }

            var output = OutputResolver(name);
            if (string.IsNullOrEmpty(output))
                return null;

            return Path.Combine(OutputPath, output);
        }

        public string GetTemporaryFile()
        {
            if (!Directory.Exists(TempPath))
            {
                context.Log.ErrorFormat("Current temp path '{0}' does not exist.", TempPath);
                return null;
            }

            return Path.Combine(TempPath, Guid.NewGuid().ToString() + ".tmp");
        }
    }
}
