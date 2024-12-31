using Doki.Helpers;
using Microsoft.VisualBasic;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpycTest
{
    public class RpycFile
    {
        public bool Valid = true;

        public Dictionary<BlockEntryPoint, RenpyBlock> Labels = new Dictionary<BlockEntryPoint, RenpyBlock>();

        public RpycFile(byte[] Contents)
        {
            List<byte> listBytes = new List<byte>();

            using (MemoryStream stream = new MemoryStream(Contents))
            {
                stream.Position = 0x2E; //Zlib header (78 DA for RPYC 2 files)

                for (long i = stream.Position; i < stream.Length; )
                {
                    var data = stream.ReadByte();

                    listBytes.Add((byte)data);
                }
            }

            PythonObj pythonObj = Unpickler.UnpickleZlibBytes(listBytes.ToArray());

            if (pythonObj.Type != PythonObj.ObjType.TUPLE)
            {
                Valid = false;

                throw new Exception("Invalid .rpyc file!");
            }

            Process(pythonObj.Tuple[1].List);

            //TUPLE ->
            // - DICT (key, unlocked, version)
            // - LIST (python objects)
        }

        private bool SkipPyExpression(string rawExpression)
        {
            List<string> forbiddenKeywords = new List<string>()
            {
                "import"
            };

            foreach(var keyword in forbiddenKeywords)
            {
                if (rawExpression.ToLower().Contains(keyword))
                    return true;
            }

            return false;
        }

        private object DoPythonExpression(PythonObj pythonObj, List<Line> retLines)
        {
            PythonObj code = pythonObj.Fields["code"];

            if (pythonObj.Fields.ContainsKey("store"))
            {
                PythonObj store = code.Fields["store"];

                string variableAssignmentRaw = store.Args.Tuple[0].String;

                if (!variableAssignmentRaw.Contains(" = "))
                {
                    //Not variable assignment
                    return null;
                } else
                    return new RenpyOneLinePython($"$ {variableAssignmentRaw}");
            }

            return null;
        }

        private RenpyShow ParseShow(PythonObj pythonObj)
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

        private void ProcessLine(PythonObj pythonObj, List<Line> retLines)
        {
            switch(pythonObj.Name)
            {
                case "renpy.ast.Python":
                    object pyExprRes = DoPythonExpression(pythonObj, retLines);

                    if (pyExprRes != null)
                        retLines.Add((Line)pyExprRes);
                    break;
                case "renpy.ast.Show":
                    retLines.Add(ParseShow(pythonObj));
                    break;
            }
        }

        private List<Line> ProcessBlock(PythonObj pythonObj)
        {
            List<Line> retLines = new List<Line>();

            pythonObj.List.ForEach(x => {
                ProcessLine(x, retLines);
            });

            return retLines;
        }

        private void BuildBlock(PythonObj rawBlock)
        {
            if (rawBlock.Type != PythonObj.ObjType.NEWOBJ)
                return;

            string name = rawBlock.Fields["name"].String;
            PythonObj renBlock = rawBlock.Fields["block"];

            //handle parameters (under rawBlocks.Fields["parameters"] later)

            BlockEntryPoint entryPoint = new BlockEntryPoint(name, 0);

            RenpyBlock block = new RenpyBlock();

            block.callParameters = [];
            block.Label = name;
            block.IsMainLabel = false;
            block.Contents = ProcessBlock(renBlock);

            Labels.Add(entryPoint, block);
        }

        private void HandleLabel(PythonObj labelBlock) => BuildBlock(labelBlock);

        private void HandleInit(PythonObj pythonObj)
        {
            //To-do
        }

        private void Process(List<PythonObj> List)
        {
            foreach(PythonObj obj in List)
            {
                switch(obj.Name)
                {
                    case "renpy.ast.Label":
                        HandleLabel(obj);
                        break;
                    case "renpy.ast.Init":
                        HandleInit(obj);
                        break;
                }
            }
        }
    }
}
