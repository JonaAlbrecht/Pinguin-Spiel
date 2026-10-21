# 01 – Konzept: Von der Kolonie zur Siedlung

> **Grundlage:** die Minigame-Ideen der Kolleg:innen (Loop-Seite, Stand 13.10.2026). Sie sind jetzt der **Kern des Spiels**.
> Die vorher ausgearbeitete Smart City **Pingopolis** ist kein Ersatz-Setting mehr. Sie ist eine **mögliche Fortsetzung** – siehe [05_Erweiterung_Pingopolis_SmartCity.md](05_Erweiterung_Pingopolis_SmartCity.md).
> Das Ziel bleibt gleich: **Ein Spiel für Siemens-Mitarbeitende ohne technischen Hintergrund, das spielerisch erklärt, wie Simulationen funktionieren und wo Siemens sie einsetzt.**
> Zielgruppe, Lernziele und Sprachregeln stehen in [00_Zielgruppe_und_Siemens-Bezug.md](00_Zielgruppe_und_Siemens-Bezug.md). Dieses Dokument hat Vorrang vor allen anderen.

---

## 1. Grundidee

> Einst kannten die Pinguine nur eine Strategie gegen die tödliche Kälte der Antarktis: den **Huddle** – das dichte Zusammenrücken im Wind. Doch die Evolution schläft nicht. Die Pinguine haben Werkzeuge gebaut, ein Stromnetz errichtet und gründen ihre erste Stadt.

Das Spiel zeigt diesen Sprung **von der Kolonie zur Siedlung** in drei Minigames. Jedes Minigame hat ein Simulations-Thema und einen Siemens-Bezug:

| # | Minigame | Storyline | Thema | Siemens-Bezug (Vorschlag*) |
|---|---|---|---|---|
| MG1 | **Huddle** | Die Pinguine wärmen sich oben auf dem Eis – und planen ihre erste Siedlung | Wärme | Schaltschrank: SIMARIS therm, Simcenter 3D |
| MG2 | **Schlitten oder Fischfang** | Fahrt zum Meer bzw. zum Fischfang – mit dem Polar-Express | Strömung | Velaro Novo (Siemens Mobility) bzw. Schaltschrank-Kühlung (Simcenter Flotherm) |
| MG3 | **Fischfabrik** | Eine Fischfabrik bauen | Prozesse | Tecnomatix Plant Simulation |

\* **Alle Produkt- und Projektbeispiele sind Vorschläge. Die Fachbereiche müssen sie vor der Verwendung bestätigen und freigeben.**

Hinweis zur Nummerierung: Auf der Loop-Seite heißen die Minigames 1, 2 und 4. Eine Nummer 3 gibt es dort nicht. Hier sind sie fortlaufend als MG1–MG3 nummeriert. Ob ein weiteres Minigame dazukommt, ist offen (siehe Abschnitt 7).

---

## 2. Warum die Kolonie im Zentrum steht

Der frühere Entwurf hatte die Antarktis zu einem kurzen Prolog gemacht und das Spiel in eine Smart City verlegt. Die Ideen der Kolleg:innen zeigen: Die Kolonie liefert schon drei klare Minigames mit eigenem Siemens-Bezug.

| Aspekt | Vorteil der Kolonie als Kern |
|---|---|
| Team-Ideen | Die Ideen der Kolleg:innen werden umgesetzt, nicht überschrieben |
| Umfang | 3 fokussierte Minigames statt 8 – realistisch für einen ersten Release |
| Story | „Vom Huddle zur Siedlung“ ist ein geschlossener, leicht verständlicher Bogen |
| Siemens-Bezug | Drei Geschäftsfelder: Elektrifizierung/Schaltanlagen (Smart Infrastructure), Mobility, Fabrikplanung (Digital Industries) – dazu Simulationssoftware von Digital Industries Software |
| Roter Faden | Die Minigames bauen aufeinander auf: Die Siedlung (MG1) braucht Fisch, der Zug (MG2) bringt ihn, die Fabrik (MG3) verarbeitet ihn |
| Erweiterung | Die Siedlung kann später zur Smart City Pingopolis wachsen. Die Ausarbeitung dazu bleibt vollständig nutzbar (Dokument 05) |

---

## 3. Design-Säulen

| # | Entscheidung | Ausprägung |
|---|---|---|
| 01 | **3D-Vogelperspektive**, fixe Kamera (Vorbild Animal Crossing) | Leicht geneigte Kamera (~50°) über Eisscholle und Siedlung; in Minigames Wechsel auf Top-Down für Klarheit |
| 02 | **Kleine Open World** mit eingebetteten Minigames | Hub = Eisscholle mit drei Orten: Huddle-Platz/Siedlung, Gleis zur Küste, Fischfabrik |
| 03 | **Setting** | Antarktische Kolonie im Aufbruch: Eis und Schnee, erste Werkzeuge, erstes Stromnetz, Industrie-Laternen, ein Zug über das Packeis |
| 04 | **Ästhetik** | Realistisch beleuchtet (seit 19.10.2026, ADR-27): physikalisches Licht, Himmel, Dunst, glaubwürdige Oberflächen; die Figuren bleiben freundliche, vereinfachte Pinguine. Regeln: [07_Stilregeln.md](07_Stilregeln.md). Vorher: Stylized mit Toon-Shading |
| 05 | **Erst simulieren, dann bauen** | Jede Mission zeigt: „einfach ausprobieren“ (Kalle Klotz) scheitert sichtbar und lustig, „vorher simulieren“ (Spieler) funktioniert |
| 06 | **Die Simulation ist das Spielzeug** | Wärmebild, Strömungslinien und Warteschlangen sind die eigentliche Spielfläche |
| 07 | **Für Nicht-Techniker gebaut** | Keine Formeln, keine Abkürzungen, kein Zeitdruck. Ergebnisse als Farben, Ampeln und Sterne. Modus „Blick unter die Haube“ ist optional |
| 08 | **Jedes Minigame ein Siemens-Geschäft** | Danach: „So macht's Siemens“ mit echtem Beispiel, Kundennutzen und einem Satz zum Mitnehmen |

---

## 4. Didaktisches Grundgerüst (unverändert)

```
Modellierung → Physikalische Gleichung → Systemaufbau → Lösung → Optimierung → (Transfer)
```

Im Spiel: **Was ist wichtig? → Welche Regel gilt? → Alles hängt zusammen → Der Computer probiert's aus → Besser machen → So macht's Siemens** (Details: Dokument 00, Abschnitt 5).

Jedes Minigame hat eine Schwerpunkt-Phase:

| Minigame | Schwerpunkt-Phase | Warum |
|---|---|---|
| MG1 Huddle | **Alles hängt zusammen** + **Besser machen** | Jeder wärmt seine Nachbarn → Gleichgewicht zwischen „warm genug“ und „schmilzt“ finden |
| MG2 Polar-Express | **Besser machen** | Die Zugnase formen, bis die Luft glatt vorbeiströmt |
| MG3 Fischfabrik | **Was ist wichtig?** + **Zufall** | Die Fabrik selbst aufbauen und erleben, dass ein einziger Testlauf nicht reicht |

**Roter Faden (aus dem ursprünglichen Konzept):** In jeder Simulation steckt derselbe Gedanke – viele kleine lokale Beziehungen ergeben zusammen das Gesamtsystem.
- **Wärme (MG1)** und **Luft (MG2)** sind „fließende“ Probleme. Der Computer teilt sie erst in kleine Kästchen → Simcenter-Welt.
- **Die Fabrik (MG3)** besteht schon aus einzelnen Stationen und Warteschlangen → Plant-Simulation-Welt.

Botschaft für Nicht-Techniker: *Wer Simulation einmal verstanden hat, erkennt sie überall – darum setzt Siemens sie in so vielen Branchen ein.*

---

## 5. Spielablauf (Core Loop)

```
 ┌──────────────── Hub: Eisscholle / Siedlung ────────────────┐
 │  NPC gibt Auftrag  →  Ort betreten  →  Minigame            │
 │        ▲                                   │               │
 │        │                                   ▼               │
 │  Siedlung wächst  ←  „So macht's Siemens“  ←  Ergebnis     │
 │  (Iglus, Gleis,      (Siemens-Beispiel)       + Sterne     │
 │   Fabrik)                                                  │
 └────────────────────────────────────────────────────────────┘
        Optimierungs-Loop: Minigame erneut spielen → Bestwert
```

- **Session-Länge:** ein Minigame ≤ 10 Min., ganzes Kernspiel ca. 30–40 Min. – passt in eine Mittagspause oder ein Lern-Event.
- **Fortschritt:** Jedes gelöste Minigame verändert die Siedlung sichtbar: Iglus und Laternen nach MG1, das Gleis zur Küste nach MG2, die Fabrik mit rauchendem Schornstein nach MG3.
- **Sammeln:** Notizbuch mit **Siemens-Landkarte** (3 Karten im Kernspiel), Glossar in Alltagssprache, „Das kannst du jetzt erzählen“-Sätze, kosmetische Belohnungen (Mützen, Schals, Helme).

---

## 6. Umfang & Releases

| Paket | Inhalt | Begründung |
|---|---|---|
| **Vertical Slice** | Hub-Ausschnitt (Huddle-Platz), **MG1 Huddle** komplett (mit Rechenschritt-Tafel in Schritt 2), 1 Transfer-Screen, Hub-Dialoge | Kleinster vollständiger Ausschnitt; zeigt alle didaktischen Phasen; nutzt den Wärme-Kern, der schon spezifiziert ist |
| **Release 1 – Kernspiel** | + **MG2 Polar-Express**, + **MG3 Fischfabrik**, ganze Story, Epilog | Alle Ideen der Kolleg:innen spielbar; drei Siemens-Geschäftsfelder |
| **Release 2 – Erweiterung (optional)** | Die Siedlung wächst zur Smart City **Pingopolis** | Vollständig ausgearbeitet in [05_Erweiterung_Pingopolis_SmartCity.md](05_Erweiterung_Pingopolis_SmartCity.md); wird erst nach Release 1 priorisiert |

---

## 7. Offene Entscheidungen

| # | Frage | Optionen | Empfehlung |
|---|---|---|---|
| 1 | **MG2: Schlitten/Zug oder Fischfang/Schaltschrank-Kühlung?** | A) Polar-Express → Siemens Mobility (Velaro Novo). B) Kühlluft im Schaltschrank → Simcenter Flotherm | **A als Hauptvariante** (die Kolleg:innen haben sie schon ausformuliert, und sie bringt ein drittes Geschäftsfeld). B als optionales Bonus-Level, weil es MG1 (Schaltschrank-Wärme) gut fortsetzt. Beide nutzen denselben Strömungs-Kern |
| 2 | **Gibt es ein Minigame 3?** | Auf der Loop-Seite fehlt Nummer 3 | Kolleg:innen fragen. Kandidaten aus dem Pingopolis-Fundus: Netz-Balance (Stromnetz der Siedlung) oder Brücke über eine Eisspalte |
| 3 | **Produktzuordnung** | SIMARIS therm, Simcenter 3D, Simcenter Flotherm, Velaro Novo, Tecnomatix Plant Simulation | Mit den Fachbereichen bestätigen (Pflicht vor jedem Transfer-Screen) |

---

## 8. Dokumente

| Datei | Inhalt |
|---|---|
| [00_Zielgruppe_und_Siemens-Bezug.md](00_Zielgruppe_und_Siemens-Bezug.md) | **Leitplanke:** Zielgruppe, Lernziele, Siemens-Landkarte, Sprachregeln |
| [01_Konzept_Kolonie.md](01_Konzept_Kolonie.md) | Dieses Dokument – Vision, Säulen, Umfang des Kernspiels |
| [02_Story_und_Charaktere.md](02_Story_und_Charaktere.md) | Welt, Storyline, Charaktere, Dialogton |
| [03_Minigames.md](03_Minigames.md) | Die drei Kern-Minigames inkl. Simulationsmodell, Spielablauf, Transfer |
| [04_Technische_Spezifikation.md](04_Technische_Spezifikation.md) | Architektur, Unity/Blender-Pipeline, Full-Code-Workflow, Tests, Hosting |
| [05_Erweiterung_Pingopolis_SmartCity.md](05_Erweiterung_Pingopolis_SmartCity.md) | **Spätere Erweiterung:** Smart City Pingopolis (Konzept, Story, 8 Minigames) |
