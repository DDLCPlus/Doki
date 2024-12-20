using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Doki.Extensions
{
    public class ProxyAssetBundle
    {
        public AssetBundle FakeInstance { get; set; }
        private Dictionary<string, string> AssetsMap = []; // asset key -> asset path

        public ProxyAssetBundle()
        {
            FakeInstance = (AssetBundle)AccessTools.Constructor(typeof(AssetBundle)).Invoke([]);
        }

        public bool Exists(string key) => AssetsMap.ContainsKey(key);
        public void Map(string key, string path) => AssetsMap[key] = path;
        public string ToPath(string key) => AssetsMap[key];
    }
}
