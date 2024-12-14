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

        static PatchUtils()
        {
            HarmonyInstance = new Harmony("Doki");
        }

        public static void ApplyPatches()
        {
			try
            {
                HarmonyInstance.Patch(typeof(RenpyScriptExecution).GetMethod("PostLoad"), new HarmonyMethod(typeof(PatchUtils).GetMethod("MainScriptContextPatch", BindingFlags.Static | BindingFlags.NonPublic)));
                HarmonyInstance.Patch(typeof(RenpyScriptExecution).GetMethod("Run"), new HarmonyMethod(typeof(PatchUtils).GetMethod("MainScriptContextPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                ConsoleUtils.Log("[BASE PATCH PREFIXES] Attempting to patch assets, BIOS, Dialogue, Quitting.");

                //HarmonyInstance.Patch(typeof(ActiveAssetBundles).GetMethod("LoadPermanentBundles"), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("LoadPermanentBundlesPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                //HarmonyInstance.Patch(typeof(ActiveLabelAssetBundles).GetMethod("ChangeLabel", new Type[] { typeof(string) }), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ChangeLabelPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                //HarmonyInstance.Patch(typeof(ActiveLabelAssetBundles).GetMethod("ChangeLabelSync", new Type[] { typeof(string) }), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ChangeLabelSyncPatch", BindingFlags.Static | BindingFlags.NonPublic)));

                //HarmonyInstance.Patch(typeof(ActiveLabelAssetBundles).GetMethod("ClearCurrentLabelBundle", BindingFlags.Instance | BindingFlags.NonPublic), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ClearCurrentLabelBundlePatch", BindingFlags.Static | BindingFlags.NonPublic)));

                //HarmonyInstance.Patch(typeof(ActiveLabelAssetBundles).GetMethod("ValidateLoad", BindingFlags.Instance | BindingFlags.NonPublic), prefix: new HarmonyMethod(typeof(PatchUtils).GetMethod("ValidateLoadPatch", BindingFlags.Static | BindingFlags.NonPublic)));

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

        private static bool MainScriptContextPatch(RenpyScriptExecution __instance, RenpyExecutionContext ____executionContext)
        {
            if (RenpyUtils.ScriptExecutionContext == null)
                RenpyUtils.ScriptExecutionContext = new RenpyScriptExecutionContext(); //It would make no sense storing execution context as we just pass it into every method we use anyways

            if (DokiModsManager.ActiveScriptModifierIndex == -1)
                return true;

            RenpyUtils.ScriptExecutionContext.HandleContextChange(__instance, ____executionContext);

            return false;
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

		private static bool SayPatch(ref string __0, ref string __1, ref DialogueLine __2)
        {
            if (DokiModsManager.ActiveScriptModifierIndex == -1)
                return true;

            var line = RenpyUtils.RetrieveLineFromText(__2.TextID);

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
