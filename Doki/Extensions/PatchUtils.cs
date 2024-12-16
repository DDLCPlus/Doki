using Doki.Extensions;
using Doki.Mods;
using Doki.RenpyUtils;
using HarmonyLib;
using MonoMod.Utils;
using RenpyLauncher;
using RenpyParser;
using RenPyParser;
using RenPyParser.AssetManagement;
using RenPyParser.Images;
using RenPyParser.Screens.Invert;
using RenPyParser.Screens.Tear;
using RenPyParser.Transforms;
using RenPyParser.VGPrompter.DataHolders;
using RenPyParser.VGPrompter.Script.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using UnityPS;

namespace Doki.Utils
{
    public static class PatchUtils
    {
        private static Harmony HarmonyInstance { get; set; }

        public static bool Patched = false;

        static PatchUtils()
        {
            HarmonyInstance = new Harmony("Doki");
        }

        public static void ApplyPatches()
        {
			try
            {
                HarmonyInstance.Patch(typeof(Blocks).GetMethod("OnAfterDeserialize"), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ScriptExecutionPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyScript).GetMethod("ResolveLabel"), new HarmonyMethod(typeof(PatchUtils).GetMethod("ResolveLabelPatch", BindingFlags.Static | BindingFlags.NonPublic)), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ResolveLabelPatchAfter", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyScript).GetMethod("TryResolveLabel"), new HarmonyMethod(typeof(PatchUtils).GetMethod("TryResolveLabelPatch", BindingFlags.Static | BindingFlags.NonPublic)), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("TriedResolveLabelPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(UnityEngine.Logger).GetMethod("Log", new[] { typeof(LogType), typeof(object) }), new HarmonyMethod(typeof(PatchUtils).GetMethod("DebugPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(AssetBundle).GetMethod("LoadAsset", new[] { typeof(string), typeof(Type) }), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("LoadPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(ActiveAssetBundles).GetMethod("LoadPermanentBundles"), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("LoadPermanentBundlesPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyLoadImage).GetMethod("Immediate", BindingFlags.Static | BindingFlags.Public), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ImmediatePatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyCallstackEntry).GetMethod("Next"), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("NextPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                foreach (var mod in DokiModsManager.Mods)
                {
                    if (mod.Prefixes != null && mod.Prefixes.Count() > 0)
                    {
                        foreach (var prefix in mod.Prefixes)
                        {
                            HarmonyInstance.Patch(prefix.Key, prefix: prefix.Value);
                            ConsoleUtils.Log($"[MOD PATCH PREFIXES -> {mod.Name}]: Patched {prefix.Key.Name}!");
                        }
                    }

                    if (mod.Postfixes != null && mod.Postfixes.Count() > 0)
                    {
                        foreach (var postfix in mod.Postfixes)
                        {
                            HarmonyInstance.Patch(postfix.Key, postfix: postfix.Value);
                            ConsoleUtils.Log($"[MOD PATCH POSTFIXES -> {mod.Name}]: Patched {postfix.Key.Name}!");
                        }
                    }
                }

                HarmonyInstance.Patch(typeof(RenpyWindowManager).GetMethod("Say"), new HarmonyMethod(typeof(PatchUtils).GetMethod("SayPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                ConsoleUtils.Log("[BASE PATCH PREFIXES] Patched");
            }
            catch(Exception e)
            {
                ConsoleUtils.Log($"Failed to patch: {e.ToString()}");
            }
            finally
            {
                ConsoleUtils.Log($"All Patches (base & mods) applied.");
                Patched = true;
            }
        }

        private static void NextPatch(RenpyCallstackEntry __instance, Line __result)
        {
            int index = (int)__instance.GetPrivateField("lastLine");

            if (RenpyUtils.RenpyUtils.CustomDefinitions.TryGetValue(new Tuple<int, string>(index, __instance.blockName), out RenpyDefinition definition))
            {
                Renpy.CurrentContext.SetVariableString(definition.Name, definition.Value.ToString());

                ConsoleUtils.Log($"Ran next patch -> Handled Renpy Variable Assignment to " + definition.Name);
            }
        }

        private static bool ValidateLoadPatch(string path, out string bundleName, bool mustExist, bool __result)
        {
            Tuple<string, Tuple<string, bool>> outCome = AssetUtils.GetBundleDetailsByAssetKey(path);

            if (outCome == default)
            {
                bundleName = "";

                return true;
            }

            bundleName = outCome.Item1;

            Console.WriteLine("Validating load for " + path + " - which compared was: " + outCome.Item1);

            __result = true;
            return false;
        }

        private static void LoadPermanentBundlesPatch(ref ActiveAssetBundles __instance)
        {
            Dictionary<string, AssetBundle> m_ActiveAssetBundles = (Dictionary<string, AssetBundle>)__instance.GetPrivateField("m_ActiveAssetBundles");

            foreach (var kvp in m_ActiveAssetBundles)
            {
                if (!AssetUtils.AssetBundles.ContainsKey(kvp.Key))
                {
                    AssetUtils.AssetBundles[kvp.Key] = kvp.Value;
                }
            }

            foreach (var bundle in AssetUtils.AssetBundles)
            {
                var bundleVar = bundle.Value;

                string key = bundle.Key;

                foreach (var asset in bundleVar.GetAllAssetNames())
                {
                    string assetKey = Path.GetFileNameWithoutExtension(asset);

                    AssetUtils.AssetsToBundles[assetKey] = new Tuple<string, Tuple<string, bool>>(key, new Tuple<string, bool>(asset, true));

                    // //assetKey -> bundleName -> assetFullPathInBundle
                }
            }

            ConsoleUtils.Log($"AssetsToBundles contains {AssetUtils.AssetsToBundles.Count} items.");
        }

        private static bool ImmediatePatch(RenpyLoadImage __instance, GameObject gameObject, CurrentTransform currentTransform, string key)
        {
            gameObject.DestroyChildIfOnlyChild();

            GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(AssetUtils.FixLoad(key), gameObject.transform, false);

            if (key.Contains("poem_special") || key.Contains("poem_end"))
            {
                string languageKey = key.Replace("original", Renpy.GetCurrentLanguagePrefix());

                if (AssetUtils.alternateImageNames.ContainsKey(languageKey))
                {
                    languageKey = AssetUtils.alternateImageNames[languageKey];
                }

                typeof(RenpyLoadImage).GetMethod("ReplaceSprite", BindingFlags.NonPublic | BindingFlags.Static).Invoke(__instance, new object[] { gameObject2, languageKey });
            }

            if (key.Contains("bsod"))
            {
                string languageKey2 = "bsod_" + Renpy.GetCurrentLanguagePrefix();

                typeof(RenpyLoadImage).GetMethod("ReplaceSprite", BindingFlags.NonPublic | BindingFlags.Static).Invoke(__instance, new object[] { gameObject2, languageKey2 });
            }

            gameObject2.name = key;

            ApplyTransformData.Apply(gameObject, currentTransform, false);

            return false;
        }

        private static bool LoadPatch(AssetBundle __instance, string name, Type type, ref object __result)
        {
            if (!AssetUtils.AssetBundles.ContainsKey(__instance.name))
                return true;
            //if (AssetUtils.GetBundleDetailsByAssetKey(Path.GetFileNameWithoutExtension(name)) == default)
            //    return true;

            if (name != "select" && name != "hover" && name != "frame")
                Console.WriteLine($"Trying to force load -> {name} as type: {type.ToString()}");

            //Yes, I know I have an instance to the AssetBundle but shit will fucking crash if I use it.

            AssetBundle realInstance = AssetUtils.AssetBundles[__instance.name];

            __result = realInstance.ForceLoadAsset(name, type);

            return false;
        }

        private static void ScriptExecutionPatch(ref Blocks __instance)
        {
            var __blocks = (Dictionary<string, RenpyBlock>)__instance.GetPrivateField("blocks");
            var __blockEntryPoints = (Dictionary<string, BlockEntryPoint>)__instance.GetPrivateField("blockEntryPoints");

            foreach (var block in RenpyScriptProcessor.BlocksDict)
            {
                string labelShit = block.Key;

                Tuple<BlockEntryPoint, RenpyBlock> blockShit = block.Value;

                if (__blocks.TryGetValue(labelShit, out RenpyBlock _))
                {
                    __blocks.Remove(labelShit);
                    __blockEntryPoints.Remove(labelShit);
                }

                __blockEntryPoints.Add(labelShit, blockShit.Item1);
                __blocks.Add(labelShit, blockShit.Item2);

                ConsoleUtils.Log($"Block processed -> {block.Key}");
            }

            //__blockEntryPoints.Add("bg_test_original", new BlockEntryPoint("bg_test_original"));
            //__blockEntryPoints.Add("bg bg_test2", new BlockEntryPoint("bg bg_test2"));

            //__blocks.Add("bg bg_test2", new RenpyBlock("bg bg_test2")
            //{
            //    callParameters = new RenpyCallParameter[0],
            //    Contents = new List<Line>()
            //    {
            //        new RenpyGoTo("bg_test_original", true, "call bg_test_original"),
            //        new RenpySize("size(1280,720)", null, null, true, true)
            //    }
            //});

            //__blocks.Add("bg bg_test2", new RenpyBlock("bg bg_test2")
            //{
            //    callParameters = new RenpyCallParameter[0],
            //    Contents = new List<Line>()
            //    {
            //        new RenpyLoadImage("bg_test_original", "bg/bg_test2.png"),
            //        new RenpySize("size(1280,720)", null, null, true, true)
            //    }
            //});

            //__blocks.Add("bg_test_original", new RenpyBlock("bg_test_original")
            //{
            //    callParameters = new RenpyCallParameter[0],
            //    Contents = new List<Line>()
            //    {
            //        new RenpyLoadImage("bg_test_original", "bg/bg_test_original.prefab")
            //    }
            //});

            foreach (var block in __blocks)
            {
                if (block.Key == "start")
                {
                    block.Value.Contents.Clear();
                    block.Value.Contents.AddRange(new List<Line>()
                    {
                        new RenpyGoTo(RenpyScriptProcessor.BlocksDict.First().Key, false, $"jump {RenpyScriptProcessor.BlocksDict.First().Key}")
                    });
                }
            }

            __instance.SetPrivateField("blocks", __blocks);
            __instance.SetPrivateField("blockEntryPoints", __blockEntryPoints);

            ConsoleUtils.Log("Blocks deserialization overriden!");
        }

        private static bool DebugPatch(ref LogType logType, ref object message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(logType.ToString() + ": " + message);
            Console.ForegroundColor = ConsoleColor.White;

            return false;
            //return true;
        }

        private static bool ResolveLabelPatch(ref RenpyScript __instance, ref ValueTuple<RenpyBlock, int> __result, ref string label)
        {
            //Console.WriteLine($"Resolving normal label -> {label}");

            return true;
        }

        private static void ResolveLabelPatchAfter(ref RenpyScript __instance, ref ValueTuple<RenpyBlock, int> __result, ref string label)
        {
            if (__result.Item1.Label.Contains("bg"))
            {
                RenpyUtils.RenpyUtils.DumpBlock(__result.Item1);

                Console.WriteLine($"Resolved normal label -> {label}");
            }
        }

        private static bool TryResolveLabelPatch(ref RenpyScript __instance, ref string label)
        {
            //Console.WriteLine($"Trying to resolve normal label -> {label}");

            return true;
        }

        private static void TriedResolveLabelPatch(ref RenpyScript __instance, ref string label, ref ValueTuple<RenpyBlock, int> tuple)
        {
            Console.WriteLine($"Tried to resolve normal label -> {label}");

            if (label.Contains("bg"))
                RenpyUtils.RenpyUtils.DumpBlock(tuple.Item1);
        }

        private static bool SayPatch(string tag, string character, DialogueLine dialogueLine)
        {
            if (DokiModsManager.ActiveScriptModifierIndex == -1)
                return true;

            var line = RenpyUtils.RenpyUtils.RetrieveLineFromText(dialogueLine.TextID);

            if (line != null)
            {
                dialogueLine.Text = line.Text;

                //if (line.FromPlayer)
                //    character = Renpy.CurrentContext.GetVariableString("player");
                //else
                //    character = line.Character;
            }

            return true;
        }
    }
}
