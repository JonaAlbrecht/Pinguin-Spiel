using System.Collections.Generic;
using SFE.SimCore.Data;

namespace SFE.SimCore.Flow
{
    /// <summary>Eine Auflösung des Kanals: <c>Scale</c> = Kästchen je Fein-Einheit (grob 0,5 · mittel 0,75 · fein 1).</summary>
    public sealed class FlowResolution
    {
        public string Id;
        public double Scale;
        public int Nx;
        public int Ny;
    }

    /// <summary>Startwerte der Rechentafel in MG2 Schritt 2 (Block <c>lesson</c>).</summary>
    public sealed class FlowLessonData
    {
        public double[] Air;
        public double[] Push;
        public double Tau;
        public int Steps;
        public int RunSteps;
    }

    /// <summary>
    /// Leveldaten MG2 „Polar-Express“ (<c>Resources/Data/Levels/MG2_PolarExpress.json</c>) – dieselbe Datei liest
    /// die Python-Referenz (<c>simref/flow_train.py</c>). Längen in Fein-Einheiten, Fahrzeit in Spiel-Stunden.
    /// </summary>
    public sealed class PolarExpressLevel
    {
        public double ChannelLength, ChannelHeight;
        public List<FlowResolution> Resolutions = new List<FlowResolution>();
        public string PlayResolution;
        public double U, Reynolds;
        public int Steps, AverageSteps, Ramp;
        public double Start, Length, Height, NoseMax, TipMin;
        public double[] Transitions;
        public double TransitionWidth, TransitionHeight;
        public double Headroom;
        public int ColumnsPerRow, SeatsPerRow, Passengers;
        public double Traction, Rolling, Headwind, DragScale, Distance, MinSpeed;
        public double TargetTime, BestTime;
        public int Rounds;
        public Dictionary<string, TrainShape> Shapes = new Dictionary<string, TrainShape>();
        public FlowLessonData Lesson;

        public double End => Start + Length;

        public static PolarExpressLevel FromJson(string json)
        {
            var d = MiniJson.ParseObject(json);
            var fl = d.Obj("flow");
            var tr = d.Obj("train");
            var se = d.Obj("seats");
            var tv = d.Obj("travel");
            var go = d.Obj("goals");
            var lv = new PolarExpressLevel
            {
                ChannelLength = d.Obj("channel").Num("length"),
                ChannelHeight = d.Obj("channel").Num("height"),
                PlayResolution = d.Str("playResolution"),
                U = fl.Num("u"), Reynolds = fl.Num("reynolds"), Steps = fl.Int("steps"),
                AverageSteps = fl.Int("averageSteps"), Ramp = fl.Int("ramp"),
                Start = tr.Num("start"), Length = tr.Num("length"), Height = tr.Num("height"),
                NoseMax = tr.Num("noseMax"), TipMin = tr.Num("tipMin"),
                Transitions = MiniJson.NumArray(tr.Arr("transitions")),
                TransitionWidth = tr.Num("transitionWidth"), TransitionHeight = tr.Num("transitionHeight"),
                Headroom = se.Num("headroom"), ColumnsPerRow = se.Int("columnsPerRow"),
                SeatsPerRow = se.Int("seatsPerRow"), Passengers = se.Int("passengers"),
                Traction = tv.Num("traction"), Rolling = tv.Num("rolling"), Headwind = tv.Num("headwind"),
                DragScale = tv.Num("dragScale"), Distance = tv.Num("distance"), MinSpeed = tv.Num("minSpeed"),
                TargetTime = go.Num("targetTime"), BestTime = go.Num("bestTime"),
                Rounds = d.Int("rounds"),
            };
            foreach (var r in d.Arr("resolutions"))
            {
                var o = (Dictionary<string, object>)r;
                double s = o.Num("scale");
                lv.Resolutions.Add(new FlowResolution
                {
                    Id = o.Str("id"), Scale = s,
                    Nx = (int)System.Math.Round(lv.ChannelLength * s, System.MidpointRounding.ToEven),
                    Ny = (int)System.Math.Round(lv.ChannelHeight * s, System.MidpointRounding.ToEven),
                });
            }
            foreach (var kv in d.Obj("shapes"))
                lv.Shapes[kv.Key] = TrainShape.FromJson((Dictionary<string, object>)kv.Value).Clamped(lv);
            var le = d.Obj("lesson");
            lv.Lesson = new FlowLessonData
            {
                Air = MiniJson.NumArray(le.Arr("air")), Push = MiniJson.NumArray(le.Arr("push")),
                Tau = le.Num("tau"), Steps = le.Int("steps"), RunSteps = le.Int("runSteps"),
            };
            return lv;
        }

        public FlowResolution Resolution(string id)
        {
            foreach (var r in Resolutions) if (r.Id == id) return r;
            throw new KeyNotFoundException("Auflösung fehlt: " + id);
        }

        /// <summary>Gleiche Reynolds-Zahl in jeder Auflösung: nu = u · Zughöhe in Kästchen / Re.</summary>
        public double Tau(FlowResolution r) => 0.5 + 3.0 * (U * Height * r.Scale / Reynolds);

        /// <summary>Gleiche Zeit in jeder Auflösung: Schritte proportional zur Feinheit (Python: round, ganzzahlig gerade).</summary>
        public int StepsFor(FlowResolution r) => RoundHalfEven(Steps * r.Scale);
        public int AverageStepsFor(FlowResolution r) => RoundHalfEven(AverageSteps * r.Scale);
        public int RampFor(FlowResolution r) => RoundHalfEven(Ramp * r.Scale);

        private static int RoundHalfEven(double v) => (int)System.Math.Round(v, System.MidpointRounding.ToEven);
    }
}
