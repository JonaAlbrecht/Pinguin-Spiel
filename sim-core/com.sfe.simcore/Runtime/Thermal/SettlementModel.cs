using System;
using System.Collections.Generic;
using SFE.SimCore.LinearAlgebra;

namespace SFE.SimCore.Thermal
{
    /// <summary>
    /// MG1 Teil B „Die erste Siedlung“: stationäre Wärmebilanz je Zelle.
    /// <code>0 = q_i − h_i (T_i − T_Luft) + Σ_N k_ij (T_j − T_i)</code>
    /// k_ij = harmonisches Mittel der Leitwerte, h_i = h(Material) · Wind/Windreferenz · (Abschirmfaktor im Lee eines Schneewalls).
    /// Identisch zu <c>sim-reference/simref/settlement.py</c>.
    /// </summary>
    public static class SettlementModel
    {
        /// <summary>True für Zellen im Windschatten eines Schneewalls.</summary>
        public static bool[] ShieldedMask(SettlementGrid grid, SettlementMaterialSet mats, string windFrom)
        {
            WindDirection.BlowsTowards(windFrom, out double east, out double north);
            double dx = east, dy = -north; // Zeilen zählen nach Süden
            var walls = new List<int>();
            for (int i = 0; i < grid.CellCount; i++) if (grid[i] == Cell.Wall) walls.Add(i);

            var mask = new bool[grid.CellCount];
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    foreach (int w in walls)
                    {
                        int vx = x - w % grid.Width, vy = y - w / grid.Width;
                        double dist = Math.Sqrt(vx * vx + vy * vy);
                        if (dist == 0.0 || dist > mats.ShieldRange) continue;
                        if ((vx * dx + vy * dy) / dist >= mats.ShieldCone)
                        {
                            mask[y * grid.Width + x] = true;
                            break;
                        }
                    }
                }
            }
            return mask;
        }

        /// <summary>
        /// Baut das Gleichungssystem A·T = b. <paramref name="onCoupling"/> meldet jede Kopplung zweier
        /// Nachbarzellen (Index i, Index j, Leitwert) – für die „Wärmefäden“ im Spiel.
        /// </summary>
        public static CsrMatrix Assemble(SettlementGrid grid, SettlementLevel level, SettlementMaterialSet mats,
            out double[] rhs, Action<int, int, double> onCoupling = null)
        {
            int n = grid.CellCount;
            bool[] shield = ShieldedMask(grid, mats, level.WindFrom);
            double windFactor = level.Wind / mats.WindReference;
            var k = new double[n];
            var h = new double[n];
            rhs = new double[n];
            var builder = new SparseMatrixBuilder(n);

            for (int i = 0; i < n; i++)
            {
                var m = mats.Get(grid[i]);
                k[i] = m.K;
                h[i] = m.H * windFactor * (shield[i] ? mats.ShieldFactor : 1.0);
                double q = grid[i] == Cell.Lantern ? level.LanternHeat : m.Q;
                rhs[i] = q + h[i] * level.AirTemp;
                builder.Add(i, i, h[i]);
            }

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    int i = y * grid.Width + x;
                    if (x + 1 < grid.Width) Couple(builder, k, i, i + 1, onCoupling);
                    if (y + 1 < grid.Height) Couple(builder, k, i, i + grid.Width, onCoupling);
                }
            }
            return builder.Build();
        }

        private static void Couple(SparseMatrixBuilder b, double[] k, int i, int j, Action<int, int, double> onCoupling)
        {
            double kij = 2.0 * k[i] * k[j] / (k[i] + k[j]);
            b.Add(i, i, kij);
            b.Add(j, j, kij);
            b.Add(i, j, -kij);
            b.Add(j, i, -kij);
            onCoupling?.Invoke(i, j, kij);
        }

        public static IglooState Classify(double temperature, SettlementLevel level)
        {
            if (temperature >= level.Melt) return IglooState.Melting;
            if (temperature >= level.ComfortMin) return IglooState.Comfortable;
            return IglooState.Cold;
        }
    }

    public enum IglooState { Cold, Comfortable, Melting }
}
