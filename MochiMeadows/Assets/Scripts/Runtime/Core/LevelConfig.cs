using System;
using System.IO;
using UnityEngine;

namespace MochiMeadows.Core
{
    // ---- Level data: drop a level.json into StreamingAssets/ (or the dev
    //      folder) to override the built-in world layout. ----

    [Serializable]
    public class LevelWorld { public int width = 40; public int height = 24; }

    [Serializable]
    public class LevelFarm
    {
        public int originX = 9, originY = 7;
        public int baseW = 12, baseH = 7;
        public int maxW = 16, maxH = 10;
    }

    [Serializable]
    public class LevelRect { public int x = 27, y = 15, w = 3, h = 2; }

    [Serializable]
    public class LevelFloat2 { public float x; public float y; }

    [Serializable]
    public class LevelMochi { public LevelFloat2 home; public float wander = 2.2f; }

    [Serializable]
    public class LevelPath { public int x0, y0, x1, y1; }

    [Serializable]
    public class LevelSpots { public int[] shop; public int[] questBoard; public int[] blanket; public int[] kitchen; }

    [Serializable]
    public class LevelTree { public int x, y; public bool peach; }

    [Serializable]
    public class LevelFlowers { public int count = 22; public int seed = 20260802; }

    [Serializable]
    public class LevelConfig
    {
        public LevelWorld world;
        public LevelFarm farm;
        public LevelRect pond;
        public LevelFloat2 playerSpawn;
        public LevelMochi mochi;
        public LevelPath[] paths;
        public LevelSpots spots;
        public LevelTree[] trees;
        public LevelFloat2[] chickens;
        public LevelFlowers flowers;

        public static LevelConfig Current { get; private set; }

        // Load order: StreamingAssets/level.json, then dev folder override.
        public static void Load()
        {
            Current = Default();

            string bundled = Path.Combine(Application.streamingAssetsPath, "level.json");
            string dev = Path.Combine(Application.persistentDataPath, "level_export.json");

            if (File.Exists(dev)) { Apply(File.ReadAllText(dev), "dev level_export.json"); }
            else if (File.Exists(bundled)) { Apply(File.ReadAllText(bundled), "level.json"); }
        }

        static void Apply(string json, string source)
        {
            try
            {
                var cfg = JsonUtility.FromJson<LevelConfig>(json);
                if (cfg != null && cfg.world != null && cfg.world.width > 0)
                {
                    Current = cfg;
                    Debug.Log($"[Level] Loaded custom level from {source}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Level] Could not parse {source}: {e.Message}");
            }
        }

        public static LevelConfig Default()
        {
            return new LevelConfig
            {
                world = new LevelWorld { width = 40, height = 24 },
                farm = new LevelFarm { originX = 9, originY = 7, baseW = 12, baseH = 7, maxW = 16, maxH = 10 },
                pond = new LevelRect { x = 27, y = 15, w = 3, h = 2 },
                playerSpawn = new LevelFloat2 { x = 8.5f, y = 14.5f },
                mochi = new LevelMochi { home = new LevelFloat2 { x = 4.5f, y = 14.2f }, wander = 2.2f },
                paths = new[]
                {
                    new LevelPath { x0 = 4, y0 = 6, x1 = 4, y1 = 15 },
                    new LevelPath { x0 = 4, y0 = 6, x1 = 9, y1 = 6 },
                },
                spots = new LevelSpots
                {
                    shop = new[] { 4, 15 },
                    questBoard = new[] { 6, 15 },
                    blanket = new[] { 30, 4 },
                    kitchen = new[] { 28, 5 },
                },
                trees = new[]
                {
                    new LevelTree { x = 31, y = 5, peach = true },
                    new LevelTree { x = 25, y = 3 },
                    new LevelTree { x = 2, y = 3 },
                    new LevelTree { x = 35, y = 19 },
                    new LevelTree { x = 6, y = 20 },
                },
                chickens = new[]
                {
                    new LevelFloat2 { x = 22.5f, y = 14.5f },
                    new LevelFloat2 { x = 24.5f, y = 15.5f },
                    new LevelFloat2 { x = 23.5f, y = 13.8f },
                },
                flowers = new LevelFlowers { count = 22, seed = 20260802 },
            };
        }
    }
}
