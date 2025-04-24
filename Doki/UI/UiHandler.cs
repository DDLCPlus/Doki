using Doki.Extensions;
using RenpyParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Doki.UI
{

    public static class UiHandler
    {
        private static int NumberOfChoiceButtons { get; set; }
        private static List<UiEvent> UiEvents { get; set; }

        static UiHandler()
        {
            UiEvents = new List<UiEvent>();
            NumberOfChoiceButtons = 11;
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

        //public static void ShowChoiceMenu(List<ChoiceMenuButton> buttons)
        //{
        //    GameObject choiceMenuPanel = GameObject.Find("UI/UIGameCanvas/ChoiceMenuPanel");

        //    if (choiceMenuPanel.activeSelf || buttons.Count > NumberOfChoiceButtons)
        //        return;

        //    List<GameObject> buttonObjects = new List<GameObject>();

        //    for (int i = 0; i < NumberOfChoiceButtons; i++)
        //    {
        //        string buttonName = "RenpyUIButton";

        //        if (i > 0)
        //            buttonName += $" ({i})";

        //        GameObject choiceButtonObject = GameObject.Find($"UI/UIGameCanvas/ChoiceMenuPanel/{buttonName}");

        //        if (choiceButtonObject != null)
        //        {
        //            choiceButtonObject.SetActive(false);
        //            buttonObjects.Add(choiceButtonObject);
        //        }
        //    }

        //    for(int j = 0; j < buttons.Count; j++)
        //    {
        //        if (j < buttonObjects.Count)
        //        {
        //            GameObject matchingButtonObj = buttonObjects[j];

        //            if (matchingButtonObj != null)
        //            {
        //                TextMeshProUGUI buttonText = matchingButtonObj.GetComponentInChildren<TextMeshProUGUI>();
        //                Button buttonComponent = matchingButtonObj.GetComponent<Button>();

        //                if (buttonText != null)
        //                    buttonText.text = buttons[j].Text;

        //                if (buttonComponent != null)
        //                {
        //                    buttonComponent.onClick.RemoveAllListeners();
        //                    buttonComponent.onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
        //                    {
        //                        buttons[j].Clicked.Invoke();
        //                    }));
        //                }

        //                matchingButtonObj.SetActive(true);
        //            }
        //        }
        //    }

        //    choiceMenuPanel.SetActive(true);
        //}

        public static void ShowChoiceMenu(List<ChoiceMenuButton> buttons)
        {
            GameObject choiceMenuPanel = GameObject.Find("UI/UIGameCanvas/ChoiceMenuPanel");
            GameObject buttonPrefab = GameObject.Find("UI/UIGameCanvas/ChoiceMenuPanel/RenpyUIButton");

            if (choiceMenuPanel == null || buttonPrefab == null || choiceMenuPanel.activeSelf || buttons.Count > NumberOfChoiceButtons)
                return;

            foreach (Transform child in choiceMenuPanel.transform) 
                child.gameObject.SetActive(false);

            for (int i = 0; i < buttons.Count; i++)
            {
                GameObject choiceButtonObject = GameObject.Instantiate(buttonPrefab, choiceMenuPanel.transform);
                choiceButtonObject.name = $"ChoiceButton_{i}";

                RenpyButtonUI renpyButtonUIComponent = choiceButtonObject.GetComponent<RenpyButtonUI>();

                GameObject.Destroy(renpyButtonUIComponent);

                TextMeshProUGUI buttonText = choiceButtonObject.GetComponentInChildren<TextMeshProUGUI>();
                Button buttonComponent = choiceButtonObject.GetComponent<Button>();

                if (buttonText != null)
                    buttonText.text = buttons[i].Text;

                if (buttonComponent != null)
                {
                    int buttonIndex = i;

                    buttonComponent.onClick.AddListener(new UnityAction(() =>
                    {
                        buttons[buttonIndex].Clicked?.Invoke();

                        GameObject.Destroy(choiceButtonObject);

                        if (choiceMenuPanel.transform.childCount == 1)
                            HideChoiceMenu();
                    }));
                }

                choiceButtonObject.SetActive(true);
            }

            choiceMenuPanel.SetActive(true);
        }

        public static void HideChoiceMenu()
        {
            GameObject choiceMenuPanel = GameObject.Find("UI/UIGameCanvas/ChoiceMenuPanel");

            if (!choiceMenuPanel.activeSelf)
                return;

            choiceMenuPanel.SetActive(false);
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
