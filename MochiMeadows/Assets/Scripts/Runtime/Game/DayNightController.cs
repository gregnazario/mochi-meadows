using UnityEngine;
using MochiMeadows.Art;

namespace MochiMeadows.Game
{
    // Pastel day/night: sky color, sun/moon arc, stars, drifting clouds.
    public class DayNightController : MonoBehaviour
    {
        public Camera MainCam;
        public Camera BgCam;
        public GameManager Gm;

        // Keyframes: minute-of-day -> sky color
        static readonly (float minute, Color color)[] Keyframes =
        {
            (6 * 60,  Palette.DawnSky),      // 6:00 dawn
            (8 * 60,  Palette.Sky),          // 8:00 day
            (17 * 60, Palette.Sky),          // 17:00
            (18 * 60, Palette.SunsetSky),    // 18:00 sunset
            (20 * 60, Palette.SunsetSky),    // 20:00
            (20 * 60 + 30, Palette.Lavender),// 20:30 dusk
            (21 * 60, Palette.NightSky),     // 21:00 night
            (24 * 60, Palette.NightSky),
        };

        SpriteRenderer sun, moon, nightOverlay;
        public Color SeasonTint = Color.white;
        public float SeasonTintWeight;
        public float RainFactor;
        public bool RainClouds;
        SpriteRenderer[] stars;
        SpriteRenderer[] clouds;
        float[] cloudSpeeds;
        float[] cloudBase;
        Color currentSky = Palette.DawnSky;

        void Start()
        {
            BuildSky();
        }

        void BuildSky()
        {
            float y = 17f; // sky layer height
            sun = MakeSkySprite(SpriteBank.Sun, new Vector3(0, y, 10), 1.2f);
            moon = MakeSkySprite(SpriteBank.Moon, new Vector3(0, y, 10), 1.2f);

            nightOverlay = new GameObject("NightOverlay").AddComponent<SpriteRenderer>();
            nightOverlay.sprite = SpriteFactory.SoftCircle(2, Color.white);
            nightOverlay.color = new Color(0.20f, 0.18f, 0.38f, 0f);
            nightOverlay.transform.localScale = new Vector3(200f, 200f, 1f);
            nightOverlay.transform.position = new Vector3(0, 0, 5f);

            stars = new SpriteRenderer[50];
            for (int i = 0; i < stars.Length; i++)
            {
                var s = MakeSkySprite(SpriteBank.Star, new Vector3(Random.Range(0f, 40f) - 4f, Random.Range(12f, 18.5f), 9.9f), Random.Range(0.5f, 0.9f));
                stars[i] = s;
            }

            clouds = new SpriteRenderer[4];
            cloudSpeeds = new float[4];
            cloudBase = new float[4];
            for (int i = 0; i < clouds.Length; i++)
            {
                clouds[i] = MakeSkySprite(SpriteBank.Cloud, new Vector3(Random.Range(0f, 36f), Random.Range(14f, 17.5f), 9.8f), Random.Range(1.4f, 2.2f));
                clouds[i].color = new Color(1f, 1f, 1f, 0.85f);
                cloudBase[i] = 0.85f;
                cloudSpeeds[i] = Random.Range(0.25f, 0.6f);
            }
        }

        SpriteRenderer MakeSkySprite(Sprite sprite, Vector3 pos, float scale)
        {
            var go = new GameObject(sprite.name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.transform.position = pos;
            sr.transform.localScale = new Vector3(scale, scale, 1);
            return sr;
        }

        void Update()
        {
            if (Gm == null) return;
            float m = Gm.ClockMinutes;

            currentSky = SampleSky(m);
            if (SeasonTintWeight > 0f)
                currentSky = Color.Lerp(currentSky, SeasonTint, SeasonTintWeight);
            if (RainFactor > 0f)
                currentSky = Color.Lerp(currentSky, new Color(0.58f, 0.63f, 0.72f), RainFactor * 0.45f);
            MainCam.backgroundColor = currentSky;
            BgCam.backgroundColor = currentSky;

            // sun/moon arc across the sky
            float tDay = Mathf.InverseLerp(6 * 60, 20 * 60, m);
            float tNight = Mathf.InverseLerp(20 * 60, 24 * 60, m);
            float sunX = Mathf.Lerp(-16f, 16f, tDay);
            float sunY = Mathf.Lerp(9f, 16.5f, Mathf.Sin(tDay * Mathf.PI));
            sun.transform.position = new Vector3(sunX, sunY, 10f);
            float nightFactor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(19 * 60 + 30, 21 * 60, m));
            sun.color = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.75f, 0f), Mathf.Clamp01(nightFactor * 1.2f));
            if (RainFactor > 0f) sun.color = new Color(sun.color.r, sun.color.g, sun.color.b, sun.color.a * (1f - RainFactor * 0.6f));
            sun.gameObject.SetActive(nightFactor < 0.98f);

            float moonX = Mathf.Lerp(20f, -14f, tNight);
            moon.transform.position = new Vector3(moonX, 15.5f, 10f);
            moon.color = new Color(1f, 1f, 1f, nightFactor);
            moon.gameObject.SetActive(nightFactor > 0.02f);

            nightOverlay.color = new Color(0.18f, 0.16f, 0.36f, nightFactor * 0.35f);

            for (int i = 0; i < stars.Length; i++)
            {
                float tw = 0.55f + 0.45f * Mathf.Sin(Time.time * (1f + i * 0.13f) + i * 1.7f);
                stars[i].color = new Color(1f, 1f, 1f, nightFactor * tw);
            }

            for (int i = 0; i < clouds.Length; i++)
            {
                clouds[i].transform.position += Vector3.right * cloudSpeeds[i] * Time.deltaTime;
                if (clouds[i].transform.position.x > 40f)
                    clouds[i].transform.position = new Vector3(-4f, clouds[i].transform.position.y, 9.8f);
                clouds[i].color = new Color(1f, 1f, 1f, Mathf.Lerp(cloudBase[i], 0.55f, RainFactor));
            }
        }

        public static Color SampleSky(float minute)
        {
            minute = Mathf.Clamp(minute, 6 * 60, 24 * 60);
            for (int i = 0; i < Keyframes.Length - 1; i++)
            {
                var a = Keyframes[i]; var b = Keyframes[i + 1];
                if (minute >= a.minute && minute <= b.minute)
                {
                    float t = Mathf.InverseLerp(a.minute, b.minute, minute);
                    return Color.Lerp(a.color, b.color, t);
                }
            }
            return Keyframes[Keyframes.Length - 1].color;
        }
    }
}
