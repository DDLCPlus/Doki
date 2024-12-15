using Doki.Mods;
using Doki.Utils;
using RenDisco;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using SimpleExpressionEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Dialogue = Doki.Extensions.Dialogue;

namespace Doki.RenpyUtils
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

    public static class RenpyUtils
    {
        private static RenpyExecutionContext Context { get; set; }

        public static void SetContext(RenpyExecutionContext context) => Context = context;

        public static RenpyExecutionContext GetContext() => Context;

        public static List<Dialogue> CustomDialogue = new List<Dialogue>();

        public static List<RenpyShow> BaseShowContents = new List<RenpyShow>();

        public static List<RenpyShow> CustomShowContents = new List<RenpyShow>();

        public static List<int> CustomTextIDs = new List<int>();

        public static RenpyScriptExecutionContext ScriptExecutionContext { get; set; }

        public static Dialogue RetrieveLineFromText(string text)
        {
            foreach (var line in CustomDialogue)
            {
                if (line.Text.ToLower() == text.ToLower())
                    return line;
            }

            return null;
        }

        public static RenpyBlock Translate(string label, List<RenpyCommand> commandsPassed)
        {
            List<Line> Lines = new List<Line>();

            List<RenpyCommand> commands = ((Label)commandsPassed.First()).Commands;

            foreach (RenpyCommand command in commands)
            {
                switch (command)
                {
                    case RenDisco.Dialogue dialogue:
                        Lines.Add(Dialogue(label, dialogue.Text, true, false, dialogue.Character, false));
                        break;
                    case RenDisco.Hide hide:
                        Lines.Add(new RenpyHide()
                        {
                            hide = new RenpyParser.Hide(hide.Image, false, false),
                            HideData = hide.Raw
                        });
                        break;
                    case RenDisco.Show show:
                        Lines.Add(new RenpyShow(show.Raw));
                        break;
                    case Jump jump:
                        Lines.Add(new RenpyGoTo(jump.Label, false, $"jump {jump.Label}"));
                        break;
                    case RenDisco.Scene scene:
                        Lines.Add(new RenpyScene(scene.Raw));
                        break;
                    case PlayMusic playMusic:
                        Lines.Add(new RenpyPlay()
                        {
                            play = new RenpyParser.Play()
                            {
                                Asset = playMusic.File,
                                Channel = Channel.Music,
                                fadein = (float)playMusic.FadeIn
                            }
                        });
                        break;
                    case StopMusic stopMusic:
                        Lines.Add(new RenpyStop()
                        {
                            stop = new Stop()
                            {
                                Channel = Channel.Music,
                                fadeout = (float)stopMusic.FadeOut,
                            }
                        });
                        break;
                    case Pause pause:
                        Lines.Add(new RenpyPause(pause.Raw, null));
                        break;
                    case Narration narration:
                        Lines.Add(Dialogue(label, narration.Text, false, false, "mc", false));
                        break;
                    case RenDisco.Return _:
                        Lines.Add(new RenpyReturn());
                        break;
                }
            }

            RenpyBlock renpyBlock = new RenpyBlock(label);

            renpyBlock.callParameters = [];
            renpyBlock.Contents = Lines;

            return renpyBlock;
        }

        public static Dialogue RetrieveLineFromText(int textID)
        {
            foreach (var line in CustomDialogue)
            {
                if (line.TextID == textID)
                    return line;
            }

            return null;
        }

        public static RenpyShow Show(string data)
        {
            foreach (var show in BaseShowContents)
            {
                if (show.ShowData == data)
                    return show;
            }

            foreach (var customshow in CustomShowContents)
            {
                if (customshow.ShowData == data)
                    return customshow;
            }

            return null;
        }

        public static Line Dialogue(string label, string what, bool quotes = true, bool skipWait = false, string who = "mc", bool glitch = false)
        {
            string text = what;

            if (quotes)
                text = $"\"{text}\"";

            var line = new Dialogue(label, who, text, skipWait, glitch, who == "mc" || who == "player");

            CustomDialogue.Add(line);
            CustomTextIDs.Add(line.TextID);

            return line.Line;
        }
    }
}
