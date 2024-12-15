using Doki.Extensions;
using Doki.Mods;
using Doki.RenpyUtils;
using Doki.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Doki
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
                ConsoleUtils.Log("First time setup.. creating necessary directories and loading assets..");

                Directory.CreateDirectory("Doki");
                Directory.CreateDirectory("Doki\\Mods");

                ConsoleUtils.Log("First time setup complete! To install a mod, drag & drop it into the \"Mods\" folder which can be found within the \"Doki\" folder in your game directory.");

                return;
            }

            ConsoleUtils.Log("RenDisco parser setup.");
            ConsoleUtils.Log("Loading mods..");

            DokiModsManager.LoadMods();

            DokiMod contextMod = DokiModsManager.Mods.FirstOrDefault(x => x.ModifiesContext);

            if (contextMod != null)
            {
                DokiModsManager.ActiveScriptModifierIndex = DokiModsManager.Mods.IndexOf(contextMod);

                ConsoleUtils.Log("Parsing blocks for Mod's scripts..");

                string[] scriptPaths = Directory.GetFiles(contextMod.ScriptsPath);

                for (int i = 0; i < scriptPaths.Count(); i++)
                {
                    string scriptPath = scriptPaths[i];

                    ConsoleUtils.Log($"Parsing {scriptPath}...");

                    RenpyScriptProcessor.ProcessScriptFromFile(scriptPath);

                    ConsoleUtils.Log($"Blocks processed for script");
                }
            }

            PatchUtils.ApplyPatches();

            ConsoleUtils.Log("Initialized BootLoader successfully");
        }
    }
}
