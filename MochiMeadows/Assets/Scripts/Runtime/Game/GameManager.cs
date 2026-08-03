using System;
using System.Collections.Generic;
using UnityEngine;
using MochiMeadows.Art;
using MochiMeadows.Audio;

namespace MochiMeadows.Game
{
    // Central game state: day, clock, money, energy, inventory.
    public class GameManager : MonoBehaviour
    {
        public static GameManager I;

        public const int CostHoe = 3;
        public const int CostWater = 3;
        public const int CostPlant = 2;
        public const int RewardHarvest = 2;
        public const int MaxEnergy = 100;
        public const int DayStartMinute = 6 * 60;     // 6:00
        public const int DayEndMinute = 24 * 60;      // 24:00
        const float DayLengthSeconds = 360f;          // one cozy day = 6 real minutes
        public const float MinutesPerSecond = (DayEndMinute - DayStartMinute) / DayLengthSeconds;

        public int Day = 1;
        public float ClockMinutes = DayStartMinute;   // minutes since midnight
        public int Money = 60;
        public float Energy = MaxEnergy;
        public int[] Seeds = new int[6];              // seed packets owned
        public int[] Basket = new int[6];             // harvested items
        public int[] FishBasket = new int[3];         // caught fish
        public int EggBasket;                          // chicken eggs

        // --- decorations ---
        public static readonly string[] DecorNames = { "Fence", "Flower", "Lantern", "Gnome" };
        public static readonly int[] DecorCosts = { 10, 15, 30, 50 };
        public int[] DecorInventory = new int[4];
        public int DecorPlacing = -1;
        public const int DecorMax = 40;
        public int DecorCount;
        public readonly int[] DecorX = new int[DecorMax];
        public readonly int[] DecorY = new int[DecorMax];
        public readonly int[] DecorType = new int[DecorMax];
        Transform decorRoot;

        public void InitDecorRoot(Transform root) { decorRoot = root; }

        public bool BuyDecor(int i)
        {
            int cost = DecorCosts[i];
            if (Money < cost) { Announce("Not enough coins~"); Audio.Play(AudioService.Sfx.Nope); return false; }
            Money -= cost;
            DecorInventory[i]++;
            Audio.Play(AudioService.Sfx.Coin);
            Announce($"+1 {DecorNames[i]}");
            OnMoneyChanged?.Invoke();
            return true;
        }

        public Sprite DecorSprite(int i) =>
            i == 0 ? SpriteBank.Fence1 : i == 1 ? SpriteBank.Flower0 : i == 2 ? SpriteBank.Lantern : SpriteBank.Gnome;

        public void StartPlacing(int i)
        {
            if (i < 0 || i >= DecorInventory.Length || DecorInventory[i] <= 0) return;
            DecorPlacing = i;
            Audio.Play(AudioService.Sfx.UISelect);
            Announce($"Tap a grassy spot to place the {DecorNames[i]} (tap placed decor to pick it up)");
        }

        public bool PlaceDecor(int wx, int wy)
        {
            if (DecorPlacing < 0 || DecorInventory[DecorPlacing] <= 0 || DecorCount >= DecorMax) return false;
            if (!Core.GameBootstrap.IsDecorSpotFree(wx, wy)) { Announce("Not a spot for that~"); Audio.Play(AudioService.Sfx.Tap); return false; }
            for (int i = 0; i < DecorCount; i++)
                if (DecorX[i] == wx && DecorY[i] == wy) return false;
            int idx = DecorCount++;
            DecorX[idx] = wx; DecorY[idx] = wy; DecorType[idx] = DecorPlacing;
            DecorInventory[DecorPlacing]--;
            SpawnDecorVisual(idx);
            Audio.Play(AudioService.Sfx.Pop);
            if (DecorInventory[DecorPlacing] <= 0) DecorPlacing = -1;
            return true;
        }

        public bool TryPickupDecor(int wx, int wy)
        {
            if (DecorPlacing < 0) return false;
            for (int i = 0; i < DecorCount; i++)
            {
                if (DecorX[i] == wx && DecorY[i] == wy)
                {
                    DecorInventory[DecorType[i]]++;
                    if (decorRoot != null)
                    {
                        var child = decorRoot.Find($"decor_{i}");
                        if (child != null) UnityEngine.Object.Destroy(child.gameObject);
                    }
                    // compact the list
                    for (int j = i; j < DecorCount - 1; j++)
                    {
                        DecorX[j] = DecorX[j + 1]; DecorY[j] = DecorY[j + 1]; DecorType[j] = DecorType[j + 1];
                    }
                    DecorCount--;
                    Audio.Play(AudioService.Sfx.Pop);
                    Announce($"{DecorNames[DecorType[i]]} picked up!");
                    return true;
                }
            }
            return false;
        }

        void SpawnDecorVisual(int idx)
        {
            if (decorRoot == null) return;
            var go = new GameObject($"decor_{idx}");
            go.transform.SetParent(decorRoot, false);
            go.transform.position = new Vector3(DecorX[idx] + 0.5f, DecorY[idx] + 0.5f, -2.5f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DecorSprite(DecorType[idx]);
            sr.sortingOrder = -2;
        }

        public void RebuildDecorVisuals()
        {
            if (decorRoot == null) return;
            for (int i = 0; i < decorRoot.childCount; i++)
                UnityEngine.Object.Destroy(decorRoot.GetChild(i).gameObject);
            for (int i = 0; i < DecorCount; i++)
                SpawnDecorVisual(i);
        }
        public const int EggSellPrice = 15;
        public const int EggEnergy = 12;
        public bool IsSleeping;
        public bool GameStarted;

        public int[] Hotbar = { 0, 1, 2, 3, 4, 5, 6, 7, 8 }; // slot -> item id (see ItemId)
        public bool DevPlaytest;

        // --- player outfits ---
        public static readonly string[] DressNames = { "Strawberry", "Mint", "Sky", "Lavender", "Peach" };
        public static readonly Color[] DressColors =
        {
            Palette.StrawberryPink, Palette.Mint, Palette.BabyBlue, Palette.Lavender, Palette.Peach,
        };
        public static readonly string[] HairNames = { "Cocoa", "Plum", "Espresso", "Caramel" };
        public static readonly Color[] HairColors =
        {
            Palette.Chocolate, new Color(0.42f, 0.27f, 0.41f), new Color(0.30f, 0.20f, 0.15f), new Color(0.66f, 0.44f, 0.28f),
        };
        public const int DressCost = 60;
        public const int HairCost = 40;
        public int OutfitDress;
        public int OutfitHair;
        public int OwnedDressesMask = 1;   // bitmask, bit 0 always owned
        public int OwnedHairsMask = 1;

        public bool OwnsDress(int i) => (OwnedDressesMask & (1 << i)) != 0;
        public bool OwnsHair(int i) => (OwnedHairsMask & (1 << i)) != 0;

        public bool BuyDress(int i)
        {
            if (OwnsDress(i)) { ApplyOutfit(i, OutfitHair); return true; }
            if (Money < DressCost) { Announce("Not enough coins for the makeover~"); Audio.Play(AudioService.Sfx.Nope); return false; }
            Money -= DressCost;
            OwnedDressesMask |= 1 << i;
            ApplyOutfit(i, OutfitHair);
            Announce($"{DressNames[i]} dress! So cute~");
            Audio.Play(AudioService.Sfx.Coin);
            OnMoneyChanged?.Invoke();
            return true;
        }

        public bool BuyHair(int i)
        {
            if (OwnsHair(i)) { ApplyOutfit(OutfitDress, i); return true; }
            if (Money < HairCost) { Announce("Not enough coins for the makeover~"); Audio.Play(AudioService.Sfx.Nope); return false; }
            Money -= HairCost;
            OwnedHairsMask |= 1 << i;
            ApplyOutfit(OutfitDress, i);
            Announce($"{HairNames[i]} hair! Adorable~");
            Audio.Play(AudioService.Sfx.Coin);
            OnMoneyChanged?.Invoke();
            return true;
        }

        public void ApplyOutfit(int dressIdx, int hairIdx)
        {
            OutfitDress = Mathf.Clamp(dressIdx, 0, DressColors.Length - 1);
            OutfitHair = Mathf.Clamp(hairIdx, 0, HairColors.Length - 1);
            SpriteBank.RebuildPlayerSprites(DressColors[OutfitDress], HairColors[OutfitHair]);
        }
        public int SelectedSlot;

        public FarmGrid Farm;
        public Ui.UiController Ui;
        public PlayerController Player;
        public NpcController Mochi;
        public CritterController[] Critters;
        public QuestManager Quests;
        public Audio.AudioService Audio;

        public Action OnMinuteTick;              // pass hour
        public Action OnNewDay;
        public Action OnMoneyChanged;
        public Action OnEnergyChanged;
        public Action OnBasketChanged;
        public Action<string> OnToast;

        float saveTimer;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        public void Init(FarmGrid farm, Ui.UiController ui, PlayerController player, NpcController mochi, Audio.AudioService audio)
        {
            Farm = farm; Ui = ui; Player = player; Mochi = mochi; Audio = audio;
        }

        void Update()
        {
            if (!GameStarted || IsSleeping) return;

            ClockMinutes += Time.deltaTime * MinutesPerSecond;
            if (ClockMinutes >= DayEndMinute)
            {
                ClockMinutes = DayEndMinute;
                ForceSleep("You nodded off under the stars...");
                return;
            }
            if (OnMinuteTick != null)
            {
                int h = Hour;
                OnMinuteTick();
            }

            // gentle sleepiness at the end of the day
            if (ClockMinutes > 23 * 60 && Energy > 0 && Mathf.Repeat(ClockMinutes, 5) < 0.5f)
                AddEnergy(-0.5f);

            saveTimer += Time.deltaTime;
            if (saveTimer > 45f) { saveTimer = 0; if (!DevPlaytest) SaveSystem.Save(this); }
        }

        public int Hour => Mathf.FloorToInt(ClockMinutes / 60f);
        public int MinuteOfHour => Mathf.FloorToInt(ClockMinutes % 60f);
        public bool IsNight => ClockMinutes >= 20 * 60;

        public Season CurrentSeason => (Season)(((Day - 1) / 7) % 4);

        public string SeasonName => CurrentSeason.ToString();

        public string DayText => $"{Day}";

        public string ClockText
        {
            get
            {
                int h = Hour; int m = MinuteOfHour;
                if (h >= 24) { h = 24; m = 0; }
                string ampm = h >= 12 ? "PM" : "AM";
                int h12 = h % 12; if (h12 == 0) h12 = 12;
                return $"{h12}:{m:00} {ampm}";
            }
        }

        // --- Energy ---
        public bool SpendEnergy(float amount)
        {
            if (Energy < amount) { Announce("So sleepy... take a nap under the peach tree!"); Audio.Play(AudioService.Sfx.Nope); return false; }
            Energy -= amount;
            OnEnergyChanged?.Invoke();
            return true;
        }

        public void AddEnergy(float amount)
        {
            Energy = Mathf.Clamp(Energy + amount, 0, MaxEnergy);
            OnEnergyChanged?.Invoke();
        }

        // --- Economy ---
        public bool CanAfford(int cost) => Money >= cost;

        public bool BuySeed(CropId crop)
        {
            var def = CropDef.Get(crop);
            if (Money < def.SeedPrice) { Announce("Not enough coins~ Sell something to Mochi!"); Audio.Play(AudioService.Sfx.Nope); return false; }
            Money -= def.SeedPrice;
            Seeds[(int)crop]++;
            Audio.Play(AudioService.Sfx.Coin);
            Quests?.OnBuy();
            Announce($"+1 {def.SeedName}");
            OnMoneyChanged?.Invoke();
            return true;
        }

        public bool TryUseSeed(CropId crop)
        {
            if (Seeds[(int)crop] <= 0) { Announce($"No {CropDef.Get(crop).SeedName} left! Buy them from Mochi~"); return false; }
            Seeds[(int)crop]--;
            return true;
        }

        public void RefundSeed(CropId crop) { Seeds[(int)crop]++; }

        public int FishBasketTotalValue()
        {
            int total = 0;
            for (int i = 0; i < FishBasket.Length; i++)
                total += FishBasket[i] * FishDef.All[i].SellPrice;
            return total;
        }

        public void AddFish(int fishId, int count)
        {
            FishBasket[fishId] += count;
            Quests?.OnFish();
            OnBasketChanged?.Invoke();
        }

        public void AddToBasket(CropId crop, int count)
        {
            Basket[(int)crop] += count;
            OnBasketChanged?.Invoke();
        }

        public int SellAllInBasket()
        {
            int total = 0;
            for (int i = 0; i < Basket.Length; i++)
            {
                if (Basket[i] > 0)
                {
                    total += Basket[i] * CropDef.Get((CropId)i).CurrentSellPrice(CurrentSeason);
                    Basket[i] = 0;
                }
            }
            int fishTotal = 0;
            for (int i = 0; i < FishBasket.Length; i++)
            {
                if (FishBasket[i] > 0)
                {
                    fishTotal += FishBasket[i] * FishDef.All[i].SellPrice;
                    FishBasket[i] = 0;
                }
            }
            int eggTotal = EggBasket * EggSellPrice;
            EggBasket = 0;
            System.Array.Clear(DecorInventory, 0, DecorInventory.Length);
            DecorPlacing = -1;
            DecorCount = 0;
            if (total > 0 || fishTotal > 0 || eggTotal > 0)
            {
                Money += total + fishTotal + eggTotal;
                Announce($"Sold the basket for {total + fishTotal + eggTotal} coins! Meow~");
                Quests?.OnSell(total + fishTotal + eggTotal);
                Audio.Play(AudioService.Sfx.Coin);
                OnMoneyChanged?.Invoke();
                OnBasketChanged?.Invoke();
            }
            return total + fishTotal + eggTotal;
        }

        public void EatEgg()
        {
            if (EggBasket <= 0) return;
            EggBasket--;
            AddEnergy(EggEnergy);
            Audio.Play(AudioService.Sfx.Pop);
            Announce("Yum! A warm egg. +energy~");
            OnBasketChanged?.Invoke();
        }

        // --- cooking ---
        public class Recipe
        {
            public string Name;
            public string Desc;
            public int CropId;
            public int CropCount;
            public int Energy;
            public Sprite Icon;

            public static readonly Recipe[] All =
            {
                new Recipe { Name = "Strawberry Tart", Desc = "2 Strawberries", CropId = 0, CropCount = 2, Energy = 20 },
                new Recipe { Name = "Blueberry Muffin", Desc = "2 Blueberries", CropId = 1, CropCount = 2, Energy = 25 },
                new Recipe { Name = "Pumpkin Soup", Desc = "1 Pumpkitten", CropId = 2, CropCount = 1, Energy = 30 },
                new Recipe { Name = "Mochi Balls", Desc = "2 Mochi Rice", CropId = 5, CropCount = 2, Energy = 30 },
            };
        }

        public bool CanCook(Recipe r) => Basket[r.CropId] >= r.CropCount && Energy < MaxEnergy;

        public bool Cook(Recipe r)
        {
            if (!CanCook(r)) return false;
            Basket[r.CropId] -= r.CropCount;
            AddEnergy(r.Energy);
            Audio.Play(AudioService.Sfx.Sprout);
            Announce($"Cooked {r.Name}! Yummy +{r.Energy} energy~");
            Quests?.OnCook();
            OnBasketChanged?.Invoke();
            return true;
        }

        public int BasketTotalValue()
        {
            int total = 0;
            for (int i = 0; i < Basket.Length; i++)
                total += Basket[i] * CropDef.Get((CropId)i).CurrentSellPrice(CurrentSeason);
            return total;
        }

        // --- Sleep & days ---
        public void SleepAtBlanket()
        {
            if (IsSleeping) return;
            Announce("Sweet dreams~ <3");
            StartCoroutine(SleepRoutine());
        }

        public void ForceSleep(string message)
        {
            if (IsSleeping) return;
            Announce(message);
            StartCoroutine(SleepRoutine());
        }

        System.Collections.IEnumerator SleepRoutine()
        {
            IsSleeping = true;
            yield return Ui.FadeToBlack(true, 1.2f);
            Day++;
            ClockMinutes = DayStartMinute;
            Energy = MaxEnergy;
            OnEnergyChanged?.Invoke();
            Farm.GrowWateredCrops();
            Farm.StartNewDay();
            OnNewDay?.Invoke();
            if (Critters != null)
            {
                int laid = 0;
                for (int i = 0; i < Critters.Length; i++)
                {
                    if (Critters[i] == null) continue;
                    if (Critters[i].PetsToday > 0) laid++;
                    Critters[i].NewDay();
                }
                if (laid > 0)
                {
                    EggBasket += laid;
                    Quests?.OnEgg(laid);
                    Announce($"The chickens laid {laid} egg{(laid > 1 ? "s" : "")}! They love your pets~");
                }
            }
            if (Quests != null) Quests.NewDay();
            if (Day % 7 == 1) Announce($"A new season begins... {SeasonName}!");
            if (!DevPlaytest) SaveSystem.Save(this);
            yield return new WaitForSeconds(0.6f);
            yield return Ui.FadeToBlack(false, 1.2f);
            IsSleeping = false;
            Announce($"Good morning! Day {Day} ~");
        }

        public void Announce(string msg)
        {
            if (Ui != null) Ui.ShowToast(msg);
        }

        public void SelectHotbar(int slot)
        {
            SelectedSlot = Mathf.Clamp(slot, 0, Hotbar.Length - 1);
            Ui.RefreshHotbar();
        }

        public int SelectedItem => Hotbar[SelectedSlot];
        public bool IsFishing => Ui != null && Ui.IsFishing;

        // --- start / load ---
        public void StartGame(bool load, Ui.UiController ui)
        {
            if (load)
            {
                var data = SaveSystem.Load();
                if (data != null) ApplySave(data);
            }
            else
            {
                ResetToNewGame();
            }
            GameStarted = true;
            if (Quests != null && (Quests.Today[0] == null || Quests.Today[0].Target == 0)) Quests.NewDay();
            ui.RefreshAll();
            Audio.Play(AudioService.Sfx.Wake);
            Announce("Welcome to Mochi Meadows! Talk to Mochi the cat to get seeds~");
        }

        void ResetToNewGame()
        {
            Day = 1;
            ClockMinutes = DayStartMinute;
            Money = 60;
            Energy = MaxEnergy;
            System.Array.Clear(Seeds, 0, Seeds.Length);
            System.Array.Clear(Basket, 0, Basket.Length);
            System.Array.Clear(FishBasket, 0, FishBasket.Length);
            EggBasket = 0;
            System.Array.Clear(DecorInventory, 0, DecorInventory.Length);
            DecorPlacing = -1;
            DecorCount = 0;
            Seeds[0] = 2; // two strawberry packets to start
            OutfitDress = 0; OutfitHair = 0;
            OwnedDressesMask = 1; OwnedHairsMask = 1;
            SpriteBank.RebuildPlayerSprites(DressColors[0], HairColors[0]);
            SelectedSlot = 0;
            Hotbar = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
            Player.TeleportTo(new Vector3(8.5f, 14.5f, -1));
            if (Mochi != null) Mochi.transform.position = new Vector3(4.5f, 14.2f, -1);
            Farm.ResetGrid();
            OnMoneyChanged?.Invoke();
            OnEnergyChanged?.Invoke();
            OnBasketChanged?.Invoke();
        }

        void ApplySave(SaveData data)
        {
            Day = data.day;
            OutfitDress = Mathf.Clamp(data.outfitDress, 0, DressColors.Length - 1);
            OutfitHair = Mathf.Clamp(data.outfitHair, 0, HairColors.Length - 1);
            OwnedDressesMask = data.ownedDressesMask == 0 ? 1 : data.ownedDressesMask;
            OwnedHairsMask = data.ownedHairsMask == 0 ? 1 : data.ownedHairsMask;
            SpriteBank.RebuildPlayerSprites(DressColors[OutfitDress], HairColors[OutfitHair]);
            ClockMinutes = data.clockMinutes;
            Money = data.money;
            Energy = Mathf.Clamp(data.energy, 0, MaxEnergy);
            for (int i = 0; i < Seeds.Length; i++)
                Seeds[i] = i < data.seeds.Length ? data.seeds[i] : 0;
            for (int i = 0; i < Basket.Length; i++)
                Basket[i] = i < data.basket.Length ? data.basket[i] : 0;
            for (int i = 0; i < FishBasket.Length; i++)
                FishBasket[i] = i < data.fishBasket.Length ? data.fishBasket[i] : 0;
            EggBasket = data.eggBasket;
            DecorCount = Mathf.Min(data.decorCount, DecorMax);
            for (int i = 0; i < DecorCount; i++)
            {
                DecorX[i] = i < data.decorX.Length ? data.decorX[i] : 0;
                DecorY[i] = i < data.decorY.Length ? data.decorY[i] : 0;
                DecorType[i] = i < data.decorType.Length ? data.decorType[i] : 0;
            }
            for (int i = 0; i < DecorInventory.Length; i++)
                DecorInventory[i] = i < data.decorInventory.Length ? data.decorInventory[i] : 0;
            RebuildDecorVisuals();
            SelectedSlot = Mathf.Clamp(data.selectedSlot, 0, Hotbar.Length - 1);
            Hotbar = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
            for (int i = 0; i < Hotbar.Length; i++)
                Hotbar[i] = i < data.hotbar.Length ? data.hotbar[i] : i;
            if (data.playerX != 0 || data.playerY != 0)
                Player.TeleportTo(new Vector3(data.playerX, data.playerY, -1));
            if (Mochi != null && (data.mochiX != 0 || data.mochiY != 0))
                Mochi.transform.position = new Vector3(data.mochiX, data.mochiY, -1);

            // plots
            if (data.activeW >= 12 && data.activeH >= 7)
            {
                Farm.ActiveW = data.activeW;
                Farm.ActiveH = data.activeH;
                Farm.ExpansionTier = data.expansionTier;
            }
            for (int x = 0; x < FarmGrid.MaxW; x++)
            {
                for (int y = 0; y < FarmGrid.MaxH; y++)
                {
                    int i = y * FarmGrid.MaxW + x;
                    if (i >= data.plotState.Length) continue;
                    var pd = new PlotData
                    {
                        state = (TileState)data.plotState[i],
                        cropId = data.plotCrop[i],
                        stage = data.plotStage[i],
                        wateredDays = data.plotWateredDays[i],
                        wateredToday = data.plotWatered[i],
                    };
                    Farm.ImportPlot(x, y, pd);
                }
            }
            Farm.RefreshAll();
            OnMoneyChanged?.Invoke();
            OnEnergyChanged?.Invoke();
            OnBasketChanged?.Invoke();
            Announce("Welcome back, farmer~ Your farm is waiting!");
        }
    }
}
