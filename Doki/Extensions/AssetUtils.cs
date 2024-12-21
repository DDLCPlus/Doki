using HarmonyLib;
using RenpyParser;
using RenPyParser.AssetManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityPS;

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

        public static Dictionary<string, ProxyAssetBundle> FakeBundles = []; //mod ID -> fake asset bundles
        public static Dictionary<string, AssetBundle> AssetBundles = [];
        public static Dictionary<string, Tuple<string, Tuple<string, bool>>> AssetsToBundles = [];

        public static Dictionary<string, string> QuickAudioAssetMap = []; //asset key -> bundle name

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

        public static AssetBundle LoadGameBundle(string label)
        {
            string text = Path.Combine(Application.streamingAssetsPath, $"AssetBundles/{PathHelpers.GetPlatformForAssetBundles(Application.platform)}/{label}.cy");
            AssetBundle assetBundle = AssetBundle.LoadFromStream(new XorFileStream(text, FileMode.Open, FileAccess.Read));

            if (assetBundle == null)
                ConsoleUtils.Error("Doki", new InvalidOperationException($"Trying to load an asset bundle that doesn't exist: {text}"));

            return assetBundle;
        }

        public static UnityEngine.Object LoadFromUnknownBundle(string key, Type type, bool secondMethod = false)
        {
            Tuple<string, Tuple<string, bool>> bundleDetails = GetBundleDetailsByAssetKey(key);
            AssetBundle foundBundle = AssetUtils.AssetBundles[bundleDetails.Item1];

            return secondMethod ? foundBundle.ForceLoadAsset(key, type) : foundBundle.LoadAsset(key, type);
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

        public static ProxyAssetBundle FindProxyBundleByAssetKey(string key)
        {
            foreach(var proxyBundle in FakeBundles)
            {
                var bundle = proxyBundle.Value;

                if (bundle.Exists(key))
                    return bundle;
            }

            return null;
        }

        public static AssetBundle GetPreciseAudioRelatedBundle(string assetKey)
        {
            if (assetKey == "7g2")
                return AssetUtils.AssetBundles["bgm-coarse00"];

            if (!QuickAudioAssetMap.ContainsKey(assetKey))
            {
                var outCome = AssetUtils.GetBundleDetailsByAssetKey(assetKey);

                if (outCome == default)
                    return null;

                return AssetUtils.AssetBundles[outCome.Item1];
            }
            else
                return AssetBundles[QuickAudioAssetMap[assetKey]];
        }

        public static GameObject FixLoad(string key)
        {
            GameObject objResult = (GameObject)LoadFromUnknownBundle(key, typeof(GameObject));
            ConsoleUtils.Debug("Doki", $"Do we have asset in db: {AssetUtils.AssetsToBundles.ContainsKey(key)} - fixLoad");
            return objResult;
        }
    }
}
