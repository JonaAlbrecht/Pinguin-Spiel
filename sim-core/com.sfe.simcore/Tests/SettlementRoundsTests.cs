using NUnit.Framework;
using SFE.SimCore.Data;
using SFE.SimCore.Thermal;

namespace SFE.SimCore.Tests
{
    /// <summary>Spielregeln der Runden in MG1 Schritt 5 (beste Runde, Rundenvergleich, Iglu-Waage, Rückgängig).</summary>
    public class SettlementRoundsTests
    {
        private static SettlementResult R(int stars, int cosy, int melted = 0, int lanterns = 9) =>
            new SettlementResult { Stars = stars, Comfortable = cosy, Melted = melted, Lanterns = lanterns };

        // ---------- beste Runde (F-02) ----------

        [Test]
        public void FirstRound_IsAlwaysTheBest() => Assert.IsTrue(SettlementRounds.IsBetter(R(0, 0), null));

        [Test]
        public void MoreStars_Win_EvenWithFewerCosyIgloos() => Assert.IsTrue(SettlementRounds.IsBetter(R(2, 12), R(1, 14)));

        [Test]
        public void FewerStars_NeverReplaceTheBest() => Assert.IsFalse(SettlementRounds.IsBetter(R(1, 16), R(2, 12)));

        [Test]
        public void SameStars_MoreCosyIgloos_Win() => Assert.IsTrue(SettlementRounds.IsBetter(R(0, 9), R(0, 7)));

        [Test]
        public void ATie_KeepsTheEarlierRound() => Assert.IsFalse(SettlementRounds.IsBetter(R(2, 12), R(2, 12)));

        [Test]
        public void AWorseReplay_DoesNotOverwriteTheBest()
        {
            // Review zu PR #2: „nochmal planen“ überschrieb die beste Runde mit der ersten neuen, auch wenn sie schlechter war.
            var best = R(3, 12, lanterns: 8);
            var replayFirstRound = R(0, 4);
            Assert.IsFalse(SettlementRounds.IsBetter(replayFirstRound, best));
        }

        // ---------- Rundenvergleich und Klaras Kommentar ----------

        [Test]
        public void Compare_ReportsDifferencesAsNowMinusBefore()
        {
            var c = SettlementRounds.Compare(R(0, 8, melted: 3, lanterns: 10), R(1, 12, melted: 1, lanterns: 9));
            Assert.AreEqual(4, c.CosyDelta);
            Assert.AreEqual(-2, c.MeltDelta);
            Assert.AreEqual(-1, c.LanternDelta);
            Assert.IsFalse(c.Unchanged);
        }

        [Test]
        public void MoreStars_AreBetter() => Assert.AreEqual(RoundVerdict.Better, SettlementRounds.Compare(R(1, 12), R(2, 12)).Verdict);

        [Test]
        public void SameStars_MoreCosy_AreBetter() => Assert.AreEqual(RoundVerdict.Better, SettlementRounds.Compare(R(0, 5), R(0, 8)).Verdict);

        [Test]
        public void FewerLanterns_SameResult_IsCheaper() =>
            Assert.AreEqual(RoundVerdict.Cheaper, SettlementRounds.Compare(R(2, 12, lanterns: 10), R(2, 12, lanterns: 9)).Verdict);

        [Test]
        public void FewerLanterns_ButMoreMelted_IsNotPraisedAsCheaper()
        {
            // QA-Befund: Klara lobte „gut gewirtschaftet“, obwohl mehr geschmolzen war.
            var v = SettlementRounds.Compare(R(0, 6, melted: 0, lanterns: 10), R(0, 6, melted: 2, lanterns: 9)).Verdict;
            Assert.AreNotEqual(RoundVerdict.Cheaper, v);
        }

        [Test]
        public void FewerStars_AreWorse() => Assert.AreEqual(RoundVerdict.Worse, SettlementRounds.Compare(R(2, 12), R(1, 12, melted: 1)).Verdict);

        [Test]
        public void FewerCosy_AreWorse() => Assert.AreEqual(RoundVerdict.Worse, SettlementRounds.Compare(R(0, 9), R(0, 6)).Verdict);

        [Test]
        public void IdenticalRounds_AreTheSame()
        {
            var c = SettlementRounds.Compare(R(1, 12, 1), R(1, 12, 1));
            Assert.AreEqual(RoundVerdict.Same, c.Verdict);
            Assert.IsTrue(c.Unchanged);
        }

        // ---------- Iglu-Waage ----------

        private static SettlementLevel Level =>
            SettlementLevel.FromJson(TestPaths.Read(TestPaths.Data("Levels/MG1_Huddle_Settlement.json")));

        private static SettlementMaterialSet Materials =>
            SettlementMaterialSet.FromJson(TestPaths.Read(TestPaths.Data("Materials/materials_settlement_v1.json")));

        [Test]
        public void BalanceScale_IsTheLargestIglooThroughput_SoEveryBarFits()
        {
            var level = Level;
            var sim = new SettlementSimulation(SettlementGrid.FromRows(level.HintTiles), level, Materials);
            sim.RunToEnd();
            var b = SettlementBalance.Compute(sim.Grid, level, Materials, sim.Temperatures);
            double scale = SettlementRounds.BalanceScale(sim.Grid, b);
            bool reached = false;
            for (int i = 0; i < sim.Grid.CellCount; i++)
            {
                if (sim.Grid[i] != Cell.Igloo) continue;
                Assert.LessOrEqual(b.In(i), scale + 1e-12);
                reached |= System.Math.Abs(b.In(i) - scale) < 1e-12;
            }
            Assert.IsTrue(reached, "ein Iglu füllt die Skala ganz");
        }

        [Test]
        public void Throughput_DoesNotTellHowWarmAnIglooIs()
        {
            // Hält die Spieltexte ehrlich: Iglus zwischen Laternen sind kälter, leiten aber MEHR Wärme weiter als
            // wärmere am Rand. Darum dürfen die Texte der Waage „lange Balken = warm“ nicht behaupten
            // (erster Entwurf von v0.2 tat das – dieser Test hat es aufgedeckt).
            var level = Level;
            var sim = new SettlementSimulation(SettlementGrid.FromRows(level.HintTiles), level, Materials);
            sim.RunToEnd();
            var b = SettlementBalance.Compute(sim.Grid, level, Materials, sim.Temperatures);
            int warm = -1, cold = -1;
            for (int i = 0; i < sim.Grid.CellCount; i++)
            {
                if (sim.Grid[i] != Cell.Igloo) continue;
                if (warm < 0 || sim.Temperatures[i] > sim.Temperatures[warm]) warm = i;
                if (cold < 0 || sim.Temperatures[i] < sim.Temperatures[cold]) cold = i;
            }
            Assert.Greater(b.In(cold), b.In(warm));
            var texts = MiniJson.ParseObject(TestPaths.Read(TestPaths.Data("Content/mg1_content_de.json"))).Obj("balance");
            StringAssert.DoesNotContain("je länger", texts.Str("explain").ToLowerInvariant());
        }

        [Test]
        public void BalanceScale_IsZero_WhenNoIglooIsLeft()
        {
            var level = Level;
            var sim = new SettlementSimulation(SettlementGrid.FromRows(level.KalleTiles), level, Materials);
            sim.RunToEnd(); // Kalles Siedlung: alles geschmolzen
            var b = SettlementBalance.Compute(sim.Grid, level, Materials, sim.Temperatures);
            Assert.AreEqual(0.0, SettlementRounds.BalanceScale(sim.Grid, b));
        }

        [Test]
        public void Balance_RejectsTemperaturesThatDoNotFitTheGrid()
        {
            var grid = SettlementGrid.FromRows(Level.HintTiles);
            Assert.Throws<System.ArgumentException>(() => SettlementBalance.Compute(grid, Level, Materials, new double[3]));
        }

        // ---------- Rückgängig ----------

        [Test]
        public void AStrokeThatChangedNothing_IsNotAnUndoStep()
        {
            // Review zu PR #2: abgelehnte Klicks (Grenze erreicht) legten Rückgängig-Schritte an.
            var rows = new[] { "..I", "L.." };
            Assert.IsFalse(SettlementRounds.Changed(rows, new[] { "..I", "L.." }));
            Assert.IsTrue(SettlementRounds.Changed(rows, new[] { "..I", "LI." }));
        }
    }
}
