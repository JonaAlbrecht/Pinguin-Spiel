using System.Collections.Generic;
using SFE.SimCore.Data;

namespace SFE.SimCore.Process
{
    /// <summary>Eine Station der Linie (Sortieren, Filetieren, Verpacken): Zeichen im Raster, Arbeitszeit, Störungen.</summary>
    public sealed class ProcessStage
    {
        public string Id;
        public char Symbol;
        public double Low, Mode, High;
        /// <summary>Mittlere Zeit bis zur nächsten Störung je Maschine; 0 = keine Störungen.</summary>
        public double Mtbf;
        /// <summary>Mittlere Reparaturdauer.</summary>
        public double Mttr;
    }

    /// <summary>Rechentafel in Schritt 2: eine Station, einzeln ankommende Kisten.</summary>
    public sealed class StationLessonData
    {
        public ulong Seed;
        public double ArrivalMean;
        public double Low, Mode, High;
        public int Events;
    }

    /// <summary>
    /// Leveldaten von MG3 aus <c>Resources/Data/Levels/MG3_Fischfabrik.json</c> – dieselbe Datei liest die
    /// Python-Referenz (<c>simref/process.py</c>, <c>load_level</c>). Zeiten in Minuten ab Schichtbeginn.
    /// </summary>
    public sealed class ProcessLevel
    {
        public string Id;
        public int Width, Height;
        public string[] InitialTiles, KalleTiles, OlafTiles, HintTiles;
        /// <summary>Schichtbeginn als Minute des Tages (360 = 6:00) – nur für die Uhr im Spiel.</summary>
        public double ShiftStart;
        public double ShiftLength;
        public double KutterMeanGap;
        public int KutterBoxes;
        public double KutterLast;
        public double TrainFirst, TrainEvery;
        public int TrainBoxes;
        public double GullChance;
        public ProcessStage[] Stages;
        public int RegalCapacity;
        public double RobotMinutesPerCell;
        public int RobotParking;
        public double CrowdDelay;
        public double BeltMinutesPerCell;
        public double Handover;
        public readonly Dictionary<char, int> Costs = new Dictionary<char, int>();
        public readonly Dictionary<char, int> Limits = new Dictionary<char, int>();
        public int BuildBudget, Budget;
        public int Target;
        public double SpoilMax;
        public int Replications;
        public ulong Seed;
        public int Rounds;
        public StationLessonData Lesson;

        public static ProcessLevel FromJson(string json) => FromDict(MiniJson.ParseObject(json));

        public static ProcessLevel FromDict(Dictionary<string, object> d)
        {
            var grid = d.Obj("grid");
            var shift = d.Obj("shift");
            var k = d.Obj("kutter");
            var t = d.Obj("train");
            var tr = d.Obj("transport");
            var goals = d.Obj("goals");
            var lv = new ProcessLevel
            {
                Id = d.Str("id"),
                Width = grid.Int("width"),
                Height = grid.Int("height"),
                InitialTiles = d.StrArray("initialTiles"),
                KalleTiles = d.StrArray("kalleTiles"),
                OlafTiles = d.StrArray("olafTiles"),
                HintTiles = d.StrArray("hintTiles"),
                ShiftStart = shift.Num("start"),
                ShiftLength = shift.Num("length"),
                KutterMeanGap = k.Num("meanGap"),
                KutterBoxes = k.Int("boxes"),
                KutterLast = k.Num("lastArrival"),
                TrainFirst = t.Num("first"),
                TrainEvery = t.Num("every"),
                TrainBoxes = t.Int("boxes"),
                GullChance = d.Obj("gulls").Num("chance"),
                RegalCapacity = d.Obj("regal").Int("capacity"),
                RobotMinutesPerCell = tr.Num("robotMinutesPerCell"),
                RobotParking = tr.Int("robotParking"),
                CrowdDelay = tr.Num("crowdDelay"),
                BeltMinutesPerCell = tr.Num("beltMinutesPerCell"),
                Handover = tr.Num("handover"),
                BuildBudget = d.Int("buildBudget"),
                Budget = d.Int("budget"),
                Target = goals.Int("delivered"),
                SpoilMax = goals.Num("spoiledMax"),
                Replications = d.Int("replications"),
                Seed = (ulong)d.Int("seed"),
                Rounds = d.Has("rounds") ? d.Int("rounds") : 3,
            };
            var stages = d.Arr("stages");
            lv.Stages = new ProcessStage[stages.Count];
            for (int i = 0; i < stages.Count; i++)
            {
                var s = (Dictionary<string, object>)stages[i];
                var svc = s.Obj("service");
                lv.Stages[i] = new ProcessStage
                {
                    Id = s.Str("id"),
                    Symbol = s.Str("symbol")[0],
                    Low = svc.Num("min"),
                    Mode = svc.Num("mode"),
                    High = svc.Num("max"),
                    Mtbf = s.Has("mtbf") ? s.Num("mtbf") : 0.0,
                    Mttr = s.Has("mttr") ? s.Num("mttr") : 0.0,
                };
            }
            foreach (var kv in d.Obj("costs")) lv.Costs[kv.Key[0]] = (int)System.Math.Round(System.Convert.ToDouble(kv.Value));
            foreach (var kv in d.Obj("limits")) lv.Limits[kv.Key[0]] = (int)System.Math.Round(System.Convert.ToDouble(kv.Value));
            if (d.Has("stationLesson"))
            {
                var l = d.Obj("stationLesson");
                var svc = l.Obj("service");
                lv.Lesson = new StationLessonData
                {
                    Seed = (ulong)l.Int("seed"),
                    ArrivalMean = l.Num("arrivalMean"),
                    Low = svc.Num("min"),
                    Mode = svc.Num("mode"),
                    High = svc.Num("max"),
                    Events = l.Int("events"),
                };
            }
            return lv;
        }

        public int Cost(char symbol) => Costs.TryGetValue(symbol, out int c) ? c : 0;

        public int Limit(char symbol) => Limits.TryGetValue(symbol, out int l) ? l : int.MaxValue;

        /// <summary>Index der Station mit diesem Zeichen, sonst −1.</summary>
        public int StageOf(char symbol)
        {
            for (int i = 0; i < Stages.Length; i++) if (Stages[i].Symbol == symbol) return i;
            return -1;
        }
    }
}
