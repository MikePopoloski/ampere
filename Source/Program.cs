﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Roslyn.Compilers;
using Roslyn.Scripting;
using Roslyn.Scripting.CSharp;
using System.Threading.Tasks;
using System.Threading;

namespace Ampere
{
    class Program
    {
        static FileWatcher Watcher = new FileWatcher();
        static bool nextBuildIsRebuild;
        static readonly string StartupPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (o, e) => ScriptRunner.Resolver(null, e);
            Run(args);
        }

        static void Run(string[] args)
        {
            // parse CLI options
            var options = new Options();
            if (!CommandLineParser.Default.ParseArguments(args, options))
                return;

            // run continuously if that's what we specified on the command line
            while (true)
            {
                // run in a separate domain so we can unload assemblies
                var domain = AppDomain.CreateDomain("Script Runner", null, AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.RelativeSearchPath, true);
                domain.AssemblyResolve += (o, e) => ScriptRunner.Resolver(null, e);

                var runner = (ScriptRunner)domain.CreateInstanceAndUnwrap(typeof(ScriptRunner).Assembly.FullName, typeof(ScriptRunner).FullName);
                BuildResults results = null;

                try
                {
                    results = runner.Run(options.BuildScript, options.LogLevel, nextBuildIsRebuild);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                nextBuildIsRebuild = false;
                AppDomain.Unload(domain);
                Console.WriteLine();
                Directory.SetCurrentDirectory(StartupPath);

                if (results == null)
                {
                    Console.WriteLine("Press return to try again.");
                    Console.ReadLine();
                    continue;
                }

                if (!results.ShouldRunAgain)
                    break;

                Console.WriteLine("Waiting for changes... (press enter to force a rebuild)");
                WaitForChanges(options, results);
                Console.WriteLine();
            }
        }

        static void WaitForChanges(Options options, BuildResults results)
        {
            Watcher.Clear();
            Watcher.Add(StartupPath, "*.csx");
            Watcher.Add(StartupPath, "*.cs");

            if (!string.IsNullOrEmpty(options.BuildScript))
            {
                Watcher.Add(Path.GetDirectoryName(Path.GetFullPath(options.BuildScript)), "*.csx");
                Watcher.Add(Path.GetDirectoryName(Path.GetFullPath(options.BuildScript)), "*.cs");
            }

            if (results != null)
            {
                foreach (var path in results.ProbedPaths)
                    Watcher.Add(Path.GetFullPath(path), "*.*");

                foreach (var path in results.LoadedPlugins)
                    Watcher.Add(path, "*.dll");
            }

            var waitTask = Task.Run(() => Watcher.Wait());
            while (!waitTask.IsCompleted)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        nextBuildIsRebuild = true;
                        break;
                    }
                }

                Thread.Sleep(0);
            }
        }
    }
}
