using Doki.Mods;
using Doki.Utils;
using RenpyParser;
using RenPyParser;
using RenPyParser.Transforms;
using RenPyParser.VGPrompter.DataHolders;
using RenPyParser.VGPrompter.Script.Internal;
using SimpleExpressionEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Doki.Interpreter
{
    public static class Interpreter
    {
        private static string WorkingLabel = "";

        public static Dictionary<string, string> CharacterNameMappings = new Dictionary<string, string>();

        public static List<PurePython> PythonStatements = new List<PurePython>();

        public static List<string> JumpStatements = new List<string>();

        public static BlockConstructor WorkingBlock = null;

        public static List<Tuple<BlockEntryPoint, RenpyBlock>> AssetBlocks = new List<Tuple<BlockEntryPoint, RenpyBlock>>();

        public static Dictionary<string, string> FakePathsToRealPaths = new Dictionary<string, string>();

        public static DokiMod ModContext = null;

        private static Dictionary<string, string> WorkingResolveAssets = new Dictionary<string, string>();

        public static void Reset()
        {
            WorkingLabel = "";
            CharacterNameMappings.Clear();
            PythonStatements.Clear();
            JumpStatements.Clear();
            WorkingResolveAssets.Clear();
            AssetBlocks.Clear();
            FakePathsToRealPaths.Clear();
            WorkingBlock = null;
            ModContext = null;
        }

        public static void SetEnv(string label, DokiMod modContext = null)
        {
            WorkingLabel = label;

            if (modContext != null)
                ModContext = modContext;

            WorkingBlock = new BlockConstructor(label);
        }

        public static RenpyReturn Return()
        {
            return new RenpyReturn();
        }

        public static RenpyNOP NOP()
        {
            return new RenpyNOP();
        }

        public static RenpyShow Show(string data)
        {
            return new RenpyShow(data);
        }

        public static RenpyStop Stop(Channel channel = Channel.Music, float fadeout = 2.0f)
        {
            return new RenpyStop()
            {
                stop = new Stop()
                {
                    Channel = channel,
                    fadeout = fadeout,
                }
            };
        }

        public static RenpyScene Scene(string asset, bool background = true)
        {
            string sceneData = "scene ";

            if (background)
                sceneData += "bg ";

            sceneData += asset;

            return new RenpyScene(sceneData);
        }

        public static RenpyWith With(string data)
        {
            return new RenpyWith(data, new CompiledExpression()
            {
                constantFloats = new List<float>(),
                constantObjects = new List<object>(),
                constantStrings = new List<string>()
                {
                    data
                },
                instructions = new List<CompiledInstruction>()
                {
                    new CompiledInstruction(InstructionType.LoadVariable, 0)
                }
            });
        }

        public static RenpyPlay Play(string audio, Channel channel = Channel.Music, float fadeout = 0f, float fadein = 2.0f)
        {
            return new RenpyPlay()
            {
                play = new Play()
                {
                    Channel = channel,
                    fadeout = fadeout,
                    fadein = fadein,
                    Asset = audio
                }
            };
        }

        public static DataValue RunExpression(string expression, RenpyExecutionContext context)
        {
            try
            {
                CompiledExpression compiledExpression = new CompiledExpression();

                SimpleExpressionEngine.Parser.Parse(new Tokenizer(new StringReader(expression))).Compile(compiledExpression);

                return ExpressionRuntime.Execute(compiledExpression, context);
            }
            catch { }

            return default;
        }

        public static Tuple<RenpyObjectType, object> GetObjectAndType(Line line)
        {
            try
            {
                string rawType = line.GetType().ToString();

                rawType = rawType.Replace("RenpyParser.", ""); //remove RenpyParser.

                switch (rawType) 
                {
                    default:
                        return default;
                    case "RenpyDialogueLine":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Dialog, (RenpyDialogueLine)line);
                    case "RenpyShow":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Show, (RenpyShow)line);
                    case "RenpyHide":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Hide, (RenpyHide)line);
                    case "RenpyWindow":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Window, (RenpyWindow)line);
                    case "RenpyScene":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Scene, (RenpyScene)line);
                    case "RenpyWith":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.With, (RenpyWith)line);
                    case "RenpyPlay":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Play, (RenpyPlay)line);
                    case "RenpyStop":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Stop, (RenpyStop)line);
                    case "RenpyQueue":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Queue, (RenpyQueue)line);
                    case "RenpyFunction":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Function, (RenpyFunction)line);
                    case "RenpyOneLinePython":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.OneLinePython, (RenpyOneLinePython)line);
                    case "RenpyInlinePython":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.InlinePython, (RenpyInlinePython)line);
                    case "RenpyLabelEntryPoint":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.LabelEntryPoint, (RenpyLabelEntryPoint)line);
                    case "RenpyNestedLabel":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.NestedLabel, Convert.ChangeType(line, Type.GetType("RenpyParser.RenpyNestedLabel")));
                    case "RenpyGoTo":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Goto, (RenpyGoTo)line);
                    case "RenpyIfElse":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.IfElse, Convert.ChangeType(line, Type.GetType("RenpyParser.RenpyIfElse")));
                    case "RenpyGoToLine":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.GotoLine, (RenpyGoToLine)line);
                    case "RenpyGoToLineUnless":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.GotoLineUnless, (RenpyGoToLineUnless)line);
                    case "RenpyImmediateTransform":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.ImmediateTransform, (RenpyImmediateTransform)line);
                    case "RenpyEasedTransform":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.EasedTransform, (RenpyEasedTransform)line);
                    case "RenpyPause":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Pause, (RenpyPause)line);
                    case "RenpyTime":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Time, (RenpyTime)line);
                    case "RenpySize":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Size, (RenpySize)line);
                    case "RenpyLoadImage":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.LoadImage, (RenpyLoadImage)line);
                    case "RenpyNOP":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.NOP, (RenpyNOP)line);
                    case "RenpyParallel":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Parallel, Convert.ChangeType(line, Type.GetType("RenpyParser.RenpyParallel")));
                    case "RenpyChoiceSet":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.ChoiceSet, Convert.ChangeType(line, Type.GetType("RenpyParser.RenpyChoiceSet")));
                    case "RenpyMenu":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Menu, Convert.ChangeType(line, Type.GetType("RenpyParser.RenpyMenu")));
                    case "RenpyMenuInput":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.MenuInput, (RenpyMenuInput)line);
                    case "RenpyForkGoToLine":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.ForkGotoLine, (RenpyForkGoToLine)line);
                    case "RenpySetRandomRange":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.SetRandRange, (RenpySetRandomRange)line);
                    case "RenpyStandardProxyLib.Text":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Text, (RenpyStandardProxyLib.Text)line);
                    case "RenpyStandardProxyLib.Expression":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Expression, (RenpyStandardProxyLib.Expression)line);
                    case "RenpyStandardProxyLib.WaitForScreen":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.WaitForScreen, (RenpyStandardProxyLib.WaitForScreen)line);
                    case "RenpyStandardProxyLib.WindowAuto":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.WindowAuto, (RenpyStandardProxyLib.WindowAuto)line);
                    case "RenpyGoToLineTimeout":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.GotoLineTimeout, (RenpyGoToLineTimeout)line);
                    case "RenpyUnlock":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Unlock, (RenpyUnlock)line);
                    case "RenpyClrFlag":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.ClrFlag, (RenpyClrFlag)line);
                    case "RenpyReturn":
                        return new Tuple<RenpyObjectType, object>(RenpyObjectType.Return, (RenpyReturn)line);
                }
            }
            catch(Exception e)
            {
                ConsoleUtils.Log($"Convert to Object failed -> {e.Message}");

                return default;
            }
        }

        public static RenpyObject MakeRenpyObject(Line line)
        {
            try
            {
                Tuple<RenpyObjectType, object> rawObjectType = GetObjectAndType(line);

                if (rawObjectType == default)
                {
                    ConsoleUtils.Log($"ConvertToObject in MakeRenpyObject failed -> Unknown Line type: {line.GetType().ToString()}");

                    return null;
                }

                return new RenpyObject(rawObjectType.Item2, line, rawObjectType.Item1);
            }
            catch(Exception e)
            {
                ConsoleUtils.Log($"MakeRenpyObject failed -> {e.Message}"); 

                return null;
            }
        }

        public static RenpyObject MakeRenpyObjectRaw(string rawLine, Line line)
        {
            try
            {
                Tuple<RenpyObjectType, object> rawObjectType = GetObjectAndType(line);

                if (rawObjectType == default)
                {
                    ConsoleUtils.Log($"ConvertToObject in MakeRenpyObject failed -> Unknown Line type: {line.GetType().ToString()}");

                    return null;
                }

                return new RenpyObject(rawObjectType.Item2, rawLine, line, rawObjectType.Item1);
            }
            catch (Exception e)
            {
                ConsoleUtils.Log($"MakeRenpyObject failed -> {e.Message}");

                return null;
            }
        }

        public static Tuple<int, RenpyObject> ParseLine(string line)
        {
            try
            {
                line = line.Replace("\t", "");
                //line = line.Replace(":", "");

                if (String.IsNullOrWhiteSpace(line))
                    return new Tuple<int, RenpyObject>(-1, null);

                //-1 = null/whitespace error code
                //0 = OK
                //1 = working block is complete, stop trying to parse lines
                //-2 = exception

                RenpyObject resultObject = null;
                string[] tokens = line.Split(' ');

                switch (tokens[0])
                {
                    case "python":
                        ConsoleUtils.Log("Inline python needs to be supported");
                        break;
                    //case "label":
                    //    ConsoleUtils.Log("Trying to parse type of Label entry point -> " + line);

                    //    WorkingLabel = tokens[1];
                    //    WorkingBlock = new BlockConstructor(WorkingLabel);

                    //    break; //dont add label entry points to the stack (since i mean we know its a label entry point??)
                    default:
                        if (tokens[0].StartsWith("\""))
                        {
                            //MC dialog

                            resultObject = MakeRenpyObjectRaw(line, RenpyUtils.Dialogue(WorkingLabel, line.Split('"')[1].Split('"')[0], true, false, "", false));

                            break;
                        }

                        if (tokens[0].Length == 1)
                        {
                            string charName = CharacterNameMappings[tokens[0]];

                            if (charName == null || charName == default)
                                charName = tokens[0].ToString();

                            resultObject = MakeRenpyObjectRaw(line, RenpyUtils.Dialogue(WorkingLabel, line.Split('"')[1].Split('"')[0], true, false, charName, false));
                            break;
                        }

                        if (tokens[0].Length > 1 && tokens[1].StartsWith("\""))
                        {
                            string initial = tokens[0][0].ToString();

                            if (!CharacterNameMappings.ContainsKey(initial))
                                CharacterNameMappings.Add(initial, tokens[0]);

                            resultObject = MakeRenpyObjectRaw(line, RenpyUtils.Dialogue(WorkingLabel, line.Split('"')[1].Split('"')[0], true, false, tokens[0], false));
                            break;
                        }

                        if (tokens[0][0] == '#')
                            break; //ignore comments

                        ConsoleUtils.Log($"StrToLine failed -> Unsupported statement: {line}");
                        break;
                    case "show":
                        resultObject = MakeRenpyObjectRaw(line, new RenpyShow(line));
                        break;
                    case "hide":
                        resultObject = MakeRenpyObjectRaw(line, new RenpyHide()
                        {
                            HideData = line,
                            hide = new Hide(tokens[1], false, false)
                        });
                        break;

                    case "sayori":
                    case "s":
                        resultObject = MakeRenpyObjectRaw(line, RenpyUtils.Dialogue(WorkingLabel, line.Split('"')[1].Split('"')[0], true, false, "Sayori", false));
                        break;

                    case "monika":
                    case "m":
                        resultObject = MakeRenpyObjectRaw(line, RenpyUtils.Dialogue(WorkingLabel, line.Split('"')[1].Split('"')[0], true, false, "Monika", false));
                        break;

                    case "yuri":
                    case "y":
                        resultObject = MakeRenpyObjectRaw(line, RenpyUtils.Dialogue(WorkingLabel, line.Split('"')[1].Split('"')[0], true, false, "Yuri", false));
                        break;

                    case "natsuki":
                    case "n":
                        resultObject = MakeRenpyObjectRaw(line, RenpyUtils.Dialogue(WorkingLabel, line.Split('"')[1].Split('"')[0], true, false, "Natsuki", false));
                        break;

                    case "stop":
                        resultObject = MakeRenpyObjectRaw(line, Interpreter.Stop());
                        break;
                    case "scene":
                        resultObject = MakeRenpyObjectRaw(line, new RenpyScene(line));
                        break;
                    case "with":
                        resultObject = MakeRenpyObjectRaw(line, Interpreter.With(line.Split(' ')[1]));
                        break;
                    case "play":
                        resultObject = MakeRenpyObjectRaw(line, Interpreter.Play(line.Split(' ')[2]));
                        break;
                    case "sfx":
                        resultObject = MakeRenpyObjectRaw(line, Interpreter.Play(line.Split(' ')[2], Channel.Sound));
                        break;
                    case "say":
                        resultObject = MakeRenpyObjectRaw(line, RenpyUtils.Dialogue(WorkingLabel, line.Split('"')[1].Split('"')[0], true, false, line.Split(' ')[1], false));
                        break;
                    case "return":
                        resultObject = MakeRenpyObjectRaw(line, Interpreter.Return());
                        break;
                    case "jump":
                        string labelJump = line.Split(' ')[1].ToString();

                        resultObject = MakeRenpyObjectRaw(line, new RenpyGoTo(labelJump, false, $"jump {labelJump}"));
                        break;
                    case "define_bg":
                        if (ModContext == null)
                            break;

                        string identifier = line.Split(' ')[1].ToString();

                        RenpyBlock block = new RenpyBlock("bg " + identifier, false, RenpyBlockAttributes.None, new List<Line>
                        {
                            new RenpyLoadImage(identifier, "assets/bgs/" + identifier + ".prefab")
                        });

                        AssetBlocks.Add(new Tuple<BlockEntryPoint, RenpyBlock>(new BlockEntryPoint(block.Label, 0), block));
                        break;
                    case "$":
                        string variable = line.Split(' ')[1].ToString();

                        string value = line.Split(' ')[3].ToString();

                        if (value.Contains("persistent.") || value.Contains("config."))
                            value = Renpy.CurrentContext.GetVariableString(value);

                        if (value.StartsWith("\""))
                            Renpy.CurrentContext.SetVariableString(variable, value.Replace("\"", ""));

                        if (value.ToLower().Contains("true") || value.ToLower().Contains("false"))
                            Renpy.CurrentContext.SetVariableString(variable, value);

                        if (float.TryParse(value, out float result))
                            Renpy.CurrentContext.SetVariableFloat(value, result);

                        break;
                }

                if (WorkingBlock != null)
                {
                    if (resultObject != null)
                    {
                        if (resultObject.Line.GetType() == typeof(RenpyShow))
                        {
                            string character = line.Split(' ')[1];
                            string express = line.Split(' ')[2];

                            if (express.Length > 2)
                                express = express.Substring(0, 2);

                            RenpyObject loadImageBlock = MakeRenpyObjectRaw($"load img: {character.ToLower()}/{express.ToLower()}", new RenpyLoadImage($"{character.ToLower()} {express.ToLower()}", $"assets/ddlc/gennedprefabs/{character.ToLower()} {express.ToLower()}.prefab"));

                            WorkingBlock.Add(loadImageBlock);
                        }
                        else if (resultObject.Line.GetType() == typeof(RenpyScene) && !WorkingResolveAssets.ContainsKey(line.Split(' ')[2]))
                        {
                            string bg = line.Split(' ')[2];

                            RenpyObject loadBgBlock = MakeRenpyObjectRaw($"load img: bg/{bg}", new RenpyLoadImage("bg_" + bg + "_original", $"assets/ddlc/gennedprefabs/bg_{bg}_original.prefab"));

                            WorkingBlock.Add(loadBgBlock);
                        }

                        WorkingBlock.Add(resultObject);
                    }

                    WorkingBlock.DetermineIfComplete();
                }

                return new Tuple<int, RenpyObject>(0, resultObject);
            }
            catch (Exception e)
            {
                ConsoleUtils.Log($"StrToLine failed -> {e.StackTrace}");

                return new Tuple<int, RenpyObject>(-2, null);
            }
        }
    }
}
