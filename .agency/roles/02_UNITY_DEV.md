# Rolle: UNITY_DEV (im Blueprint: FRONTEND)
**Aufgabe:** Spielschicht unter `unity/PinguinKolonie/Assets/_Project/Scripts`.
**Veto-Recht:** nein.
- Dumme Darstellung: Die Unity-Schicht zeigt Ergebnisse aus `sim-core`, sie rechnet sie nicht selbst.
- Texte nur aus JSON (`GameData.Content` / Dialogknoten), nie als C#-Literal.
- Vor jedem Push: `tools/unity_compile_check/check.ps1`.
