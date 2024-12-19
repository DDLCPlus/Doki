using Doki.Mods;
using Doki.RenpyUtils;
using HarmonyLib;
using RenpyParser;
using RenPyParser.AssetManagement;
using RenPyParser.Transforms;
using RenPyParser.VGPrompter.DataHolders;
using SimpleExpressionEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityPS;

namespace Doki.Extensions
{
    public static class PatchUtils
    {
        private static Harmony HarmonyInstance { get; set; }
        public static bool Patched = false;

        private static int CurrentLine = 0;

        static PatchUtils() =>
            HarmonyInstance = new Harmony("Doki");

        public static void ApplyPatches()
        {
            try
            {
                HarmonyInstance.Patch(typeof(Blocks).GetMethod("OnAfterDeserialize"), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ScriptExecutionPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyScript).GetMethod("ResolveLabel"), new HarmonyMethod(typeof(PatchUtils).GetMethod("ResolveLabelPatch", BindingFlags.Static | BindingFlags.NonPublic)), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ResolveLabelPatchAfter", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyScript).GetMethod("TryResolveLabel"), new HarmonyMethod(typeof(PatchUtils).GetMethod("TryResolveLabelPatch", BindingFlags.Static | BindingFlags.NonPublic)), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("TriedResolveLabelPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(UnityEngine.Logger).GetMethod("Log", [typeof(LogType), typeof(object)]), new HarmonyMethod(typeof(PatchUtils).GetMethod("DebugPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(AssetBundle).GetMethod("LoadAsset", [typeof(string), typeof(Type)]), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("LoadPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(ActiveAssetBundles).GetMethod("LoadPermanentBundles"), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("LoadPermanentBundlesPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyLoadImage).GetMethod("Immediate", BindingFlags.Static | BindingFlags.Public), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ImmediatePatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyCallstackEntry).GetMethod("Next"), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("NextPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(MusicSource).GetMethod("InitAudioSource", BindingFlags.Instance | BindingFlags.NonPublic), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("InitAudioSourcePatch", BindingFlags.Static | BindingFlags.NonPublic))); //If I could patch IResources.Load's implementation and have it trigger with 0Harmony, I would. I'm sorry.

                foreach (var mod in DokiModsManager.Mods)
                {
                    if (mod.Prefixes != null && mod.Prefixes.Count() > 0)
                    {
                        foreach (var prefix in mod.Prefixes)
                        {
                            HarmonyInstance.Patch(prefix.Key, prefix: prefix.Value);
                            ConsoleUtils.Log("Doki", $"[MOD PATCH PREFIXES -> {mod.Name}]: Patched {prefix.Key.Name}!");
                        }
                    }

                    if (mod.Postfixes != null && mod.Postfixes.Count() > 0)
                    {
                        foreach (var postfix in mod.Postfixes)
                        {
                            HarmonyInstance.Patch(postfix.Key, postfix: postfix.Value);
                            ConsoleUtils.Log("Doki", $"[MOD PATCH POSTFIXES -> {mod.Name}]: Patched {postfix.Key.Name}!");
                        }
                    }
                }

                HarmonyInstance.Patch(typeof(RenpyWindowManager).GetMethod("Say"), new HarmonyMethod(typeof(PatchUtils).GetMethod("SayPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                ConsoleUtils.Log("Doki", "[BASE PATCH PREFIXES] Patched");
            }
            catch (Exception e)
            {
                ConsoleUtils.Error("Doki", $"Failed to patch: {e}");
            }
            finally
            {
                ConsoleUtils.Log("Doki", $"All Patches (base & mods) applied.");
                Patched = true;
            }
        }

        private static void NextPatch(RenpyCallstackEntry __instance, Line __result)
        {
            CurrentLine = (int)__instance.GetPrivateField("lastLine");
            if (RenpyUtils.RenpyUtils.CustomDefinitions.TryGetValue(new Tuple<int, string>(CurrentLine, __instance.blockName), out RenpyDefinition definition))
                Renpy.CurrentContext.SetVariableString(definition.Name, definition.Value.ToString());
        }

        private static void LoadPermanentBundlesPatch(ref ActiveAssetBundles __instance)
        {
            Dictionary<string, AssetBundle> m_ActiveAssetBundles = (Dictionary<string, AssetBundle>)__instance.GetPrivateField("m_ActiveAssetBundles");

            foreach (var kvp in m_ActiveAssetBundles)
            {
                if (!AssetUtils.AssetBundles.ContainsKey(kvp.Key))
                    AssetUtils.AssetBundles[kvp.Key] = kvp.Value;
            }

            foreach (var bundle in AssetUtils.AssetBundles)
            {
                var bundleVal = bundle.Value;
                string key = bundle.Key;

                foreach (var asset in bundleVal.GetAllAssetNames())
                {
                    // //assetKey -> bundleName -> assetFullPathInBundle

                    string assetKey = Path.GetFileNameWithoutExtension(asset);
                    AssetUtils.AssetsToBundles[assetKey] = new Tuple<string, Tuple<string, bool>>(key, new Tuple<string, bool>(asset, true));
                }
            }

            ConsoleUtils.Log("Doki", $"AssetsToBundles contains {AssetUtils.AssetsToBundles.Count} items.");
        }

        private static bool ImmediatePatch(RenpyLoadImage __instance, GameObject gameObject, CurrentTransform currentTransform, string key)
        {
            gameObject.DestroyChildIfOnlyChild();
            GameObject gameObject2 = UnityEngine.Object.Instantiate(AssetUtils.FixLoad(key), gameObject.transform, false);

            if (key.Contains("poem_special") || key.Contains("poem_end"))
            {
                string languageKey = key.Replace("original", Renpy.GetCurrentLanguagePrefix());
                if (AssetUtils.alternateImageNames.ContainsKey(languageKey))
                    languageKey = AssetUtils.alternateImageNames[languageKey];

                typeof(RenpyLoadImage).GetMethod("ReplaceSprite", BindingFlags.NonPublic | BindingFlags.Static).Invoke(__instance, [gameObject2, languageKey]);
            }

            if (key.Contains("bsod"))
            {
                string languageKey2 = $"bsod_{Renpy.GetCurrentLanguagePrefix()}";
                typeof(RenpyLoadImage).GetMethod("ReplaceSprite", BindingFlags.NonPublic | BindingFlags.Static).Invoke(__instance, [gameObject2, languageKey2]);
            }

            gameObject2.name = key;
            ApplyTransformData.Apply(gameObject, currentTransform, false);
            return false;
        }

        private static bool LoadPatch(AssetBundle __instance, string name, Type type, ref object __result)
        {
            if (!AssetUtils.AssetBundles.ContainsKey(__instance.name))
                return true;

            if (name != "select" && name != "hover" && name != "frame")
                ConsoleUtils.Debug("Doki", $"Trying to force load -> {name} as type: {type}");

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

                ConsoleUtils.Log("Doki", $"Block processed -> {block.Key}");
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
                    block.Value.Contents.AddRange(
                    [
                        new RenpyGoTo(RenpyScriptProcessor.JumpTolabel, false, $"jump {RenpyScriptProcessor.JumpTolabel}")
                    ]);
                }
            }

            __instance.SetPrivateField("blocks", __blocks);
            __instance.SetPrivateField("blockEntryPoints", __blockEntryPoints);

            ConsoleUtils.Log("Doki", "Blocks deserialization overriden!");
        }

        private static bool DebugPatch(ref LogType logType, ref object message)
        {
            ConsoleUtils.ColourWrite([
                new ConsoleUtils.ColouredText($"{logType}: {message}\n", ConsoleColor.Yellow)
            ]);
            return false /*true*/;
        }

        private static bool ResolveLabelPatch(ref RenpyScript __instance, ref ValueTuple<RenpyBlock, int> __result, ref string label)
        {
            //ConsoleUtils.Debug("Doki", $"Resolving normal label -> {label}");
            return true;
        }

        private static void ResolveLabelPatchAfter(ref RenpyScript __instance, ref ValueTuple<RenpyBlock, int> __result, ref string label)
        {
            if (__result.Item1.Label.Contains("bg"))
            {
                RenpyUtils.RenpyUtils.DumpBlock(__result.Item1);
                ConsoleUtils.Debug("Doki", $"Resolved normal label -> {label}");
            }
        }

        private static bool TryResolveLabelPatch(ref RenpyScript __instance, ref string label)
        {
            //ConsoleUtils.Debug("Doki", $"Trying to resolve normal label -> {label}");

            return true;
        }

        private static void TriedResolveLabelPatch(ref RenpyScript __instance, ref string label, ref ValueTuple<RenpyBlock, int> tuple)
        {
            ConsoleUtils.Debug("Doki", $"Tried to resolve normal label -> {label}");

            if (label.Contains("bg"))
                RenpyUtils.RenpyUtils.DumpBlock(tuple.Item1);
        }

        private static bool InitAudioSourcePatch(MusicSource __instance, RenpyAudioData audioData, ref int index, bool looped, int loopCount, IContext context, string setFlag, bool immediate = true, int loopNumber = 0)
        {
            //name = t2
            //simple asset name = 2
            // load from simple asset name but check from name

            if (!RenpyUtils.RenpyUtils.Sounds.Contains(audioData.name))
                return true; //It is a whole nightmare dealing with official shit

            var musicSources = AccessTools.Field(__instance.GetType(), "musicSources").GetValue(__instance) as AudioSource[];
            var musicDataArray = AccessTools.Field(__instance.GetType(), "musicData").GetValue(__instance);
            var sourceCount = (int)AccessTools.Field(__instance.GetType(), "sourceCount").GetValue(__instance);
            var queueIndexField = AccessTools.Field(__instance.GetType(), "queueIndex");

            if (index >= musicSources.Length)
                index = 0;

            AudioSource audioSource = musicSources[index];

            object musicDataInstance = ((Array)musicDataArray).GetValue(index);

            int queueIndex = (index + 1) % sourceCount;
            queueIndexField.SetValue(__instance, queueIndex);

            object nextMusicData = ((Array)musicDataArray).GetValue(queueIndex);

            AssetBundle foundBundle = AssetUtils.GetPreciseAudioRelatedBundle(audioData.simpleAssetName);

            if (foundBundle == null)
                return true;

            AudioClip audioClip = foundBundle.ForceLoadAsset<AudioClip>(audioData.simpleAssetName);
            audioSource.clip = audioClip;

            if (looped)
            {
                string loop = audioData.GetLoop(context);
                audioSource.time = string.IsNullOrEmpty(loop) ? 0f : float.Parse(loop);
            }
            else
            {
                string from = audioData.GetFrom(context);
                string to = audioData.GetTo(context);
                string loop = audioData.GetLoop(context);

                float startTime = string.IsNullOrEmpty(from) ? 0f : float.Parse(from);
                float endTime = string.IsNullOrEmpty(to) ? audioClip.length : float.Parse(to);
                float loopPoint = string.IsNullOrEmpty(loop) ? 0f : float.Parse(loop);

                while (startTime >= endTime - 0.05f && startTime >= 0.05f)
                    startTime -= endTime - loopPoint;

                startTime = Math.Max(0f, startTime);
                audioSource.time = startTime;
            }

            var audioDataField = musicDataInstance.GetType().GetField("AudioData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var loopNumberField = musicDataInstance.GetType().GetField("LoopNumber", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            audioDataField?.SetValue(musicDataInstance, audioData);
            loopNumberField?.SetValue(musicDataInstance, loopNumber);

            if (immediate)
            {
                var config = AudioSettings.GetConfiguration();
                var startTimeField = musicDataInstance.GetType().GetField("StartTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                double startTime = AudioSettings.dspTime + (double)config.dspBufferSize / config.sampleRate;
                startTimeField?.SetValue(musicDataInstance, startTime);
                audioSource.PlayScheduled(startTime);
            }
            else
            {
                var startTimeField = musicDataInstance.GetType().GetField("NextStartTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                double nextStartTime = (double)startTimeField?.GetValue(musicDataInstance);

                startTimeField?.SetValue(musicDataInstance, nextStartTime);
                audioSource.PlayScheduled(nextStartTime);

                if (!string.IsNullOrWhiteSpace(setFlag))
                    __instance.StartCoroutine((IEnumerator)AccessTools.Method(__instance.GetType(), "FlagCoroutine").Invoke(__instance, new object[] { nextStartTime, setFlag }));
            }

            var calculateNextStartTimeMethod = AccessTools.Method(typeof(MusicSource), "CalculateNextStartTime");
            double calculatedNextStartTime = (double)calculateNextStartTimeMethod.Invoke(null, new object[] { musicDataInstance, audioSource, context });

            var setScheduledEndTimeMethod = AccessTools.Method(typeof(AudioSource), "SetScheduledEndTime");
            setScheduledEndTimeMethod.Invoke(audioSource, new object[] { calculatedNextStartTime });

            var currentEndTimeField = AccessTools.Field(__instance.GetType(), "m_CurrentEndTime");
            currentEndTimeField.SetValue(__instance, calculatedNextStartTime);

            var nextStartTimeField = nextMusicData.GetType().GetField("NextStartTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            nextStartTimeField?.SetValue(nextMusicData, calculatedNextStartTime);

            if (audioData.Looping)
            {
                var queuedAudioField = AccessTools.Field(__instance.GetType(), "queuedAudio");
                Queue<MusicSource.QueuedMusicData> queuedAudioQueue = (Queue<MusicSource.QueuedMusicData>)queuedAudioField.GetValue(__instance);

                queuedAudioQueue.Enqueue(new MusicSource.QueuedMusicData
                {
                    Looped = true,
                    LoopNumber = loopNumber + 1,
                    LoopsRemaining = loopCount,
                    AudioData = audioData,
                    SetFlag = setFlag
                });
            }

            return false;
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
