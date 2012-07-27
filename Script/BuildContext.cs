﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static readonly ILog Log = LogManager.GetLogger("Build");

        List<OutputNode> rules = new List<OutputNode>();
        ConcurrentDictionary<string, Lazy<Task>> runningBuilds = new ConcurrentDictionary<string, Lazy<Task>>();

        public BuildEnvironment Env
        {
            get;
            private set;
        }

        public BuildContext()
        {
            Env = new BuildEnvironment();
        }

        public TransientNode Build(string pattern, params string[] additional)
        {
            return new OutputNode(pattern, 0, additional);
        }

        public TransientNode Build(string pattern, int priority, params string[] additional)
        {
            return new OutputNode(pattern, priority, additional);
        }

        public Task Start(string name)
        {
            // find best applicable rule
            var best = rules
                .Select(r => r.Match(name))
                .Where(m => m.MatchResults.Success)
                .GroupBy(m => m.Priority)
                .OrderBy(g => g.Key)
                .FirstOrDefault();

            if (best == null)
            {
                Log.ErrorFormat("No applicable rule found for asset '{0}'.", name);
                return null;
            }
            else if (best.Count() != 1)
                Log.WarnFormat("More than one rule with the same priority matches asset '{0}' (rules on lines: {1})", name, string.Join(", ", best));

            // we've found the rule we will use. queue up the task to build the asset, or return the current one if it's already being built
            var chosen = best.First();
            var task = runningBuilds.GetOrAdd(name, new Lazy<Task>(() =>
            {
                var job = Task.Run(() => InternalStart(name, chosen));
                job.ContinueWith(t => runningBuilds.Remove(name));
                return job;
            })).Value;

            // also register the task for any byproducts
            foreach(var byproduct in chosen.Byproducts)
            {
                var byproductName = chosen.MatchResults.Result(byproduct);
                runningBuilds.TryAdd(byproductName, new Lazy<Task>(() => task));
            }

            return task;
        }

        void InternalStart(string name, OutputNode rule)
        {
            // walk down the pipeline and build from the bottom-up
            var currentStage = rule.GetBottomNode();
            IEnumerable<Stream> state = null;
            while (currentStage != null)
            {
                // run the current stage, saving the results and passing them on to the next stage in the pipeline
                state = currentStage.Evaluate(this, state);
                if (state == null)
                    return;

                currentStage = currentStage.OutputNode;
            }

            Log.InfoFormat("Build for '{0}' successful.", name);
        }
    }
}
