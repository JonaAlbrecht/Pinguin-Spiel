# Pinguin-Spiel – „Vom Huddle zur Siedlung“

*Simulation for Everyone*: Ein Pinguinspiel, das Siemens-Mitarbeitenden **ohne technischen Hintergrund** spielerisch erklärt, wie Simulationen funktionieren und wo Siemens sie einsetzt.

Im Zentrum stehen die Minigame-Ideen der Kolleg:innen: Eine Pinguin-Kolonie wächst vom Huddle zur ersten Siedlung.

| # | Minigame | Thema | Siemens-Bezug (Vorschlag) |
|---|---|---|---|
| MG1 | Huddle – Die erste Siedlung | Wärme | Schaltschrank: SIMARIS therm, Simcenter 3D |
| MG2 | Schlitten – Der Polar-Express (Bonus: Fischfang / Schaltschrank-Kühlung) | Strömung | Velaro Novo (Siemens Mobility) bzw. Simcenter Flotherm |
| MG3 | Fischfabrik | Prozesse | Tecnomatix Plant Simulation |

Die früher ausgearbeitete Smart City **Pingopolis** ist jetzt eine optionale **Erweiterung**: Die Siedlung wächst zur Stadt.

## Dokumentation

| Dokument | Inhalt |
|---|---|
| [docs/00_Zielgruppe_und_Siemens-Bezug.md](docs/00_Zielgruppe_und_Siemens-Bezug.md) | **Leitplanke:** Zielgruppe, Lernziele, Siemens-Landkarte, Sprachregeln, Abnahme-Kriterien |
| [docs/01_Konzept_Kolonie.md](docs/01_Konzept_Kolonie.md) | Vision, Design-Säulen, Core Loop, Umfang, offene Entscheidungen |
| [docs/02_Story_und_Charaktere.md](docs/02_Story_und_Charaktere.md) | Welt, Storyline (Prolog – Akt 3 – Epilog), Charaktere, Dialogton |
| [docs/03_Minigames.md](docs/03_Minigames.md) | 3 Kern-Minigames (+ 1 Bonus) – je „Für Spielende“ (ohne Formeln) und „Technik“ (Entwicklerteam) |
| [docs/04_Technische_Spezifikation.md](docs/04_Technische_Spezifikation.md) | Architektur, Unity/Blender-Pipeline, Full-Code-Workflow, Tests, Hosting |
| [docs/05_Erweiterung_Pingopolis_SmartCity.md](docs/05_Erweiterung_Pingopolis_SmartCity.md) | **Erweiterung:** Smart City Pingopolis – Konzept, Story, 8 weitere Minigames |
| [docs/06_Open_Questions_Prototype.md](docs/06_Open_Questions_Prototype.md) | Offene Fragen für User- und Tech-Spezifikation des ersten Prototyps |
| [specs/001-mg1-vertical-slice/spec.md](specs/001-mg1-vertical-slice/spec.md) | **Prototyp v0.1:** Anforderungen mit Prüfart, Annahmen zu den offenen Fragen, bekannte Grenzen |
| [specs/002-v0.2-lernen-und-leben/spec.md](specs/002-v0.2-lernen-und-leben/spec.md) | **v0.2:** neue Figuren (Theo, Klara, Flocke, sichtbarer ZWILLI), Was-wäre-wenn, Iglu-Waage, Ton, Notizbuch, Prüfschritte |
| [.agency/](.agency/README.md) | Arbeitsweise (Rollen, Definition of Done, Entscheidungen, Review von v0.1) nach dem Agency Blueprint |

## Stack (Kurzfassung)

Unity 6 LTS (URP, WebGL) · Blender LTS (FBX) · reiner C#-Simulationskern als UPM-Paket (`dotnet test`-fähig) · Python-Referenz zur Validierung · ink-Dialoge · GitHub Actions

## Prototyp v0.1 – Vertical Slice

Titel → Hub „Huddle-Platz“ → Prolog „Der letzte Huddle“ → MG1 „Die erste Siedlung“ (Schritte 1–5) → „So macht's Siemens“ (Entwurf) → Siedlung im Hub.

| Teil | Ordner | Stand |
|---|---|---|
| Python-Referenz (Wärmemodelle, Balancing) | `sim-reference/` | getestet (pytest) |
| Golden Files | `golden/` | aus der Python-Referenz erzeugt |
| C#-Simulationskern (UPM-Paket, ohne UnityEngine) | `sim-core/com.sfe.simcore/` | getestet (`dotnet test`, Abgleich mit Golden Files) |
| Unity-Spiel (alles per Code gebaut) | `unity/PinguinKolonie/` | kompiliert gegen Unity 6 (`tools/unity_compile_check`), **noch nicht im Editor gespielt** |
| Text-Lint (Sprachregeln für Nicht-Techniker, auch Texte im C#-Code) | `tools/text_lint.py` | läuft in CI |

Starten in Unity: [unity/PinguinKolonie/README.md](unity/PinguinKolonie/README.md).
Tests ohne Unity:

```
pip install -r sim-reference/requirements.txt
python tools/text_lint.py
cd sim-reference && python -m pytest -q && cd ..
dotnet test "sim-core/com.sfe.simcore/Dotnet~/Tests/SFE.SimCore.Tests.csproj"
powershell -ExecutionPolicy Bypass -File tools/unity_compile_check/check.ps1   # nur mit installiertem Unity 6
```

> **Hinweis Klassifizierung:** Dieses GitHub-Repository ist öffentlich, die Konzeptdokumente gelten laut docs/04 als „Restricted“ (offene Fragen 0-03 und N-01 in docs/06).
