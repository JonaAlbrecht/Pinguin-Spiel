using System.Collections.Generic;
using SFE.SimCore.Data;
using SFE.SimCore.LinearAlgebra;

namespace SFE.SimCore.Thermal
{
    /// <summary>Leveldaten der Wärmewelle in MG1 Schritt 3 (Block <c>wave</c>).</summary>
    public sealed class SettlementWaveLesson
    {
        public double Capacity;
        public double Dt;
        public int Steps;
        public int ManualSteps;
        public string[] Tiles;

        public static SettlementWaveLesson FromLevel(Dictionary<string, object> level)
        {
            if (!level.Has("wave")) return null;
            var w = level.Obj("wave");
            return new SettlementWaveLesson
            {
                Capacity = w.Num("capacity"),
                Dt = w.Num("dt"),
                Steps = w.Int("steps"),
                ManualSteps = w.Int("manualSteps"),
                Tiles = w.StrArray("tiles"),
            };
        }
    }

    /// <summary>
    /// Wärmewelle in MG1 Schritt 3 „Alles hängt zusammen“: dieselbe Bilanz wie die Siedlung
    /// (<see cref="SettlementModel.Assemble"/>), dazu ein Wärmespeicher je Feld, implizites Euler:
    /// <code>(C/dt · I + A) T' = C/dt · T + b</code>
    /// So sieht man, wie die Wärme Schritt für Schritt von Feld zu Feld wandert. Der Endzustand ist genau die
    /// stationäre Lösung der Siedlung. Identisch zu <c>sim-reference/simref/settlement_wave.py</c>.
    /// </summary>
    public sealed class SettlementWave
    {
        private readonly CsrMatrix _system;
        private readonly double[] _b;
        private readonly double _c;

        public SettlementGrid Grid { get; }
        public double[] Temperatures { get; private set; }
        public int Step { get; private set; }

        public SettlementWave(SettlementWaveLesson lesson, SettlementLevel level, SettlementMaterialSet mats)
        {
            Grid = SettlementGrid.FromRows(lesson.Tiles);
            var a = SettlementModel.Assemble(Grid, level, mats, out _b);
            _c = lesson.Capacity / lesson.Dt;
            var values = (double[])a.Values.Clone();
            for (int row = 0; row < a.Size; row++)
                for (int k = a.RowStart[row]; k < a.RowStart[row + 1]; k++)
                    if (a.Columns[k] == row) values[k] += _c;
            _system = new CsrMatrix(a.Size, a.RowStart, a.Columns, values);
            Temperatures = new double[a.Size];
            for (int i = 0; i < Temperatures.Length; i++) Temperatures[i] = level.AirTemp;
        }

        /// <summary>Ein Zeitschritt für alle Felder auf einmal.</summary>
        public void Advance()
        {
            var rhs = new double[_b.Length];
            for (int i = 0; i < rhs.Length; i++) rhs[i] = _c * Temperatures[i] + _b[i];
            Temperatures = new ConjugateGradientSolver(_system, rhs, Temperatures, 1e-12).SolveToEnd();
            Step++;
        }
    }
}
