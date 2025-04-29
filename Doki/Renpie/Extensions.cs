using Doki.Helpers;
using Doki.Renpie.Parser;
using Doki.Renpie.RenDisco;
using HarmonyLib;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Doki.Renpie.Rpyc
{
    public static class Extensions
    {
        public static bool IsValidBlock(this PythonObj pythonObj) => pythonObj.Type == PythonObj.ObjType.NEWOBJ;

        public static Line ParsePyExpression(PythonObj pythonObj)
        {
            //foreach (var entry in pythonObj.Fields)
            //{
            //    Console.WriteLine($"====== {entry.Key} BEGIN ======");
            //    Console.WriteLine(entry.Value.ToString());
            //    Console.WriteLine($"====== {entry.Key} END ======");
            //}

            //PythonObj code = pythonObj.Fields["code"];

            //if (pythonObj.Fields.ContainsKey("store"))
            //{
            //    PythonObj store = code.Fields["store"];

            //    string variableAssignmentRaw = store.Args.Tuple[0].String;

            //    if (!variableAssignmentRaw.Contains(" = "))
            //    {
            //        //Not variable assignment
            //        return null;
            //    }
            //    else
            //        return new RenpyOneLinePython($"$ {variableAssignmentRaw}");
            //}

            return null;
        }

        private static RenpyShow ParseShowExpression(PythonObj pythonObj)
        {
            var imspec = pythonObj.Fields["imspec"];
            var imspecTuple = imspec.Tuple;
            var showAssets = imspecTuple[0].Tuple;
            var pyExprs = imspecTuple.Where(x => x.Name == "renpy.ast.PyExpr").ToList(); //first would be the transform data, could tell by the letter -> numbers after
            var hasZOrder = pyExprs.Where(x => x.Args.Tuple.Count > 0 && x.Args.Tuple[0].Type == PythonObj.ObjType.INT).Count() > 0;

            var imgAsset = showAssets[0].String ?? "";
            var specExpr = "";

            if (showAssets.Count() >= 2)
                specExpr = showAssets[1].String;

            //second would be the zorder

            /* showAssets is:
                (
                 'sayori'
                 '1a'
                ) */

            string outputShow = $"show {imgAsset} {(specExpr == null ? "" : specExpr)}";

            RenpyShow retShow = new RenpyShow("show ")
            {
                show =
                {
                    IsImage = true,
                    ImageName = imgAsset,
                    Variant = string.IsNullOrEmpty(specExpr) ? specExpr : "",
                    TransformName = "",
                    TransformCallParameters = [],
                    Layer = "master",
                    As = "",
                    HasZOrder = hasZOrder
                }
            };

            //Handle layer, transform name & call parameters, and as

            retShow.ShowData = outputShow;

            return retShow;
        }

        private static RenpyHide ParseHideExpression(PythonObj pythonObj)
        {
            var imspec = pythonObj.Fields["imspec"];
            var imspecTuple = imspec.Tuple;
            var hideAssets = imspecTuple[0].Tuple;
            var imgAsset = hideAssets[0].String ?? "";

            return new RenpyHide()
            {
                hide = new RenpyParser.Hide(imgAsset, false)
            };
        }

        private static Line ParseSayExpression(PythonObj pythonObj, string respectiveLabel)
        {
            var whoObj = pythonObj.Fields["who"];
            var who = whoObj.Type == PythonObj.ObjType.NONE ? "" : whoObj.String;
            var what = pythonObj.Fields["what"].String;

            return new Parser.Dialogue(respectiveLabel, who, what, false, false, who == "mc" || who == "player", "say").Line;
        }
        
        private static RenpyScene ParseSceneExpression(PythonObj pythonObj)
        {
            var type = pythonObj.Fields["imspec"].Tuple[0].Tuple[0].String;
            var what = pythonObj.Fields["imspec"].Tuple[0].Tuple[1].String;

            return new RenpyScene($"{type} {what}");
        }

        private static RenpyWith ParseWithExpression(PythonObj pythonObj)
        {
            var transition = pythonObj.Fields["expr"].Args.Tuple[0].String;

            return new RenpyWith(transition, SimpleExpressionEngine.Parser.Compile(transition));
        }

        private static RenpyWindow ParseWindowExpression(string rawLine)
        {
            string mode = rawLine.Split(' ')[1];

            switch(mode.ToLower())
            {
                case "hide":
                    return new RenpyWindow() { window = new Window { Mode = RenpyWindowManager.WindowManagerMode.Hide, Transition = null }, WindowData = rawLine };
                case "auto":
                    return new RenpyWindow() { window = new Window { Mode = RenpyWindowManager.WindowManagerMode.Auto, Transition = null }, WindowData = rawLine };
                case "show":
                    return new RenpyWindow() { window = new Window { Mode = RenpyWindowManager.WindowManagerMode.Show, Transition = null }, WindowData = rawLine };
            }

            return null;
        }

        private static Line ParseUserStatement(PythonObj pythonObj, string respectiveLabel)
        {
            Line retLine = new Line();

            var line = pythonObj.Fields["line"];

            if (line.Type == PythonObj.ObjType.STRING)
            {
                if (line.String.StartsWith("stop "))
                {
                    string[] stopArguments = line.String.Split(' ');

                    RenpyStop retStop = new() { stop = new Stop() };

                    if (stopArguments.Length > 2 && stopArguments[3].Contains("fadeout"))
                        retStop.stop.fadeout = float.Parse(stopArguments[4]);

                    retStop.stop.Channel = stopArguments[1] switch
                    {
                        "musicpoem" => Channel.MusicPoem,
                        "sound" => Channel.Sound,
                        _ => Channel.Music,
                    };

                    retLine = retStop;
                }
                else if (line.String.StartsWith("play "))
                {
                    string[] playArguments = line.String.Split(' ');

                    RenpyPlay retPlay = new() { play = new RenpyParser.Play() { Asset = playArguments[2] } };

                    if (playArguments.Length > 3 && playArguments[4].Contains("fadein")) //Handle fadein
                        retPlay.play.fadein = float.Parse(playArguments[5]);

                    retPlay.play.Channel = playArguments[1] switch
                    {
                        "musicpoem" => Channel.MusicPoem,
                        "sound" => Channel.Sound,
                        _ => Channel.Music,
                    };

                    retLine = retPlay;
                }
                else if (line.String.StartsWith("window "))
                    return ParseWindowExpression(line.String); // IScriptLine scriptLine = ((IWrappable)line).ToWrapper(executionContext.script);
                else
                    Console.WriteLine(line.String + " - HANDLE!");
            }

            return retLine;
        }

        public static string CodeAsString(this PythonObj pythonObj)
        {
            if (pythonObj.Name != "renpy.ast.PyCode")
                throw new Exception($"Invalid PyCode to be extracted. Expected renpy.ast.PyCode but got {pythonObj.Name}!");

            PythonObj store = pythonObj.Fields["store"];

            if (store == null)
                throw new Exception("Could not parse raw assignment value of PyCode. Unable to find source field.");

            string rawValue = store.Args.Tuple[0].String;

            if (string.IsNullOrEmpty(rawValue))
                throw new Exception("Could not parse raw assignment value of PyCode. Unable to extract inner value argument.");

            return rawValue;
        }

        public static RenpyDefinition AsRenpyDefinition(this PythonObj pythonObj)
        {
            if (pythonObj.Name != "renpy.ast.Define")
                throw new Exception($"Invalid renpy definition to be parsed. Expected renpy.ast.Define but got {pythonObj.Name}!");

            string varname = pythonObj.Fields["varname"].String;

            if (string.IsNullOrEmpty(varname))
                throw new Exception($"Could not parse RenpyDefinition from AST. Couldn't find varname. Are you sure this is a valid Definition?");

            string rawCode = pythonObj.Fields["code"].CodeAsString();

            return new RenpyDefinition(varname, rawCode, DefinitionType.Define);
        }

        public static Line AsRenpyLine(this PythonObj pythonObj, string respectiveLabel)
        {
            switch(pythonObj.Name)
            {
                default:
                    Console.WriteLine("NAME - " + pythonObj.Name);
                    return null;
                //case "renpy.ast.Python":
                //    object pyExprRes = ParsePyExpression(pythonObj);

                //    if (pyExprRes != null)
                //        return (Line)pyExprRes;

                //    break;
                case "renpy.ast.Show":
                    return ParseShowExpression(pythonObj);
                case "renpy.ast.Hide":
                    return ParseHideExpression(pythonObj);
                case "renpy.ast.Define":
                    RenpyDefinition definition = pythonObj.AsRenpyDefinition();

                    return new RenpyOneLinePython($"$ {definition.Name} = {definition.Value}");
                case "renpy.ast.Return":
                    return new RenpyReturn();
                case "renpy.ast.Scene":
                    return ParseSceneExpression(pythonObj);
                case "renpy.ast.With":
                    return ParseWithExpression(pythonObj);
                case "renpy.ast.Say":
                    return ParseSayExpression(pythonObj, respectiveLabel);
                case "renpy.ast.UserStatement":
                    return ParseUserStatement(pythonObj, respectiveLabel);
            }
        }
    }
}
