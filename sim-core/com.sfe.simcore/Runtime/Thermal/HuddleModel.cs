using System;
using System.Collections.Generic;
using SFE.SimCore.Data;
using SFE.SimCore.LinearAlgebra;

namespace SFE.SimCore.Thermal
{
    public sealed class HuddleParams
    {
        public double Heat;
        public double Capacity;
        public double Contact;
        public double HExposed;
        public double HShielded;
        public double ContactDistance;
        public double ShieldDistance;
        public double ShieldCone;
        public double AirTemp;
    }

    /// <summary>Startwerte des Zwei-Pinguine-Beispiels vor dem Huddle (Grete im Wind, Pip in ihrem Windschatten).</summary>
    public sealed class HuddlePairLesson
    {
        public double GreteStart;
        public double PipStart;
        public double Distance;
        public double WindEast;
        public double WindNorth;
        public double Dt;
        public int Steps;

        /// <summary>Ein Modell mit zwei Pinguinen: 0 = Grete (windaufwärts, Westen), 1 = Pip (Osten).</summary>
        public HuddleModel CreateModel(HuddleParams p)
        {
            var m = new HuddleModel(p, 2, 0);
            m.X[0] = -Distance / 2;
            m.X[1] = Distance / 2;
            m.Temperatures[0] = GreteStart;
            m.Temperatures[1] = PipStart;
            return m;
        }
    }

    /// <summary>
    /// Zerlegung eines Zeitschritts je Pinguin: <c>neu − vorher = Own + FromNeighbours − ToWind</c>.
    /// Die Teile gelten mit den neuen Temperaturen (implizites Euler), dann geht die Rechnung exakt auf.
    /// FromNeighbours summiert zu null – der Austausch erzeugt und vernichtet keine Wärme.
    /// </summary>
    public sealed class HuddleStepTerms
    {
        public double[] Own;
        public double[] FromNeighbours;
        public double[] ToWind;
    }

    /// <summary>Leveldaten des Prologs „Der letzte Huddle“ (MG1 Teil A).</summary>
    public sealed class HuddleLevel
    {
        public HuddlePairLesson PairLesson;
        public string Id;
        public int Penguins;
        public double PenguinRadius;
        public double ArenaRadius;
        public double SpawnRadius;
        public double StartTemp;
        public double FreezeBelow;
        public double SimSecondsPerSecond;
        public double SimStep;
        public double TargetSeconds;
        public double WindChangeEvery;
        public double WindWarning;
        public string[] WindSequence;
        public HuddleParams Model;

        public static HuddleLevel FromJson(string json)
        {
            var o = MiniJson.ParseObject(json);
            var m = o.Obj("model");
            return new HuddleLevel
            {
                Id = o.Str("id"),
                Penguins = o.Int("penguins"),
                PenguinRadius = o.Num("penguinRadius"),
                ArenaRadius = o.Num("arenaRadius"),
                SpawnRadius = o.Num("spawnRadius"),
                StartTemp = o.Num("startTemp"),
                FreezeBelow = o.Num("freezeBelow"),
                SimSecondsPerSecond = o.Num("simSecondsPerSecond"),
                SimStep = o.Num("simStep"),
                TargetSeconds = o.Num("targetSeconds"),
                WindChangeEvery = o.Num("windChangeEvery"),
                WindWarning = o.Num("windWarning"),
                WindSequence = o.StrArray("windSequence"),
                PairLesson = ReadPair(o),
                Model = new HuddleParams
                {
                    Heat = m.Num("heat"),
                    Capacity = m.Num("capacity"),
                    Contact = m.Num("contact"),
                    HExposed = m.Num("hExposed"),
                    HShielded = m.Num("hShielded"),
                    ContactDistance = m.Num("contactDistance"),
                    ShieldDistance = m.Num("shieldDistance"),
                    ShieldCone = m.Num("shieldCone"),
                    AirTemp = m.Num("airTemp"),
                },
            };
        }

        private static HuddlePairLesson ReadPair(Dictionary<string, object> o)
        {
            if (!o.Has("pairLesson")) return null;
            var p = o.Obj("pairLesson");
            var wind = MiniJson.NumArray(p.Arr("windTo"));
            return new HuddlePairLesson
            {
                GreteStart = p.Num("greteStart"),
                PipStart = p.Num("pipStart"),
                Distance = p.Num("distance"),
                WindEast = wind[0],
                WindNorth = wind[1],
                Dt = p.Num("dt"),
                Steps = p.Int("steps"),
            };
        }
    }

    /// <summary>
    /// MG1 Teil A: Jeder Pinguin ist ein Knoten. Transient, implizites Euler:
    /// <code>(C/dt + h_i + Σ G) T_i' − Σ G T_j' = C/dt · T_i + q + h_i · T_Luft</code>
    /// h_i = hExposed, wenn kein Pinguin windaufwärts in shieldDistance steht, sonst hShielded.
    /// Positionen in Metern, x = Osten, y = Norden. Identisch zu <c>sim-reference/simref/huddle.py</c>.
    /// </summary>
    public sealed class HuddleModel
    {
        private readonly HuddleParams _p;

        public double[] X { get; }
        public double[] Y { get; }
        public double[] Temperatures { get; }
        public bool[] Exposed { get; }
        public int Count => X.Length;

        public HuddleModel(HuddleParams p, int count, double startTemp)
        {
            _p = p;
            X = new double[count];
            Y = new double[count];
            Temperatures = new double[count];
            Exposed = new bool[count];
            for (int i = 0; i < count; i++) Temperatures[i] = startTemp;
        }

        public void UpdateExposure(double windEast, double windNorth)
        {
            double n = Math.Sqrt(windEast * windEast + windNorth * windNorth);
            double wx = windEast / n, wy = windNorth / n;
            for (int i = 0; i < Count; i++)
            {
                Exposed[i] = true;
                for (int j = 0; j < Count; j++)
                {
                    if (i == j) continue;
                    double vx = X[j] - X[i], vy = Y[j] - Y[i];
                    double d = Math.Sqrt(vx * vx + vy * vy);
                    if (d == 0.0 || d > _p.ShieldDistance) continue;
                    if (-(vx * wx + vy * wy) / d >= _p.ShieldCone)
                    {
                        Exposed[i] = false;
                        break;
                    }
                }
            }
        }

        /// <summary>Kontaktpaare (i, j) in Berührungsabstand – für die „Wärmefäden“.</summary>
        public List<(int, int)> Contacts()
        {
            var list = new List<(int, int)>();
            for (int i = 0; i < Count; i++)
                for (int j = i + 1; j < Count; j++)
                    if (Distance(i, j) <= _p.ContactDistance) list.Add((i, j));
            return list;
        }

        public void Step(double dt, double windEast, double windNorth)
        {
            UpdateExposure(windEast, windNorth);
            int n = Count;
            double c = _p.Capacity / dt;
            var builder = new SparseMatrixBuilder(n);
            var b = new double[n];
            for (int i = 0; i < n; i++)
            {
                double h = Exposed[i] ? _p.HExposed : _p.HShielded;
                builder.Add(i, i, c + h);
                b[i] = c * Temperatures[i] + _p.Heat + h * _p.AirTemp;
            }
            foreach (var (i, j) in Contacts())
            {
                builder.Add(i, i, _p.Contact);
                builder.Add(j, j, _p.Contact);
                builder.Add(i, j, -_p.Contact);
                builder.Add(j, i, -_p.Contact);
            }
            var solver = new ConjugateGradientSolver(builder.Build(), b, Temperatures, 1e-12);
            var t = solver.SolveToEnd();
            Array.Copy(t, Temperatures, n);
        }

        /// <summary>
        /// Zerlegt den letzten Schritt (nach <see cref="Step"/> aufrufen): Positionen, Wind und die neuen Temperaturen
        /// sind die, mit denen gerechnet wurde. Identisch zu <c>step_terms</c> in <c>simref/huddle.py</c>.
        /// </summary>
        public HuddleStepTerms LastStepTerms(double dt)
        {
            int n = Count;
            double f = dt / _p.Capacity;
            var t = new HuddleStepTerms { Own = new double[n], FromNeighbours = new double[n], ToWind = new double[n] };
            for (int i = 0; i < n; i++)
            {
                double h = Exposed[i] ? _p.HExposed : _p.HShielded;
                t.Own[i] = f * _p.Heat;
                t.ToWind[i] = f * h * (Temperatures[i] - _p.AirTemp);
            }
            foreach (var (i, j) in Contacts())
            {
                double flow = f * _p.Contact * (Temperatures[j] - Temperatures[i]);
                t.FromNeighbours[i] += flow;
                t.FromNeighbours[j] -= flow;
            }
            return t;
        }

        public double Distance(int i, int j)
        {
            double dx = X[i] - X[j], dy = Y[i] - Y[j];
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
