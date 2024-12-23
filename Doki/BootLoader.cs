using Doki.Core;
using Doki.Extensions;
using System;
using System.Reflection;
using UnityEngine;

namespace Doki
{
    public static class BootLoader
    {
        public static bool DontLoad = false;
        public static bool NoConsole = false;

        public static void Load()
        {
            if (!DontLoad)
            {
                ConsoleUtils.ShowConsole("Doki - A DDLC+ Project");
                ConsoleUtils.Log("Doki", "Created GameObject to handle BootLoader.. initializing..");
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(onResolveAssembly);
                LoadHarmony();

                var obj = new GameObject();
                obj.AddComponent<Main>();
                UnityEngine.Object.DontDestroyOnLoad(obj);
            }
        }

        private static Assembly onResolveAssembly(object sender, ResolveEventArgs args)
        {
            return EmbeddedAssembly.Get(args.Name);
        }

        private static void LoadHarmony()
        {
            ConsoleUtils.Log("Doki", "Loading Harmony...");
            EmbeddedAssembly.Load("Doki.Properties.0Harmony.dll", "0Harmony.dll");
        }
    }
}
