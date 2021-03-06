﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ampere
{
    /// <summary>
    /// The top-level context for the build script.
    /// </summary>
    public class BuildContext
    {
        public class BuildStats
        {
            public int Failed;
            public int Succeeded;
            public int Skipped;
        }

        BuildHistory history;
        List<OutputNode> rules = new List<OutputNode>();
        ConcurrentDictionary<string, Lazy<Task<BuildInstance>>> runningBuilds = new ConcurrentDictionary<string, Lazy<Task<BuildInstance>>>();
        ConcurrentBag<string> builtAssets = new ConcurrentBag<string>();
        ConcurrentBag<string> allAssets = new ConcurrentBag<string>();
        string connectionInfo;

        public BuildEnvironment Env
        {
            get;
            private set;
        }

        public BuildLog Log
        {
            get;
            private set;
        }

        public ConcurrentBag<string> ProbedPaths
        {
            get;
            private set;
        }

        public bool ShouldRunAgain
        {
            get;
            private set;
        }

        public bool FullRebuild
        {
            get;
            set;
        }

        public BuildStats Stats
        {
            get;
            private set;
        }

        public IEnumerable<string> AllAssets
        {
            get { return allAssets.ToSet(); }
        }

        public BuildContext()
        {
            Stats = new BuildStats();
            Env = new BuildEnvironment(this);
            ProbedPaths = new ConcurrentBag<string>();
        }

        public void Initialize(string historyPath, bool fullRebuild, BuildLog log)
        {
            Log = log;
            history = new BuildHistory(this, historyPath);
            FullRebuild = fullRebuild;
        }

        public DirectoryCache CreateDirectoryCache(string directory)
        {
            return new DirectoryCache(this, directory);
        }

        public TransientNode Build(string pattern, params string[] additional)
        {
            var node = new OutputNode(pattern, 0, additional);
            rules.Add(node);

            return node;
        }

        public TransientNode Build(string pattern, int priority, params string[] additional)
        {
            var node = new OutputNode(pattern, priority, additional);
            rules.Add(node);

            return node;
        }

        public void WaitAll()
        {
            while (runningBuilds.Count > 0)
                Task.WaitAll(runningBuilds.Values.Select(l => l.Value).ToArray());
        }

        public void Notify(string connectionInfo)
        {
            this.connectionInfo = connectionInfo;
        }

        public void RunAgain()
        {
            ShouldRunAgain = true;
        }

        public void Finished()
        {
            // called when the build is completely done and ready to exit
            history.Save();

            if (connectionInfo != null)
                Notifier.Notify(connectionInfo, builtAssets.ToList());
        }

        public Task<BuildInstance> Start(string name, bool tempBuild = false)
        {
            // find best applicable rule
            var best = rules
                .Select(r => new { Node = r, Match = r.Match(name) })
                .Where(m => m.Match.Success)
                .GroupBy(m => m.Node.Priority)
                .OrderBy(g => g.Key)
                .FirstOrDefault();

            if (best == null)
            {
                Log.Error("No applicable rule found for asset '{0}'.", name);
                Interlocked.Increment(ref Stats.Failed);
                return null;
            }
            else if (best.Count() != 1)
                Log.Warning("More than one rule with the same priority matches asset '{0}' (rules on lines: {1})", name, string.Join(", ", best.Select(b => b.Node.LineNumber)));

            // we've found the rule we will use. queue up the task to build the asset, or return the current one if it's already being built
            var chosen = best.First();
            var task = runningBuilds.GetOrAdd(name, new Lazy<Task<BuildInstance>>(() =>
            {
                var instance = new BuildInstance(this, chosen.Match, chosen.Node, tempBuild);
                var job = Task.Run(() => InternalStart(name, chosen.Node, instance));
                job.ContinueWith(t => runningBuilds.Remove(name));
                return job;
            })).Value;

            // also register the task for any byproducts
            foreach (var byproduct in chosen.Node.Byproducts)
            {
                var byproductName = chosen.Match.Result(byproduct);
                runningBuilds.TryAdd(byproductName, new Lazy<Task<BuildInstance>>(() => task));
            }

            return task;
        }

        BuildInstance InternalStart(string name, OutputNode rule, BuildInstance instance)
        {
            // walk down the pipeline and build from the bottom-up
            var currentStage = rule.GetBottomNode();
            var inputNode = currentStage as InputNode;

            if (!inputNode.ResolveNames(instance) || !rule.ResolveNames(instance))
                return BuildFailed(name, instance);

            // check to see if we even need to do this build
            if (!FullRebuild && !history.ShouldBuild(instance))
            {
                allAssets.Add(name);
                foreach (var entry in history.GetDependencies(instance.OutputName))
                    allAssets.Add(entry);

                Log.Info("Skipping '{0}' (up-to-date).", name);
                instance.Status = BuildStatus.Skipped;
                Interlocked.Increment(ref Stats.Skipped);
                return instance;
            }

            // run the pipeline
            IEnumerable<object> state = null;
            while (currentStage != null)
            {
                // run the current stage, saving the results and passing them on to the next stage in the pipeline
                try
                {
                    state = currentStage.Evaluate(instance, state);
                }
                catch (Exception e)
                {
                    Log.Error("Exception thrown while building '{0}': {1}", name, e);
                    return BuildFailed(name, instance);
                }

                if (state == null)
                {
                    if (instance.IsTempBuild && currentStage is OutputNode)
                        return instance;

                    return BuildFailed(name, instance);
                }

                currentStage = currentStage.OutputNode;
            }

            if (instance.Status == BuildStatus.Failed)
                return BuildFailed(name, instance);

            history.BuildSucceeded(instance);
            builtAssets.Add(instance.OutputName);
            allAssets.Add(instance.OutputName);
            foreach (var byproduct in instance.Byproducts)
            {
                allAssets.Add(byproduct);
                builtAssets.Add(byproduct);
            }

            Log.Write("Build for '{0}' successful.", name);
            instance.Status = BuildStatus.Succeeded;
            Interlocked.Increment(ref Stats.Succeeded);
            return instance;
        }

        BuildInstance BuildFailed(string name, BuildInstance instance)
        {
            history.BuildFailed(instance);
            Log.Error("FAILED! Build for '{0}'", name);
            instance.Status = BuildStatus.Failed;
            Interlocked.Increment(ref Stats.Failed);
            return instance;
        }
    }
}
