using Doki.Game;
using Doki.Mods;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Doki.Utils
{
    public static class AssetUtils
    {
        public static AssetBundle LoadAssetBundle(string path)
        {
            AssetBundle bundle = null;

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var tempStream = new MemoryStream((int)stream.Length))
            {
                stream.CopyTo(tempStream);

                var loadFromMemoryMethod = AccessTools.Method(typeof(AssetBundle), "LoadFromMemory_Internal");

                bundle = (AssetBundle)loadFromMemoryMethod.Invoke(null, new object[] { tempStream.ToArray(), (uint)0 });
                bundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            return bundle;
        }

        public static T ForceLoadAsset<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object
        {
            var loadAssetInternal = AccessTools.Method(typeof(AssetBundle), "LoadAsset_Internal");

            return (T)((object)loadAssetInternal.Invoke(bundle, new object[] { name, typeof(T) }));
        }
    }
}
