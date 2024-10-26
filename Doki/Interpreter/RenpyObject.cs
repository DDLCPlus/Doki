using RenpyParser;
using RenPyParser.VGPrompter.DataHolders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Interpreter
{
    public class RenpyObject
    {
        public object Object { get; set; }
        
        public string Contents { get; set; }

        public Line Line { get; set; }

        public RenpyObjectType Type { get; set; }

        public RenpyObject(object obj, Line line, RenpyObjectType type)
        {
            Object = obj;
            Line = line;
            Type = type;
        }

        public RenpyObject(object obj, string contents, Line line, RenpyObjectType type)
        {
            Object = obj;
            Contents = contents;
            Line = line;
            Type = type;
        }
    }
}
