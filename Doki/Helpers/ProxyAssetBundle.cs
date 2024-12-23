using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Doki.Extensions
{
    public class ProxyAssetBundle
    {
        public AssetBundle FakeInstance { get; set; }
        public Dictionary<string, string> AssetsMap = []; // asset key -> asset path

        public ProxyAssetBundle()
        {
            FakeInstance = AssetUtils.LoadInternalAssetBundle();

            //FakeInstance = (AssetBundle)AccessTools.Constructor(typeof(AssetBundle)).Invoke([]);
        }

        public bool Exists(string key) => AssetsMap.ContainsKey(key);
        public void Map(string key, string path) => AssetsMap[key] = path;
        public string ToPath(string key) => AssetsMap[key];

        private AudioClip LoadAudioClip(string path)
        { // Thanks kizby :3
            AudioType audioType = path.ToLower().EndsWith(".ogg") ? AudioType.OGGVORBIS : path.ToLower().EndsWith(".mp3") ? AudioType.MPEG : AudioType.UNKNOWN;
            
            if (audioType == AudioType.UNKNOWN)
            {
                ConsoleUtils.Error("ProxyAssetBundle", new InvalidOperationException($"Unsupported audio file type: {path}"));
                return null;
            }

            if (!File.Exists(path))
            {
                ConsoleUtils.Error("ProxyAssetBundle", new InvalidOperationException($"Audio file does not exist: {path}"));
                return null;
            }

            byte[] fileBytes = File.ReadAllBytes(path);
            if (fileBytes == null || fileBytes.Length == 0)
            {
                ConsoleUtils.Error("ProxyAssetBundle", new InvalidOperationException($"Failed to read audio file: {path}"));
                return null;
            }

            string tempFilePath = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(tempFilePath, fileBytes);

                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(new Uri(tempFilePath).AbsoluteUri, audioType))
                {
                    request.SendWebRequest();
                    while (!request.isDone) { }

                    if (request.responseCode == 200 || request.responseCode == 204)
                        return DownloadHandlerAudioClip.GetContent(request);
                    else {
                        ConsoleUtils.Error("ProxyAssetBundle", new InvalidOperationException($"Failed to load audio file: {path}"));
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleUtils.Error("ProxyAssetBundle", ex, $"Failed to load audio file.");
                return null;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtils.Error("ProxyAssetBundle", ex, $"Failed to delete temp path -> {tempFilePath} for audio file: {path} ...");
                    }
                }
            }
        }

        private Texture2D LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                ConsoleUtils.Error("ProxyAssetBundle", new InvalidOperationException($"Texture does not exist: {path}"));
                return null;
            }

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        private Sprite LoadSprite(string path, float ppu = 1f)
        {
            Texture2D Texture = LoadTexture(path);

            if (Texture == null)
            {
                ConsoleUtils.Error("ProxyAssetBundle", new InvalidOperationException($"Texture is null: {path}"));
                return null;
            }

            return Sprite.Create(Texture, new Rect(0, 0, Texture.width, Texture.height), new Vector2(0.5f, 0.5f), ppu);
        }

        private GameObject LoadBackgroundPrefab(string path)
        {
            string assetname = Path.GetFileNameWithoutExtension(path);
            string pathWithoutExtension = Path.Combine(Path.GetDirectoryName(path), assetname);
            string endingExtension = File.Exists($"{pathWithoutExtension}.png") ? ".png" : (File.Exists($"{pathWithoutExtension}.jpg") ? ".jpg" : null);

            Sprite loadedSprite = LoadSprite($"{pathWithoutExtension}{endingExtension}");
            GameObject parentObject = GameObject.Find($"OnScreen/master/bg {assetname}");

            if (loadedSprite == null || parentObject == null)
                return null;

            GameObject bgInsides = new GameObject(assetname) {
                transform = {
                    parent = parentObject.transform
                }
            };

            SpriteRenderer spriteRenderer = bgInsides.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = loadedSprite;
            spriteRenderer.sortingOrder = 0;

            return bgInsides;
        }

        public object Load(string type, string path)
        { 
            switch(type)
            {
                case "UnityEngine.AudioClip":
                    return LoadAudioClip(path);
                case "UnityEngine.Texture2D":
                    return LoadTexture(path);
                case "UnityEngine.Sprite":
                    return LoadSprite(path);
                case "UnityEngine.GameObject":
                    return LoadBackgroundPrefab(path);
                default:
                    ConsoleUtils.Log("ProxyAssetBundle", $"No method to handle asset type: {type}");
                    return null;
            }
        }
    }
}
