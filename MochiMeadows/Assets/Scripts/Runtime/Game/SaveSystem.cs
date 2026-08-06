using System;
using UnityEngine;
using MochiMeadows.Game;

namespace MochiMeadows.Game
{
    [Serializable]
    public class SaveData
    {
        public int day;
        public float clockMinutes;
        public int money;
        public float energy;
        public int[] seeds = new int[7];
        public int[] basket = new int[7];
        public int[] fishBasket = new int[4];
        public int eggBasket;
        public int honeyBasket;
        public int collectedCropsMask, collectedFishMask;
        public bool collectedEgg, collectedHoney;
        public int totalEarned;
        public int[] decorInventory = new int[6];
        public int decorCount;
        public int[] decorX = new int[GameManager.DecorMax];
        public int[] decorY = new int[GameManager.DecorMax];
        public int[] decorType = new int[GameManager.DecorMax];
        public int selectedSlot;
        public int[] hotbar = new int[10];
        // plots as parallel arrays
        public int[] plotState = new int[FarmGrid.MaxW * FarmGrid.MaxH];
        public int[] plotCrop = new int[FarmGrid.MaxW * FarmGrid.MaxH];
        public int[] plotStage = new int[FarmGrid.MaxW * FarmGrid.MaxH];
        public int[] plotWateredDays = new int[FarmGrid.MaxW * FarmGrid.MaxH];
        public bool[] plotWatered = new bool[FarmGrid.MaxW * FarmGrid.MaxH];
        public int[] plotRegrows = new int[FarmGrid.MaxW * FarmGrid.MaxH];
        public float playerX, playerY;
        public float mochiX, mochiY;
        public int activeW, activeH, expansionTier;
        public int outfitDress, outfitHair, ownedDressesMask, ownedHairsMask;
        public bool indoors;
        public bool hasSave;
    }

    public static class SaveSystem
    {
        const string Key = "mochi_meadows_save_v1";

        public static void Save(GameManager gm)
        {
            var data = new SaveData();
            var farm = gm.Farm;
            data.indoors = Core.MapManager.I != null && Core.MapManager.I.Indoors;
            data.outfitDress = gm.OutfitDress;
            data.outfitHair = gm.OutfitHair;
            data.ownedDressesMask = gm.OwnedDressesMask;
            data.ownedHairsMask = gm.OwnedHairsMask;
            data.activeW = farm.ActiveW;
            data.activeH = farm.ActiveH;
            data.expansionTier = farm.ExpansionTier;
            for (int x = 0; x < FarmGrid.MaxW; x++)
            {
                for (int y = 0; y < FarmGrid.MaxH; y++)
                {
                    var p = farm.Get(x, y);
                    int i = y * FarmGrid.MaxW + x;
                    data.plotState[i] = (int)p.state;
                    data.plotCrop[i] = p.cropId;
                    data.plotStage[i] = p.stage;
                    data.plotWateredDays[i] = p.wateredDays;
                    data.plotWatered[i] = p.wateredToday;
                    data.plotRegrows[i] = p.regrows;
                }
            }
            data.day = gm.Day;
            data.clockMinutes = gm.ClockMinutes;
            data.money = gm.Money;
            data.energy = gm.Energy;
            data.seeds = (int[])gm.Seeds.Clone();
            data.basket = (int[])gm.Basket.Clone();
            data.fishBasket = (int[])gm.FishBasket.Clone();
            data.eggBasket = gm.EggBasket;
            data.honeyBasket = gm.HoneyBasket;
            data.collectedCropsMask = gm.CollectedCropsMask;
            data.collectedFishMask = gm.CollectedFishMask;
            data.collectedEgg = gm.CollectedEgg;
            data.collectedHoney = gm.CollectedHoney;
            data.totalEarned = gm.TotalEarned;
            data.decorInventory = (int[])gm.DecorInventory.Clone();
            data.decorCount = gm.DecorCount;
            for (int i = 0; i < gm.DecorCount; i++)
            {
                data.decorX[i] = gm.DecorX[i];
                data.decorY[i] = gm.DecorY[i];
                data.decorType[i] = gm.DecorType[i];
            }
            data.selectedSlot = gm.SelectedSlot;
            data.hotbar = (int[])gm.Hotbar.Clone();
            if (gm.Player != null)
            {
                data.playerX = gm.Player.transform.position.x;
                data.playerY = gm.Player.transform.position.y;
            }
            if (gm.Mochi != null)
            {
                data.mochiX = gm.Mochi.transform.position.x;
                data.mochiY = gm.Mochi.transform.position.y;
            }
            data.hasSave = true;

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        public static SaveData Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
                return data != null && data.hasSave ? data : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool HasSave => PlayerPrefs.HasKey(Key) && Load() != null;
    }
}
