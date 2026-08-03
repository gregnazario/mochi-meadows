# Mochi Meadows

A kawaii cozy farming game — Stardew Valley energy, but cuter. Pastel pixel
art, a marshmallow cat shopkeeper, sparkle hearts everywhere, and zero pressure.

## Concept

You inherit a tiny farm with a big heart. Plant crops, water them, watch them
bloom, and sell your harvest to Mochi, the marshmallow cat, to buy cuter seeds.
No combat, no deadlines. If you run out of energy, a nap under the peach tree
restores you — the day just rolls on, sunny or starry.

## Core loop

1. Buy seed packets from Mochi's shop stand (coins).
2. Hoe soil, plant, water (energy cost per action).
3. Crops grow over days, in pastel stages with a cute animation at harvest.
4. Harvest -> earn coins + sparkle effect.
5. Energy depletes; sleep restores it. Day counter advances.
6. Everything autosaves.

## Systems

- **Farm grid** — 10x6 tillable plots; tiles track {tilled, watered, crop}.
- **Crops** (4): Strawberry (sweet pink), Blueberry (baby blue), Pumpkitten
  (mini orange cat-pumpkin), Sakura Turnip (cherry-blossom pastel).
  Each has 4 growth stages, sell price, seed price.
- **Energy** — hoe/water/plant cost energy; harvest gives a little back (treat).
- **Day/night** — 20 in-game hours over 6 real minutes; sky, light, and
  ambient tint lerp through pastel dawn / sunny day / peach sunset / starry night.
- **Economy** — coins in wallet, shop UI with buy seeds / sell crops.
- **Save** — JSON autosave on quit and each morning (day, money, energy, grid,
  crops, wallet, player position).
- **NPC** — Mochi the cat wanders the farm; talking opens the shop.

## Kawaii visual language

- Palette: strawberry pink `#FF9BB3`, mint `#B8F2D4`, cream `#FFF6E5`,
  lavender `#D6C4F5`, baby blue `#BFE3FF`, peach `#FFD9A8`, chocolate brown
  for outlines. Everything is pastel; nothing is saturated.
- Pixel art defined as ASCII art in code (no binary assets) — every sprite is
  generated procedurally at startup, so the whole game is one repo, one build.
- Chibi player: big head, sparkle eyes, blush cheeks, little bob animation.
- Feedback: heart particles on harvest, water droplets on watering, sparkles on
  plant, gentle bounce on walk.

## Controls & input (cross-platform)

- **Desktop**: WASD / arrows move, Space or E interact, 1-4 hotbar, Esc close UI.
- **Touch (mobile/tablet)**: dynamic virtual joystick on the left half of the
  screen, tap a tile to act, tap the NPC to talk. UI repositions for thumbs.
- Camera: orthographic, follows player, clamps to farm bounds.

## Screen / aspect handling

- **Canvas**: `ScaleWithScreenSize` at 1920x1080 reference, match 0.5 — UI
  scales proportionally on any resolution from phone portrait to ultrawide.
- **Game view**: pixel-perfect virtual resolution with letterbox bars when the
  aspect ratio goes outside 16:9 (target ~16:9, supports 9:16 through 32:9).
- Tested builds: macOS standalone, WebGL (desktop + mobile browser), and the
  code paths for iOS/Android are present (input abstraction + responsive UI).

## Architecture

```
GameBootstrap          — creates the world at runtime from one empty scene
  GameManager          — day, time, money, energy, win state, save/load
  FarmGrid             — tile states, growth ticks, tile visuals
  CropDatabase         — static crop definitions (seeds, prices, palettes)
  PlayerController     — movement, facing, action raycasting
  NpcController        — Mochi wander AI + dialog
  InputService         — keyboard + virtual joystick + tap abstraction
  UiController         — HUD, hotbar, day/time, energy, dialogs, shop
  SpriteFactory        — ASCII-art pixel sprites -> textures (procedural art)
  AudioService         — procedural chimes (no audio files)
  DayNightController   — sky/light tint, sun/moon sprite, clock
  SaveSystem           — JSON autosave (PlayerPrefs wrapper, web-safe)
```

## Seasons

Four 7-day seasons (Spring/Summer/Autumn/Winter). The world shifts with the
season: tree canopies, sky palette, and a subtle world tint (golden autumn,
snowy winter). Each crop has a home season — selling in season pays +25%.
Six crops: Strawberry, Blueberry, Pumpkitten, Sakura, Melon (summer),
Mochi Rice (winter).

## Critters

Three kawaii chickens wander the farm (one sakura-pink). Pet them (hand or
tap) for hearts, a peep, and +2 energy — up to 3 pets per day.

## Quests

Mochi's quest board posts three daily tasks (water/harvest/hoe/plant/pet/
sell/buy) with coin rewards. New tasks each morning; claim from the panel.

## Farm expansion

Mochi sells Farm Expansion Deeds (300/800 coins) unlocking 14x8 then 16x9
plots. Locked land renders with a fence-cross until purchased.

## Cosmetics

Mochi sells makeovers: 5 dress colors and 4 hair colors (60/40 coins; owned
colors switch for free). The player sprites rebuild from their art arrays with
a recolored palette — one function, no duplicated assets.

## Weather

~20% chance of rain each morning. Rain waters every plot for free, greys the
sky, darkens clouds, plays a soft noise bed, and recycles falling streak
sprites. Dawn brings random birdsong.

## Fishing

Interact at the pond with the hand: a bobber sweeps a water column and the
pink fish zone drifts; tap when they overlap. Three fish (Goldfish 25,
Bubble Fish 40, Sakura Fish 70) with weighted odds join the basket and the
quest board can ask for a catch. Costs 3 energy.

## Settings & input

Music/SFX volume sliders (menu panel, also on title screen). Gamepad support:
A/Enter = action, B/Esc = close UI, Start/M = menu. Touch keeps the dynamic
joystick and tap-to-act.

## Playtesting

`-playtest` runs a headless simulated farmer for N days, exercising the full
loop (water, harvest, sell, reinvest) and writing a day-by-day balance report.
Verified: 28-day year, 60 -> 2032 coins, 71 harvests, PASS.

## Out of scope (v1)

Multiplayer, NPC relationships, crafting, fishing, weather beyond pretty skies,
quest board.
