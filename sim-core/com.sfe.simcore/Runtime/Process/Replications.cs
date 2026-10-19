using System;
using System.Collections.Generic;

namespace SFE.SimCore.Process
{
    /// <summary>Viele Tage desselben Plans: Mittelwert und 90-%-Vertrauensbereich der pünktlichen Sushi-Kisten.</summary>
    public sealed class DaysSummary
    {
        public DayResult[] Days;
        public double MeanDelivered, SdDelivered;
        /// <summary>Halbe Breite des 90-%-Vertrauensbereichs des Mittelwerts.</summary>
        public double HalfWidth;
        /// <summary>Untergrenze des Vertrauensbereichs – im Spiel „auch an schlechten Tagen“.</summary>
        public double Lower;
        public double MeanSpoiled, MeanStolen, MeanLeadTime;
        public int MinDelivered, MaxDelivered;
    }

    /// <summary>
    /// „Die Möwen-Frage“: Ein Tag ist Zufall – erst viele Tage zeigen, wie gut ein Plan ist (docs/03 MG3 Schritt 5).
    /// Tag d rechnet mit Seed <c>Basis + d</c>. Identisch zu <c>run_days</c>, <c>summarize</c>, <c>stars</c> und
    /// <c>lights</c> in <c>simref/process.py</c>.
    /// </summary>
    public static class Replications
    {
        /// <summary>t-Quantile (95 %, zweiseitig 90 %) für 1…30 Freiheitsgrade; darüber die Normalverteilung.</summary>
        private static readonly double[] T95 =
        {
            6.313752, 2.919986, 2.353363, 2.131847, 2.015048, 1.943180, 1.894579, 1.859548, 1.833113, 1.812461,
            1.795885, 1.782288, 1.770933, 1.761310, 1.753050, 1.745884, 1.739607, 1.734064, 1.729133, 1.724718,
            1.720743, 1.717144, 1.713872, 1.710882, 1.708141, 1.705618, 1.703288, 1.701131, 1.699127, 1.697261,
        };
        private const double Z95 = 1.644854;

        public static double TQuantile95(int df) => df >= 1 && df <= T95.Length ? T95[df - 1] : Z95;

        public static ulong DaySeed(ulong baseSeed, int day) => baseSeed + (ulong)day;

        public static DayResult RunDay(FactoryConfig config, ulong seed) => new FactorySimulation(config, seed).RunDay();

        public static DaysSummary Run(FactoryConfig config, ulong baseSeed, int days)
        {
            var results = new DayResult[days];
            for (int d = 0; d < days; d++) results[d] = RunDay(config, DaySeed(baseSeed, d));
            return Summarize(results);
        }

        public static DaysSummary Summarize(IList<DayResult> days)
        {
            int n = days.Count;
            double sum = 0, spoiled = 0, stolen = 0, lead = 0;
            int min = int.MaxValue, max = int.MinValue;
            foreach (var d in days)
            {
                sum += d.Delivered;
                spoiled += d.Spoiled;
                stolen += d.Stolen;
                lead += d.LeadTime;
                min = Math.Min(min, d.Delivered);
                max = Math.Max(max, d.Delivered);
            }
            double mean = sum / n;
            double sq = 0;
            foreach (var d in days) sq += (d.Delivered - mean) * (d.Delivered - mean);
            double sd = n > 1 ? Math.Sqrt(sq / (n - 1)) : 0.0;
            double half = n > 1 ? TQuantile95(n - 1) * sd / Math.Sqrt(n) : 0.0;
            var arr = new DayResult[n];
            days.CopyTo(arr, 0);
            return new DaysSummary
            {
                Days = arr,
                MeanDelivered = mean,
                SdDelivered = sd,
                HalfWidth = half,
                Lower = mean - half,
                MeanSpoiled = spoiled / n,
                MeanStolen = stolen / n,
                MeanLeadTime = lead / n,
                MinDelivered = min,
                MaxDelivered = max,
            };
        }

        /// <summary>★ im Schnitt pünktlich · ★★ dazu kaum Verdorbenes · ★★★ dazu auch an schlechten Tagen (Untergrenze ≥ Ziel).</summary>
        public static int Stars(DaysSummary s, ProcessLevel level)
        {
            if (s.MeanDelivered < level.Target) return 0;
            if (s.MeanSpoiled > level.SpoilMax) return 1;
            return s.Lower >= level.Target ? 3 : 2;
        }

        /// <summary>Ampeln 2 = grün, 1 = gelb, 0 = rot: pünktlich, kaum verdorben, auch an schlechten Tagen.</summary>
        public static int[] Lights(DaysSummary s, ProcessLevel level)
        {
            double t = level.Target;
            int onTime = s.MeanDelivered >= t ? 2 : s.MeanDelivered >= 0.85 * t ? 1 : 0;
            int fresh = s.MeanSpoiled <= level.SpoilMax ? 2 : s.MeanSpoiled <= 2 * level.SpoilMax ? 1 : 0;
            int robust = s.Lower >= t ? 2 : s.Lower >= 0.85 * t ? 1 : 0;
            return new[] { onTime, fresh, robust };
        }
    }
}
