using Doki.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Renpie
{
    public class ArchiveFile
    {
        public byte[] Contents { get; set; }

        public string FilePath { get; set; }

        public string Parent { get; set; }

        public string Name { get; set; }

        public string FileNameExt { get; set; }

        public bool IsScript = false;

        public bool IsAsset = false;

        public ArchiveFile(string parent, byte[] contents, string name)
        {
            Parent = parent;
            Contents = contents;
            FilePath = name;
            FileNameExt = Path.GetFileName(name);
            Name = Path.GetFileNameWithoutExtension(name);
            IsScript = Path.GetExtension(FilePath) == ".rpyc" || Path.GetExtension(FilePath) == ".rpy";
            IsAsset = !IsScript;
        }

        public void Export(string outPath)
        {
            if (Contents.Length == 0)
                return;

            try
            {
                File.WriteAllBytes(outPath, Contents);
            }
            catch(Exception ex)
            {
                ConsoleUtils.Error("ArchiveFile.Export", ex);
            }
        }

        public bool Process()
        {
            return IsScript ? ScriptsHandler.ProcessFromBytes(Path.GetExtension(FilePath) == ".rpyc", Contents) : AssetUtils.SetupFromArchiveFile(Parent, Name, FilePath, Contents);
        }
    }
}
