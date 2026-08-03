using System.Collections.Generic;
using UnityEngine;
using MochiMeadows.Art;
using MochiMeadows.Audio;

namespace MochiMeadows.Game
{
    // Cozy rain: rolled each morning, waters crops, darkens the sky,
    // plays soft rain, and draws falling streaks.
    public class WeatherController : MonoBehaviour
    {
        public static WeatherController I;

        public bool Raining { get; private set; }
        public float RainAmount { get; private set; }   // 0..1, for sky/audio blending

        GameManager gm;
        DayNightController dayNight;
        AudioService audio;

        readonly List<SpriteRenderer> streaks = new List<SpriteRenderer>();
        float rainRolledFor;
        float chirpT;

        void Awake() { I = this; }

        public void Init(GameManager gameManager, DayNightController dn, AudioService aud)
        {
            gm = gameManager;
            dayNight = dn;
            audio = aud;
            gm.OnNewDay += RollWeather;
        }

        void RollWeather()
        {
            bool willRain = Random.value < 0.20f; // ~1 rainy day per week
            SetRaining(willRain);
            if (willRain)
            {
                // rain waters everything for free
                var farm = gm.Farm;
                int watered = 0;
                for (int x = 0; x < farm.ActiveW; x++)
                {
                    for (int y = 0; y < farm.ActiveH; y++)
                    {
                        var p = farm.Get(x, y);
                        if ((p.state == TileState.Tilled || p.state == TileState.Cropped) && !p.wateredToday)
                        {
                            p.wateredToday = true;
                            if (p.state == TileState.Tilled) p.state = TileState.Watered;
                            farm.SetTileVisual(x, y);
                            watered++;
                        }
                    }
                }
                gm.Announce("It's raining! The sky is watering your crops~ \u2614");
                audio.Play(AudioService.Sfx.Water);
            }
            else
            {
                gm.Announce("Sunny skies today. Perfect farming weather!");
            }
        }

        public void ForceRain()
        {
            SetRaining(true);
        }

        void SetRaining(bool on)
        {
            Raining = on;
            if (on)
            {
                if (audio != null) audio.StartRain();
            }
            else
            {
                RainAmount = 0f;
                foreach (var s in streaks) s.gameObject.SetActive(false);
                if (audio != null) audio.StopRain();
            }
        }

        void Start()
        {
            // build the streak pool (inactive until it rains)
            var parent = new GameObject("RainStreaks").transform;
            parent.SetParent(transform, false);
            for (int i = 0; i < 70; i++)
            {
                var go = new GameObject("streak");
                go.transform.SetParent(parent, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteBank.WaterDrop;
                sr.color = new Color(0.75f, 0.88f, 1f, 0.55f);
                sr.sortingOrder = 25;
                go.transform.localScale = new Vector3(0.12f, 0.55f, 1f);
                go.SetActive(false);
                streaks.Add(sr);
            }
        }

        void Update()
        {
            if (gm == null) return;

            // dawn birdsong
            if (!Raining && gm.ClockMinutes > 6 * 60 && gm.ClockMinutes < 8 * 60)
            {
                chirpT -= Time.deltaTime;
                if (chirpT <= 0f)
                {
                    chirpT = Random.Range(5f, 14f);
                    audio.Play(AudioService.Sfx.Chirp);
                }
            }

            if (Raining)
            {
                RainAmount = Mathf.Lerp(RainAmount, 1f, Time.deltaTime * 2f);
                // spawn / recycle streaks across the visible world band
                for (int i = 0; i < streaks.Count; i++)
                {
                    var sr = streaks[i];
                    if (!sr.gameObject.activeSelf)
                    {
                        Vector3 p = new Vector3(Random.Range(0f, 40f), Random.Range(0f, 24f), 20f);
                        sr.transform.position = p;
                        sr.gameObject.SetActive(true);
                    }
                    Vector3 pos = sr.transform.position;
                    pos.y -= 9f * Time.deltaTime;
                    pos.x -= 1.2f * Time.deltaTime; // gentle wind
                    sr.transform.position = pos;
                    if (pos.y < -1f)
                    {
                        // recycle at the top so the rain keeps falling
                        sr.transform.position = new Vector3(Random.Range(0f, 40f), Random.Range(20f, 25f), 20f);
                    }
                }
                // sprinkle the sky with a soft gray
                if (dayNight != null)
                {
                    dayNight.RainFactor = Mathf.Lerp(dayNight.RainFactor, 1f, Time.deltaTime * 1.5f);
                    dayNight.RainClouds = true;
                }
            }
            else
            {
                if (dayNight != null)
                {
                    dayNight.RainFactor = Mathf.Lerp(dayNight.RainFactor, 0f, Time.deltaTime * 2f);
                    if (dayNight.RainFactor < 0.02f) dayNight.RainClouds = false;
                }
            }
        }

        void OnDestroy()
        {
            if (gm != null) gm.OnNewDay -= RollWeather;
        }
    }
}
