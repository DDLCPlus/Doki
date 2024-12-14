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
    /*
     RENDISCO MIT LICENSE:

     Copyright (c) 2024 aaartrtrt

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
    */

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
