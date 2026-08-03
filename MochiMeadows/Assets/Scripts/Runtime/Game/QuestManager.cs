using System;
using UnityEngine;
using MochiMeadows.Audio;

namespace MochiMeadows.Game
{
    public enum QuestType { Water = 0, Harvest = 1, Hoe = 2, Plant = 3, Pet = 4, Sell = 5, Buy = 6, Fish = 7, Egg = 8, Cook = 9 }

    public class FishDef
    {
        public int Id;
        public string Name;
        public int SellPrice;
        public int Weight;   // catch probability weight

        public static readonly FishDef[] All =
        {
            new FishDef { Id = 0, Name = "Goldfish", SellPrice = 25, Weight = 50 },
            new FishDef { Id = 1, Name = "Bubble Fish", SellPrice = 40, Weight = 32 },
            new FishDef { Id = 2, Name = "Sakura Fish", SellPrice = 70, Weight = 18 },
        };
    }

    [Serializable]
    public class QuestDef
    {
        public QuestType Type;
        public string Title;
        public int Target;
        public int Reward;
        public int Progress;
        public bool Claimed;
    }

    // Daily cozy tasks posted on the bulletin board by Mochi.
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager I;

        public QuestDef[] Today = new QuestDef[3];
        public Vector3 BoardPos;

        int water, harvest, hoe, plant, pet, sellValue, buy, fish, egg, cook;

        public int[] ClaimedToday = new int[3];

        void Awake() { I = this; }

        public void NewDay()
        {
            for (int i = 0; i < 3; i++)
            {
                water = harvest = hoe = plant = pet = sellValue = buy = fish = egg = cook = 0;
                Today[i] = Generate();
            }
            Array.Clear(ClaimedToday, 0, ClaimedToday.Length);
        }

        QuestDef Generate()
        {
            var rnd = new System.Random();
            int roll = rnd.Next(0, 12);
            switch (roll)
            {
                case 0:
                case 1: return new QuestDef { Type = QuestType.Water, Title = "Water your crops", Target = 6, Reward = 30 };
                case 2:
                case 3: return new QuestDef { Type = QuestType.Harvest, Title = "Harvest ripe crops", Target = 4, Reward = 40 };
                case 4: return new QuestDef { Type = QuestType.Hoe, Title = "Hoe new soil", Target = 5, Reward = 25 };
                case 5: return new QuestDef { Type = QuestType.Plant, Title = "Plant seeds", Target = 4, Reward = 30 };
                case 6: return new QuestDef { Type = QuestType.Pet, Title = "Pet the chickens", Target = 3, Reward = 20 };
                case 7:
                case 8: return new QuestDef { Type = QuestType.Sell, Title = "Sell to Mochi", Target = 100, Reward = 50 };
                case 9: return new QuestDef { Type = QuestType.Fish, Title = "Catch a fish in the pond", Target = 1, Reward = 30 };
                case 10: return new QuestDef { Type = QuestType.Egg, Title = "Pet chickens so they lay eggs", Target = 1, Reward = 25 };
                case 11: return new QuestDef { Type = QuestType.Cook, Title = "Cook a cozy snack", Target = 1, Reward = 35 };
                default: return new QuestDef { Type = QuestType.Buy, Title = "Buy seed packets", Target = 2, Reward = 25 };
            }
        }

        void Tick(QuestType type, int amount = 1)
        {
            switch (type)
            {
                case QuestType.Water: water += amount; break;
                case QuestType.Harvest: harvest += amount; break;
                case QuestType.Hoe: hoe += amount; break;
                case QuestType.Plant: plant += amount; break;
                case QuestType.Pet: pet += amount; break;
                case QuestType.Sell: sellValue += amount; break;
                case QuestType.Buy: buy += amount; break;
                case QuestType.Fish: fish += amount; break;
                case QuestType.Egg: egg += amount; break;
                case QuestType.Cook: cook += amount; break;
            }
            for (int i = 0; i < Today.Length; i++)
            {
                if (Today[i] == null || Today[i].Claimed || Today[i].Type != type) continue;
                int v = type == QuestType.Sell ? sellValue : Value(Today[i].Type);
                Today[i].Progress = Mathf.Min(v, Today[i].Target);
            }
            var gm = GameManager.I;
            if (gm != null && gm.Ui != null) gm.Ui.RefreshQuestPanel();
        }

        int Value(QuestType t)
        {
            switch (t)
            {
                case QuestType.Water: return water;
                case QuestType.Harvest: return harvest;
                case QuestType.Hoe: return hoe;
                case QuestType.Plant: return plant;
                case QuestType.Pet: return pet;
                case QuestType.Sell: return sellValue;
                case QuestType.Buy: return buy;
                case QuestType.Fish: return fish;
                case QuestType.Egg: return egg;
                case QuestType.Cook: return cook;
            }
            return 0;
        }

        public bool IsComplete(QuestDef q) => q != null && !q.Claimed && q.Progress >= q.Target;

        public bool Claim(QuestDef q)
        {
            if (q == null || q.Claimed || !IsComplete(q)) return false;
            q.Claimed = true;
            GameManager.I.Money += q.Reward;
            GameManager.I.Announce($"Quest done! +{q.Reward} coins!");
            GameManager.I.Audio.Play(AudioService.Sfx.Harvest);
            GameManager.I.OnMoneyChanged?.Invoke();
            return true;
        }

        public void OnWater(int n = 1) => Tick(QuestType.Water, n);
        public void OnHarvest(int n = 1) => Tick(QuestType.Harvest, n);
        public void OnHoe(int n = 1) => Tick(QuestType.Hoe, n);
        public void OnPlant(int n = 1) => Tick(QuestType.Plant, n);
        public void OnPet(int n = 1) => Tick(QuestType.Pet, n);
        public void OnSell(int value) => Tick(QuestType.Sell, value);
        public void OnBuy(int n = 1) => Tick(QuestType.Buy, n);
        public void OnFish(int n = 1) => Tick(QuestType.Fish, n);
        public void OnEgg(int n = 1) => Tick(QuestType.Egg, n);
        public void OnCook(int n = 1) => Tick(QuestType.Cook, n);

        public int RemainingRewards()
        {
            int total = 0;
            foreach (var q in Today)
                if (IsComplete(q)) total += q.Reward;
            return total;
        }
    }
}
