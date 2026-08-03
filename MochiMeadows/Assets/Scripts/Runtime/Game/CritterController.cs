using UnityEngine;
using MochiMeadows.Art;
using MochiMeadows.Audio;

namespace MochiMeadows.Game
{
    // Kawaii farm critters: wander, sleep at night, love pets.
    public class CritterController : MonoBehaviour
    {
        public Vector2 Home;
        public float WanderRadius = 2.0f;
        public float PetRange = 1.5f;
        public bool Pink;

        SpriteRenderer sr;
        AnimatorFrames anim = new AnimatorFrames();
        Vector2 target;
        float waitT;
        bool moving;
        public int PetsToday { get; private set; }

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            target = transform.position;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.IsSleeping) return;

            if (gm.IsNight)
            {
                moving = false;
                sr.sprite = Pink ? SpriteBank.ChickenPink0 : SpriteBank.Chicken0;
                return;
            }

            waitT -= Time.deltaTime;
            if (waitT <= 0f && !moving)
            {
                target = Home + new Vector2(Random.Range(-WanderRadius, WanderRadius), Random.Range(-WanderRadius, WanderRadius));
                moving = true;
                waitT = Random.Range(1.0f, 3.0f);
            }

            if (moving)
            {
                Vector2 to = target - (Vector2)transform.position;
                if (to.sqrMagnitude < 0.02f)
                {
                    moving = false;
                }
                else
                {
                    transform.position += (Vector3)(to.normalized * 1.0f * Time.deltaTime);
                    transform.localScale = new Vector3(to.x < 0 ? -1 : 1, 1, 1);
                }
            }

            anim.Update(moving);
            sr.sprite = moving
                ? (anim.Frame == 0
                    ? (Pink ? SpriteBank.ChickenPink1 : SpriteBank.Chicken1)
                    : (Pink ? SpriteBank.ChickenPink2 : SpriteBank.Chicken2))
                : (Pink ? SpriteBank.ChickenPink0 : SpriteBank.Chicken0);
        }

        public void Pet()
        {
            var gm = GameManager.I;
            if (gm == null || gm.IsNight) return;

            if (PetsToday >= 3)
            {
                gm.Announce("The chicken is all loved out for today~");
                gm.Audio.Play(AudioService.Sfx.Tap);
                return;
            }
            PetsToday++;
            gm.AddEnergy(2f);
            QuestManager.I?.OnPet();
            gm.Audio.Play(AudioService.Sfx.MeowSoft);
            var ui = gm.Ui;
            if (ui != null)
            {
                ui.ShowBubble(transform.position + new Vector3(0, 0.8f, 0), Pink ? "Kyu~ <3" : "Peep~!", 1.6f);
                gm.Farm.SpawnTilePuff(new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y)), Palette.DeepPink);
            }
        }

        public void NewDay() => PetsToday = 0;
    }
}
