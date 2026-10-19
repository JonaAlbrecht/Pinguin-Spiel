namespace SFE.SimCore.Process
{
    /// <summary>Eine Zeile der Rechentafel: Kisten vorher + kommen an − werden fertig = Kisten neu.</summary>
    public sealed class LessonRow
    {
        public double Time;
        public bool IsArrival;
        public int Before, Arrived, Finished, After;
        /// <summary>Davon in der Warteschlange (der Rest ist in Arbeit).</summary>
        public int Waiting;
    }

    /// <summary>
    /// Rechentafel in MG3 Schritt 2: eine Filetier-Station mit Warteschlange, Ereignis für Ereignis.
    /// Kein eigenes Modell – dieselbe <see cref="FactorySimulation"/> wie die ganze Fabrik, nur mit einer Station,
    /// einzeln ankommenden Kisten, ohne Zug, Möwen, Störungen und Wege (<c>simref/process_lesson.py</c>, Golden <c>lesson</c>).
    /// </summary>
    public sealed class StationLesson
    {
        private readonly StationLessonData _data;
        private FactorySimulation _sim;

        public StationLesson(StationLessonData data)
        {
            _data = data;
            Reset();
        }

        public int Events => _data.Events;
        public int Step { get; private set; }
        public double Time => _sim.Time;
        /// <summary>Kisten an der Station (wartend + in Arbeit).</summary>
        public int InStation => _sim.InStation(0);
        public int Waiting => _sim.Waiting(0);
        public bool Working => _sim.State(0, 0) == MachineState.Busy;
        public FactorySimulation Simulation => _sim;

        public static FactoryConfig Config(StationLessonData d)
        {
            const double endless = 1e9;
            return new FactoryConfig
            {
                ShiftLength = endless,
                KutterMeanGap = d.ArrivalMean,
                KutterBoxes = 1,
                KutterLast = endless,
                TrainFirst = 0,
                TrainEvery = 0,
                TrainBoxes = 0,
                GullChance = 0,
                Stages = new[] { new StageConfig { Machines = 1, Low = d.Low, Mode = d.Mode, High = d.High, Capacity = -1 } },
                Hops = new[] { new Hop(HopMode.Direct, 0, 0), new Hop(HopMode.Direct, 0, 0) },
                Robots = 0,
                RobotDelay = 0,
            };
        }

        public void Reset()
        {
            _sim = new FactorySimulation(Config(_data), _data.Seed);
            Step = 0;
        }

        /// <summary>Nächstes Ereignis: Eine Kiste kommt an oder die Station wird mit einer fertig.</summary>
        public LessonRow Next()
        {
            int before = _sim.InStation(0);
            _sim.Step(out var ev);
            Step++;
            int after = _sim.InStation(0);
            bool arrival = ev.Kind == EventKind.Kutter;
            return new LessonRow
            {
                Time = ev.Time,
                IsArrival = arrival,
                Before = before,
                Arrived = arrival ? 1 : 0,
                Finished = ev.Kind == EventKind.Done ? 1 : 0,
                After = after,
                Waiting = _sim.Waiting(0),
            };
        }
    }
}
