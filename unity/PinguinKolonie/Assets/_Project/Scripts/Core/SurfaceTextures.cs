using System.Collections.Generic;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Prozedurale Normal-Maps für realistische Oberflächen (ADR-27) – per Code erzeugt wie alles im Spiel, keine
    /// fremden Texturen. Kachelbar (ganzzahlige Frequenzen, gewickeltes Rauschen), einmal je Art berechnet.
    /// Gespeichert als RGB mit Alpha 1: URP liest das über „RG oder AG“ auf allen Plattformen richtig.
    /// </summary>
    public static class SurfaceTextures
    {
        public enum Kind { Snow, Waves }

        private const int Size = 256;
        private static readonly Dictionary<Kind, Texture2D> Cache = new Dictionary<Kind, Texture2D>();

        public static Texture2D Get(Kind kind)
        {
            if (Cache.TryGetValue(kind, out var t) && t != null) return t;
            t = Build(kind);
            Cache[kind] = t;
            return t;
        }

        private static Texture2D Build(Kind kind)
        {
            var h = new float[Size * Size];
            var rnd = new System.Random(kind == Kind.Snow ? 7 : 11);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size, v = y / (float)Size;
                    h[y * Size + x] = kind == Kind.Snow
                        // Neuschnee: sanfte Verwehungen, dazu Körnung bis fast auf Pixelgröße (das Glitzern im Streiflicht)
                        ? 0.55f * Tileable(u, v, 3, 0.3f) + 0.30f * Tileable(u, v, 11, 4.1f) + 0.15f * Tileable(u, v, 47, 9.7f)
                        // Wasser: überlagerte Wellenzüge in verschiedenen Richtungen, dazu feines Kräuseln
                        : Waves(u, v) + 0.18f * Tileable(u, v, 23, 2.2f);
                }
            }
            // Ein paar einzelne Körner, die stärker hervortreten (Schnee glitzert punktuell, nicht flächig)
            if (kind == Kind.Snow)
                for (int k = 0; k < 900; k++) h[rnd.Next(h.Length)] += 0.35f;

            float strength = kind == Kind.Snow ? 6f : 3.5f;
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = H(h, x + 1, y) - H(h, x - 1, y);
                    float dy = H(h, x, y + 1) - H(h, x, y - 1);
                    var n = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                    px[y * Size + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255), (byte)((n.y * 0.5f + 0.5f) * 255), // Stil-Lint: Rechnung
                        (byte)((n.z * 0.5f + 0.5f) * 255), 255);
                }
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true, true)
            {
                name = "Normal_" + kind,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
            };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        private static float H(float[] h, int x, int y) => h[((y + Size) % Size) * Size + (x + Size) % Size];

        /// <summary>Kachelbares Rauschen: vier versetzte Abtastungen, nach Lage gemischt – die Ränder passen nahtlos.</summary>
        private static float Tileable(float u, float v, float freq, float seed)
        {
            float x = u * freq, y = v * freq;
            float a = Mathf.PerlinNoise(x + seed, y + seed);
            float b = Mathf.PerlinNoise(x - freq + seed, y + seed);
            float c = Mathf.PerlinNoise(x + seed, y - freq + seed);
            float d = Mathf.PerlinNoise(x - freq + seed, y - freq + seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        private static float Waves(float u, float v)
        {
            const float tau = Mathf.PI * 2f;
            return 0.40f * Mathf.Sin(tau * (3f * u + 1f * v)) +
                   0.25f * Mathf.Sin(tau * (-2f * u + 5f * v) + 1.3f) +
                   0.15f * Mathf.Sin(tau * (7f * u - 3f * v) + 2.1f) +
                   0.10f * Mathf.Sin(tau * (11f * u + 9f * v) + 0.4f);
        }
    }
}
