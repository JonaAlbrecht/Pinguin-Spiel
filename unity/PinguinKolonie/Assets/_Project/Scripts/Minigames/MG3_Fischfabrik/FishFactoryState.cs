using SFE.SimCore.Process;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Spielstand von MG3 in dieser Sitzung. Eigene Klasse, weil <see cref="GameState"/> eine gemeinsame Datei ist –
    /// der Integrator kann die Felder später dorthin verschieben (Spec 005, Abschnitt „Geteilte Dateien“).
    /// </summary>
    public static class FishFactoryState
    {
        public static bool Done;
        /// <summary>−1 = noch keine Runde. Die beste Runde zählt, auch über „nochmal planen“ hinweg.</summary>
        public static int BestStars = -1;
        public static string[] BestLayout;
        public static DaysSummary BestSummary;
        /// <summary>Foto der besten Fabrik auf ZWILLIS Bildschirm – links im Transfer-Screen.</summary>
        public static Texture2D Snapshot;
        /// <summary>Foto von Kalles Versuch in der echten Halle – in Schritt 4 neben der Rechnung.</summary>
        public static Texture2D HallPhoto;

        private static ProcessLevel _level;

        /// <summary>Leveldaten aus <c>Resources/Data/Levels/MG3_Fischfabrik.json</c> (dieselbe Datei wie Python und dotnet).</summary>
        public static ProcessLevel Level => _level ?? (_level = ProcessLevel.FromJson(GameData.Text("Data/Levels/MG3_Fischfabrik")));
    }
}
