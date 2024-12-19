﻿using Doki.Extensions;
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

            foreach (DokiMod mod in DokiModsManager.Mods)
            {
                foreach (string assetBundlePath in Directory.GetFiles(mod.AssetsPath))
                {
                    ConsoleUtils.Log("Doki", $"Loading mod asset bundle -> {assetBundlePath}");

                    string key = Path.GetFileNameWithoutExtension(assetBundlePath);
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
