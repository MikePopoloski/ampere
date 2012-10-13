﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using log4net;
using Roslyn.Compilers;
using Roslyn.Scripting;
using Roslyn.Scripting.CSharp;

namespace Ampere
{
    class Program
    {
        static FileWatcher Watcher = new FileWatcher();

        static void Main(string[] args)
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
                var runner = (ScriptRunner)domain.CreateInstanceAndUnwrap(typeof(ScriptRunner).Assembly.FullName, typeof(ScriptRunner).FullName);

                var results = runner.Run(options.BuildScript, options.PluginDirectory, options.LogLevel);
                AppDomain.Unload(domain);
                Console.WriteLine();

                if (results == null)
                {
                    Console.WriteLine("Press return to try again.");
                    Console.ReadLine();
                    continue;
                }

                if (!results.ShouldRunAgain)
                    break;

                Console.WriteLine("Waiting for changes...");
                WaitForChanges(options, results);
                Console.WriteLine();
            }
        }

        static void WaitForChanges(Options options, BuildResults results)
        {
            Watcher.Clear();

            var startupPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            Watcher.Add(startupPath, "*.cs");

            Directory.SetCurrentDirectory(startupPath);
            if (!string.IsNullOrEmpty(options.BuildScript))
                Watcher.Add(Path.GetDirectoryName(Path.GetFullPath(options.BuildScript)), "*.cs");

            if (!string.IsNullOrEmpty(options.PluginDirectory))
                Watcher.Add(Path.GetFullPath(options.PluginDirectory), "*.dll");

            if (results != null)
            {
                foreach (var path in results.ProbedPaths)
                    Watcher.Add(Path.GetFullPath(path), "*.*");
            }

            Watcher.Wait();
        }
    }
}
