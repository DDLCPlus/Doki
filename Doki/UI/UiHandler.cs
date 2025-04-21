using Doki.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Doki.UI
{
    public static class UiHandler
    {
        private static List<UiEvent> UiEvents { get; set; }

        static UiHandler()
        {
            UiEvents = new List<UiEvent>();
        }

        public static List<UiEvent> GetUiEventsForTrigger(string trigger)
        {
            List<UiEvent> events = new List<UiEvent>();

            foreach (UiEvent _event in UiEvents)
            {
                if (_event.Trigger.ToLower() == trigger.ToLower())
                    events.Add(_event);
            }

            return events;
        }

        public static UiEvent GetUiEvent(string ID)
        {
            foreach (UiEvent _event in UiEvents)
            {
                if (_event.Trigger.ToLower() == ID.ToLower())
                    return _event;
            }

            return null;
        }

        public static void AddEvent(string when, Action what, bool triggerOnce = true) => UiEvents.Add(new UiEvent(when, what, triggerOnce));

        public static bool RemoveEvent(string ID)
        {
            UiEvent uiEvent = GetUiEvent(ID);

            if (uiEvent == null)
                return false;

            UiEvents.Remove(uiEvent);

            return true;
        }

        public static void InvokeEvents(string trigger)
        {
            List<UiEvent> uiEvents = GetUiEventsForTrigger(trigger);

            if (uiEvents.Count == 0)
                return;

            foreach (UiEvent uiEvent in uiEvents)
            { 
                if (uiEvent.TriggerOnce && uiEvent.Triggered)
                    continue;

                uiEvent.Action.Invoke();
                uiEvent.Triggered = true;
            }
        }

        public static void OverrideWallpaper(string fileName, bool doOnlyOnce = false)
        {
            AddEvent("custom_wallpaper", new Action(() =>
            {
                var wallpaper = GameObject.Find("LauncherMainCanvas/DesktopCanvas/CustomWallpaper");

                if (wallpaper == null)
                    return;

                ProxyAssetBundle proxyBundle = AssetUtils.FindProxyBundleByAssetKey(Path.GetFileNameWithoutExtension(fileName));

                if (proxyBundle == null) 
                    return;

                string outPath = proxyBundle.ToPath(Path.GetFileNameWithoutExtension(fileName));

                wallpaper.GetComponent<Image>().sprite = (Sprite)proxyBundle.Load("UnityEngine.Sprite", outPath);
                wallpaper.name = "CustomWallpaper_Modified";

            }), doOnlyOnce);
        }

        public static GameObject CreateStartMenuButton(string buttonText, Action buttonAction, bool buttonOpensMenu = false, Color backgroundHoverColor = default, Color textColor = default, bool newIconActive = false, string icon = null)
        {
            GameObject referenceButton = GameObject.Find("LauncherMainCanvas/DesktopCanvas/DesktopDesktop/StartMenuItemCanvas/StartMenuButton");
            GameObject heartMenuPanel = GameObject.Find("LauncherMainCanvas/DesktopCanvas/DesktopDesktop/StartMenuContainer/HeartMenuPanel");

            if (referenceButton == null || heartMenuPanel == null)
                return null;

            GameObject retButton = Transform.Instantiate(referenceButton, referenceButton.transform.parent.transform);
            Image buttonImage = retButton.GetComponent<Image>();

            retButton.transform.Find("FilesButtonText (TMP)").GetComponent<TextMeshProUGUI>().text = buttonText;

            if (textColor != default)
                retButton.transform.Find("FilesButtonText (TMP)").GetComponent<TextMeshProUGUI>().color = textColor;

            if (icon != null)
            {
                ProxyAssetBundle proxyBundle = AssetUtils.FindProxyBundleByAssetKey(Path.GetFileNameWithoutExtension(icon));

                if (proxyBundle == null)
                    return null;

                string outPath = proxyBundle.ToPath(Path.GetFileNameWithoutExtension(icon));

                retButton.transform.Find("HighlightImage").GetComponent<Image>().sprite = (Sprite)proxyBundle.Load("UnityEngine.Sprite", outPath);
            }

            if (newIconActive)
                retButton.transform.Find("HighlightImage").Find("New").gameObject.SetActive(true);

            retButton.GetComponent<StartMenuButton>().onClick.RemoveAllListeners();
            retButton.GetComponent<StartMenuButton>().onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                buttonAction.Invoke();
            }));

            retButton.name = $"{buttonText}Button";

            if (heartMenuPanel != null && buttonImage != null)
            {
                RectTransform heartMenuRectTransform = heartMenuPanel.GetComponent<RectTransform>();
                RectTransform buttonRectTransform = buttonImage.rectTransform;

                heartMenuRectTransform.sizeDelta = new Vector2(heartMenuRectTransform.sizeDelta.x, heartMenuRectTransform.sizeDelta.y + buttonRectTransform.rect.height);

                LayoutRebuilder.ForceRebuildLayoutImmediate(heartMenuRectTransform);
            }

            return retButton;
        }
    }
}
