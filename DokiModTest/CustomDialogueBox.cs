using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DokiModTest
{
    public class CustomDialogueBox : MonoBehaviour
    {
        private Color dialogueColor { get; set; }
        private GameObject gameObj { get; set; }
        private Image image { get; set; }

        public void Awake()
        {
            gameObj = GameObject.Find("UI/UIGameCanvas/DialogueWindowCanvas/Window/Image");
            image = gameObj.GetComponent<Image>();
            dialogueColor = new Color(0.26f, 0.5472f, 1, 1);
        }

        public void Update()
        {
            if (image == null)
                return;

            if (image.color != dialogueColor)
                image.color = dialogueColor;
        }

        private IEnumerator DoTheCoroutine()
        {
            while (image.color == dialogueColor)
                yield return null;

            image.color = dialogueColor;
        }
    }
}
