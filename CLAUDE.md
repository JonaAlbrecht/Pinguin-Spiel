# Pinguin-Spiel – Hinweise für KI-gestützte Entwicklung

Lernspiel für Siemens-Mitarbeitende **ohne technischen Hintergrund**. Leitplanke: `docs/00`. Aktuelle Arbeit: `specs/001-mg1-vertical-slice/spec.md`.

## Befehle
- Python-Referenz + Text-/Stil-Lint: `pip install -r sim-reference/requirements.txt && python tools/text_lint.py && python tools/style_lint.py && (cd sim-reference && python -m pytest -q)`
- Golden Files neu erzeugen (nach Modell- oder Leveländerung): `cd sim-reference && python generate_golden.py`
- C#-Kern: `dotnet test "sim-core/com.sfe.simcore/Dotnet~/Tests/SFE.SimCore.Tests.csproj"`
- Unity: siehe `unity/PinguinKolonie/README.md`
- Unity-Prüfung ohne Editorstart (Paketliste + Kompilierung, Pflicht vor dem Push): `powershell -ExecutionPolicy Bypass -File tools/unity_compile_check/check.ps1`
- Paketliste (`Packages/manifest.json`) nur bei geschlossenem Unity ändern – ein offener Editor schreibt sie zurück
- Arbeitsweise, Definition of Done, Entscheidungen: `.agency/` (Agency Blueprint)

## Regeln, die nicht offensichtlich sind
- `sim-core/.../Runtime` hat **keine** `UnityEngine`-Abhängigkeit: netstandard2.1, C# 9, kein `record`/`init`, kein `System.Text.Json`, keine Threads. JSON nur über `SFE.SimCore.Data.MiniJson`.
- Jede Modelländerung zuerst in `sim-reference/simref/*.py`, dann Golden Files neu erzeugen, dann C# anpassen, bis `dotnet test` grün ist.
- Leveldaten liegen nur einmal: `unity/PinguinKolonie/Assets/_Project/Resources/Data/` (Python, dotnet-Tests und Unity lesen dieselben Dateien).
- Spieltexte nur in den JSON-Dateien unter `Resources/Data/` – nie Formeln oder Fachwörter (Text-Lint bricht sonst ab). Oberflächentexte in `Content/ui_de.json`, im Code über `Data.Ui("schlüssel", …)`. Technische Texte nur im Abschnitt `underTheHood` (`Data.Tech`), angezeigt im Modus „Blick unter die Haube“.
- Neue Regel im Modell = neues Golden File, und eine Mutationsprobe: Regel in einer Kopie brechen, ein Test muss rot werden.
- C#-Dateien nicht per Shell-Skript mit Backslashes ändern (die Bash-Brücke macht aus `\\n` ein `\n`) – Edit/Write nutzen und danach kompilieren.
- Die Unity-Schicht baut alles per Code; keine Szenen- oder Prefab-YAML von Hand bearbeiten.
- Aussehen nur über `ArtStyle.cs` (Schattierung, Detailgrad, Proportionen) und `Palette.cs` steuern, runde Formen über `Prims.Make`/`Shapes` – keine eigenen Segmentzahlen oder Glanzwerte im Code (docs/07). Nach Stiländerungen „Pinguin-Spiel → Stilprobe rendern“.
- Farben nur aus `Palette.cs`, und die nur aus dem Siemens-Farbschema (`Palette.Siemens`, Primärfarben zuerst) – kein `new Color(…)` im übrigen Code. Prüfung: `python tools/style_lint.py`.
- In Unity-Texten nur Zeichen verwenden, die die eingebaute Schrift hat (keine Emoji, keine Symbole wie ★ ✔ ▸).
- Siemens-Produktnamen und -Beispiele sind Vorschläge und brauchen eine Freigabe (Feld `approval` der Transfer-Karte).
