using Doki.Extensions;
using Doki.Renpie.Parser;
using Doki.Renpie.RenDisco;
using Doki.Renpie.Rpyc;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Doki.Renpie
{
    public static class ScriptsHandler
    {
        public static List<Script> LoadedScripts { get; set; }
        public static string JumpTolabel { get; set; }
        public static Renpie.RenDisco.RenpyParser Parser { get; set; }

        static ScriptsHandler()
        {
            LoadedScripts = new List<Script>();
            Parser = new RenDisco.RenpyParser();
        }

        public static bool ProcessFromFile(bool IsRpyc, string pathToScript)
        {
            if (IsRpyc)
                return false; //to-do

            Script retScript = new Script();

            bool outCome = retScript.Process(Parser, pathToScript);

            if (outCome)
            {
                LoadedScripts.Add(retScript);
                return true;
            }

            return false;
        }

        public static bool ProcessFromBytes(bool IsRpyc, byte[] Contents)
        {
            if (IsRpyc)
            {
                RpycFile rpyc = new RpycFile(Contents);

                if (!rpyc.Valid)
                    return false;

                ConsoleUtils.Log($"Rpyc processing...", $"Valid! {Contents.Length}");

                Script rpycScript = new Script();

                if (rpyc.Inits.Count > 0)
                {
                    foreach(var init in rpyc.Inits)
                    {
                        RenpyBlock block = new RenpyBlock()
                        {
                            Contents = init.Contents
                        };

                        rpycScript.InitBlocks.Add(block);
                    }
                }

                if (rpyc.Py.Count > 0)
                {
                    foreach(var py in rpyc.Py)
                        rpycScript.EarlyPython.Add(py);
                }
 
                foreach (var block in rpyc.Labels)
                    rpycScript.BlocksDict.Add(block.Value.Label, new Tuple<BlockEntryPoint, RenpyBlock>(block.Key, block.Value));

                ConsoleUtils.Log("Labels", $"Labels count: {rpyc.Labels.Count}");
                LoadedScripts.Add(rpycScript);

                return true;
            }

            Script retScript = new Script();

            bool outCome = retScript.Process(Parser, Contents);

            if (outCome)
            {
                LoadedScripts.Add(retScript);
                return true;
            }

            return false;
        }
    }
}
