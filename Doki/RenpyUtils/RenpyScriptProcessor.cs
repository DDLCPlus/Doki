using RenDisco;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.RenpyUtils
{
    public static class RenpyScriptProcessor
    {
        public static List<Tuple<BlockEntryPoint, RenpyBlock>> ModBlocks { get; set; }

        public static Dictionary<string, Tuple<BlockEntryPoint, RenpyBlock>> BlocksDict { get; set; } //Block label -> Commands translated

        public static RenDisco.RenpyParser Parser { get; set; }

        static RenpyScriptProcessor()
        {
            ModBlocks = new List<Tuple<BlockEntryPoint, RenpyBlock>>();
            BlocksDict = new Dictionary<string, Tuple<BlockEntryPoint, RenpyBlock>>();
            Parser = new RenDisco.RenpyParser();
        }

        public static Tuple<BlockEntryPoint, RenpyBlock> ProcessScriptFromFile(string pathToScript)
        {
            string labelExecutionOverride = Path.GetFileNameWithoutExtension(pathToScript);

            Tuple<BlockEntryPoint, RenpyBlock> BlocksFound = ModBlocks.FirstOrDefault(x => x.Item2.Label == labelExecutionOverride);

            if (BlocksFound != null || BlocksFound != default)
                return BlocksFound;

            List<RenpyCommand> Commands = Parser.ParseFromFile(pathToScript);

            if (Commands.Count() == 0)
                return null;

            int[] beginningIndexes = Commands.Where(x => x.Type == "label").Select(x => Commands.IndexOf(x)).ToArray();

            if (beginningIndexes.Length == 0)
                throw new Exception("Failed to convert DDLCScript to blocks -> RenDisco reported no labels for this script");

            List<Tuple<BlockEntryPoint, RenpyBlock>> Blocks = new List<Tuple<BlockEntryPoint, RenpyBlock>>();

            for (int i = 0; i < beginningIndexes.Length; i++)
            {
                int startIndex = beginningIndexes[i];

                string label = ((Label)Commands[startIndex]).Name;

                int endIndex = i + 1 < beginningIndexes.Length ? beginningIndexes[i + 1] : Commands.Count;

                List<RenpyCommand> blockCommands = Commands.GetRange(startIndex, endIndex - startIndex);

                BlockEntryPoint entryPoint = new BlockEntryPoint(label);

                RenpyBlock block = RenpyUtils.Translate(label, blockCommands);

                BlocksDict.Add(label, new Tuple<BlockEntryPoint, RenpyBlock>(entryPoint, block)); //will be used by resolve label

                BlocksFound = new Tuple<BlockEntryPoint, RenpyBlock>(entryPoint, block);
            }

            return BlocksFound;
        }
    }
}
