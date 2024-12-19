using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Doki.Extensions
{
    public static class AssetUtils
    {
        public static IDictionary<string, string> alternateImageNames = new Dictionary<string, string>
        {
            {
                "poem_end_clearall_fr-fr",
                "poem_end_clearall_fr-fr2"
            },
            {
                "poem_end_clearall_ko",
                "poem_end_clearall_ko_2"
            }
        };

        public static Dictionary<string, AssetBundle> AssetBundles = [];
        public static Dictionary<string, Tuple<string, Tuple<string, bool>>> AssetsToBundles = [];

        private static readonly MethodInfo loadAssetInternal = AccessTools.Method(typeof(AssetBundle), "LoadAsset_Internal");
        private static readonly MethodInfo loadFromMemoryMethod = AccessTools.Method(typeof(AssetBundle), "LoadFromMemory_Internal");

        public static AssetBundle LoadAssetBundle(string path)
        {
            AssetBundle bundle = null;

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var tempStream = new MemoryStream((int)stream.Length))
            {
                stream.CopyTo(tempStream);
                bundle = (AssetBundle)loadFromMemoryMethod.Invoke(null, [tempStream.ToArray(), (uint)0]);
                bundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            return bundle;
        }

        public static T ForceLoadAsset<T>(this AssetBundle bundle, string name) where T : UnityEngine.Object =>
            (T)loadAssetInternal.Invoke(bundle, [name, typeof(T)]);

        public static UnityEngine.Object ForceLoadAsset(this AssetBundle bundle, string name, Type type) =>
            (UnityEngine.Object)loadAssetInternal.Invoke(bundle, [name, type]);

        public static Tuple<string, Tuple<string, bool>> GetBundleDetailsByAssetKey(string assetKey)
        {
            if (AssetUtils.AssetsToBundles.TryGetValue(assetKey, out Tuple<string, Tuple<string, bool>> output))
                return output;
            else return default;
        }

        public static UnityEngine.Object LoadFromUnknownBundle(string key, Type type, bool secondMethod = false)
        {
            Tuple<string, Tuple<string, bool>> bundleDetails = GetBundleDetailsByAssetKey(key);
            AssetBundle foundBundle = AssetUtils.AssetBundles[bundleDetails.Item1];

            return secondMethod ? foundBundle.ForceLoadAsset(key, type) : foundBundle.LoadAsset(key, type);
        }

        public static AssetBundle GetPreciseAudioRelatedBundle(string assetKey)
        {
            if (assetKey == "7g2")
                return AssetUtils.AssetBundles["bgm-coarse00"];

            string[] baseAudioBundles = {
                "bgm-coarse",
                "bgm-coarse00",
                "bgm-ddlcplus-coarse",
                "sfx-coarse"
            };

            // If assetKey is in baseAudioBundles, return it directly
            foreach(var baseAudioBundle in baseAudioBundles)
            {
                AssetBundle baseBundle = AssetUtils.AssetBundles[baseAudioBundle];

                if (baseBundle.GetAllAssetNames().Any(assetName => Path.GetFileNameWithoutExtension(assetName) == assetKey))
                    return baseBundle;
            }

            foreach (var bundleEntry in AssetUtils.AssetBundles)
            {
                string assetBundleName = bundleEntry.Key;
                AssetBundle bundle = bundleEntry.Value;

                if (bundle.GetAllAssetNames().Any(assetName => Path.GetFileNameWithoutExtension(assetName) == assetKey))
                    return bundle;
            }

            return null;
        }

        public static GameObject FixLoad(string key)
        {
            GameObject objResult = (GameObject)LoadFromUnknownBundle(key, typeof(GameObject));
            ConsoleUtils.Debug("Doki", $"Do we have asset in db: {AssetUtils.AssetsToBundles.ContainsKey(key)} - fixLoad");
            return objResult;
        }
    }
}
