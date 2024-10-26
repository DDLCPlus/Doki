using Doki.Game;
using Doki.Interpreter;
using Doki.Mods;
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
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace Doki.Utils
{
    public static class PatchUtils
    {
        private static Harmony HarmonyInstance { get; set; }

        public static bool Patched = false;

        public static RenpyBlock blockStuff { get; set; }

        static PatchUtils()
        {
            HarmonyInstance = new Harmony("Doki");
        }

        public static void ApplyPatches()
        {
			try
            {
                HarmonyInstance.Patch(typeof(Blocks).GetMethod("OnAfterDeserialize"), new HarmonyMethod(typeof(PatchUtils).GetMethod("ScriptExecutionPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                HarmonyInstance.Patch(typeof(RenpyScriptExecution).GetMethod("PostLoad"), new HarmonyMethod(typeof(PatchUtils).GetMethod("PostLoadPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyScriptExecution).GetMethod("Run"), new HarmonyMethod(typeof(PatchUtils).GetMethod("RunPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                ConsoleUtils.Log("[BASE PATCH PREFIXES] Attempting to patch assets, BIOS, Dialogue, Quitting.");

                HarmonyInstance.Patch(typeof(RenpyCallstackEntry).GetMethod("Next", BindingFlags.Instance | BindingFlags.Public), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("NextContextPatch", BindingFlags.Static | BindingFlags.NonPublic))); //Script Context Handling

                HarmonyInstance.Patch(typeof(RenpyCallstackEntry).GetMethod("Rewind", BindingFlags.Instance | BindingFlags.Public), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("RewindContextPatch", BindingFlags.Static | BindingFlags.NonPublic))); //Script Context Handling

                HarmonyInstance.Patch(typeof(ActiveAssetBundles).GetMethod("LoadPermanentBundles"), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("LoadPermanentBundlesPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                HarmonyInstance.Patch(typeof(ActiveLabelAssetBundles).GetMethod("ChangeLabel", new Type[] { typeof(string) }), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ChangeLabelPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                HarmonyInstance.Patch(typeof(ActiveLabelAssetBundles).GetMethod("ChangeLabelSync", new Type[] { typeof(string) }), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ChangeLabelSyncPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                HarmonyInstance.Patch(typeof(ActiveLabelAssetBundles).GetMethod("ClearCurrentLabelBundle", BindingFlags.Instance | BindingFlags.NonPublic), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ClearCurrentLabelBundlePatch", BindingFlags.Static | BindingFlags.NonPublic)));

                HarmonyInstance.Patch(typeof(ActiveLabelAssetBundles).GetMethod("ValidateLoad", BindingFlags.Instance | BindingFlags.NonPublic), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ValidateLoadPatch", BindingFlags.Static | BindingFlags.NonPublic)));

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

        private static bool ResolveLabelPatch(ref RenpyScript __instance, ref ValueTuple<RenpyBlock, int> __result, ref string __0)
        {
            ConsoleUtils.Log("Resolving Label:  " + __0);

            string label = __0;

            if (Interpreter.Interpreter.AssetBlocks.FirstOrDefault(x => x.Item2.Label == label) == default)
                return true;

            var stuff = Interpreter.Interpreter.AssetBlocks.FirstOrDefault(x => x.Item2.Label == label);

            __result = new ValueTuple<RenpyBlock, int>(stuff.Item2, 0);

            return false;
        }

        private static bool PostLoadPatch(ref RenpyScriptExecution __instance, ref bool __0)
        {
            RenpyExecutionContext _executionContext = (RenpyExecutionContext)__instance.GetPrivateField("_executionContext");

            if (RenpyUtils.GetContext() == default || RenpyUtils.GetContext() == null)
                RenpyUtils.SetContext(_executionContext);

            return true;
        }

        private static bool RunPatch(ref RenpyScriptExecution __instance, ref string __0)
        {
            RenpyExecutionContext _executionContext = (RenpyExecutionContext)__instance.GetPrivateField("_executionContext");

            if (RenpyUtils.GetContext() == default || RenpyUtils.GetContext() == null)
                RenpyUtils.SetContext(_executionContext);

            ConsoleUtils.Log("Running Label -> " + __0);

            return true;
        }

        //Kizby, I have admired your work on DDLC+ since 2021. This method for custom assets just works, all credit to you. Rest in peace.
        private static bool LoadPermanentBundlesPatch(ref ActiveAssetBundles __instance)
		{
			if (DokiModsManager.Mods.Count() == 0)
				return true; //No mods, no need for anything.

			Dictionary<string, AssetBundle> m_ActiveAssetBundles = (Dictionary<string, AssetBundle>)__instance.GetPrivateField("m_ActiveAssetBundles");
            ActiveBundles m_ActiveBundles = (ActiveBundles)__instance.GetPrivateField("m_ActiveBundles");

            var PermanentBundles = new List<string>()
            {
                "gui",
                "bg",
                "cg",
                "monika",
                "yuri",
                "natsuki",
                "sayori",
                "bgm-coarse",
                "sfx-coarse",
                "bgm-coarse_00",
            };

            Dictionary<string, string> assetBundleNameToPath = new Dictionary<string, string>();
            Dictionary<string, string> ModIdsToBundleName = new Dictionary<string, string>();

            foreach (var bundleFile in Directory.GetFiles("Doki Doki Literature Club Plus_Data/StreamingAssets/AssetBundles/" + PathHelpers.GetPlatformForAssetBundles(Application.platform)))
            {
                if (bundleFile.EndsWith(".cy"))
                {
                    var filename = Path.GetFileNameWithoutExtension(bundleFile);

                    if (filename.StartsWith("label "))
                    {
                        PermanentBundles.Insert(0, filename);
                    }
                }
            }

            foreach(var mod in DokiModsManager.Mods)
            {
                if (Directory.GetFiles(mod.WorkingDirectory + "\\Assets").Length > 0)
                {
                    //load asset bundles for mods

                    foreach(var file in Directory.GetFiles(mod.WorkingDirectory + "\\Assets"))
                    {
                        if (file.EndsWith(".cy"))
                            continue; //add encrypted bundle support

                        PermanentBundles.Add(Path.GetFileNameWithoutExtension(file));
                        assetBundleNameToPath.Add(Path.GetFileNameWithoutExtension(file), file);
                        ModIdsToBundleName.Add(Path.GetFileNameWithoutExtension(file), mod.ID);
                    }
                }
            }


            var gestaltDependencies = ScriptableObject.CreateInstance<LabelAssetBundleDependencies>();
            var seenBundles = new HashSet<string>(PermanentBundles);

            for (int i = 0; i < PermanentBundles.Count; i++)
            {
                var bundle = PermanentBundles[i];

                if (assetBundleNameToPath.TryGetValue(bundle, out var path))
                {
                    //custom asset bundle logic
                    string mod = "";

                    ModIdsToBundleName.TryGetValue(bundle, out mod);

                    DokiMod dokiMod = DokiModsManager.Mods.FirstOrDefault(x => x.ID == mod);

                    if (dokiMod == null)
                        continue;

                    AssetBundle bundy = AssetUtils.LoadAssetBundle(path);

                    if (bundy == null)
                        continue;

                    dokiMod.ModBundles.Add(bundy);

                    m_ActiveAssetBundles.Add(bundle, bundy);
                    m_ActiveBundles.ForceAdd(bundle);

                    foreach(var assety in bundy.GetAllAssetNames())
                        gestaltDependencies.AddAsset(PathHelpers.SanitizePathToAddressableName(assety), bundle, assety);

                    Debug.Log($"Loaded Mod bundle: {bundle}");

                    continue;
                }

                if (!m_ActiveAssetBundles.ContainsKey(bundle))
                {
                    Debug.Log($"Loading bundle: {bundle}");

                    AccessTools.Method(typeof(ActiveAssetBundles), "LoadBundleSync").Invoke(__instance, new object[] { bundle });
                }

                var assetBundle = m_ActiveAssetBundles[bundle];

                if (assetBundle == null)
                {
                    Debug.Log("Failed to load bundle!");

                    continue;
                }

                m_ActiveBundles.ForceAdd(bundle);

                foreach (var asset in assetBundle.GetAllAssetNames())
                {
                    gestaltDependencies.AddAsset(PathHelpers.SanitizePathToAddressableName(asset), bundle, $"Definitely Correct Path/{bundle}/{asset}");
                    //So basically, the game will error if bundles with the same names are already loaded. I think that's why Kizby did this?
                }

                foreach (var dependencies in assetBundle.LoadAllAssets<LabelAssetBundleDependencies>())
                {
                    dependencies.RequiredBundles.DoIf(seenBundles.Add, b => PermanentBundles.Insert(i + 1, b));
                }
            }

            ActiveLabelAssetBundles labelBundles = AccessTools.StaticFieldRefAccess<ActiveLabelAssetBundles>(typeof(Renpy), "s_ActiveLabelAssetBundles");

            AccessTools.Field(typeof(ActiveLabelAssetBundles), "<LabelAssetBundle>k__BackingField").SetValue(labelBundles, gestaltDependencies);
            AccessTools.Field(typeof(ActiveLabelAssetBundles), "<HasLabelAssetBundleLoaded>k__BackingField").SetValue(labelBundles, true);

            AccessTools.Field(typeof(ActiveAssetBundles), "PermanentHashCodes").SetValue(__instance, PermanentBundles.Select(s => s.GetHashCode()).ToArray());
            AccessTools.Field(typeof(ActiveAssetBundles), "PermanentHashCodesLength").SetValue(__instance, PermanentBundles.Count());

            __instance.SetPrivateField("m_ActiveAssetBundles", m_ActiveAssetBundles);
            __instance.SetPrivateField("m_ActiveBundles", m_ActiveBundles);

            return false;
		}

        //Written by Kizby. Prevents assets from being unloaded while the game changes labels.
        private static bool ChangeLabelPatch(ref ActiveLabelAssetBundles __instance, ref bool __result, ref string __0)
        {
            if (DokiModsManager.Mods.Count() == 0)
                return true;

            AccessTools.Field(typeof(ActiveLabelAssetBundles), "<ActiveLabel>k__BackingField").SetValue(__instance, __0);

            __result = true;

            return false;
        }

        //Written by Kizby. Prevents assets from being unloaded while the game changes labels.
        private static bool ChangeLabelSyncPatch(ref ActiveLabelAssetBundles __instance, ref string __0)
        {
            if (DokiModsManager.Mods.Count() == 0)
                return true;

            AccessTools.Field(typeof(ActiveLabelAssetBundles), "<ActiveLabel>k__BackingField").SetValue(__instance, __0);

            return false;
        }

        //Written by Kizby. Prevents assets from being unloaded while the game changes labels.
        private static bool ClearCurrentLabelBundlePatch()
        {
            if (DokiModsManager.Mods.Count() == 0)
                return true;

            return false;
        }

        //Written by Kizby. Ensures perm bundles are always loaded.
        private static bool ValidateLoadPatch(ref ActiveLabelAssetBundles __instance)
        {
            if (DokiModsManager.Mods.Count() == 0)
                return true;

            if (!__instance.HasLabelAssetBundleLoaded)
                Renpy.LoadPermanentAssetBundles();

            return true;
        }

        private static void NextContextPatch(ref RenpyCallstackEntry __instance, ref Line __result)
        {
            RenpyBlock block = __instance.GetBlock();

            int index = block.Contents.ToList().IndexOf(__result);

			RenpyUtils.ScriptExecutionContext = new RenpyScriptExecutionContext(__instance, __result, index, block);

            foreach (var mod in DokiModsManager.Mods)
                mod.OnNextLine();
        }

        private static void RewindContextPatch(ref RenpyCallstackEntry __instance)
        {
            RenpyBlock block = __instance.GetBlock();

			int index = __instance.nextLine;

			Line line = block.Contents[index];

            RenpyUtils.ScriptExecutionContext = new RenpyScriptExecutionContext(__instance, line, index, block);

			foreach (var mod in DokiModsManager.Mods)
				mod.OnNextLine();
        }

        private static bool ScriptExecutionPatch(ref Blocks __instance)
		{
            if (DokiModsManager.Mods.Count() == 0)
                return true;

            DokiMod mod = DokiModsManager.Mods.FirstOrDefault(x => x.Scripts.Count() > 0);

            if (mod == null)
                return true;

            var fakeBlocks = new Dictionary<string, RenpyBlock>();
            var fakeBlockEntryPoints = new Dictionary<string, BlockEntryPoint>();

            RenpyReturn item = new RenpyReturn();
            RenpyNOP item2 = new RenpyNOP();

            foreach (DDLCScript script in mod.Scripts)
            {
                ConsoleUtils.Log("Processing script: " + script.Label);

                Tuple<BlockEntryPoint, RenpyBlock> ExecutedBlock = mod.Execute(script);

                if (ExecutedBlock != null && ExecutedBlock != default)
                {
                    RenpyBlock renpyBlock = ExecutedBlock.Item2;
                    BlockEntryPoint entryPoint = ExecutedBlock.Item1;

                    fakeBlocks.Add(renpyBlock.Label, renpyBlock);
                    fakeBlockEntryPoints.Add(renpyBlock.Label, entryPoint);

                    ConsoleUtils.Log("Block Pushed: " + script.Label);
                }
            }

            if (Interpreter.Interpreter.AssetBlocks.Count > 0)
            {
                foreach(var assetBlock in Interpreter.Interpreter.AssetBlocks)
                {
                    fakeBlocks.Add(assetBlock.Item2.Label, assetBlock.Item2);
                    fakeBlockEntryPoints.Add(assetBlock.Item2.Label, assetBlock.Item1);

                    ConsoleUtils.Log("Asset Block Pushed: " + assetBlock.Item2.Label);
                }
            }

            for (int num = 0; num != Math.Min(__instance.blockkeys.Count, __instance.blockcontents.Count); num++)
            {
                List<Line> list = new List<Line>(__instance.blockcontents[num].contents.Count);

                string label = __instance.blockcontents[num].label;

                string decompiledScript = "";

                foreach (RenpyBlockLineDescriptor renpyBlockLineDescriptor in __instance.blockcontents[num].contents)
                {
                    switch (renpyBlockLineDescriptor.type)
                    {
                        case RenpyBlockLineDescriptor.Type.Dialog:
                            {
                                RenpyBlockDialogContent renpyBlockDialogContent = __instance.blockcontents[num].dialogcontents[renpyBlockLineDescriptor.index];
                                List<Tuple<int, float>> list2 = new List<Tuple<int, float>>(renpyBlockDialogContent.waitIndices.Count);
                                for (int i = 0; i < renpyBlockDialogContent.waitIndices.Count; i++)
                                {
                                    list2.Add(new Tuple<int, float>(renpyBlockDialogContent.waitIndices[i], renpyBlockDialogContent.waitTimes[i]));
                                }

                                RenpyDialogueLine Line = new RenpyDialogueLine(__instance.blockcontents[num].label, renpyBlockDialogContent.textID, renpyBlockDialogContent.label, renpyBlockDialogContent.variant, renpyBlockDialogContent.interpolate, renpyBlockDialogContent.skipWait, renpyBlockDialogContent.hasCps, renpyBlockDialogContent.cps, renpyBlockDialogContent.cpsStart, renpyBlockDialogContent.cpsEnd, renpyBlockDialogContent.cpsMultiplier, renpyBlockDialogContent.developerCommentary, renpyBlockDialogContent.immediateUntil, list2, renpyBlockDialogContent.command_type);

                                if (Line.Tag != "dc")
                                {
                                    string text = Renpy.Text.GetLocalisedString(Line.TextID, LocalisedStringPostProcessing.None, Line.ParentLabel, false);

                                    decompiledScript += (Line.Tag == "" ? "" : Line.Tag + " ") + "\"" + text + "\"" + "\n";
                                }

                                list.Add(Line);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Show:
                            decompiledScript += __instance.blockcontents[num].showContents[renpyBlockLineDescriptor.index].ShowData + "\n";

                            RenpyUtils.BaseShowContents.Add(__instance.blockcontents[num].showContents[renpyBlockLineDescriptor.index]);
                            list.Add(__instance.blockcontents[num].showContents[renpyBlockLineDescriptor.index]);
                            break;
                        case RenpyBlockLineDescriptor.Type.Hide:
                            decompiledScript += __instance.blockcontents[num].hideContents[renpyBlockLineDescriptor.index].HideData + "\n";

                            list.Add(__instance.blockcontents[num].hideContents[renpyBlockLineDescriptor.index]);
                            break;
                        case RenpyBlockLineDescriptor.Type.Scene:
                            decompiledScript += __instance.blockcontents[num].sceneContents[renpyBlockLineDescriptor.index].SceneData + "\n";

                            list.Add(__instance.blockcontents[num].sceneContents[renpyBlockLineDescriptor.index]);
                            break;
                        case RenpyBlockLineDescriptor.Type.With:
                            decompiledScript += "with " + __instance.blockcontents[num].withContents[renpyBlockLineDescriptor.index].WithData + "\n";

                            list.Add(__instance.blockcontents[num].withContents[renpyBlockLineDescriptor.index]);
                            break;
                        case RenpyBlockLineDescriptor.Type.Play:
                            {
                                Play play = __instance.blockcontents[num].playAudioContents[renpyBlockLineDescriptor.index].play;

                                decompiledScript += "play " + play.Channel.ToString().ToLower() + " " + play.Asset.ToLower() + "\n";

                                RenpyPlay item3 = __instance.blockcontents[num].playAudioContents[renpyBlockLineDescriptor.index];
                                list.Add(item3);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Stop:
                            {
                                Stop stop = __instance.blockcontents[num].stopAudioContents[renpyBlockLineDescriptor.index].stop;

                                decompiledScript += "stop " + stop.Channel.ToString().ToLower() + " " + (stop.fadeout > 0.0 ? $"fadeout {stop.fadeout}" : "") + "\n";

                                RenpyStop item4 = __instance.blockcontents[num].stopAudioContents[renpyBlockLineDescriptor.index];
                                list.Add(item4);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Queue:
                            {
                                decompiledScript += "queue: " + __instance.blockcontents[num].queueAudioContents[renpyBlockLineDescriptor.index].QueueData + "\n";

                                RenpyQueue item5 = __instance.blockcontents[num].queueAudioContents[renpyBlockLineDescriptor.index];
                                list.Add(item5);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Function:
                            {
                                decompiledScript += "function: " + __instance.blockcontents[num].functionContents[renpyBlockLineDescriptor.index].FunctionData + "\n";

                                RenpyFunction item6 = __instance.blockcontents[num].functionContents[renpyBlockLineDescriptor.index];
                                list.Add(item6);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.OneLinePython:
                            {
                                RenpyOneLinePython item7 = __instance.blockcontents[num].onelinePythonContents[renpyBlockLineDescriptor.index];
                                list.Add(item7);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.InlinePython:
                            {
                                RenpyInlinePython item8 = __instance.blockcontents[num].inlinePythonContents[renpyBlockLineDescriptor.index];
                                list.Add(item8);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.LabelEntryPoint:
                            {
                                decompiledScript += "label: " + __instance.blockcontents[num].entryPoints[renpyBlockLineDescriptor.index].NestedLabelData + "\n";

                                RenpyLabelEntryPoint renpyLabelEntryPoint = __instance.blockcontents[num].entryPoints[renpyBlockLineDescriptor.index];
                                list.Add(renpyLabelEntryPoint);
                                fakeBlockEntryPoints.Add(renpyLabelEntryPoint.entryPoint.label, new BlockEntryPoint(renpyLabelEntryPoint.entryPoint.rootLabel, renpyLabelEntryPoint.entryPoint.startIndex));
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Goto:
                            {
                                decompiledScript += "go to: " + __instance.blockcontents[num].gotoContents[renpyBlockLineDescriptor.index].TargetLabel + "\n";

                                RenpyGoTo item9 = __instance.blockcontents[num].gotoContents[renpyBlockLineDescriptor.index];

                                list.Add(item9);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Return:
                            decompiledScript += "return\n";

                            list.Add(item);
                            break;
                        case RenpyBlockLineDescriptor.Type.GotoLine:
                            {
                                decompiledScript += "goto line num: " + __instance.blockcontents[num].gotoLineContents[renpyBlockLineDescriptor.index].TargetLine + "\n";

                                RenpyGoToLine item10 = __instance.blockcontents[num].gotoLineContents[renpyBlockLineDescriptor.index];
                                list.Add(item10);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.GotoLineUnless:
                            {
                                decompiledScript += "goto line unless: " + __instance.blockcontents[num].gotoLineUnlessContents[renpyBlockLineDescriptor.index].ConditionText + "\n";

                                RenpyGoToLineUnless item11 = __instance.blockcontents[num].gotoLineUnlessContents[renpyBlockLineDescriptor.index];
                                list.Add(item11);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.GotoLineTimeout:
                            {
                                decompiledScript += "goto line timeout: " + __instance.blockcontents[num].gotoLineTimeoutContents[renpyBlockLineDescriptor.index].TargetLine + "\n";

                                RenpyGoToLineTimeout item12 = __instance.blockcontents[num].gotoLineTimeoutContents[renpyBlockLineDescriptor.index];
                                list.Add(item12);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.ForkGotoLine:
                            {
                                decompiledScript += "fork goto line: " + __instance.blockcontents[num].forkGotoLineContents[renpyBlockLineDescriptor.index].TargetLine + "\n";

                                RenpyForkGoToLine item13 = __instance.blockcontents[num].forkGotoLineContents[renpyBlockLineDescriptor.index];
                                list.Add(item13);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Immediate:
                            {
                                decompiledScript += "immediate: " + __instance.blockcontents[num].immediateTransforms[renpyBlockLineDescriptor.index].TransformCommand + "\n";

                                RenpyImmediateTransform item14 = __instance.blockcontents[num].immediateTransforms[renpyBlockLineDescriptor.index];
                                list.Add(item14);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Ease:
                            {
                                decompiledScript += "ease: " + __instance.blockcontents[num].easedTransforms[renpyBlockLineDescriptor.index].TransformCommand + "\n";

                                RenpyEasedTransform item15 = __instance.blockcontents[num].easedTransforms[renpyBlockLineDescriptor.index];
                                list.Add(item15);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Pause:
                            {
                                decompiledScript += "pause: " + __instance.blockcontents[num].pauses[renpyBlockLineDescriptor.index].PauseData + "\n";

                                RenpyPause item16 = __instance.blockcontents[num].pauses[renpyBlockLineDescriptor.index];
                                list.Add(item16);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.LoadImage:
                            {
                                decompiledScript += "load img: " + __instance.blockcontents[num].images[renpyBlockLineDescriptor.index].fullImageDetails + "\n";

                                RenpyLoadImage item17 = __instance.blockcontents[num].images[renpyBlockLineDescriptor.index];
                                list.Add(item17);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Size:
                            {
                                decompiledScript += "size: " + __instance.blockcontents[num].sizes[renpyBlockLineDescriptor.index].SizeData + "\n";

                                RenpySize item18 = __instance.blockcontents[num].sizes[renpyBlockLineDescriptor.index];
                                list.Add(item18);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Time:
                            {
                                decompiledScript += "time: " + __instance.blockcontents[num].times[renpyBlockLineDescriptor.index].TimeData + "\n";

                                RenpyTime item19 = __instance.blockcontents[num].times[renpyBlockLineDescriptor.index];
                                list.Add(item19);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.SetRandRange:
                            {
                                decompiledScript += "set rand range: " + __instance.blockcontents[num].setRandomRanges[renpyBlockLineDescriptor.index].Range + "\n";

                                RenpySetRandomRange item20 = __instance.blockcontents[num].setRandomRanges[renpyBlockLineDescriptor.index];
                                list.Add(item20);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Text:
                            {
                                decompiledScript += "text: " + __instance.blockcontents[num].texts[renpyBlockLineDescriptor.index].ToString() + "\n";

                                RenpyStandardProxyLib.Text item21 = __instance.blockcontents[num].texts[renpyBlockLineDescriptor.index];
                                list.Add(item21);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Expression:
                            {
                                decompiledScript += "expression: " + __instance.blockcontents[num].expressions[renpyBlockLineDescriptor.index].Expr.ToString() + "\n";

                                RenpyStandardProxyLib.Expression item22 = __instance.blockcontents[num].expressions[renpyBlockLineDescriptor.index];
                                list.Add(item22);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.WaitForScreen:
                            {
                                decompiledScript += "wait for screen: " + __instance.blockcontents[num].waitForScreen[renpyBlockLineDescriptor.index].screen + "\n";

                                RenpyStandardProxyLib.WaitForScreen item23 = __instance.blockcontents[num].waitForScreen[renpyBlockLineDescriptor.index];
                                list.Add(item23);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.MenuInput:
                            {
                                decompiledScript += "menu input: " + __instance.blockcontents[num].menuInputContents[renpyBlockLineDescriptor.index].ToString() + "\n";

                                RenpyMenuInput item24 = __instance.blockcontents[num].menuInputContents[renpyBlockLineDescriptor.index];
                                list.Add(item24);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.Window:
                            decompiledScript += "window: " + __instance.blockcontents[num].windowContents[renpyBlockLineDescriptor.index].WindowData + "\n";

                            list.Add(__instance.blockcontents[num].windowContents[renpyBlockLineDescriptor.index]);
                            break;
                        case RenpyBlockLineDescriptor.Type.NOP:
                            decompiledScript += "nop\n";
                            list.Add(item2);
                            break;
                        case RenpyBlockLineDescriptor.Type.Unlock:
                            {
                                decompiledScript += __instance.blockcontents[num].unlocks[renpyBlockLineDescriptor.index].UnlockData + "\n";

                                RenpyUnlock item25 = __instance.blockcontents[num].unlocks[renpyBlockLineDescriptor.index];
                                list.Add(item25);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.WindowAuto:
                            {
                                decompiledScript += "window auto: " + __instance.blockcontents[num].windowAuto[renpyBlockLineDescriptor.index].command + "\n";

                                RenpyStandardProxyLib.WindowAuto item26 = __instance.blockcontents[num].windowAuto[renpyBlockLineDescriptor.index];
                                list.Add(item26);
                                break;
                            }
                        case RenpyBlockLineDescriptor.Type.ClrFlag:
                            {
                                if (__instance.blockcontents[num].clrFlags != null)
                                {
                                    RenpyClrFlag item27 = __instance.blockcontents[num].clrFlags[renpyBlockLineDescriptor.index];

                                    list.Add(item27);
                                }
                                break;
                            }
                        default:
                            break;
                    }
                }

                //File.WriteAllText($"DokiDev\\Scripts\\{__instance.blockcontents[num].label}.rpy", decompiledScript);

                DDLCScript script = mod.Scripts[0];

                if (__instance.blockcontents[num].label == "ch0_main")
                {
                    DDLCScript script2 = mod.Scripts[0];

                    ConsoleUtils.Log("Processing script: " + script2.Label);

                    Tuple<BlockEntryPoint, RenpyBlock> ExecutedBlock = mod.Execute(script);

                    list.Clear();
                    list.Add(new RenpyGoTo(mod.Scripts[0].Label, false, $"jump {mod.Scripts[0].Label}"));
                }

                RenpyBlock renpyBlock = new RenpyBlock(__instance.blockcontents[num].label, __instance.blockcontents[num].isMainLabel, __instance.blockcontents[num].attributes, list);
                renpyBlock.callParameters = __instance.blockcontents[num].callParameters;
                fakeBlocks.Add(__instance.blockkeys[num], renpyBlock);
                fakeBlockEntryPoints.Add(__instance.blockkeys[num], new BlockEntryPoint(__instance.blockkeys[num], 0));
            }

            __instance.SetPrivateField("blocks", fakeBlocks);
            __instance.SetPrivateField("blockEntryPoints", fakeBlockEntryPoints);

            ConsoleUtils.Log("Ready to continue!");

            return false;
        }

		private static bool SayPatch(ref string __0, ref string __1, ref DialogueLine __2)
        {
            if (DokiModsManager.Mods.Count() > 0)
            {
                var line = RenpyUtils.RetrieveLineFromText(__2.TextID);

                if (line != null)
                {
                    __2.Text = line.Text;

                    if (line.FromPlayer)
                        __0 = Renpy.CurrentContext.GetVariableString("player");
                    else
                        __0 = line.Character;
                }
            }
            return true;
        }
    }
}
