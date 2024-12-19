using System;
using System.IO;
using System.Runtime.InteropServices;

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

        public static void Log(string moduleName, string text)
        {
            ColourWrite([
                new ColouredText($"[Info] ", ConsoleColor.White),
                new ColouredText($"({DateTime.Now.ToShortTimeString()}) ", ConsoleColor.DarkGray),
                new ColouredText($"{moduleName}", ConsoleColor.Magenta),
                new ColouredText($": {text}\n", ConsoleColor.White)
            ]);
        }

        public static void Debug(string moduleName, string text)
        {
            ColourWrite([
                new ColouredText($"[Debug] ", ConsoleColor.Gray),
                new ColouredText($"({DateTime.Now.ToShortTimeString()}) ", ConsoleColor.DarkGray),
                new ColouredText($"{moduleName}", ConsoleColor.Magenta),
                new ColouredText($": {text}\n", ConsoleColor.Gray)
            ]);
        }

        public static void Warning(string moduleName, string text)
        {
            ColourWrite([
                new ColouredText($"[Warning] ", ConsoleColor.Yellow),
                new ColouredText($"({DateTime.Now.ToShortTimeString()}) ", ConsoleColor.DarkGray),
                new ColouredText($"{moduleName}", ConsoleColor.Magenta),
                new ColouredText($": {text}\n", ConsoleColor.Yellow)
            ]);
        }

        public static void Error(string moduleName, string text)
        {
            ColourWrite([
                new ColouredText($"[Error] ", ConsoleColor.Red),
                new ColouredText($"({DateTime.Now.ToShortTimeString()}) ", ConsoleColor.Gray),
                new ColouredText($"{moduleName}", ConsoleColor.Magenta),
                new ColouredText($": {text}\n", ConsoleColor.Red)
            ]);
        }

        public static void ColourWrite(ColouredText[] text)
        {
            foreach (var segment in text)
            {
                Console.ForegroundColor = segment.Color;
                Console.Write(segment.Text);
            }
            if (Console.ForegroundColor != ConsoleColor.White)
                Console.ForegroundColor = ConsoleColor.White;
        }

        public class ColouredText(string text, ConsoleColor color)
        {
            public string Text { get; set; } = text;
            public ConsoleColor Color { get; set; } = color;
        }
    }
}
