namespace SFE.SimCore.Thermal
{
    /// <summary>Klaras Kommentar zur Runde, im Vergleich zur Vorrunde.</summary>
    public enum RoundVerdict { Better, Cheaper, Worse, Same }

    /// <summary>Unterschied zweier Runden – Grundlage für den Rundenvergleich in Schritt 5 („Optimieren heißt vergleichen“).</summary>
    public struct RoundComparison
    {
        public int CosyDelta;
        public int MeltDelta;
        public int LanternDelta;
        public RoundVerdict Verdict;

        public bool Unchanged => CosyDelta == 0 && MeltDelta == 0 && LanternDelta == 0;
    }

    /// <summary>
    /// Spielregeln rund um die Runden von MG1 Schritt 5. Liegen im Kern statt in der Unity-Schicht, damit sie ohne
    /// Unity testbar sind (Architektur-Gesetz: was ein Ergebnis berechnet, gehört in den Kern). Keine Physik –
    /// darum ohne Python-Referenz, aber mit NUnit-Tests (ADR-09).
    /// </summary>
    public static class SettlementRounds
    {
        /// <summary>
        /// F-02: Es zählt die beste Runde. Besser heißt mehr Sterne; bei gleich vielen Sternen mehr gemütliche Iglus.
        /// Gleichstand ist nicht besser – die zuerst erreichte Runde bleibt stehen.
        /// </summary>
        public static bool IsBetter(SettlementResult candidate, SettlementResult best)
        {
            if (best == null) return true;
            if (candidate.Stars != best.Stars) return candidate.Stars > best.Stars;
            return candidate.Comfortable > best.Comfortable;
        }

        public static RoundComparison Compare(SettlementResult before, SettlementResult now)
        {
            var c = new RoundComparison
            {
                CosyDelta = now.Comfortable - before.Comfortable,
                MeltDelta = now.Melted - before.Melted,
                LanternDelta = now.Lanterns - before.Lanterns,
            };
            if (now.Stars > before.Stars || (now.Stars == before.Stars && c.CosyDelta > 0)) c.Verdict = RoundVerdict.Better;
            // „Gut gewirtschaftet“ nur, wenn weniger Laternen nichts verschlechtert haben – auch nicht beim Schmelzen.
            else if (now.Stars == before.Stars && c.CosyDelta >= 0 && c.MeltDelta <= 0 && c.LanternDelta < 0) c.Verdict = RoundVerdict.Cheaper;
            else if (now.Stars < before.Stars || c.CosyDelta < 0) c.Verdict = RoundVerdict.Worse;
            else c.Verdict = RoundVerdict.Same;
            return c;
        }

        /// <summary>
        /// Voller Balken der Iglu-Waage: der größte Wärmedurchfluss eines Iglus dieser Runde. Eingeschwungen ist jede
        /// Waage im Gleichgewicht; erst die gemeinsame Skala zeigt, welches Iglu viel und welches wenig Wärme bekommt.
        /// 0, wenn kein Iglu stehen geblieben ist.
        /// </summary>
        public static double BalanceScale(SettlementGrid grid, SettlementBalance balance)
        {
            double max = 0;
            for (int i = 0; i < grid.CellCount; i++)
                if (grid[i] == Cell.Igloo && balance.In(i) > max) max = balance.In(i);
            return max;
        }

        /// <summary>Hat ein Malstrich etwas verändert? Nur dann gibt es einen Rückgängig-Schritt.</summary>
        public static bool Changed(string[] before, string[] after)
        {
            if (before == null || after == null || before.Length != after.Length) return true;
            for (int i = 0; i < before.Length; i++) if (before[i] != after[i]) return true;
            return false;
        }
    }
}
