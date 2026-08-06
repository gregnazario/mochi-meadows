using System.IO;
using UnityEditor;
using UnityEngine;
using MochiMeadows.Art;

namespace MochiMeadows.EditorTools
{
    // Generates the kawaii app icon (lavender bg + Mochi + heart) at build time:
    // iOS/Android icon sets + PWA PNGs. Called automatically before mobile/WebGL builds.
    public static class IconGenerator
    {
        public static Texture2D RenderIcon(int size)
        {
            if (SpriteBank.MochiIdle == null) SpriteBank.BuildAll();
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];

            // soft lavender background with rounded corners
            float r = size * 0.22f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - r);
                    float dy = Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - r);
                    float d = Mathf.Max(dx, 0) * Mathf.Max(dx, 0) + Mathf.Max(dy, 0) * Mathf.Max(dy, 0);
                    float corner = (dx <= 0 && dy <= 0) ? 0 : Mathf.Sqrt(d);
                    float t = y / (float)size;
                    var col = Color.Lerp(new Color(0.42f, 0.36f, 0.62f), new Color(0.52f, 0.45f, 0.72f), t);
                    float a = 1f - Mathf.Clamp01(corner - r + 1.5f);
                    px[y * size + x] = new Color(col.r, col.g, col.b, a);
                }
            }

            // soft glow behind the cat
            float cx = size * 0.5f, cy = size * 0.56f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float glow = Mathf.Clamp01(1f - d / (size * 0.42f));
                    int i = y * size + x;
                    px[i] = Color.Lerp(px[i], new Color(1f, 0.95f, 0.98f, 1f), glow * 0.45f);
                }
            }

            // Mochi (white cat) centered, scaled up
            if (SpriteBank.MochiIdle != null)
            {
                var src = SpriteBank.MochiIdle.texture;
                int scale = Mathf.Max(1, (int)(size * 0.72f / src.width));
                int ox = (size - src.width * scale) / 2;
                int oy = (int)(size * 0.12f);
                for (int sy = 0; sy < src.height; sy++)
                {
                    for (int sx = 0; sx < src.width; sx++)
                    {
                        var c = src.GetPixel(sx, sy);
                        if (c.a <= 0.01f) continue;
                        for (int dy = 0; dy < scale; dy++)
                        {
                            for (int dx = 0; dx < scale; dx++)
                            {
                                int tx = ox + sx * scale + dx;
                                int ty = oy + (src.height - 1 - sy) * scale + dy;
                                if (tx < 0 || ty < 0 || tx >= size || ty >= size) continue;
                                int i = ty * size + tx;
                                px[i] = Color.Lerp(px[i], new Color(c.r, c.g, c.b, 1f), c.a);
                            }
                        }
                    }
                }
            }

            // pink heart, bottom right
            DrawHeart(px, size, size * 0.80f, size * 0.84f, size * 0.10f, new Color(1f, 0.62f, 0.75f, 1f));

            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static void DrawHeart(Color[] px, int size, float cx, float cy, float hr, Color color)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - cx) / hr, dy = (y - cy) / hr;
                    // classic heart implicit curve
                    float v = Mathf.Pow(dx * dx + dy * dy - 1f, 3) - dx * dx * dy * dy * dy;
                    if (v < 0.12f && v > -0.12f)
                    {
                        int i = y * size + x;
                        float aa = 1f - Mathf.Abs(v) / 0.12f;
                        px[i] = Color.Lerp(px[i], color, aa);
                    }
                }
            }
        }

        public static void WritePng(Texture2D tex, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }

        public static void EnsureIcons()
        {
            if (SpriteBank.MochiIdle == null) SpriteBank.BuildAll();
            var big = RenderIcon(1024);
            try
            {
                var kinds = PlayerSettings.GetSupportedIconKindsForPlatform(BuildTargetGroup.iOS);
                var arr = new Texture2D[kinds.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = big;
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, arr);
            }
            catch (System.Exception e) { Debug.LogWarning("[Icon] iOS icons skipped: " + e.Message); }
            try
            {
                var kinds = PlayerSettings.GetSupportedIconKindsForPlatform(BuildTargetGroup.Android);
                var arr = new Texture2D[kinds.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = big;
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, arr);
            }
            catch (System.Exception e) { Debug.LogWarning("[Icon] Android icons skipped: " + e.Message); }
            Debug.Log("[Icon] icon set for iOS + Android");

            // PWA PNGs
            WritePng(big, "Assets/WebGLTemplates/MochiPWA/icons/icon-512.png");
            WritePng(RenderIcon(192), "Assets/WebGLTemplates/MochiPWA/icons/icon-192.png");
            WritePng(RenderIcon(180), "Assets/WebGLTemplates/MochiPWA/icons/apple-touch-icon.png");
        }
    }
}
