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

## Stack (Kurzfassung)

Unity 6 LTS (URP, WebGL) · Blender LTS (FBX) · reiner C#-Simulationskern als UPM-Paket (`dotnet test`-fähig) · Python-Referenz zur Validierung · ink-Dialoge · GitHub Actions
