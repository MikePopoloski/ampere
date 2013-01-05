﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using log4net;

namespace Ampere
{
    /// <summary>
    /// The top-level context for the build script.
    /// </summary>
    public class BuildContext
    {
        BuildHistory history;
        List<OutputNode> rules = new List<OutputNode>();
        ConcurrentDictionary<string, Lazy<Task<BuildInstance>>> runningBuilds = new ConcurrentDictionary<string, Lazy<Task<BuildInstance>>>();
        ConcurrentBag<string> builtAssets = new ConcurrentBag<string>();
        string connectionInfo;
        bool fullRebuild;

        public BuildEnvironment Env
        {
            get;
            private set;
        }

        public ILog Log
        {
            get;
            private set;
        }

        public IList<string> ProbedPaths
        {
            get;
            private set;
        }

        public bool ShouldRunAgain
        {
            get;
            private set;
        }

        public BuildContext(string historyPath, bool fullRebuild)
        {
            Env = new BuildEnvironment(this);
            Log = LogManager.GetLogger("Build");
            history = new BuildHistory(historyPath);
            ProbedPaths = new List<string>();
            this.fullRebuild = fullRebuild;
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
                Log.ErrorFormat("No applicable rule found for asset '{0}'.", name);
                return null;
            }
            else if (best.Count() != 1)
                Log.WarnFormat("More than one rule with the same priority matches asset '{0}' (rules on lines: {1})", name, string.Join(", ", best.Select(b => b.Node.LineNumber)));

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
            {
                Log.ErrorFormat("FAILED! Build for '{0}'", name);
                return null;
            }

            // check to see if we even need to do this build
            if (!fullRebuild && !history.ShouldBuild(instance))
            {
                Log.InfoFormat("Skipping '{0}' (up-to-date).", name);
                return null;
            }

            // run the pipeline
            IEnumerable<object> state = null;
            while (currentStage != null)
            {
                // run the current stage, saving the results and passing them on to the next stage in the pipeline
                state = currentStage.Evaluate(instance, state);
                if (state == null)
                {
                    if (instance.IsTempBuild && currentStage is OutputNode)
                        return instance;

                    history.BuildFailed(instance);
                    return null;
                }

                currentStage = currentStage.OutputNode;
            }

            history.BuildSucceeded(instance);
            builtAssets.Add(instance.OutputName);
            foreach (var byproduct in instance.Byproducts)
                builtAssets.Add(byproduct);

            Log.InfoFormat("Build for '{0}' successful.", name);
            return instance;
        }
    }
}
