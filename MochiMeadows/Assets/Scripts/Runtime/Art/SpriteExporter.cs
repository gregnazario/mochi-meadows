using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MochiMeadows.Art
{
    // Dev tool: `-export-art` writes every game sprite as an editable PNG into
    // persistentDataPath/art_export/. Edit the PNGs and relaunch (or copy them
    // into StreamingAssets/art/) — the loader picks them up.
    public static class SpriteExporter
    {
        public static string ExportAll()
        {
            string dir = Path.Combine(Application.persistentDataPath, "art_export");
            Directory.CreateDirectory(dir);
            int count = 0;
            var manifest = new System.Text.StringBuilder();

            var fields = typeof(SpriteBank).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(Sprite)) continue;
                var sprite = f.GetValue(null) as Sprite;
                if (sprite == null || sprite.texture == null) continue;
                var tex = sprite.texture;
                try
                {
                    var bytes = tex.EncodeToPNG();
                    string path = Path.Combine(dir, f.Name + ".png");
                    File.WriteAllBytes(path, bytes);
                    manifest.AppendLine(f.Name);
                    count++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Art] export {f.Name} failed: {e.Message}");
                }
            }

            // manifest for the WebGL bundled loader
            File.WriteAllText(Path.Combine(dir, "manifest.txt"), manifest.ToString());
            Debug.Log($"[Art] Exported {count} sprites to {dir}");
            return dir;
        }
    }
}
