using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using Roslyn.Compilers;
using Roslyn.Scripting;
using Roslyn.Scripting.CSharp;

namespace Ampere
{
    class Program
    {
        static readonly string[] Namespaces = new[] {
            "System",
            "System.IO",
            "System.Linq",
            "System.Collections.Generic",
            "System.Threading.Tasks"
        };

        static readonly string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ampere");

        static void Main(string[] args)
        {
            // parse CLI options
            var options = new Options();
            if (!CommandLineParser.Default.ParseArguments(args, options))
                return;

            // initialize the logging system
            Logging.Initialize("%thread> %level - %message%newline", options.LogLevel);
            var log = LogManager.GetLogger("main");

            // if we weren't given a build script, try to find one in the current directory
            string scriptPath = options.BuildScript;
            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            {
                var file = Path.GetDirectoryName(Directory.GetCurrentDirectory()) + ".cs";
                if (File.Exists(file))
                    scriptPath = file;
                else if (File.Exists("build.cs"))
                    scriptPath = "build.cs";
                else
                {
                    log.Error("Could not find or open build script.");
                    return;
                }
            }

            scriptPath = Path.GetFullPath(scriptPath);
            var pluginPath = Path.GetFullPath(options.PluginDirectory ?? Path.GetDirectoryName(scriptPath));
            Directory.SetCurrentDirectory(Path.GetDirectoryName(scriptPath));

            // create the script engine
            var context = new BuildContext(Path.Combine(DataDirectory, "history.dat"));
            var scriptEngine = new ScriptEngine();
            var session = Session.Create(context);

            // load plugins and assemblies
            session.AddReference(typeof(BuildContext).Assembly);
            session.AddReference(typeof(Enumerable).Assembly);
            session.AddReference(typeof(HashSet<>).Assembly);
            foreach (var file in Directory.EnumerateFiles(pluginPath, "*.dll"))
            {
                // check whether this is a managed assembly
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    session.AddReference(assembly);

                    log.InfoFormat("Loaded plugin: '{0}'", assembly);
                }
                catch (BadImageFormatException)
                {
                }
                catch (FileLoadException)
                {
                }
            }

            // import default namespaces
            session.ImportNamespace(typeof(BuildContext).Namespace);
            foreach (var n in Namespaces)
                session.ImportNamespace(n);

            try
            {
                // run the script
                log.InfoFormat("Running build script ({0})", scriptPath);
                log.InfoFormat("Build started at {0}", DateTime.Now);
                scriptEngine.ExecuteFile(scriptPath, session);

                context.WaitAll();
                context.Finished();
            }
            catch (CompilationErrorException e)
            {
                foreach (var error in e.Diagnostics)
                {
                    var position = error.Location.GetLineSpan(true);
                    log.ErrorFormat("({0}) {1}", position.StartLinePosition, error.Info.GetMessage());
                }
            }
        }
    }
}
