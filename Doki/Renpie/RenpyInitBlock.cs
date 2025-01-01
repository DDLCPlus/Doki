using RenpyParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Renpie.Rpyc
{
    public class RenpyInitBlock
    {
        public List<Line> Contents { get; set; }

        public RenpyInitBlock(List<Line> rawContents)
        {
            Contents = rawContents;
        }
    }
}
