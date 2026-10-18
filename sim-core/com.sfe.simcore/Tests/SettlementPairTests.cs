using System.Collections.Generic;
using NUnit.Framework;
using SFE.SimCore.Data;
using SFE.SimCore.Thermal;

namespace SFE.SimCore.Tests
{
    /// <summary>
    /// Zwei-Felder-Beispiel am Ende von MG1 Schritt 2: Die Zahlen der Rechentafel im Spiel sind die des Modells
    /// und stimmen mit der Python-Referenz überein (Golden <c>pair_settlement</c>).
    /// </summary>
    public class SettlementPairTests
    {
        private static SettlementLevel Level =>
            SettlementLevel.FromJson(TestPaths.Read(TestPaths.Data("Levels/MG1_Huddle_Settlement.json")));

        [Test]
        public void PairLesson_MatchesPythonReference_StepByStep()
        {
            var g = MiniJson.ParseObject(TestPaths.Read(TestPaths.Golden("thermal/pair_settlement.json")));
            double tol = g.Num("tolerance");
            var lesson = Level.PairLesson;
            var steps = g.Arr("steps");
            Assert.AreEqual(lesson.Steps, steps.Count);
            foreach (var s in steps)
            {
                var e = (Dictionary<string, object>)s;
                Close(MiniJson.NumArray(e.Arr("before")), lesson.Temperatures, tol, "vorher");
                lesson.Step();
                var t = lesson.LastStepTerms();
                Close(MiniJson.NumArray(e.Arr("after")), lesson.Temperatures, tol, "neu");
                Close(MiniJson.NumArray(e.Arr("own")), t.Own, tol, "selbst erzeugt");
                Close(MiniJson.NumArray(e.Arr("fromNeighbour")), t.FromNeighbour, tol, "vom Nachbarn");
                Close(MiniJson.NumArray(e.Arr("toWind")), t.ToWind, tol, "an den Wind");
            }
            Close(MiniJson.NumArray(g.Arr("steady")), lesson.SteadyState(), tol, "Gleichgewicht");
        }

        [Test]
        public void StepTerms_AddUp_AndTheExchangeConservesHeat()
        {
            var lesson = Level.PairLesson;
            for (int k = 0; k < lesson.Steps; k++)
            {
                var before = (double[])lesson.Temperatures.Clone();
                lesson.Step();
                var t = lesson.LastStepTerms();
                for (int i = 0; i < 2; i++)
                    Assert.AreEqual(lesson.Temperatures[i] - before[i], t.Own[i] + t.FromNeighbour[i] - t.ToWind[i], 1e-9, $"Feld {i}");
                // Wärmeerhaltung: Was das Iglu bekommt, gibt die Laterne ab.
                Assert.AreEqual(0.0, t.FromNeighbour[0] + t.FromNeighbour[1], 1e-12);
                Assert.Greater(t.FromNeighbour[SettlementPairLesson.Igloo], 0.0);
            }
        }

        [Test]
        public void Steps_ApproachTheSteadyState_AndTheIglooEndsComfortable()
        {
            var level = Level;
            var lesson = level.PairLesson;
            for (int k = 0; k < lesson.Steps; k++) lesson.Step();
            double igloo = lesson.Temperatures[SettlementPairLesson.Igloo];
            Assert.That(igloo, Is.InRange(level.ComfortMin, level.Melt), "Iglu am Ende der Tafel gemütlich");
            for (int k = 0; k < 200; k++) lesson.Step();
            Close(lesson.SteadyState(), lesson.Temperatures, 1e-9, "Gleichgewicht");
            lesson.Reset();
            Assert.AreEqual(level.AirTemp, lesson.Temperatures[SettlementPairLesson.Igloo]);
        }

        private static void Close(double[] expected, double[] actual, double relTol, string what)
        {
            Assert.AreEqual(expected.Length, actual.Length, what);
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], actual[i], relTol * System.Math.Max(1.0, System.Math.Abs(expected[i])), $"{what}: Feld {i}");
        }
    }
}
