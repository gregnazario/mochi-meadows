using UnityEngine;
using MochiMeadows.Art;
using MochiMeadows.Core;

namespace MochiMeadows.Game
{
    // Drives the four seasons: world tint, tree canopies, sky shift, badge.
    public class SeasonController : MonoBehaviour
    {
        public GameManager Gm;
        public DayNightController DayNight;
        public SpriteRenderer[] Canopies;
        public SpriteRenderer TintQuad;

        Season current = (Season)(-1);
        static readonly Color[] TintColors =
        {
            new Color(1f, 1f, 1f, 0f),        // spring: fresh, no tint
            new Color(1f, 0.98f, 0.90f, 0.06f), // summer: warm sun
            new Color(1f, 0.78f, 0.45f, 0.14f), // autumn: golden
            new Color(0.85f, 0.90f, 1f, 0.20f), // winter: snow light
        };
        static readonly Color[] SkyTints =
        {
            new Color(1f, 1f, 1f, 0f),          // spring
            new Color(1f, 0.93f, 0.78f, 0.22f), // summer warmth
            new Color(1f, 0.84f, 0.60f, 0.30f), // autumn gold
            new Color(0.80f, 0.87f, 1f, 0.35f), // winter pale
        };

        void Update()
        {
            if (Gm == null) return;
            Season now = Gm.CurrentSeason;
            if (now == current) return;
            current = now;
            Apply(now);
        }

        void Apply(Season season)
        {
            int s = (int)season;

            if (TintQuad != null)
                TintQuad.color = TintColors[s];

            if (DayNight != null)
            {
                DayNight.SeasonTint = SkyTints[s];
                DayNight.SeasonTintWeight = s == 0 ? 0f : (s == 1 ? 0.20f : 0.32f);
            }

            if (Canopies != null)
            {
                for (int i = 0; i < Canopies.Length; i++)
                {
                    if (Canopies[i] == null) continue;
                    Canopies[i].sprite = CanopyFor(season);
                }
            }

            Debug.Log($"[Season] {season}");
        }

        public static Sprite CanopyFor(Season season)
        {
            switch (season)
            {
                case Season.Summer: return GameBootstrap.MakeGreenCanopy();
                case Season.Autumn: return GameBootstrap.MakeAutumnCanopy();
                case Season.Winter: return GameBootstrap.MakeWinterCanopy();
                default: return GameBootstrap.MakePeachCanopy();
            }
        }
    }
}
