using System;
using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Ses efektleri. Ses dosyası kullanmak yerine sesleri matematikle (sinüs dalgası + gürültü) üretiyoruz.
    /// İleride gerçek ses dosyaları eklediğimizde sadece buradaki klipleri değiştireceğiz.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx I { get; private set; }

        AudioSource source;
        AudioClip whoosh, bump, horn, pop, coin, win, lift;

        public enum Sound { Whoosh, Bump, Horn, Pop, Coin, Win, Lift }

        void Awake()
        {
            I = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            Generate();
        }

        /// <summary>pitch verilirse sesi o tonda çalar (kombo için giderek tizleşen ses).</summary>
        public static void Play(Sound s, float volume = 1f, float pitch = -1f)
        {
            if (I == null || !SaveData.Sound) return;
            AudioClip clip = s switch
            {
                Sound.Whoosh => I.whoosh,
                Sound.Bump => I.bump,
                Sound.Horn => I.horn,
                Sound.Pop => I.pop,
                Sound.Coin => I.coin,
                Sound.Win => I.win,
                _ => I.lift
            };
            I.source.pitch = pitch > 0f ? pitch : s == Sound.Win ? 1f : UnityEngine.Random.Range(0.93f, 1.08f);
            I.source.PlayOneShot(clip, volume);
        }

        const int Rate = 44100;

        static AudioClip Make(string name, float duration, Func<float, float> wave)
        {
            int n = Mathf.CeilToInt(Rate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave((float)i / Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sin(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);

        void Generate()
        {
            var rng = new System.Random(7);
            float Noise() => (float)rng.NextDouble() * 2f - 1f;

            // Araba çıkış sesi: motor homurtusu + rüzgar
            float lp = 0f;
            whoosh = Make("whoosh", 0.55f, t =>
            {
                lp += (Noise() - lp) * 0.08f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.55f));
                float engine = Mathf.Sign(Sin(70f + 160f * t, t)) * 0.12f + Sin(140f + 300f * t, t) * 0.1f;
                return (lp * 0.9f + engine) * env * 0.8f;
            });

            // Çarpma: tok bir "güm"
            bump = Make("bump", 0.3f, t =>
            {
                float env = Mathf.Exp(-t * 22f);
                float f = 120f * Mathf.Exp(-t * 6f) + 50f;
                return (Sin(f, t) * 0.9f + Noise() * 0.35f * Mathf.Exp(-t * 60f)) * env;
            });

            // Korna: iki notalı "bip"
            horn = Make("horn", 0.26f, t =>
            {
                float env = Mathf.Clamp01(t / 0.015f) * Mathf.Clamp01((0.26f - t) / 0.05f);
                float w = (float)Math.Tanh(3f * (Sin(370f, t) + Sin(466f, t)));
                return w * env * 0.22f;
            });

            // Buton tıklama
            pop = Make("pop", 0.12f, t => Sin(520f + 900f * t, t) * Mathf.Exp(-t * 35f) * 0.5f);

            // Para sesi: iki parlak nota
            coin = Make("coin", 0.4f, t =>
            {
                float f = t < 0.08f ? 988f : 1319f;
                float local = t < 0.08f ? t : t - 0.08f;
                return (Sin(f, t) + 0.3f * Sin(f * 2f, t)) * Mathf.Exp(-local * 9f) * 0.3f;
            });

            // Kazanma: Do-Mi-Sol-Do arpeji
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            win = Make("win", 1.1f, t =>
            {
                int i = Mathf.Min(3, (int)(t / 0.12f));
                float local = t - i * 0.12f;
                float decay = i == 3 ? Mathf.Exp(-local * 3f) : Mathf.Exp(-local * 10f);
                float f = notes[i];
                float tri = Mathf.PingPong(t * f * 2f, 1f) * 2f - 1f;
                return (tri * 0.5f + Sin(f, t) * 0.5f) * decay * 0.35f;
            });

            // Vinç kaldırma
            lift = Make("lift", 0.6f, t => Sin(200f + 500f * t, t) * Mathf.Sin(Mathf.PI * t / 0.6f) * 0.25f);
        }
    }
}
