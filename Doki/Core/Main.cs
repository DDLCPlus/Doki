using Doki.Interpreter;
using Doki.Mods;
using Doki.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Doki
{
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

            ConsoleUtils.Log("Setting up rendisco renpyparser..");

            RenpyUtils.Parser = new RenDisco.RenpyParser();

            ConsoleUtils.Log("Loading mods..");

            DokiModsManager.LoadMods();

            PatchUtils.ApplyPatches();

            ConsoleUtils.Log("Initialized BootLoader successfully");
        }
    }
}
