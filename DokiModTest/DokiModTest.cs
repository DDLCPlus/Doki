using Doki.Extensions;
using Doki.Mods;
using Doki.UI;
using HarmonyLib;
using RenpyLauncher;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DokiModTest
{
    public class DokiModTest : DokiMod
    {
        public override string ID => "DokiModTest";
        public override string Name => "Doki Mod Test";
        public override string Author => "Doki Dev Team";
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

        private bool fix_menu_stuff()
        {
            RenpyMainMenuUI.playMainMenuTheme = true;
            RenpyMainMenuUI.refreshMainMenu = true;

            //Console.WriteLine("Trying to fix menu stuff with the help of inline \"python function\"!");

            return true;
        }

        public override void OnUnload()
        {
            //Console.WriteLine("I'm unloading :D");
        }

        public override void OnLoad()
        {
            //Console.WriteLine("I'm loading :D");
            UiHandler.OverrideWallpaper("normal_fucking_background.jpg", false);
            //UiHandler.AddEvent("start_menu_open", new Action(() =>
            //{
            //    UiHandler.CreateStartMenuButton("Test", new Action(() =>
            //    {
            //        Console.WriteLine("hey mom im a button!");
            //    }), false, default, default, false, "button_icon_test.png");
            //}));
        }

        private void do_custom_dialogue_box()
        {
            GameObject coroutineObject = new GameObject();

            coroutineObject.AddComponent<CustomDialogueBox>();

            UnityEngine.Object.DontDestroyOnLoad(coroutineObject);
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
