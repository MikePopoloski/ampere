using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    public class BuildLog
    {
        object sync = new object();

        public bool Verbose
        {
            get;
            set;
        }

        public void Error(string format, params object[] args)
        {
            Write(ConsoleColor.Red, "[error] " + format, args);
        }

        public void Warning(string format, params object[] args)
        {
            Write(ConsoleColor.Yellow, "[warning] " + format, args);
        }

        public void Info(string format, params object[] args)
        {
            if (!Verbose)
                return;

            Write(null, format, args);
        }

        public void Write(string format, params object[] args)
        {
            Write(null, format, args);
        }

        internal void Write(ConsoleColor? color, string format, params object[] args)
        {
            lock (sync)
            {
                if (color != null)
                {
                    Console.ForegroundColor = color.Value;
                    Console.WriteLine(format, args);
                    Console.ResetColor();
                }
                else
                    Console.WriteLine(format, args);
            }
        }


    }
}
