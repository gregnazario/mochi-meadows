# Level Guide — designing your own world

The world layout is data-driven. Drop a `level.json` into
`Assets/StreamingAssets/level.json` (shipped) — or the dev override at
`~/Library/Application Support/Mochi Meadows/Mochi Meadows/level_export.json`
(no rebuild needed) — and the engine builds the world from it.

## Quick start

1. Export the current layout:

   ```sh
   "Mochi Meadows.app/Contents/MacOS/Mochi Meadows" -export-level
   ```

   This writes `level_export.json` to the app support folder — edit it and
   relaunch to see changes instantly.

2. When happy, copy it to `Assets/StreamingAssets/level.json` and rebuild.

## Schema (all fields optional — omitted = default)

```jsonc
{
  "world": { "width": 40, "height": 24 },          // meadow size in tiles

  "farm": {
    "originX": 9, "originY": 7,                    // top-left of the plot grid
    "baseW": 12, "baseH": 7,                       // starting size
    "maxW": 16, "maxH": 10                         // size after buying deeds
  },

  "pond": { "x": 27, "y": 15, "w": 3, "h": 2 },    // fishing pond

  "playerSpawn": { "x": 8.5, "y": 14.5 },          // where the farmer starts

  "mochi": { "home": { "x": 4.5, "y": 14.2 }, "wander": 2.2 },

  "paths": [ { "x0": 4, "y0": 6, "x1": 4, "y1": 15 },
             { "x0": 4, "y0": 6, "x1": 9, "y1": 6 } ],   // path rectangles

  "spots": {
    "shop":        [4, 15],    // Mochi's market stand
    "questBoard":  [6, 15],    // daily quest board
    "blanket":     [30, 4],    // sleep spot (nap here)
    "kitchen":     [28, 5]     // cooking table
  },

  "trees": [ { "x": 31, "y": 5, "peach": true },
             { "x": 25, "y": 3 },
             { "x": 2,  "y": 3 },
             { "x": 35, "y": 19 },
             { "x": 6,  "y": 20 } ],

  "chickens": [ { "x": 22.5, "y": 14.5 },
                { "x": 24.5, "y": 15.5 },
                { "x": 23.5, "y": 13.8 } ],

  "flowers": { "count": 22, "seed": 20260802 }     // decorative flowers
}
```

## How the engine uses it

- `GameBootstrap` reads `LevelConfig.Current` and builds the ground, paths,
  pond, trees, shops, spawns, and NPC homes from the JSON.
- `IsDecorSpotFree` derives its rules from the same config (farm bounds, pond,
  paths, spots, trees) — move the pond in JSON and decor placement follows.
- Fences, flowers, and all game systems (quests, fishing, cooking, sleeping)
  work with any layout — they reference the config, not hardcoded tiles.

## Tips

- Coordinates are world tiles; characters' positions are floats (tile + .5).
- Keep the farm within the world bounds — the camera clamps to the world.
- `PondRect`, `KitchenPos`, and spawns are all read from the config, so
  fishing/cooking/napping work wherever you move them.
- The JSON is parsed with `JsonUtility` — arrays of objects, no comments.
