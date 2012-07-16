using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (string.IsNullOrEmpty(options.BuildScript) || !File.Exists(options.BuildScript))
            {
                var file = Path.GetDirectoryName(Directory.GetCurrentDirectory()) + ".cs";
                if (File.Exists(file))
                    options.BuildScript = file;
                else if (File.Exists("build.cs"))
                    options.BuildScript = "build.cs";
                else
                {
                    log.Error("Could not find or open build script.");
                    return;
                }
            }
            
            // create the script engine
            var context = new BuildContext();
            var scriptEngine = new ScriptEngine();
            var session = Session.Create(context);

            // load plugins and assemblies
            session.AddReference(typeof(BuildContext).Assembly);

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
