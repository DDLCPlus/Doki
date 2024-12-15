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
        public static Dictionary<string, Tuple<BlockEntryPoint, RenpyBlock>> BlocksDict { get; set; } //Block label -> Commands translated

        public static RenDisco.RenpyParser Parser { get; set; }

        static RenpyScriptProcessor()
        {
            BlocksDict = new Dictionary<string, Tuple<BlockEntryPoint, RenpyBlock>>();
            Parser = new RenDisco.RenpyParser();
        }

        public static Tuple<BlockEntryPoint, RenpyBlock> ProcessScriptFromFile(string pathToScript)
        {
            Tuple<BlockEntryPoint, RenpyBlock> retTuple = default;

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
                int endIndex = i + 1 < beginningIndexes.Length ? beginningIndexes[i + 1] : Commands.Count;

                string label = ((Label)Commands[startIndex]).Name;

                List<RenpyCommand> blockCommands = Commands.GetRange(startIndex, endIndex - startIndex);

                BlockEntryPoint entryPoint = new BlockEntryPoint(label);

                RenpyBlock block = RenpyUtils.Translate(label, blockCommands);

                retTuple = new Tuple<BlockEntryPoint, RenpyBlock>(entryPoint, block);

                BlocksDict.Add(label, retTuple); 
            }

            return retTuple;
        }
    }
}
