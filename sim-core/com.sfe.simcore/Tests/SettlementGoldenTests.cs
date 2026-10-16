using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SFE.SimCore.Data;
using SFE.SimCore.Thermal;

namespace SFE.SimCore.Tests
{
    /// <summary>Vergleicht den C#-Wärmekern mit den Golden Files der Python-Referenz.</summary>
    public class SettlementGoldenTests
    {
        private static SettlementLevel Level =>
            SettlementLevel.FromJson(TestPaths.Read(TestPaths.Data("Levels/MG1_Huddle_Settlement.json")));

        private static SettlementMaterialSet Materials =>
            SettlementMaterialSet.FromJson(TestPaths.Read(TestPaths.Data("Materials/materials_settlement_v1.json")));

        public static IEnumerable<string> Cases()
        {
            foreach (var f in Directory.GetFiles(TestPaths.Golden("thermal"), "settlement_*.json"))
                yield return Path.GetFileNameWithoutExtension(f);
        }

        [TestCaseSource(nameof(Cases))]
        public void MatchesPythonReference(string name)
        {
            var g = MiniJson.ParseObject(TestPaths.Read(TestPaths.Golden($"thermal/{name}.json")));
            double tol = g.Num("tolerance");
            var level = Level;
            var mats = Materials;
            var grid = SettlementGrid.FromRows(g.StrArray("tiles"));

            // Windschatten
            var shield = SettlementModel.ShieldedMask(grid, mats, level.WindFrom);
            var expectedShield = g.Arr("shielded");
            for (int y = 0; y < grid.Height; y++)
            {
                var row = MiniJson.NumArray((List<object>)expectedShield[y]);
                for (int x = 0; x < grid.Width; x++)
                    Assert.AreEqual(row[x] != 0, shield[y * grid.Width + x], $"Abschirmung ({x},{y})");
            }

            // Erster Löse-Durchlauf
            var first = new SettlementSimulation(grid, level, mats);
            while (first.SolveCount == 1 && !first.IsConverged && first.MeltPasses.Count == 0) first.Step();
            var firstExpected = MiniJson.NumArray(g.Arr("firstSolve"));
            if (first.MeltPasses.Count == 0) AssertClose(firstExpected, first.Temperatures, tol, "erster Durchlauf");

            // Kompletter Lauf mit Schmelz-Kaskade
            var sim = new SettlementSimulation(grid, level, mats);
            var r = sim.RunToEnd();
            AssertClose(MiniJson.NumArray(g.Arr("finalSolve")), sim.Temperatures, tol, "Endzustand");
            CollectionAssert.AreEqual(g.StrArray("finalTiles"), sim.Grid.ToRows());
            var passes = MiniJson.NumArray(g.Arr("meltPasses"));
            Assert.AreEqual(passes.Length, sim.MeltPasses.Count, "Anzahl Schmelz-Durchläufe");
            for (int p = 0; p < passes.Length; p++) Assert.AreEqual((int)passes[p], sim.MeltPasses[p].Length);

            var e = g.Obj("result");
            Assert.AreEqual(e.Int("igloosTotal"), r.IgloosTotal, "igloosTotal");
            Assert.AreEqual(e.Int("comfortable"), r.Comfortable, "comfortable");
            Assert.AreEqual(e.Int("cold"), r.Cold, "cold");
            Assert.AreEqual(e.Int("melted"), r.Melted, "melted");
            Assert.AreEqual(e.Int("lanterns"), r.Lanterns, "lanterns");
            Assert.AreEqual(e.Int("walls"), r.Walls, "walls");
            Assert.AreEqual(e.Int("cost"), r.Cost, "cost");
            Assert.AreEqual(e.Int("stars"), r.Stars, "stars");
        }

        [Test]
        public void FirstSolve_MatchesReference_ForMeltingCase()
        {
            // Für Fälle mit Schmelzen prüft dieser Test den ersten Durchlauf direkt über das Modell.
            var g = MiniJson.ParseObject(TestPaths.Read(TestPaths.Golden("thermal/settlement_kalle.json")));
            var grid = SettlementGrid.FromRows(g.StrArray("tiles"));
            var a = SettlementModel.Assemble(grid, Level, Materials, out var b);
            var x = new LinearAlgebra.ConjugateGradientSolver(a, b).SolveToEnd();
            AssertClose(MiniJson.NumArray(g.Arr("firstSolve")), x, g.Num("tolerance"), "erster Durchlauf Kalle");
        }

        [Test]
        public void Stars_FollowTheRules()
        {
            var level = Level;
            var r = new SettlementResult { Comfortable = 12, Melted = 0, Cost = 800 };
            Assert.AreEqual(3, SettlementResult.ComputeStars(r, level));
            r.Cost = 900;
            Assert.AreEqual(2, SettlementResult.ComputeStars(r, level));
            r.Melted = 1;
            Assert.AreEqual(1, SettlementResult.ComputeStars(r, level));
            r.Comfortable = 11;
            Assert.AreEqual(0, SettlementResult.ComputeStars(r, level));
        }

        private static void AssertClose(double[] expected, double[] actual, double relTol, string what)
        {
            Assert.AreEqual(expected.Length, actual.Length, what);
            for (int i = 0; i < expected.Length; i++)
            {
                double scale = System.Math.Max(1.0, System.Math.Abs(expected[i]));
                Assert.AreEqual(expected[i], actual[i], relTol * scale, $"{what}: Zelle {i}");
            }
        }
    }
}
