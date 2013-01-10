using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    [Flags]
    public enum ChangeDetection
    {
        None = 0,
        Length = 0x2,
        Timestamp = 0x4,
        Hash = 0x8
    }

    /// <summary>
    /// Allows the build script to interact with the host environment.
    /// </summary>
    public class BuildEnvironment
    {
        string inputPath;

        public BuildContext Context
        {
            get;
            private set;
        }

        public ChangeDetection OutputChangeDetection { get; set; }
        public ChangeDetection InputChangeDetection { get; set; }

        public string InputPath
        {
            get { return inputPath; }
            set
            {
                if (inputPath == value)
                    return;

                inputPath = value;
                Context.ProbedPaths.Add(inputPath);
            }
        }

        public string OutputPath { get; set; }
        public string TempPath { get; set; }

        public Func<string, string> InputResolver { get; set; }
        public Func<string, string> OutputResolver { get; set; }

        public bool CreateOutputDirectory { get; set; }
        public bool WriteTempBuilds { get; set; }

        public BuildEnvironment(BuildContext context)
        {
            Context = context;
            TempPath = Path.GetTempPath();

            OutputChangeDetection = ChangeDetection.Length;
            InputChangeDetection = ChangeDetection.Length /*| ChangeDetection.Timestamp*/ | ChangeDetection.Hash;

            InputResolver = Resolvers.PassThrough();
            OutputResolver = Resolvers.PassThrough();
        }

        public string ResolveInput(string name)
        {
            if (!Directory.Exists(InputPath))
            {
                Context.Log.ErrorFormat("Current input path '{0}' does not exist.", InputPath);
                return null;
            }

            if (InputResolver == null)
            {
                Context.Log.ErrorFormat("No input resolver function set.");
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
                if (CreateOutputDirectory)
                    Directory.CreateDirectory(OutputPath);
                else
                {
                    Context.Log.ErrorFormat("Current output path '{0}' does not exist.", OutputPath);
                    return null;
                }
            }

            if (OutputResolver == null)
            {
                Context.Log.ErrorFormat("No output resolver function set.");
                return null;
            }

            var output = OutputResolver(name);
            if (string.IsNullOrEmpty(output))
                return null;

            var path = Path.Combine(OutputPath, output);
            Context.ProbedPaths.Add(Path.GetDirectoryName(path));

            return path;
        }

        public string ResolveTemp(string name)
        {
            if (OutputResolver == null)
            {
                Context.Log.ErrorFormat("No output resolver function set.");
                return null;
            }

            var output = OutputResolver(name);
            if (string.IsNullOrEmpty(output))
                return null;

            return Path.Combine(TempPath, output);
        }
    }
}
