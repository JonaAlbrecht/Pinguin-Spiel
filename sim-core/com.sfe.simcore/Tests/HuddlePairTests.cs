using System.Collections.Generic;
using NUnit.Framework;
using SFE.SimCore.Data;
using SFE.SimCore.Thermal;

namespace SFE.SimCore.Tests
{
    /// <summary>
    /// Zwei-Pinguine-Beispiel vor dem Huddle: Die Zahlen der Rechenregel im Spiel sind die des echten Modells
    /// und stimmen mit der Python-Referenz überein (Golden <c>huddle_pair</c>).
    /// </summary>
    public class HuddlePairTests
    {
        private static HuddleLevel Level =>
            HuddleLevel.FromJson(TestPaths.Read(TestPaths.Data("Levels/MG1A_LastHuddle.json")));

        [Test]
        public void PairLesson_MatchesPythonReference_StepByStep()
        {
            var g = MiniJson.ParseObject(TestPaths.Read(TestPaths.Golden("thermal/huddle_pair.json")));
            double tol = g.Num("tolerance");
            var level = Level;
            var lesson = level.PairLesson;
            var model = lesson.CreateModel(level.Model);
            var steps = g.Arr("steps");
            Assert.AreEqual(lesson.Steps, steps.Count);
            foreach (var s in steps)
            {
                var e = (Dictionary<string, object>)s;
                Close(MiniJson.NumArray(e.Arr("before")), model.Temperatures, tol, "vorher");
                model.Step(lesson.Dt, lesson.WindEast, lesson.WindNorth);
                var t = model.LastStepTerms(lesson.Dt);
                Close(MiniJson.NumArray(e.Arr("after")), model.Temperatures, tol, "neu");
                Close(MiniJson.NumArray(e.Arr("own")), t.Own, tol, "selbst erzeugt");
                Close(MiniJson.NumArray(e.Arr("fromNeighbours")), t.FromNeighbours, tol, "vom Nachbarn");
                Close(MiniJson.NumArray(e.Arr("toWind")), t.ToWind, tol, "an den Wind");
            }
        }

        [Test]
        public void StepTerms_AddUp_AndTheExchangeConservesHeat()
        {
            var level = Level;
            var lesson = level.PairLesson;
            var model = lesson.CreateModel(level.Model);
            for (int k = 0; k < lesson.Steps; k++)
            {
                var before = (double[])model.Temperatures.Clone();
                model.Step(lesson.Dt, lesson.WindEast, lesson.WindNorth);
                var t = model.LastStepTerms(lesson.Dt);
                for (int i = 0; i < 2; i++)
                    Assert.AreEqual(model.Temperatures[i] - before[i], t.Own[i] + t.FromNeighbours[i] - t.ToWind[i], 1e-9, $"Pinguin {i}");
                // Wärmeerhaltung: Was Pip (1) bekommt, gibt Grete (0) ab.
                Assert.AreEqual(0.0, t.FromNeighbours[0] + t.FromNeighbours[1], 1e-12);
                Assert.Greater(t.FromNeighbours[1], 0.0);
                // Grete steht im Wind, Pip in ihrem Windschatten.
                Assert.IsTrue(model.Exposed[0]);
                Assert.IsFalse(model.Exposed[1]);
            }
        }

        private static void Close(double[] expected, double[] actual, double relTol, string what)
        {
            Assert.AreEqual(expected.Length, actual.Length, what);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], actual[i], relTol * System.Math.Max(1.0, System.Math.Abs(expected[i])), $"{what}: Pinguin {i}");
        }
    }
}
