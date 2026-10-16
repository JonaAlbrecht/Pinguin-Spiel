using System.Collections.Generic;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Geräusche, zur Laufzeit aus Zahlen erzeugt (Frage J-04: Musik/Sound war für v0.1 gestrichen).
    /// Keine Audiodateien: nichts zu lizenzieren, nichts in Git LFS, und es läuft in WebGL wie im Editor.
    /// Ein Wind-Loop, dessen Lautstärke das Spiel steuert, plus kurze Einzeltöne.
    /// </summary>
    public static class Sfx
    {
        public enum Sound { Plop, Erase, Splash, Ding, Bleep, Click, Success, Wrong }

        private const int Rate = 22050;
        private static AudioSource _oneShot;
        private static AudioSource _wind;
        // Eigene Quelle für ZWILLIs Stimme: pitch gilt für alle One-Shots einer Quelle, sonst verstimmt das
        // Stottern auch gerade klingende Plopps und Dings.
        private static AudioSource _voice;
        private static readonly Dictionary<Sound, AudioClip> Clips = new Dictionary<Sound, AudioClip>();
        private static float _windTarget;

        public static bool Muted { get; private set; }

        public static void Init(GameObject host)
        {
            _oneShot = host.AddComponent<AudioSource>();
            _oneShot.playOnAwake = false;
            _voice = host.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _wind = host.AddComponent<AudioSource>();
            _wind.clip = WindLoop();
            _wind.loop = true;
            _wind.volume = 0f;
            _wind.Play();
            Clips[Sound.Plop] = Make("plop", 0.09f, (t, r) => Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(620, 220, t / 0.09f) * t) * Env(t, 0.09f));
            Clips[Sound.Erase] = Make("erase", 0.08f, (t, r) => Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(260, 520, t / 0.08f) * t) * Env(t, 0.08f) * 0.6f);
            Clips[Sound.Splash] = Make("splash", 0.45f, Splash);
            Clips[Sound.Ding] = Make("ding", 0.7f, (t, r) => (Mathf.Sin(2 * Mathf.PI * 880 * t) + 0.4f * Mathf.Sin(2 * Mathf.PI * 1320 * t)) * Mathf.Exp(-6f * t) * 0.6f);
            Clips[Sound.Bleep] = Make("bleep", 0.14f, Bleep);
            Clips[Sound.Click] = Make("click", 0.025f, (t, r) => Mathf.Sin(2 * Mathf.PI * 1800 * t) * Env(t, 0.025f) * 0.3f);
            Clips[Sound.Success] = Make("success", 0.9f, Success);
            Clips[Sound.Wrong] = Make("wrong", 0.25f, (t, r) => Mathf.Sin(2 * Mathf.PI * (t < 0.12f ? 330 : 247) * t) * Env(t, 0.25f) * 0.5f);
        }

        public static void Play(Sound s, float volume = 0.7f)
        {
            if (Muted || _oneShot == null || !Clips.TryGetValue(s, out var clip)) return;
            _oneShot.PlayOneShot(clip, volume);
        }

        /// <summary>ZWILLIs Stimme: je größer <paramref name="glitch"/>, desto mehr Stottern (es wächst mit jedem Schritt).</summary>
        public static void Speak(float glitch)
        {
            if (Muted || _voice == null) return;
            _voice.pitch = 1f + Random.Range(-0.15f, 0.15f) * (0.3f + glitch);
            _voice.PlayOneShot(Clips[Sound.Bleep], 0.35f);
        }

        /// <summary>Wind 0…1; wird weich nachgeführt, damit Windwechsel nicht knacken.</summary>
        public static void Wind(float level) => _windTarget = Mathf.Clamp01(level);

        public static void Tick()
        {
            if (_wind == null) return;
            float target = Muted ? 0f : _windTarget * 0.45f;
            _wind.volume = Mathf.MoveTowards(_wind.volume, target, Time.unscaledDeltaTime * 0.5f);
        }

        public static void SetMuted(bool muted) => Muted = muted;

        // ---------- Klangerzeugung ----------

        private delegate float Wave(float t, System.Random rnd);

        private static AudioClip Make(string name, float seconds, Wave wave)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            var rnd = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate, rnd), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Kurzer Anschlag, weiches Ausklingen – ohne Knacken am Anfang und Ende.</summary>
        private static float Env(float t, float length)
        {
            float attack = Mathf.Clamp01(t / 0.005f);
            return attack * Mathf.Clamp01(1f - t / length);
        }

        private static float _splashLow;

        private static float Splash(float t, System.Random r)
        {
            float noise = (float)(r.NextDouble() * 2 - 1);
            _splashLow += (noise - _splashLow) * 0.12f; // Tiefpass: Rauschen klingt nach Wasser statt nach Zischen
            float blub = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(380, 120, t / 0.45f) * t) * Mathf.Exp(-9f * t);
            return (_splashLow * 1.6f * Mathf.Exp(-5f * t) + blub * 0.5f) * Mathf.Clamp01(t / 0.01f);
        }

        private static float Bleep(float t, System.Random r)
        {
            float f = t < 0.05f ? 1200 : t < 0.09f ? 1500 : 1000;
            float square = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t));
            return square * 0.25f * Env(t, 0.14f);
        }

        private static float Success(float t, System.Random r)
        {
            // Drei Töne nacheinander (C – E – G), jeder klingt aus
            float s = 0f;
            float[] notes = { 523.25f, 659.25f, 783.99f };
            for (int k = 0; k < 3; k++)
            {
                float start = k * 0.14f;
                if (t < start) continue;
                float u = t - start;
                s += Mathf.Sin(2 * Mathf.PI * notes[k] * u) * Mathf.Exp(-4f * u) * 0.35f;
            }
            return s;
        }

        /// <summary>Zwei Sekunden tief gefiltertes Rauschen; Anfang und Ende überblendet, damit der Loop nahtlos ist.</summary>
        private static AudioClip WindLoop()
        {
            int n = Rate * 2, fade = Rate / 4;
            var data = new float[n];
            var rnd = new System.Random(7);
            float low = 0f, gust = 0f;
            for (int i = 0; i < n + fade; i++)
            {
                float noise = (float)(rnd.NextDouble() * 2 - 1);
                low += (noise - low) * 0.02f;
                gust = 0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * i / (float)n * 3f);
                float v = low * 3f * gust;
                if (i < n) data[i] = v;
                else data[i - n] = Mathf.Lerp(v, data[i - n], (i - n) / (float)fade);
            }
            var clip = AudioClip.Create("wind", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
