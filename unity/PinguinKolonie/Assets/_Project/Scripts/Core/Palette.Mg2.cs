using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Farben von MG2 „Polar-Express“ – nur Verweise auf das Siemens-Farbschema (docs/07). Das Strömungsbild ist
    /// farbfehlsichtig-tauglich: Geschwindigkeit als Helligkeit in Blau, Wirbel in Rot (dazu kreisen die Teilchen).
    /// </summary>
    public static partial class Palette
    {
        // ---------- ZWILLIS Windkanal ----------
        /// <summary>Stehende Luft auf dem Bildschirm (dunkel, damit die Teilchen leuchten).</summary>
        public static readonly Color Mg2AirStill = Siemens.DeepBlue90;
        public static readonly Color Mg2AirSlow = Siemens.DarkBlue;
        public static readonly Color Mg2AirFast = Siemens.Blue;
        public static readonly Color Mg2AirFastest = Siemens.SoftBlue;
        /// <summary>Wirbel: Luft, die gegen den Wind zurückströmt.</summary>
        public static readonly Color Mg2Swirl = Shade(Siemens.Red, 0.15f);
        public static readonly Color Mg2Train = Siemens.LightSand;
        /// <summary>Offener Wagenübergang (Kupplung und Faltenbalg ragen kantig über das Dach).</summary>
        public static readonly Color Mg2TransitionOpen = Siemens.DarkOrange;
        public static readonly Color Mg2Particle = Siemens.LightSand;
        public static readonly Color Mg2CellLine = Siemens.DeepBlue70;
        public static readonly Color Mg2Handle = Siemens.Yellow;
        public static readonly Color Mg2HandleActive = Siemens.BoldGreen;
        public static readonly Color Mg2HandleLine = Siemens.DarkYellow;
        public static readonly Color Mg2WindArrow = Siemens.SoftBlue;

        // ---------- Rechentafel (fünf Kästchen) ----------
        public static readonly Color Mg2ArrowRight = Siemens.BoldBlue;
        public static readonly Color Mg2ArrowLeft = Siemens.Orange;
        public static readonly Color Mg2ArrowStay = Siemens.SoftSand;
        public static readonly Color Mg2CellBox = Siemens.DeepBlue80;
        public static readonly Color Mg2Wall = Siemens.DeepBlue40;

        // ---------- Draußen in der Tundra ----------
        public static readonly Color Mg2Tundra = Tint(Siemens.SoftBlue, 0.12f);
        public static readonly Color Mg2Rail = Siemens.DeepBlue70;
        public static readonly Color Mg2Sleeper = Siemens.DarkSand;
        public static readonly Color Mg2Sled = Siemens.SoftSand;
        public static readonly Color Mg2SledFront = Siemens.DarkSand;
        public static readonly Color Mg2Motor = Siemens.DeepBlue80;
        public static readonly Color Mg2Smoke = Siemens.DeepBlue20;
        public static readonly Color Mg2Flake = Siemens.LightSand;
        /// <summary>Pips windschnittiger Zug draußen: hell mit Petrol-Streifen (Siemens-Primärfarben).</summary>
        public static readonly Color Mg2TrainOutside = Siemens.LightSand;
        public static readonly Color Mg2TrainStripe = Siemens.Petrol;
        public static readonly Color Mg2Window = Siemens.DeepBlue60;

        // ---------- Symbolbild Transfer ----------
        public static readonly Color Mg2PixelTrain = Siemens.DeepBlue10;
        public static readonly Color Mg2PixelLine = Siemens.BoldBlue;
    }
}
