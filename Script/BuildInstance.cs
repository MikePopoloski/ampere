﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ampere
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class BuildInstance
    {
        BuildContext context;

        public BuildEnvironment Env
        {
            get;
            private set;
        }

        public Match Match
        {
            get;
            private set;
        }

        public BuildNode Pipeline
        {
            get;
            private set;
        }

        public string[] Byproducts
        {
            get;
            private set;
        }

        public string[] Inputs
        {
            get;
            private set;
        }

        public string Output
        {
            get { return Match.Value; }
        }

        internal BuildInstance(BuildContext context, Match match, OutputNode pipeline, string[] inputs)
        {
            Env = context.Env;
            Match = match;
            Pipeline = pipeline;
            Byproducts = pipeline.Byproducts;
            Inputs = inputs;

            this.context = context;
        }

        public void Log(LogLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    context.Log.DebugFormat(message, args);
                    break;

                case LogLevel.Info:
                    context.Log.InfoFormat(message, args);
                    break;

                case LogLevel.Warning:
                    context.Log.WarnFormat(message, args);
                    break;

                case LogLevel.Error:
                    context.Log.ErrorFormat(message, args);
                    break;
            }
        }
    }
}
