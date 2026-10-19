using NUnit.Framework;
using SFE.SimCore.Process;

namespace SFE.SimCore.Tests
{
    /// <summary>MG3: Regeln der Fabrik, die das Spiel erzählt – unabhängig von den Golden Files geprüft.</summary>
    public class ProcessRulesTests
    {
        private static ProcessLevel Level =>
            ProcessLevel.FromJson(TestPaths.Read(TestPaths.Data("Levels/MG3_Fischfabrik.json")));

        private static FactoryConfig Config(string[] rows) => FactoryLayout.FromRows(rows).ToConfig(Level);

        [Test]
        public void NoBoxVanishes()
        {
            var level = Level;
            foreach (var rows in new[] { level.OlafTiles, level.KalleTiles, level.HintTiles })
            {
                for (ulong seed = 0; seed < 4; seed++)
                {
                    var sim = new FactorySimulation(Config(rows), seed);
                    foreach (double t in new[] { 37.0, 211.0, level.ShiftLength })
                    {
                        sim.RunUntil(t);
                        int inside = sim.InTransport + sim.Ready;
                        for (int s = 0; s < sim.StageCount; s++) inside += sim.InStation(s);
                        Assert.AreEqual(sim.Arrived, sim.Stolen + sim.Spoiled + inside + sim.Delivered, $"Zeit {t}");
                    }
                }
            }
        }

        [Test]
        public void KallesDoubleRobots_MakeItWorse()
        {
            var level = Level;
            var olaf = Replications.Run(Config(level.OlafTiles), level.Seed, level.Replications);
            var kalle = Replications.Run(Config(level.KalleTiles), level.Seed, level.Replications);
            Assert.Less(kalle.MeanDelivered, olaf.MeanDelivered - 10);
            Assert.Greater(Config(level.KalleTiles).RobotDelay, 0.0);
            Assert.AreEqual(0.0, Config(level.OlafTiles).RobotDelay);
        }

        [Test]
        public void Example_ReachesThreeStars_WithinBudget()
        {
            var level = Level;
            var s = Replications.Run(Config(level.HintTiles), level.Seed, level.Replications);
            Assert.AreEqual(3, Replications.Stars(s, level));
            Assert.LessOrEqual(FactoryLayout.FromRows(level.HintTiles).Cost(level), level.Budget);
        }

        [Test]
        public void SameSeedSameDay_OtherSeedOtherDay()
        {
            var c = Config(Level.OlafTiles);
            var a = Replications.RunDay(c, 5);
            var b = Replications.RunDay(c, 5);
            var d = Replications.RunDay(c, 6);
            Assert.AreEqual(a.Delivered, b.Delivered);
            Assert.AreEqual(a.LeadTime, b.LeadTime);
            Assert.AreNotEqual(a.LeadTime, d.LeadTime);
        }

        [Test]
        public void Stars_FollowTheRules()
        {
            var level = Level;
            var days = new DayResult[10];
            for (int i = 0; i < days.Length; i++)
                days[i] = new DayResult { Delivered = level.Target + 10, Spoiled = 0, LeadTime = 30, Utilization = new double[3], MaxQueue = new int[3], EndQueue = new int[3] };
            var s = Replications.Summarize(days);
            Assert.AreEqual(3, Replications.Stars(s, level));
            s.MeanSpoiled = level.SpoilMax + 0.5;
            Assert.AreEqual(1, Replications.Stars(s, level));
            s.MeanSpoiled = 0;
            s.Lower = level.Target - 1;
            Assert.AreEqual(2, Replications.Stars(s, level));
            s.MeanDelivered = level.Target - 1;
            Assert.AreEqual(0, Replications.Stars(s, level));
        }

        [Test]
        public void MissingParts_AreReported_AndCannotRun()
        {
            var level = Level;
            var empty = FactoryLayout.FromRows(level.InitialTiles);
            Assert.AreEqual(4, empty.Problems(level).Count); // S, F, P, Kühlregal
            Assert.Throws<System.InvalidOperationException>(() => empty.ToConfig(level));
            Assert.AreEqual(0, FactoryLayout.FromRows(level.HintTiles).Problems(level).Count);
        }

        [Test]
        public void Lesson_RowsAddUp()
        {
            var lesson = new StationLesson(Level.Lesson);
            int prev = 0;
            for (int i = 0; i < lesson.Events; i++)
            {
                var r = lesson.Next();
                Assert.AreEqual(prev, r.Before);
                Assert.AreEqual(r.After, r.Before + r.Arrived - r.Finished);
                prev = r.After;
            }
        }
    }
}
