using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Doki.Mods
{
    public class DokiMod
    {
        public virtual string ID => "DokiMod";
        public virtual string Name => "A Doki Mod";
        public virtual string Author => "A Doki Mod Developer";
        public virtual string Version => "1.0";
        public virtual string LabelEntryPoint => "startmod";

        public List<AssetBundle> ModBundles = [];

        public virtual Dictionary<MethodBase, HarmonyMethod> Prefixes { get; set; }
        public virtual Dictionary<MethodBase, HarmonyMethod> Postfixes { get; set; }

        public string ScriptsPath { get; set; }
        public string AssetsPath { get; set; }

        public bool ModifiesContext { get; set; }

        public virtual void OnLoad() { }
        public virtual void OnNextLine() { }
    }
}
