using Doki.Game;
using Doki.Interpreter;
using Doki.Utils;
using HarmonyLib;
using RenDisco;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Doki.Mods
{
    public class DokiMod
    {
        public virtual string ID => "DokiMod";

        public virtual string Name => "A Doki Mod";

        public virtual string Author => "A Doki Mod Developer";

        public virtual string Version => "1.0";

        public List<AssetBundle> ModBundles = new List<AssetBundle>();

        public virtual List<DDLCScript> Scripts { get; set; }

        public Dictionary<int, Tuple<string, string>> IdentifiersToRelativePaths = new Dictionary<int, Tuple<string, string>>();

        public virtual string WorkingDirectory => "DokiModTest";

        public virtual string ScriptsDirectory => "Scripts";

        public virtual string CustomAssetsDirectory => "Assets";

        public virtual Dictionary<MethodBase, HarmonyMethod> Prefixes { get; set; }

        public virtual Dictionary<MethodBase, HarmonyMethod> Postfixes { get; set; }

        public virtual void OnLoad()
        {

        }

        public virtual void OnNextLine()
        {

        }
    }
}
