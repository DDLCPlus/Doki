using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Doki.Utils;
using Doki.Mods;
using RenDisco;
using RenPyParser.VGPrompter.DataHolders;
using RenpyParser;
using SimpleExpressionEngine;
using Doki.Interpreter;

namespace Doki.Game
{
    public class DDLCScript
    {
        public string Path { get; set; } //Script Path!

        public DokiMod ModContext { get; set; }

        private List<RenpyCommand> Commands { get; set; }

        private List<Tuple<BlockEntryPoint, RenpyBlock>> BlocksCache { get; set; }

        public List<Tuple<BlockEntryPoint, RenpyBlock>> ToBlocks()
        {
            if (BlocksCache == null)
            {
                int[] beginningIndexes = Commands.Where(x => x.Type == "label").Select(x => Commands.IndexOf(x)).ToArray();

                if (beginningIndexes.Length == 0)
                    throw new Exception("Failed to convert DDLCScript to blocks -> RenDisco reported no labels for this script");

                List<Tuple<BlockEntryPoint, RenpyBlock>> Blocks = new List<Tuple<BlockEntryPoint, RenpyBlock>>();

                for (int i = 0; i < beginningIndexes.Length; i++)
                {
                    int startIndex = beginningIndexes[i];

                    string label = ((Label)Commands[startIndex]).Name;

                    int endIndex = (i + 1 < beginningIndexes.Length) ? beginningIndexes[i + 1] : Commands.Count;

                    List<RenpyCommand> blockCommands = Commands.GetRange(startIndex, endIndex - startIndex);

                    BlockEntryPoint entryPoint = new BlockEntryPoint(label);

                    RenpyBlock block = RenpyUtils.Translate(label, blockCommands);

                    Blocks.Add(new Tuple<BlockEntryPoint, RenpyBlock>(entryPoint, block));
                }

                BlocksCache = Blocks;

                return Blocks;
            }
            else return BlocksCache;
        }

        public DDLCScript(DokiMod ModContext, string Path)
        {
            this.ModContext = ModContext;
            this.Path = ModContext.WorkingDirectory + "\\" + ModContext.ScriptsDirectory + "\\" + Path;

            if (File.Exists(this.Path))
                Commands = RenpyUtils.Parser.ParseFromFile(Path);
        }
    }

}
