using Doki.Extensions;
using Doki.Mods;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace Doki.RenpyUtils
{
    public class RenpyScriptExecutionContext
    {
        private bool ModifiedContext = false;

        public void HandleContextChange(RenpyExecutionContext executionContext)
        {
            if (ModifiedContext)
                return;

            DokiMod LoadedMod = DokiModsManager.Mods[DokiModsManager.ActiveScriptModifierIndex];

            if (LoadedMod == null)
                return;

            var script = executionContext.script;
            var blocks = script.Blocks;

            Dictionary<string, RenpyBlock> rawBlocks = new Dictionary<string, RenpyBlock>();
            Dictionary<string, BlockEntryPoint> rawBlockEntryPoints = new Dictionary<string, BlockEntryPoint>();

            var jumpLabel = RenpyScriptProcessor.ModBlocks.First();

            Console.WriteLine(jumpLabel.Item2.Label);

            foreach (var entry in RenpyScriptProcessor.ModBlocks)
            {
                Tuple<BlockEntryPoint, RenpyBlock> tuple = entry;

                Console.WriteLine($"Processing blocks in blockCache");

                BlockEntryPoint blockEntryPoint = tuple.Item1;
                RenpyBlock renpyBlock = tuple.Item2;

                rawBlocks.Add(renpyBlock.Label, renpyBlock);
                rawBlockEntryPoints.Add(renpyBlock.Label, blockEntryPoint);

                Console.WriteLine($"Processed block pair in blockCache");
            }

            Console.WriteLine($"Processed blocks in blockCache");

            blocks.SetPrivateField("blocks", rawBlocks);
            blocks.SetPrivateField("blockEntryPoints", rawBlockEntryPoints);

            ModifiedContext = true;

            Console.WriteLine("Blocks modification complete.");
        }
    }
}
