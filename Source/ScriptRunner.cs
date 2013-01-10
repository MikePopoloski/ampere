using log4net;
using Roslyn.Compilers;
using Roslyn.Scripting.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ampere
{
    class ScriptRunner : MarshalByRefObject
    {
        static readonly string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ampere");
        static readonly string[] Namespaces = new[] {
            "System",
            "System.IO",
            "System.Linq",
            "System.Collections.Generic",
            "System.Threading.Tasks"
        };

        public BuildResults Run(string buildScript, int logLevel, bool fullRebuild)
        {
            // initialize the logging system
            Logging.Initialize("%thread> %level - %message%newline", logLevel);

            var log = LogManager.GetLogger("main");

            // if we weren't given a build script, try to find one in the current directory
            string scriptPath = buildScript;
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
                    return null;
                }
            }

            scriptPath = Path.GetFullPath(scriptPath);
            Directory.SetCurrentDirectory(Path.GetDirectoryName(scriptPath));

            // create the script engine
            string historyPath = Path.Combine(DataDirectory, Murmur.Hash(scriptPath, 144) + "_history.dat");
            var context = new BuildContext(historyPath, fullRebuild);
            var scriptEngine = new ScriptEngine();
            var session = scriptEngine.CreateSession(context);

            // load plugins and assemblies
            session.AddReference(typeof(BuildContext).Assembly);
            session.AddReference(typeof(Enumerable).Assembly);
            session.AddReference(typeof(HashSet<>).Assembly);

            var code = File.ReadAllText(scriptPath);
            var buildResults = new BuildResults();

            // import default namespaces
            session.ImportNamespace(typeof(BuildContext).Namespace);
            foreach (var n in Namespaces)
                session.ImportNamespace(n);

            try
            {
                // run the script
                var startTime = DateTime.Now;
                log.InfoFormat("Running build script ({0})", scriptPath);
                log.InfoFormat("Build started at {0}", startTime);
                session.ExecuteFile(scriptPath);

                context.WaitAll();
                context.Finished();

                log.InfoFormat("Build finished ({0:N2} seconds)", (DateTime.Now - startTime).TotalSeconds);
            }
            catch (CompilationErrorException e)
            {
                foreach (var error in e.Diagnostics)
                {
                    var position = error.Location.GetLineSpan(true);
                    log.ErrorFormat("({0}) {1}", position.StartLinePosition, error.Info.GetMessage());
                }

                return null;
            }

            buildResults.ShouldRunAgain = context.ShouldRunAgain;
            buildResults.ProbedPaths = context.ProbedPaths.Select(p => Path.GetFullPath(p)).ToList();
            return buildResults;
        }
    }
}
