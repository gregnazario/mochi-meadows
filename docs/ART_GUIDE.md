# Art Guide — swapping in your own sprites

All game art is procedural pixel art by default. You can replace **any** sprite
by dropping a PNG with the matching name into the art folder. The engine does
the rest.

## Quick start (round-trip)

1. Run the game once with `-export-art`:

   ```sh
   "Mochi Meadows.app/Contents/MacOS/Mochi Meadows" -export-art
   ```

   This writes every sprite as an editable PNG to
   `~/Library/Application Support/Mochi Meadows/Mochi Meadows/art_export/`
   (87 files, one per sprite, named exactly like the code references).

2. **Edit any PNG** (they're 64×64 for 16×16 sprites — you can redraw at any
   resolution, e.g. 128×128, and it scales correctly).

3. **Relaunch the game** — the edited PNGs load automatically from the dev
   folder. To ship them, copy your PNGs into `StreamingAssets/art/` (a new
   folder at `MochiMeadows/Assets/StreamingAssets/art/`) before building.

## Naming

Names match the `SpriteBank` fields. Key ones:

| Group | Names |
|---|---|
| Player | `PlayerDown0` `PlayerDown1` `PlayerUp0` `PlayerUp1` `PlayerSide0` `PlayerSide1` |
| Mochi | `MochiIdle` `MochiWalk0` `MochiWalk1` |
| Chickens | `Chicken0..2` `ChickenPink0..2` |
| Tiles | `Grass0..2` `Path0` `Soil0` `Soil1` `SoilWet` `WaterTile` `Fence0` `Fence1` `LockedTile` |
| Crops | `Strawberry0..3` `Blueberry0..3` `Pumpkitten0..3` `Sakura0..3` `Melon0..3` `MochiRice0..3` |
| Objects | `ShopStand` `QuestBoard` `KitchenTable` `Blanket` `TreeTrunkTile` `Egg` `Honey` `Deed` |
| Decor | `Fence`-related via `Lantern` `Gnome` `Beehive` (see `DecorSprite`) |
| Fish | `Goldfish` `BubbleFish` `SakuraFish` |
| Items/UI | `Coin` `Heart` `HeartPink` `Sparkle` `WaterDrop` `MusicNote` `HoeIcon` `CanIcon` `HandIcon` `Sun` `Moon` `Star` `Cloud` `Zzz` |

The exporter writes a `manifest.txt` too — that's what the WebGL build uses to
find bundled art.

## Rules for your PNGs

- **Keep the pivot**: the sprite pivots at the center-bottom of the character
  art as drawn (0.5, 0.5) — draw characters standing on the bottom-center.
- **Any resolution works**: each sprite is scaled to keep its original world
  size, so 64×64, 96×96, 128×128 all fit the grid.
- **Transparency**: fully transparent = invisible (like the `.` pixels in the
  source art).
- If a PNG is missing or fails to parse, the original procedural sprite is
  used — the game never breaks.

## Sprite sheet import

Instead of individual PNGs, you can use one sheet:

- `spritesheet.png` in the art folder
- optional `spritesheet.json` mapping frame names to rectangles:

```json
{ "frames": [
  { "name": "PlayerDown0", "x": 0,   "y": 0,   "w": 64, "h": 64 },
  { "name": "PlayerDown1", "x": 64,  "y": 0,   "w": 64, "h": 64 }
] }
```

- Coordinates are pixels from the **top-left** of the sheet (the loader
  flips them for Unity). Any cell size works.
- No JSON = a 64×64 grid is assumed, frames mapped in the manifest order.
- Individual PNGs take precedence over the sheet, so you can mix: a sheet
  for the bulk + a single PNG for one tweaked sprite.

## If you want to start from scratch

The procedural sprites themselves are defined as ASCII pixel maps in
`Assets/Scripts/Runtime/Art/SpriteBank.cs` (see `ArtDown0`, the `CropSprites`
array, etc.). Edit the character maps there and they regenerate — or ignore
that file entirely and use PNGs.
