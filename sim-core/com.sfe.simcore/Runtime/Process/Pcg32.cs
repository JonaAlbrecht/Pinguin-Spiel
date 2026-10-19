using System;

namespace SFE.SimCore.Process
{
    /// <summary>
    /// Zufallszahlen für MG3 „Fischfabrik“: PCG32 (O'Neill, pcg32_random_r, XSH RR) – bitgenau wie
    /// <c>sim-reference/simref/process_random.py</c>. Nie <c>System.Random</c>: Python und C# müssen dieselbe Folge
    /// ziehen, sonst laufen die Golden Files auseinander (docs/04 4.1).
    /// Jeder Zweck hat einen eigenen Strom (<see cref="StreamKutter"/> …) – ändert der Spieler die Maschinen,
    /// kommen die Kutter trotzdem zu denselben Zeiten („gleicher Tag, anderer Plan“).
    /// </summary>
    public sealed class Pcg32
    {
        public const ulong StreamKutter = 1;
        public const ulong StreamGulls = 2;
        /// <summary>+ Stufe</summary>
        public const ulong StreamService = 10;
        /// <summary>+ 10 · Stufe + Maschine</summary>
        public const ulong StreamFailures = 100;

        private const ulong Multiplier = 6364136223846793005UL;
        private ulong _state;
        private readonly ulong _inc;

        public Pcg32(ulong seed, ulong stream)
        {
            _state = 0;
            _inc = (stream << 1) | 1UL;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + _inc);
            uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }

        /// <summary>Gleichverteilt in [0, 1) mit 32 Bit Auflösung.</summary>
        public double NextDouble() => NextUInt() / 4294967296.0;

        /// <summary>Wartezeit bis zum nächsten zufälligen Ereignis (Kutter, Störung, Reparatur).</summary>
        public double Exponential(double mean) => -mean * Math.Log(1.0 - NextDouble());

        /// <summary>Arbeitszeit einer Station: meist <paramref name="mode"/>, nie unter <paramref name="low"/>.</summary>
        public double Triangular(double low, double mode, double high)
        {
            double u = NextDouble();
            double f = (mode - low) / (high - low);
            if (u < f) return low + Math.Sqrt(u * (high - low) * (mode - low));
            return high - Math.Sqrt((1.0 - u) * (high - low) * (high - mode));
        }
    }
}
