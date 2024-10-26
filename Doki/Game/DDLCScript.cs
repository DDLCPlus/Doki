using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Doki.Utils;
using Doki.Mods;

namespace Doki.Game
{
    public class DDLCScript
    {
        public string Label { get; set; } //Mod label!

        public string Path { get; set; } //Script Path!

        public DokiMod ModContext { get; set; }

        private List<string> ScriptLines = new List<string>();

        public List<string> GetScriptContents()
        {
            return ScriptLines;
        }

        public DDLCScript(DokiMod ModContext, string Path, string Label = "example")
        {
            this.ModContext = ModContext;
            this.Label = Label;
            this.Path = ModContext.WorkingDirectory + "\\" + ModContext.ScriptsDirectory + "\\" + Path;

            if (File.Exists(this.Path))
                ScriptLines = File.ReadAllLines(this.Path).ToList();
        }
    }
}
