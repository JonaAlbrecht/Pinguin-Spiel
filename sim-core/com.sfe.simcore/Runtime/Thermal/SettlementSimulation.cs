using System;
using System.Collections.Generic;
using SFE.SimCore.LinearAlgebra;

namespace SFE.SimCore.Thermal
{
    /// <summary>
    /// Führt Lösen + Schmelz-Kaskade schrittweise aus:
    /// lösen → Iglus mit T ≥ Schmelzgrenze werden Wasser → neu lösen … (max. <see cref="SettlementLevel.MaxMeltPasses"/>).
    /// Die Spiel-Schicht ruft <see cref="Step"/> pro Frame auf und zeigt <see cref="Temperatures"/> als Wärmebild.
    /// </summary>
    public sealed class SettlementSimulation : IStepSolver
    {
        private readonly SettlementLevel _level;
        private readonly SettlementMaterialSet _mats;
        private readonly double _tolerance;
        private readonly int _originalIgloos;
        private ConjugateGradientSolver _cg;

        public SettlementGrid Grid { get; }
        public List<int[]> MeltPasses { get; } = new List<int[]>();
        public double[] Temperatures => _cg.Solution;
        public int Iteration { get; private set; }
        public double Residual => _cg.Residual;
        public bool IsConverged { get; private set; }
        public int SolveCount { get; private set; }

        /// <summary>Wird aufgerufen, wenn Iglus schmelzen (Zellindizes).</summary>
        public event Action<int[]> Melted;

        public SettlementSimulation(SettlementGrid grid, SettlementLevel level, SettlementMaterialSet mats,
            double tolerance = 1e-10)
        {
            Grid = grid.Clone();
            _level = level;
            _mats = mats;
            _tolerance = tolerance;
            _originalIgloos = Grid.Count(Cell.Igloo);
            var start = new double[Grid.CellCount];
            for (int i = 0; i < start.Length; i++) start[i] = level.AirTemp;
            StartSolve(start);
        }

        private void StartSolve(double[] x0)
        {
            var a = SettlementModel.Assemble(Grid, _level, _mats, out double[] b);
            _cg = new ConjugateGradientSolver(a, b, x0, _tolerance);
            SolveCount++;
        }

        public void Step(int maxIterations = 1)
        {
            int budget = maxIterations;
            while (budget > 0 && !IsConverged)
            {
                int before = _cg.Iteration;
                _cg.Step(budget);
                int done = _cg.Iteration - before;
                Iteration += done;
                budget -= Math.Max(done, 1);
                if (!_cg.IsConverged)
                {
                    if (_cg.HasStalled) throw new InvalidOperationException("Wärme-Löser ist nicht konvergiert.");
                    continue;
                }

                var melting = new List<int>();
                if (MeltPasses.Count < _level.MaxMeltPasses)
                {
                    for (int i = 0; i < Grid.CellCount; i++)
                        if (Grid[i] == Cell.Igloo && _cg.Solution[i] >= _level.Melt) melting.Add(i);
                }
                if (melting.Count == 0)
                {
                    IsConverged = true;
                    break;
                }
                var pass = melting.ToArray();
                foreach (int i in pass) Grid[i] = Cell.Water;
                MeltPasses.Add(pass);
                Melted?.Invoke(pass);
                StartSolve((double[])_cg.Solution.Clone());
            }
        }

        /// <summary>Rechnet alles bis zum Ende (für Tests und Vorschau).</summary>
        public SettlementResult RunToEnd()
        {
            while (!IsConverged) Step(256);
            return Result();
        }

        public SettlementResult Result()
        {
            if (!IsConverged) throw new InvalidOperationException("Simulation ist noch nicht fertig.");
            var r = new SettlementResult { IgloosTotal = _originalIgloos };
            var states = new IglooState[Grid.CellCount];
            for (int i = 0; i < Grid.CellCount; i++)
            {
                if (Grid[i] != Cell.Igloo) continue;
                var s = SettlementModel.Classify(_cg.Solution[i], _level);
                states[i] = s;
                if (s == IglooState.Comfortable) r.Comfortable++;
                else if (s == IglooState.Cold) r.Cold++;
            }
            foreach (var p in MeltPasses) r.Melted += p.Length;
            r.Lanterns = Grid.Count(Cell.Lantern);
            r.Walls = Grid.Count(Cell.Wall);
            r.Cost = r.Lanterns * _level.LanternCost;
            r.IglooStates = states;
            r.Stars = SettlementResult.ComputeStars(r, _level);
            return r;
        }

        public static SettlementResult Run(SettlementGrid grid, SettlementLevel level, SettlementMaterialSet mats) =>
            new SettlementSimulation(grid, level, mats).RunToEnd();
    }

    public sealed class SettlementResult
    {
        public int IgloosTotal;
        public int Comfortable;
        public int Cold;
        public int Melted;
        public int Lanterns;
        public int Walls;
        public int Cost;
        public int Stars;
        /// <summary>Zustand je Zelle (nur für Iglu-Zellen im End-Raster sinnvoll).</summary>
        public IglooState[] IglooStates;

        /// <summary>★ Ziel-Anzahl gemütlicher Iglus im Budget, ★★ dabei nichts geschmolzen, ★★★ zusätzlich ≤ 80 % Budget.</summary>
        public static int ComputeStars(SettlementResult r, SettlementLevel level)
        {
            if (r.Comfortable < level.IglooTarget || r.Cost > level.Budget) return 0;
            if (r.Melted > 0) return 1;
            return r.Cost <= 0.8 * level.Budget ? 3 : 2;
        }
    }
}
