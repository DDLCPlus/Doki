using Doki.Mods;
using Doki.Utils;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Interpreter
{
    public class RenpyScriptExecutionContext
    {
        public void HandleContextChange(RenpyScriptExecution instance, RenpyExecutionContext executionContext)
        {
            DokiMod LoadedMod = DokiModsManager.Mods[DokiModsManager.ActiveScriptModifierIndex];

            if (LoadedMod == null)
                return;

            foreach(var scripts in LoadedMod.Scripts)
            {
                var blocks = scripts.ToBlocks();

                foreach (Tuple<BlockEntryPoint, RenpyBlock> blockGroup in blocks)
                {
                    BlockEntryPoint blockEntryPoint = blockGroup.Item1;
                    RenpyBlock block = blockGroup.Item2;

                    RegisterBlock(blockEntryPoint, block, executionContext);
                }
            }
        }

        //Courtesy of Kizby, adapted by DDLCPlus Modding Group
        public void RegisterBlock(BlockEntryPoint blockEntryPoint, RenpyBlock block, RenpyExecutionContext Context)
        {
            if (Context == null || block == null)
                return;

            var script = Context.script;
            var blocks = script.Blocks;

            Dictionary<string, RenpyBlock> rawBlocks = (Dictionary<string, RenpyBlock>)blocks.GetPrivateField("blocks");
            Dictionary<string, BlockEntryPoint> rawBlockEntryPoints = (Dictionary<string, BlockEntryPoint>)blocks.GetPrivateField("blockEntryPoints");

            //If another block by the same label already exists in the current script, remove it
            if (rawBlocks.ContainsKey(block.Label) || rawBlockEntryPoints.ContainsKey(block.Label))
            {
                rawBlocks.Remove(block.Label);
                rawBlockEntryPoints.Remove(block.Label);
            }

            rawBlocks.Add(block.Label, block);
            rawBlockEntryPoints.Add(block.Label, blockEntryPoint);

            //We don't need to do jumping and sound yet since we haven't implemented menu translating.
        }
    }
}
