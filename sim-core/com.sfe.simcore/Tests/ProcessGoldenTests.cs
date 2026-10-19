using System.Collections.Generic;
using NUnit.Framework;
using SFE.SimCore.Data;
using SFE.SimCore.Process;

namespace SFE.SimCore.Tests
{
    /// <summary>
    /// MG3 „Fischfabrik“: C# rechnet Ereignis für Ereignis dasselbe wie die Python-Referenz (golden/process/*.json,
    /// erzeugt von <c>simref/golden_process.py</c>). Zufallszahlen bitgenau, Ereignisse und Kennzahlen gleich.
    /// </summary>
    public class ProcessGoldenTests
    {
        private static ProcessLevel Level =>
            ProcessLevel.FromJson(TestPaths.Read(TestPaths.Data("Levels/MG3_Fischfabrik.json")));

        private static Dictionary<string, object> Golden(string name) =>
            MiniJson.ParseObject(TestPaths.Read(TestPaths.Golden("process/" + name + ".json")));

        [Test]
        public void Pcg32_MatchesPython_BitForBit()
        {
            foreach (var o in Golden("pcg32").Arr("cases"))
            {
                var c = (Dictionary<string, object>)o;
                ulong seed = ulong.Parse(c.Str("seed"));
                ulong stream = (ulong)c.Int("stream");
                var rng = new Pcg32(seed, stream);
                foreach (var u in c.Arr("uint")) Assert.AreEqual((uint)System.Convert.ToDouble(u), rng.NextUInt(), $"Seed {seed}");
                rng = new Pcg32(seed, stream);
                foreach (var e in c.Arr("exponential20")) Assert.AreEqual(System.Convert.ToDouble(e), rng.Exponential(20.0), 1e-9);
                rng = new Pcg32(seed, stream);
                foreach (var t in c.Arr("triangular4_6_10")) Assert.AreEqual(System.Convert.ToDouble(t), rng.Triangular(4, 6, 10), 1e-9);
            }
        }

        [Test]
        public void Pcg32_MatchesTheReferenceDemo()
        {
            var rng = new Pcg32(42, 54);
            uint[] expected = { 0xA15C02B7, 0x7B47F409, 0xBA1D3330, 0x83D2F293, 0xBFA4784B, 0xCBED606E };
            foreach (uint e in expected) Assert.AreEqual(e, rng.NextUInt());
        }

        [Test]
        public void Lesson_MatchesPython_RowByRow()
        {
            var g = Golden("lesson");
            var lesson = new StationLesson(Level.Lesson);
            var rows = g.Arr("rows");
            Assert.AreEqual(Level.Lesson.Events, rows.Count);
            foreach (var o in rows)
            {
                var e = (Dictionary<string, object>)o;
                var r = lesson.Next();
                Assert.AreEqual(e.Num("time"), r.Time, 1e-9);
                Assert.AreEqual(e.Str("kind") == "arrive", r.IsArrival);
                Assert.AreEqual(e.Int("before"), r.Before);
                Assert.AreEqual(e.Int("arrived"), r.Arrived);
                Assert.AreEqual(e.Int("finished"), r.Finished);
                Assert.AreEqual(e.Int("after"), r.After);
                Assert.AreEqual(e.Int("waiting"), r.Waiting);
            }
        }

        [Test]
        public void Layout_HopsCostsAndProblems_MatchPython()
        {
            var level = Level;
            foreach (var kv in Golden("layout").Obj("layouts"))
            {
                var e = (Dictionary<string, object>)kv.Value;
                var layout = FactoryLayout.FromRows(e.StrArray("tiles"));
                Assert.AreEqual(e.Int("cost"), layout.Cost(level), kv.Key);
                var hops = layout.Hops(level);
                var expected = e.Arr("hops");
                Assert.AreEqual(expected.Count, hops.Length, kv.Key);
                for (int h = 0; h < hops.Length; h++)
                {
                    if (expected[h] == null) { Assert.IsNull(hops[h], $"{kv.Key} Weg {h}"); continue; }
                    var x = (Dictionary<string, object>)expected[h];
                    Assert.AreEqual(x.Str("mode"), ModeName(hops[h].Mode), $"{kv.Key} Weg {h}");
                    Assert.AreEqual(x.Num("time"), hops[h].Time, 1e-9, $"{kv.Key} Weg {h}");
                    Assert.AreEqual(x.Int("distance"), hops[h].Distance, $"{kv.Key} Weg {h}");
                }
                var problems = layout.Problems(level);
                var exp = e.Arr("problems");
                Assert.AreEqual(exp.Count, problems.Count, kv.Key);
                for (int i = 0; i < exp.Count; i++)
                {
                    var p = (List<object>)exp[i];
                    Assert.AreEqual((string)p[0], problems[i].Kind, kv.Key);
                    string detail = problems[i].Kind == LayoutProblem.Missing ? problems[i].Symbol.ToString() : problems[i].Hop.ToString();
                    Assert.AreEqual((string)p[1], detail, kv.Key);
                }
            }
        }

        private static string ModeName(HopMode m) => m == HopMode.Direct ? "direct" : m == HopMode.Belt ? "belt" : "robot";

        [TestCase("olaf")]
        [TestCase("kalle")]
        [TestCase("hint")]
        [TestCase("failures")]
        [TestCase("tiny_regal")]
        [TestCase("gulls")]
        public void Trace_MatchesPython_EventByEvent(string name)
        {
            var g = Golden("trace_" + name);
            var sim = new FactorySimulation(TraceConfig(name), (ulong)g.Int("seed"), trace: true);
            sim.RunUntil(g.Num("end"));
            var trace = g.Arr("trace");
            Assert.AreEqual(trace.Count, sim.Trace.Count, "Anzahl Ereignisse");
            for (int i = 0; i < trace.Count; i++)
            {
                var e = (List<object>)trace[i];
                var a = sim.Trace[i];
                Assert.AreEqual(System.Convert.ToDouble(e[0]), a.Time, 1e-9, $"Ereignis {i}: Zeit");
                Assert.AreEqual((int)System.Convert.ToDouble(e[1]), (int)a.Kind, $"Ereignis {i}: Art");
                Assert.AreEqual((int)System.Convert.ToDouble(e[2]), a.A, $"Ereignis {i}: A");
                Assert.AreEqual((int)System.Convert.ToDouble(e[3]), a.B, $"Ereignis {i}: B");
            }
            AssertDay(g.Obj("result"), sim.Result(), name);
        }

        /// <summary>Dieselben Fälle wie <c>trace_cases</c> in golden_process.py.</summary>
        private static FactoryConfig TraceConfig(string name)
        {
            var level = Level;
            var olaf = FactoryLayout.FromRows(level.OlafTiles).ToConfig(level);
            switch (name)
            {
                case "olaf": return olaf;
                case "kalle": return FactoryLayout.FromRows(level.KalleTiles).ToConfig(level);
                case "hint": return FactoryLayout.FromRows(level.HintTiles).ToConfig(level);
                case "failures":
                    foreach (var s in olaf.Stages) { s.Mtbf = 25.0; s.Mttr = 6.0; }
                    return olaf;
                case "tiny_regal":
                    foreach (var s in olaf.Stages) if (s.Capacity >= 0) s.Capacity = 1;
                    return olaf;
                case "gulls":
                    olaf.GullChance = 0.3;
                    return olaf;
            }
            throw new System.ArgumentException(name);
        }

        [TestCase("olaf")]
        [TestCase("kalle")]
        [TestCase("hint")]
        [TestCase("compact")]
        [TestCase("two_robots")]
        public void FullShiftAndTenDays_MatchPython(string name)
        {
            var level = Level;
            var g = Golden("day_" + name);
            var config = FactoryLayout.FromRows(g.StrArray("tiles")).ToConfig(level);
            ulong seed = (ulong)g.Int("seed");
            AssertDay(g.Obj("day"), Replications.RunDay(config, seed), name);

            var s = Replications.Run(config, seed, g.Int("replications"));
            var d = g.Obj("days");
            var delivered = d.Arr("delivered");
            for (int i = 0; i < delivered.Count; i++) Assert.AreEqual((int)System.Convert.ToDouble(delivered[i]), s.Days[i].Delivered, $"Tag {i}");
            Assert.AreEqual(d.Num("meanDelivered"), s.MeanDelivered, 1e-9);
            Assert.AreEqual(d.Num("sdDelivered"), s.SdDelivered, 1e-9);
            Assert.AreEqual(d.Num("halfWidth"), s.HalfWidth, 1e-9);
            Assert.AreEqual(d.Num("lower"), s.Lower, 1e-9);
            Assert.AreEqual(d.Num("meanSpoiled"), s.MeanSpoiled, 1e-9);
            Assert.AreEqual(d.Num("meanLeadTime"), s.MeanLeadTime, 1e-7);
            Assert.AreEqual(d.Int("stars"), Replications.Stars(s, level));
            CollectionAssert.AreEqual(MiniJson.NumArray(d.Arr("lights")), System.Array.ConvertAll(Replications.Lights(s, level), x => (double)x));
        }

        private static void AssertDay(Dictionary<string, object> e, DayResult r, string what)
        {
            Assert.AreEqual(e.Int("delivered"), r.Delivered, what + ": pünktlich");
            Assert.AreEqual(e.Int("finished"), r.Finished, what + ": fertig");
            Assert.AreEqual(e.Int("arrived"), r.Arrived, what + ": angekommen");
            Assert.AreEqual(e.Int("stolen"), r.Stolen, what + ": Möwen");
            Assert.AreEqual(e.Int("spoiled"), r.Spoiled, what + ": verdorben");
            Assert.AreEqual(e.Num("leadTime"), r.LeadTime, 1e-7, what + ": Durchlaufzeit");
            Assert.AreEqual(e.Int("bottleneck"), r.Bottleneck, what + ": Engpass");
            Assert.AreEqual(e.Int("events"), r.Events, what + ": Ereignisse");
            var util = MiniJson.NumArray(e.Arr("utilization"));
            for (int s = 0; s < util.Length; s++) Assert.AreEqual(util[s], r.Utilization[s], 1e-9, what + ": Auslastung");
            CollectionAssert.AreEqual(MiniJson.NumArray(e.Arr("maxQueue")), System.Array.ConvertAll(r.MaxQueue, x => (double)x), what);
            CollectionAssert.AreEqual(MiniJson.NumArray(e.Arr("endQueue")), System.Array.ConvertAll(r.EndQueue, x => (double)x), what);
        }
    }
}
