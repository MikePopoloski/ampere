using log4net;
using Roslyn.Compilers;
using Roslyn.Scripting.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var context = new BuildContext();
            AppDomain.CurrentDomain.AssemblyResolve += (o, e) => Resolver(context, e);

            // initialize the logging system
            Logging.Initialize("%thread> %level - %message%newline", logLevel);

            var log = LogManager.GetLogger("main");

            // if we weren't given a build script, try to find one in the current directory
            string scriptPath = buildScript;
            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            {
                var file = Path.GetDirectoryName(Directory.GetCurrentDirectory());
                if (File.Exists(file + ".csx"))
                    scriptPath = file + ".csx";
                else if (File.Exists(file + ".cs"))
                    scriptPath = file + ".cs";
                else if (File.Exists("build.csx"))
                    scriptPath = "build.csx";
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
            context.Initialize(historyPath, fullRebuild);
            var scriptEngine = new ScriptEngine();
            var session = scriptEngine.CreateSession(context);

            // load plugins and assemblies
            session.AddReference(typeof(BuildContext).Assembly);
            session.AddReference(typeof(Enumerable).Assembly);
            session.AddReference(typeof(HashSet<>).Assembly);
            session.AddReference(typeof(ISet<>).Assembly);

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

        public static Assembly Resolver(BuildContext context, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name);
            if (name.Name.Contains(".resources"))   // hack to avoid trying to satisfy resource requests
                return null;

            // try to load one of the embedded assemblies
            var dllName = name.Name + ".dll";
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Ampere.Embedded." + dllName);
            if (stream != null)
            {
                var block = new byte[stream.Length];
                stream.Read(block, 0, block.Length);
                stream.Close();

                return Assembly.Load(block);
            }

            // otherwise, try the extra probing paths from the script environment
            if (context != null)
            {
                foreach (var pair in context.Env.ReferencePaths)
                {
                    if (pair.Item2)
                    {
                        var results = Directory.GetFiles(pair.Item1, dllName, SearchOption.AllDirectories);
                        if (results.Length == 1)
                            return Assembly.LoadFrom(results[0]);
                    }
                    else
                    {
                        var dllPath = Path.Combine(pair.Item1, dllName);
                        if (File.Exists(dllPath))
                            return Assembly.LoadFrom(dllPath);
                    }
                }
            }

            // otherwise, defer to the default loader
            return Assembly.LoadFrom(dllName);
        }
    }
}
