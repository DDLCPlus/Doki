using Doki.Extensions;
using RenpyParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Renpie.Parser
{
    public class RenpyPass : Line
    {
        public override bool IsValid()
        {
            return true;
        }

        public override void Validate()
        {
            ConsoleUtils.Debug("Doki.RenpyPass", "Is Valid!");
        }

        public override bool SerialiseNextLine()
        {
            return true;
        }
    }
}
