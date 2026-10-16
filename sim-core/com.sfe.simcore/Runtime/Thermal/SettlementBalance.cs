using System;

namespace SFE.SimCore.Thermal
{
    /// <summary>
    /// Die „Waage“ eines Feldes im gelösten Zustand (MG1 Schritt 5, Iglu antippen):
    /// <c>Quelle + vonNachbarn = anNachbarn + anDieLuft</c>. Alle Werte ≥ 0.
    /// Identisch zu <c>balance()</c> in <c>sim-reference/simref/settlement.py</c>.
    /// Liest dieselben Leitwerte wie <see cref="SettlementModel.Assemble"/> – die Waage im Spiel zeigt
    /// also genau die Rechnung, die das Wärmebild erzeugt hat, keine Nachbildung (O-10).
    /// </summary>
    public sealed class SettlementBalance
    {
        public double[] Source;
        public double[] FromNeighbours;
        public double[] ToNeighbours;
        public double[] ToAir;

        public double In(int i) => Source[i] + FromNeighbours[i];
        public double Out(int i) => ToNeighbours[i] + ToAir[i];

        public static SettlementBalance Compute(SettlementGrid grid, SettlementLevel level, SettlementMaterialSet mats, double[] temperatures)
        {
            int n = grid.CellCount;
            if (temperatures == null || temperatures.Length != n)
                throw new ArgumentException("Temperaturen passen nicht zum Raster.", nameof(temperatures));
            bool[] shield = SettlementModel.ShieldedMask(grid, mats, level.WindFrom);
            double windFactor = level.Wind / mats.WindReference;
            var b = new SettlementBalance
            {
                Source = new double[n], FromNeighbours = new double[n], ToNeighbours = new double[n], ToAir = new double[n],
            };
            var k = new double[n];
            for (int i = 0; i < n; i++)
            {
                var m = mats.Get(grid[i]);
                k[i] = m.K;
                double h = m.H * windFactor * (shield[i] ? mats.ShieldFactor : 1.0);
                b.Source[i] = grid[i] == Cell.Lantern ? level.LanternHeat : m.Q;
                b.ToAir[i] = h * (temperatures[i] - level.AirTemp);
            }
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    int i = y * grid.Width + x;
                    if (x + 1 < grid.Width) Flow(b, k, temperatures, i, i + 1);
                    if (y + 1 < grid.Height) Flow(b, k, temperatures, i, i + grid.Width);
                }
            }
            return b;
        }

        private static void Flow(SettlementBalance b, double[] k, double[] t, int i, int j)
        {
            double flow = 2.0 * k[i] * k[j] / (k[i] + k[j]) * (t[j] - t[i]); // > 0: Wärme fließt von j nach i
            if (flow > 0)
            {
                b.FromNeighbours[i] += flow;
                b.ToNeighbours[j] += flow;
            }
            else
            {
                b.FromNeighbours[j] -= flow;
                b.ToNeighbours[i] -= flow;
            }
        }
    }
}
