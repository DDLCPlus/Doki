using Doki.Extensions;
using HarmonyLib;
using RenDisco;
using RenpyParser;
using RenPyParser;
using RenPyParser.Transforms;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static List<Dialogue> CustomDialogue = [];
        public static List<int> CustomTextIDs = [];
        public static Dictionary<Tuple<int, string>, RenpyDefinition> CustomDefinitions = [];
        public static Dictionary<object, Line> Jumps = [];
        public static Dictionary<string, string> Sounds = [];

        public static Dialogue RetrieveLineFromText(string text)
        {
            foreach (var line in CustomDialogue)
            {
                if (line.Text.ToLower() == text.ToLower())
                    return line;
            }

            return null;
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

        public static void DumpBlock(RenpyBlock block)
        {
            string output = $"label {block.Label}:\n";

            foreach (var line in block.Contents)
            {
                switch (line)
                {
                    case RenpyShow renpyShow:
                        var show = renpyShow.show;
                        var toLog = "show";

                        if (show.IsLayer)
                        {
                            toLog += " layer " + show.Name;
                        }
                        else
                        {
                            toLog += " " + show.AssetName;
                        }

                        if (show.As != "")
                        {
                            toLog += " as " + show.As;
                        }

                        var transform = show.TransformName;

                        if (transform == "" && show.IsLayer)
                        {
                            transform = "resetlayer";
                        }

                        if (transform != "")
                        {
                            toLog += " at " + show.TransformName;
                        }

                        if (show.HasBehind)
                        {
                            toLog += " behind " + show.Behind;
                        }
                        if (!show.IsLayer)
                        {
                            toLog += " onlayer " + show.Layer;
                        }
                        if (show.HasZOrder)
                        {
                            toLog += " zorder " + show.ZOrder;
                        }

                        output += toLog + "\n";
                        break;
                    case RenpyLoadImage loadImage:
                        output += $"image bg {loadImage.key} = \"{loadImage.fullImageDetails}\"\n";
                        break;
                    case RenpyHide hide:
                        output += hide.HideData + "\n";
                        break;
                    case RenpyPlay play:
                        output += play.PlayData + "\n";
                        break;
                    case RenpyPause pause:
                        output += pause.PauseData + "\n";
                        break;
                    case RenpyGoTo goTo:
                        var gotoDump = goTo.IsCall ? "call " : "jump ";

                        if (goTo.TargetLabel != "")
                            gotoDump += goTo.TargetLabel;
                        else
                            gotoDump += goTo.targetExpression.ToString();

                        if (goTo.IsCall)
                        {
                            gotoDump += "(" + goTo.callParameters.Join(p => p.expression.ToString()) + ")";
                        }

                        output += gotoDump + "\n";
                        break;
                    case RenpyStop stop:
                        output += stop.StopData + "\n";
                        break;
                    case RenpyQueue queue:
                        output += queue.QueueData + "\n";
                        break;
                    case RenpyNOP nop:
                        output += "pass\n";
                        break;
                    case RenpyReturn ret:
                        output += "return\n";
                        break;
                    case RenpySize size:
                        output += size.SizeData + "\n";
                        break;
                    case RenpyEasedTransform renpyEasedTransform:
                        output += renpyEasedTransform.TransformCommand + "\n";
                        break;
                    case RenpyGoToLineUnless renpyGoToLineUnless:
                        output += "goto " + renpyGoToLineUnless.TargetLine + " unless " + renpyGoToLineUnless.ConditionText + "\n";
                        break;
                    case RenpyImmediateTransform renpyImmediateTransform:
                        output += renpyImmediateTransform.TransformCommand + "\n";
                        break;
                    case RenpyGoToLine renpyGoToLine:
                        output += "goto " + renpyGoToLine.TargetLine + "\n";
                        break;
                    case RenpyForkGoToLine renpyForkGoToLine:
                        output += $"fork goto {renpyForkGoToLine.TargetLine}\n";
                        break;
                }
            }

            output += "\nEND\n";

            ConsoleUtils.Log("Doki", output);
        }

        private static List<Line> HandleCondition(IfCondition ifCondition = null, ElifCondition elIfCondition = null)
        {
            List<Line> RetLines = [];

            var afterIf = new RenpyNOP();

            RenpyGoToLineUnless lastGoto = null;

            if (ifCondition != null)
            {
                var compiledCondition = SimpleExpressionEngine.Parser.Compile(ifCondition.Condition);

                var gotoStmt = new RenpyGoToLineUnless(ifCondition.Condition, -1)
                {
                    CompiledExpression = compiledCondition
                };

                RetLines.Add(gotoStmt);
                lastGoto = gotoStmt;
            }

            if (elIfCondition != null)
            {
                var compiledCondition = SimpleExpressionEngine.Parser.Compile(elIfCondition.Condition);

                var gotoStmt = new RenpyGoToLineUnless(elIfCondition.Condition, -1)
                {
                    CompiledExpression = compiledCondition
                };

                RetLines.Add(gotoStmt);
                lastGoto = gotoStmt;
            }

            var hardGoto = new RenpyGoToLine(-1);

            RenpyUtils.Jumps.Add(hardGoto, afterIf);
            RenpyUtils.Jumps.Add(lastGoto, afterIf);

            RetLines.Add(afterIf);

            return RetLines;
        }

        private static List<Line> HandleIfStatement(int currentLine, List<RenpyCommand> commands, IfCondition condition)
        {
            //We need to get the elif's (if any) related to this if statement

            List<ElifCondition> elifConditions = [];

            ElseCondition elseCondition = null; //And check if there's an else statement

            for (int i = currentLine + 1; i < commands.Count; i++)
            {
                if (commands[i] is ElifCondition elifCondition)
                {
                    elifConditions.Add(elifCondition);
                }
                else if (commands[i] is ElseCondition eCondition)
                {
                    elseCondition = eCondition;
                    break; //Idk what to do with this just yet
                }
                else
                    break;
            }

            List<Line> retLines = [.. HandleCondition(condition, null)];

            foreach (var elifCondition in elifConditions)
                retLines.AddRange(HandleCondition(null, elifCondition));

            //Now that we have all the possible conditions in this block, we need to handle what to do after the statement

            return retLines;
        }

        private static RenpyStop ParseStopSequence(RenDisco.StopMusic stopMusic)
        {
            string[] stopArguments = stopMusic.Raw.Split(' ');

            RenpyStop retStop = new()
            {
                stop = new Stop()
            };

            if (stopArguments.Length > 2)
                retStop.stop.fadeout = (float)stopMusic.FadeOut;

            if (stopArguments[1] == "musicpoem")
                retStop.stop.Channel = Channel.MusicPoem;
            else if (stopArguments[1] == "sound")
                retStop.stop.Channel = Channel.Sound;
            else
                retStop.stop.Channel = Channel.Music;

            return retStop;
        }

        private static RenpyPlay ParsePlaySequence(RenDisco.PlayMusic playMusic)
        {
            string[] playArguments = playMusic.Raw.Split(' ');

            RenpyPlay retPlay = new()
            {
                play = new RenpyParser.Play()
            };

            retPlay.play.Asset = playMusic.File;

            if (playArguments.Length > 3) //Handle fadein
                retPlay.play.fadein = (float)playMusic.FadeIn;


            if (playArguments[1] == "musicpoem")
                retPlay.play.Channel = Channel.MusicPoem;
            else if (playArguments[1] == "sound")
                retPlay.play.Channel = Channel.Sound;
            else
                retPlay.play.Channel = Channel.Music;

            return retPlay;
        }

        private static RenpyShow ParseShowSequence(RenDisco.Show show)
        {
            string[] showArguments = show.Raw.Split(' ');

            RenpyShow retShow = new("show ");

            retShow.show.IsImage = true;
            retShow.show.ImageName = showArguments[1];
            retShow.show.Variant = showArguments.Length > 2 ? showArguments[2] : "";
            retShow.show.TransformName = "";

            retShow.show.TransformCallParameters = [];

            for (int i = 0; i < showArguments.Length; i++)
            {
                string arg = showArguments[i];

                if (arg.StartsWith("zorder"))
                {
                    if (int.TryParse(arg.Substring("zorder".Length), out int zorder))
                    {
                        retShow.show.ZOrder = zorder;
                        retShow.show.HasZOrder = true;
                    }
                }
                else if (arg.StartsWith("at"))
                {
                    string position = arg.Substring("at".Length).Trim();

                    if (!string.IsNullOrEmpty(position))
                    {
                        retShow.show.TransformName = position;
                    }
                }
            }

            retShow.ShowData = show.Raw;

            return retShow;
        }


        public static RenpyBlock Translate(string label, List<RenpyCommand> commandsPassed)
        {
            ConsoleUtils.Log("Doki", "Translating label..");

            List<Line> Lines = [];
            List<RenpyCommand> commands = ((Label)commandsPassed.First()).Commands;

            foreach (RenpyCommand command in commands)
            {
                int currentLine = commands.IndexOf(command);

                switch (command)
                {
                    case RenDisco.Dialogue dialogue:
                        Lines.Add(Dialogue(label, dialogue.Text, true, false, dialogue.Character, "say", false));
                        break;
                    case RenDisco.Hide hide:
                        Lines.Add(new RenpyHide()
                        {
                            hide = new RenpyParser.Hide(hide.Image, hide.Raw.Split(' ')[1] == "screen")
                        });
                        break;
                    case RenDisco.Show show:
                        Lines.Add(ParseShowSequence(show));
                        break;
                    case Jump jump:
                        Lines.Add(new RenpyGoTo(jump.Label, false, $"jump {jump.Label}"));
                        break;
                    case RenDisco.IfCondition ifCondition:
                        Lines.AddRange(HandleIfStatement(currentLine, commands, ifCondition));
                        break;
                    case RenDisco.Scene scene:
                        Lines.Add(new RenpyScene(scene.Raw));
                        break;
                    case PlayMusic playMusic:
                        Lines.Add(ParsePlaySequence(playMusic));
                        break;
                    case StopMusic stopMusic:
                        Lines.Add(ParseStopSequence(stopMusic));
                        break;
                    case Pause pause:
                        Lines.Add(new RenpyPause(pause.Raw, SimpleExpressionEngine.Parser.Compile(pause.Raw)));
                        break;
                    case Narration narration:
                        Lines.Add(Dialogue(label, narration.Text, false, false, "", "menu-with-caption", false));
                        break;
                    case RenDisco.Return _:
                        Lines.Add(new RenpyReturn());
                        break;
                    case RenDisco.Define define:
                        CustomDefinitions.Add(new Tuple<int, string>(commands.IndexOf(command), label), new RenpyDefinition(define.Name, define.Value.Replace("\"", "")));
                        break;
                }
            }

            RenpyBlock renpyBlock = new(label)
            {
                callParameters = [],
                Contents = Lines
            };

            return renpyBlock;
        }

        public static Line Dialogue(string label, string what, bool quotes = true, bool skipWait = false, string who = "mc", string command_type = "say", bool glitch = false)
        {
            string text = what;

            if (quotes)
                text = $"\"{text}\"";

            var line = new Dialogue(label, who, text, skipWait, glitch, who == "mc" || who == "player", command_type);

            CustomDialogue.Add(line);
            CustomTextIDs.Add(line.TextID);

            return line.Line;
        }
    }
}
