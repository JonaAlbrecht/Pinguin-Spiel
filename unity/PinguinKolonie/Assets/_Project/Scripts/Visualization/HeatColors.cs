using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Farbskala des Wärmebilds (Entscheidung zu Frage P-05): drei Bereiche mit deutlich unterschiedlicher
    /// Helligkeit – dunkel-/hellblau = zu kalt, hellgelb bis gold = gemütlich, orange bis dunkelrot = schmilzt.
    /// Rot/Grün wird bewusst vermieden; der Zustand der Iglus ist zusätzlich an Form/Animation erkennbar.
    /// Alle Stufen aus dem Siemens-Farbschema; das tiefe Rot ist Siemens-Rot, mit Deep Blue abgedunkelt.
    /// </summary>
    public static class HeatColors
    {
        private static readonly Color ColdDeep = Palette.Siemens.DarkBlue;
        private static readonly Color ColdLight = Palette.Siemens.SoftBlue;
        private static readonly Color ComfyLow = Palette.Siemens.SoftYellow;
        private static readonly Color ComfyHigh = Palette.Siemens.DarkYellow;
        private static readonly Color MeltLow = Palette.Siemens.DarkOrange;
        private static readonly Color MeltHigh = Palette.Shade(Palette.Siemens.Red, 0.35f);

        public static Color For(double t, double air, double comfortMin, double melt)
        {
            if (t < comfortMin) return Color.Lerp(ColdDeep, ColdLight, Mathf.InverseLerp((float)air, (float)comfortMin, (float)t));
            if (t < melt) return Color.Lerp(ComfyLow, ComfyHigh, Mathf.InverseLerp((float)comfortMin, (float)melt, (float)t));
            return Color.Lerp(MeltLow, MeltHigh, Mathf.InverseLerp((float)melt, (float)melt + 25f, (float)t));
        }

        public static Color Cold => ColdLight;
        public static Color Comfy => ComfyHigh;
        public static Color Melt => MeltLow;
    }
}
