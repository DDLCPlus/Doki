using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.UI
{
    public class StandardChoiceMenu
    {
        public List<StandardChoiceMenuOption> Items { get; set; }

        public StandardChoiceMenu(List<StandardChoiceMenuOption> items)
        {
            Items = items;
        }
    }
}
