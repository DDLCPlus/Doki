using Doki.Extensions;
using Doki.Mods;
using Doki.RenpyUtils;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Doki.Core
{
    /*
     RENDISCO MIT LICENSE:

     Copyright (c) 2024 aaartrtrt

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
    */
    public class Main : MonoBehaviour
    {
        public void Awake()
        {
            if (!Directory.Exists("Doki") && !Directory.Exists("Doki\\Mods"))
            {
                ConsoleUtils.Log("Doki", "First time setup.. creating necessary directories and loading assets..");

                Directory.CreateDirectory("Doki");
                Directory.CreateDirectory("Doki\\Mods");

                ConsoleUtils.Log("Doki", "First time setup complete! To install a mod, drag & drop it into the \"Mods\" folder which can be found within the \"Doki\" folder in your game directory.");
                return;
            }

            ConsoleUtils.Log("Doki", "RenDisco parser setup.\nLoading mods...");

            DokiModsManager.LoadMods();

            DokiMod contextMod = DokiModsManager.Mods.FirstOrDefault(x => x.ModifiesContext);

            if (contextMod != null)
            {
                ConsoleUtils.Log("Doki", "Parsing blocks for Mod's scripts..");

                DokiModsManager.ActiveScriptModifierIndex = DokiModsManager.Mods.IndexOf(contextMod);

                string[] scriptPaths = Directory.GetFiles(contextMod.ScriptsPath);
                for (int i = 0; i < scriptPaths.Count(); i++)
                {
                    ConsoleUtils.Log("Doki", $"Parsing {scriptPaths[i]}...");
                    RenpyScriptProcessor.ProcessScriptFromFile(scriptPaths[i]);
                    ConsoleUtils.Log("Doki", $"Blocks processed for script");
                }

                RenpyScriptProcessor.JumpTolabel = contextMod.LabelEntryPoint;
            }

            AssetBundle PlusBgmCoarse = AssetUtils.LoadGameBundle("bgm-ddlcplus-coarse"); //for some reason the game doesnt load this in load permanent asset bundles?

            AssetUtils.AssetBundles.Add("bgm-ddlcplus-coarse", PlusBgmCoarse); //y2k4 :pleading_face: please clean this and maybe turn loading asset bundles and mapping their internal asset names into a func?

            foreach (var asset in PlusBgmCoarse.GetAllAssetNames())
            {
                // assetKey -> bundleName -> assetFullPathInBundle
                AssetUtils.AssetsToBundles[Path.GetFileNameWithoutExtension(asset)] = new Tuple<string, Tuple<string, bool>>("bgm-ddlcplus-coarse", new Tuple<string, bool>(asset, true));
            }

            foreach (DokiMod mod in DokiModsManager.Mods)
            {
                foreach (string assetBundlePath in Directory.GetFiles(mod.AssetsPath))
                {
                    string key = Path.GetFileNameWithoutExtension(assetBundlePath);

                    if (Path.GetExtension(assetBundlePath) != "" && !Path.GetExtension(assetBundlePath).ToLower().Contains("assetbundle"))
                    {
                        if (!AssetUtils.FakeBundles.ContainsKey(mod.ID))
                        {
                            AssetUtils.FakeBundles.Add(mod.ID, new ProxyAssetBundle());

                            ConsoleUtils.Log("Doki", $"Proxying asset bundle for mod ID: {mod.ID}...");
                        }

                        ProxyAssetBundle proxyBundle = AssetUtils.FakeBundles[mod.ID];

                        proxyBundle.Map(key, assetBundlePath);

                        ConsoleUtils.Log("Doki", $"Mapping asset key: {key} to fake path -> {assetBundlePath}...");

                        continue;
                    }

                    ConsoleUtils.Log("Doki", $"Loading mod asset bundle -> {assetBundlePath}");

                    AssetBundle bundle = AssetUtils.LoadAssetBundle(assetBundlePath);
                    AssetUtils.AssetBundles.Add(key, bundle);

                    foreach (var asset in bundle.GetAllAssetNames())
                    {
                        // bundle name, asset key, asset path in bundle
                        ConsoleUtils.Debug("Doki", $"Fake asset -> {key} -> {asset} -> {Path.GetFileNameWithoutExtension(asset)}");

                        // assetKey -> bundleName -> assetFullPathInBundle
                        AssetUtils.AssetsToBundles[Path.GetFileNameWithoutExtension(asset)] = new Tuple<string, Tuple<string, bool>>(key, new Tuple<string, bool>(asset, true));
                    }

                    ConsoleUtils.Log("Doki", $"Asset bundle loaded -> {key}");
                }
            }

            ConsoleUtils.Log("Doki", "Mod asset bundles loaded.\nApplying patches...");
            PatchUtils.ApplyPatches();
            ConsoleUtils.Log("Doki", "Initialized BootLoader successfully");
        }
    }
}
