using System;
using System.Collections.Generic;

namespace SFE.SimCore.Process
{
    /// <summary>Ereignisarten – dieselben Zahlen wie in <c>simref/process.py</c>.</summary>
    public enum EventKind { Kutter = 0, Train = 1, Done = 2, Arrive = 3, RobotFree = 4, Fail = 5, Repair = 6 }

    /// <summary>
    /// Ein abgearbeitetes Ereignis. <see cref="A"/>/<see cref="B"/>: Kutter → Kisten; Zug → verladene Kisten;
    /// Fertig/Störung/Reparatur → Stufe, Maschine; Ankunft → Weg, Kiste.
    /// </summary>
    public struct SimEvent
    {
        public double Time;
        public EventKind Kind;
        public int A, B;
    }

    public enum MachineState { Idle, Busy, Down }

    /// <summary>Kennzahlen eines Tages (einer Schicht).</summary>
    public sealed class DayResult
    {
        /// <summary>Sushi-Kisten, die ein Zug pünktlich mitgenommen hat.</summary>
        public int Delivered;
        public int Finished, Arrived, Stolen, Spoiled;
        /// <summary>Mittlere Durchlaufzeit: Ankunft am Steg bis fertig verpackt.</summary>
        public double LeadTime;
        public double[] Utilization;
        public int[] MaxQueue, EndQueue;
        /// <summary>Am stärksten ausgelastete Station (der Engpass).</summary>
        public int Bottleneck;
        public int Events;
    }

    /// <summary>
    /// Ereignisdiskrete Simulation der Fischfabrik (docs/03 MG3, docs/04 4.3) – identisch zu
    /// <c>sim-reference/simref/process.py</c>: gleiche Ereignisse, gleiche Reihenfolge, gleiche Zufallsströme.
    /// Ereignisse liegen in einem Heap nach (Zeit, laufende Nummer): Gleichzeitiges kommt in Einfügereihenfolge dran.
    /// Schrittweise: <see cref="Step"/> arbeitet ein Ereignis ab, <see cref="RunUntil"/> alle bis zu einer Zeit –
    /// so kann die Unity-Schicht eine Schicht als Zeitraffer zeigen. Zeiten in Minuten ab Schichtbeginn.
    /// </summary>
    public sealed class FactorySimulation
    {
        private struct Entry
        {
            public double Time;
            public long Seq;
            public EventKind Kind;
            public int A, B, Version;
        }

        private sealed class Machine
        {
            public int Job = -1;
            public bool Down;
            public double Done, Remaining, BusySince;
            public int Version;
        }

        private readonly FactoryConfig _c;
        private readonly List<Entry> _heap = new List<Entry>();
        private long _seq;
        private readonly Pcg32 _rngKutter, _rngGulls;
        private readonly Pcg32[] _rngService;
        private readonly Pcg32[][] _rngFail;
        private readonly Queue<int>[] _queues;
        private readonly Machine[][] _machines;
        private readonly double[] _busy;
        private readonly int[] _maxQueue;
        private readonly List<double> _arrival = new List<double>();
        private readonly Queue<KeyValuePair<int, int>> _transportQueue = new Queue<KeyValuePair<int, int>>();
        private double _leadSum;

        public double Time { get; private set; }
        public int Events { get; private set; }
        public int RobotsFree { get; private set; }
        public int Ready { get; private set; }
        public int Delivered { get; private set; }
        public int Finished { get; private set; }
        public int Stolen { get; private set; }
        public int Spoiled { get; private set; }
        public int Arrived => _arrival.Count;
        public FactoryConfig Config => _c;
        /// <summary>Alle abgearbeiteten Ereignisse, wenn beim Anlegen gewünscht (Golden Files, „Blick unter die Haube“).</summary>
        public readonly List<SimEvent> Trace;

        public FactorySimulation(FactoryConfig config, ulong seed, bool trace = false)
        {
            _c = config;
            Trace = trace ? new List<SimEvent>() : null;
            int n = config.Stages.Length;
            _rngKutter = new Pcg32(seed, Pcg32.StreamKutter);
            _rngGulls = new Pcg32(seed, Pcg32.StreamGulls);
            _rngService = new Pcg32[n];
            _rngFail = new Pcg32[n][];
            _queues = new Queue<int>[n];
            _machines = new Machine[n][];
            for (int s = 0; s < n; s++)
            {
                _rngService[s] = new Pcg32(seed, Pcg32.StreamService + (ulong)s);
                int m = config.Stages[s].Machines;
                _rngFail[s] = new Pcg32[m];
                _machines[s] = new Machine[m];
                for (int i = 0; i < m; i++)
                {
                    _rngFail[s][i] = new Pcg32(seed, Pcg32.StreamFailures + (ulong)(10 * s + i));
                    _machines[s][i] = new Machine();
                }
                _queues[s] = new Queue<int>();
            }
            _busy = new double[n];
            _maxQueue = new int[n];
            RobotsFree = config.Robots;

            if (config.KutterBoxes > 0)
            {
                double first = _rngKutter.Exponential(config.KutterMeanGap);
                if (first <= config.KutterLast) Schedule(first, EventKind.Kutter);
            }
            if (config.TrainEvery > 0 && config.TrainFirst <= config.ShiftLength) Schedule(config.TrainFirst, EventKind.Train);
            for (int s = 0; s < n; s++)
            {
                if (config.Stages[s].Mtbf <= 0) continue;
                for (int m = 0; m < config.Stages[s].Machines; m++)
                {
                    double t = _rngFail[s][m].Exponential(config.Stages[s].Mtbf);
                    if (t <= config.ShiftLength) Schedule(t, EventKind.Fail, s, m);
                }
            }
        }

        // ---------- Ereignisliste ----------

        private void Schedule(double time, EventKind kind, int a = -1, int b = -1, int version = 0)
        {
            var e = new Entry { Time = time, Seq = _seq++, Kind = kind, A = a, B = b, Version = version };
            _heap.Add(e);
            int i = _heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (!Less(_heap[i], _heap[parent])) break;
                (_heap[i], _heap[parent]) = (_heap[parent], _heap[i]);
                i = parent;
            }
        }

        private static bool Less(Entry x, Entry y) => x.Time < y.Time || (x.Time == y.Time && x.Seq < y.Seq);

        private Entry Pop()
        {
            var top = _heap[0];
            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1, r = l + 1, m = i;
                if (l < _heap.Count && Less(_heap[l], _heap[m])) m = l;
                if (r < _heap.Count && Less(_heap[r], _heap[m])) m = r;
                if (m == i) break;
                (_heap[i], _heap[m]) = (_heap[m], _heap[i]);
                i = m;
            }
            return top;
        }

        /// <summary>Zeit des nächsten Ereignisses; unendlich, wenn keins mehr kommt.</summary>
        public double NextTime => _heap.Count > 0 ? _heap[0].Time : double.PositiveInfinity;

        /// <summary>Kisten, die gerade unterwegs sind (Band, Robo-Robbe, Übergabe) oder auf eine Robo-Robbe warten.</summary>
        public int InTransport
        {
            get
            {
                int n = _transportQueue.Count;
                foreach (var e in _heap) if (e.Kind == EventKind.Arrive) n++;
                return n;
            }
        }

        public int WaitingForRobot => _transportQueue.Count;

        /// <summary>Arbeitet alle Ereignisse bis einschließlich <paramref name="end"/> ab.</summary>
        public void RunUntil(double end)
        {
            while (_heap.Count > 0 && _heap[0].Time <= end) Step();
            Time = Math.Max(Time, end);
        }

        public DayResult RunDay()
        {
            RunUntil(_c.ShiftLength);
            return Result();
        }

        /// <summary>
        /// Ein Ereignis abarbeiten und zurückgeben (false, wenn keins mehr kommt). Eine Fertig-Meldung von einer Maschine,
        /// die während der Arbeit ausfiel, ist veraltet und zählt nicht – nach der Reparatur kommt eine neue.
        /// </summary>
        public bool Step(out SimEvent ev)
        {
            while (_heap.Count > 0)
            {
                var e = Pop();
                if (e.Kind == EventKind.Done && _machines[e.A][e.B].Version != e.Version) continue;
                Time = e.Time;
                Events++;
                ev = Handle(e);
                Trace?.Add(ev);
                return true;
            }
            ev = default;
            return false;
        }

        public bool Step() => Step(out _);

        private SimEvent Handle(Entry e)
        {
            double now = Time;
            var ev = new SimEvent { Time = now, Kind = e.Kind, A = e.A, B = e.B };
            switch (e.Kind)
            {
                case EventKind.Kutter:
                {
                    for (int i = 0; i < _c.KutterBoxes; i++) ArriveBox(now);
                    double next = now + _rngKutter.Exponential(_c.KutterMeanGap);
                    if (next <= _c.KutterLast) Schedule(next, EventKind.Kutter);
                    ev.A = _c.KutterBoxes;
                    ev.B = -1;
                    break;
                }
                case EventKind.Train:
                {
                    int loaded = Ready;
                    Delivered += loaded;
                    Ready = 0;
                    for (int i = 0; i < _c.TrainBoxes; i++) ArriveBox(now);
                    double next = now + _c.TrainEvery;
                    if (next <= _c.ShiftLength) Schedule(next, EventKind.Train);
                    ev.A = loaded;
                    ev.B = -1;
                    break;
                }
                case EventKind.Done:
                {
                    var m = _machines[e.A][e.B];
                    int job = m.Job;
                    m.Job = -1;
                    _busy[e.A] += now - m.BusySince;
                    Send(e.A + 1, job, now);
                    TryStart(e.A, now);
                    break;
                }
                case EventKind.Arrive:
                    Deliver(e.A, e.B, now);
                    break;
                case EventKind.RobotFree:
                    RobotsFree++;
                    if (_transportQueue.Count > 0)
                    {
                        var next = _transportQueue.Dequeue();
                        StartTrip(next.Key, next.Value, now);
                    }
                    ev.A = ev.B = -1;
                    break;
                case EventKind.Fail:
                {
                    var m = _machines[e.A][e.B];
                    m.Down = true;
                    if (m.Job >= 0)
                    {
                        m.Remaining = m.Done - now;
                        _busy[e.A] += now - m.BusySince;
                        m.Version++;
                    }
                    Schedule(now + _rngFail[e.A][e.B].Exponential(_c.Stages[e.A].Mttr), EventKind.Repair, e.A, e.B);
                    break;
                }
                case EventKind.Repair:
                {
                    var m = _machines[e.A][e.B];
                    m.Down = false;
                    if (m.Job >= 0)
                    {
                        m.Done = now + m.Remaining;
                        m.BusySince = now;
                        Schedule(m.Done, EventKind.Done, e.A, e.B, m.Version);
                    }
                    double next = now + _rngFail[e.A][e.B].Exponential(_c.Stages[e.A].Mtbf);
                    if (next <= _c.ShiftLength) Schedule(next, EventKind.Fail, e.A, e.B);
                    TryStart(e.A, now);
                    break;
                }
            }
            return ev;
        }

        // ---------- Kisten ----------

        private void ArriveBox(double now)
        {
            int job = _arrival.Count;
            _arrival.Add(now);
            if (_c.GullChance > 0 && _rngGulls.NextDouble() < _c.GullChance)
            {
                Stolen++;
                return;
            }
            Send(0, job, now);
        }

        private void Send(int h, int job, double now)
        {
            var hop = _c.Hops[h];
            if (hop.Time <= 0) Deliver(h, job, now);
            else if (hop.Mode != HopMode.Robot) Schedule(now + hop.Time, EventKind.Arrive, h, job);
            else if (RobotsFree > 0) StartTrip(h, job, now);
            else _transportQueue.Enqueue(new KeyValuePair<int, int>(h, job));
        }

        private void StartTrip(int h, int job, double now)
        {
            RobotsFree--;
            double oneWay = _c.Hops[h].Time;
            Schedule(now + oneWay + _c.RobotDelay, EventKind.Arrive, h, job);
            Schedule(now + 2 * oneWay + _c.RobotDelay, EventKind.RobotFree);
        }

        private void Deliver(int h, int job, double now)
        {
            if (h < _c.Stages.Length)
            {
                EnterQueue(h, job, now);
                return;
            }
            Finished++;
            _leadSum += now - _arrival[job];
            Ready++;
        }

        private void EnterQueue(int s, int job, double now)
        {
            int cap = _c.Stages[s].Capacity;
            var q = _queues[s];
            if (cap >= 0 && q.Count >= cap)
            {
                Spoiled++;
                return;
            }
            q.Enqueue(job);
            _maxQueue[s] = Math.Max(_maxQueue[s], q.Count);
            TryStart(s, now);
        }

        private void TryStart(int s, double now)
        {
            var q = _queues[s];
            var st = _c.Stages[s];
            var ms = _machines[s];
            for (int i = 0; i < ms.Length; i++)
            {
                if (q.Count == 0) return;
                var m = ms[i];
                if (m.Job >= 0 || m.Down) continue;
                m.Job = q.Dequeue();
                m.Done = now + _rngService[s].Triangular(st.Low, st.Mode, st.High);
                m.BusySince = now;
                Schedule(m.Done, EventKind.Done, s, i, m.Version);
            }
        }

        // ---------- Zustand für die Anzeige ----------

        public int StageCount => _c.Stages.Length;

        /// <summary>Kisten in der Warteschlange vor Stufe <paramref name="s"/> (Steg, Kühlregal, Packtisch).</summary>
        public int Waiting(int s) => _queues[s].Count;

        /// <summary>Kisten an einer Station: wartend plus in Arbeit.</summary>
        public int InStation(int s)
        {
            int n = _queues[s].Count;
            foreach (var m in _machines[s]) if (m.Job >= 0) n++;
            return n;
        }

        public MachineState State(int s, int machine)
        {
            var m = _machines[s][machine];
            return m.Down ? MachineState.Down : m.Job >= 0 ? MachineState.Busy : MachineState.Idle;
        }

        public DayResult Result()
        {
            int n = _c.Stages.Length;
            double end = Time;
            var busy = (double[])_busy.Clone();
            for (int s = 0; s < n; s++)
                foreach (var m in _machines[s])
                    if (m.Job >= 0 && !m.Down) busy[s] += end - m.BusySince;
            var util = new double[n];
            int bottleneck = n > 0 ? 0 : -1;
            for (int s = 0; s < n; s++)
            {
                int machines = _c.Stages[s].Machines;
                util[s] = machines > 0 && end > 0 ? busy[s] / (machines * end) : 0.0;
                if (util[s] > util[bottleneck]) bottleneck = s;
            }
            var endQueue = new int[n];
            for (int s = 0; s < n; s++) endQueue[s] = _queues[s].Count;
            return new DayResult
            {
                Delivered = Delivered,
                Finished = Finished,
                Arrived = Arrived,
                Stolen = Stolen,
                Spoiled = Spoiled,
                LeadTime = Finished > 0 ? _leadSum / Finished : 0.0,
                Utilization = util,
                MaxQueue = (int[])_maxQueue.Clone(),
                EndQueue = endQueue,
                Bottleneck = bottleneck,
                Events = Events,
            };
        }
    }
}
