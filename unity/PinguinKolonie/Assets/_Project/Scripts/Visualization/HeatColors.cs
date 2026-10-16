using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Farbskala des Wärmebilds (Entscheidung zu Frage P-05): drei Bereiche mit deutlich unterschiedlicher
    /// Helligkeit – dunkel-/hellblau = zu kalt, hellgelb bis gold = gemütlich, orange bis dunkelrot = schmilzt.
    /// Rot/Grün wird bewusst vermieden; der Zustand der Iglus ist zusätzlich an Form/Animation erkennbar.
    /// </summary>
    public static class HeatColors
    {
        private static readonly Color ColdDeep = new Color(0.10f, 0.18f, 0.45f);
        private static readonly Color ColdLight = new Color(0.55f, 0.80f, 0.98f);
        private static readonly Color ComfyLow = new Color(1.00f, 0.96f, 0.62f);
        private static readonly Color ComfyHigh = new Color(1.00f, 0.80f, 0.20f);
        private static readonly Color MeltLow = new Color(0.98f, 0.50f, 0.12f);
        private static readonly Color MeltHigh = new Color(0.62f, 0.05f, 0.08f);

        public static Color For(double t, double air, double comfortMin, double melt)
        {
            if (t < comfortMin) return Color.Lerp(ColdDeep, ColdLight, Mathf.InverseLerp((float)air, (float)comfortMin, (float)t));
            if (t < melt) return Color.Lerp(ComfyLow, ComfyHigh, Mathf.InverseLerp((float)comfortMin, (float)melt, (float)t));
            return Color.Lerp(MeltLow, MeltHigh, Mathf.InverseLerp((float)melt, (float)melt + 25f, (float)t));
        }

        public static Color Cold => ColdLight;
        public static Color Comfy => ComfyHigh;
        public static Color Melt => MeltLow;

        /// <summary>Farbe für einen Pinguin im Prolog (unter der Frostgrenze blau, darüber gelb bis orange).</summary>
        public static Color Penguin(double t, double freezeBelow)
        {
            if (t < freezeBelow) return Color.Lerp(ColdDeep, ColdLight, Mathf.InverseLerp((float)freezeBelow - 20f, (float)freezeBelow, (float)t));
            return Color.Lerp(ComfyLow, MeltLow, Mathf.InverseLerp((float)freezeBelow, (float)freezeBelow + 20f, (float)t));
        }
    }
}
