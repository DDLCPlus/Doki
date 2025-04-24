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

        public Action Hovered { get; set; }

        public AudioClip OnHover { get; set; }

        public AudioClip OnClicked { get; set; }

        public ChoiceMenuButton(string text, Action clicked, Action hovered)
        {
            Text = text;
            Clicked = clicked;
            Hovered = hovered;

            OnHover = Renpy.Resources.Load<AudioClip>(PathHelpers.SanitizePathToAddressableName(path), assetMustExist);
        }
    }
}
