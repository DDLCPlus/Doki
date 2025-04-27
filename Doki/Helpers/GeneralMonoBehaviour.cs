using Doki.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Doki.Helpers
{
    public class GeneralMonoBehaviour : MonoBehaviour
    {
        public IEnumerator ShowChoiceMenu(StandardChoiceMenu menu)
        {
            List<RenpyChoiceEntryUI> list = new List<RenpyChoiceEntryUI>();

            foreach (StandardChoiceMenuOption option in menu.Items)
                list.Add(new RenpyChoiceEntryUI(option.Text, option.ValueSetWhenClicked));

            RenpyScreenManager manager = Renpy.ContextControl.GetScreenManager();

            manager.GetScreen<RenpyChoiceMenuUI>(RenpyScreenID.ChooseScreen).ShowChoiceMenu(list, list.Count, true, false);

            yield return new WaitUntil(() => !manager.IsScreenShowing(RenpyScreenID.ChooseScreen));

            if (Renpy.ContextControl.GetMenuInputSelected() != null)
            {
                if (Renpy.ContextControl.GetMenuInputSelected().UserData is string)
                {
                    Renpy.CurrentContext.SetVariableString("madechoice", Renpy.ContextControl.GetMenuInputSelected().UserData as string);
                    UiHandler.ChoiceMade = Renpy.ContextControl.GetMenuInputSelected().UserData.ToString();
                }
                else
                {
                    Renpy.CurrentContext.SetVariableObject("madechoice", Renpy.ContextControl.GetMenuInputSelected().UserData);
                    UiHandler.ChoiceMade = Renpy.ContextControl.GetMenuInputSelected().UserData.ToString();
                }
            }
        }
    }
}
