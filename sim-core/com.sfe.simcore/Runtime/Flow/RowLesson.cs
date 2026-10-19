using System;

namespace SFE.SimCore.Flow
{
    /// <summary>Ein Schritt der Rechentafel, je Kästchen (Index = Kästchen von links).</summary>
    public sealed class RowLessonStep
    {
        /// <summary>Luft vorher, kommt an, zieht weiter, Luft neu: <c>neu = vorher + kommt an − zieht weiter</c>.</summary>
        public double[] Before, ComeIn, MoveOn, After;
        /// <summary>Was nach rechts / links will und was in der Spalte bleibt – vor und nach dem Ausgleichen.</summary>
        public double[] RightBefore, LeftBefore, StayBefore, RightEven, LeftEven, StayEven;
    }

    /// <summary>
    /// Rechentafel in MG2 Schritt 2: eine Reihe Kästchen, links und rechts eine Wand. Dasselbe Modell wie der Kanal
    /// (<see cref="LbmD2Q9"/> mit geschlossenen Enden, eine Zeile hoch). Je Schritt erst Ausgleichen, dann Weiterschieben.
    /// Ausgleichen ändert die Luft im Kästchen nicht, nur ihre Richtung – darum geht keine Luft verloren.
    /// Identisch zu <c>simref.flow.RowLesson</c>.
    /// </summary>
    public sealed class RowLesson
    {
        private readonly LbmD2Q9 _lbm;
        private readonly double[] _post;

        public int Cells => _lbm.Nx;
        public LbmD2Q9 Lbm => _lbm;
        public int StepCount => _lbm.Iteration;

        public RowLesson(double[] air, double[] push, double tau)
        {
            if (air.Length != push.Length) throw new ArgumentException("Luft und Schwung brauchen gleich viele Kästchen.");
            _lbm = new LbmD2Q9(air.Length, 1, tau);
            for (int x = 0; x < air.Length; x++) _lbm.SetEquilibrium(x, air[x], push[x], 0.0);
            _post = new double[9 * air.Length];
        }

        public RowLesson(FlowLessonData d) : this(d.Air, d.Push, d.Tau) { }

        /// <summary>Luft je Kästchen jetzt.</summary>
        public double[] Air
        {
            get
            {
                var a = new double[Cells];
                for (int x = 0; x < Cells; x++) { LbmD2Q9.Moments(_lbm.F, Cells, x, out a[x], out _, out _); }
                return a;
            }
        }

        public static void Aggregates(double[] f, int n, int x, out double right, out double left, out double stay)
        {
            right = f[n + x] + f[5 * n + x] + f[8 * n + x];
            left = f[3 * n + x] + f[6 * n + x] + f[7 * n + x];
            stay = f[x] + f[2 * n + x] + f[4 * n + x];
        }

        public RowLessonStep Step()
        {
            int n = Cells;
            var s = new RowLessonStep
            {
                Before = Air, ComeIn = new double[n], MoveOn = new double[n],
                RightBefore = new double[n], LeftBefore = new double[n], StayBefore = new double[n],
                RightEven = new double[n], LeftEven = new double[n], StayEven = new double[n],
            };
            for (int x = 0; x < n; x++) Aggregates(_lbm.F, n, x, out s.RightBefore[x], out s.LeftBefore[x], out s.StayBefore[x]);
            _lbm.Collide(_lbm.F, _post);
            for (int x = 0; x < n; x++) Aggregates(_post, n, x, out s.RightEven[x], out s.LeftEven[x], out s.StayEven[x]);
            for (int x = 0; x < n; x++)
            {
                if (x < n - 1) { s.MoveOn[x] += s.RightEven[x]; s.ComeIn[x + 1] += s.RightEven[x]; }
                if (x > 0) { s.MoveOn[x] += s.LeftEven[x]; s.ComeIn[x - 1] += s.LeftEven[x]; }
            }
            _lbm.Step();
            s.After = Air;
            return s;
        }

        /// <summary>
        /// Gerundete Tafelzeile, die aufgeht: vorher und neu werden normal gerundet, „kommt an“ und „zieht weiter“
        /// so, dass <c>vorher + kommt an − zieht weiter = neu</c> auch in der Anzeige stimmt. Jeder angezeigte Wert
        /// weicht höchstens um eine Stelle (<paramref name="unit"/>) vom echten ab.
        /// </summary>
        /// <param name="unit">Kleinste angezeigte Stelle, z. B. 0,1.</param>
        public static void RoundRow(double before, double comeIn, double moveOn, double after, double unit,
            out double b, out double c, out double m, out double a)
        {
            b = Math.Round(before / unit) * unit;
            a = Math.Round(after / unit) * unit;
            c = Math.Round(comeIn / unit) * unit;
            m = Math.Round(moveOn / unit) * unit;
            long missing = (long)Math.Round((a - b - c + m) / unit);
            if (missing == 0) return;
            // Den Teil anpassen, dessen Rundung am wenigsten sicher war (größter Rest)
            double restC = Math.Abs(comeIn / unit - Math.Round(comeIn / unit));
            double restM = Math.Abs(moveOn / unit - Math.Round(moveOn / unit));
            if (restC >= restM) c += missing * unit;
            else m -= missing * unit;
        }
    }
}
