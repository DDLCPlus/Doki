using Doki.Extensions;
using Doki.Mods;
using Doki.RenpyUtils;
using HarmonyLib;
using RenpyLauncher;
using RenpyParser;
using RenPyParser;
using RenPyParser.AssetManagement;
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
using System.Text;
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
                //HarmonyInstance.Patch(typeof(Blocks).GetMethod("OnAfterDeserialize"), new HarmonyMethod(typeof(PatchUtils).GetMethod("ScriptExecutionPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyScript).GetMethod("ResolveLabel"), new HarmonyMethod(typeof(PatchUtils).GetMethod("ResolveLabelPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyScript).GetMethod("TryResolveLabel"), new HarmonyMethod(typeof(PatchUtils).GetMethod("TryResolveLabelPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                //HarmonyInstance.Patch(typeof(Lines).GetMethod("GetValue"), new HarmonyMethod(typeof(PatchUtils).GetMethod("GetValuePatch", BindingFlags.Static | BindingFlags.NonPublic)));

                HarmonyInstance.Patch(typeof(UnityEngine.Logger).GetMethod("Log", new[] { typeof(LogType), typeof(object) }), new HarmonyMethod(typeof(PatchUtils).GetMethod("DebugPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                foreach (var mod in DokiModsManager.Mods)
                {
                    if (mod.Prefixes != null && mod.Prefixes.Count() > 0)
                    {
                        foreach (var prefix in mod.Prefixes)
                        {
                            ConsoleUtils.Log($"[MOD PATCH PREFIXES -> {mod.Name}]: Patching {prefix.Key.Name}...");

                            HarmonyInstance.Patch(prefix.Key, prefix: prefix.Value);

                            ConsoleUtils.Log($"[MOD PATCH PREFIXES -> {mod.Name}]: Patched {prefix.Key.Name}!");
                        }
                    }

                    if (mod.Postfixes != null && mod.Postfixes.Count() > 0)
                    {
                        foreach (var postfix in mod.Postfixes)
                        {
                            ConsoleUtils.Log($"[MOD PATCH POSTFIXES -> {mod.Name}]: Patching {postfix.Key.Name}...");

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
            //Console.WriteLine(logType.ToString() + ": " + message);

            //return false;
            return true;
        }

        private static bool ResolveLabelPatch(ref RenpyScript __instance, ref ValueTuple<RenpyBlock, int> __result, ref string label)
        {
            //Console.WriteLine($"Resolved normal label -> {label}");

            return true;
        }

        private static bool TryResolveLabelPatch(ref RenpyScript __instance, ref string label)
        {
            //Console.WriteLine($"Resolved normal label -> {label}");

            return true;
        }

        private static bool SayPatch(ref string __0, ref string __1, ref DialogueLine __2)
        {
            if (DokiModsManager.ActiveScriptModifierIndex == -1)
                return true;

            var line = RenpyUtils.RenpyUtils.RetrieveLineFromText(__2.TextID);

            if (line != null)
            {
                __2.Text = line.Text;

                if (line.FromPlayer)
                    __0 = Renpy.CurrentContext.GetVariableString("player");
                else
                    __0 = line.Character;
            }

            return true;
        }
    }
}
