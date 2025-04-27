using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.UI
{
    public class StandardChoiceMenuOption
    {
        public string Text { get; set; }

        public string ValueSetWhenClicked { get; set; }

        public Action ActionRanWhenClicked { get; set; }

        public string LabelJumpedToWhenClicked { get; set; }

        public StandardChoiceMenuOption(string text, string valueSetWhenClicked)
        {
            Text = text;
            ValueSetWhenClicked = valueSetWhenClicked;
        }

        public StandardChoiceMenuOption(string text, Action actionRanWhenClicked)
        {
            Text = text;
            ValueSetWhenClicked = new Guid().ToString();
            ActionRanWhenClicked = actionRanWhenClicked;
        }

        public StandardChoiceMenuOption(string text, string labelJumpedToWhenClicked, bool softJump = false)
        {
            Text = text;
            ValueSetWhenClicked = new Guid().ToString();
            LabelJumpedToWhenClicked = labelJumpedToWhenClicked;
        }
    }
}
