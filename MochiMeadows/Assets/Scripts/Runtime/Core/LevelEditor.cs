using System.Collections.Generic;
using UnityEngine;
using MochiMeadows.Art;
using MochiMeadows.Game;

namespace MochiMeadows.Core
{
    // In-game level editor: `-edit` opens a palette; tap tiles to place/remove
    // ground, decor, trees, and special spots. "Save level" writes the layout
    // to level_export.json (see LEVEL_GUIDE.md).
    public class LevelEditor : MonoBehaviour
    {
        public static LevelEditor I;

        enum Item
        {
            Grass, Path, Water,
            DecorFence, DecorFlower, DecorLantern, DecorGnome, DecorBeehive, DecorChime,
            Tree, PeachTree, Shop, QuestBoard, Blanket, Kitchen, HouseDoor, None
        }

        static readonly string[] ItemNames =
        {
            "Grass", "Path", "Water",
            "Fence", "Flower", "Lantern", "Gnome", "Beehive", "Chime",
            "Tree", "Peach Tree", "Shop", "Quest Board", "Blanket", "Kitchen", "House Door", "",
        };

        Item selected = Item.None;
        GameObject panel;
        Transform worldRoot;

        public bool Active => selected != Item.None;

        public void Init(Transform world)
        {
            I = this;
            worldRoot = world;
            BuildPanel();
        }

        void BuildPanel()
        {
            panel = new GameObject("LevelEditorPanel");
            panel.transform.SetParent(GameManager.I.Ui.Canvas.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0, 96);

            var bg = Ui.UiController.CreateImage(panel.transform, "Bg", Ui.UiController.RoundedSprite(Palette.Cream), new Color(1, 1, 1, 0.92f));
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.offsetMin = Vector2.zero;
            bg.rectTransform.offsetMax = Vector2.zero;

            int count = 16;
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                var btn = Ui.UiController.CreateButton(panel.transform, "Item" + i, ItemNames[i], 14, Color.white, out var bBg);
                var brt = (RectTransform)btn.transform;
                brt.anchorMin = new Vector2(0, 0);
                brt.anchorMax = new Vector2(0, 1);
                brt.pivot = new Vector2(0, 0.5f);
                brt.anchoredPosition = new Vector2(8 + i * 82f, 8);
                brt.sizeDelta = new Vector2(76, 64);
                bBg.sprite = Ui.UiController.RoundedSprite(Palette.BabyBlue);
                bBg.type = UnityEngine.UI.Image.Type.Sliced;
                bBg.color = new Color(0.8f, 0.87f, 0.98f, 1);
                btn.onClick.AddListener(() =>
                {
                    selected = idx == count - 1 ? Item.None : (Item)idx;
                    GameManager.I.Announce(selected == Item.None ? "Editor: pick an item above"
                        : "Editor: tap tiles to place " + ItemNames[idx] + " (tap placed items to remove)");
                });
            }

            var save = Ui.UiController.CreateButton(panel.transform, "Save", "Save level", 18, Color.white, out var sBg);
            var srt = (RectTransform)save.transform;
            srt.anchorMin = new Vector2(1, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(1, 0.5f);
            srt.anchoredPosition = new Vector2(-10, 8);
            srt.sizeDelta = new Vector2(120, 64);
            sBg.sprite = Ui.UiController.RoundedSprite(Palette.Mint);
            sBg.type = UnityEngine.UI.Image.Type.Sliced;
            sBg.color = new Color(0.75f, 0.95f, 0.8f, 1);
            save.onClick.AddListener(() =>
            {
                string path = LevelExporter.Export();
                GameManager.I.Announce("Level saved! Relaunch to load it.");
            });
        }

        public void OnTileTap(int wx, int wy)
        {
            var lvl = LevelConfig.Current;
            switch (selected)
            {
                case Item.Grass:
                case Item.Path:
                case Item.Water:
                    ToggleGround(wx, wy, (int)selected);
                    break;
                case Item.DecorFence: case Item.DecorFlower: case Item.DecorLantern:
                case Item.DecorGnome: case Item.DecorBeehive: case Item.DecorChime:
                    ToggleDecor(wx, wy, (int)selected - (int)Item.DecorFence);
                    break;
                case Item.Tree:
                case Item.PeachTree:
                    ToggleTree(wx, wy, selected == Item.PeachTree);
                    break;
                default:
                    ToggleSpot(wx, wy, (int)selected - (int)Item.Shop);
                    break;
            }
        }

        void ToggleGround(int x, int y, int t)
        {
            var lvl = LevelConfig.Current;
            var list = new List<GroundOverride>(lvl.ground ?? new GroundOverride[0]);
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].x == x && list[i].y == y) list.RemoveAt(i);
            if (t != 0) list.Add(new GroundOverride { x = x, y = y, t = t });
            lvl.ground = list.ToArray();

            var ground = worldRoot.Find("Ground");
            var go = ground != null ? ground.Find($"tile_{x}_{y}") : null;
            if (go != null)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = t == 1 ? SpriteBank.Path0 : t == 2 ? SpriteBank.WaterTile : null;
                if (sr.sprite == null) sr.sprite = GameBootstrap.I != null ? null : null;
                if (t == 0) sr.sprite = PickGrass(x, y);
                sr.sortingOrder = t == 2 ? -4 : -5;
            }
        }

        Sprite PickGrass(int x, int y)
        {
            int h = (x * 31 + y * 17) % 5;
            return h == 0 ? SpriteBank.Grass0 : (h == 1 || h == 2 ? SpriteBank.Grass1 : SpriteBank.Grass2);
        }

        void ToggleDecor(int x, int y, int type)
        {
            var gm = GameManager.I;
            for (int i = 0; i < gm.DecorCount; i++)
            {
                if (gm.DecorX[i] == x && gm.DecorY[i] == y)
                {
                    gm.PickupDecorAt(i);
                    return;
                }
            }
            if (!GameBootstrap.IsDecorSpotFree(x, y)) { gm.Announce("Not a free spot~"); return; }
            gm.AddDecorDirect(x, y, type);
        }

        void ToggleTree(int x, int y, bool peach)
        {
            var lvl = LevelConfig.Current;
            var list = new List<LevelTree>(lvl.trees ?? new LevelTree[0]);
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].x == x && list[i].y == y) { list.RemoveAt(i); RemoveTileVisual(x, y); }
            if (list.Count == 0 || !ContainsTree(list, x, y))
            {
                list.Add(new LevelTree { x = x, y = y, peach = peach });
                // spawn visual
                var canopy = new GameObject("Canopy");
                canopy.transform.SetParent(worldRoot, false);
                canopy.transform.position = new Vector3(x + 0.5f, y + 1.65f, 0);
                canopy.transform.localScale = Vector3.one * 3.2f;
                var sr = canopy.AddComponent<SpriteRenderer>();
                sr.sprite = peach ? GameBootstrap.MakePeachCanopy() : GameBootstrap.MakeGreenCanopy();
                sr.sortingOrder = 7;
                var trunk = MakeTile(worldRoot, x, y, SpriteBank.TreeTrunkTile);
                trunk.sortingOrder = 2;
            }
            lvl.trees = list.ToArray();
        }

        static bool ContainsTree(List<LevelTree> list, int x, int y)
        {
            foreach (var t in list) if (t.x == x && t.y == y) return true;
            return false;
        }

        void ToggleSpot(int x, int y, int spotIdx)
        {
            var lvl = LevelConfig.Current;
            var spots = lvl.spots;
            var target = spotIdx switch
            {
                0 => spots.shop, 1 => spots.questBoard, 2 => spots.blanket,
                3 => spots.kitchen, _ => spots.houseDoor,
            };
            if (target != null && target.Length == 2 && target[0] == x && target[1] == y)
            {
                // remove
                SetSpot(spotIdx, new[] { -100, -100 });
                RemoveTileVisual(x, y);
                return;
            }
            SetSpot(spotIdx, new[] { x, y });
            var sprite = spotIdx switch
            {
                0 => SpriteBank.ShopStand, 1 => SpriteBank.QuestBoard, 2 => SpriteBank.Blanket,
                3 => SpriteBank.KitchenTable, _ => SpriteBank.Door,
            };
            var tile = MakeTile(worldRoot, x, y, sprite);
            tile.sortingOrder = 3;
            GameManager.I.Announce("Spot moved! (Save level to keep it)");
        }

        void SetSpot(int spotIdx, int[] xy)
        {
            var spots = LevelConfig.Current.spots;
            switch (spotIdx)
            {
                case 0: spots.shop = xy; break;
                case 1: spots.questBoard = xy; break;
                case 2: spots.blanket = xy; break;
                case 3: spots.kitchen = xy; break;
                default: spots.houseDoor = xy; break;
            }
        }

        void RemoveTileVisual(int x, int y)
        {
            var go = worldRoot.Find($"decor_{x}_{y}");
            if (go != null) Destroy(go.gameObject);
        }

        SpriteRenderer MakeTile(Transform parent, int x, int y, Sprite spr)
        {
            var go = new GameObject($"tile_{x}_{y}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
            return go.AddComponent<SpriteRenderer>();
        }
    }
}
