using NUnit.Framework;
using SFE.SimCore.Flow;

namespace SFE.SimCore.Tests
{
    /// <summary>Regeln des Strömungskerns unabhängig von den Golden Files: Erhaltung, Ränder, Rechentafel, Zugform.</summary>
    public class FlowRulesTests
    {
        private static PolarExpressLevel Level => FlowGoldenTests.Level;

        [Test]
        public void ClosedBox_KeepsItsAir_AndSettles()
        {
            int nx = 12, ny = 7;
            var solid = new bool[nx * ny];
            solid[3 * nx + 5] = solid[3 * nx + 6] = true;
            var lbm = new LbmD2Q9(nx, ny, 0.8, solid);
            var rnd = new SFE.SimCore.Tests.TinyRandom(3);
            for (int c = 0; c < nx * ny; c++)
                if (!solid[c]) lbm.SetEquilibrium(c, 1.0 + 0.2 * rnd.Next(), 0.05 * rnd.Next(), 0.0);
            double air0 = lbm.TotalAir();
            lbm.Step(400);
            Assert.AreEqual(air0, lbm.TotalAir(), 1e-10 * air0, "Luft geht nicht verloren");
            for (int c = 0; c < nx * ny; c++)
            {
                lbm.Moments(c, out _, out double ux, out double uy);
                Assert.Less(System.Math.Abs(ux) + System.Math.Abs(uy), 2e-3);
            }
        }

        [Test]
        public void BounceBack_ReversesAPopulation_AndPushesTheTrain()
        {
            var solid = new bool[3];
            solid[2] = true;
            var lbm = new LbmD2Q9(3, 1, 1e9, solid);
            lbm.F[1 * 3 + 1] += 0.5;
            lbm.Step();
            Assert.AreEqual(1.0 / 9.0 + 0.5, lbm.F[3 * 3 + 1], 1e-6);
            Assert.AreEqual(2 * (1.0 / 9.0 + 0.5) + 4.0 / 36.0, lbm.ForceX, 1e-6);
        }

        [Test]
        public void EmptyChannel_CarriesTheInflow_WithoutDrag()
        {
            var lbm = new LbmD2Q9(40, 10, 0.8, null, true, 0.08, 50);
            lbm.Step(600);
            for (int c = 0; c < 400; c++)
            {
                lbm.Moments(c, out _, out double ux, out double uy);
                Assert.AreEqual(0.08, ux, 2e-3);
                Assert.AreEqual(0.0, uy, 2e-3);
            }
            Assert.AreEqual(0.0, lbm.ForceX);
        }

        [Test]
        public void Lesson_TableAddsUp_AndAirIsNeverLost()
        {
            var lv = Level;
            var lesson = new RowLesson(lv.Lesson);
            double total = 0;
            foreach (double a in lv.Lesson.Air) total += a;
            for (int k = 0; k < lv.Lesson.Steps; k++)
            {
                var s = lesson.Step();
                double come = 0, move = 0, after = 0;
                for (int x = 0; x < lesson.Cells; x++)
                {
                    Assert.AreEqual(s.After[x], s.Before[x] + s.ComeIn[x] - s.MoveOn[x], 1e-12);
                    Assert.AreEqual(s.Before[x], s.RightEven[x] + s.LeftEven[x] + s.StayEven[x], 1e-12, "Ausgleichen behält die Luft");
                    come += s.ComeIn[x]; move += s.MoveOn[x]; after += s.After[x];
                }
                Assert.AreEqual(come, move, 1e-12);
                Assert.AreEqual(total, after, 1e-12);
            }
        }

        /// <summary>Die gerundete Tafel geht auch in der Anzeige auf, und kein Wert weicht mehr als eine Stelle ab.</summary>
        [Test]
        public void Lesson_RoundedRowsAddUp()
        {
            var lv = Level;
            var lesson = new RowLesson(lv.Lesson);
            const double unit = 0.1, scale = 10.0; // Anzeige: Luft × 10 mit einer Nachkommastelle
            for (int k = 0; k < lv.Lesson.Steps; k++)
            {
                var s = lesson.Step();
                for (int x = 0; x < lesson.Cells; x++)
                {
                    RowLesson.RoundRow(s.Before[x] * scale, s.ComeIn[x] * scale, s.MoveOn[x] * scale, s.After[x] * scale, unit,
                        out double b, out double c, out double m, out double a);
                    Assert.AreEqual(a, b + c - m, 1e-9, $"Schritt {k + 1}, Kästchen {x}");
                    Assert.LessOrEqual(System.Math.Abs(c - s.ComeIn[x] * scale), unit + 1e-9);
                    Assert.LessOrEqual(System.Math.Abs(m - s.MoveOn[x] * scale), unit + 1e-9);
                }
            }
        }

        [Test]
        public void Grob_CannotTellKallesBoxFromTheRoundedNose()
        {
            var lv = Level;
            var grob = lv.Resolution("grob");
            var fein = lv.Resolution("fein");
            CollectionAssert.AreEqual(TrainGeometry.Mask(lv.Shapes["kalle"], lv, grob), TrainGeometry.Mask(lv.Shapes["start"], lv, grob));
            CollectionAssert.AreNotEqual(TrainGeometry.Mask(lv.Shapes["kalle"], lv, fein), TrainGeometry.Mask(lv.Shapes["start"], lv, fein));
        }

        [Test]
        public void Clamp_KeepsHandlesInTheNoseArea()
        {
            var lv = Level;
            var s = lv.Shapes["start"].Clone();
            s.X[0] = 5; s.Y[0] = 40; s.X[1] = 90; s.Y[1] = -3; s.X[2] = 10; s.Y[2] = 30; s.X[3] = 200; s.Y[3] = 1;
            var c = s.Clamped(lv);
            for (int i = 1; i < 4; i++) Assert.GreaterOrEqual(c.X[i], c.X[i - 1]);
            Assert.AreEqual(lv.Start, c.X[0]);
            Assert.AreEqual(lv.Height, c.Y[3]);
            Assert.LessOrEqual(c.X[3], lv.Start + lv.NoseMax);
            Assert.GreaterOrEqual(c.Y[0], lv.TipMin);
            for (int i = 0; i < 4; i++) Assert.That(c.Y[i], Is.InRange(0.0, lv.Height));
        }

        [Test]
        public void Seats_DropWithALongNose_AndTransitionsDoNotCount()
        {
            var lv = Level;
            int box = TrainGeometry.Seats(lv.Shapes["kalle"], lv), hint = TrainGeometry.Seats(lv.Shapes["hint"], lv);
            Assert.Greater(box, hint);
            Assert.GreaterOrEqual(hint, lv.Passengers);
            var covered = lv.Shapes["kalle"].Clone();
            for (int i = 0; i < covered.Covered.Length; i++) covered.Covered[i] = true;
            Assert.AreEqual(box, TrainGeometry.Seats(covered, lv));
        }

        [Test]
        public void Travel_FollowsTheForceBalance_AndStarsTheRules()
        {
            var lv = Level;
            foreach (double cd in new[] { 2.0, 2.5, 3.0 })
            {
                double v = Travel.Speed(cd, lv);
                Assert.AreEqual(lv.Traction, lv.Rolling + lv.DragScale * cd * (v + lv.Headwind) * (v + lv.Headwind), 1e-12);
            }
            Assert.AreEqual(lv.MinSpeed, Travel.Speed(50.0, lv));
            Assert.AreEqual(0, Travel.Stars(lv.TargetTime + 0.1, 999, lv));
            Assert.AreEqual(1, Travel.Stars(lv.TargetTime, lv.Passengers - 1, lv));
            Assert.AreEqual(2, Travel.Stars(lv.TargetTime, lv.Passengers, lv));
            Assert.AreEqual(3, Travel.Stars(lv.BestTime, lv.Passengers, lv));
        }
    }

    /// <summary>Kleiner fester Zufall nur für Testdaten (der Core nutzt Pcg32 – hier reicht ein LCG).</summary>
    internal sealed class TinyRandom
    {
        private ulong _s;
        public TinyRandom(ulong seed) { _s = seed * 2862933555777941757UL + 3037000493UL; }
        public double Next()
        {
            _s = _s * 6364136223846793005UL + 1442695040888963407UL;
            return (_s >> 11) * (1.0 / 9007199254740992.0);
        }
    }
}
