# 01 – Konzept: „Pingopolis“ – Pinguine in der Smart City

> Weiterentwicklung von *Simulation for Everyone – Konzeptvorstellung* (30.09.2026).
> Ziel bleibt unverändert: **Ein Spiel für Siemens-Mitarbeiter, das spielerisch erklärt, wie Simulationen funktionieren und wie sie in realen Siemens-Use-Cases angewendet werden.**
> Geändert wird das Setting: von der Pinguinkolonie in der Antarktis zur **Pinguin-Smart-City**.
> **Zielgruppe: Siemens-Mitarbeitende ohne technischen Hintergrund.** Zielgruppe, Lernziele und Siemens-Bezug sind in [00_Zielgruppe_und_Siemens-Bezug.md](00_Zielgruppe_und_Siemens-Bezug.md) festgelegt und haben Vorrang vor allen anderen Dokumenten.

---

## 1. Warum Smart City statt Antarktis?

| Aspekt | Antarktis (bisher) | Smart City (neu) |
|---|---|---|
| Nähe zu Siemens | indirekt (Analogien) | **direkt**: Jedes Viertel steht für ein Siemens-Geschäft (Smart Infrastructure, Digital Industries, Mobility, Siemens Xcelerator) – die Stadt *ist* eine spielbare Siemens-Landkarte |
| Verständlichkeit für Nicht-Techniker | Thermodynamik von Pinguinen ist abstrakt | Hitze im Viertel, Stau in der Bahn, volle Lager – das kennt jeder aus dem Alltag |
| Transfer-Screens | „Pinguinbucht ↔ Schiffsschraube“ | „Stadtviertel ↔ echtes Siemens-Projekt“ – der Sprung ist kleiner |
| Anzahl Use Cases | 4, danach schwer erweiterbar | jede Infrastruktur ist ein potenzielles Minigame (Wärme, Strömung, Struktur, Prozesse, Netze, Verkehr) |
| Open World | Eisschollen, wenig Abwechslung | Stadtviertel mit klarer Identität = natürliche Level-Struktur |
| Charme | Pinguine im Schnee | Pinguine im Schnee **plus** Gegensatz „primitive Kolonie → Hightech-Stadt“ als Story-Motor |

Die Antarktis geht nicht verloren: Sie wird zum **Prolog**. Das ursprüngliche *Huddle*-Minigame dient als Tutorial – die Pinguine wärmen sich auf der schmelzenden Eisscholle, bevor sie in die Stadt ziehen. Damit bleibt der bereits ausgearbeitete Didaktik-Kern erhalten.

---

## 2. Design-Säulen

Übernommen aus der Konzeptvorstellung, an das neue Setting angepasst:

| # | Entscheidung | Ausprägung in Pingopolis |
|---|---|---|
| 01 | **3D-Vogelperspektive**, fixe Kamera (Vorbild Animal Crossing) | Leicht geneigte Kamera (~50°), Stadt als „gewölbte“ Welt; in Minigames Wechsel auf Top-Down/Ortho für Klarheit |
| 02 | **Open World** mit eingebetteten Minigames | Stadt = Hub mit 6 Vierteln, jedes Viertel = ein Simulations-Thema |
| 03 | **Setting** | Schwimmende Smart City *Pingopolis*, kühl-skandinavisch, Schnee auf Solardächern, Seilbahnen, Drohnen |
| 04 | **Ästhetik** | Stylized High-Fidelity (Animal Crossing / Beacon Pines / Overcooked), weiche Formen, Palette-Texturen, Toon-Shading |

Zusätzliche Säulen:

5. **Erst simulieren, dann bauen.** Jede Mission zeigt den Unterschied zwischen „einfach ausprobieren“ (NPC Kalle Klotz) und „vorher simulieren“ (Spieler). Fehlschläge ohne Simulation sind sichtbar, lustig und nie bestrafend.
6. **Die Simulation ist das Spielzeug.** Heatmaps, Strömungslinien, Spannungsfarben und Warteschlangen sind keine Nachher-Grafik, sondern die eigentliche Spielfläche.
7. **Für Nicht-Techniker gebaut.** Keine Formeln, keine Abkürzungen, kein Zeitdruck. Ergebnisse als Farben, Ampeln und Sterne. Ein optionaler Modus „Blick unter die Haube“ zeigt technisch Interessierten mehr – er ist nie Voraussetzung.
8. **Jedes Viertel ein Siemens-Geschäft.** Nach jedem Minigame: „So macht's Siemens“ mit echtem Beispiel, Kundennutzen und einem Satz zum Mitnehmen („Das kannst du jetzt erzählen“).

---

## 3. Didaktisches Grundgerüst (unverändert)

```
Modellierung → Physikalische Gleichung → Systemaufbau → Lösung → Optimierung → (Transfer)
```

Im Spiel heißen die Schritte alltagssprachlich: **Was ist wichtig? → Welche Regel gilt? → Alles hängt zusammen → Der Computer probiert's aus → Besser machen → So macht's Siemens** (Details in Dokument 00, Abschnitt 5).

Jedes Minigame durchläuft alle Schritte, **betont aber einen Schwerpunkt** (siehe [03_Minigames.md](03_Minigames.md)). Nach jedem Minigame folgt ein **Transfer-Screen „So macht's Siemens“**: links das Ergebnis des Spielers, rechts ein echtes Siemens-Beispiel mit derselben Methode, dazu der Kundennutzen in Business-Sprache.

**Zentrale Erkenntnis aus der Konzeptvorstellung** – bleibt der rote Faden:
Der *Systemaufbau* (lokale Beziehungen → Gesamtsystem) steckt in jeder Simulation.
- Kontinuierliche Probleme (Luft, Wärme im Asphalt, Stahl) müssen erst **diskretisiert** werden → Simcenter-Welt.
- Natürlich diskrete Systeme (Fabrik, Bahnbetrieb, Stromnetzknoten) sind es bereits → Plant-Simulation-Welt.

Die Smart City macht das besonders sichtbar: Wärmenetz eines Viertels, Stromnetz und Brückenfachwerk folgen **derselben Logik** (intern: dieselbe Matrixstruktur). Im Spiel wird das ohne Mathematik inszeniert: ZWILLI erkennt das Muster wieder („Strom verteilt sich wie Wärme!“). Botschaft für Nicht-Techniker: *Wer Simulation einmal verstanden hat, erkennt sie überall – und darum kann Siemens sie in so vielen Branchen einsetzen.*

---

## 4. Spielablauf (Core Loop)

```
 ┌───────────────── Hub: Stadt erkunden ─────────────────┐
 │  NPC gibt Auftrag  →  Viertel betreten  →  Minigame   │
 │        ▲                                      │       │
 │        │                                      ▼       │
 │  Stadt verändert sich  ←  Transfer-Screen  ←  Ergebnis │
 │  (Viertel wird „smart“,   (Siemens-Beispiel)  + Score  │
 │   Zwilli-Fragment)                                     │
 └────────────────────────────────────────────────────────┘
        Optimierungs-Loop: Minigame erneut spielen → Bestwert / Leaderboard
```

- **Session-Länge:** ein Viertel ≤ 10 Min., gesamte Story ca. 90 Min. – passt in eine Mittagspause bzw. ein Lern-Event.
- **Fortschritt:** Jedes gelöste Viertel stellt ein Fragment des Digitalen Zwillings wieder her und verändert die Stadt sichtbar (Bäume wachsen, Bahn fährt, Lichter gehen an).
- **Sammeln:** *Notizbuch* mit **Siemens-Landkarte** (welches Viertel = welches Siemens-Geschäft), Glossar in Alltagssprache, Transfer-Karten und „Das kannst du jetzt erzählen“-Sätzen, kosmetische Belohnungen (Mützen, Schals, Helme für den Spieler-Pinguin).
- **Optimierung/Wiederspielwert:** Rundenbasierte Bestwerte, optional Leaderboard (siehe offene Punkte in der Tech-Spec).

---

## 5. Umfang & Vertical Slice

Die Konzeptvorstellung sieht 2–3 Minigames für den ersten Umfang vor. Empfehlung:

| Paket | Inhalt | Begründung |
|---|---|---|
| **Vertical Slice** | Hub-Ausschnitt (Zentrum + 2 Viertel), **MG1 Hitzeinsel** + **MG4 Smart Harbor**, 2 Transfer-Screens, Dialoge Akt 1 | Zwei Siemens-Geschäfte (Smart Infrastructure, Digital Industries), beide aus dem Alltag verständlich (Hitze, Stau); intern deckt es die Trennung *kontinuierlich (Simcenter) vs. diskret (Plant Simulation)* ab |
| **Release 1** | + Prolog (Huddle), **MG6 Stromnetz** | Stromnetz nutzt denselben Solver wie MG1 → günstig, und liefert das „Strom verteilt sich wie Wärme“-Aha |
| **Release 2** | + MG2 Windschneise, MG3 Hochbahn-Brücke, MG5 Pinguin-Express, Finale | Vollständige Story |

---

## 6. Dokumente

| Datei | Inhalt |
|---|---|
| [00_Zielgruppe_und_Siemens-Bezug.md](00_Zielgruppe_und_Siemens-Bezug.md) | **Leitplanke:** Zielgruppe, Lernziele, Siemens-Landkarte, Sprachregeln |
| [01_Konzept_SmartCity.md](01_Konzept_SmartCity.md) | Dieses Dokument – Vision, Säulen, Umfang |
| [02_Story_und_Charaktere.md](02_Story_und_Charaktere.md) | Welt, Storyline, Charaktere, Dialogton |
| [03_Minigames.md](03_Minigames.md) | Alle Minigames inkl. Simulationsmodell, Spielablauf, Transfer |
| [04_Technische_Spezifikation.md](04_Technische_Spezifikation.md) | Architektur, Unity/Blender-Pipeline, Full-Code-Workflow, Tests, Hosting |
