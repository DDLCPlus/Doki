using Doki.Mods;
using HarmonyLib;
using RenpyLauncher;
using System.Collections.Generic;
using System.Reflection;

namespace DokiModTest
{
    public class DokiModTest : DokiMod
    {
        public override string ID => "DokiMod2";
        public override string Name => "Doki Mod Test";
        public override string Author => "Uh";
        public override string Version => "1.0";
        public override string LabelEntryPoint => "startmod";

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
