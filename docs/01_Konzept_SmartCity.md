# 01 – Konzept: „Pingopolis“ – Pinguine in der Smart City

> Weiterentwicklung von *Simulation for Everyone – Konzeptvorstellung* (30.09.2026).
> Ziel bleibt unverändert: **Ein Spiel für Siemens-Mitarbeiter, das spielerisch erklärt, wie Simulationen funktionieren und wie sie in realen Siemens-Use-Cases angewendet werden.**
> Geändert wird das Setting: von der Pinguinkolonie in der Antarktis zur **Pinguin-Smart-City**.

---

## 1. Warum Smart City statt Antarktis?

| Aspekt | Antarktis (bisher) | Smart City (neu) |
|---|---|---|
| Nähe zu Siemens | indirekt (Analogien) | **direkt**: Gebäude, Energienetze, Bahn, Fabrik, Digitaler Zwilling sind Siemens-Kerngeschäft |
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
7. **Zwei Tiefen.** Standardmodus ohne Formeln für alle Mitarbeitenden; *Ingenieursmodus* blendet Gleichungen, Matrix und Konvergenzplot ein.

---

## 3. Didaktisches Grundgerüst (unverändert)

```
Modellierung → Physikalische Gleichung → Systemaufbau → Lösung → Optimierung → (Transfer)
```

Jedes Minigame durchläuft alle Schritte, **betont aber einen Schwerpunkt** (siehe [03_Minigames.md](03_Minigames.md)). Nach jedem Minigame folgt ein **Transfer-Screen**: links das Ergebnis des Spielers, rechts ein echtes Siemens-Beispiel mit derselben Methode.

**Zentrale Erkenntnis aus der Konzeptvorstellung** – bleibt der rote Faden:
Der *Systemaufbau* (lokale Beziehungen → Gesamtsystem) steckt in jeder Simulation.
- Kontinuierliche Probleme (Luft, Wärme im Asphalt, Stahl) müssen erst **diskretisiert** werden → Simcenter-Welt.
- Natürlich diskrete Systeme (Fabrik, Bahnbetrieb, Stromnetzknoten) sind es bereits → Plant-Simulation-Welt.

Die Smart City macht das besonders sichtbar: Wärmenetz eines Viertels, Stromnetz und Brückenfachwerk führen **auf dieselbe Matrixstruktur**. Dieses „Aha“ wird im Spiel explizit inszeniert (Zwilli erkennt das Muster wieder, siehe Story).

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

- **Session-Länge:** ein Minigame 8–12 Min., gesamte Story ca. 90 Min. – passt in eine Mittagspause bzw. ein Lern-Event.
- **Fortschritt:** Jedes gelöste Viertel stellt ein Fragment des Digitalen Zwillings wieder her und verändert die Stadt sichtbar (Bäume wachsen, Bahn fährt, Lichter gehen an).
- **Sammeln:** *Simulations-Notizbuch* (Glossar, Formeln, Transfer-Karten), kosmetische Belohnungen (Mützen, Schals, Helme für den Spieler-Pinguin).
- **Optimierung/Wiederspielwert:** Rundenbasierte Bestwerte, optional Leaderboard (siehe offene Punkte in der Tech-Spec).

---

## 5. Umfang & Vertical Slice

Die Konzeptvorstellung sieht 2–3 Minigames für den ersten Umfang vor. Empfehlung:

| Paket | Inhalt | Begründung |
|---|---|---|
| **Vertical Slice** | Hub-Ausschnitt (Zentrum + 2 Viertel), **MG1 Hitzeinsel** + **MG4 Smart Harbor**, 2 Transfer-Screens, Dialoge Akt 1 | Deckt genau die Siemens-Trennung *kontinuierlich (Simcenter) vs. diskret (Plant Simulation)* ab |
| **Release 1** | + Prolog (Huddle), **MG6 Stromnetz** | Stromnetz nutzt denselben Solver wie MG1 → günstig, und liefert das „gleiche Matrix“-Aha |
| **Release 2** | + MG2 Windschneise, MG3 Hochbahn-Brücke, MG5 Pinguin-Express, Finale | Vollständige Story |

---

## 6. Dokumente

| Datei | Inhalt |
|---|---|
| [01_Konzept_SmartCity.md](01_Konzept_SmartCity.md) | Dieses Dokument – Vision, Säulen, Umfang |
| [02_Story_und_Charaktere.md](02_Story_und_Charaktere.md) | Welt, Storyline, Charaktere, Dialogton |
| [03_Minigames.md](03_Minigames.md) | Alle Minigames inkl. Simulationsmodell, Spielablauf, Transfer |
| [04_Technische_Spezifikation.md](04_Technische_Spezifikation.md) | Architektur, Unity/Blender-Pipeline, Full-Code-Workflow, Tests, Hosting |
