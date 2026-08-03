using System;
using System.Collections.Generic;
using UnityEngine;

namespace MochiMeadows.Art
{
    // Kawaii pixel palette — every color is pastel.
    public static class Palette
    {
        public static readonly Color StrawberryPink = Hex("#FF9BB3");
        public static readonly Color DeepPink = Hex("#FF7A9E");
        public static readonly Color Blush = Hex("#FFA8B8");
        public static readonly Color Cream = Hex("#FFF6E5");
        public static readonly Color Mint = Hex("#B8F2D4");
        public static readonly Color Lavender = Hex("#D6C4F5");
        public static readonly Color BabyBlue = Hex("#BFE3FF");
        public static readonly Color Peach = Hex("#FFD9A8");
        public static readonly Color Orange = Hex("#FF9E5E");
        public static readonly Color Chocolate = Hex("#8C5A44");
        public static readonly Color DarkChoco = Hex("#6B4131");
        public static readonly Color Skin = Hex("#FFE9D6");
        public static readonly Color EyePlum = Hex("#4A2B3A");
        public static readonly Color Grass = Hex("#AEE29A");
        public static readonly Color GrassDark = Hex("#A2D98C");
        public static readonly Color Soil = Hex("#C99E74");
        public static readonly Color SoilShade = Hex("#B88960");
        public static readonly Color WetSoil = Hex("#8A6A4E");
        public static readonly Color WetSoilShade = Hex("#7A5C42");
        public static readonly Color Path = Hex("#F2DDBB");
        public static readonly Color PathShade = Hex("#E4C99F");
        public static readonly Color Water = Hex("#A8DBF2");
        public static readonly Color WaterDeep = Hex("#8ECBEA");
        public static readonly Color Wood = Hex("#DEAC76");
        public static readonly Color WoodShade = Hex("#C6925C");
        public static readonly Color Leaf = Hex("#8FD88A");
        public static readonly Color LeafDark = Hex("#7CC477");
        public static readonly Color PetalPink = Hex("#FFB7D0");
        public static readonly Color White = Hex("#FFFFFF");
        public static readonly Color Yellow = Hex("#FFE49E");
        public static readonly Color NightSky = Hex("#8B86B8");
        public static readonly Color Sky = Hex("#A5D8F5");
        public static readonly Color DawnSky = Hex("#FFD9D2");
        public static readonly Color SunsetSky = Hex("#FFB7B0");
        public static readonly Color TreeTrunk = Hex("#B98A5C");
        public static readonly Color StarGold = Hex("#FFF3C4");

        public static Color Hex(string hx)
        {
            hx = hx.TrimStart('#');
            return new Color(
                (float)Convert.ToInt32(hx.Substring(0, 2), 16) / 255f,
                (float)Convert.ToInt32(hx.Substring(2, 2), 16) / 255f,
                (float)Convert.ToInt32(hx.Substring(4, 2), 16) / 255f,
                1f);
        }
    }

    // A pixel sprite defined as ASCII rows; '.' = transparent.
    // Char -> color via a per-sprite palette map.
    public class PixelSprite
    {
        public string Name;
        public int Width;
        public int Height;
        public Color[] Pixels; // row-major

        public static PixelSprite From(string name, string[] rows, Dictionary<char, Color> palette)
        {
            int h = rows.Length;
            int w = 0;
            foreach (var r in rows)
                if (r.Length > w) w = r.Length;

            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                string row = rows[y];
                for (int x = 0; x < w; x++)
                {
                    char c = x < row.Length ? row[x] : '.';
                    px[y * w + x] = (c == '.' || c == ' ') ? Color.clear
                        : (palette.TryGetValue(c, out var col) ? col : Color.magenta);
                }
            }
            return new PixelSprite { Name = name, Width = w, Height = h, Pixels = px };
        }

        // Pixel (x,y) at 4x scale with optional vertical flip, drawn into dest.
        public void Blit(Color[] dest, int destW, int destH, int ox, int oy, int scale, bool flipY, bool flipX)
        {
            for (int y = 0; y < Height; y++)
            {
                int sy = flipY ? Height - 1 - y : y;
                for (int x = 0; x < Width; x++)
                {
                    int sx = flipX ? Width - 1 - x : x;
                    Color c = Pixels[sy * Width + sx];
                    if (c.a <= 0f) continue;
                    for (int dy = 0; dy < scale; dy++)
                    {
                        for (int dx = 0; dx < scale; dx++)
                        {
                            int px = ox + x * scale + dx;
                            int py = oy + (Height - 1 - y) * scale + dy;
                            if (px < 0 || py < 0 || px >= destW || py >= destH) continue;
                            dest[py * destW + px] = c;
                        }
                    }
                }
            }
        }

        // 1-art-pixel outline around opaque pixels (8-neighbour), for crisp silhouettes.
        public PixelSprite Outlined(Color outlineColor)
        {
            var result = new PixelSprite { Name = Name + "_o", Width = Width, Height = Height, Pixels = new Color[Pixels.Length] };
            System.Array.Copy(Pixels, result.Pixels, Pixels.Length);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    if (Pixels[i].a > 0.01f) continue;
                    bool neighbor = false;
                    for (int dy = -1; dy <= 1 && !neighbor; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                            if (Pixels[ny * Width + nx].a > 0.01f) { neighbor = true; break; }
                        }
                    }
                    if (neighbor) result.Pixels[i] = outlineColor;
                }
            }
            return result;
        }
    }

    // Turns PixelSprites into textures/sprites and bundles them.
    public static class SpriteFactory
    {
        public const int PixelsPerUnit = 16;
        const int Scale = 4; // 1 art pixel -> 4 texture px, crisp upscaled look

        public static Texture2D ToTexture(PixelSprite ps)
        {
            int w = ps.Width * Scale;
            int h = ps.Height * Scale;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            ps.Blit(px, w, h, 0, 0, Scale, false, false);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        public static Sprite ToSprite(PixelSprite ps)
        {
            var tex = ToTexture(ps);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        public static Sprite SpriteFrom(Color[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        // Procedural soft-circle sprite (for hearts, particles, etc.)
        public static Sprite SoftCircle(int size, Color color)
        {
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - r) / r;
                    float dy = (y + 0.5f - r) / r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < 1f)
                    {
                        Color c = color;
                        c.a = Mathf.Clamp01(1f - d);
                        px[y * size + x] = c;
                    }
                }
            }
            return SpriteFrom(px, size, size);
        }

        static System.Collections.Generic.Dictionary<int, Sprite> arcCache;
        public static Sprite ArcSprite(Color color)
        {
            int key = color.GetHashCode();
            if (arcCache == null) arcCache = new System.Collections.Generic.Dictionary<int, Sprite>();
            if (arcCache.TryGetValue(key, out var cached)) return cached;
            const int size = 20;
            var px = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float r = size * 0.46f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d1 = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float d2 = Mathf.Sqrt((x - cx - r * 0.55f) * (x - cx - r * 0.55f) + (y - cy) * (y - cy));
                    if (d1 < r && d2 > r * 0.62f)
                    {
                        px[y * size + x] = new Color(color.r, color.g, color.b, 1f);
                    }
                }
            }
            var spr = SpriteFrom(px, size, size);
            arcCache[key] = spr;
            return spr;
        }

        public static Sprite HeartSprite(Color color)
        {
            string[] art =
            {
                ".rr..rr.",
                "rrrrrrrr",
                "rrrrrrrr",
                ".rrrrrr.",
                "..rrrr..",
                "...rr...",
                "........",
            };
            return ToSprite(PixelSprite.From("heart", art, new Dictionary<char, Color> { ['r'] = color }));
        }
    }
}
