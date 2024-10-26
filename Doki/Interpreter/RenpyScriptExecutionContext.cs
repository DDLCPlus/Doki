using RenpyParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Interpreter
{
    public class RenpyScriptExecutionContext
    {
        public RenpyCallstackEntry RenpyCallstackEntry { get; set; }

        public Line CurrentLine { get; set; }

        public int LineIndex { get; set; }

        public RenpyBlock CurrentBlock { get; set; }

        public RenpyScriptExecutionContext(RenpyCallstackEntry entry, Line currentLine, int lineIndex, RenpyBlock currentBlock)
        {
            RenpyCallstackEntry = entry;
            CurrentLine = currentLine;
            LineIndex = lineIndex;
            CurrentBlock = currentBlock;
        }
    }
}
