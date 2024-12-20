using Doki.Extensions;
using RenDisco;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Doki.RenpyUtils
{
    public static class RenpyScriptProcessor
    {
        public static Dictionary<string, Tuple<BlockEntryPoint, RenpyBlock>> BlocksDict { get; set; } //Block label -> Commands translated
        public static string JumpTolabel { get; set; }
        public static RenDisco.RenpyParser Parser { get; set; }

        static RenpyScriptProcessor()
        {
            BlocksDict = [];
            Parser = new RenDisco.RenpyParser();
        }

        public static void ProcessScriptFromFile(string pathToScript)
        {
            List<RenpyCommand> Commands = Parser.ParseFromFile(pathToScript);

            if (Commands.Count() == 0)
                return;

            int[] beginningIndexes = Commands.Where(x => x.Type == "label").Select(x => Commands.IndexOf(x)).ToArray();
            if (beginningIndexes.Length == 0)
            {
                ConsoleUtils.Error("RenpyUtils", "Failed to convert DDLCScript to blocks -> RenDisco reported no labels for this script");
                throw new Exception("Failed to convert DDLCScript to blocks -> RenDisco reported no labels for this script");
            }

            for (int i = 0; i < beginningIndexes.Length; i++)
            {
                int startIndex = beginningIndexes[i];
                int endIndex = i + 1 < beginningIndexes.Length ? beginningIndexes[i + 1] : Commands.Count;
                string label = ((Label)Commands[startIndex]).Name;

                List<RenpyCommand> blockCommands = Commands.GetRange(startIndex, endIndex - startIndex);
                BlockEntryPoint entryPoint = new(label);

                RenpyBlock block = RenpyUtils.Translate(label, blockCommands);
                var container = block.Contents;

                foreach (var entry in RenpyUtils.Jumps)
                {
                    switch (entry.Key)
                    {
                        case RenpyGoToLine goToLine:
                            goToLine.TargetLine = container.IndexOf(RenpyUtils.Jumps[goToLine]);
                            break;
                        case RenpyGoToLineUnless goToLineUnless:
                            goToLineUnless.TargetLine = container.IndexOf(RenpyUtils.Jumps[goToLineUnless]);
                            break;
                        case RenpyMenuInputEntry menuInputEntry:
                            menuInputEntry.gotoLineTarget = container.IndexOf(entry.Value);
                            break;
                    }
                }

                //Credits to Kizby for this jumps map implementation, I was overthinking if & elif and whatnot statements.
                RenpyUtils.Jumps.Clear();
                BlocksDict.Add(label, new Tuple<BlockEntryPoint, RenpyBlock>(entryPoint, block));
            }
        }
    }
}
