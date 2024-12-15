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

        public RenpyDialogueLine Line { get; set; }

        public Dialogue(string label, string character, string text, bool skipWait, bool glitch, bool fromPlayer = false)
        {
            TextID = text.GetHashCode();
            Character = character;
            Text = text;
            Glitch = glitch;
            SkipWait = skipWait;
            FromPlayer = fromPlayer;
            Line = new RenpyDialogueLine(label, TextID, character, "", true, skipWait, false, 1, 0, 0, false, false, 0, new List<Tuple<int, float>>());

            ((DialogueLine)Line.GetPrivateField("m_DialogueLine")).Text = text;
        }
    }
}
