using Doki.Mods;
using HarmonyLib;
using RenPyParser.Images;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static Dictionary<string, AssetBundle> AssetBundles = new Dictionary<string, AssetBundle>();

        public static Dictionary<string, Tuple<string, Tuple<string, bool>>> AssetsToBundles = new Dictionary<string, Tuple<string, Tuple<string, bool>>>();

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

            return (T)loadAssetInternal.Invoke(bundle, new object[] { name, typeof(T) });
        }

        public static UnityEngine.Object ForceLoadAsset(this AssetBundle bundle, string name, Type type)
        {
            var loadAssetInternal = AccessTools.Method(typeof(AssetBundle), "LoadAsset_Internal");

            return (UnityEngine.Object)loadAssetInternal.Invoke(bundle, new object[] { name, type });
        }

        public static Tuple<string, Tuple<string, bool>> GetBundleDetailsByAssetKey(string assetKey)
        {
            if (AssetUtils.AssetsToBundles.TryGetValue(assetKey, out Tuple<string, Tuple<string, bool>> output))
                return output;
            else return default;
        }

        public static UnityEngine.Object LoadFromUnknownBundle(string key, Type type)
        {
            Tuple<string, Tuple<string, bool>> bundleDetails = GetBundleDetailsByAssetKey(key);

            //if (bundleDetails == null)
            //    return new UnityEngine.Object();

            AssetBundle foundBundle = AssetUtils.AssetBundles[bundleDetails.Item1];

            //AssetBundle foundBundle = AssetUtils.AssetBundles[bundleDetails.Item1];

            return foundBundle.LoadAsset(key, type);
            //return bundleDetails.Item2 ? foundBundle.ForceLoadAsset(key, type) : foundBundle.LoadAsset(key, type);
        }

        public static GameObject FixLoad(string key)
        {
            GameObject objResult = (GameObject)LoadFromUnknownBundle(key, typeof(GameObject));

            Console.WriteLine("Do we have asset in db: " + AssetUtils.AssetsToBundles.ContainsKey(key) + " - fixLoad");

            return objResult;
        }
    }
}
