using UnityEngine;
using MochiMeadows.Art;
using MochiMeadows.Game;
using MochiMeadows.Inputs;
using MochiMeadows.Core;
using MochiMeadows.Audio;

namespace MochiMeadows.Game
{
    // Chibi player: walk, face, interact with tools.
    public class PlayerController : MonoBehaviour
    {
        public enum ItemId { Hoe = 0, Can = 1, Strawberry = 2, Blueberry = 3, Pumpkitten = 4, Sakura = 5, Melon = 6, MochiRice = 7, Hand = 8 }

        public float Speed = 3.4f;

        public Transform Body;
        SpriteRenderer sr;
        AnimatorFrames anim = new AnimatorFrames();
        Vector2 facing = Vector2.down;
        float actionCooldown;
        Vector3 bobBase;
        float bobT;
        float stepT;
        Collider2D[] overlaps = new Collider2D[16];

        public Vector2Int FacingTile
        {
            get
            {
                Vector2Int t = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
                return t + new Vector2Int(Mathf.RoundToInt(facing.x), Mathf.RoundToInt(facing.y));
            }
        }

        public Vector2 Facing => facing;

        void Start()
        {
            sr = Body.GetComponent<SpriteRenderer>();
            bobBase = transform.position;
        }

        void Update()
        {
            var input = InputService.I;
            if (input == null || GameManager.I == null) return;
            var gm = GameManager.I;

            if (gm.IsSleeping || gm.IsPaused) return;
            if (gm.IsFishing) return;

            Vector2 move = input.MoveAxis;
            bool moving = move.sqrMagnitude > 0.01f;
            if (moving)
            {
                transform.position += (Vector3)(move * Speed * Time.deltaTime);
                if (Mathf.Abs(move.x) > Mathf.Abs(move.y)) facing = new Vector2(Mathf.Sign(move.x), 0);
                else facing = new Vector2(0, Mathf.Sign(move.y));
                stepT -= Time.deltaTime;
                if (stepT <= 0f)
                {
                    stepT = 0.3f;
                    gm.Audio.Play(AudioService.Sfx.Step);
                }
            }
            bobT += Time.deltaTime * (moving ? 9f : 1f);
            Vector3 pos = transform.position;
            pos.z = -1f;
            transform.position = pos;
            Body.localPosition = new Vector3(0, Mathf.Sin(bobT * Mathf.PI) * (moving ? 0.05f : 0f), 0);

            anim.Update(moving);
            UpdateSprite();

            // --- actions ---
            actionCooldown -= Time.deltaTime;
            bool wantAct = (input.ActionPressed || input.ActPressed || (input.ActionHeld && actionCooldown <= -0.15f));
            Vector2Int target = FacingTile;

            // Touch tap overrides target tile
            if (input.TapTileX.HasValue)
            {
                target = new Vector2Int(input.TapTileX.Value, input.TapTileY.Value);
                wantAct = true;
            }

            if (wantAct && actionCooldown <= 0f)
            {
                actionCooldown = 0.28f;
                DoAction(target, gm);
            }
            input.ConsumeTap();

            // hotbar selection
            if (input.HotbarPressed.HasValue)
                gm.SelectHotbar(input.HotbarPressed.Value);
        }

        void UpdateSprite()
        {
            int f = anim.Frame;
            if (facing.y > 0.5f)
                sr.sprite = f == 0 ? SpriteBank.PlayerUp0 : SpriteBank.PlayerUp1;
            else if (facing.y < -0.5f)
                sr.sprite = f == 0 ? SpriteBank.PlayerDown0 : SpriteBank.PlayerDown1;
            else
            {
                sr.sprite = f == 0 ? SpriteBank.PlayerSide0 : SpriteBank.PlayerSide1;
                Body.localScale = new Vector3(facing.x < 0 ? -1f : 1f, 1f, 1f);
            }
        }

        void DoAction(Vector2Int target, GameManager gm)
        {
            var farm = gm.Farm;
            int item = gm.SelectedItem;

            // 1) Interact with things in the world (NPC, blanket, shop) with the hand
            if (item == (int)ItemId.Hand)
            {
                if (TryPetCritter()) return;
                if (TryTalkToMochi()) return;
                if (TryOpenQuestBoard()) return;
                if (TryFish()) return;
                if (TryOpenKitchen()) return;
                if (TrySleepAtBlanket()) return;
            }

            // 2) Farm tile action
            var result = FarmGrid.ActionResult.Nothing;
            switch ((ItemId)item)
            {
                case ItemId.Hoe: result = farm.Hoe(target.x, target.y); break;
                case ItemId.Can: result = farm.Water(target.x, target.y); break;
                case ItemId.Strawberry: case ItemId.Blueberry: case ItemId.Pumpkitten: case ItemId.Sakura:
                case ItemId.Melon: case ItemId.MochiRice:
                    result = farm.Plant(target.x, target.y, (CropId)(item - (int)ItemId.Strawberry)); break;
                case ItemId.Hand: result = farm.Harvest(target.x, target.y); break;
            }

            // feedback
            switch (result)
            {
                case FarmGrid.ActionResult.Ok:
                    if (item == (int)ItemId.Hoe) { gm.Audio.Play(AudioService.Sfx.Hoe); gm.Audio.Play(AudioService.Sfx.Swing); gm.Farm.SpawnArc(target, Art.Palette.Wood); gm.Farm.SpawnTilePuff(target, Art.Palette.Soil); }
                    else if (item == (int)ItemId.Can) { gm.Audio.Play(AudioService.Sfx.Water); gm.Audio.Play(AudioService.Sfx.Swing); gm.Farm.SpawnArc(target, Art.Palette.BabyBlue); gm.Farm.SpawnTilePuff(target, Art.Palette.BabyBlue); }
                    else if (item >= (int)ItemId.Strawberry && item <= (int)ItemId.MochiRice) { gm.Audio.Play(AudioService.Sfx.Sprout); gm.Farm.SpawnArc(target, Art.Palette.Mint); gm.Farm.SpawnTilePuff(target, Art.Palette.Mint); }
                    break;
                case FarmGrid.ActionResult.NoEnergy: break;
                case FarmGrid.ActionResult.WrongTile: gm.Audio.Play(AudioService.Sfx.Tap); break;
                case FarmGrid.ActionResult.NoSeeds: gm.Audio.Play(AudioService.Sfx.Tap); break;
                case FarmGrid.ActionResult.AlreadyWatered: gm.Audio.Play(AudioService.Sfx.Tap); break;
                case FarmGrid.ActionResult.NotReady:
                    gm.Audio.Play(AudioService.Sfx.Pop);
                    var crop = farm.Get(target.x, target.y);
                    if (crop.cropId >= 0)
                        gm.Announce($"{CropDef.Get((CropId)crop.cropId).Name} is still growing... keep watering it!");
                    break;
            }
        }

        bool TryPetCritter()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Critters == null) return false;
            for (int i = 0; i < gm.Critters.Length; i++)
            {
                var c = gm.Critters[i];
                if (c == null) continue;
                if (Vector2.Distance(transform.position, c.transform.position) <= c.PetRange)
                {
                    c.Pet();
                    return true;
                }
            }
            return false;
        }

        bool TryOpenKitchen()
        {
            var gm = GameManager.I;
            if (gm == null) return false;
            if (Vector2.Distance(transform.position, GameBootstrap.KitchenPos) > 1.9f) return false;
            gm.Audio.Play(AudioService.Sfx.UISelect);
            gm.Ui.OpenCooking();
            return true;
        }

        bool TryFish()
        {
            var gm = GameManager.I;
            if (gm == null) return false;
            var pond = GameBootstrap.PondRect;
            float px = transform.position.x, py = transform.position.y;
            float nearestX = Mathf.Clamp(px, pond.x, pond.x + pond.width);
            float nearestY = Mathf.Clamp(py, pond.y, pond.y + pond.height);
            if (Mathf.Abs(px - nearestX) > 1.4f || Mathf.Abs(py - nearestY) > 1.4f) return false;
            gm.Ui.OpenFishing();
            return true;
        }

        bool TryOpenQuestBoard()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Quests == null) return false;
            if (Vector2.Distance(transform.position, gm.Quests.BoardPos) > 1.9f) return false;
            gm.Audio.Play(AudioService.Sfx.UISelect);
            gm.Ui.OpenQuests();
            return true;
        }

        bool TryTalkToMochi()
        {
            var mochi = GameManager.I.Mochi;
            if (mochi == null) return false;
            float d = Vector2.Distance(transform.position, mochi.transform.position);
            if (d > 1.8f) return false;
            mochi.TalkTo();
            return true;
        }

        bool TrySleepAtBlanket()
        {
            var blanket = GameManager.I.Farm.BlanketPos;
            if (blanket == Vector3.zero) return false;
            if (Vector2.Distance(transform.position, blanket) > 1.8f) return false;
            GameManager.I.SleepAtBlanket();
            return true;
        }

        public void TeleportTo(Vector3 worldPos) { transform.position = worldPos; }
    }

    // Tiny walk-cycle timer (2 frames).
    public class AnimatorFrames
    {
        float t;
        public int Frame => (int)(t * 2f) % 2;
        public void Update(bool moving)
        {
            t += Time.deltaTime * (moving ? 4f : 1.5f);
        }
    }
}
