using System.Collections.Generic;
using SFE.SimCore.Data;

namespace SFE.SimCore.Thermal
{
    /// <summary>
    /// Zerlegung eines Zeitschritts je Feld: <c>neu − vorher = Own + FromNeighbour − ToWind</c>.
    /// Die Teile gelten mit den neuen Temperaturen (implizites Euler), dann geht die Rechnung exakt auf.
    /// FromNeighbour summiert zu null – der Austausch erzeugt und vernichtet keine Wärme.
    /// </summary>
    public sealed class PairStepTerms
    {
        public double[] Own;
        public double[] FromNeighbour;
        public double[] ToWind;
    }

    /// <summary>
    /// Zwei-Felder-Beispiel am Ende von MG1 Schritt 2: Feld 0 = Laterne, Feld 1 = Iglu. Dieselbe Zellbilanz wie die
    /// Siedlung, aber mit Speicherglied, damit man das Rechnen Zeitschritt für Zeitschritt sieht:
    /// <code>C·dT_i/dt = q_i − h (T_i − T_Luft) + k (T_j − T_i)</code>
    /// Implizites Euler; der Endzustand ist die stationäre Bilanz der Siedlung für diese zwei Felder
    /// (<see cref="SteadyState"/>). Identisch zu <c>sim-reference/simref/settlement_pair.py</c>.
    /// </summary>
    public sealed class SettlementPairLesson
    {
        public const int Lantern = 0;
        public const int Igloo = 1;

        public double LanternHeat;
        public double IglooHeat;
        public double K;
        public double H;
        public double Capacity;
        public double AirTemp;
        public double Dt;
        public int Steps;

        /// <summary>Temperaturen nach dem letzten <see cref="Step"/>; beide Felder starten bei Lufttemperatur.</summary>
        public double[] Temperatures { get; private set; }

        /// <summary>Liest den Block <c>pairLesson</c> des Siedlungs-Levels; null, wenn er fehlt.</summary>
        public static SettlementPairLesson FromLevel(Dictionary<string, object> level, double airTemp)
        {
            if (!level.Has("pairLesson")) return null;
            var p = level.Obj("pairLesson");
            var lesson = new SettlementPairLesson
            {
                LanternHeat = p.Num("lanternHeat"),
                IglooHeat = p.Num("iglooHeat"),
                K = p.Num("k"),
                H = p.Num("h"),
                Capacity = p.Num("capacity"),
                AirTemp = airTemp,
                Dt = p.Num("dt"),
                Steps = p.Int("steps"),
            };
            lesson.Reset();
            return lesson;
        }

        public double Heat(int i) => i == Lantern ? LanternHeat : IglooHeat;

        public void Reset() => Temperatures = new[] { AirTemp, AirTemp };

        /// <summary>Ein Zeitschritt (2×2-System, direkt gelöst).</summary>
        public void Step()
        {
            double c = Capacity / Dt;
            double d = c + H + K;
            double b0 = c * Temperatures[0] + Heat(0) + H * AirTemp;
            double b1 = c * Temperatures[1] + Heat(1) + H * AirTemp;
            Temperatures = Solve(d, b0, b1);
        }

        /// <summary>Zerlegt den letzten Schritt (nach <see cref="Step"/> aufrufen).</summary>
        public PairStepTerms LastStepTerms()
        {
            double f = Dt / Capacity;
            double flow = f * K * (Temperatures[1] - Temperatures[0]); // > 0: Wärme fließt vom Iglu zur Laterne
            return new PairStepTerms
            {
                Own = new[] { f * Heat(0), f * Heat(1) },
                FromNeighbour = new[] { flow, -flow },
                ToWind = new[] { f * H * (Temperatures[0] - AirTemp), f * H * (Temperatures[1] - AirTemp) },
            };
        }

        /// <summary>Gleichgewichtspunkt: dieselbe Bilanz ohne Speicherglied – so rechnet die Siedlung.</summary>
        public double[] SteadyState() => Solve(H + K, Heat(0) + H * AirTemp, Heat(1) + H * AirTemp);

        // [[d, −K], [−K, d]] · T = b
        private double[] Solve(double d, double b0, double b1)
        {
            double det = d * d - K * K;
            return new[] { (d * b0 + K * b1) / det, (K * b0 + d * b1) / det };
        }
    }
}
