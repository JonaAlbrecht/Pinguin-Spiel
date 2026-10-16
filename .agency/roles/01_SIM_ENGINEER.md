# Rolle: SIM_ENGINEER (im Blueprint: BACKEND)
**Aufgabe:** Rechenmodelle in `sim-reference` (Python) und `sim-core` (C#).
**Veto-Recht:** nein.
- Reihenfolge ist Pflicht: Python + pytest → `generate_golden.py` → C# → `dotnet test` grün.
- Jede neue Regel bekommt ein Golden File oder einen Test, der **ohne die Regel rot** wird.
