using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MochiMeadows.Art
{
    // Custom-art pipeline: replace any procedural sprite by dropping a PNG
    // with the same name into the art folder.
    //
    //   StreamingAssets/art/<SpriteName>.png        (shipped with the game)
    //   persistentDataPath/art_export/<name>.png    (dev round-trip: edit here, relaunch)
    //
    // PNGs may be any resolution; each is scaled to keep the original sprite's
    // world size, so you can redraw at higher fidelity.
    public static class CustomSpriteLoader
    {
        static readonly Dictionary<string, Sprite> loaded = new Dictionary<string, Sprite>();
        static bool done;

        public static IEnumerator LoadAll()
        {
            if (done) yield break;
            done = true;

            var files = new List<string>();

            // 1) StreamingAssets/art (bundled; WebGL loads via UnityWebRequest)
            var bundledDir = Path.Combine(Application.streamingAssetsPath, "art");
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                yield return CollectBundledWebGL(bundledDir, files);
            }
            else if (Directory.Exists(bundledDir))
            {
                files.AddRange(Directory.GetFiles(bundledDir, "*.png"));
            }

            // 2) persistentDataPath/art_export (dev round-trip, overrides bundled)
            var devDir = Path.Combine(Application.persistentDataPath, "art_export");
            if (Directory.Exists(devDir))
                files.AddRange(Directory.GetFiles(devDir, "*.png"));

            foreach (var f in files)
            {
                var name = Path.GetFileNameWithoutExtension(f);
                var tex = LoadTexture(f);
                if (tex == null) continue;
                var sprite = ToSprite(tex, name);
                loaded[name] = sprite;
            }
            ApplyToBank();
        }

        static IEnumerator CollectBundledWebGL(string dir, List<string> files)
        {
            // On WebGL, Directory APIs can't see StreamingAssets; the game
            // bundle embeds a manifest of bundled art names (see exporter).
            var manifestUrl = Path.Combine(dir, "manifest.txt").Replace('\\', '/');
            var req = UnityEngine.Networking.UnityWebRequest.Get(manifestUrl);
            yield return req.SendWebRequest();
            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success) yield break;
            foreach (var line in req.downloadHandler.text.Split('\n'))
            {
                var name = line.Trim();
                if (name.Length == 0) continue;
                files.Add(Path.Combine(dir, name + ".png").Replace('\\', '/'));
            }
        }

        static Texture2D LoadTexture(string path)
        {
            byte[] bytes = null;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                // synchronous fallback for the dev folder is impossible on
                // WebGL; the coroutine path loads bundled art, dev folder is
                // desktop-only convenience.
                return null;
            }
            try { bytes = File.ReadAllBytes(path); }
            catch (Exception) { return null; }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        // Keep the ORIGINAL sprite's world size regardless of the PNG's pixel size.
        static Sprite ToSprite(Texture2D tex, string name)
        {
            var original = GetOriginal(name);
            float ppu = 16f;
            if (original != null)
            {
                float worldW = original.rect.width / original.pixelsPerUnit;
                ppu = tex.width / worldW;
            }
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        }

        static Sprite GetOriginal(string name)
        {
            var f = typeof(SpriteBank).GetField(name, BindingFlags.Public | BindingFlags.Static);
            if (f == null) return null;
            return f.GetValue(null) as Sprite;
        }

        static void ApplyToBank()
        {
            int replaced = 0;
            var fields = typeof(SpriteBank).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(Sprite)) continue;
                if (!loaded.TryGetValue(f.Name, out var sprite)) continue;
                try { f.SetValue(null, sprite); replaced++; }
                catch (Exception) { }
            }
            if (replaced > 0)
                Debug.Log($"[Art] Loaded {replaced} custom sprites from art folder");
        }
    }

    // Describes one exported sprite (used by the exporter manifest).
    [Serializable]
    public class SpriteExportEntry
    {
        public string name;
        public int width;
        public int height;
    }
}
