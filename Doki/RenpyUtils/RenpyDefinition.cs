using RenpyParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.RenpyUtils
{
    public class RenpyDefinition
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public RenpyDefinition(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
