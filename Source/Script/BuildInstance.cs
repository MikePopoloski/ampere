using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ampere
{
    public enum BuildStatus
    {
        Pending,
        Succeeded,
        Skipped,
        Failed
    }

    public class BuildInstance
    {
        BuildContext context;

        internal List<BuildInstance> TempBuilds
        {
            get;
            private set;
        }

        internal List<string> Dependencies
        {
            get;
            private set;
        }

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

        public BuildNode Pipeline
        {
            get;
            private set;
        }

        public Match Match
        {
            get;
            private set;
        }

        public bool IsTempBuild
        {
            get;
            private set;
        }

        public string[] Byproducts
        {
            get;
            internal set;
        }

        public string[] Inputs
        {
            get;
            internal set;
        }

        public string OutputPath
        {
            get;
            internal set;
        }

        public IEnumerable<object> Results
        {
            get;
            internal set;
        }

        public BuildStatus Status
        {
            get;
            internal set;
        }

        public string OutputName
        {
            get { return Match.Value; }
        }

        internal BuildInstance(BuildContext context, Match match, OutputNode pipeline, bool tempBuild)
        {
            Env = context.Env;
            Match = match;
            Pipeline = pipeline;
            Byproducts = pipeline.Byproducts;
            IsTempBuild = tempBuild;
            Status = BuildStatus.Pending;

            this.context = context;
            TempBuilds = new List<BuildInstance>();
            Dependencies = new List<string>();
            Log = context.Log;
        }

        public Task<BuildInstance> Start(string name)
        {
            Dependencies.Add(name);
            return context.Start(name);
        }

        public BuildInstance StartTemp(string name)
        {
            var task = context.Start(name, true);
            if (task == null)
                return null;

            var result = task.Result;
            if (result == null || result.Status == BuildStatus.Failed)
                Status = BuildStatus.Failed;
            else
                TempBuilds.Add(result);

            return result;
        }
    }
}
