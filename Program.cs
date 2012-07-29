using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        static void Main(string[] args)
        {
            var appender = new ConsoleAppender();
            appender.Layout = new PatternLayout("%timestamp [%thread] %level - %message%newline");
            BasicConfigurator.Configure(appender);
            var log = LogManager.GetLogger("main");

            var options = new Options();
            if (!CommandLineParser.Default.ParseArguments(args, options))
                return;

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
            var pluginPath = options.PluginDirectory ?? Path.GetDirectoryName(scriptPath);

            // create the script engine
            var context = new BuildContext();
            var scriptEngine = new ScriptEngine();
            var session = Session.Create(context);

            // load plugins and assemblies
            session.AddReference(typeof(BuildContext).Assembly);
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

            try
            {
                // run the script
                log.InfoFormat("Starting build script ({0})...", Path.GetFullPath(options.BuildScript));
                scriptEngine.ExecuteFile(options.BuildScript, session);
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
