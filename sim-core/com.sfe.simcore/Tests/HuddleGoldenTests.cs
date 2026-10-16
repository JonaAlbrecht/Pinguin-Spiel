using System.Collections.Generic;
using NUnit.Framework;
using SFE.SimCore.Data;
using SFE.SimCore.Thermal;

namespace SFE.SimCore.Tests
{
    public class HuddleGoldenTests
    {
        private static HuddleLevel Level =>
            HuddleLevel.FromJson(TestPaths.Read(TestPaths.Data("Levels/MG1A_LastHuddle.json")));

        [Test]
        public void MatchesPythonReference()
        {
            var g = MiniJson.ParseObject(TestPaths.Read(TestPaths.Golden("thermal/huddle_random_7.json")));
            var pos = g.Arr("positions");
            var model = new HuddleModel(Level.Model, pos.Count, g.Num("startTemp"));
            for (int i = 0; i < pos.Count; i++)
            {
                var p = MiniJson.NumArray((List<object>)pos[i]);
                model.X[i] = p[0];
                model.Y[i] = p[1];
            }
            var winds = g.Arr("windTo");
            var temps = g.Arr("temperatures");
            double tol = g.Num("tolerance");

            var w0 = MiniJson.NumArray((List<object>)winds[0]);
            model.UpdateExposure(w0[0], w0[1]);
            var exposed = MiniJson.NumArray(g.Arr("exposedAtStart"));
            for (int i = 0; i < model.Count; i++) Assert.AreEqual(exposed[i] != 0, model.Exposed[i], $"Pinguin {i} im Wind");

            for (int s = 0; s < winds.Count; s++)
            {
                var w = MiniJson.NumArray((List<object>)winds[s]);
                model.Step(g.Num("dt"), w[0], w[1]);
                var expected = MiniJson.NumArray((List<object>)temps[s]);
                for (int i = 0; i < model.Count; i++)
                    Assert.AreEqual(expected[i], model.Temperatures[i], tol * System.Math.Max(1, System.Math.Abs(expected[i])),
                        $"Schritt {s}, Pinguin {i}");
            }
        }

        [Test]
        public void LonePenguin_ReachesAnalyticSteadyState_AndFreezes()
        {
            var level = Level;
            var model = new HuddleModel(level.Model, 1, level.StartTemp);
            for (int s = 0; s < 4000; s++) model.Step(1.0, 1, 0);
            double expected = level.Model.AirTemp + level.Model.Heat / level.Model.HExposed;
            Assert.AreEqual(expected, model.Temperatures[0], 1e-6);
            Assert.Less(model.Temperatures[0], level.FreezeBelow);
        }
    }
}
