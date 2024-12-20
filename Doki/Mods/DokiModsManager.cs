using Doki.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Doki.Mods
{
    public static class DokiModsManager
    {
        public static List<DokiMod> Mods = [];
        public static int ActiveScriptModifierIndex = 0;

        public static void LoadMods()
        {
            ConsoleUtils.Log("DokiModsManager", "Loading Doki Mods..");

            if (Directory.GetDirectories("Doki\\Mods").Length == 0)
            {
                ActiveScriptModifierIndex = -1;
                return;
            }

            foreach (var directory in Directory.GetDirectories("Doki\\Mods"))
            {
                foreach (var file in Directory.GetFiles(directory))
                {
                    if (Path.GetExtension(file) == ".dll")
                    {
                        try
                        {
                            ConsoleUtils.Log("DokiModsManager", $"Trying to load Doki Mod: {Path.GetFileNameWithoutExtension(file)}");

                            var assembly = Assembly.LoadFrom(Path.GetFullPath(file));
                            var types = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(DokiMod)));

                            if (types.Count() == 0)
                            {
                                ConsoleUtils.Error("DokiModsManager", $"An error occurred while trying to load Doki Mod: {Path.GetFileNameWithoutExtension(file)} -> There was no DokiMod subclass.");
                                continue;
                            }

                            DokiMod mod = Activator.CreateInstance(types.First()) as DokiMod;

                            if (Directory.Exists($"{directory}\\Scripts") && Directory.GetFiles($"{directory}\\Scripts").Length > 0)
                            {
                                mod.ScriptsPath = $"{directory}\\Scripts";
                                mod.ModifiesContext = true;
                            }

                            if (Directory.Exists($"{directory}\\Assets") && Directory.GetFiles($"{directory}\\Assets").Length > 0)
                                mod.AssetsPath = $"{directory}\\Assets";

                            mod.OnLoad();
                            Mods.Add(mod);

                            ConsoleUtils.Log("DokiModsManager", $"Loaded {mod.Name} by {mod.Author} (version: {mod.Version}) successfully.");
                        }
                        catch (Exception e)
                        {
                            ConsoleUtils.Error("DokiModsManager", $"An error occurred while trying to load Doki Mod: {Path.GetFileNameWithoutExtension(file)}");
                            throw new Exception(e.Message);
                        }
                    }
                }
            }

            ConsoleUtils.Log("DokiModsManager", "Loaded Doki Mods.");
        }
    }
}
