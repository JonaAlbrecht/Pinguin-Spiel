using System.Runtime.CompilerServices;
using SFE.SimCore.Flow;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Spielstand von MG2 zur laufenden Sitzung. Hängt am <see cref="GameState"/>, ohne GameData.cs zu ändern
    /// (paralleles Paket). Der Integrator kann die Felder später direkt in GameState übernehmen.
    /// </summary>
    public sealed class Mg2Progress
    {
        private static readonly ConditionalWeakTable<GameState, Mg2Progress> Table = new ConditionalWeakTable<GameState, Mg2Progress>();

        public static Mg2Progress Of(GameState state) => Table.GetValue(state, _ => new Mg2Progress());

        public bool Done;
        /// <summary>Beste Runde über alle Durchläufe (mehr Sterne, sonst kürzere Fahrzeit).</summary>
        public TrainResult BestResult;
        public TrainShape BestShape;
        /// <summary>Strömungsbild der besten Runde – links im Transfer-Screen.</summary>
        public Texture2D Snapshot;
        /// <summary>Foto von Kalles Schlitten draußen – in Schritt 4 neben ZWILLIS Rechnung.</summary>
        public Texture2D KallePhoto;
        public int BestStars => BestResult?.Stars ?? -1;
    }
}
