using SimpleExpressionEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Interpreter
{
    public class PurePython
    {
        private static List<string> strings = new List<string>();

        public void Add(string line)
        {
            strings.Add(line);
        }

        private string Build()
        {
            if (strings.Count == 0) return null;

            string finalStr = "";

            foreach (string str in strings)
                finalStr += str;

            return finalStr;
        }

        public DataValue Execute()
        {
            string code = Build();

            if (code == "" || code == null)
                return DataValue.EmptyString;

            return Interpreter.RunExpression(code, RenpyUtils.GetContext());
        }
    }
}
