namespace SFE.SimCore.Process
{
    public enum HopMode { Direct, Belt, Robot }

    /// <summary>Weg zwischen zwei Stationen: Übergabe, Förderband oder Robo-Robbe; <see cref="Time"/> = eine Richtung.</summary>
    public sealed class Hop
    {
        public HopMode Mode;
        public double Time;
        public int Distance;

        public Hop(HopMode mode, double time, int distance)
        {
            Mode = mode;
            Time = time;
            Distance = distance;
        }
    }

    public sealed class StageConfig
    {
        public int Machines;
        public double Low, Mode, High;
        public double Mtbf, Mttr;
        /// <summary>Plätze in der Warteschlange davor; −1 = unbegrenzt. Was nicht hineinpasst, verdirbt.</summary>
        public int Capacity;

        public StageConfig Clone() => (StageConfig)MemberwiseClone();
    }

    /// <summary>
    /// Die Fabrik als Zahlen – das, was die Simulation braucht. Entsteht aus dem Raster (<see cref="FactoryLayout.ToConfig"/>)
    /// oder direkt (Rechentafel, <see cref="StationLesson"/>).
    /// </summary>
    public sealed class FactoryConfig
    {
        public double ShiftLength;
        public double KutterMeanGap;
        public int KutterBoxes;
        public double KutterLast;
        public double TrainFirst;
        /// <summary>0 = kein Zug.</summary>
        public double TrainEvery;
        public int TrainBoxes;
        public double GullChance;
        public StageConfig[] Stages;
        /// <summary>Stages.Length + 1 Wege: in Stufe 0, …, aus der letzten Stufe zur Verladung.</summary>
        public Hop[] Hops;
        public int Robots;
        /// <summary>Zusatzzeit je Fahrt, weil sich Robo-Robben im Weg stehen.</summary>
        public double RobotDelay;

        public FactoryConfig Clone()
        {
            var c = (FactoryConfig)MemberwiseClone();
            c.Stages = new StageConfig[Stages.Length];
            for (int i = 0; i < Stages.Length; i++) c.Stages[i] = Stages[i].Clone();
            c.Hops = (Hop[])Hops.Clone();
            return c;
        }
    }
}
