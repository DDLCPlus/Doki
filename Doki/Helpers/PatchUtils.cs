﻿using Doki.Mods;
using Doki.Renpie;
using Doki.Renpie.Parser;
using HarmonyLib;
using RenpyLauncher;
using RenpyParser;
using RenPyParser.AssetManagement;
using RenPyParser.Images;
using RenPyParser.Sprites;
using RenPyParser.Transforms;
using RenPyParser.VGPrompter.DataHolders;
using SimpleExpressionEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityPS;

namespace Doki.Extensions
{
    public static class PatchUtils
    {
        private static Harmony HarmonyInstance { get; set; }
        public static bool Patched = false;
        public static bool RunModContextOnce = false;

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
                HarmonyInstance.Patch(typeof(RenpyExecutionContext).GetMethod("InitialiseWithAudioDefines", BindingFlags.Instance | BindingFlags.NonPublic), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("InitialiseWithAudioDefinesPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyScript).GetMethods().Where(x => x.Name == "Init").Last(), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("InitPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(Lines).GetMethod("GetValue"), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("HistoryPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(ActiveImage).GetMethod("ChangeAssetImmediate", BindingFlags.NonPublic | BindingFlags.Instance), postfix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ChangeAssetImmediatePostPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(FileBrowserApp).GetMethod("get_AllowRunResetSh", BindingFlags.NonPublic | BindingFlags.Instance), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("get_AllowRunResetShPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyScript).GetMethod("HandleInLinePython"), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("InLinePythonPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(Renpy).GetMethod("ForceUnloadAssetBundles"), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("DontUnloadPls", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(UnityEngine.Debug).GetMethod("LogException", new[] { typeof(Exception) }), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("LogExceptionPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(UnityEngine.Debug).GetMethod("Log", new[] { typeof(object) }), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("GeneralLogPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyScriptExecution).GetMethod("Run"), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("PythonPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(AccessTools.Method(typeof(Line), "IsValid"), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("IsValidPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(ExpressionRuntime).GetMethod("ExecuteWithInstance"), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ExecuteWithInstancePatch", BindingFlags.Static | BindingFlags.NonPublic)));

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

                ConsoleUtils.Log("Doki", "[BASE PATCH PREFIXES] Patched");
            }
            catch (Exception e)
            {
                ConsoleUtils.Error("Doki", new InvalidOperationException($"Failed to patch: {e}"));
            }
            finally
            {
                ConsoleUtils.Log("Doki", $"All Patches (base & mods) applied.");
                Patched = true;
            }
        }

        public static void DisablePatches() => Patched = false;

        public static void EnablePatches() => Patched = true;

        private static bool IsValidPatch(ref bool __result)
        {
            ConsoleUtils.Log("Doki.Line.IsValid", "Overriden to True");
            __result = true;
            return false;
        }

        private static bool DontUnloadPls()
        {
            if (!Patched)
                return true;

            return !RunModContextOnce;
        }

        private static bool GeneralLogPatch(object message)
        {
            return !BootLoader.CleanConsole;
        }

        private static bool ExecuteWithInstancePatch(ExpressionRuntime __instance, DataValue __result, CompiledExpression compiled, IContext context)
        {
            //if (compiled == null || (compiled.instructions.Count == 0 && compiled.constantFloats.Count == 0 && compiled.constantStrings.Count == 0 && compiled.constantObjects.Count == 0))
            //{
            //    __result = new DataValue("STRING");
            //    return false;
            //}

            //ConsoleUtils.Log("EXECUTE WITH INSTANCE", $"Trying to execute expression: (START)");

            //foreach (var instruction in compiled.instructions)
            //{
            //    Console.WriteLine(instruction.type + " - argument index: " + instruction.argumentIndex);
            //}

            //foreach(var flot in compiled.constantFloats)
            //{
            //    Console.WriteLine(flot + " - FLOAT");
            //}

            //foreach (var flot2 in compiled.constantStrings)
            //{
            //    Console.WriteLine(flot2 + " - STRING");
            //}

            //foreach (var obj in compiled.constantObjects)
            //{
            //    Console.WriteLine(obj.GetType() + " - " + obj.ToString() + " - OBJ (STR)");
            //}

            return true;
        }

        private static bool LogExceptionPatch(Exception exception)
        {
            return !BootLoader.CleanConsole;
        }

        private static bool PythonPatch(RenpyScriptExecution __instance, RenpyExecutionContext ____executionContext, string label)
        {
            if (!Patched)
                return true;

            foreach (Script script in ScriptsHandler.LoadedScripts)
            {
                if (script.EarlyPython.Count > 0)
                {
                    foreach (var earlyPy in script.EarlyPython)
                        Doki.Renpie.Rpyc.Extensions.ExecutePython(earlyPy, ____executionContext);

                    script.EarlyPython.Clear();
                }

                if (script.Python.Count > 0)
                {
                    foreach (var py in script.Python)
                        Doki.Renpie.Rpyc.Extensions.ExecutePython(py, ____executionContext);

                    script.Python.Clear();
                }
            }

            return true;
        }

        private static bool InLinePythonPatch(RenpyScript __instance, InLinePython InlinePython, ref object __result)
        {
            if (!Patched)
                return true;

            DokiMod mod = DokiModsManager.Mods[DokiModsManager.ActiveScriptModifierIndex];

            if (mod == null)
                return true;

            foreach (Script script in ScriptsHandler.LoadedScripts)
            {
                int hashCode = InlinePython.hash;

                if (script.InlinePython.TryGetValue(hashCode, out string functionName))
                {
                    MethodInfo methodInfo = mod.GetType().GetMethod(functionName, BindingFlags.NonPublic | BindingFlags.Instance);

                    if (methodInfo != null)
                    {
                        __result = methodInfo.Invoke(mod, null);
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool get_AllowRunResetShPatch(ref bool __result)
        {
            if (!Patched)
                return true;

            __result = true;
            return false;
        }

        private static void ChangeAssetImmediatePostPatch(ActiveImage __instance, string name, GameObject parent, bool force = false)
        {
            if (!Patched)
                return;

            foreach (Script script in ScriptsHandler.LoadedScripts)
            {
                RenpyDefinition definition = script.Definitions.FirstOrDefault(x => x.Name == name && x.Type == DefinitionType.Image);

                if (definition == null)
                    return;

                if (definition.Value.StartsWith("im.Composite("))
                {
                    var parsedCompositeValue = script.ParseFixedCompositeSprite(definition.Value);

                    GameObject compositeObject = new GameObject(name);
                    compositeObject.transform.SetParent(__instance.Object.transform, false);
                    compositeObject.transform.localPosition = Vector3.zero;
                    compositeObject.transform.localScale = Vector3.one;

                    for (int i = 0; i < parsedCompositeValue.AssetPaths.Length; i++)
                    {
                        string assetPath = parsedCompositeValue.AssetPaths[i];
                        Vector2Int offset = parsedCompositeValue.Offsets[i];
                        GameObject spriteHolder = new GameObject($"Sprite {assetPath}");

                        spriteHolder.transform.SetParent(compositeObject.transform, false);
                        spriteHolder.transform.localPosition = new Vector3(Mathf.Round(offset.x), Mathf.Round(offset.y), 0);
                        spriteHolder.transform.localScale = Vector3.one;

                        SpriteRenderer renderer = spriteHolder.AddComponent<SpriteRenderer>();
                        ProxyAssetBundle proxyBundle = AssetUtils.FindProxyBundleByAssetKey(Path.GetFileNameWithoutExtension(assetPath));

                        if (proxyBundle != null)
                        {
                            string outPath = proxyBundle.ToPath(Path.GetFileNameWithoutExtension(assetPath));
                            renderer.sprite = (Sprite)proxyBundle.Load("UnityEngine.Sprite", outPath);
                            renderer.sprite.texture.filterMode = FilterMode.Trilinear;
                            renderer.sprite.texture.wrapMode = TextureWrapMode.Clamp;
                        }
                        else
                        {
                            AssetBundle realBundle = AssetUtils.AssetBundles[AssetUtils.AssetsToBundles[Path.GetFileNameWithoutExtension(assetPath)].Item2.Item1];
                            if (realBundle == null)
                                continue;

                            renderer.sprite = realBundle.ForceLoadAsset<Sprite>(Path.GetFileNameWithoutExtension(assetPath));
                        }

                        ConsoleUtils.Log("PatchUtils -> ChangeAssetImmediatePostPatch", $"Sprite loaded: {renderer.sprite?.name ?? "null"}");
                    }
                }
            }
        }

        private static void InitPatch(RenpyScript __instance, Defaults defaults, Dictionary<string, CharacterData> characters, Defaults globals, StyleDefinitions styles, Dictionary<string, Dictionary<int, string>> lines, Dictionary<string, RenpyAudioData> audio, GameObject libObject)
        {
            if (!Patched)
                return;

            foreach (Script script in ScriptsHandler.LoadedScripts)
            {
                RenpyDefinition[] CustomCharacters = script.Definitions.Where(x => x.Type == DefinitionType.Character).ToArray();

                foreach (var customChar in script.Characters)
                    __instance.Characters.Add(customChar.Key, customChar.Value);
            }
        }

        private static bool HistoryPatch(Lines __instance, string innerKey, int outerKey, ref string __result)
        {
            if (!Patched)
                return true;

            Dictionary<string, Dictionary<int, string>> lines = (Dictionary<string, Dictionary<int, string>>)__instance.GetPrivateField("lines");

            foreach (Script script in ScriptsHandler.LoadedScripts)
            {
                var line = script.RetrieveLineFromText(outerKey);

                if (!lines.ContainsKey(innerKey))
                    lines.Add(innerKey, new Dictionary<int, string>());

                if (line != null && !lines[innerKey].ContainsKey(outerKey))
                {
                    lines[innerKey][outerKey] = line.Text;
                    __instance.SetPrivateField("lines", lines);
                }
            }

            return true;
        }

        private static void NextPatch(RenpyCallstackEntry __instance, Line __result)
        {
            if (!Patched)
                return;

            CurrentLine = (int)__instance.GetPrivateField("lastLine");
        }

        private static bool InitialiseWithAudioDefinesPatch(RenpyExecutionContext __instance, AudioDefines defines)
        {
            if (!Patched)
                return true;

            Dictionary<string, DataValue> dictionary = [];
            Dictionary<string, DataValue> m_ExecutionVariables = (Dictionary<string, DataValue>)__instance.GetPrivateField("m_ExecutionVariables");

            m_ExecutionVariables["audio"] = new DataValue(dictionary);

            foreach (KeyValuePair<string, RenpyAudioData> keyValuePair in defines.GetDictionary())
            {
                dictionary[keyValuePair.Key] = new DataValue(keyValuePair.Value);
            }

            foreach(Script script in ScriptsHandler.LoadedScripts)
            {
                foreach (RenpyDefinition renpyDefinition in script.Definitions)
                {
                    string name = renpyDefinition.Name;
                    string value = renpyDefinition.Value;

                    if (value.Contains(".ogg") || value.Contains(".mp3") || value.Contains(".wav"))
                        dictionary[name] = new DataValue(RenpyAudioData.CreateAudioData(value));
                }
            }

            __instance.SetPrivateField("m_ExecutionVariables", m_ExecutionVariables);
            return false;
        }

        private static void LoadPermanentBundlesPatch(ref ActiveAssetBundles __instance)
        {
            if (!Patched)
                return;

            try
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

                    if (bundleVal != null && !RunModContextOnce)
                        RunModContextOnce = true;

                    string key = bundle.Key;

                    foreach (var asset in bundleVal.GetAllAssetNames())
                    {
                        // assetKey -> bundleName -> assetFullPathInBundle
                        string assetKey = Path.GetFileNameWithoutExtension(asset);
                        AssetUtils.AssetsToBundles[assetKey] = new Tuple<string, Tuple<string, bool>>(key, new Tuple<string, bool>(asset, true));

                        if (key.StartsWith("bgm") || key.StartsWith("Lsfx"))
                            AssetUtils.QuickAudioAssetMap[assetKey] = key;
                    }
                }

                ConsoleUtils.Log("Doki", $"AssetsToBundles contains {AssetUtils.AssetsToBundles.Count} items.");
            }
            catch (Exception ex)
            {
                ConsoleUtils.Error("LoadPermanentBundlesPatch", ex);
            }
        }

        private static bool ImmediatePatch(RenpyLoadImage __instance, GameObject gameObject, CurrentTransform currentTransform, string key)
        {
            if (!Patched)
                return true;

            GameObject outObj = null;
            ProxyAssetBundle proxyBundle = AssetUtils.FindProxyBundleByAssetKey(key);
            bool overrideTransform = false;

            if (proxyBundle != null)
            {
                outObj = (GameObject)proxyBundle.Load("UnityEngine.GameObject", proxyBundle.ToPath(key));
                overrideTransform = true;
            }

            if (outObj == null)
                outObj = AssetUtils.FixLoad(key);

            gameObject.DestroyChildIfOnlyChild();
            GameObject gameObject2 = UnityEngine.Object.Instantiate(outObj, gameObject.transform, false);
            gameObject2.name = key;

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

            if (!overrideTransform)
                ApplyTransformData.Apply(gameObject, currentTransform, false);
            else
            {
                gameObject2.transform.position = new Vector3(0, 0, 0);
                gameObject2.transform.localPosition = new Vector3(0, -360, 0);

                gameObject2.SetActive(true);
                gameObject2.transform.parent.gameObject.SetActive(true); //why the fuck is this not active by default?
            }

            return false;
        }

        private static bool LoadPatch(AssetBundle __instance, string name, Type type, ref object __result)
        {
            if (!Patched)
                return true;

            string shortenedName = Path.GetFileNameWithoutExtension(name);
            ProxyAssetBundle proxyBundle = AssetUtils.FindProxyBundleByAssetKey(shortenedName);

            if (proxyBundle != null)
            {
                __result = proxyBundle.Load(type.ToString(), proxyBundle.ToPath(shortenedName));
                return false;
            }

            if (!AssetUtils.AssetBundles.ContainsKey(__instance.name))
               return true;

            if (name != "select" && name != "hover" && name != "frame")
               ConsoleUtils.Debug("Doki", $"Trying to force load -> {name} as type: {type}");

            // Yes, I know I have an instance to the AssetBundle but shit will fucking crash if I use it.
            AssetBundle realInstance = AssetUtils.AssetBundles[__instance.name];
            __result = realInstance.ForceLoadAsset(name, type);
            return false;
        }

        private static void ScriptExecutionPatch(ref Blocks __instance)
        {
            if (!Patched)
                return;

            try
            {
                var __blocks = (Dictionary<string, RenpyBlock>)__instance.GetPrivateField("blocks");
                var __blockEntryPoints = (Dictionary<string, BlockEntryPoint>)__instance.GetPrivateField("blockEntryPoints");

                foreach(Script script in ScriptsHandler.LoadedScripts)
                {
                    foreach (var block in script.BlocksDict)
                    {
                        string label = block.Key;
                        Tuple<BlockEntryPoint, RenpyBlock> brick = block.Value;

                        if (__blocks.TryGetValue(label, out RenpyBlock _))
                        {
                            __blocks.Remove(label);
                            __blockEntryPoints.Remove(label);
                        }

                        __blockEntryPoints.Add(label, brick.Item1);
                        __blocks.Add(label, brick.Item2);

                        ConsoleUtils.Log("Doki", $"Block processed -> {block.Key}");
                    }

                    RenpyDefinition[] imageDefinitions = script.Definitions.Where(x => x.Type == DefinitionType.Image).ToArray();

                    foreach (var imageDefinition in imageDefinitions)
                    {
                        bool isBackgroundDefinition = imageDefinition.Name.StartsWith("bg ");

                        if (isBackgroundDefinition)
                        {
                            var backgroundDefinition = imageDefinition; //im lazy

                            string labelThatCalls = backgroundDefinition.Name; //the label just -> call label that loads image file -> sizes -> in this case bg bg_test2
                            string prefabForBackground = backgroundDefinition.Value.Split('/')[0]; //prefab name for game obj with bg -> in this case bg_test_original.prefab aka bg_test_original
                            string imageFile = backgroundDefinition.Value.Split('/')[1]; //image file -> in this case bg_test2.png

                            if (!__blockEntryPoints.ContainsKey(labelThatCalls))
                            {
                                __blockEntryPoints.Add(labelThatCalls, new BlockEntryPoint(labelThatCalls));
                                __blocks.Add(labelThatCalls, new RenpyBlock(labelThatCalls)
                                {
                                    callParameters = [],
                                    Contents = [new RenpyLoadImage(prefabForBackground, imageFile), RenpyUtils.CreateSize(1280, 720)]
                                });
                            }
                        }
                        else
                        {
                            string imageBlockName = imageDefinition.Name;
                            string val = imageDefinition.Value;

                            if (!__blockEntryPoints.ContainsKey(imageBlockName))
                            {
                                __blockEntryPoints.Add(imageBlockName, new BlockEntryPoint(imageBlockName));
                                __blocks.Add(imageBlockName, new RenpyBlock(imageBlockName)
                                {
                                    callParameters = [],
                                    Contents = [new RenpyLoadImage(imageBlockName, val)]
                                });
                            }
                        }
                    }
                }

                foreach (var block in __blocks)
                {
                    if (block.Key == "start")
                    {
                        block.Value.Contents.Clear();
                        block.Value.Contents.AddRange(
                        [
                            new RenpyGoTo(ScriptsHandler.JumpTolabel, false, $"jump {ScriptsHandler.JumpTolabel}")
                        ]);
                    }
                }

                __instance.SetPrivateField("blocks", __blocks);
                __instance.SetPrivateField("blockEntryPoints", __blockEntryPoints);

                ConsoleUtils.Log("Doki", "Blocks deserialization overriden!");
            }
            catch(Exception e)
            {
                ConsoleUtils.Error("Doki", e, "Failed to override Blocks deserialization!");
            }
        }

        private static bool DebugPatch(ref LogType logType, ref object message)
        {
            //if (!Patched)
            //    return true;

            if (!BootLoader.CleanConsole)
            {
                ConsoleUtils.ColourWrite([
                    new ConsoleUtils.ColouredText($"{logType}: {message}\n", ConsoleColor.Yellow)
                ]);
            }

            return !BootLoader.CleanConsole;
        }

        private static bool ResolveLabelPatch(ref RenpyScript __instance, ref ValueTuple<RenpyBlock, int> __result, ref string label)
        {
            //ConsoleUtils.Debug("Doki", $"Resolving normal label -> {label}");
            return true;
        }

        private static void ResolveLabelPatchAfter(ref RenpyScript __instance, ref ValueTuple<RenpyBlock, int> __result, ref string label)
        {
            if (!Patched)
                return;

            if (__result.Item1.Label.Contains("natsuki") || __result.Item1.Label.Contains("bg") || __result.Item1.Label.Contains("t11"))
            {
                RenpyUtils.DumpBlock(__result.Item1);
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
            if (!Patched)
                return;

            ConsoleUtils.Debug("Doki", $"Tried to resolve normal label -> {label}");

            if (label.Contains("bg") || label.Contains("natsuki") || label.Contains("t11"))
                RenpyUtils.DumpBlock(tuple.Item1);
        }

        private static bool InitAudioSourcePatch(MusicSource __instance, RenpyAudioData audioData, ref int index, bool looped, int loopCount, IContext context, string setFlag, bool immediate = true, int loopNumber = 0)
        {
            if (!Patched)
                return true;

            //name = t2
            //simple asset name = 2
            // load from simple asset name but check from name

            bool noResultsFound = ScriptsHandler.LoadedScripts.Where(x => !x.Sounds.Contains(audioData.name) && !x.Sounds.Contains(audioData.simpleAssetName)).Any();

            if (noResultsFound)
                return true; //It is a whole nightmare dealing with official shit

            var musicSources = AccessTools.Field(__instance.GetType(), "musicSources").GetValue(__instance) as AudioSource[];
            var musicDataArray = AccessTools.Field(__instance.GetType(), "musicData").GetValue(__instance);
            var sourceCount = (int)AccessTools.Field(__instance.GetType(), "sourceCount").GetValue(__instance);
            var queueIndexField = AccessTools.Field(__instance.GetType(), "queueIndex");

            if (index >= musicSources.Length)
                index = 0;

            AudioClip audioClip = null;
            AudioSource audioSource = musicSources[index];
            object musicDataInstance = ((Array)musicDataArray).GetValue(index);

            int queueIndex = (index + 1) % sourceCount;
            queueIndexField.SetValue(__instance, queueIndex);

            object nextMusicData = ((Array)musicDataArray).GetValue(queueIndex);

            ProxyAssetBundle proxyBundle = AssetUtils.FindProxyBundleByAssetKey(audioData.simpleAssetName);
            if (proxyBundle != null)
            {
                string outPath = proxyBundle.ToPath(audioData.simpleAssetName);
                audioClip = (AudioClip)proxyBundle.Load("UnityEngine.AudioClip", outPath);
            }

            if (audioClip == null)
            {
                AssetBundle foundBundle = AssetUtils.GetPreciseAudioRelatedBundle(audioData.simpleAssetName);

                if (foundBundle == null)
                    return true;

                audioClip = foundBundle.ForceLoadAsset<AudioClip>(audioData.simpleAssetName);
            }

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
                    __instance.StartCoroutine((IEnumerator)AccessTools.Method(__instance.GetType(), "FlagCoroutine").Invoke(__instance, [nextStartTime, setFlag]));
            }

            var calculateNextStartTimeMethod = AccessTools.Method(typeof(MusicSource), "CalculateNextStartTime");
            double calculatedNextStartTime = (double)calculateNextStartTimeMethod.Invoke(null, [musicDataInstance, audioSource, context]);

            var setScheduledEndTimeMethod = AccessTools.Method(typeof(AudioSource), "SetScheduledEndTime");
            setScheduledEndTimeMethod.Invoke(audioSource, [calculatedNextStartTime]);

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
    }
}
