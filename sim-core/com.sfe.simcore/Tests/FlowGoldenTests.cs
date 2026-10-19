using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SFE.SimCore.Data;
using SFE.SimCore.Flow;

namespace SFE.SimCore.Tests
{
    /// <summary>
    /// MG2 Polar-Express gegen die Python-Referenz (golden/flow): Maske, Widerstand, Kraftverlauf, Strömungsfeld,
    /// Plätze, Fahrzeit und Sterne – je Form und Auflösung. Toleranz 1e-4 relativ (docs/04 Abschnitt 4.4).
    /// </summary>
    public class FlowGoldenTests
    {
        internal static PolarExpressLevel Level =>
            PolarExpressLevel.FromJson(TestPaths.Read(TestPaths.Data("Levels/MG2_PolarExpress.json")));

        private static IEnumerable<string> Cases()
        {
            foreach (var f in Directory.GetFiles(TestPaths.Golden("flow"), "flow_*_*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(f);
                if (name != "flow_lesson" && name != "flow_travel") yield return name;
            }
        }

        [TestCaseSource(nameof(Cases))]
        public void TrainRun_MatchesPythonReference(string name)
        {
            var g = MiniJson.ParseObject(TestPaths.Read(TestPaths.Golden("flow/" + name + ".json")));
            double tol = g.Num("tolerance");
            var lv = Level;
            var res = lv.Resolution(g.Str("resolution"));
            var shape = TrainShape.FromJson(g);
            var run = new TrainRun(shape, lv, res);

            Assert.AreEqual(g.Int("nx"), res.Nx);
            Assert.AreEqual(g.Int("ny"), res.Ny);
            Assert.AreEqual(g.Num("tau"), lv.Tau(res), 1e-12, "tau");
            Assert.AreEqual(g.Int("steps"), run.TotalSteps);
            Assert.AreEqual(g.Int("averageSteps"), run.AverageSteps);
            Assert.AreEqual(g.Int("ramp"), run.Lbm.Ramp);
            CollectionAssert.AreEqual(g.StrArray("mask"), TrainGeometry.MaskRows(run.Lbm.Solid, res.Nx, res.Ny), "Maske");

            var forces = MiniJson.NumArray(g.Arr("forceEvery50"));
            int next = 0;
            while (!run.IsConverged)
            {
                run.Step(50);
                Close(forces[next++], run.Lbm.ForceX, tol, 1e-3, $"Kraft nach {run.Iteration} Schritten");
            }
            Assert.AreEqual(forces.Length, next);
            Close(g.Num("drag"), run.Drag, tol, 1e-3, "Widerstandsbeiwert");

            var cells = MiniJson.NumArray(g.Arr("sampleCells"));
            var rho = MiniJson.NumArray(g.Arr("rho"));
            var ux = MiniJson.NumArray(g.Arr("ux"));
            var uy = MiniJson.NumArray(g.Arr("uy"));
            for (int i = 0; i < cells.Length; i++)
            {
                run.Lbm.Moments((int)cells[i], out double r, out double x, out double y);
                Close(rho[i], r, tol, 1e-3, $"Luft in Kästchen {cells[i]}");
                Close(ux[i], x, tol, 1e-2, $"Geschwindigkeit x in Kästchen {cells[i]}");
                Close(uy[i], y, tol, 1e-2, $"Geschwindigkeit y in Kästchen {cells[i]}");
            }

            var result = run.Result();
            Assert.AreEqual(g.Int("seats"), result.Seats, "Plätze");
            Close(g.Num("speed"), result.Speed, tol, 1e-3, "Geschwindigkeit");
            Close(g.Num("time"), result.Time, tol, 1e-3, "Fahrzeit");
            Assert.AreEqual(g.Int("stars"), result.Stars, "Sterne");
        }

        [Test]
        public void Lesson_MatchesPythonReference_StepByStep()
        {
            var g = MiniJson.ParseObject(TestPaths.Read(TestPaths.Golden("flow/flow_lesson.json")));
            double tol = g.Num("tolerance");
            var lesson = new RowLesson(Level.Lesson);
            foreach (var o in g.Arr("steps"))
            {
                var e = (Dictionary<string, object>)o;
                var s = lesson.Step();
                Same(e, "before", s.Before, tol);
                Same(e, "after", s.After, tol);
                Same(e, "comeIn", s.ComeIn, tol);
                Same(e, "moveOn", s.MoveOn, tol);
                Same(e, "rightBefore", s.RightBefore, tol);
                Same(e, "leftBefore", s.LeftBefore, tol);
                Same(e, "stayBefore", s.StayBefore, tol);
                Same(e, "rightEven", s.RightEven, tol);
                Same(e, "leftEven", s.LeftEven, tol);
                Same(e, "stayEven", s.StayEven, tol);
            }
            for (int i = 0; i < Level.Lesson.RunSteps; i++) lesson.Step();
            Same(g, "afterRun", lesson.Air, tol);
        }

        [Test]
        public void Travel_MatchesPythonReference()
        {
            var g = MiniJson.ParseObject(TestPaths.Read(TestPaths.Golden("flow/flow_travel.json")));
            var lv = Level;
            foreach (var o in g.Arr("rows"))
            {
                var r = (Dictionary<string, object>)o;
                Assert.AreEqual(r.Num("speed"), Travel.Speed(r.Num("drag"), lv), 1e-9);
                Assert.AreEqual(r.Num("time"), Travel.Time(r.Num("drag"), lv), 1e-9 * System.Math.Max(1, r.Num("time")));
            }
        }

        private static void Same(Dictionary<string, object> e, string key, double[] actual, double tol)
        {
            var expected = MiniJson.NumArray(e.Arr(key));
            Assert.AreEqual(expected.Length, actual.Length, key);
            for (int i = 0; i < expected.Length; i++) Close(expected[i], actual[i], tol, 1.0, $"{key}[{i}]");
        }

        /// <summary>Relativ zu <paramref name="tol"/>, bei Werten nahe null bezogen auf <paramref name="floor"/>.</summary>
        internal static void Close(double expected, double actual, double tol, double floor, string what) =>
            Assert.AreEqual(expected, actual, tol * System.Math.Max(floor, System.Math.Abs(expected)), what);
    }
}
