using System;

namespace SFE.SimCore.Flow
{
    /// <summary>
    /// Lattice-Boltzmann D2Q9 mit BGK-Stoß (MG2 „Polar-Express“, docs/03 und docs/04 Abschnitt 4.3).
    /// Jedes Kästchen trägt neun Anteile Luft, einen je Richtung. Ein Schritt:
    /// <list type="number">
    /// <item><b>Ausgleichen</b> (Stoß): <c>f* = f − (f − f_eq) / tau</c> – Luftmenge und Schwung des Kästchens bleiben.</item>
    /// <item><b>Weiterschieben</b> (Strömen): jeder Anteil wandert ein Kästchen in seine Richtung. Am Zug prallt er zurück
    /// (Bounce-Back, halber Abstand), oben und unten spiegelt der glatte Kanalrand, links strömt Luft ein (sanft
    /// hochfahrend), rechts strömt sie mit festem Druck aus. Ein geschlossener Kasten (Rechentafel) hat Wände.</item>
    /// </list>
    /// Luftwiderstand per Impulsaustausch an den zurückprallenden Anteilen. Die Streaming-Regeln stehen als
    /// Quelltabelle – identisch zu <c>sim-reference/simref/flow.py</c>, auch in der Rechenreihenfolge je Kästchen.
    /// Keine Allokation in <see cref="Step"/> (docs/04 Abschnitt 4.1).
    /// </summary>
    public sealed class LbmD2Q9 : IStepSolver
    {
        // 0 ruhend, 1 rechts, 2 oben, 3 links, 4 unten, 5 rechts oben, 6 links oben, 7 links unten, 8 rechts unten
        public static readonly int[] Cx = { 0, 1, 0, -1, 0, 1, -1, -1, 1 };
        public static readonly int[] Cy = { 0, 0, 1, 0, -1, 1, 1, -1, -1 };
        public static readonly double[] W =
        {
            4.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0,
        };
        public static readonly int[] Opp = { 0, 3, 4, 1, 2, 7, 8, 5, 6 };
        public static readonly int[] MirrorY = { 0, 1, 4, 3, 2, 8, 7, 6, 5 };

        public int Nx { get; }
        public int Ny { get; }
        public int CellCount { get; }
        public double Tau { get; }
        public bool OpenEnds { get; }
        public double InflowSpeed { get; }
        public int Ramp { get; }
        public bool[] Solid { get; }

        /// <summary>Anteile, Index <c>k · CellCount + Zelle</c>, Zelle = y · Nx + x (Zeile 0 unten).</summary>
        public double[] F { get; private set; }
        private double[] _post;
        private readonly int[] _src;
        private readonly int[] _links;
        private readonly double[] _linkCx, _linkCy;
        private readonly int[] _inlet, _outlet;
        private readonly double[] _feq = new double[9];

        public double ForceX { get; private set; }
        public double ForceY { get; private set; }
        public int Iteration { get; private set; }
        /// <summary>Nach so vielen Schritten gilt der Lauf als fertig (<see cref="IsConverged"/>).</summary>
        public int TargetSteps = int.MaxValue;
        public bool IsConverged => Iteration >= TargetSteps;
        /// <summary>Änderung der Widerstandskraft im letzten Schritt (für „Blick unter die Haube“).</summary>
        public double Residual { get; private set; }

        /// <param name="openEnds">true: Kanal mit Einströmen links und Ausströmen rechts; false: Kasten mit Wänden.</param>
        public LbmD2Q9(int nx, int ny, double tau, bool[] solid = null, bool openEnds = false, double inflowSpeed = 0.0, int ramp = 0)
        {
            if (nx < 1 || ny < 1) throw new ArgumentException("Kanal braucht mindestens ein Kästchen.");
            if (tau <= 0.5) throw new ArgumentException("tau muss größer als 0,5 sein (sonst wird die Rechnung instabil).");
            Nx = nx;
            Ny = ny;
            CellCount = nx * ny;
            Tau = tau;
            OpenEnds = openEnds;
            InflowSpeed = inflowSpeed;
            Ramp = ramp;
            Solid = solid != null ? (bool[])solid.Clone() : new bool[CellCount];
            if (Solid.Length != CellCount) throw new ArgumentException("Maske passt nicht zur Kanalgröße.");

            int n = CellCount;
            _src = new int[9 * n];
            var linksK = new System.Collections.Generic.List<int>();
            var linksCell = new System.Collections.Generic.List<int>();
            for (int k = 0; k < 9; k++)
            {
                for (int y = 0; y < ny; y++)
                {
                    for (int x = 0; x < nx; x++)
                    {
                        int cell = y * nx + x;
                        int target = k * n + cell;
                        if (Solid[cell]) { _src[target] = target; continue; }
                        int sx = x - Cx[k], sy = y - Cy[k];
                        if (sx < 0 || sx >= nx)
                        {
                            // offener Kanal: Platzhalter, Ein-/Ausströmen überschreibt die Spalte; Kasten: Wand
                            _src[target] = openEnds ? target : Opp[k] * n + cell;
                        }
                        else if (sy < 0 || sy >= ny)
                        {
                            int scell = y * nx + sx;
                            if (Solid[scell])
                            {
                                _src[target] = Opp[k] * n + cell;
                                linksK.Add(Opp[k]);
                                linksCell.Add(cell);
                            }
                            else _src[target] = MirrorY[k] * n + scell;
                        }
                        else if (Solid[sy * nx + sx])
                        {
                            _src[target] = Opp[k] * n + cell;
                            linksK.Add(Opp[k]);
                            linksCell.Add(cell);
                        }
                        else _src[target] = k * n + sy * nx + sx;
                    }
                }
            }
            _links = new int[linksK.Count];
            _linkCx = new double[linksK.Count];
            _linkCy = new double[linksK.Count];
            for (int i = 0; i < _links.Length; i++)
            {
                _links[i] = linksK[i] * n + linksCell[i];
                _linkCx[i] = 2.0 * Cx[linksK[i]];
                _linkCy[i] = 2.0 * Cy[linksK[i]];
            }

            var inlet = new System.Collections.Generic.List<int>();
            var outlet = new System.Collections.Generic.List<int>();
            for (int y = 0; y < ny; y++)
            {
                if (!Solid[y * nx]) inlet.Add(y * nx);
                if (!Solid[y * nx + nx - 1]) outlet.Add(y * nx + nx - 1);
            }
            _inlet = inlet.ToArray();
            _outlet = outlet.ToArray();

            F = new double[9 * n];
            _post = new double[9 * n];
            Equilibrium(1.0, 0.0, 0.0, _feq);
            for (int k = 0; k < 9; k++)
                for (int c = 0; c < n; c++)
                    F[k * n + c] = _feq[k];
        }

        /// <summary>Gleichgewicht <c>f_eq = w rho (1 + 3 c·u + 4,5 (c·u)² − 1,5 u²)</c>.</summary>
        public static void Equilibrium(double rho, double ux, double uy, double[] feq)
        {
            double usq = ux * ux + uy * uy;
            for (int k = 0; k < 9; k++)
            {
                double cu = Cx[k] * ux + Cy[k] * uy;
                feq[k] = W[k] * rho * (1.0 + 3.0 * cu + 4.5 * cu * cu - 1.5 * usq);
            }
        }

        /// <summary>Setzt ein Kästchen ins Gleichgewicht zu Luftmenge und Geschwindigkeit (Startzustand der Rechentafel).</summary>
        public void SetEquilibrium(int cell, double rho, double ux, double uy)
        {
            Equilibrium(rho, ux, uy, _feq);
            for (int k = 0; k < 9; k++) F[k * CellCount + cell] = _feq[k];
        }

        /// <summary>Luftmenge und Geschwindigkeit eines Kästchens aus beliebigen Anteilen (feste Summenreihenfolge).</summary>
        public static void Moments(double[] f, int n, int c, out double rho, out double ux, out double uy)
        {
            double f0 = f[c], f1 = f[n + c], f2 = f[2 * n + c], f3 = f[3 * n + c], f4 = f[4 * n + c];
            double f5 = f[5 * n + c], f6 = f[6 * n + c], f7 = f[7 * n + c], f8 = f[8 * n + c];
            rho = f0 + f1 + f2 + f3 + f4 + f5 + f6 + f7 + f8;
            double jx = f1 - f3 + f5 - f6 - f7 + f8;
            double jy = f2 - f4 + f5 + f6 - f7 - f8;
            ux = jx / rho;
            uy = jy / rho;
        }

        /// <summary>Luftmenge und Geschwindigkeit eines Kästchens; im Zug 1 und 0.</summary>
        public void Moments(int cell, out double rho, out double ux, out double uy)
        {
            if (Solid[cell]) { rho = 1.0; ux = 0.0; uy = 0.0; return; }
            Moments(F, CellCount, cell, out rho, out ux, out uy);
        }

        /// <summary>Ausgleichen (Stoß) aller Kästchen von <paramref name="f"/> nach <paramref name="post"/>.</summary>
        public void Collide(double[] f, double[] post)
        {
            int n = CellCount;
            for (int c = 0; c < n; c++)
            {
                if (Solid[c])
                {
                    for (int k = 0; k < 9; k++) post[k * n + c] = f[k * n + c];
                    continue;
                }
                // Ausgeschrieben statt Schleife über k (WebGL-Budget, docs/04 4.3). Bitgleich zur Schleife und zu Python:
                // c·u mit c aus {−1, 0, 1} ist exakt −ux, 0 + uy usw.
                double f0 = f[c], f1 = f[n + c], f2 = f[2 * n + c], f3 = f[3 * n + c], f4 = f[4 * n + c];
                double f5 = f[5 * n + c], f6 = f[6 * n + c], f7 = f[7 * n + c], f8 = f[8 * n + c];
                double rho = f0 + f1 + f2 + f3 + f4 + f5 + f6 + f7 + f8;
                double ux = (f1 - f3 + f5 - f6 - f7 + f8) / rho;
                double uy = (f2 - f4 + f5 + f6 - f7 - f8) / rho;
                double u15 = 1.5 * (ux * ux + uy * uy);
                double r0 = W0 * rho, r1 = W1 * rho, r5 = W5 * rho;
                post[c] = f0 - (f0 - r0 * (1.0 - u15)) / Tau;
                post[n + c] = f1 - (f1 - r1 * Poly(ux, u15)) / Tau;
                post[2 * n + c] = f2 - (f2 - r1 * Poly(uy, u15)) / Tau;
                post[3 * n + c] = f3 - (f3 - r1 * Poly(-ux, u15)) / Tau;
                post[4 * n + c] = f4 - (f4 - r1 * Poly(-uy, u15)) / Tau;
                post[5 * n + c] = f5 - (f5 - r5 * Poly(ux + uy, u15)) / Tau;
                post[6 * n + c] = f6 - (f6 - r5 * Poly(-ux + uy, u15)) / Tau;
                post[7 * n + c] = f7 - (f7 - r5 * Poly(-ux - uy, u15)) / Tau;
                post[8 * n + c] = f8 - (f8 - r5 * Poly(ux - uy, u15)) / Tau;
            }
        }

        private const double W0 = 4.0 / 9.0, W1 = 1.0 / 9.0, W5 = 1.0 / 36.0;

        /// <summary>Klammer des Gleichgewichts <c>1 + 3 cu + 4,5 cu² − 1,5 u²</c> (u15 = 1,5 u²), Reihenfolge wie Python.</summary>
        private static double Poly(double cu, double u15) => 1.0 + 3.0 * cu + 4.5 * cu * cu - u15;

        /// <summary>Einströmgeschwindigkeit im Schritt <paramref name="step"/>: halbe Kosinuswelle bis <see cref="Ramp"/>.</summary>
        public static double InflowAt(double uIn, int ramp, int step)
        {
            if (ramp <= 0 || step >= ramp) return uIn;
            return uIn * 0.5 * (1.0 - Math.Cos(Math.PI * step / ramp));
        }

        public void Step(int maxIterations = 1)
        {
            int n = CellCount;
            for (int it = 0; it < maxIterations; it++)
            {
                Collide(F, _post);
                double fx = 0.0, fy = 0.0;
                for (int i = 0; i < _links.Length; i++)
                {
                    double v = _post[_links[i]];
                    fx += _linkCx[i] * v;
                    fy += _linkCy[i] * v;
                }
                Residual = Math.Abs(fx - ForceX);
                ForceX = fx;
                ForceY = fy;

                var f = F;
                for (int i = 0; i < f.Length; i++) f[i] = _post[_src[i]];
                if (OpenEnds)
                {
                    Equilibrium(1.0, InflowAt(InflowSpeed, Ramp, Iteration + 1), 0.0, _feq);
                    foreach (int c in _inlet)
                        for (int k = 0; k < 9; k++) f[k * n + c] = _feq[k];
                    // Ausströmen mit festem Druck: Luftmenge 1, Geschwindigkeit der Nachbarspalte
                    foreach (int c in _outlet)
                    {
                        Moments(f, n, c - 1, out _, out double ox, out double oy);
                        Equilibrium(1.0, ox, oy, _feq);
                        for (int k = 0; k < 9; k++) f[k * n + c] = _feq[k];
                    }
                }
                Iteration++;
            }
        }

        /// <summary>Gesamte Luft im Kanal (außerhalb des Zuges) – im geschlossenen Kasten bleibt sie gleich.</summary>
        public double TotalAir()
        {
            double sum = 0.0;
            for (int c = 0; c < CellCount; c++)
            {
                if (Solid[c]) continue;
                Moments(F, CellCount, c, out double rho, out _, out _);
                sum += rho;
            }
            return sum;
        }
    }
}
