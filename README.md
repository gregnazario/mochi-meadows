# Mochi Meadows

A kawaii cozy farming game — Stardew Valley energy, but cuter. Pastel pixel
art, a marshmallow cat shopkeeper, sparkle hearts, and zero pressure.

Design doc: [docs/DESIGN.md](docs/DESIGN.md)

## Play

- **Play it in your browser**: https://gregnazario.github.io/mochi-meadows/  (GitHub Pages)

  Note: the WebGL build uses Unity's brotli *decompression fallback* so it
  works on static hosts (like GitHub Pages) that don't send
  `Content-Encoding: br`. If you host it elsewhere, either keep the fallback
  or add the header to the `.br` files.
- **macOS**: `MochiMeadows/Builds/Mac/MochiMeadows.app`
- **WebGL (local)**: serve `MochiMeadows/Builds/WebGL` (e.g. `python3 -m http.server 8000`)
- **iOS**: `Tools/Mochi Meadows/Build iOS (Xcode project)` — for the Simulator
  add `-simulator` (builds arm64; then open `Builds/iOS` in Xcode and run)
- **Windows/Linux**: add the module in Unity Hub, then `Tools/Mochi Meadows/Build Windows` / `Build Linux`

### Controls

| Action | Desktop | Touch |
|---|---|---|
| Move | WASD / arrows | dynamic joystick (left half of screen) |
| Use tool / talk / harvest | Space or E (or click) | tap the tile / tap Mochi |
| Select hotbar tool | keys 1–7 or scroll wheel | tap the slot |
| Close UI | Esc | ✕ button |

## How to play

1. **Hoe** grass into soil, **plant** seeds, **water** them every day.
2. Crops grow one stage per watered day; keep watering until they bloom.
3. **Harvest** with the hand (facing the crop), then sell to **Mochi** the cat
   at her stand (south-west) for coins.
4. Energy = hearts. Nap under the **peach tree** (north-east) to sleep and
   restore; the day also auto-ends at midnight.
5. Autosaves every 45 s, at dawn, and on quit.

## Quests, expansions & settings

- **Quest board** (next to Mochi's stand): three cozy daily tasks — water,
  harvest, hoe, plant, pet chickens, sell, or buy seeds — each with a coin
  reward. New tasks every morning.
- **Farm expansion**: Mochi sells Farm Expansion Deeds (300 / 800 coins) that
  unlock 14x8 then 16x9 plots. Locked land shows a fence-cross until you buy it.
- **Settings**: the ☕ menu (or the gear on the title screen) has Music and SFX
  sliders. Gamepad: A/Enter acts, B/Esc closes UI, Start or M opens the menu.

## Cosmetics, weather & fishing

- **Makeover** (✨ button in Mochi's shop): 5 dress colors and 4 hair colors.
  New colors cost coins, owned ones are free to switch (★ = wearing it).
- **Rain**: ~1 day in 5 it rains — the sky greys, pastel streaks fall, and
  the rain waters every plot for free. Cozy rain sound, darker clouds.
- **Fishing**: stand by the pond and interact with the hand. A bobber sweeps
  the water column — tap when it's on the pink fish zone! Goldfish, Bubble
  Fish, and the rare Sakura Fish go straight to your basket.
- **Game feel**: tool swings arc across the tile, soft footsteps, splash on
  cast, dawn birdsong, whoosh on tools.

## Multi-harvest, beehives & scrapbook

- **Multi-harvest crops**: strawberries regrow once, blueberries twice — the
  plant stays and blooms again instead of dying after the first harvest.
- **Beehives** (decor): place one near flowers and the bees make honey jars
  each morning — sell them or eat them for +15 energy.
- **Scrapbook** (in Mochi's shop): tracks every crop, fish, and delicacy you
  discover, plus lifetime coins earned and days on the farm.
- **Mobile UI**: a thumb-friendly Act button and a persistent joystick pad on
  phones; bigger touch targets.

## Seasons & critters

- Each season lasts 7 days (28-day year). The world shifts with the season:
  blossom-pink spring, warm summer, golden autumn, snowy winter — trees, sky,
  and lighting all change.
- 6 crops total (Strawberry, Blueberry, Pumpkitten, Sakura, Melon, Mochi Rice).
  Each has a home season: selling **in season** pays +25%.
- Three kawaii chickens (one is sakura-pink!) wander between the farm and the
  pond. Pet them with the hand (or a tap) for hearts and +energy — 3 pets/day.

## Dev tools

- `-playtest [-playtest-days N] [-playtest-seed S]` runs a headless simulated
  farmer (deterministic with a seed) and writes a balance report to
  `playtest_report.txt`, including a save/load round-trip check.
- Verified: 28-day full-year soak with seed 777: 60 -> 2863 coins, 71
  harvests, round-trip PASS, zero exceptions.
- `-screenshot-game [-day N]` / `-screenshot-shop` capture verification shots.
- Latest playtest (28 days): 60 coins -> 2032 coins, 71 harvests, PASS.

## Project structure

```
Assets/Scripts/Runtime/
  Art/      procedural pixel art: ASCII sprites -> textures (no image files)
  Game/     farm grid, crops, economy, day/night, NPC, save system
  Inputs/   keyboard + touch joystick abstraction
  Ui/       runtime-built UI (HUD, hotbar, shop), responsive CanvasScaler
  Audio/    synthesized chimes + lullaby (no audio files)
  Core/     GameBootstrap (builds the whole game from an empty scene)
Assets/Scripts/Editor/
  SceneBuilder.cs   creates the one scene (Boot object)
  BuildScript.cs    macOS / Windows / Linux / WebGL builds
```

Everything (art, music, UI) is generated at runtime from code — the repo has
zero binary assets, so builds stay small and portable.

## Screen-size handling

- Canvas scales from a 1920x1080 reference (ScaleWithScreenSize, match 0.5).
- Game view is a fixed 16:9 world; any other aspect (phone portrait to
  ultrawide) gets pastel letterbox bars and a background camera.
- **Pixel-perfect**: the camera picks an integer screen-pixel scale (3px per
  art pixel at 720p, 4px at 1080p) and snaps to the texel grid, so sprites
  stay razor sharp at any resolution. Characters/crops/critters have soft
  1px outlines and ground shadows for crisp silhouettes; tree canopies are
  posterized into clean pastel bands.
- Verified across a resolution matrix — 640x360, 720p/1080p 16:9, 21:9
  (3440x1440), 32:9 (3840x1080), phone portrait (720x1280, 1080x2340),
  and iPad 4:3 (2048x1536) — letterbox bars, HUD, hotbar, and modals all
  render correctly, with an integer pixel scale at every size.

## Build from CLI

```
"/Applications/Unity/Hub/Editor/<ver>/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath MochiMeadows \
  -executeMethod MochiMeadows.EditorTools.BuildScript.BuildMac
# or BuildWebGL / BuildWindows / BuildLinux
```

Dev screenshots: run the app with `-screenshot` (title), `-screenshot-game`
(in-game), or `-screenshot-shop`; PNGs land in
`~/Library/Application Support/Mochi Meadows/.../Screenshots`.
