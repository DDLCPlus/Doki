using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Doki.Extensions
{

    public static class ConsoleUtils
    {
        private const int STD_OUTPUT_HANDLE = -11;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr handle);

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        public static void ShowConsole(string title = null)
        {
            AllocConsole();

            IntPtr stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
            var writer = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };
            Console.SetOut(writer);

            SetStdHandle(STD_OUTPUT_HANDLE, stdHandle);

            if (title != null)
                Console.Title = title;
        }

        public static void Log(string text)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("[Doki] ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("(");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(DateTime.Now.ToShortTimeString());
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(")");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(": " + text + "\n");
        }
    }
}
