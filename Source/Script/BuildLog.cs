using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ampere
{
    public class BuildLog
    {
        public bool Verbose
        {
            get;
            set;
        }

        public void Error(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[error] " + format, args);
            Console.ResetColor();
        }

        public void Warning(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[warning] " + format, args);
            Console.ResetColor();
        }

        public void Info(string format, params object[] args)
        {
            if (!Verbose)
                return;

            Console.WriteLine(format, args);
        }

        internal void Write(ConsoleColor? color, string format, params object[] args)
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

        internal void Write(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}
