# Pinguin-Spiel – „Pingopolis“

*Simulation for Everyone*: Ein Open-World-Pinguinspiel, das Siemens-Mitarbeitenden **ohne technischen Hintergrund** spielerisch erklärt, wie Simulationen funktionieren und wo sie bei Siemens eingesetzt werden – jetzt angesiedelt in einer **Pinguin-Smart-City** statt in der Antarktis.

## Dokumentation

| Dokument | Inhalt |
|---|---|
| [docs/00_Zielgruppe_und_Siemens-Bezug.md](docs/00_Zielgruppe_und_Siemens-Bezug.md) | **Leitplanke:** Zielgruppe, Lernziele, Siemens-Landkarte (Viertel ↔ Siemens-Geschäft), Sprachregeln, Abnahme-Kriterien |
| [docs/01_Konzept_SmartCity.md](docs/01_Konzept_SmartCity.md) | Vision, Design-Säulen, Core Loop, Umfang & Vertical Slice |
| [docs/02_Story_und_Charaktere.md](docs/02_Story_und_Charaktere.md) | Welt, Storyline (Prolog – Akt 3), Charaktere, Dialogton |
| [docs/03_Minigames.md](docs/03_Minigames.md) | 8 Minigames – je „Für Spielende“ (ohne Formeln) und „Technik“ (Entwicklerteam), mit Siemens-Geschäft und Satz zum Mitnehmen |
| [docs/04_Technische_Spezifikation.md](docs/04_Technische_Spezifikation.md) | Architektur, Unity/Blender-Pipeline, Full-Code-Workflow, Tests, Hosting |

## Stack (Kurzfassung)

Unity 6 LTS (URP, WebGL) · Blender LTS (FBX) · reiner C#-Simulationskern als UPM-Paket (`dotnet test`-fähig) · Python-Referenz zur Validierung · ink-Dialoge · GitHub Actions
