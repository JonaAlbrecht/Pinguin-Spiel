using NUnit.Framework;
using SFE.SimCore.Data;
using SFE.SimCore.Thermal;

namespace SFE.SimCore.Tests
{
    /// <summary>
    /// Wärmewelle in MG1 Schritt 3: dieselben Zahlen wie die Python-Referenz (Golden <c>wave_settlement</c>),
    /// und der Endzustand ist der der Siedlung – keine zweite Wahrheit.
    /// </summary>
    public class SettlementWaveTests
    {
        private static SettlementLevel Level =>
            SettlementLevel.FromJson(TestPaths.Read(TestPaths.Data("Levels/MG1_Huddle_Settlement.json")));

        private static SettlementMaterialSet Materials =>
            SettlementMaterialSet.FromJson(TestPaths.Read(TestPaths.Data("Materials/materials_settlement_v1.json")));

        [Test]
        public void Wave_MatchesPythonReference_StepByStep()
        {
            var g = MiniJson.ParseObject(TestPaths.Read(TestPaths.Golden("thermal/wave_settlement.json")));
            double tol = g.Num("tolerance");
            var level = Level;
            var wave = new SettlementWave(level.Wave, level, Materials);
            var cells = MiniJson.NumArray(g.Arr("cells"));
            var steps = g.Arr("steps");
            Assert.AreEqual(level.Wave.Steps, steps.Count);
            for (int s = 0; s < steps.Count; s++)
            {
                wave.Advance();
                var expected = MiniJson.NumArray((System.Collections.Generic.List<object>)steps[s]);
                for (int c = 0; c < cells.Length; c++)
                    Assert.AreEqual(expected[c], wave.Temperatures[(int)cells[c]], tol * System.Math.Max(1.0, System.Math.Abs(expected[c])), $"Schritt {s + 1}, Feld {cells[c]}");
                if (s == 0) Close(MiniJson.NumArray(g.Arr("first")), wave.Temperatures, tol, "erster Schritt");
            }
            Close(MiniJson.NumArray(g.Arr("last")), wave.Temperatures, tol, "letzter Schritt");
        }

        [Test]
        public void Wave_EndsAtTheSettlementSteadyState()
        {
            var level = Level;
            var mats = Materials;
            var wave = new SettlementWave(level.Wave, level, mats);
            for (int s = 0; s < 150; s++) wave.Advance();
            var sim = new SettlementSimulation(wave.Grid, level, mats);
            sim.RunToEnd();
            Close(sim.Temperatures, wave.Temperatures, 1e-7, "Gleichgewicht");
        }

        private static void Close(double[] expected, double[] actual, double relTol, string what)
        {
            Assert.AreEqual(expected.Length, actual.Length, what);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], actual[i], relTol * System.Math.Max(1.0, System.Math.Abs(expected[i])), $"{what}: Feld {i}");
        }
    }
}
