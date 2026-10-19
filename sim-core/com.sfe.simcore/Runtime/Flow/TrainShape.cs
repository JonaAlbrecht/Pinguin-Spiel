using System;
using System.Collections.Generic;
using SFE.SimCore.Data;

namespace SFE.SimCore.Flow
{
    /// <summary>
    /// Form des Polar-Express: vier Ziehpunkte der Nase (Fein-Einheiten) und welche Wagenübergänge verkleidet sind.
    /// P0 liegt auf der Stirn (nur Höhe frei), P3 auf dem Dach (nur Länge frei), P1 und P2 frei; x aufsteigend.
    /// Geometrie identisch zu <c>simref/flow_train.py</c>.
    /// </summary>
    public sealed class TrainShape
    {
        public readonly double[] X = new double[4];
        public readonly double[] Y = new double[4];
        public bool[] Covered;

        public static TrainShape FromJson(Dictionary<string, object> o)
        {
            var s = new TrainShape();
            var nose = o.Arr("nose");
            for (int i = 0; i < 4; i++)
            {
                var p = MiniJson.NumArray((List<object>)nose[i]);
                s.X[i] = p[0];
                s.Y[i] = p[1];
            }
            var cov = o.Arr("covered");
            s.Covered = new bool[cov.Count];
            for (int i = 0; i < cov.Count; i++) s.Covered[i] = (bool)cov[i];
            return s;
        }

        public TrainShape Clone()
        {
            var s = new TrainShape { Covered = (bool[])Covered.Clone() };
            Array.Copy(X, s.X, 4);
            Array.Copy(Y, s.Y, 4);
            return s;
        }

        public bool SameAs(TrainShape o)
        {
            for (int i = 0; i < 4; i++) if (X[i] != o.X[i] || Y[i] != o.Y[i]) return false;
            if (Covered.Length != o.Covered.Length) return false;
            for (int i = 0; i < Covered.Length; i++) if (Covered[i] != o.Covered[i]) return false;
            return true;
        }

        private static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;

        /// <summary>Ziehpunkte in den erlaubten Bereich (wie <c>clamp_shape</c> in Python).</summary>
        public TrainShape Clamped(PolarExpressLevel lv)
        {
            var s = Clone();
            double x0 = lv.Start, h = lv.Height;
            double p3x = Clamp(X[3], x0 + 1.0, x0 + lv.NoseMax);
            double p1x = Clamp(X[1], x0, p3x);
            double p2x = Clamp(X[2], p1x, p3x);
            s.X[0] = x0; s.Y[0] = Clamp(Y[0], lv.TipMin, h);
            s.X[1] = p1x; s.Y[1] = Clamp(Y[1], 0.0, h);
            s.X[2] = p2x; s.Y[2] = Clamp(Y[2], 0.0, h);
            s.X[3] = p3x; s.Y[3] = h;
            return s;
        }

        /// <summary>Kubische Bézier-Kurve der Nase an der Stelle t (0…1).</summary>
        public void Bezier(double t, out double x, out double y)
        {
            double u = 1.0 - t;
            double a = u * u * u, b = 3.0 * u * u * t, c = 3.0 * u * t * t, d = t * t * t;
            x = a * X[0] + b * X[1] + c * X[2] + d * X[3];
            y = a * Y[0] + b * Y[1] + c * Y[2] + d * Y[3];
        }

        /// <summary>Höhe der Dachlinie an der Stelle x (P0.x ≤ x ≤ P3.x) – 60 Halbierungen.</summary>
        public double NoseHeight(double x)
        {
            double lo = 0.0, hi = 1.0;
            for (int i = 0; i < 60; i++)
            {
                double mid = 0.5 * (lo + hi);
                Bezier(mid, out double bx, out _);
                if (bx < x) lo = mid;
                else hi = mid;
            }
            Bezier(0.5 * (lo + hi), out _, out double y);
            return y;
        }
    }

    /// <summary>Zug im Kanal: Höhe je Stelle, Hindernis-Maske je Auflösung, Plätze.</summary>
    public static class TrainGeometry
    {
        /// <summary>Index des Wagenübergangs an der Stelle x, sonst −1.</summary>
        public static int TransitionAt(PolarExpressLevel lv, double x)
        {
            for (int i = 0; i < lv.Transitions.Length; i++)
                if (lv.Start + lv.Transitions[i] <= x && x < lv.Start + lv.Transitions[i] + lv.TransitionWidth) return i;
            return -1;
        }

        /// <summary>Höhe des Zuges an der Stelle x (0 = kein Zug). Heck = Spiegelbild der Nase.</summary>
        public static double Profile(TrainShape s, PolarExpressLevel lv, double x)
        {
            double x0 = lv.Start, x1 = lv.Start + lv.Length;
            if (x < x0 || x >= x1) return 0.0;
            int t = TransitionAt(lv, x);
            if (t >= 0) return s.Covered[t] ? lv.Height : lv.TransitionHeight;
            double noseEnd = s.X[3];
            if (x < noseEnd) return s.NoseHeight(x);
            double mirrored = x0 + (x1 - x);
            if (mirrored < noseEnd) return s.NoseHeight(mirrored);
            return lv.Height;
        }

        /// <summary>Maske je Kästchen (Index y · Nx + x, Zeile 0 unten): Mitte unter der Dachlinie → Zug.</summary>
        public static bool[] Mask(TrainShape s, PolarExpressLevel lv, FlowResolution r)
        {
            var m = new bool[r.Nx * r.Ny];
            for (int i = 0; i < r.Nx; i++)
            {
                double h = Profile(s, lv, (i + 0.5) / r.Scale);
                for (int j = 0; j < r.Ny; j++)
                    if ((j + 0.5) / r.Scale < h) m[j * r.Nx + i] = true;
            }
            return m;
        }

        /// <summary>Maske als Text, oberste Zeile zuerst ('#' = Zug).</summary>
        public static string[] MaskRows(bool[] mask, int nx, int ny)
        {
            var rows = new string[ny];
            var sb = new System.Text.StringBuilder(nx);
            for (int j = 0; j < ny; j++)
            {
                sb.Clear();
                for (int i = 0; i < nx; i++) sb.Append(mask[j * nx + i] ? '#' : '.');
                rows[ny - 1 - j] = sb.ToString();
            }
            return rows;
        }

        /// <summary>Plätze: Spalten mit genug Kopfhöhe (ohne Übergänge), je Sitzreihe <c>ColumnsPerRow</c> Spalten.</summary>
        public static int Seats(TrainShape s, PolarExpressLevel lv)
        {
            int cols = 0;
            int from = (int)Math.Round(lv.Start, MidpointRounding.ToEven), to = (int)Math.Round(lv.Start + lv.Length, MidpointRounding.ToEven);
            for (int i = from; i < to; i++)
            {
                double x = i + 0.5;
                if (TransitionAt(lv, x) < 0 && Profile(s, lv, x) >= lv.Headroom * lv.Height) cols++;
            }
            return cols / lv.ColumnsPerRow * lv.SeatsPerRow;
        }
    }
}
