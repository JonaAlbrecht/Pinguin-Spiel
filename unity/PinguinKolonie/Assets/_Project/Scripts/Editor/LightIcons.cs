using UnityEditor;
using UnityEngine;

namespace SFE.Game.EditorTools
{
    /// <summary>
    /// Blendet im Editor das Symbol für Lichtquellen aus (gelbe Glühbirne mit Strahlen). Jede Laterne mit Licht trägt eine
    /// Punktlichtquelle; mit eingeschalteten Gizmos in der Game-Ansicht zeichnete Unity über jede Laterne ein großes
    /// flaches Glühbirnen-Symbol, das die Laterne verdeckte (Spieltest 21.10.2026: Spielbrett in MG1, Kalles Siedlung).
    /// Das Symbol ist reine Editor-Hilfe – im fertigen Spiel gibt es es nicht und es hat keine Lernfunktion.
    /// Andere Gizmos bleiben, wie sie sind. Wieder einblenden: Gizmos-Menü der Szenen- oder Game-Ansicht, „Light“.
    /// </summary>
    [InitializeOnLoad]
    public static class LightIcons
    {
        private const string Done = "SFE.LightIconsHidden";

        static LightIcons()
        {
            // Einmal je Editor-Sitzung, damit ein bewusst wieder eingeschaltetes Symbol nicht bei jedem Kompilieren verschwindet.
            if (SessionState.GetBool(Done, false)) return;
            SessionState.SetBool(Done, true);
            EditorApplication.delayCall += () => GizmoUtility.SetIconEnabled(typeof(Light), false);
        }
    }
}
