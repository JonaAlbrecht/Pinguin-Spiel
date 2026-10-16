# Definition of Done

Eine Aufgabe ist erst fertig, wenn **alle** Punkte erfüllt sind. „Fast fertig, nur die Tests fehlen“ ist nicht fertig.

## 1. Werkzeuge sind die Wahrheit (alle grün, sonst REJECT)

| Gate | Befehl | Was es schützt |
|---|---|---|
| Text-Lint | `python tools/text_lint.py` | Sprachregeln für Nicht-Techniker (docs/00 Abschnitt 5) – JSON **und** Texte im C#-Code |
| Python-Referenz | `cd sim-reference && python -m pytest -q` | Das Modell, gegen das alles andere geprüft wird |
| C#-Kern | `dotnet test "sim-core/com.sfe.simcore/Dotnet~/Tests/SFE.SimCore.Tests.csproj"` | Abgleich mit den Golden Files (≤ 1e-6 relativ) |
| Unity-6-Kompilierung | `tools/unity_compile_check/check.ps1` (nur mit installiertem Unity) | Spielschicht kompiliert gegen die echten Unity-6-DLLs, altes **und** neues Input System, ohne veraltete API |

Die ersten drei laufen auch in CI. Die Kompilierprüfung braucht einen lokalen Unity-Editor und ist darum Pflicht **vor dem Push**.

## 2. Tests

- Jede Modelländerung: zuerst `sim-reference`, dann Golden Files neu erzeugen, dann C#, bis `dotnet test` grün ist.
- Ein Test zu einem Fix muss **ohne den Fix fehlschlagen** – sonst beweist er nichts.
- Spiellogik ohne Unity-Abhängigkeit gehört in `sim-core` (testbar); Unity-Code bleibt dünn.

## 3. Spiel und Lernen

- Abnahmekriterium des PRODUCT_OWNER erfüllt und gegen docs/00 (Lernziele, Sprachregeln) geprüft.
- Neue Spieltexte nur in `Resources/Data/**.json`; neue Siemens-Inhalte nur als Entwurf (Feld `approval`).
- Verhalten, das nur im Editor prüfbar ist, steht als **manuelle Prüfung** in der Spec – mit Schritten, nicht nur „testen“.

## 4. Dokumentation

- Kommentare sagen *warum*, nicht *was*.
- Größere Entscheidungen als Eintrag in `memory/decisions.md` (mit verworfener Alternative).
- `memory/context.md` und die Spec sind auf dem neuen Stand.
