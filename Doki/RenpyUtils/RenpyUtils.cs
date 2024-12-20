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
using System.Text.RegularExpressions;
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
        public static Dictionary<Tuple<int, string>, RenpyDefinition> CustomVariables = [];
        public static List<RenpyDefinition> CustomDefinitions = [];
        public static Dictionary<string, CharacterData> Characters = [];
        public static Dictionary<object, Line> Jumps = [];
        public static List<string> Sounds = [];

        public static Dialogue RetrieveLineFromText(string text)
        {
            foreach (var line in CustomDialogue)
            {
                if (line.Text.ToLower() == text.ToLower())
                    return line;
            }

            return null;
        }

        public static CharacterData ParseCharacterDefinition(string rawDefinition)
        {
            CharacterData character = new();
            if (rawDefinition.Contains("DynamicCharacter("))
            {
                int startIndex = rawDefinition.IndexOf("DynamicCharacter(") + "DynamicCharacter(".Length;
                int endIndex = rawDefinition.LastIndexOf(")");

                string[] args = rawDefinition.Substring(startIndex, endIndex - startIndex).Split(',');

                foreach (var arg in args)
                {
                    var parts = arg.Split('=');

                    if (parts.Length == 1)
                        character.name = parts[0].Trim().Trim('\'');
                    else if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim().Trim('\'', '\"');

                        switch (key)
                        {
                            case "image":
                                character.image = value;
                                break;
                            case "what_prefix":
                                character.what_prefix = value;
                                break;
                            case "what_suffix":
                                character.what_suffix = value;
                                break;
                            case "ctc":
                                character.ctc = value;
                                break;
                            case "ctc_position":
                                character.ctc_position = value;
                                break;
                        }
                    }
                }
            }

            return character;
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
                        var transform = show.TransformName;

                        if (transform == "" && show.IsLayer)
                            transform = "resetlayer";

                        output += $"show{
                            (show.IsLayer ? $" layer {show.Name}" : $" {show.AssetName}") +
                            (show.As != "" ? $" as {show.As}" : "") +
                            (transform != "" ? $" at {show.TransformName}" : "") +
                            (show.HasBehind ? $" behind {show.Behind}" : "") +
                            (show.IsLayer ? $" onlayer {show.Layer}" : "") +
                            (show.HasZOrder ? $" zorder {show.ZOrder}" : "")}\n";
                        break;
                    case RenpyLoadImage loadImage:
                        output += $"image bg {loadImage.key} = \"{loadImage.fullImageDetails}\"\n";
                        break;
                    case RenpyHide hide:
                        output += $"{hide.HideData}\n";
                        break;
                    case RenpyPlay play:
                        output += $"{play.PlayData}\n";
                        break;
                    case RenpyPause pause:
                        output += $"{pause.PauseData}\n";
                        break;
                    case RenpyGoTo goTo:
                        output += (goTo.IsCall ? "call " : "jump ") +
                            (goTo.TargetLabel != "" ? goTo.TargetLabel : goTo.targetExpression.ToString()) +
                            (goTo.IsCall ? $"({goTo.callParameters.Join(p => p.expression.ToString())})" : "") + "\n";
                        break;
                    case RenpyStop stop:
                        output += $"{stop.StopData}\n";
                        break;
                    case RenpyQueue queue:
                        output += $"{queue.QueueData}\n";
                        break;
                    case RenpyNOP nop:
                        output += "pass\n";
                        break;
                    case RenpyReturn ret:
                        output += "return\n";
                        break;
                    case RenpySize size:
                        output += $"{size.SizeData}\n";
                        break;
                    case RenpyEasedTransform renpyEasedTransform:
                        output += $"{renpyEasedTransform.TransformCommand}\n";
                        break;
                    case RenpyGoToLineUnless renpyGoToLineUnless:
                        output += $"goto {renpyGoToLineUnless.TargetLine} unless {renpyGoToLineUnless.ConditionText}\n";
                        break;
                    case RenpyImmediateTransform renpyImmediateTransform:
                        output += $"{renpyImmediateTransform.TransformCommand}\n";
                        break;
                    case RenpyGoToLine renpyGoToLine:
                        output += $"goto {renpyGoToLine.TargetLine}\n";
                        break;
                    case RenpyForkGoToLine renpyForkGoToLine:
                        output += $"fork goto {renpyForkGoToLine.TargetLine}\n";
                        break;
                }
            }

            output += "\nEND\n";
            ConsoleUtils.Debug("Doki", output);
        }

        private static List<Line> HandleCondition(IfCondition ifCondition = null, ElifCondition elIfCondition = null)
        {
            List<Line> RetLines = [];
            var afterIf = new RenpyNOP();
            RenpyGoToLineUnless lastGoto = null;

            if (ifCondition != null)
            {
                var compiledCondition = SimpleExpressionEngine.Parser.Compile(ifCondition.Condition);
                var gotoStmt = new RenpyGoToLineUnless(ifCondition.Condition, -1) { CompiledExpression = compiledCondition };

                RetLines.Add(gotoStmt);
                lastGoto = gotoStmt;
            }

            if (elIfCondition != null)
            {
                var compiledCondition = SimpleExpressionEngine.Parser.Compile(elIfCondition.Condition);
                var gotoStmt = new RenpyGoToLineUnless(elIfCondition.Condition, -1) { CompiledExpression = compiledCondition };

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
            // We need to get the elif's (if any) related to this if statement

            List<ElifCondition> elifConditions = [];
            ElseCondition elseCondition = null; // And check if there's an else statement

            for (int i = currentLine + 1; i < commands.Count; i++)
            {
                if (commands[i] is ElifCondition elifCondition)
                    elifConditions.Add(elifCondition);
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

            // Now that we have all the possible conditions in this block, we need to handle what to do after the statement

            return retLines;
        }

        private static RenpyStop ParseStopSequence(RenDisco.StopMusic stopMusic)
        {
            string[] stopArguments = stopMusic.Raw.Split(' ');
            RenpyStop retStop = new() { stop = new Stop() };

            if (stopArguments.Length > 2)
                retStop.stop.fadeout = (float)stopMusic.FadeOut;

            retStop.stop.Channel = stopArguments[1] switch
            {
                "musicpoem" => Channel.MusicPoem,
                "sound" => Channel.Sound,
                _ => Channel.Music,
            };
            return retStop;
        }

        private static RenpyPlay ParsePlaySequence(RenDisco.PlayMusic playMusic)
        {
            string[] playArguments = playMusic.Raw.Split(' ');
            RenpyPlay retPlay = new() { play = new RenpyParser.Play() { Asset = playMusic.File } };

            if (playArguments.Length > 3) //Handle fadein
                retPlay.play.fadein = (float)playMusic.FadeIn;

            retPlay.play.Channel = playArguments[1] switch
            {
                "musicpoem" => Channel.MusicPoem,
                "sound" => Channel.Sound,
                _ => Channel.Music,
            };
            return retPlay;
        }

        private static RenpyShow ParseShowSequence(RenDisco.Show show)
        { // Yeah Yeah I get it, parse the shit when the parser has already done it..
            string[] showArguments = show.Raw.Split(' ');
            RenpyShow retShow = new("show ") {
                show = {
                    IsImage = true,
                    ImageName = showArguments[1],
                    Variant = showArguments.Length > 2 ? showArguments[2] : "",
                    TransformName = "",
                    TransformCallParameters = [],
                    Layer = "master",
                    As = "",
                    HasZOrder = false
                }
            };

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
                else if (arg == "at")
                {
                    if (i + 1 < showArguments.Length)
                        retShow.show.TransformName = showArguments[++i];
                }
                else if (arg == "onlayer")
                {
                    if (i + 1 < showArguments.Length)
                        retShow.show.Layer = showArguments[++i];
                }
                else if (arg == "as")
                {
                    if (i + 1 < showArguments.Length)
                        retShow.show.As = showArguments[++i];
                }
            }
            retShow.ShowData = show.Raw;
            
            return retShow;
        }

        public static Line HandleDialogue(string label, string text, bool includeQuotes, bool allowSkip, string character_tag, string command_type = "say", bool glitch_text = false)
        {
            RenpyDefinition characterDefinition = CustomDefinitions.FirstOrDefault(x => x.Name == character_tag && x.Type == DefinitionType.Character);

            if (characterDefinition != null && !Characters.ContainsKey(character_tag))
                Characters.Add(character_tag, ParseCharacterDefinition(characterDefinition.Value));

            if (Characters.ContainsKey(character_tag))
                character_tag = Characters[character_tag].name;

            return MakeDialogueLine(label, text, includeQuotes, allowSkip, character_tag, command_type, glitch_text);
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
                        Lines.Add(HandleDialogue(label, dialogue.Text, true, false, dialogue.Character, "say", false));
                        break;
                    case RenDisco.Hide hide:
                        Lines.Add(new RenpyHide()
                        {
                            hide = new RenpyParser.Hide(hide.Image, hide.Raw.Split(' ')[1] == "screen")
                        });
                        break;
                    case RenDisco.With with:
                        Lines.Add(new RenpyWith(with.Transition, SimpleExpressionEngine.Parser.Compile(with.Transition)));
                        break;
                    case RenDisco.Show show:
                        Lines.Add(ParseShowSequence(show));
                        break;
                    case Jump jump:
                        Lines.Add(new RenpyGoTo(jump.Label, false, $"jump {jump.Label}"));
                        break;
                    case Call call:
                        Lines.Add(new RenpyGoTo(call.Label, true, $"call {call.Label}"));
                        break;
                    case RenDisco.IfCondition ifCondition:
                        Lines.AddRange(HandleIfStatement(currentLine, commands, ifCondition));
                        break;
                    case RenDisco.Scene scene:
                        Lines.Add(new RenpyScene(scene.Raw));
                        break;
                    case PlayMusic playMusic:
                        Lines.Add(ParsePlaySequence(playMusic));
                        RenpyUtils.Sounds.Add(playMusic.File);
                        break;
                    case StopMusic stopMusic:
                        Lines.Add(ParseStopSequence(stopMusic));
                        break;
                    case Pause pause:
                        Lines.Add(new RenpyPause(pause.Raw, SimpleExpressionEngine.Parser.Compile(pause.Raw)));
                        break;
                    case Narration narration:
                        Lines.Add(MakeDialogueLine(label, narration.Text, false, false, "", "menu-with-caption", false));
                        break;
                    case RenDisco.Image image:
                        CustomDefinitions.Add(new RenpyDefinition(image.Name, image.Value.Replace("\"", ""), DefinitionType.Image));
                        break;
                    case RenDisco.Return _:
                        Lines.Add(new RenpyReturn());
                        break;
                    case RenDisco.Define define:
                        if (define.Raw.Contains("$"))
                            CustomVariables.Add(new Tuple<int, string>(commands.IndexOf(command), label), new RenpyDefinition(define.Name, define.Value.Replace("\"", ""), DefinitionType.Variable));
                        else if (define.Raw.Contains("DynamicCharacter("))
                            CustomDefinitions.Add(new RenpyDefinition(define.Name, define.Value.Replace("\"", ""), DefinitionType.Character));
                        else
                            CustomDefinitions.Add(new RenpyDefinition(define.Name, define.Value.Replace("\"", ""), DefinitionType.Audio)); //fucking handle other cases idk
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

        public static Line MakeDialogueLine(string label, string what, bool quotes = true, bool skipWait = false, string who = "mc", string command_type = "say", bool glitch = false)
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
