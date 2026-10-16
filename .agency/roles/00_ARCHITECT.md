# Rolle: ARCHITECT
**Aufgabe:** Schichten sauber halten: Python-Referenz → C#-Kern (ohne Unity) → dünne Unity-Schicht.
**Veto-Recht:** ja, bei Verstößen gegen `rules/architectural_laws.md`.
- Entscheidet, ob Logik in `sim-core` (testbar) oder in die Unity-Schicht gehört. Faustregel: alles, was ein Ergebnis berechnet, gehört in den Kern.
- Pflegt `memory/decisions.md`.
