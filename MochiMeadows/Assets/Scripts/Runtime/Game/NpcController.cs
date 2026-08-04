using UnityEngine;
using MochiMeadows.Art;
using MochiMeadows.Audio;

namespace MochiMeadows.Game
{
    // Mochi the marshmallow cat: wanders, sleeps at night, runs the shop.
    public class NpcController : MonoBehaviour
    {
        public Vector2 Home;
        public float WanderRadius = 2.2f;
        public float TalkRange = 1.8f;

        SpriteRenderer sr;
        AnimatorFrames anim = new AnimatorFrames();
        Vector2 target;
        float waitT;
        bool moving;
        GameObject zzz;

        public bool IsSleeping => GameManager.I != null && GameManager.I.IsNight;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            target = transform.position;
            BuildZzz();
        }

        void BuildZzz()
        {
            zzz = new GameObject("Zzz");
            zzz.transform.SetParent(transform, false);
            zzz.transform.localPosition = new Vector3(0.35f, 0.9f, -0.1f);
            zzz.transform.localScale = Vector3.one * 0.5f;
            var zsr = zzz.AddComponent<SpriteRenderer>();
            zsr.sprite = SpriteBank.Zzz;
            zzz.SetActive(false);
        }

        void Update()
        {
            if (GameManager.I == null || GameManager.I.IsSleeping || GameManager.I.IsPaused) return;

            bool sleeping = IsSleeping;
            zzz.SetActive(sleeping);
            if (sleeping)
            {
                moving = false;
                sr.sprite = SpriteBank.MochiIdle;
                return;
            }

            waitT -= Time.deltaTime;
            if (waitT <= 0f && !moving)
            {
                // pick new wander point
                Vector2 p = Home + new Vector2(Random.Range(-WanderRadius, WanderRadius), Random.Range(-WanderRadius, WanderRadius));
                target = p;
                moving = true;
                waitT = Random.Range(1.2f, 3.5f);
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
                    Vector2 dir = to.normalized;
                    transform.position += (Vector3)(dir * 1.1f * Time.deltaTime);
                    transform.localScale = new Vector3(dir.x < 0 ? -1 : 1, 1, 1);
                }
            }

            anim.Update(moving);
            sr.sprite = moving ? (anim.Frame == 0 ? SpriteBank.MochiWalk0 : SpriteBank.MochiWalk1) : SpriteBank.MochiIdle;
        }

        public void TalkTo()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            if (IsSleeping)
            {
                gm.Announce("Mochi is snoozing... Zzz");
                gm.Audio.Play(AudioService.Sfx.MeowSoft);
                return;
            }
            gm.Audio.Play(AudioService.Sfx.Meow);
            gm.Ui.OpenShop(this);
        }
    }
}
