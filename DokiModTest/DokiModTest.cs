using Doki.Game;
using Doki.Mods;
using Doki.Utils;
using HarmonyLib;
using RenpyLauncher;
using RenpyParser;
using RenPyParser.AssetManagement;
using RenPyParser.Images;
using SimpleExpressionEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityPS;

namespace DokiModTest
{
    public class DokiModTest : DokiMod
    {
        public override string ID => "DokiMod2";

        public override string Name => "Doki Mod Test";

        public override string Author => "Uh";

        public override string Version => "1.0";

        public override string WorkingDirectory => "DokiModTest";

        public override string ScriptsDirectory => "Scripts";

        public override string CustomAssetsDirectory => "Assets";


        public override Dictionary<MethodBase, HarmonyMethod> Prefixes => new Dictionary<MethodBase, HarmonyMethod>()
        {
            {
                typeof(RenpyMainBase).GetMethod("WantsToQuit", BindingFlags.Instance | BindingFlags.NonPublic),
                new HarmonyLib.HarmonyMethod(typeof(DokiModTest).GetMethod("QuitPatch", BindingFlags.Static | BindingFlags.NonPublic))
            },
            {
                typeof(LauncherMain).GetMethod("WantsToQuit", BindingFlags.Instance | BindingFlags.NonPublic),
                new HarmonyLib.HarmonyMethod(typeof(DokiModTest).GetMethod("QuitPatch", BindingFlags.Static | BindingFlags.NonPublic))
            },
            {
                typeof(LauncherMain).GetMethod("SwitchToApp"),
                new HarmonyLib.HarmonyMethod(typeof(DokiModTest).GetMethod("SkipToLoginPatch", BindingFlags.Static | BindingFlags.NonPublic))
            }
        };

        public override List<DDLCScript> Scripts
        {
            get
            {
                List<DDLCScript> allScripts = new List<DDLCScript>
                {
                    new DDLCScript(this, "script.rpy", "dokimodtest_shennanigans")
                };

                return allScripts;
            }
        }

        private static bool QuitPatch(ref bool __result)
        {
            __result = true;
            return false;
        }

        private static bool SkipToLoginPatch(ref LauncherAppId __0)
        {
            if (__0 == LauncherAppId.Bios)
                __0 = LauncherAppId.Login; //Login

            return true;
        }
    }
}
