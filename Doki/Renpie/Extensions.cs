using Doki.Extensions;
using Doki.Helpers;
using Doki.Renpie.Parser;
using Doki.Renpie.RenDisco;
using HarmonyLib;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using SimpleExpressionEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine.UIElements;
using static RenpyStandardProxyLib;

namespace Doki.Renpie.Rpyc
{
    public static class Extensions
    {
        public static bool IsValidBlock(this PythonObj pythonObj) => pythonObj.Type == PythonObj.ObjType.NEWOBJ;

        //Thanks Kizby <3
        public static string ExtractPyExpr(PythonObj expr)
        {
            if (expr.Type == PythonObj.ObjType.NONE)
                return "None";

            if (expr.Type == PythonObj.ObjType.STRING)
                return expr.String;

            if (expr.Name == "renpy.ast.PyExpr")
                return expr.Args.Tuple[0].String;

            else if (expr.Name == "renpy.ast.PyCode")
            {
                var source = expr.Fields["source"];
                return ExtractPyExpr(expr.Fields["source"]);
            }

            return "";
        }

        public static DataValue ExecutePython(PythonObj python, RenpyExecutionContext context)
        {
            var rawExpression = ExtractPyExpr(python.Fields["code"]);

            try
            {
                var expression = new CompiledExpression();
                SimpleExpressionEngine.Parser.Parse(new Tokenizer(new StringReader(rawExpression))).Compile(expression);
                return ExpressionRuntime.Execute(expression, context);
            }
            catch (SyntaxException e)
            {
                ConsoleUtils.Error("Doki.Renpie.Extensions", e);
                //stub for now
                //unparseablePython.Add(Tuple.Create(rawExpression, e));
            }
            return new DataValue();
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
                else if (line.String.StartsWith("pause "))
                    return new RenpyPause(line.String, SimpleExpressionEngine.Parser.Compile(line.String));
                else
                    Console.WriteLine(line.String + " - HANDLE!");
            }

            return retLine;
        }

        private static RenpyOneLinePython ParseDefinition(PythonObj pythonObj)
        {
            //if (pythonObj.Name != "renpy.ast.Define" || pythonObj.Name != "renpy.ast.Default")
            //    throw new Exception($"Invalid renpy definition to be parsed. Expected renpy.ast.Define (or Default) but got {pythonObj.Name}!");

            string varname = pythonObj.Fields["varname"].String;

            if (string.IsNullOrEmpty(varname))
                throw new Exception($"Could not parse RenpyDefinition from AST. Couldn't find varname. Are you sure this is a valid Definition?");

            var pyCodeDefinition = pythonObj.Fields["code"];

            if (pyCodeDefinition.Name != "renpy.ast.PyCode")
                throw new Exception($"Invalid PyCode to be extracted. Expected renpy.ast.PyCode but got {pyCodeDefinition.Name}!");

            var pyStore = pythonObj.Fields["store"].String;

            if (pyStore == null)
                throw new Exception("Could not parse raw assignment value of PyCode. Unable to find source field.");

            var pySource = pyCodeDefinition.Fields["source"];

            string rawValue = ExtractPyExpr(pyCodeDefinition);

            if (string.IsNullOrEmpty(rawValue))
                throw new Exception("Could not parse raw assignment value of PyCode. Unable to extract inner value argument.");

            ConsoleUtils.Debug("Extensions.ParseDefinition", $"$ {varname} = {rawValue}");

            return new RenpyOneLinePython($"$ {varname} = {rawValue}");
        }

        private static RenpyGoTo ParseCall(PythonObj pythonObj)
        {
            string label = pythonObj.Fields["label"].String;

            //handle arguments(?) under .Fields["arguments"] (should be <none>, usually)

            return new RenpyGoTo(label, true, $"call {label}");
        }

        //Thanks Kizby, part 2.
        public static List<Line> HandleWhileStatement(PythonObj pythonObj, string label, List<Line> ContainerLines, Dictionary<object, Line> JumpMap)
        {
            List<Line> retLines = new List<Line>();

            var conditionStr = ExtractPyExpr(pythonObj.Fields["condition"]);
            var condition = SimpleExpressionEngine.Parser.Compile(conditionStr);
            var gotoStmt = new RenpyGoToLineUnless(conditionStr, -1);
            gotoStmt.CompiledExpression = condition;
            retLines.Add(gotoStmt);

            foreach (var stmt in pythonObj.Fields["block"].List)
                ContainerLines.Add(stmt.AsRenpyLine(label));

            var loopGoto = new RenpyGoToLine(-1);

            retLines.Add(loopGoto);
            JumpMap.Add(loopGoto, gotoStmt);

            var gotoTarget = new RenpyNOP();
            retLines.Add(gotoTarget);
            JumpMap.Add(gotoStmt, gotoTarget);

            return retLines;
        }

        // Thanks, Kizby.
        public static List<Line> HandleIfStatement(PythonObj pythonObj, string label, List<Line> ContainerLines, Dictionary<object, Line> JumpMap)
        {
            //return goto Stmt and hard Goto and after If 
            List<Line> retLines = new List<Line>();

            var entries = pythonObj.Fields["entries"].List;
            var afterIf = new RenpyNOP();
            RenpyGoToLineUnless lastGoto = null;

            foreach (var entry in entries)
            {
                var conditionString = ExtractPyExpr(entry.Tuple[0]);
                var condition = SimpleExpressionEngine.Parser.Compile(conditionString);
                var gotoStmt = new RenpyGoToLineUnless(conditionString, -1);

                gotoStmt.CompiledExpression = condition;
                retLines.Add(gotoStmt);

                if (lastGoto != null)
                    JumpMap.Add(lastGoto, gotoStmt);

                lastGoto = gotoStmt;

                foreach (var stmt in entry.Tuple[1].List)
                    ContainerLines.Add(stmt.AsRenpyLine(label));

                var hardGoto = new RenpyGoToLine(-1);
                JumpMap.Add(hardGoto, afterIf);
                retLines.Add(hardGoto);
            }

            JumpMap.Add(lastGoto, afterIf);
            retLines.Add(afterIf);

            return retLines;
        }

        //Kizby again - I'm lazy.
        public static Line ParsePythonStatement(PythonObj pythonObj, string respectiveLabel)
        {
            var codeString = ExtractPyExpr(pythonObj.Fields["code"]);

            if (codeString.Contains("\n"))
            {
                RenpyInlinePython renpyInlinePython = new RenpyInlinePython(codeString, respectiveLabel);
                InLinePython m_InlinePython = (InLinePython)renpyInlinePython.GetPrivateField("m_InlinePython");

                if (m_InlinePython.hash == 2039296337)
                {
                    // default first run setting block - kizby
                    m_InlinePython.hash = 318042419;
                    m_InlinePython.functionName = "splashscreen_inlinepythonblock_318042419";
                }
                else if (m_InlinePython.hash == 1991019598)
                {
                    // default s_kill_early check
                    m_InlinePython.hash = 85563775;
                    m_InlinePython.functionName = "splashscreen_inlinepythonblock_85563775";
                }

                return renpyInlinePython;
            }
            else
            {
                RenpyOneLinePython renpyOneLinePython = new RenpyOneLinePython("$" + codeString);

                return renpyOneLinePython;
            }
        }

        public static Line AsRenpyLine(this PythonObj pythonObj, string respectiveLabel)
        {
            /*
             * renpy.ast.Init
1 - init
2 - init - 1
3 - init - renpy.ast.Define of 1
            */

            ConsoleUtils.Debug("Extensions.AsRenpyLine", "Handling " + pythonObj.Name + "...");
            switch (pythonObj.Name)
            {
                default:
                    Console.WriteLine("NAME - " + pythonObj.Name + " - returning dummy line");
                    return null; //return dummy line
                case "renpy.ast.Python":
                    return ParsePythonStatement(pythonObj, respectiveLabel);
                case "renpy.ast.Pass":
                    return new RenpyPass();
                case "renpy.ast.Call":
                    return ParseCall(pythonObj);
                case "renpy.ast.Show":
                    return ParseShowExpression(pythonObj);
                case "renpy.ast.Hide":
                    return ParseHideExpression(pythonObj);
                case "renpy.ast.Default":
                case "renpy.ast.Define":
                    return ParseDefinition(pythonObj);
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
