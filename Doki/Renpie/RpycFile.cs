using Doki.Extensions;
using Doki.Helpers;
using Microsoft.VisualBasic;
using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Renpie.Rpyc
{
    public class RpycFile
    {
        public bool Valid = true;

        public Dictionary<BlockEntryPoint, RenpyBlock> Labels = new Dictionary<BlockEntryPoint, RenpyBlock>();

        public Dictionary<object, Line> JumpMap = new Dictionary<object, Line>(); //Credits to Kizby for this

        public List<RenpyInitBlock> Inits = new List<RenpyInitBlock>();

        public List<PythonObj> EarlyPy = new List<PythonObj>();

        public List<PythonObj> Py = new List<PythonObj>();

        private int FindZlibStart(byte[] data, byte[][] headers)
        {
            for (int i = 0; i < data.Length - 1; i++)
            {
                foreach (byte[] header in headers)
                {
                    if (data.Length >= i + header.Length && data.Skip(i).Take(header.Length).SequenceEqual(header))
                        return i;
                }
            }

            return -1;
        }

        public RpycFile(byte[] Contents)
        {
            byte[][] zlibHeaders = new byte[][]
            {
                new byte[] { 0x78, 0xDA },
                new byte[] { 0x78, 0x9C },
                new byte[] { 0x78, 0x01 }
            };

            int zlibStartIndex = FindZlibStart(Contents, zlibHeaders);

            if (zlibStartIndex == -1)
            {
                Valid = false;
                throw new Exception("No valid zlib header found in this .rpyc file!");
            }

            byte[] zlibData = Contents.Skip(zlibStartIndex).ToArray();

            using (MemoryStream stream = new MemoryStream(zlibData))
            {
                byte[] compressedData = new byte[stream.Length - zlibStartIndex];

                stream.Read(compressedData, 0, compressedData.Length);

                PythonObj pythonObj = Unpickler.UnpickleZlibBytes(compressedData);

                if (pythonObj.Type != PythonObj.ObjType.TUPLE)
                {
                    Valid = false;

                    throw new Exception("Invalid .rpyc file!");
                }

                File.WriteAllText($"script-{Math.Round((double)new Random().Next(100, 10000))}.debug", pythonObj.ToString());

                //Console.WriteLine(pythonObj.ToString());

                Process(pythonObj.Tuple[1].List);
            }

            //TUPLE ->
            // - DICT (key, unlocked, version)
            // - LIST (python objects)
        }

        private List<Line> ProcessBlock(PythonObj pythonObj, string respectiveLabel)
        {
            List<Line> retLines = new List<Line>();

            foreach(var x in pythonObj.List)
            {
                if (x.Name == "renpy.ast.If")
                {
                    retLines.AddRange(Extensions.HandleIfStatement(x, respectiveLabel, retLines, JumpMap));
                    continue;
                }

                if (x.Name == "renpy.ast.While")
                {
                    retLines.AddRange(Extensions.HandleWhileStatement(x, respectiveLabel, retLines, JumpMap));
                    continue;
                }

                Line outLine = x.AsRenpyLine(respectiveLabel);

                if (outLine != null)
                    retLines.Add(outLine);
            }

            return retLines;
        }

        private void BuildBlock(PythonObj rawBlock)
        {
            if (rawBlock.Type != PythonObj.ObjType.NEWOBJ)
                return;

            string name = rawBlock.Fields["name"].String;
            PythonObj renBlock = rawBlock.Fields["block"];

            //Console.WriteLine(renBlock.ToString());

            //foreach(var entry in rawBlock.Fields)
            //{
            //    Console.WriteLine($"====== {entry.Key} BEGIN ======");
            //    Console.WriteLine(entry.Value.ToString());
            //    Console.WriteLine($"====== {entry.Key} END ======");
            //}
            //handle parameters(under rawBlocks.Fields["parameters"] later)

            BlockEntryPoint entryPoint = new BlockEntryPoint(name, 0);

            RenpyBlock block = new RenpyBlock();

            block.callParameters = new RenpyCallParameter[0];
            block.Label = name;
            block.IsMainLabel = false;
            block.Contents = ProcessBlock(renBlock, name);

            var container = block.Contents;

            foreach(var entry in JumpMap)
            {
                switch (entry.Key)
                {
                    case RenpyGoToLine goToLine:
                        goToLine.TargetLine = container.IndexOf(JumpMap[goToLine]);
                        break;
                    case RenpyGoToLineUnless goToLineUnless:
                        goToLineUnless.TargetLine = container.IndexOf(JumpMap[goToLineUnless]);
                        break;
                    case RenpyMenuInputEntry menuInputEntry:
                        menuInputEntry.gotoLineTarget = container.IndexOf(entry.Value);
                        break;
                }
            }

            JumpMap.Clear();
            Labels.Add(entryPoint, block);
        }

        private void HandleLabel(PythonObj labelBlock) => BuildBlock(labelBlock);

        private void HandleInit(PythonObj pythonObj)
        {
            List<Line> retLines = new List<Line>();

            List<PythonObj> initBlockContents = pythonObj.Fields["block"].List;

            foreach (PythonObj initBlock in initBlockContents)
            {
                if (initBlock.Name == "renpy.ast.EarlyPython")
                {
                    Py.Add(initBlock);
                    continue;
                }

                Line equivalent = initBlock.AsRenpyLine(null);

                if (equivalent != null)
                    retLines.Add(equivalent);
            }

            Inits.Add(new RenpyInitBlock(retLines));
        }

        private void Process(List<PythonObj> List)
        {
            foreach (PythonObj obj in List)
            {
                switch (obj.Name)
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
