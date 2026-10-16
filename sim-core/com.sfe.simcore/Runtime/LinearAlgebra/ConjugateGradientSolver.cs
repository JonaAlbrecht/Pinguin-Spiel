using System;

namespace SFE.SimCore.LinearAlgebra
{
    /// <summary>
    /// Verfahren der konjugierten Gradienten für symmetrisch positiv definite Matrizen.
    /// Schrittweise: jede Iteration ist ein Aufruf von <see cref="Step"/>.
    /// </summary>
    public sealed class ConjugateGradientSolver : IStepSolver
    {
        private readonly CsrMatrix _a;
        private readonly double[] _x;
        private readonly double[] _r;
        private readonly double[] _p;
        private readonly double[] _ap;
        private readonly double _normB;
        private readonly double _tolerance;
        private readonly int _maxIterations;
        private double _rr;

        public double[] Solution => _x;
        public int Iteration { get; private set; }
        public double Residual { get; private set; }
        public bool IsConverged => Residual <= _tolerance;
        public bool HasStalled => !IsConverged && Iteration >= _maxIterations;

        public ConjugateGradientSolver(CsrMatrix a, double[] b, double[] x0 = null, double tolerance = 1e-10,
            int maxIterations = 0)
        {
            if (b.Length != a.Size) throw new ArgumentException("b passt nicht zur Matrix.");
            _a = a;
            _tolerance = tolerance;
            _maxIterations = maxIterations > 0 ? maxIterations : 10 * a.Size;
            _x = new double[a.Size];
            if (x0 != null) Array.Copy(x0, _x, a.Size);
            _r = new double[a.Size];
            _p = new double[a.Size];
            _ap = new double[a.Size];

            _a.Multiply(_x, _ap);
            for (int i = 0; i < a.Size; i++)
            {
                _r[i] = b[i] - _ap[i];
                _p[i] = _r[i];
            }
            _rr = Dot(_r, _r);
            _normB = Math.Sqrt(Dot(b, b));
            Residual = RelativeResidual();
        }

        public void Step(int maxIterations = 1)
        {
            for (int n = 0; n < maxIterations && !IsConverged && Iteration < _maxIterations; n++)
            {
                _a.Multiply(_p, _ap);
                double pAp = Dot(_p, _ap);
                if (pAp <= 0.0) throw new InvalidOperationException("Matrix ist nicht positiv definit.");
                double alpha = _rr / pAp;
                for (int i = 0; i < _x.Length; i++)
                {
                    _x[i] += alpha * _p[i];
                    _r[i] -= alpha * _ap[i];
                }
                double rrNew = Dot(_r, _r);
                double beta = rrNew / _rr;
                for (int i = 0; i < _p.Length; i++) _p[i] = _r[i] + beta * _p[i];
                _rr = rrNew;
                Iteration++;
                Residual = RelativeResidual();
            }
        }

        /// <summary>Rechnet bis zur Konvergenz und gibt die Lösung zurück.</summary>
        public double[] SolveToEnd()
        {
            while (!IsConverged && !HasStalled) Step(64);
            if (!IsConverged) throw new InvalidOperationException("CG ist nicht konvergiert.");
            return _x;
        }

        private double RelativeResidual() => _normB > 0.0 ? Math.Sqrt(_rr) / _normB : Math.Sqrt(_rr);

        private static double Dot(double[] a, double[] b)
        {
            double s = 0.0;
            for (int i = 0; i < a.Length; i++) s += a[i] * b[i];
            return s;
        }
    }
}
