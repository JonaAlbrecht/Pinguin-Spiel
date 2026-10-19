using System;

namespace SFE.SimCore.Flow
{
    /// <summary>Ergebnis einer Runde: Beiwert, Plätze, Fahrzeit, Sterne.</summary>
    public sealed class TrainResult
    {
        public double Drag;
        public int Seats;
        public double Speed;
        public double Time;
        public int Stars;
        public int OpenTransitions;
    }

    /// <summary>
    /// Ein Zug im Windkanal von ZWILLI: Maske aus der Form, Strömung schrittweise (<see cref="IStepSolver"/>),
    /// am Ende der gemittelte Widerstandsbeiwert. Wie <c>simulate</c> in <c>simref/flow_train.py</c>.
    /// </summary>
    public sealed class TrainRun : IStepSolver
    {
        public PolarExpressLevel Level { get; }
        public FlowResolution Resolution { get; }
        public TrainShape Shape { get; }
        public LbmD2Q9 Lbm { get; }
        public int TotalSteps { get; }
        public int AverageSteps { get; }
        private double _sum;

        public TrainRun(TrainShape shape, PolarExpressLevel level, FlowResolution resolution)
        {
            Level = level;
            Resolution = resolution;
            Shape = shape.Clone();
            TotalSteps = level.StepsFor(resolution);
            AverageSteps = level.AverageStepsFor(resolution);
            Lbm = new LbmD2Q9(resolution.Nx, resolution.Ny, level.Tau(resolution), TrainGeometry.Mask(Shape, level, resolution),
                true, level.U, level.RampFor(resolution)) { TargetSteps = TotalSteps };
        }

        public bool IsConverged => Lbm.Iteration >= TotalSteps;
        public int Iteration => Lbm.Iteration;
        public double Residual => Lbm.Residual;
        public double Progress => Math.Min(1.0, (double)Lbm.Iteration / TotalSteps);
        /// <summary>Kästchen-Schritte bis zum Ende (für „Wie lange rechnet's?“).</summary>
        public long Work => (long)Resolution.Nx * Resolution.Ny * TotalSteps;

        public void Step(int maxIterations = 1)
        {
            for (int i = 0; i < maxIterations && !IsConverged; i++)
            {
                Lbm.Step();
                if (Lbm.Iteration > TotalSteps - AverageSteps) _sum += Lbm.ForceX;
            }
        }

        public void RunToEnd() => Step(TotalSteps);

        /// <summary>Widerstandsbeiwert = mittlere Kraft / (½ u² · Zughöhe in Kästchen); erst am Ende gültig.</summary>
        public double Drag => _sum / AverageSteps / (0.5 * Level.U * Level.U * Level.Height * Resolution.Scale);

        public TrainResult Result()
        {
            if (!IsConverged) throw new InvalidOperationException("Die Rechnung ist noch nicht fertig.");
            return Travel.Evaluate(Drag, Shape, Level);
        }
    }

    /// <summary>
    /// Fahrzeit zur Küste: konstante Zugkraft gegen Rollwiderstand und Luftwiderstand C·(v + w)², w = Gegenwind.
    /// <c>v = sqrt((F − R) / C) − w</c>, mindestens <c>MinSpeed</c>; Fahrzeit = Strecke / v.
    /// </summary>
    public static class Travel
    {
        public static double Speed(double drag, PolarExpressLevel lv)
        {
            double c = lv.DragScale * drag;
            double v = Math.Sqrt(Math.Max(lv.Traction - lv.Rolling, 0.0) / c) - lv.Headwind;
            return Math.Max(v, lv.MinSpeed);
        }

        public static double Time(double drag, PolarExpressLevel lv) => lv.Distance / Speed(drag, lv);

        /// <summary>★ Küste in der Ziel-Fahrzeit, ★★ dazu Plätze für alle, ★★★ dazu Bestzeit-Bereich.</summary>
        public static int Stars(double time, int seats, PolarExpressLevel lv)
        {
            if (time > lv.TargetTime) return 0;
            if (seats < lv.Passengers) return 1;
            return time <= lv.BestTime ? 3 : 2;
        }

        public static TrainResult Evaluate(double drag, TrainShape shape, PolarExpressLevel lv)
        {
            int seats = TrainGeometry.Seats(shape, lv);
            double time = Time(drag, lv);
            int open = 0;
            foreach (bool c in shape.Covered) if (!c) open++;
            return new TrainResult
            {
                Drag = drag, Seats = seats, Speed = Speed(drag, lv), Time = time, Stars = Stars(time, seats, lv), OpenTransitions = open,
            };
        }

        /// <summary>Ist <paramref name="now"/> besser als <paramref name="best"/>? Mehr Sterne, sonst kürzere Fahrzeit.</summary>
        public static bool IsBetter(TrainResult now, TrainResult best) =>
            best == null || now.Stars > best.Stars || (now.Stars == best.Stars && now.Time < best.Time);
    }
}
