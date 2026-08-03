using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using MochiMeadows.Game;

namespace MochiMeadows.Core
{
    // Headless balance test: `-playtest [days]` simulates a cozy farmer for N days
    // and writes a report to persistentDataPath/playtest_report.txt.
    public class PlaytestSimulator : MonoBehaviour
    {
        public int Days = 14;

        void Start()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-playtest-days")
                    int.TryParse(args[i + 1], out Days);
                if (args[i] == "-playtest-seed" && int.TryParse(args[i + 1], out int seed))
                    Random.InitState(seed);
            }
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            yield return null;
            yield return null;
            GameManager.I.Ui.StartGame(false);

            var sb = new StringBuilder();
            sb.AppendLine("Mochi Meadows playtest report");
            sb.AppendLine($"simulated days: {Days}");
            sb.AppendLine($"day | season | money | energy | planted | harvested | sold | fish | quests");
            sb.AppendLine("----+--------+-------+--------+---------+-----------+-----+------+-------");

            int startMoney = GameManager.I.Money;
            int totalHarvested = 0;

            for (int day = 1; day <= Days; day++)
            {
                var gm = GameManager.I;
                int planted = 0, harvested = 0;

                // 1) water every planted crop
                int watered = 0;
                for (int x = 0; x < gm.Farm.ActiveW; x++)
                {
                    for (int y = 0; y < gm.Farm.ActiveH; y++)
                    {
                        var p = gm.Farm.Get(x, y);
                        if (p.state == TileState.Cropped && !p.wateredToday)
                        {
                            if (gm.Farm.Water(x, y) == FarmGrid.ActionResult.Ok) watered++;
                        }
                    }
                }

                // 2) harvest everything ready
                for (int x = 0; x < gm.Farm.ActiveW; x++)
                {
                    for (int y = 0; y < gm.Farm.ActiveH; y++)
                    {
                        var p = gm.Farm.Get(x, y);
                        if (p.state == TileState.Cropped && p.stage >= 3)
                        {
                            if (gm.Farm.Harvest(x, y) == FarmGrid.ActionResult.Ok)
                                harvested++;
                        }
                    }
                }

                // 3) plant into already-tilled empty plots first, then hoe new ones (max 4/day)
                int plantedTarget = 0;
                for (int x = 0; x < gm.Farm.ActiveW && plantedTarget < 4; x++)
                {
                    for (int y = 0; y < gm.Farm.ActiveH && plantedTarget < 4; y++)
                    {
                        var p = gm.Farm.Get(x, y);
                        if ((p.state == TileState.Tilled || p.state == TileState.Watered) && TryPlant(x, y, out _))
                        {
                            planted++;
                            plantedTarget++;
                        }
                    }
                }
                for (int x = 0; x < gm.Farm.ActiveW && plantedTarget < 4; x++)
                {
                    for (int y = 0; y < gm.Farm.ActiveH && plantedTarget < 4; y++)
                    {
                        var p = gm.Farm.Get(x, y);
                        if (p.state == TileState.Grass)
                        {
                            if (gm.Farm.Hoe(x, y) == FarmGrid.ActionResult.Ok && TryPlant(x, y, out _))
                            {
                                planted++;
                                plantedTarget++;
                            }
                        }
                    }
                }

                // 2.5) try fishing once (hand interact at the pond)
                int fishCaught = 0;
                {
                    var gm2 = GameManager.I;
                    if (gm2.Energy >= 3)
                    {
                        var pond = GameBootstrap.PondRect;
                        float px = pond.x + 0.5f, py = pond.y + pond.height + 0.5f;
                        // simulate a catch 60% of the time (tap timing)
                        bool caught = Random.value < 0.6f;
                        if (caught)
                        {
                            int roll = Random.Range(0, 100);
                            int fishId = roll < 50 ? 0 : roll < 82 ? 1 : 2;
                            gm2.AddFish(fishId, 1);
                            gm2.AddEnergy(2f);
                            gm2.SpendEnergy(3);
                            fishCaught++;
                        }
                    }
                }

                // 3.5) claim any completed quests
                int questsClaimed = 0;
                if (gm.Quests != null)
                {
                    for (int i = 0; i < gm.Quests.Today.Length; i++)
                        if (gm.Quests.Claim(gm.Quests.Today[i])) questsClaimed++;
                }

                // 4) evening: sell the basket, reinvest in the best seed we can afford
                int soldValue = gm.SellAllInBasket();
                BuyBestAffordableSeed();

                // 5) nap -> next day
                gm.SleepAtBlanket();
                while (gm.IsSleeping) yield return null;

                totalHarvested += harvested;
                sb.AppendLine($"{day,4} | {gm.SeasonName,-6} | {gm.Money,6} | {gm.Energy,5:0} | {planted,7} | {harvested,9} | {soldValue,5} | {fishCaught,4} | {questsClaimed,6}");
            }

            var final = GameManager.I;
            sb.AppendLine();
            sb.AppendLine($"start coins: {startMoney}   end coins: {final.Money}   crops harvested: {totalHarvested}");
            sb.AppendLine($"RESULT: {(final.Money >= startMoney * 2 ? "PASS (economy grows)" : "FAIL (economy stalls)")}");

            // --- save/load round-trip check ---
            var saved = GameManager.I;
            int moneyBefore = saved.Money;
            int dayBefore = saved.Day;
            int expBefore = saved.Farm.ExpansionTier;
            int[] seedsBefore = (int[])saved.Seeds.Clone();
            SaveSystem.Save(saved);
            var loaded = SaveSystem.Load();
            bool roundTripOk = loaded != null && loaded.money == moneyBefore && loaded.day == dayBefore
                && loaded.expansionTier == expBefore
                && loaded.seeds[0] == seedsBefore[0] && loaded.seeds[1] == seedsBefore[1];
            sb.AppendLine($"save/load round-trip: {(roundTripOk ? "PASS" : "FAIL")} (money {moneyBefore}->{loaded?.money})");

            string text = sb.ToString();
            Debug.Log("\n" + text);
            string path = Path.Combine(Application.persistentDataPath, "playtest_report.txt");
            File.WriteAllText(path, text);
            Debug.Log($"Playtest report written to {path}");
#if !UNITY_WEBGL
            Application.Quit();
#endif
        }

        bool TryPlant(int x, int y, out CropId crop)
        {
            var gm = GameManager.I;
            // pick the first seed type we own, cheapest first
            int[] order = { 0, 1, 3, 5, 2, 4 };
            foreach (int id in order)
            {
                if (gm.Seeds[id] > 0)
                {
                    crop = (CropId)id;
                    return gm.Farm.Plant(x, y, crop) == FarmGrid.ActionResult.Ok;
                }
            }
            crop = 0;
            return false;
        }

        void BuyBestAffordableSeed()
        {
            var gm = GameManager.I;
            // buy the best seed we can afford, up to 3 packets / keep 30 coins
            for (int buy = 0; buy < 3; buy++)
            {
                if (gm.Money <= 30) break;
                var season = gm.CurrentSeason;
                int best = -1; float bestScore = 0;
                for (int i = 0; i < CropDef.All.Length; i++)
                {
                    var def = CropDef.All[i];
                    if (def.SeedPrice > gm.Money) continue;
                    float bonus = def.Season == season ? CropDef.SeasonBonus : 1f;
                    float score = def.SellPrice * bonus / (float)def.SeedPrice / def.GrowthDays;
                    if (score > bestScore) { bestScore = score; best = i; }
                }
                if (best >= 0 && !gm.BuySeed((CropId)best)) break;
            }
        }
    }
}
