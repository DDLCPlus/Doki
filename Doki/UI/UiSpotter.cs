using Doki.Extensions;
using RenpyParser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Doki.UI
{
    public class UiSpotter : MonoBehaviour
    {
        private float delay = 0.2f;

        public void Awake()
        {
            ConsoleUtils.Log("UiSpotter.Awake", $"UiSpotter is awake!");

            StartCoroutine(CheckForUiElements());
        }

        public UiCheck[] uiElementsToCheck =
        [
            new UiCheck { ObjectPath = "LauncherMainCanvas/DesktopCanvas/CustomWallpaper", EventName = "custom_wallpaper", SceneName = "LauncherScene" },
            new UiCheck { ObjectPath = "LauncherMainCanvas/SettingsCanvas", EventName = "custom_settings", SceneName = "LauncherScene" },
            new UiCheck { ObjectPath = "LauncherMainCanvas/DesktopCanvas/DesktopDesktop/StartMenuItemCanvas", EventName = "start_menu_open", SceneName = "LauncherScene" },
        ];

        public IEnumerator CheckForUiElements()
        {
            while (true)
            {
                UiCheck[] uiElementsToCheckScene = uiElementsToCheck.Where(x => x.SceneName == SceneManager.GetActiveScene().name).ToArray();

                foreach (var uiCheck in uiElementsToCheckScene)
                {
                    GameObject uiObject = GameObject.Find(uiCheck.ObjectPath);

                    if (uiObject != null && uiObject.activeInHierarchy)
                        UiHandler.InvokeEvents(uiCheck.EventName);
                }

                yield return new WaitForSeconds(delay);
            }
        }
    }
}
