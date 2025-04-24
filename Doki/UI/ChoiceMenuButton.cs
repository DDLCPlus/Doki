using RenPyParser.AssetManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Doki.UI
{
    public class ChoiceMenuButton
    {
        public string Text { get; set; }

        public Action Clicked { get; set; }

        public ChoiceMenuButton(string text, Action clicked)
        {
            Text = text;
            Clicked = clicked;
        }
    }
}
