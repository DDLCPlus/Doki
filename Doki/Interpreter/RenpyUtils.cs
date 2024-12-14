using Doki.Game;
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
using Dialogue = Doki.Game.Dialogue;

namespace Doki.Interpreter
{
    public static class RenpyUtils
    {
        public static void AddToHistory(string who, string what, string label = "ch0_main") =>
            Renpy.HistoryManager.HistoryLines.Enqueue(new RenpyHistoryEntry(who, what, label));

        public static void AddToHistory(string who, string what) =>
            Renpy.HistoryManager.HistoryLines.Enqueue(new RenpyHistoryEntry(who, what, Renpy.CurrentLabel));

        private static RenpyExecutionContext Context { get; set; }

        public static void SetContext(RenpyExecutionContext context) => Context = context;

        public static RenpyExecutionContext GetContext() => Context;

        public static List<Dialogue> CustomDialogue = new List<Dialogue>();

        public static List<RenpyShow> BaseShowContents = new List<RenpyShow>();

        public static List<RenpyShow> CustomShowContents = new List<RenpyShow>();

        public static List<int> CustomTextIDs = new List<int>();

        public static RenDisco.RenpyParser Parser { get; set; }

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

        public static RenpyBlock Translate(string label, List<RenpyCommand> commands)
        {
            List<Line> Lines = new List<Line>();

            foreach(RenpyCommand command in commands)
            {
                switch(command)
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
                    case RenDisco.Jump jump:
                        Lines.Add(new RenpyGoTo(jump.Label, false, $"jump {jump.Label}"));
                        break;
                    case RenDisco.Scene scene:
                        Lines.Add(new RenpyScene(scene.Raw));
                        break;
                    case RenDisco.PlayMusic playMusic:
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
                    case RenDisco.StopMusic stopMusic:
                        Lines.Add(new RenpyStop()
                        {
                            stop = new RenpyParser.Stop()
                            {
                                Channel = Channel.Music,
                                fadeout = (float)stopMusic.FadeOut,
                            }
                        });
                        break;
                    case RenDisco.Pause pause:
                        Lines.Add(new RenpyPause(pause.Raw, null));
                        break;
                    case RenDisco.Narration narration:
                        Lines.Add(Dialogue(label, narration.Text, false, false, "mc", false));
                        break;
                }
            }

            RenpyBlock renpyBlock = new RenpyBlock(label, true, RenpyBlockAttributes.None, Lines);

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
