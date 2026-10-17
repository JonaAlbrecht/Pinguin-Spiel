---
name: pinguin-simulation-pull-request
description: Pull Request im Pinguin-Spiel (Siemens-Lernspiel, Unity) sauber erstellen – Branch, Gates, Commit, Push, PR-Beschreibung und IMMER eine Handprobe als Issue im GitHub-Projekt „Pinguin-Spiel“. Aufrufen bei "Pull Request", "PR erstellen", "PR aufmachen", "committen und pushen", "Handprobe anlegen", "nach main", oder sobald eine Änderung am Spiel fertig ist.
---

# Pull Request im Pinguin-Spiel

Repo `JonaAlbrecht/Pinguin-Spiel`, GitHub-Projekt **Pinguin-Spiel** (https://github.com/users/JonaAlbrecht/projects/2).
Kein `gh` auf diesem Rechner – alles GitHub-Seitige läuft über `github.py` in diesem Ordner.
Regeln und Gates kommen aus `CLAUDE.md` und `.agency/rules/definition_of_done.md`; dieser Skill setzt sie in Reihenfolge.

## Harte Regeln

1. **Jede Änderung, die man im Spiel sehen, hören oder bedienen kann, bekommt eine Handprobe im Projekt.**
   Unity läuft hier nicht automatisch; Compile-Check und Tests sagen nichts über Layout, Klickbarkeit, Ton, Optik.
   Ohne angelegte Handprobe gibt es keinen PR. Ausnahme nur bei reinen Änderungen an Doku, Python-Referenz
   oder Tests ohne Wirkung im Spiel – dann im PR ausdrücklich „Keine Handprobe nötig: …“ mit Grund.
2. **Kein Token in Ausgaben, Commits, Issues oder PR-Texten.** `github.py` liest ihn aus einer Datei und gibt ihn nie aus.
   Nie `cat` auf die Tokendatei, nie den Token in eine Kommandozeile schreiben.
3. **Vor jedem Push den PR-Status prüfen** (`github.py status`) – Jona merged zwischendurch selbst; ein Nachzügler auf
   einen schon gemergten Branch geht verloren. Dann neuen Branch von `origin/main`.
4. **Nie direkt auf `main`.** Nicht mergen, außer Jona sagt es ausdrücklich.
5. `Packages/manifest.json` nur bei geschlossenem Unity ändern. Szenen/Prefab-YAML nicht von Hand bearbeiten.

## Ablauf

### 1. Branch
```bash
git fetch origin
python .claude/skills/pinguin-simulation-pull-request/github.py status   # gibt es schon einen (gemergten) PR?
git switch -c feature/<kurz-und-deutsch> origin/main                     # oder fix/<…>
```

### 2. Gates – alle grün, sonst kein Commit
Python aus der Repo-`.venv` (`.venv/Scripts/python.exe`; die System-Pythons sind auf diesem Rechner teils kaputt).
```bash
.venv/Scripts/python.exe tools/text_lint.py
.venv/Scripts/python.exe tools/style_lint.py             # Farben nur aus Palette.cs / Siemens-Farbschema (docs/07)
.venv/Scripts/python.exe tools/render_formulas.py --check      # Formelbilder passen zur LaTeX-Quelle in ui_de.json
(cd sim-reference && ../.venv/Scripts/python.exe -m pytest -q)
dotnet test "sim-core/com.sfe.simcore/Dotnet~/Tests/SFE.SimCore.Tests.csproj"
powershell -ExecutionPolicy Bypass -File tools/unity_compile_check/check.ps1   # Pflicht vor dem Push
```
- Modelländerung: erst `sim-reference`, dann `generate_golden.py`, dann C# – und eine Mutationsprobe (Regel brechen → Test rot).
- Formel in `underTheHood.formulas` geändert: `render_formulas.py` ohne `--check` laufen lassen und die Bilder mitcommitten.
- Ergebnis (Anzahl Tests, „grün“) für die PR-Beschreibung notieren.

### 3. Selbstdurchsicht vor dem Commit
- `git status` und `git diff --stat`: nur gewollte Dateien. Keine Tokendateien, keine `Library/`, `Temp/`, `*.csproj` von Unity.
- Neue Assets unter `Assets/` haben ihre `.meta` (sonst würfelt jeder Rechner eine eigene GUID).
- Unity hat beim Öffnen nichts Fremdes eingetragen (Paketliste, Dateien in `Assets/`).
- Spieltexte nur in `Resources/Data/**.json`, Fachwörter nur unter `underTheHood` (Text-Lint prüft das).
- Größere Entscheidung → Eintrag in `.agency/memory/decisions.md` mit verworfener Alternative (ADR-Nummer fortlaufend;
  vorher `git fetch` und die höchste Nummer auf `origin/main` nachsehen – parallele PRs vergeben sonst dieselbe).
- Keine Farbe als `new Color(…)` außerhalb von `Palette.cs`/`ArtStyle.cs` – fehlt ein Name, ihn in `Palette.cs` aus
  `Palette.Siemens` ableiten.

### 4. Commit
Stil wie in der Historie (`git log`): deutsch, Betreff sagt *was* aus Spielersicht, Text sagt *warum*
(oft „Spieltest TT.MM.JJJJ: …“), Aufzählung der Änderungen, am Ende die Gates.
```
Rechentafel: Knöpfe während Dialogen gesperrt, Menü „Einstellungen“

Spieltest 17.10.2026: „Nächster Zeitschritt“ sah klickbar aus, während Grete sprach.
- …
Tests: Text-Lint, 56 pytest, 54 NUnit, Unity-Prüfung grün.

Co-Authored-By: <Attribution laut Systemhinweis>
```
Lieber mehrere thematische Commits als einen Sammelcommit. Dateien einzeln `git add`en, nicht `git add -A`.

### 5. Handprobe anlegen (immer, siehe Regel 1)
Checkliste als Datei schreiben (im Scratchpad, nicht im Repo) und anlegen:
```bash
python .claude/skills/pinguin-simulation-pull-request/github.py handprobe --title "<Thema>" --body-file <datei.md>
```
Das legt ein Issue „Handprobe: <Thema>“ mit Label `handprobe` an und stellt es im Projekt auf **Todo**.
Gute Handprobe:
- Kopf: Branch/PR, was die Gates schon abdecken – die Handprobe prüft nur, was ein Mensch im Spiel sieht.
- Nach Spielort gegliedert (Hub, Prolog/Rechentafel, Siedlung, Transfer, Menüs), je Punkt eine **Checkbox mit
  konkretem Schritt und erwartetem Ergebnis** („Solange Grete spricht, ist ‚Nächster Zeitschritt‘ ausgegraut“),
  nicht „Einstellungen testen“.
- Auch Rückwege prüfen (Einstellung wieder zurück, Fenster schließen, zweiter Durchlauf).
- Auflösung nennen, wenn Layout betroffen ist (1920×1080 und z. B. QHD im Game-View).
- Abschnitt „Auffällig, nicht Teil dieser Änderung“ für Beobachtungen nebenbei.

### 6. Push und PR
Hat sich `origin/main` bewegt: `git merge origin/main` in den Branch (kein Rebase eines schon gepushten Branches),
Konflikte so lösen, dass beide Seiten erhalten bleiben, danach **alle Gates erneut**.
```bash
python .claude/skills/pinguin-simulation-pull-request/github.py status   # nochmal direkt vor dem Push
git push -u origin <branch>
python .claude/skills/pinguin-simulation-pull-request/github.py pr --title "<Betreff>" --body-file <pr.md>
```
PR-Beschreibung (deutsch, für Jona als Leser):
```markdown
## Was und warum
<2–4 Sätze aus Spielersicht, Anlass (Spieltest/Wunsch)>

## Änderungen
- …

## Prüfung
- Gates: Text-Lint, render_formulas --check, N pytest, N NUnit, Unity-Prüfung (3 Varianten) – grün
- Handprobe: #<Issue> (im Projekt Pinguin-Spiel, Todo)

## Entscheidungen
ADR-<n> in .agency/memory/decisions.md – <ein Satz>

## Offen
<was bewusst nicht gemacht wurde>

<Attribution laut Systemhinweis>
```
Die Handprobe **nicht** mit „Closes #…“ verknüpfen – sie schließt Jona nach dem Spieltest, nicht der Merge.

### 7. Danach
- Dem Nutzer PR-Link und Handprobe-Link nennen, und was er im Editor zuerst anschauen soll.
- Befunde aus Jonas Handprobe → neuer Commit auf demselben Branch (vorher `status`!), Checkbox im Issue bleibt bei Jona.
