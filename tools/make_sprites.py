#!/usr/bin/env python3
"""
Generate PNG sprite files from SpriteBank.cs ASCII art definitions.
Outputs 128x128 PNGs (8x scale) into StreamingAssets/art/.
Usage: python3 tools/make_sprites.py [--scale N]
"""
import re, sys, os
from PIL import Image

SPRITEBANK = os.path.join(os.path.dirname(__file__), '..', 'MochiMeadows/Assets/Scripts/Runtime/Art/SpriteBank.cs')
OUT_DIR    = os.path.join(os.path.dirname(__file__), '..', 'MochiMeadows/Assets/StreamingAssets/art')
SCALE      = int(sys.argv[sys.argv.index('--scale')+1]) if '--scale' in sys.argv else 8

os.makedirs(OUT_DIR, exist_ok=True)

PAL = {
    'h': (140, 90,  68),
    'H': (107, 65,  49),
    'l': (185, 145, 120),
    'c': (255, 233, 214),
    'k': (74,  43,  58),
    'w': (255, 255, 255),
    'b': (255, 168, 184),
    'p': (255, 155, 179),
    'P': (255, 122, 158),
    's': (255, 246, 229),
    'd': (140, 90,  68),
    't': (255, 217, 168),
    'r': (255, 183, 208),
    'y': (255, 228, 158),
    'o': (255, 158,  94),
    'g': (143, 216, 138),
    'G': (162, 217, 140),
    'f': (255, 217, 168),
    'L': (124, 196, 119),
    'n': (243, 243, 196),
    'S': (184, 137,  96),
    'W': (168, 219, 242),
    'D': (198, 146,  92),
    'O': (122, 106,  78),
    'u': (191, 227, 255),
    'U': (165, 216, 245),
    'R': (255, 217, 168),
    'm': (184, 242, 212),
    'B': (255, 155, 179),
    'C': (255, 246, 229),
    'Y': (255, 228, 158),
    'M': (255, 183, 208),
    'z': (139, 134, 184),
}
TRANSPARENT = (0, 0, 0, 0)
OUTLINE     = (92, 69, 84, 224)

def parse_spritemap(src):
    sprites = {}
    pat = re.compile(
        r'=\s*S\("([A-Za-z0-9_]+)",\s*(?:true,\s*|false,\s*)?\w+Pal,\s*((?:"[^"]*"\s*,?\s*)+)\)',
        re.S
    )
    for m in pat.finditer(src):
        name = m.group(1)
        rows = re.findall(r'"([^"]*)"', m.group(2))
        if rows:
            sprites[name] = rows
    return sprites

def make_png(rows, scale, outline=True):
    art_w = max(len(r) for r in rows)
    art_h = len(rows)
    pixels = []
    for y, row in enumerate(rows):
        line = []
        for x in range(art_w):
            c = row[x] if x < len(row) else '.'
            if c in '. ':
                line.append(None)
            else:
                line.append(PAL.get(c, (255, 0, 255)))
        pixels.append(line)

    if outline:
        outlined = [row[:] for row in pixels]
        for y in range(art_h):
            for x in range(art_w):
                if pixels[y][x] is None:
                    has_neighbour = any(
                        pixels[ny][nx] is not None
                        for dy in (-1, 0, 1) for dx in (-1, 0, 1)
                        if 0 <= (ny := y+dy) < art_h and 0 <= (nx := x+dx) < art_w
                    )
                    if has_neighbour:
                        outlined[y][x] = 'OUTLINE'
        pixels = outlined

    img = Image.new('RGBA', (art_w * scale, art_h * scale), (0, 0, 0, 0))
    px = img.load()
    for y in range(art_h):
        for x in range(art_w):
            col = pixels[y][x]
            if col is None:
                rgba = TRANSPARENT
            elif col == 'OUTLINE':
                rgba = OUTLINE
            else:
                rgba = (col[0], col[1], col[2], 255)
            for dy in range(scale):
                for dx in range(scale):
                    px[x*scale + dx, (art_h - 1 - y)*scale + dy] = rgba
    return img

def main():
    src = open(SPRITEBANK).read()
    sprites = parse_spritemap(src)
    print(f"Found {len(sprites)} sprites in SpriteBank.cs")
    saved = 0
    for name, rows in sprites.items():
        try:
            img = make_png(rows, SCALE, outline=True)
            path = os.path.join(OUT_DIR, f"{name}.png")
            img.save(path)
            saved += 1
        except Exception as e:
            print(f"  WARN {name}: {e}")
    print(f"Saved {saved} PNGs to {OUT_DIR}/")

if __name__ == '__main__':
    main()
