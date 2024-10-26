using Doki.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Mods
{
    public static class DokiModsManager
    {
        public static List<DokiMod> Mods = new List<DokiMod>();

        public static void LoadMods()
        {
            ConsoleUtils.Log("Loading Doki Mods..");

            foreach (var file in Directory.GetFiles("Doki\\Mods"))
            {
                if (Path.GetExtension(file) == ".dll")
                {
                    try
                    {
                        ConsoleUtils.Log($"Trying to load Doki Mod: {Path.GetFileNameWithoutExtension(file)}");

                        var assembly = Assembly.LoadFrom(Path.GetFullPath(file));

                        var types = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(DokiMod)));

                        if (types.Count() == 0)
                        {
                            ConsoleUtils.Log($"An error occurred while trying to load Doki Mod: {Path.GetFileNameWithoutExtension(file)} -> There was no DokiMod subclass.");
                        }

                        DokiMod mod = Activator.CreateInstance(types.First()) as DokiMod;

                        mod.OnLoad();

                        Mods.Add(mod);

                        ConsoleUtils.Log($"Loaded {mod.Name} by {mod.Author} (version: {mod.Version}) successfully.");
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        ConsoleUtils.Log($"An error occurred while trying to load Doki Mod: {Path.GetFileNameWithoutExtension(file)}");
                    }
                }
            }

            ConsoleUtils.Log("Loaded Doki Mods.");
        }
    }
}
