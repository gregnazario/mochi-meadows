using System;
using System.Collections.Generic;
using UnityEngine;
using MochiMeadows.Art;

namespace MochiMeadows.Game
{
    public enum Season { Spring = 0, Summer = 1, Autumn = 2, Winter = 3 }
    public enum CropId { Strawberry = 0, Blueberry = 1, Pumpkitten = 2, Sakura = 3, Melon = 4, MochiRice = 5 }

    public class CropDef
    {
        public CropId Id;
        public string Name;
        public string SeedName;
        public string Yum;              // kawaii harvest line
        public int SeedPrice;
        public int SellPrice;
        public int GrowthDays;          // watered days to reach ready
        public int MinYield, MaxYield;  // harvest amount
        public Season Season;           // in-season bonus season
        public Color Tint;

        public const float SeasonBonus = 1.25f;

        public int CurrentSellPrice(Season current)
        {
            return Mathf.RoundToInt(SellPrice * (current == Season ? SeasonBonus : 1f));
        }

        public static readonly CropDef[] All =
        {
            new CropDef { Id = CropId.Strawberry, Name = "Strawberry", SeedName = "Strawberry Seeds", Yum = "Sweet strawberry! +sparkle+",
                          SeedPrice = 10, SellPrice = 20, GrowthDays = 2, MinYield = 1, MaxYield = 2, Season = Season.Spring, Tint = Palette.DeepPink },
            new CropDef { Id = CropId.Blueberry, Name = "Blueberry", SeedName = "Blueberry Seeds", Yum = "Baby-blue berry!",
                          SeedPrice = 15, SellPrice = 30, GrowthDays = 3, MinYield = 1, MaxYield = 3, Season = Season.Summer, Tint = Palette.BabyBlue },
            new CropDef { Id = CropId.Pumpkitten, Name = "Pumpkitten", SeedName = "Pumpkitten Seeds", Yum = "It purrs!  Meow~",
                          SeedPrice = 25, SellPrice = 60, GrowthDays = 3, MinYield = 1, MaxYield = 2, Season = Season.Autumn, Tint = Palette.Orange },
            new CropDef { Id = CropId.Sakura, Name = "Sakura Turnip", SeedName = "Sakura Seeds", Yum = "Blossom-fresh!",
                          SeedPrice = 40, SellPrice = 100, GrowthDays = 4, MinYield = 1, MaxYield = 2, Season = Season.Spring, Tint = Palette.PetalPink },
            new CropDef { Id = CropId.Melon, Name = "Melon", SeedName = "Melon Seeds", Yum = "So juicy~!",
                          SeedPrice = 60, SellPrice = 150, GrowthDays = 5, MinYield = 1, MaxYield = 2, Season = Season.Summer, Tint = Palette.Leaf },
            new CropDef { Id = CropId.MochiRice, Name = "Mochi Rice", SeedName = "Mochi Rice Seeds", Yum = "Soft & squishy!",
                          SeedPrice = 35, SellPrice = 90, GrowthDays = 3, MinYield = 1, MaxYield = 2, Season = Season.Winter, Tint = Palette.White },
        };

        public static CropDef Get(CropId id) => All[(int)id];
    }

    public enum TileState { Grass = 0, Tilled, Watered, Cropped }

    [Serializable]
    public struct PlotData
    {
        public TileState state;
        public int cropId;     // -1 none
        public int stage;      // 0..3 (3 = ready)
        public int wateredDays;
        public bool wateredToday;
    }

    // The farm plot grid: hoe, plant, water, grow, harvest.
    public class FarmGrid : MonoBehaviour
    {
        public const int MaxW = 16;
        public const int MaxH = 10;
        public const float TileSize = 1f;
        public int ActiveW = 12;
        public int ActiveH = 7;
        public int ExpansionTier;      // 0 = base, 1 = +2 cols/+1 row, 2 = max
        public const int ExpansionCost1 = 300;
        public const int ExpansionCost2 = 800;

        public Vector2Int Origin = new Vector2Int(9, 7); // world tile of plot (0,0)
        public Vector3 BlanketPos;                        // set by bootstrap
        public float TilePuffZ = -2f;

        PlotData[,] plots = new PlotData[MaxW, MaxH];
        SpriteRenderer[,] tileRenderers = new SpriteRenderer[MaxW, MaxH];
        SpriteRenderer[,] cropRenderers = new SpriteRenderer[MaxW, MaxH];
        Transform cropRoot;
        GameManager gm;

        public enum ActionResult { Ok, NoEnergy, WrongTile, NoSeeds, AlreadyWatered, NotReady, Nothing }

        public void Init(GameManager gameManager, SpriteRenderer[,] renderers, Transform cropRootT)
        {
            gm = gameManager;
            if (renderers != null)
            {
                tileRenderers = renderers;
                cropRenderers = new SpriteRenderer[MaxW, MaxH];
                for (int x = 0; x < MaxW; x++)
                    for (int y = 0; y < MaxH; y++)
                    {
                        var crop = cropRootT.Find($"crop_{x}_{y}");
                        cropRenderers[x, y] = crop != null ? crop.GetComponent<SpriteRenderer>() : null;
                    }
            }
            if (cropRootT != null) cropRoot = cropRootT;
            ResetGrid();
        }

        public void ResetGrid()
        {
            for (int x = 0; x < ActiveW; x++)
                for (int y = 0; y < ActiveH; y++)
                    plots[x, y] = new PlotData { state = TileState.Grass, cropId = -1 };
            if (tileRenderers != null)
            {
                for (int x = 0; x < MaxW; x++)
                    for (int y = 0; y < MaxH; y++)
                    {
                        var sr = tileRenderers[x, y];
                        if (sr != null) sr.sprite = PickGrass(x, y);
                    }
            }
            ActiveW = 12;
            ActiveH = 7;
            ExpansionTier = 0;
            // Onboarding: three pre-planted strawberry plots (second row, left)
            PlantFree(1, 0, CropId.Strawberry);
            PlantFree(1, 1, CropId.Strawberry);
            PlantFree(1, 2, CropId.Strawberry);
            RefreshAll();
        }

        public void ImportPlot(int x, int y, PlotData data)
        {
            if (!InBounds(x, y)) return;
            plots[x, y] = data;
        }

        Sprite PickGrass(int x, int y)
        {
            int h = (x * 7 + y * 13) % 3;
            return h == 0 ? SpriteBank.Grass0 : h == 1 ? SpriteBank.Grass1 : SpriteBank.Grass2;
        }

        public bool InBounds(int x, int y) => x >= 0 && x < ActiveW && y >= 0 && y < ActiveH;
        public bool IsLocked(int x, int y) => x >= ActiveW || y >= ActiveH;
        public Vector2Int WorldToPlot(Vector3 worldPos) => new Vector2Int(
            Mathf.FloorToInt(worldPos.x - Origin.x),
            Mathf.FloorToInt(worldPos.y - Origin.y));

        public PlotData Get(int x, int y) => plots[x, y];

        public SpriteRenderer CropRenderer(int x, int y) => cropRenderers[x, y];

        public void SetTileVisual(int x, int y)
        {
            var p = plots[x, y];
            var sr = tileRenderers[x, y];
            if (sr == null) return;
            switch (p.state)
            {
                case TileState.Grass: sr.sprite = PickGrass(x, y); break;
                case TileState.Tilled: sr.sprite = ((x + y) % 2 == 0) ? SpriteBank.Soil0 : SpriteBank.Soil1; break;
                case TileState.Watered:
                case TileState.Cropped: sr.sprite = SpriteBank.SoilWet; break;
            }
            var cr = cropRenderers[x, y];
            if (p.state == TileState.Cropped && p.cropId >= 0)
            {
                cr.gameObject.SetActive(true);
                cr.sprite = SpriteBank.CropSprites[p.cropId, Mathf.Clamp(p.stage, 0, 3)];
            }
            else cr.gameObject.SetActive(false);
        }

        public void RefreshAll()
        {
            for (int x = 0; x < MaxW; x++)
                for (int y = 0; y < MaxH; y++)
                {
                    if (tileRenderers[x, y] == null) continue;
                    if (IsLocked(x, y))
                    {
                        tileRenderers[x, y].sprite = SpriteBank.LockedTile;
                        continue;
                    }
                    SetTileVisual(x, y);
                }
        }

        // --- Actions ---

        public ActionResult Hoe(int x, int y)
        {
            if (!InBounds(x, y)) return ActionResult.Nothing;
            if (plots[x, y].state != TileState.Grass) return ActionResult.WrongTile;
            if (!gm.SpendEnergy(GameManager.CostHoe)) return ActionResult.NoEnergy;
            plots[x, y].state = TileState.Tilled;
            SetTileVisual(x, y);
            QuestManager.I?.OnHoe();
            return ActionResult.Ok;
        }

        public ActionResult Water(int x, int y)
        {
            if (!InBounds(x, y)) return ActionResult.Nothing;
            var p = plots[x, y];
            if (p.state != TileState.Tilled && p.state != TileState.Cropped) return ActionResult.WrongTile;
            if (p.wateredToday) return ActionResult.AlreadyWatered;
            if (!gm.SpendEnergy(GameManager.CostWater)) return ActionResult.NoEnergy;
            var np = plots[x, y];
            np.wateredToday = true;
            if (np.state == TileState.Tilled) np.state = TileState.Watered;
            plots[x, y] = np;
            SetTileVisual(x, y);
            QuestManager.I?.OnWater();
            return ActionResult.Ok;
        }

        public ActionResult Plant(int x, int y, CropId crop)
        {
            if (!InBounds(x, y)) return ActionResult.Nothing;
            var p = plots[x, y];
            if (p.state != TileState.Tilled && p.state != TileState.Watered) return ActionResult.WrongTile;
            if (p.cropId >= 0) return ActionResult.WrongTile;
            if (!gm.TryUseSeed(crop)) return ActionResult.NoSeeds;
            if (!gm.SpendEnergy(GameManager.CostPlant)) { gm.RefundSeed(crop); return ActionResult.NoEnergy; }
            plots[x, y] = new PlotData
            {
                state = TileState.Cropped,
                cropId = (int)crop,
                stage = 0,
                wateredDays = 0,
                wateredToday = p.wateredToday,
            };
            SetTileVisual(x, y);
            QuestManager.I?.OnPlant();
            return ActionResult.Ok;
        }

        // Direct planting for onboarding / save load (no cost).
        public void PlantFree(int x, int y, CropId crop, int stage = 0, int wateredDays = 0, bool watered = false)
        {
            if (!InBounds(x, y)) return;
            plots[x, y] = new PlotData
            {
                state = TileState.Cropped,
                cropId = (int)crop,
                stage = stage,
                wateredDays = wateredDays,
                wateredToday = watered,
            };
            SetTileVisual(x, y);
        }

        public ActionResult Harvest(int x, int y)
        {
            if (!InBounds(x, y)) return ActionResult.Nothing;
            var p = plots[x, y];
            if (p.state != TileState.Cropped || p.cropId < 0) return ActionResult.NotReady;
            var def = CropDef.Get((CropId)p.cropId);
            if (p.stage < 3) return ActionResult.NotReady;

            int yield = UnityEngine.Random.Range(def.MinYield, def.MaxYield + 1);
            gm.AddToBasket((CropId)p.cropId, yield);
            plots[x, y] = new PlotData { state = TileState.Watered, cropId = -1 };
            SetTileVisual(x, y);
            gm.AddEnergy(GameManager.RewardHarvest);
            gm.Announce($"Yay! {yield} {def.Name}(s) to your basket! {def.Yum}");
            QuestManager.I?.OnHarvest();
            return ActionResult.Ok;
        }

        // Called at dawn: crops that were watered yesterday grow.
        public void GrowWateredCrops()
        {
            for (int x = 0; x < ActiveW; x++)
            {
                for (int y = 0; y < ActiveH; y++)
                {
                    var p = plots[x, y];
                    if (p.state != TileState.Cropped || p.cropId < 0) continue;
                    if (p.wateredToday && p.stage < 3)
                    {
                        p.wateredDays++;
                        var def = CropDef.Get((CropId)p.cropId);
                        p.stage = Mathf.Min(3, p.wateredDays * 3 / def.GrowthDays);
                        p.wateredToday = false;
                        plots[x, y] = p;
                        if (p.stage == 3) gm.Announce($"Your {(CropId)p.cropId} is ready to pick!");
                    }
                }
            }
            RefreshAll();
        }

        // Watered soil dries visually at dawn (unless cropped).
        public void StartNewDay()
        {
            for (int x = 0; x < ActiveW; x++)
            {
                for (int y = 0; y < ActiveH; y++)
                {
                    var p = plots[x, y];
                    p.wateredToday = false;
                    if (p.state == TileState.Watered) p.state = TileState.Tilled;
                    if (p.state == TileState.Cropped && !p.wateredToday)
                        p.state = TileState.Cropped; // keep
                    plots[x, y] = p;
                }
            }
            RefreshAll();
        }

        public bool Expand()
        {
            if (ExpansionTier >= 2) return false;
            ExpansionTier++;
            ActiveW = Mathf.Min(MaxW, ActiveW + 2);
            ActiveH = Mathf.Min(MaxH, ActiveH + 1);
            for (int x = 0; x < ActiveW; x++)
                for (int y = 0; y < ActiveH; y++)
                    if (tileRenderers[x, y] != null) SetTileVisual(x, y);
            return true;
        }

        public int NextExpansionCost => ExpansionTier == 0 ? ExpansionCost1 : ExpansionCost2;
        public bool CanExpand => ExpansionTier < 2;

        public PlotData[,] ExportPlots() => (PlotData[,])plots.Clone();
        public void ImportPlots(PlotData[,] data)
        {
            for (int x = 0; x < ActiveW; x++)
                for (int y = 0; y < ActiveH; y++)
                    plots[x, y] = data[x, y];
            RefreshAll();
        }

        // Cute floating particle burst (hearts, sparkles, droplets).
        static GameObject puffRoot;
        public void SpawnTilePuff(Vector2Int tile, Color color)
        {
            if (puffRoot == null)
            {
                puffRoot = new GameObject("PuffRoot");
                GameObject.DontDestroyOnLoad(puffRoot);
            }
            Vector3 pos = new Vector3(tile.x + 0.5f, tile.y + 0.5f, TilePuffZ);
            int n = UnityEngine.Random.Range(4, 7);
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Puff");
                go.transform.SetParent(puffRoot.transform, false);
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.35f, 0.7f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.SoftCircle(8, color);
                sr.sortingOrder = 30;
                var puff = go.AddComponent<PuffParticle>();
                puff.Vel = new Vector2(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(1.5f, 3.2f));
            }
        }

        static GameObject arcRoot;
        public void SpawnArc(Vector2Int tile, Color color)
        {
            if (arcRoot == null)
            {
                arcRoot = new GameObject("ArcRoot");
                GameObject.DontDestroyOnLoad(arcRoot);
            }
            var go = new GameObject("Arc");
            go.transform.SetParent(arcRoot.transform, false);
            go.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.9f, -2.2f);
            go.transform.localScale = Vector3.one * 0.55f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.ArcSprite(color);
            sr.sortingOrder = 12;
            go.AddComponent<ArcFx>();
        }

        class ArcFx : MonoBehaviour
        {
            float t;
            SpriteRenderer sr;

            void Start() { sr = GetComponent<SpriteRenderer>(); }

            void Update()
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.22f);
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(-55f, 55f, p));
                transform.position += new Vector3(0, 0.5f, 0) * Time.deltaTime * 1.6f;
                if (sr != null) sr.color = new Color(1, 1, 1, 1f - p);
                if (p >= 1f) Destroy(gameObject);
            }
        }

        class PuffParticle : MonoBehaviour
        {
            public Vector2 Vel;
            float life = 0.75f;
            SpriteRenderer sr;

            void Start() { sr = GetComponent<SpriteRenderer>(); }

            void Update()
            {
                life -= Time.deltaTime;
                if (life <= 0) { Destroy(gameObject); return; }
                Vel.y -= 4f * Time.deltaTime;
                transform.position += (Vector3)(Vel * Time.deltaTime);
                transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 0.15f, 1f - life / 0.75f);
                if (sr != null) sr.color = new Color(1, 1, 1, life / 0.75f);
            }
        }
    }
}
