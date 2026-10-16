# Meta-Learnings

Jedes Learning: was hakte · Gegenmaßnahme für künftige Durchläufe.

## 2026-10-16 – Review v0.1 und Umsetzung v0.2

1. **Grüne Tests bewiesen nicht, was die Spec behauptete.** Drei Regeln (Kaskade, Windtabelle, Budget) ließen sich
   brechen, ohne dass ein Test rot wurde. → *Gegenmaßnahme:* Für jede neue Regel eine Mutationsprobe (Regel in einer
   Kopie brechen, Test muss rot werden) – steht jetzt in `roles/03_TEST_DEV.md`.
2. **„Kompiliert gegen Referenz-Assemblies“ hieß Unity 2021, Ziel war Unity 6.** → *Gegenmaßnahme:*
   `tools/unity_compile_check` kompiliert gegen den installierten Editor; Pflicht vor dem Push.
3. **Texte im Code entgingen dem Lint.** → *Gegenmaßnahme:* Lint prüft C#-Literale in Oberflächen-Aufrufen und ob
   jeder angeforderte Schlüssel existiert.
4. **Shell-Escapes verfälschten Code.** Über die Bash-Brücke wurde `\n` zu `\n`; Python-Ersetzungen schrieben echte
   Zeilenumbrüche in C#-Zeichenliterale. → *Gegenmaßnahme:* C#-Änderungen nur mit Edit/Write, nie per Skript mit
   Backslashes; danach immer kompilieren.
5. **Lösung vorweggenommen:** Erster Entwurf des Was-wäre-wenn nutzte die Beispiel-Siedlung aus Schritt 5.
   → *Gegenmaßnahme:* Bei jedem neuen Lehrmoment prüfen, ob er eine spätere Aufgabe verrät (Test
   `test_what_if_settlement_is_not_the_example_solution`).
6. **WebGL kennt keine Kulturdaten.** `CultureInfo("de-DE")` hätte im Browser-Build versagt. → *Gegenmaßnahme:*
   deutsches Zahlenformat von Hand (`GameData.German`).
7. **Ein Lehrmoment war physikalisch falsch – zweimal.** Die erste Iglu-Waage behauptete ein Ungleichgewicht, das das
   eingeschwungene Modell nie hat (QA-Peer-Review fand es); die Korrektur behauptete „lange Balken = warm“ (ein neuer
   Test fand es). → *Gegenmaßnahme:* Jede Erklärung, die das Spiel über das Modell gibt, bekommt einen Test, der die
   Aussage am Modell prüft – nicht nur einen, der den Code prüft.
8. **Peer-Review vor dem Commit lohnt sich.** Alle Werkzeuge waren grün, trotzdem fand der QA-Durchgang einen Blocker
   und sieben wichtige Befunde (Layout-Überlappung, doppelte Vorstellung, Quiz-Reihenfolge). → *Gegenmaßnahme:*
   `agent_inbox/QA_ENGINEER.yml` ist Pflicht vor jedem Merge, auch wenn die Gates grün sind.
