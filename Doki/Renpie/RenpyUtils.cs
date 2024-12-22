using Doki.Extensions;
using Doki.Renpie.RenDisco;
using HarmonyLib;
using RenpyParser;
using RenPyParser;
using RenPyParser.Transforms;
using RenPyParser.VGPrompter.DataHolders;
using SimpleExpressionEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using static RenPyParser.Sprites.CompositeSpriteParser;
using Dialogue = Doki.Renpie.Parser.Dialogue;

namespace Doki.Renpie
{
    /*
     RENDISCO MIT LICENSE:

     Copyright (c) 2024 aaartrtrt

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
    */

    public static class RenpyUtils
    {
        public static void DumpBlock(RenpyBlock block)
        {
            string output = $"label {block.Label}:\n";

            foreach (var line in block.Contents)
            {
                switch (line)
                {
                    case RenpyShow renpyShow:
                        var show = renpyShow.show;
                        var transform = show.TransformName;

                        if (transform == "" && show.IsLayer)
                            transform = "resetlayer";

                        output += $"show{
                            (show.IsLayer ? $" layer {show.Name}" : $" {show.AssetName}") +
                            (show.As != "" ? $" as {show.As}" : "") +
                            (transform != "" ? $" at {show.TransformName}" : "") +
                            (show.HasBehind ? $" behind {show.Behind}" : "") +
                            (show.IsLayer ? $" onlayer {show.Layer}" : "") +
                            (show.HasZOrder ? $" zorder {show.ZOrder}" : "")}\n";
                        break;
                    case RenpyLoadImage loadImage:
                        output += $"image {loadImage.key} = \"{loadImage.fullImageDetails}\"\n";
                        break;
                    case RenpyHide hide:
                        output += $"{hide.HideData}\n";
                        break;
                    case RenpyPlay play:
                        output += $"{play.PlayData}\n";
                        break;
                    case RenpyPause pause:
                        output += $"{pause.PauseData}\n";
                        break;
                    case RenpyGoTo goTo:
                        output += (goTo.IsCall ? "call " : "jump ") +
                            (goTo.TargetLabel != "" ? goTo.TargetLabel : goTo.targetExpression.ToString()) +
                            (goTo.IsCall ? $"({goTo.callParameters.Join(p => p.expression.ToString())})" : "") + "\n";
                        break;
                    case RenpyStop stop:
                        output += $"{stop.StopData}\n";
                        break;
                    case RenpyQueue queue:
                        output += $"{queue.QueueData}\n";
                        break;
                    case RenpyNOP nop:
                        output += "pass\n";
                        break;
                    case RenpyReturn ret:
                        output += "return\n";
                        break;
                    case RenpySize size:
                        output += $"{size.SizeData}\n";
                        break;
                    case RenpyEasedTransform renpyEasedTransform:
                        output += $"{renpyEasedTransform.TransformCommand}\n";
                        break;
                    case RenpyGoToLineUnless renpyGoToLineUnless:
                        output += $"goto {renpyGoToLineUnless.TargetLine} unless {renpyGoToLineUnless.ConditionText}\n";
                        break;
                    case RenpyImmediateTransform renpyImmediateTransform:
                        output += $"{renpyImmediateTransform.TransformCommand}\n";
                        break;
                    case RenpyGoToLine renpyGoToLine:
                        output += $"goto {renpyGoToLine.TargetLine}\n";
                        break;
                    case RenpyForkGoToLine renpyForkGoToLine:
                        output += $"fork goto {renpyForkGoToLine.TargetLine}\n";
                        break;
                }
            }

            output += "\nEND\n";

            ConsoleUtils.Debug("Doki", output);
        }

        public static RenpySize CreateSize(int width, int height)
        {
            RenpySize ret = new($"size({width}x{height})", null, null, true, true);

            var sizeXProperty = typeof(RenpySize).GetProperty("SizeX", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var sizeYProperty = typeof(RenpySize).GetProperty("SizeY", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            sizeXProperty?.SetValue(ret, width); // If null, set value
            sizeYProperty?.SetValue(ret, height); // I had no clue this existed this is fucking cool

            return ret;
        }
    }
}
