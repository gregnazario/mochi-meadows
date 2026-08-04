using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MochiMeadows.Art;
using MochiMeadows.Game;
using MochiMeadows.Inputs;
using MochiMeadows.Ui;
using MochiMeadows.Audio;

namespace MochiMeadows.Core
{
    // Builds the entire game at runtime from a single empty scene.
    public class GameBootstrap : MonoBehaviour
    {
        static bool iosShotMode;
        public static int WorldW => LevelConfig.Current.world.width;
        public static int WorldH => LevelConfig.Current.world.height;
        public static RectInt PondRect
        {
            get
            {
                var p = LevelConfig.Current.pond;
                return new RectInt(p.x, p.y, p.w, p.h);
            }
        }
        public static Vector3 KitchenPos
        {
            get
            {
                var k = LevelConfig.Current.spots.kitchen;
                return new Vector3(k[0] + 0.5f, k[1] + 0.5f, 0);
            }
        }

        public static bool IsDecorSpotFree(int wx, int wy)
        {
            if (wx < 1 || wy < 1 || wx > WorldW - 2 || wy > WorldH - 2) return false;
            var lvl = LevelConfig.Current;
            var f = lvl.farm;
            if (wx >= f.originX && wx < f.originX + f.maxW && wy >= f.originY && wy < f.originY + f.maxH) return false;
            var p = lvl.pond;
            if (wx >= p.x && wx < p.x + p.w && wy >= p.y && wy < p.y + p.h) return false;
            if (lvl.paths != null)
            {
                foreach (var path in lvl.paths)
                {
                    if (wx >= Mathf.Min(path.x0, path.x1) && wx <= Mathf.Max(path.x0, path.x1)
                        && wy >= Mathf.Min(path.y0, path.y1) && wy <= Mathf.Max(path.y0, path.y1)) return false;
                }
            }
            var spots = lvl.spots;
            foreach (var spot in new[] { spots.shop, spots.questBoard, spots.blanket, spots.kitchen })
            {
                if (spot != null && spot.Length >= 2 && spot[0] == wx && spot[1] == wy) return false;
            }
            if (lvl.trees != null)
            {
                foreach (var t in lvl.trees)
                    if (t.x == wx && t.y == wy) return false;
            }
            return true;
        }

        public static GameBootstrap I;

        Camera mainCam, bgCam;
        public Camera MainCam => mainCam;

        void Awake()
        {
            I = this;
            DontDestroyOnLoad(gameObject);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;

            string[] cmdArgs = System.Environment.GetCommandLineArgs();
            bool shotMode = false;
            foreach (var a in cmdArgs)
                if (a.StartsWith("-screenshot")) { shotMode = true; break; }
            // iOS: launch args don't reach Unity, so the simulator sets a user default instead
            if (!shotMode && PlayerPrefs.GetInt("mochi_dev_shot", 0) == 1)
            {
                shotMode = true;
                iosShotMode = true;
                PlayerPrefs.DeleteKey("mochi_dev_shot");
                PlayerPrefs.Save();
            }
            if (shotMode)
            {
                StartCoroutine(ScreenshotRoutine());
            }
            if (cmdArgs.Any(a => a == "-export-art") || cmdArgs.Any(a => a == "-export-level"))
            {
                StartCoroutine(ExportRoutine(cmdArgs.Any(a => a == "-export-art"), cmdArgs.Any(a => a == "-export-level")));
            }

            SpriteBank.BuildAll();
            LevelConfig.Load();

            var audio = gameObject.AddComponent<AudioService>();
            var input = gameObject.AddComponent<InputService>();
            var gm = gameObject.AddComponent<GameManager>();
            gm.Audio = audio;
            gm.DevPlaytest = cmdArgs.Any(a => a == "-playtest");

            StartCoroutine(InitRoutine(gm, audio));
        }

        public static bool InitComplete { get; private set; }

        System.Collections.IEnumerator ExportRoutine(bool art, bool level)
        {
            while (!InitComplete) yield return null;
            if (art) Art.SpriteExporter.ExportAll();
            if (level) LevelExporter.Export();
#if !UNITY_WEBGL
            Application.Quit();
#endif
        }

        System.Collections.IEnumerator InitRoutine(GameManager gm, AudioService audio)
        {
            // wait for any custom art (async on WebGL; instant elsewhere)
            yield return CustomSpriteLoader.LoadAll();

            BuildCameras();
            var world = BuildWorld();
            var farm = BuildFarm(world, gm);
            var (player, mochi) = BuildCharacters(farm);
            var critters = BuildCritters(farm);
            var quests = gameObject.AddComponent<QuestManager>();
            var qspot = LevelConfig.Current.spots.questBoard;
            quests.BoardPos = new Vector3(qspot[0] + 0.5f, qspot[1] + 0.5f, 0);
            BuildQuestBoard(world, quests.BoardPos);
            var decorRootGo = new GameObject("Decor");
            decorRootGo.transform.SetParent(transform, false);
            gm.Farm = farm;
            gm.Player = player;
            gm.Mochi = mochi;
            gm.Critters = critters;
            gm.Quests = quests;
            gm.InitDecorRoot(decorRootGo.transform);

            var dayNight = gameObject.AddComponent<DayNightController>();
            dayNight.MainCam = mainCam;
            dayNight.BgCam = bgCam;
            dayNight.Gm = gm;

            var weather = gameObject.AddComponent<WeatherController>();

            var seasons = gameObject.AddComponent<SeasonController>();
            seasons.Gm = gm;
            seasons.DayNight = dayNight;
            seasons.Canopies = canopies.ToArray();
            seasons.TintQuad = MakeSeasonTint();
            weather.Init(gm, dayNight, audio);

            var ui = gameObject.AddComponent<UiController>();
            ui.BuildUi();
            gm.Ui = ui;
            gm.Init(farm, ui, player, mochi, audio);

            var rig = gameObject.AddComponent<CameraRig>();
            rig.Init(mainCam, player.transform, farm, this);

            if (gm.DevPlaytest)
            {
                gameObject.AddComponent<PlaytestSimulator>();
            }
            InitComplete = true;
        }

        SpriteRenderer MakeSeasonTint()
        {
            var go = new GameObject("SeasonTint");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.SoftCircle(2, Color.white);
            sr.color = Color.clear;
            sr.transform.localScale = new Vector3(200f, 200f, 1f);
            sr.transform.position = new Vector3(0, 0, 4.8f);
            return sr;
        }

        // Dev helper: `-screenshot` captures the title screen; `-screenshot-game`
        // starts the game first; `-screenshot-shop` also opens Mochi's shop.
        System.Collections.IEnumerator ScreenshotRoutine()
        {
            var args = System.Environment.GetCommandLineArgs();
            Debug.Log("[SHOT] mode start");
            if (args.Contains("-screenshot-scrapbook"))
            {
                yield return null;
                yield return null;
                GameManager.I.Ui.StartGame(false);
                GameManager.I.CollectedCropsMask = 0b111111;
                GameManager.I.CollectedFishMask = 0b111;
                GameManager.I.CollectedEgg = true;
                GameManager.I.CollectedHoney = true;
                GameManager.I.TotalEarned = 1234;
                GameManager.I.Ui.OpenScrapbook();
            }
            if (args.Contains("-screenshot-makeover") || args.Contains("-screenshot-fish") || args.Contains("-screenshot-rain"))
            {
                yield return null;
                yield return null;
                GameManager.I.Ui.StartGame(false);
                if (args.Contains("-screenshot-makeover")) GameManager.I.Ui.OpenMakeover();
                if (args.Contains("-screenshot-fish")) GameManager.I.Ui.OpenFishing();
                if (args.Contains("-screenshot-rain")) WeatherController.I.ForceRain();
            }
            if (args.Contains("-screenshot-quest") || args.Contains("-screenshot-menu"))
            {
                yield return null;
                yield return null;
                GameManager.I.Ui.StartGame(false);
                if (args.Contains("-screenshot-quest")) GameManager.I.Ui.OpenQuests();
                else GameManager.I.Ui.OpenMenu();
                if (args.Contains("-screenshot-controls")) GameManager.I.Ui.ShowControls();
            }
            if (args.Contains("-screenshot-game") || args.Contains("-screenshot-shop") || iosShotMode)
            {
                yield return null;
                yield return null;
                Debug.Log("[SHOT] starting game");
                GameManager.I.Ui.StartGame(false);   // hides the title panel too
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "-day" && int.TryParse(args[i + 1], out int d))
                    {
                        GameManager.I.Day = Mathf.Max(1, d);
                        GameManager.I.Ui.RefreshClock();
                    }
                }
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "-outfit" && int.TryParse(args[i + 1], out int od))
                    {
                        GameManager.I.OwnedDressesMask |= 1 << od;
                        GameManager.I.ApplyOutfit(od, 0);
                    }
                }
                if (args.Contains("-expanded"))
                {
                    GameManager.I.Farm.Expand();
                    GameManager.I.Farm.Expand();
                    GameManager.I.Money += 2000;
                }
                if (args.Contains("-screenshot-shop"))
                {
                    Debug.Log("[SHOT] opening shop");
                    GameManager.I.Ui.OpenShop(GameManager.I.Mochi);
                }
            }
            Debug.Log("[SHOT] waiting");
            yield return new WaitForSeconds(4.0f);
            string dir = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots");
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, $"shot_{System.DateTime.Now:HHmmss}.png");
            Debug.Log("[SHOT] capturing");
            try { ScreenCapture.CaptureScreenshot(path); }
            catch (System.Exception e) { Debug.Log("[SHOT] capture failed: " + e.Message); }
            yield return new WaitForSeconds(2.0f);   // let the PNG flush before quitting
            Debug.Log($"Screenshot saved to {path}");
#if !UNITY_WEBGL
            Application.Quit();
#endif
        }

        void BuildCameras()
        {
            bgCam = new GameObject("BackgroundCamera").AddComponent<Camera>();
            bgCam.clearFlags = CameraClearFlags.SolidColor;
            bgCam.backgroundColor = Palette.DawnSky;
            bgCam.orthographic = true;
            bgCam.orthographicSize = 8.4375f;
            bgCam.depth = -2;
            bgCam.cullingMask = 0;

            mainCam = new GameObject("MainCamera").AddComponent<Camera>();
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Palette.DawnSky;
            mainCam.orthographic = true;
            mainCam.orthographicSize = 8.4375f;
            mainCam.depth = -1;
            mainCam.transform.position = new Vector3(20, 12, -10);

            // fit 16:9 view into any aspect with pastel letterbox bars
            FitCamera();
        }

        void FitCamera()
        {
            float aspect = Screen.width / (float)Screen.height;
            const float target = 16f / 9f;
            if (aspect > target)
            {
                float w = target / aspect;
                mainCam.rect = new Rect((1f - w) / 2f, 0, w, 1f);
            }
            else
            {
                float h = aspect / target;
                mainCam.rect = new Rect(0, (1f - h) / 2f, 1f, h);
            }
        }

        // --- world: ground, path, pond, fence, trees, flowers (level-driven) ---
        Transform BuildWorld()
        {
            var root = new GameObject("World").transform;
            root.SetParent(transform, false);
            var ground = new GameObject("Ground").transform;
            ground.SetParent(root, false);

            var lvl = LevelConfig.Current;
            var rand = new System.Random(lvl.flowers != null ? lvl.flowers.seed : 20260802);

            for (int x = 0; x < WorldW; x++)
            {
                for (int y = 0; y < WorldH; y++)
                {
                    var sr = MakeTile(ground, x, y, PickGround(x, y));
                    sr.sortingOrder = -5;
                }
            }

            // paths
            if (lvl.paths != null)
            {
                foreach (var path in lvl.paths)
                {
                    for (int x = path.x0; x <= path.x1; x++)
                        for (int y = path.y0; y <= path.y1; y++)
                            SetGround(x, y, SpriteBank.Path0);
                }
            }

            // pond
            if (lvl.pond != null)
            {
                for (int x = lvl.pond.x; x < lvl.pond.x + lvl.pond.w; x++)
                    for (int y = lvl.pond.y; y < lvl.pond.y + lvl.pond.h; y++)
                    {
                        var sr = MakeTile(ground, x, y, SpriteBank.WaterTile);
                        sr.sortingOrder = -4;
                    }
            }

            // fence perimeter
            for (int x = 0; x < WorldW; x++)
            {
                MakeTile(ground, x, 0, SpriteBank.Fence0).sortingOrder = -3;
                MakeTile(ground, x, WorldH - 1, SpriteBank.Fence0).sortingOrder = -3;
            }
            for (int y = 0; y < WorldH; y++)
            {
                MakeTile(ground, x: 0, y, SpriteBank.Fence1).sortingOrder = -3;
                MakeTile(ground, x: WorldW - 1, y, SpriteBank.Fence1).sortingOrder = -3;
            }

            // flowers (decor)
            int placed = 0, guard = 0;
            int flowerTarget = lvl.flowers != null ? lvl.flowers.count : 22;
            while (placed < flowerTarget && guard++ < 2000)
            {
                int x = rand.Next(1, WorldW - 1);
                int y = rand.Next(1, WorldH - 1);
                if (InFarmArea(x, y)) continue;
                if (x >= 27 && x <= 29 && y >= 15 && y <= 16) continue;
                if (x == 4 && y >= 6 && y <= 15) continue;
                if (y == 6 && x >= 4 && x <= 9) continue;
                if (IsOccupiedSpot(x, y)) continue;
                Sprite spr = rand.Next(4) == 0 ? SpriteBank.Flower2
                    : (rand.Next(3) == 0 ? SpriteBank.Daisy : (rand.Next(2) == 0 ? SpriteBank.Flower1 : SpriteBank.Flower0));
                var sr = MakeTile(ground, x, y, spr);
                sr.sortingOrder = -2;
                placed++;
            }

            // trees + blanket + shop + kitchen (level-driven)
            if (lvl.trees != null)
            {
                foreach (var t in lvl.trees)
                    MakeTree(root, new Vector2Int(t.x, t.y), t.peach);
            }
            var bspot = lvl.spots != null && lvl.spots.blanket != null ? lvl.spots.blanket : new[] { 30, 4 };
            var blanket = MakeTile(root, bspot[0], bspot[1], SpriteBank.Blanket);
            blanket.sortingOrder = 3;
            var sspot = lvl.spots != null && lvl.spots.shop != null ? lvl.spots.shop : new[] { 4, 15 };
            var shop = MakeTile(root, sspot[0], sspot[1], SpriteBank.ShopStand);
            shop.sortingOrder = 3;
            var kspot = lvl.spots != null && lvl.spots.kitchen != null ? lvl.spots.kitchen : new[] { 28, 5 };
            var kitchen = MakeTile(root, kspot[0], kspot[1], SpriteBank.KitchenTable);
            kitchen.sortingOrder = 3;
            return root;
        }

        bool InFarmArea(int x, int y) => x >= 9 && x <= 20 && y >= 7 && y <= 13;
        bool IsOccupiedSpot(int x, int y) =>
            (x == 31 && y == 5) || (x == 30 && y == 4) || (x == 25 && y == 3) || (x == 2 && y == 3) ||
            (x == 35 && y == 19) || (x == 6 && y == 20) || (x == 4 && y == 15);

        Sprite PickGround(int x, int y)
        {
            int h = (x * 31 + y * 17) % 5;
            return h == 0 ? SpriteBank.Grass0 : (h == 1 || h == 2 ? SpriteBank.Grass1 : SpriteBank.Grass2);
        }

        void SetGround(int x, int y, Sprite spr)
        {
            var sr = MakeTile(FindOrCreateGround(), x, y, spr);
            sr.sortingOrder = -5;
        }

        Transform groundRoot;
        readonly List<SpriteRenderer> canopies = new List<SpriteRenderer>();

        Transform FindOrCreateGround()
        {
            if (groundRoot != null) return groundRoot;
            groundRoot = transform.Find("World/Ground");
            return groundRoot;
        }

        SpriteRenderer MakeTile(Transform parent, int x, int y, Sprite spr)
        {
            var go = new GameObject($"tile_{x}_{y}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            return sr;
        }

        void MakeTree(Transform root, Vector2Int baseTile, bool peach)
        {
            var trunk = MakeTile(root, baseTile.x, baseTile.y, SpriteBank.TreeTrunkTile);
            trunk.sortingOrder = 2;

            var canopy = new GameObject("Canopy");
            canopy.transform.SetParent(root, false);
            canopy.transform.position = new Vector3(baseTile.x + 0.5f, baseTile.y + 1.65f, 0);
            canopy.transform.localScale = new Vector3(3.2f, 3.2f, 1);
            var sr = canopy.AddComponent<SpriteRenderer>();
            sr.sprite = peach ? MakePeachCanopy() : MakeGreenCanopy();
            sr.sortingOrder = 7;
            canopies.Add(sr);
        }

        static Texture2D canopyTex;
        public static Sprite MakePeachCanopy()
        {
            return MakeCanopy(Palette.PetalPink, Palette.Peach, Palette.Leaf);
        }

        public static Sprite MakeGreenCanopy()
        {
            return MakeCanopy(Palette.Leaf, Palette.Mint, Palette.LeafDark);
        }

        static Sprite MakeCanopy(Color main, Color light, Color leaf)
        {
            const int size = 64;
            var px = new Color[size * size];
            var rnd = new System.Random(7);
            // big blobby crown made of overlapping circles
            var centers = new List<Vector2>
            {
                new Vector2(20, 40), new Vector2(32, 46), new Vector2(44, 40), new Vector2(24, 30), new Vector2(40, 30), new Vector2(32, 22)
            };
            foreach (var c in centers)
            {
                float radius = rnd.Next(11, 15);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), c);
                        if (d < radius)
                        {
                            int i = y * size + x;
                            if (px[i].a <= 0.01f)
                            {
                                // pastel shading by height
                                float t = y / (float)size;
                                Color col = t < 0.45f ? main : Color.Lerp(main, light, (t - 0.45f) * 2f);
                                if (rnd.NextDouble() < 0.12) col = leaf;
                                if (rnd.NextDouble() < 0.08) col = light;
                                px[i] = new Color(col.r, col.g, col.b, 1f);
                            }
                        }
                    }
                }
            }
            // posterize: snap every opaque pixel to the nearest of 4 deliberate
            // shades (main / mid / light / leaf) so the canopy reads as clean bands.
            var shades = new[] { main, Color.Lerp(main, light, 0.5f), light, leaf };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    if (px[i].a <= 0.01f) continue;
                    Color c0 = px[i];
                    int best = 0; float bestD = float.MaxValue;
                    for (int k = 0; k < shades.Length; k++)
                    {
                        float dr = c0.r - shades[k].r, dg = c0.g - shades[k].g, db = c0.b - shades[k].b;
                        float dist = dr * dr + dg * dg + db * db;
                        if (dist < bestD) { bestD = dist; best = k; }
                    }
                    px[i] = new Color(shades[best].r, shades[best].g, shades[best].b, px[i].a);
                }
            }
            // soften edge: shrink alpha near boundary
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    if (px[i].a <= 0.01f) continue;
                    bool edge = false;
                    for (int dy = -1; dy <= 1 && !edge; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= size || ny >= size) { edge = true; break; }
                            if (px[ny * size + nx].a <= 0.01f) { edge = true; break; }
                        }
                    }
                    if (edge) px[i].a = 0.92f;
                }
            }
            return SpriteFactory.SpriteFrom(px, size, size);
        }

        static Sprite autumnCanopy, winterCanopy;
        public static Sprite MakeAutumnCanopy()
        {
            if (autumnCanopy == null) autumnCanopy = MakeCanopy(Palette.Orange, Palette.Peach, Palette.LeafDark);
            return autumnCanopy;
        }

        public static Sprite MakeWinterCanopy()
        {
            if (winterCanopy == null) winterCanopy = MakeCanopy(new Color(0.93f, 0.96f, 1f), Palette.BabyBlue, Palette.Cream);
            return winterCanopy;
        }

        // --- farm ---
        FarmGrid BuildFarm(Transform worldRoot, GameManager gm)
        {
            var farmGo = new GameObject("Farm");
            farmGo.transform.SetParent(transform, false);
            var farm = farmGo.AddComponent<FarmGrid>();

            var tileRoot = new GameObject("Tiles").transform;
            tileRoot.SetParent(farmGo.transform, false);
            var cropRoot = new GameObject("Crops").transform;
            cropRoot.SetParent(farmGo.transform, false);

            var renderers = new SpriteRenderer[FarmGrid.MaxW, FarmGrid.MaxH];
            for (int x = 0; x < FarmGrid.MaxW; x++)
            {
                for (int y = 0; y < FarmGrid.MaxH; y++)
                {
                    var go = new GameObject($"plot_{x}_{y}");
                    go.transform.SetParent(tileRoot, false);
                    go.transform.position = new Vector3(farm.Origin.x + x + 0.5f, farm.Origin.y + y + 0.5f, 0);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = -4;
                    renderers[x, y] = sr;

                    var cropGo = new GameObject($"crop_{x}_{y}");
                    cropGo.transform.SetParent(cropRoot, false);
                    cropGo.transform.position = go.transform.position + new Vector3(0, 0.18f, -0.1f);
                    var cropSr = cropGo.AddComponent<SpriteRenderer>();
                    cropSr.sortingOrder = 1;
                    cropGo.SetActive(false);
                }
            }
            var lf = LevelConfig.Current.farm;
            farm.Origin = new Vector2Int(lf.originX, lf.originY);
            farm.ActiveW = lf.baseW;
            farm.ActiveH = lf.baseH;
            farm.BlanketPos = new Vector3(LevelConfig.Current.spots.blanket[0] + 0.5f, LevelConfig.Current.spots.blanket[1] + 0.3f, 0);
            farm.Init(gm, renderers, cropRoot);
            return farm;
        }

        (PlayerController, NpcController) BuildCharacters(FarmGrid farm)
        {
            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(transform, false);
            playerGo.transform.position = new Vector3(LevelConfig.Current.playerSpawn.x, LevelConfig.Current.playerSpawn.y, -1);
            AddGroundShadow(playerGo.transform, new Vector3(0, -0.44f, 0));
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(playerGo.transform, false);
            var playerSr = bodyGo.AddComponent<SpriteRenderer>();
            playerSr.sprite = SpriteBank.PlayerDown0;
            playerSr.sortingOrder = 0;
            var player = playerGo.AddComponent<PlayerController>();
            player.Body = bodyGo.transform;

            var mochiGo = new GameObject("Mochi");
            mochiGo.transform.SetParent(transform, false);
            mochiGo.transform.position = new Vector3(4.5f, 14.2f, -1);
            AddGroundShadow(mochiGo.transform, new Vector3(0, -0.44f, 0));
            var mochiSr = mochiGo.AddComponent<SpriteRenderer>();
            mochiSr.sprite = SpriteBank.MochiIdle;
            mochiSr.sortingOrder = 0;
            var mochi = mochiGo.AddComponent<NpcController>();
            mochi.Home = new Vector2(4.5f, 14.2f);

            return (player, mochi);
        }

        static Sprite shadowSprite;
        public static void AddGroundShadow(Transform parent, Vector3 localPos)
        {
            if (shadowSprite == null)
                shadowSprite = SpriteFactory.SoftCircle(16, new Color(0.25f, 0.20f, 0.27f, 0.45f));
            var go = new GameObject("Shadow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(0.75f, 0.32f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = shadowSprite;
            sr.sortingOrder = -1;
        }

        void BuildQuestBoard(Transform worldRoot, Vector3 pos)
        {
            var board = MakeTile(worldRoot, (int)pos.x, (int)pos.y, SpriteBank.QuestBoard);
            board.sortingOrder = 3;
        }

        CritterController[] BuildCritters(FarmGrid farm)
        {
            var homes = new Vector2[LevelConfig.Current.chickens.Length];
            for (int i = 0; i < homes.Length; i++)
                homes[i] = new Vector2(LevelConfig.Current.chickens[i].x, LevelConfig.Current.chickens[i].y);
            var critters = new CritterController[homes.Length];
            for (int i = 0; i < homes.Length; i++)
            {
                var go = new GameObject("Chicken" + (i + 1));
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteBank.Chicken0;
                sr.sortingOrder = 0;
                go.transform.position = new Vector3(homes[i].x, homes[i].y, -1);
                AddGroundShadow(go.transform, new Vector3(0, -0.36f, 0));
                var c = go.AddComponent<CritterController>();
                c.Home = homes[i];
                c.Pink = i == 2;
                critters[i] = c;
            }
            return critters;
        }
    }

    // Smooth follow camera: pixel-perfect integer scaling + texel snapping.
    public class CameraRig : MonoBehaviour
    {
        public const float BaseViewArtHeight = 270f; // 16.875 tiles at PPU 16

        Camera cam;
        Transform target;

        public void Init(Camera camera, Transform follow, FarmGrid farm, GameBootstrap boot)
        {
            cam = camera;
            target = follow;
        }

        void LateUpdate()
        {
            if (cam == null || target == null) return;

            // --- pixel-perfect: pick an integer screen-pixel scale for art pixels ---
            float rectHpx = Screen.height * cam.rect.height;
            int scale = Mathf.Max(1, Mathf.RoundToInt(rectHpx / BaseViewArtHeight));
            cam.orthographicSize = (rectHpx / scale) / (2f * SpriteFactory.PixelsPerUnit);

            float halfH = cam.orthographicSize;
            // visible world width comes from the letterboxed rect aspect, not the screen aspect
            float rectAspect = (cam.rect.width * Screen.width) / (cam.rect.height * Screen.height);
            float halfW = halfH * rectAspect;

            float minX = halfW, maxX = GameBootstrap.WorldW - halfW;
            float minY = halfH, maxY = GameBootstrap.WorldH - halfH;

            Vector3 p = target.position;
            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.y = Mathf.Clamp(p.y, minY, maxY);
            // if the view is wider/taller than the world (extreme aspects), center it
            if (minX > maxX) p.x = GameBootstrap.WorldW * 0.5f;
            if (minY > maxY) p.y = GameBootstrap.WorldH * 0.5f;
            p.z = -10;

            Vector3 pos = Vector3.Lerp(cam.transform.position, p, Time.deltaTime * 6f);

            // snap to the texel grid so pixels stay uniform and sharp
            float step = 1f / (SpriteFactory.PixelsPerUnit * scale);
            pos.x = Mathf.Round(pos.x / step) * step;
            pos.y = Mathf.Round(pos.y / step) * step;
            cam.transform.position = pos;
        }
    }
}
