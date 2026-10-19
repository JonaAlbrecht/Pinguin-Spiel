using System;
using System.Collections.Generic;

namespace SFE.SimCore.Process
{
    /// <summary>Was dem Aufbau fehlt: eine Station/ein Kühlregal (<see cref="Missing"/>) oder Robo-Robben für einen Weg.</summary>
    public sealed class LayoutProblem
    {
        public const string Missing = "missing";
        public const string NoRobot = "noRobot";
        public string Kind;
        /// <summary>Fehlendes Zeichen (bei <see cref="Missing"/>).</summary>
        public char Symbol;
        /// <summary>Weg ohne Träger (bei <see cref="NoRobot"/>): 0 = Anlegestelle → erste Station …</summary>
        public int Hop = -1;
    }

    /// <summary>
    /// Die Fabrikhalle als Raster (eine Zeichenkette je Zeile, Zeile 0 oben). Zeichen wie im Level:
    /// D Anlegestelle, T Verladung, # Säule, . Boden; S/F/P Stationen, K Kühlregal, B Förderband, R Robo-Robbe.
    /// Rechnet aus dem Raster die Wege, Kosten und die Zahlen für die Simulation – genau wie <c>simref/process.py</c>.
    /// </summary>
    public sealed class FactoryLayout
    {
        public const char Dock = 'D', Train = 'T', Floor = '.', Pillar = '#';
        public const char Regal = 'K', Belt = 'B', Robot = 'R';

        public readonly int Width, Height;
        private readonly char[] _cells;

        public FactoryLayout(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new char[width * height];
            for (int i = 0; i < _cells.Length; i++) _cells[i] = Floor;
        }

        public static FactoryLayout FromRows(string[] rows)
        {
            var l = new FactoryLayout(rows[0].Length, rows.Length);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    l[x, y] = rows[y][x];
            return l;
        }

        public string[] ToRows()
        {
            var rows = new string[Height];
            for (int y = 0; y < Height; y++) rows[y] = new string(_cells, y * Width, Width);
            return rows;
        }

        public FactoryLayout Clone() => FromRows(ToRows());

        public int CellCount => _cells.Length;

        public char this[int index]
        {
            get => _cells[index];
            set => _cells[index] = value;
        }

        public char this[int x, int y]
        {
            get => _cells[y * Width + x];
            set => _cells[y * Width + x] = value;
        }

        public int Count(char symbol)
        {
            int n = 0;
            foreach (char c in _cells) if (c == symbol) n++;
            return n;
        }

        /// <summary>Felder mit diesem Zeichen, Zeile für Zeile von oben.</summary>
        public List<int> Cells(char symbol)
        {
            var list = new List<int>();
            for (int i = 0; i < _cells.Length; i++) if (_cells[i] == symbol) list.Add(i);
            return list;
        }

        /// <summary>Gehört zur Halle (Anlegestelle, Verladung, Säule) – kann der Spieler nicht ändern.</summary>
        public static bool IsFixed(char c) => c == Dock || c == Train || c == Pillar;

        // ---------- Wege ----------

        /// <summary>Kürzester Abstand (Kästchen waagerecht + senkrecht) zwischen zwei Gruppen von Feldern.</summary>
        public int Distance(List<int> a, List<int> b)
        {
            int best = int.MaxValue;
            foreach (int i in a)
                foreach (int j in b)
                    best = Math.Min(best, Math.Abs(i % Width - j % Width) + Math.Abs(i / Width - j / Width));
            return best;
        }

        /// <summary>
        /// Anzahl Förderband-Felder auf dem kürzesten Band von Gruppe a nach Gruppe b; −1 = kein Band dazwischen.
        /// Breitensuche über Bandfelder: Start sind Bandfelder direkt neben a, Ziel ist ein Bandfeld direkt neben b.
        /// </summary>
        public int BeltLength(List<int> a, List<int> b)
        {
            var nearA = Neighbours(a);
            var nearB = Neighbours(b);
            var seen = new bool[_cells.Length];
            var frontier = new List<int>();
            for (int i = 0; i < _cells.Length; i++)
                if (_cells[i] == Belt && nearA.Contains(i)) { frontier.Add(i); seen[i] = true; }
            int depth = 1;
            while (frontier.Count > 0)
            {
                foreach (int c in frontier) if (nearB.Contains(c)) return depth;
                var next = new List<int>();
                foreach (int c in frontier)
                {
                    int x = c % Width, y = c / Width;
                    foreach (int n in Around(x, y))
                    {
                        if (n < 0 || seen[n] || _cells[n] != Belt) continue;
                        seen[n] = true;
                        next.Add(n);
                    }
                }
                frontier = next;
                depth++;
            }
            return -1;
        }

        private HashSet<int> Neighbours(List<int> group)
        {
            var set = new HashSet<int>();
            foreach (int c in group)
                foreach (int n in Around(c % Width, c / Width))
                    if (n >= 0) set.Add(n);
            return set;
        }

        /// <summary>Die vier Nachbarfelder (rechts, links, unten, oben); −1 außerhalb.</summary>
        private IEnumerable<int> Around(int x, int y)
        {
            yield return x + 1 < Width ? y * Width + x + 1 : -1;
            yield return x - 1 >= 0 ? y * Width + x - 1 : -1;
            yield return y + 1 < Height ? (y + 1) * Width + x : -1;
            yield return y - 1 >= 0 ? (y - 1) * Width + x : -1;
        }

        /// <summary>Anlegestelle, die Stationen der Reihe nach, Verladung – die Stationen des Fischwegs.</summary>
        public static char[] GroupSymbols(ProcessLevel level)
        {
            var g = new char[level.Stages.Length + 2];
            g[0] = Dock;
            for (int i = 0; i < level.Stages.Length; i++) g[i + 1] = level.Stages[i].Symbol;
            g[g.Length - 1] = Train;
            return g;
        }

        /// <summary>Je Weg (Anlegestelle → erste Station, …, letzte Station → Verladung) Art und Dauer; null = Station fehlt.</summary>
        public Hop[] Hops(ProcessLevel level)
        {
            var symbols = GroupSymbols(level);
            var hops = new Hop[symbols.Length - 1];
            for (int h = 0; h < hops.Length; h++)
            {
                var a = Cells(symbols[h]);
                var b = Cells(symbols[h + 1]);
                if (a.Count == 0 || b.Count == 0) continue;
                int dist = Distance(a, b);
                if (dist <= 1) { hops[h] = new Hop(HopMode.Direct, level.Handover, dist); continue; }
                int belt = BeltLength(a, b);
                hops[h] = belt >= 0
                    ? new Hop(HopMode.Belt, level.BeltMinutesPerCell * (belt + 1), dist)
                    : new Hop(HopMode.Robot, level.RobotMinutesPerCell * dist, dist);
            }
            return hops;
        }

        /// <summary>Was fehlt, damit Fisch durchläuft (Hinweise im Spiel). Leer = die Fabrik läuft.</summary>
        public List<LayoutProblem> Problems(ProcessLevel level)
        {
            var found = new List<LayoutProblem>();
            foreach (var s in level.Stages)
                if (Count(s.Symbol) == 0) found.Add(new LayoutProblem { Kind = LayoutProblem.Missing, Symbol = s.Symbol });
            if (Count(Regal) == 0) found.Add(new LayoutProblem { Kind = LayoutProblem.Missing, Symbol = Regal });
            if (Count(Robot) == 0)
            {
                var hops = Hops(level);
                for (int h = 0; h < hops.Length; h++)
                    if (hops[h] != null && hops[h].Mode == HopMode.Robot)
                        found.Add(new LayoutProblem { Kind = LayoutProblem.NoRobot, Hop = h });
            }
            return found;
        }

        /// <summary>Kosten der gesetzten Bausteine (Anlegestelle, Verladung, Säulen gehören zur Halle).</summary>
        public int Cost(ProcessLevel level)
        {
            int sum = 0;
            foreach (char c in _cells) sum += level.Cost(c);
            return sum;
        }

        public FactoryConfig ToConfig(ProcessLevel level)
        {
            var problems = Problems(level);
            if (problems.Count > 0) throw new InvalidOperationException("Aufbau unvollständig: " + problems.Count + " Teile fehlen");
            var hops = Hops(level);
            int robots = Count(Robot);
            var stages = new StageConfig[level.Stages.Length];
            for (int i = 0; i < stages.Length; i++)
            {
                var s = level.Stages[i];
                stages[i] = new StageConfig
                {
                    Machines = Count(s.Symbol),
                    Low = s.Low, Mode = s.Mode, High = s.High, Mtbf = s.Mtbf, Mttr = s.Mttr,
                    // Das Kühlregal steht vor dem Filetieren (Stufe 1), dort ist die Warteschlange begrenzt
                    Capacity = i == 1 ? level.RegalCapacity * Count(Regal) : -1,
                };
            }
            return new FactoryConfig
            {
                ShiftLength = level.ShiftLength,
                KutterMeanGap = level.KutterMeanGap,
                KutterBoxes = level.KutterBoxes,
                KutterLast = level.KutterLast,
                TrainFirst = level.TrainFirst,
                TrainEvery = level.TrainEvery,
                TrainBoxes = level.TrainBoxes,
                GullChance = level.GullChance,
                Stages = stages,
                Hops = hops,
                Robots = robots,
                RobotDelay = level.CrowdDelay * Math.Max(0, robots - level.RobotParking),
            };
        }

        /// <summary>Gleicher Aufbau? (Rückgängig legt nur dann einen Schritt an, wenn sich etwas geändert hat.)</summary>
        public static bool Same(string[] a, string[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
