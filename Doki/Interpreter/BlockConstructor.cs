using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Interpreter
{
    public class BlockConstructor
    {
        private List<RenpyObject> objects = new List<RenpyObject>();

        private string Label { get; set; }

        public bool IsComplete = false;

        public BlockConstructor(string label)
        {
            Label = label;
        }

        public void Add(RenpyObject obj)
        {
            if (obj.Line.GetType() == typeof(RenpyLabelEntryPoint))
                return; //dont add other label entry points

            if (obj.Line.GetType() == typeof(RenpyReturn) || obj.Line.GetType().ToString().Contains("RenpyGoTo"))
                IsComplete = true;
            else
                objects.Add(obj);
        }

        public void DetermineIfComplete()
        {
            if (objects.Where(x => x.Line.GetType() == typeof(RenpyReturn)).Count() > 0)
                IsComplete = true;

            if (objects.Where(x => x.Line.GetType().ToString().Contains("RenpyGoTo")).Count() > 0)
                IsComplete = true;
        }

        public Tuple<BlockEntryPoint, RenpyBlock> Build()
        {
            if (!IsComplete) 
                return default; //Don't build unfinished blocks

            List<Line> builtLines = new List<Line>();

            BlockEntryPoint entryPoint = new BlockEntryPoint(Label, 0);

            foreach (RenpyObject obj in objects)
                builtLines.Add(obj.Line);

            RenpyBlock block = new RenpyBlock(Label, true, RenpyBlockAttributes.None, builtLines);

            block.callParameters = new RenpyCallParameter[0];

            return new Tuple<BlockEntryPoint, RenpyBlock>(entryPoint, block);
        }
    }
}
