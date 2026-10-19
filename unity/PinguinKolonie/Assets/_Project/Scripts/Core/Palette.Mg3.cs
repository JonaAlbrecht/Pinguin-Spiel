using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Farben von MG3 „Fischfabrik“ – alle aus dem Siemens-Farbschema (<see cref="Siemens"/>), aufgehellt nur mit
    /// Light Sand, abgedunkelt nur mit Deep Blue (docs/07). Eigene Datei, damit MG2 und MG3 nebeneinander arbeiten.
    /// </summary>
    public static partial class Palette
    {
        // Halle „in echt“ (Kalles Versuch): warm und stofflich, ohne Raster – nichts davon erinnert an ZWILLIS Bildschirm
        public static readonly Color Mg3HallFloor = Tint(Siemens.SoftSand, 0.55f);
        public static readonly Color Mg3HallFloorAlt = Tint(Siemens.DarkSand, 0.45f);
        public static readonly Color Mg3HallWall = Siemens.DeepBlue10;
        public static readonly Color Mg3HallTrim = Siemens.Petrol;
        public static readonly Color Mg3Pier = Siemens.DarkSand;
        public static readonly Color Mg3Pillar = Siemens.DeepBlue30;

        // Stationen
        public static readonly Color Mg3Machine = Siemens.DeepBlue20;
        public static readonly Color Mg3MachineDark = Siemens.DeepBlue60;
        public static readonly Color Mg3SortTable = Siemens.DarkSand;
        public static readonly Color Mg3Blade = Siemens.DeepBlue8;
        public static readonly Color Mg3PackTop = Siemens.Petrol;
        public static readonly Color Mg3Regal = Tint(Siemens.SoftBlue, 0.55f);
        public static readonly Color Mg3RegalFrame = Siemens.DeepBlue60;
        public static readonly Color Mg3Belt = Siemens.DeepBlue70;
        public static readonly Color Mg3BeltRoller = Siemens.DeepBlue40;
        public static readonly Color Mg3Fish = Siemens.SoftBlue;

        // Kisten
        public static readonly Color Mg3Crate = Siemens.SoftSand;
        public static readonly Color Mg3CrateBand = Siemens.Petrol;
        public static readonly Color Mg3Sushi = Siemens.DarkOrange;
        public static readonly Color Mg3SushiLid = Siemens.LightSand;

        // Robo-Robbe: graue Robbe mit leuchtenden Augen
        public static readonly Color Mg3Robot = Siemens.DeepBlue40;
        public static readonly Color Mg3RobotBelly = Siemens.DeepBlue20;
        public static readonly Color Mg3RobotEye = Siemens.BoldBlue;
        public static readonly Color Mg3RobotWheel = Siemens.DeepBlue80;

        // Kutter, Zug, Möwe
        public static readonly Color Mg3BoatHull = Siemens.DarkOrange;
        public static readonly Color Mg3BoatCabin = Siemens.LightSand;
        public static readonly Color Mg3Rail = Siemens.DeepBlue70;
        public static readonly Color Mg3Train = Siemens.Petrol;
        public static readonly Color Mg3TrainWindow = Siemens.BoldBlue;
        public static readonly Color Mg3Gull = Siemens.LightSand;
        public static readonly Color Mg3GullWing = Siemens.DeepBlue30;

        // Lämpchen an jeder Maschine: arbeitet / Störung / wartet
        public static readonly Color Mg3LampBusy = Siemens.Green;
        public static readonly Color Mg3LampDown = Siemens.Red;
        public static readonly Color Mg3LampIdle = Siemens.DeepBlue40;
        /// <summary>Blinken der Station, an der sich Kisten stapeln.</summary>
        public static readonly Color Mg3Jam = Siemens.Red;

        // Oberfläche: Balken „gute und schlechte Tage“
        public static readonly Color Mg3BarGood = Siemens.Green;
        public static readonly Color Mg3BarBad = Siemens.DarkOrange;
        public static readonly Color Mg3TargetLine = Siemens.DeepBlue;
        public static readonly Color Mg3Arrow = Siemens.Orange;
    }
}
