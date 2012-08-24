using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;

namespace Ampere
{
    static class Logging
    {
        static readonly Level[] LogLevels = { Level.Error, Level.Warn, Level.Info, Level.Debug };

        public static void Initialize(string logLayout, int level)
        {
            level = Clamp(level, 0, 3);

            var appender = new ColoredConsoleAppender();
            appender.Layout = new PatternLayout(logLayout);
            appender.Threshold = LogLevels[level];
            appender.ActivateOptions();

            BasicConfigurator.Configure(appender);
        }

        static T Clamp<T>(T input, T min, T max)
        {
            var comparer = Comparer<T>.Default;
            if (comparer.Compare(input, min) < 0)
                return min;

            if (comparer.Compare(input, max) > 0)
                return max;

            return input;
        }
    }
}
