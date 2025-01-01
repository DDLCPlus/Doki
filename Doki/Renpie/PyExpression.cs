using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Renpie.Rpyc
{
    public enum Where
    {
        Left,
        Right
    }

    public class Direction
    {
        public object At { get; set; }

        public Where Where { get; set; }
    }

    public class PyExpression
    {
        public Direction Left { get; set; }

        public Direction Right { get; set; }
    }
}
