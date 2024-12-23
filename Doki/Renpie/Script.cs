using Doki.Extensions;
using Doki.Renpie.Parser;
using Doki.Renpie.RenDisco;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RenPyParser.Sprites.CompositeSpriteParser;
using UnityEngine;
using System.IO;
using SimpleExpressionEngine;

namespace Doki.Renpie
{
    public class Script
    {
        public List<Parser.Dialogue> Dialogue = [];
        public List<int> TextIDs = [];
        public List<RenpyDefinition> Definitions = [];
        public Dictionary<string, CharacterData> Characters = [];
        public Dictionary<object, Line> Jumps = [];
        public List<string> Sounds = [];
        public Dictionary<string, Tuple<BlockEntryPoint, RenpyBlock>> BlocksDict = []; //Block label -> Commands translated

        public Parser.Dialogue RetrieveLineFromText(int textID)
        {
            foreach (var line in Dialogue)
            {
                if (line.TextID == textID)
                    return line;
            }

            return null;
        }

        private RenpyStop ParseStopSequence(StopMusic stopMusic)
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

        private RenpyPlay ParsePlaySequence(PlayMusic playMusic)
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

        private RenpyShow ParseShowSequence(Renpie.RenDisco.Show show)
        { // Yeah Yeah I get it, parse the shit when the parser has already done it..
            string[] showArguments = show.Raw.Split(' ');
            RenpyShow retShow = new("show ")
            {
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

        private CharacterData ParseCharacterDefinition(string rawDefinition)
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

        public FixedCompositeSprite ParseFixedCompositeSprite(string input)
        {
            if (string.IsNullOrEmpty(input))
                ConsoleUtils.Error("ParseFixedCompositeSprite", new ArgumentException("Input cannot be null or empty."));

            if (!input.StartsWith("im.Composite"))
                ConsoleUtils.Error("ParseFixedCompositeSprite", new FormatException("Input must start with 'im.Composite'."));

            int startIndex = input.IndexOf("(") + 1;
            int endIndex = input.LastIndexOf(")");
            int height = 0;
            int width, offsetX, offsetY = -1;

            if (startIndex <= 0 || endIndex <= startIndex)
                ConsoleUtils.Error("ParseFixedCompositeSprite", new FormatException("Invalid format: parentheses mismatch or missing."));

            string content = input.Substring(startIndex, endIndex - startIndex);

            string[] parts = content.Split(',');

            if (parts.Length < 3 || (parts.Length - 2) % 3 != 0)
                ConsoleUtils.Error("ParseFixedCompositeSprite", new FormatException("Invalid format: insufficient parts or malformed data."));

            if (!int.TryParse(parts[0].Trim('(', ')', ' '), out width) || !int.TryParse(parts[1].Trim('(', ')', ' '), out height))
                ConsoleUtils.Error("ParseFixedCompositeSprite", new FormatException("Invalid format: size values must be integers."));

            Vector2Int size = new Vector2Int(width, height);

            int assetCount = (parts.Length - 2) / 3;

            Vector2Int[] offsets = new Vector2Int[assetCount];

            string[] assetPaths = new string[assetCount];

            for (int i = 0; i < assetCount; i++)
            {
                int baseIndex = 2 + i * 3;

                string offsetPartX = parts[baseIndex].Trim('(', ')', ' ');
                string offsetPartY = parts[baseIndex + 1].Trim('(', ')', ' ');

                if (!int.TryParse(offsetPartX, out offsetX) || !int.TryParse(offsetPartY, out offsetY))
                    ConsoleUtils.Error("ParseFixedCompositeSprite", new FormatException($"Invalid format: offset values must be integers for asset {i + 1}."));

                offsets[i] = new Vector2Int(offsetX, offsetY);

                string assetPath = parts[baseIndex + 2].Trim(' ', '"');

                if (!assetPath.StartsWith("gui/") && !assetPath.StartsWith("images/"))
                    assetPath = "images/" + assetPath;

                assetPaths[i] = assetPath;
            }

            return new FixedCompositeSprite
            {
                Size = size,
                Offsets = offsets,
                AssetPaths = assetPaths
            };
        }

        private Line HandleDialogue(string label, string text, bool allowSkip, string character_tag, string command_type = "say", bool glitch_text = false)
        {
            RenpyDefinition characterDefinition = Definitions.FirstOrDefault(x => x.Name == character_tag && x.Type == DefinitionType.Character);

            if (characterDefinition != null && !Characters.ContainsKey(character_tag))
                Characters.Add(character_tag, ParseCharacterDefinition(characterDefinition.Value));

            if (Characters.ContainsKey(character_tag))
                character_tag = Characters[character_tag].name;

            return MakeDialogueLine(label, text, allowSkip, character_tag, command_type, glitch_text);
        }

        private Line MakeDialogueLine(string label, string speech, bool skipWait = false, string who = "mc", string command_type = "say", bool glitch = false)
        {
            var line = new Parser.Dialogue(label, who, speech, skipWait, glitch, who == "mc" || who == "player", command_type);

            Dialogue.Add(line);
            TextIDs.Add(line.TextID);

            return line.Line;
        }

        private List<Line> HandleCondition(IfCondition ifCondition = null, ElifCondition elIfCondition = null)
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
            Jumps.Add(hardGoto, afterIf);
            Jumps.Add(lastGoto, afterIf);
            RetLines.Add(afterIf);

            return RetLines;
        }

        private List<Line> HandleIfStatement(int currentLine, List<RenpyCommand> commands, IfCondition condition)
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

        private RenpyBlock Translate(string label, List<RenpyCommand> commandsPassed)
        {
            try
            {
                ConsoleUtils.Log("Doki", "Translating label..");

                List<Line> Lines = [];
                List<RenpyCommand> commands = ((Label)commandsPassed.First()).Commands;

                foreach (RenpyCommand command in commands)
                {
                    int currentLine = commands.IndexOf(command);

                    switch (command)
                    {
                        case Renpie.RenDisco.Dialogue dialogue:
                            Lines.Add(HandleDialogue(label, dialogue.Text, false, dialogue.Character, "say", false));
                            break;
                        case Renpie.RenDisco.Hide hide:
                            Lines.Add(new RenpyHide()
                            {
                                hide = new RenpyParser.Hide(hide.Image, hide.Raw.Split(' ')[1] == "screen")
                            });
                            break;
                        case With with:
                            Lines.Add(new RenpyWith(with.Transition, SimpleExpressionEngine.Parser.Compile(with.Transition)));
                            break;
                        case Renpie.RenDisco.Show show:
                            Lines.Add(ParseShowSequence(show));
                            break;
                        case Jump jump:
                            Lines.Add(new RenpyGoTo(jump.Label, false, $"jump {jump.Label}"));
                            break;
                        case Call call:
                            Lines.Add(new RenpyGoTo(call.Label, true, $"call {call.Label}"));
                            break;
                        case IfCondition ifCondition:
                            Lines.AddRange(HandleIfStatement(currentLine, commands, ifCondition));
                            break;
                        case Renpie.RenDisco.Scene scene:
                            Lines.Add(new RenpyScene(scene.Raw));
                            break;
                        case PlayMusic playMusic:
                            Lines.Add(ParsePlaySequence(playMusic));
                            Sounds.Add(playMusic.File);
                            break;
                        case StopMusic stopMusic:
                            Lines.Add(ParseStopSequence(stopMusic));
                            break;
                        case Pause pause:
                            Lines.Add(new RenpyPause(pause.Raw, SimpleExpressionEngine.Parser.Compile(pause.Raw)));
                            break;
                        case Narration narration:
                            Lines.Add(MakeDialogueLine(label, narration.Text, false, "", "menu-with-caption", false));
                            break;
                        case Image image:
                            Definitions.Add(new RenpyDefinition(image.Name, image.Value.Replace("\"", ""), DefinitionType.Image));
                            break;
                        case Return _:
                            Lines.Add(new RenpyReturn());
                            break;
                        case Define define:
                            if (define.Raw.Contains("$"))
                                Lines.Add(new RenpyOneLinePython(define.Raw));
                            else
                                Definitions.Add(new RenpyDefinition(define.Name, define.Value.Replace("\"", ""), define.Raw.Contains("Character(") ? DefinitionType.Character : DefinitionType.Unknown));
                            break;
                    }
                }

                RenpyBlock renpyBlock = new(label)
                {
                    callParameters = [],
                    Contents = Lines
                };

                ConsoleUtils.Log("Doki", $"Label {label} translated.");

                return renpyBlock;
            }
            catch (Exception e)
            {
                ConsoleUtils.Error("RenpyUtils.Translate", e);

                return new RenpyBlock();
            }
        }

        private bool DoFromCommands(List<RenpyCommand> Commands)
        {
            try
            {
                if (Commands.Count() == 0)
                    return false;

                int[] beginningIndexes = Commands.Where(x => x.Type == "label").Select(x => Commands.IndexOf(x)).ToArray();
                if (beginningIndexes.Length == 0)
                    return false;

                for (int i = 0; i < beginningIndexes.Length; i++)
                {
                    int startIndex = beginningIndexes[i];
                    int endIndex = i + 1 < beginningIndexes.Length ? beginningIndexes[i + 1] : Commands.Count;
                    string label = ((Label)Commands[startIndex]).Name;

                    List<RenpyCommand> blockCommands = Commands.GetRange(startIndex, endIndex - startIndex);
                    BlockEntryPoint entryPoint = new(label);

                    RenpyBlock block = Translate(label, blockCommands);

                    var container = block.Contents;

                    foreach (var entry in Jumps)
                    {
                        switch (entry.Key)
                        {
                            case RenpyGoToLine goToLine:
                                goToLine.TargetLine = container.IndexOf(Jumps[goToLine]);
                                break;
                            case RenpyGoToLineUnless goToLineUnless:
                                goToLineUnless.TargetLine = container.IndexOf(Jumps[goToLineUnless]);
                                break;
                            case RenpyMenuInputEntry menuInputEntry:
                                menuInputEntry.gotoLineTarget = container.IndexOf(entry.Value);
                                break;
                        }
                    }

                    Jumps.Clear();

                    BlocksDict.Add(label, new Tuple<BlockEntryPoint, RenpyBlock>(entryPoint, block));
                }

                return true;
            }
            catch (Exception ex)
            {
                ConsoleUtils.Error("Script.Process", ex);
                return false;
            }
        }

        public bool Process(RenDisco.RenpyParser parser, string path) => DoFromCommands(parser.ParseFromFile(path));

        public bool Process(RenDisco.RenpyParser parser, byte[] contents) => DoFromCommands(parser.Parse(System.Text.Encoding.UTF8.GetString(contents)));
    }
}
