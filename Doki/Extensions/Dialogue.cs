using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenpyParser;

namespace Doki.Extensions
{
    public class Dialogue
    {
        public int TextID { get; set; }

        public string Label { get; set; }
        public string Character { get; set; }
        public string Text { get; set; }

        public bool Glitch { get; set; }
        public bool SkipWait { get; set; }
        public bool FromPlayer { get; set; }

        public Line Line { get; set; }

        public Dialogue(string label, string character, string text, bool skipWait, bool glitch, bool fromPlayer = false, string command_type = "say")
        {
            TextID = text.GetHashCode();
            Character = character;
            Text = text;
            Glitch = glitch;
            SkipWait = skipWait;
            FromPlayer = fromPlayer;

            Line = Activator.CreateInstance(
                typeof(DialogueLine).Assembly.GetType("RenpyParser.RenpyDialogueLine"), new object[] { label, TextID, character, "", true, skipWait, false, 1, 0, 0, false, false, 0, new List<Tuple<int, float>>(), command_type }
            ) as Line;
        }
    }
}
