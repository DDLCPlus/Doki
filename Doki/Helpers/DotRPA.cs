using Doki.Extensions;
using Doki.Mods;
using Doki.Renpie;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Doki.Helpers
{
    public static class DotRPA
    {
        private static Dictionary<string, Archive> GetArchives()
        {
            Dictionary<string, Archive> archives = new Dictionary<string, Archive>();

            foreach(var file in Directory.GetFiles("game"))
            {
                if (Path.GetExtension(file) != ".rpa")
                    continue;

                var archive = new Archive(file);

                if (!archive.Valid)
                    continue;

                archives.Add(Path.GetFileNameWithoutExtension(file), archive);
            }

            return archives;
        }

        private static bool ProcessArchiveFile(string name, ArchiveFile archiveFile)
        {
            ConsoleUtils.Log("DotRPA.Process", $"Processing {name}...");

            return archiveFile.Process();
        }

        private static bool ProcessArchive(string name, Archive archive)
        {
            int successCount = 0;

            if (!archive.Valid)
                return false;

            ConsoleUtils.Log("DotRPA", $"Handling {name}.. inside are {archive.Files.Count} file(s)");

            foreach(var file in archive.Files)
            {
                if (ProcessArchiveFile(file.Key, file.Value))
                    successCount++;
            }

            return successCount == archive.Files.Count();
        }

        public static bool Init()
        {
            DokiModsManager.ActiveScriptModifierIndex = 0;
            DokiModsManager.Mods.Add(new DokiMod()
            {
                ModifiesContext = true,
                ScriptsPath = "RPA-MOD",
                AssetsPath = "RPA-MOD",
                ModBundles = new List<UnityEngine.AssetBundle>(),
                Postfixes = new Dictionary<System.Reflection.MethodBase, HarmonyLib.HarmonyMethod>(),
                Prefixes = new Dictionary<System.Reflection.MethodBase, HarmonyLib.HarmonyMethod>()
            });

            ConsoleUtils.Log("DotRPA", $"Initiated RPA-MOD proxy");

            int successCount = 0;

            var archives = GetArchives();

            //if (archives.Count() < 4) //scripts.rpa, images.rpa, audio.rpa, fonts.rpa (base game shit)
            //    return false;

            ConsoleUtils.Log("DotRPA", $"Handling {archives.Count()} .rpa files...");

            foreach (var archive in archives)
            {
                if (ProcessArchive(archive.Key, archive.Value))
                    successCount++;
            }

            return successCount == archives.Count();
        }
    }
}
