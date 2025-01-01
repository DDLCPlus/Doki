using Doki.Helpers;
using Doki.Renpie.Parser;
using HarmonyLib;
using RenpyParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Renpie.Rpyc
{
    public static class Extensions
    {
        public static bool IsValidBlock(this PythonObj pythonObj) => pythonObj.Type == PythonObj.ObjType.NEWOBJ;

        //public static PyExpression ParsePyExpression(this PythonObj pythonObj)
        //{
        //    PythonObj code = pythonObj.Fields["code"];

        //    if (pythonObj.Fields.ContainsKey("store"))
        //    {
        //        PythonObj store = code.Fields["store"];

        //        string variableAssignmentRaw = store.Args.Tuple[0].String;

        //        if (!variableAssignmentRaw.Contains(" = "))
        //        {
        //            //Not variable assignment
        //            return null;
        //        }
        //        else
        //            return new RenpyOneLinePython($"$ {variableAssignmentRaw}");
        //    }

        //    return null;
        //}

        private static RenpyShow ParseShowExpression(PythonObj pythonObj)
        {
            var imspec = pythonObj.Fields["imspec"];
            var imspecTuple = imspec.Tuple;
            var showAssets = imspecTuple[0].Tuple;
            var pyExprs = imspecTuple.Where(x => x.Name == "renpy.ast.PyExpr").ToList(); //first would be the transform data, could tell by the letter -> numbers after
            var hasZOrder = pyExprs.Where(x => x.Args.Tuple.Count > 0 && x.Args.Tuple[0].Int != default).Count() > 0;

            var imgAsset = showAssets[0].String;
            var specExpr = showAssets[1].String;

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
                    TransformCallParameters = new RenpyCallParameter[0],
                    Layer = "master",
                    As = "",
                    HasZOrder = hasZOrder
                }
            };

            //Handle layer, transform name & call parameters, and as

            retShow.ShowData = outputShow;

            return retShow;
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

        public static Line AsRenpyLine(this PythonObj pythonObj)
        {
            switch(pythonObj.Name)
            {
                //case "renpy.ast.Python":
                //    object pyExprRes = DoPythonExpression(pythonObj, retLines);

                //    if (pyExprRes != null)
                //        return (Line)pyExprRes;
                //    break;
                case "renpy.ast.Show":
                    return ParseShowExpression(pythonObj);
                case "renpy.ast.Define":
                    RenpyDefinition definition = pythonObj.AsRenpyDefinition();

                    return new RenpyOneLinePython($"$ {definition.Name} = {definition.Value}");
                case "renpy.ast.Return":
                    return new RenpyReturn();
            }

            return null;
        }
    }
}
