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

        public static void UnloadMods()
        {
            foreach (var mod in Mods)
            {
                try
                {
                    mod.OnUnload();
                }
                catch (Exception e)
                {
                    ConsoleUtils.Error("DokiModsManager", e, $"An error occurred while unloading mod: {mod.Name}");
                }
            }

            Mods.Clear();
            ActiveScriptModifierIndex = 0;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public static void LoadMods()
        {
            if (BootLoader.DontMod || BootLoader.RpaMod)
                return;

            bool IsReload = Mods.Count() > 0;

            ConsoleUtils.Log("DokiModsManager", $"{(IsReload ? "Rel" : "L")}oading Doki Mods...");

            if (IsReload)
                UnloadMods();

            if (Directory.GetDirectories("Doki\\Mods").Length == 0)
            {
                ActiveScriptModifierIndex = -1;
                return;
            }

            if (Directory.Exists("game") && Directory.GetFiles("game").Length > 0)
            {
                ConsoleUtils.Log("DotRPA", "Detected classic game modding method - kicking in dotRPA support");
                BootLoader.RpaMod = true;
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
                            //Load from stream so we prevent files from getting locked :3 - for reloading n shtuff
                            var assembly = Assembly.Load(File.ReadAllBytes(file));
                            var types = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(DokiMod)));

                            if (types.Count() == 0)
                            {
                                ConsoleUtils.Error("DokiModsManager", new InvalidOperationException($"An error occurred while trying to load Doki Mod: {Path.GetFileNameWithoutExtension(file)} -> There was no DokiMod subclass."));
                                continue;
                            }

                            DokiMod mod = Activator.CreateInstance(types.First()) as DokiMod;

                            ConsoleUtils.Log("DokiModsManager", $"Trying to load Doki Mod: {Path.GetFileNameWithoutExtension(file)}");

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
                            ConsoleUtils.Error("DokiModsManager", e, $"An error occurred while trying to load Doki Mod: {Path.GetFileNameWithoutExtension(file)}");
                        }
                    }
                }
            }

            ConsoleUtils.Log("DokiModsManager", $"{(IsReload ? "Re" : "")}loaded Doki Mods...");
        }
    }
}
