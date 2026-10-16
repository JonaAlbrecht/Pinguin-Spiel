# Pinguin-Spiel – Hinweise für KI-gestützte Entwicklung

Lernspiel für Siemens-Mitarbeitende **ohne technischen Hintergrund**. Leitplanke: `docs/00`. Aktuelle Arbeit: `specs/001-mg1-vertical-slice/spec.md`.

## Befehle
- Python-Referenz + Text-Lint: `pip install -r sim-reference/requirements.txt && python tools/text_lint.py && (cd sim-reference && python -m pytest -q)`
- Golden Files neu erzeugen (nach Modell- oder Leveländerung): `cd sim-reference && python generate_golden.py`
- C#-Kern: `dotnet test "sim-core/com.sfe.simcore/Dotnet~/Tests/SFE.SimCore.Tests.csproj"`
- Unity: siehe `unity/PinguinKolonie/README.md` (hier nicht lauffähig)

## Regeln, die nicht offensichtlich sind
- `sim-core/.../Runtime` hat **keine** `UnityEngine`-Abhängigkeit: netstandard2.1, C# 9, kein `record`/`init`, kein `System.Text.Json`, keine Threads. JSON nur über `SFE.SimCore.Data.MiniJson`.
- Jede Modelländerung zuerst in `sim-reference/simref/*.py`, dann Golden Files neu erzeugen, dann C# anpassen, bis `dotnet test` grün ist.
- Leveldaten liegen nur einmal: `unity/PinguinKolonie/Assets/_Project/Resources/Data/` (Python, dotnet-Tests und Unity lesen dieselben Dateien).
- Spieltexte nur in den JSON-Dateien unter `Resources/Data/` – nie Formeln oder Fachwörter (Text-Lint bricht sonst ab). Technische Texte nur im Modus „Blick unter die Haube“.
- Die Unity-Schicht baut alles per Code; keine Szenen- oder Prefab-YAML von Hand bearbeiten.
- In Unity-Texten nur Zeichen verwenden, die die eingebaute Schrift hat (keine Emoji, keine Symbole wie ★ ✔ ▸).
- Siemens-Produktnamen und -Beispiele sind Vorschläge und brauchen eine Freigabe (Feld `approval` der Transfer-Karte).
