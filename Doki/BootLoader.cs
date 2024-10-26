using Doki.Utils;
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Doki
{
    public static class BootLoader
    {
        public static bool DontLoad = false;

        public static void Load()
        {
            if (!DontLoad)
            {
                ConsoleUtils.ShowConsole("Doki - A DDLC+ Project");

                ConsoleUtils.Log("Created GameObject to handle BootLoader.. initializing..");

                var obj = new GameObject();

                obj.AddComponent<Main>();

                UnityEngine.Object.DontDestroyOnLoad(obj);
            }
        }
    }
}
