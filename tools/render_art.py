#!/usr/bin/env python3
"""Render MochiMeadows ASCII pixel art from SpriteBank.cs in the terminal (truecolor)."""
import re, sys, subprocess

src = open('/Users/greg/git/kawaii-game/MochiMeadows/Assets/Scripts/Runtime/Art/SpriteBank.cs').read()

# palette mapping
pal = {
 'h':'#8C5A44','H':'#6B4131','c':'#FFE9D6','k':'#4A2B3A','w':'#FFFFFF','b':'#FFA8B8',
 'p':'#FF9BB3','P':'#FF7A9E','s':'#FFF6E5','d':'#8C5A44',
 'o':'#FF9E5E','g':'#8FD88A','t':'#FFD9A8',
 'G':'#A2D98C','f':'#FFD9A8','l':'#8FD88A','L':'#7CC477','r':'#FFB7D0','y':'#FFE49E',
 'u':'#BFE3FF','U':'#A5D8F5','O':'#FFD9A8','R':'#FFD9A8','n':'#FFF3C4',
 'S':'#B88960','W':'#A8DBF2','m':'#FFFFFF','D':'#C6925C','Y':'#FFE49E',
 'B':'#FF9BB3','C':'#FFE9D6',
}
def hex2rgb(h):
    h=h.lstrip('#'); return tuple(int(h[i:i+2],16) for i in (0,2,4))

def render(rows):
    h=len(rows); w=max(len(r) for r in rows)
    out=[]
    for r in rows:
        line=[]
        for x in range(w):
            c = r[x] if x < len(r) else '.'
            if c in ' .': line.append('\x1b[40m  ')  # transparent: dark bg
            else:
                rgb=hex2rgb(pal.get(c,'#FF00FF'))
                line.append(f'\x1b[48;2;{rgb[0]};{rgb[1]};{rgb[2]}m  ')
        line.append('\x1b[0m')
        out.append(''.join(line))
    return '\n'.join(out)

def show(name, rows):
    print(f'--- {name} ---')
    print(render(rows))
    print()

# extract "Name = S("Name", Pal, \n rows ...);" blocks
pat = re.compile(r'([A-Za-z0-9_]+) = S\("([A-Za-z0-9_]+)",\s*\w+Pal,(.*?)\);', re.S)
count=0
for m in pat.finditer(src):
    var, name, body = m.groups()
    rows = re.findall(r'"([^"]*)"', body)
    if not rows: continue
    # validate width
    wset={len(r) for r in rows}
    if len(wset)>1:
        print(f'!! {name}: inconsistent widths {sorted(wset)}')
    if len(rows)>20: rows=rows[:20]
    count+=1
    if name in ('Grass0','Soil0','SoilWet','WaterTile','Pond0'):  # repetitive tiles: skip unless requested
        continue
    show(name, rows)
print(f'{count} sprites found')
