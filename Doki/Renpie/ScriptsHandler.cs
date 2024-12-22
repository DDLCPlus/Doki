using Doki.Extensions;
using Doki.Renpie.Parser;
using Doki.Renpie.RenDisco;
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

        public static bool ProcessFromFile(string pathToScript)
        {
            Script retScript = new Script();

            bool outCome = retScript.Process(Parser, pathToScript);

            if (outCome)
            {
                LoadedScripts.Add(retScript);
                return true;
            }

            return false;
        }
    }
}
