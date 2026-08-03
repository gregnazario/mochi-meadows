using System.Collections.Generic;
using UnityEngine;

namespace MochiMeadows.Audio
{
    // All sound is synthesized at runtime — no audio files in the repo.
    public class AudioService : MonoBehaviour
    {
        public enum Sfx
        {
            Pop, Coin, Water, Hoe, Sprout, Harvest, Tap, Nope, Meow, MeowSoft, Sleep, Wake, UISelect,
            Step, Swing, Splash, Chirp
        }

        public static AudioService I;

        AudioSource sfxSource;
        AudioSource musicSource;
        AudioSource rainSource;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        bool musicOn = true;
        public float MusicVolume = 1f;
        public float SfxVolume = 1f;

        // gentle lullaby state
        float nextNoteTime;
        int step;

        void Awake()
        {
            I = this;
            BuildClips();
        }

        void Start()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            rainSource = gameObject.AddComponent<AudioSource>();
            rainSource.playOnAwake = false;
            rainSource.loop = true;
            rainSource.volume = 0f;
            nextNoteTime = Time.time + 0.8f;
        }

        void BuildClips()
        {
            clips[Sfx.Pop] = Tone(new float[] { 620f, 880f }, new float[] { 0.06f, 0.10f }, 0.35f);
            clips[Sfx.UISelect] = Tone(new float[] { 740f, 990f }, new float[] { 0.05f, 0.08f }, 0.30f);
            clips[Sfx.Coin] = Tone(new float[] { 990f, 1320f, 1480f }, new float[] { 0.05f, 0.05f, 0.12f }, 0.4f);
            clips[Sfx.Water] = Noise(0.35f, 1400f, 0.5f);
            clips[Sfx.Hoe] = Noise(0.18f, 700f, 0.7f);
            clips[Sfx.Sprout] = Tone(new float[] { 520f, 660f, 880f }, new float[] { 0.05f, 0.05f, 0.12f }, 0.45f);
            clips[Sfx.Harvest] = Tone(new float[] { 660f, 880f, 1320f, 1760f }, new float[] { 0.05f, 0.05f, 0.05f, 0.18f }, 0.5f);
            clips[Sfx.Tap] = Tone(new float[] { 340f }, new float[] { 0.05f }, 0.25f);
            clips[Sfx.Nope] = Tone(new float[] { 220f, 210f }, new float[] { 0.09f, 0.12f }, 0.3f);
            clips[Sfx.Meow] = Meow(0.28f);
            clips[Sfx.MeowSoft] = Meow(0.16f);
            clips[Sfx.Sleep] = Tone(new float[] { 660f, 550f, 440f }, new float[] { 0.15f, 0.15f, 0.4f }, 0.4f);
            clips[Sfx.Wake] = Tone(new float[] { 440f, 550f, 660f, 880f }, new float[] { 0.1f, 0.1f, 0.1f, 0.25f }, 0.4f);
            clips[Sfx.Step] = Tone(new float[] { 320f }, new float[] { 0.035f }, 0.10f);
            clips[Sfx.Swing] = Noise(0.12f, 1600f, 0.25f);
            clips[Sfx.Splash] = Noise(0.3f, 900f, 0.4f);
            clips[Sfx.Chirp] = Tone(new float[] { 2300f, 1900f, 2400f }, new float[] { 0.04f, 0.05f, 0.06f }, 0.12f);
        }

        public void Play(Sfx sfx)
        {
            if (sfxSource != null && clips.TryGetValue(sfx, out var c) && c != null)
                sfxSource.PlayOneShot(c, SfxVolume);
        }

        public void ToggleMusic() { musicOn = !musicOn; }

        AudioClip rainClip;
        public void StartRain()
        {
            if (rainClip == null)
            {
                // soft lowpassed noise bed
                int sampleRate = 44100;
                int n = sampleRate * 2;
                var data = new float[n];
                float prev = 0f;
                float rc = 1f / (700f * 2f * Mathf.PI);
                float dt = 1f / sampleRate;
                var rnd = new System.Random(42);
                for (int i = 0; i < n; i++)
                {
                    float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
                    float alpha = dt / (rc + dt);
                    prev = prev + alpha * (white - prev);
                    data[i] = prev * 0.7f;
                }
                rainClip = AudioClip.Create("rain", n, 1, sampleRate, false);
                rainClip.SetData(data, 0);
            }
            rainSource.clip = rainClip;
            rainSource.volume = 0.12f * SfxVolume;
            rainSource.Play();
        }

        public void StopRain()
        {
            if (rainSource != null && rainSource.isPlaying)
            {
                StartCoroutine(FadeRainOut());
            }
        }

        System.Collections.IEnumerator FadeRainOut()
        {
            float t = 0;
            float from = rainSource.volume;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                rainSource.volume = Mathf.Lerp(from, 0f, t / 0.8f);
                yield return null;
            }
            rainSource.Stop();
        }

        void Update()
        {
            // lazy music loop: soft pentatonic plucks + low drone
            if (musicSource == null || !musicOn) return;
            if (Time.time >= nextNoteTime)
            {
                step++;
                PlayNote(step);
                nextNoteTime = Time.time + NoteGap(step);
            }
        }

        float NoteGap(int s)
        {
            int bar = s / 8;
            if (bar % 8 == 7) return 0.9f; // breath at phrase end
            return (s % 2 == 0) ? 0.45f : 0.30f;
        }

        void PlayNote(int s)
        {
            // C major pentatonic: C D E G A (frequencies in C4-C6 range)
            float[] scale = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f };
            int[] melody =
            {
                0, 2, 4, 2, 1, 3, 4, 3,
                2, 4, 5, 4, 3, 1, 0, -1,
            };
            int idx = melody[s % melody.Length];
            if (idx >= 0)
            {
                float f = scale[idx];
                // slight octave lift on every 4th note, soft volume
                AudioClip note = Tone(new[] { f }, new[] { 0.42f }, 0.16f);
                musicSource.PlayOneShot(note, 0.5f * MusicVolume);
            }
            // soft low drone on the beat
            if (s % 2 == 0)
            {
                AudioClip bass = Tone(new[] { 130.81f }, new[] { 0.6f }, 0.10f);
                musicSource.PlayOneShot(bass, 0.35f * MusicVolume);
            }
        }

        // Sequence of sine blips with envelopes.
        static AudioClip Tone(float[] freqs, float[] durations, float masterVol)
        {
            int sampleRate = 44100;
            int total = 0;
            foreach (var d in durations) total += (int)(d * sampleRate);
            var data = new float[total];
            int offset = 0;
            for (int i = 0; i < freqs.Length; i++)
            {
                int n = (int)(durations[i] * sampleRate);
                float f = freqs[i];
                for (int j = 0; j < n; j++)
                {
                    float t = (float)j / sampleRate;
                    float env = Mathf.Exp(-t * 9f);
                    float harmonic = Mathf.Sin(2f * Mathf.PI * f * t);
                    float shimmer = Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.18f;
                    data[offset + j] = (harmonic + shimmer) * env * masterVol;
                }
                offset += n;
            }
            var clip = AudioClip.Create("tone", total, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Short filtered-noise puff (water, dirt).
        static AudioClip Noise(float duration, float cutoff, float vol)
        {
            int sampleRate = 44100;
            int n = (int)(duration * sampleRate);
            var data = new float[n];
            float prev = 0f;
            float rc = 1f / (cutoff * 2f * Mathf.PI);
            float dt = 1f / sampleRate;
            for (int i = 0; i < n; i++)
            {
                float white = Random.value * 2f - 1f;
                float alpha = dt / (rc + dt);
                prev = prev + alpha * (white - prev);
                float env = 1f - (float)i / n;
                data[i] = prev * env * vol * 1.6f;
            }
            var clip = AudioClip.Create("noise", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Two wobbling sine tones = a tiny meow.
        static AudioClip Meow(float vol)
        {
            int sampleRate = 44100;
            float dur = 0.22f;
            int n = (int)(dur * sampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                float f = Mathf.Lerp(600f, 950f, t / dur);
                float wobble = 1f + 0.05f * Mathf.Sin(2f * Mathf.PI * 22f * t);
                float env = Mathf.Sin(Mathf.PI * t / dur);
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t * wobble) * env * vol;
            }
            var clip = AudioClip.Create("meow", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
