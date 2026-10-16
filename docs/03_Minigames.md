# 03 – Minigames

> Zielgruppe: Siemens-Mitarbeitende **ohne technischen Hintergrund** (siehe [00_Zielgruppe_und_Siemens-Bezug.md](00_Zielgruppe_und_Siemens-Bezug.md)).
> Grundlage: die Minigame-Ideen der Kolleg:innen (Huddle, Schlitten/Fischfang, Fischfabrik).
> Jedes Minigame ist zweigeteilt:
> **„Für Spielende“** beschreibt, was man sieht und tut – ohne Formeln.
> **„Technik (nur Entwicklerteam)“** beschreibt das Modell im Hintergrund. Der Spieler sieht davon nichts, außer im optionalen Modus „Blick unter die Haube“.
> Die Minigames der Smart-City-Erweiterung stehen in [05_Erweiterung_Pingopolis_SmartCity.md](05_Erweiterung_Pingopolis_SmartCity.md), Teil C.

## Übersicht

| # | Minigame | Thema | Siemens-Bezug (Vorschlag*) | Kernbotschaft (1 Satz) | Paket |
|---|---|---|---|---|---|
| MG1 | **Huddle** – Die erste Siedlung | Wärme | Schaltschrank: SIMARIS therm, Simcenter 3D | Wer vorher simuliert, weiß, wo es zu warm wird – bevor etwas schmilzt oder kaputtgeht. | **VS** |
| MG2 | **Schlitten** – Der Polar-Express | Strömung | Siemens Mobility: Velaro Novo | Der Computer stellt einen Zug in den Wind, bevor er gebaut wird. | R1 |
| MG2-B | *Bonus:* **Fischfang** – Kühlluft im Schaltschrank | Strömung | Simcenter Flotherm | Der Computer zeigt, wohin die kühle Luft strömt. | optional |
| MG3 | **Fischfabrik** | Prozesse | Tecnomatix Plant Simulation | Eine Fabrik erst im Computer laufen lassen zeigt Staus, bevor sie gebaut ist. | R1 |

VS = Vertical Slice, R1 = Release 1 (Kernspiel).
\* Alle Produkt-Zuordnungen sind Vorschläge und **freigabepflichtig** (siehe Dokument 00, Abschnitt 3).

### Gemeinsames Phasen-Gerüst

| Phase im Spiel | Was der Spieler tut | Darstellung |
|---|---|---|
| **1 Was ist wichtig?** | Wählt aus Karten die Dinge, die das Problem beeinflussen (mit lustigen Ablenkern) | Kartenauswahl, Frieda kommentiert |
| **2 Welche Regel gilt?** | Legt Bild-Symbole auf eine **Waage**: was kommt rein, was geht raus | Waage kippt sichtbar, kein Text-Rechnen |
| **3 Alles hängt zusammen** | Verbindet Teile mit **Fäden** und sieht, wie eine Änderung durchs Netz läuft | Fäden leuchten, Wellen breiten sich aus |
| **4 Der Computer probiert's aus** | Drückt „Simulieren“ und schaut zu | Farben, Pfeile, Figuren bewegen sich |
| **5 Besser machen** | Ändert in 3 Runden mit Budget, vergleicht | Ampel-Bewertung, Sterne |
| **6 So macht's Siemens** | Sieht das echte Siemens-Beispiel und den Satz zum Mitnehmen | Split-Screen, optional Kolleg:innen-Clip |

Pro Minigame ist **eine** Phase der Schwerpunkt (ausführlich spielbar). Die anderen Phasen dauern je 30–60 Sekunden.

**Regeln für alle Minigames:** kein Zeitdruck, kein Scheitern ohne Ausweg (nach 2 Fehlversuchen gibt Frieda einen Tipp, nach 3 eine Lösungshilfe), nur Maus/Touch, Ergebnis immer als Ampel + Sterne statt Zahlenkolonnen.

---

## MG1 – Huddle (Wärme) — *Vertical Slice*

**Storyline:** Die Pinguine wärmen sich oben auf dem Eis – und gründen ihre erste Siedlung.
**Siemens-Bezug:** Schaltschrank des Stromnetzes: thermische Simulation der Komponenten, damit keine Schäden durch Überhitzung entstehen (SIMARIS therm, Simcenter 3D – Vorschlag).
**Auftrag (Frieda):** „Die Laternen geben Licht und Wärme. Bau mit ihnen eine Siedlung, in der es gemütlich ist – aber nichts schmilzt!“
**Kalles Versuch:** Laternen direkt zwischen alle Iglus → die Iglus zerlaufen zu Schmelzwasser.

### Für Spielende

**Teil A – Der letzte Huddle (Prolog, Tutorial):**
Schneesturm auf der Eisscholle. Pip schiebt Pinguine so zusammen, dass niemand friert. Pinguine am Rand werden blau, in der Mitte rot-warm. Der Wind dreht – die Gruppe muss sich bewegen.
- **Ziel:** 60 Sekunden lang friert kein Pinguin (die Zeit läuft nur, wenn der Spieler nichts tut – kein Stress).
- **Aha:** Jeder Pinguin wärmt nur seine direkten Nachbarn, trotzdem entsteht ein warmer Kern.

**Teil B – Die erste Siedlung (Hauptteil):**
Die Siedlung ist ein Raster aus Feldern auf dem Eis. Bausteine: **Iglu**, **Industrie-Laterne** (Licht + Abwärme), **Schneewall** (Windschutz), **Eisfläche**, **Weg**.

1. **Was ist wichtig?** Karten: Laternen-Wärme ✔, Abstand ✔, Wind ✔, Schneewall ✔ – *Farbe der Iglus* ✘, *Musik im Dorf* ✘.
2. **Welche Regel gilt?** Ein Iglu als Waage. Rein: Wärme von Laternen und warmen Nachbarn. Raus: Wind und Kälte. Kippt die Waage zu weit nach „warm“, tropft das Iglu.
3. **Alles hängt zusammen (Schwerpunkt):** Spieler zieht Wärmefäden zwischen benachbarten Feldern – „wie beim Huddle: jeder wärmt seinen Nachbarn“. Dann verbindet der Computer die ganze Siedlung automatisch – hunderte Fäden in Sekunden („Genau dafür braucht man Computer“).
4. **Der Computer probiert's aus:** Ein Wärmebild legt sich über die Siedlung. Blau = zu kalt, grün = gemütlich, rot = schmilzt. Zu warme Iglus sacken sichtbar zusammen – mit Pfütze.
5. **Besser machen (Schwerpunkt) – „Der Gleichgewichtspunkt“:** 3 Runden. Strom-Budget für Laternen. Iglus, Laternen und Schneewälle verschieben, neu simulieren. Ziel: **möglichst viele Iglus**, die gemütlich sind und nicht schmelzen.
6. **So macht's Siemens:** Wärmebild der Siedlung neben dem Wärmebild eines echten Schaltschranks.

**Die Übersetzung im Transfer-Screen:**

| In der Siedlung | Im Schaltschrank |
|---|---|
| Laterne gibt Abwärme ab | Elektrische Bauteile werden im Betrieb warm |
| Iglu schmilzt ab 0 °C | Bauteile nehmen ab einer Höchsttemperatur Schaden |
| Abstand, Schneewälle, Anordnung | Anordnung der Bauteile, Lüftung, Abstände |
| Möglichst viele Iglus auf der Fläche | Möglichst viele Bauteile auf engem Raum – ohne Überhitzung |

Ehrlich vereinfacht: Im Schaltschrank zählt vor allem die Obergrenze („nicht zu heiß“). Die Untergrenze („gemütlich warm“) ist eine Spiel-Zutat.

- **Bewertung:** Ampeln für „Iglus gemütlich“ und „Nichts schmilzt“. ★ Ziel-Anzahl Iglus erreicht, ★★ dabei kein Iglu geschmolzen, ★★★ zusätzlich unter 80 % Strom-Budget („sparsam geplant“).
- **Das kannst du jetzt erzählen:** „Bevor ein Schaltschrank gebaut wird, simuliert man, wie warm die Bauteile werden. So geht nichts durch Überhitzung kaputt – und man spart teure Tests.“

### Technik (nur Entwicklerteam)
- **Teil A (Huddle):** Wärmenetzwerk, Pinguin = Knoten: `C·dT_i/dt = q + Σ_j G_ij (T_j − T_i) − h_i (T_i − T_luft)`, implizites Euler. Natürlich diskret.
- **Teil B (Siedlung):** 2D-Finite-Volumen-Gitter 24 × 24 (2 m), stationäre Bilanz je Zelle:
  `0 = q_i − h_i (T_i − T_luft) + Σ_N k_ij (T_j − T_i)` → `A·T = b`, A dünnbesetzt, SPD (5-Punkt-Stern) → Conjugate Gradient.
  - `q_i` > 0 nur in Laternen-Zellen; `h_i` klein hinter Schneewällen (Windschutz), groß auf freier Fläche.
  - Lufttemperatur −30 °C. Iglu „gemütlich“ bei −8 °C ≤ T < 0 °C, „schmilzt“ bei T ≥ 0 °C.
  - **Schmelz-Kaskade:** Nach dem Solve werden Iglu-Zellen mit T ≥ 0 °C zu Wasser-Zellen (andere `k`, `h`), dann neuer Solve – bis nichts mehr schmilzt (max. 5 Durchläufe). Bleibt linear, sieht aber dramatisch aus.
- Gleicher CG-Solver wie MG2-B und die Erweiterungs-Minigames Hitzeinsel und Netz-Balance.

---

## MG2 – Schlitten: Der Polar-Express (Strömung)

**Storyline:** Fahrt zum Meer. Tausende Kilometer zu Fuß durch Blizzards? Nicht mehr. Der schnellste Weg vom Iglu-Dorf zum offenen Eismeer führt über Schienen – doch ein normaler Zug kapituliert vor den brutalen Gegenwinden der Tundra.
**Siemens-Bezug:** Siemens Mobility – Züge wie der Velaro Novo werden aerodynamisch optimiert, bevor sie gebaut werden (Beispiel durch Fachbereich zu bestätigen).
**Auftrag (Tilda):** „Mein Schlitten-Zug kommt gegen den Wind nicht an. Mach ihn windschnittig!“
**Kalles Versuch:** Ein riesiger Schlitten mit Motor und flacher Front → er bleibt im Gegenwind fast stehen.

### Für Spielende
Mission: Werde Chefingenieur:in des Polar-Express. Forme Frontpartie und Konturen des Zuges, minimiere Verwirbelungen und bring die gefiederten Passagiere in Rekordzeit an die Küste.

1. **Was ist wichtig?** Karten: Form der Zugnase ✔, Gegenwind ✔, Übergänge zwischen den Wagen ✔ – *Farbe der Lok* ✘, *Anzahl der Fenster* ✘.
2. **Welche Regel gilt?** Bild: „Halt die Hand aus dem Autofenster: flach gegen den Wind drückt es stark, schräg gleitet sie durch.“ Luft verschwindet nicht – sie muss um den Zug herum.
3. **Alles hängt zusammen – „Genau hinschauen kostet Zeit“:** Der Computer teilt die Luft in Kästchen. Spieler wählt grob / mittel / fein. Zwei Anzeigen: „Wie genau?“ und „Wie lange rechnet's?“. Lernmoment: An der Zugnase lohnt sich Genauigkeit.
4. **Der Computer probiert's aus:** Strömungslinien fließen um den Zug. Glatte Linien = gut. Wirbel hinter kantigen Stellen drehen sich sichtbar (rot markiert).
5. **Besser machen (Schwerpunkt):** Spieler formt die Zugnase mit Ziehpunkten und glättet die Wagen-Übergänge. Zielkonflikt: Eine lange, spitze Nase ist windschnittiger, nimmt aber Platz für Passagiere weg. 3 Runden, danach fährt der Zug: Fahrzeit zur Küste und Anzahl Passagiere werden angezeigt (Ampel + Sterne).
6. **So macht's Siemens:** Strömungsbild des Polar-Express neben dem Strömungsbild eines echten Hochgeschwindigkeitszugs.

- **Bewertung:** ★ Küste erreicht in Ziel-Fahrzeit, ★★ dabei genug Plätze für alle Passagiere, ★★★ Bestzeit-Bereich („windschnittig geplant“).
- **Das kannst du jetzt erzählen:** „Bevor ein Zug gebaut wird, lässt man im Computer Luft um ihn strömen. So wird er windschnittig, schneller und braucht weniger Energie – ohne teure Tests im Windkanal.“

### Technik (nur Entwicklerteam)
- **2D-Lattice-Boltzmann (D2Q9, BGK)** im Längsschnitt des Zuges, Kanal 160 × 64 (fein) / 80 × 32 (grob), niedrige Reynolds-Zahl (im Spiel ehrlich als Vereinfachung benannt).
  - Warum nicht Potentialströmung (wie im alten Entwurf)? Sie liefert keinen Luftwiderstand und keine Wirbel – genau die braucht dieses Minigame.
- Geometrie: Zugnase als Bézier-Kurve mit 4 Ziehpunkten → Hindernis-Maske, Bounce-Back-Ränder.
- Luftwiderstand per Momentum-Exchange; Fahrzeit aus 1D-Modell `F_Zug = F_Widerstand(v)` → Endgeschwindigkeit → Fahrzeit = Strecke / v.
- Steppable: mehrere LBM-Schritte pro Frame, ganzer Lauf als Zeitraffer ≤ 10 s; Ziel ≤ 4 ms pro Frame in WebGL.
- Python-Referenz (numpy-LBM) erzeugt Golden Files für Widerstand typischer Formen; Toleranz 1e-4 relativ.

---

## MG2-B – Fischfang: Kühlluft im Schaltschrank (Bonus, optional)

Die zweite Option von der Loop-Seite für „Schlitten oder Fischfang“. Setzt MG1 fort: Frieda baut einen Schaltschrank für die Kühlhalle der Fischer – die Bauteile darin werden warm, und Lüfter sollen sie kühlen.
**Siemens-Bezug:** Strömungs- und Wärmesimulation von Elektronik und Schaltschränken (Simcenter Flotherm – Vorschlag).

### Für Spielende
Spieler platziert Lüfter, Lüftungsschlitze und Bauteile im Schrank. Die Simulation zeigt, wohin die kühle Luft strömt und wo sich warme Luft staut. Ziel: Alle Bauteile bleiben unter ihrer Höchsttemperatur.
- **Das kannst du jetzt erzählen:** „Mit Simulation sieht man, wohin die Kühlluft in einem Gerät strömt – so plant man die Lüftung, bevor es heiß wird.“

### Technik (nur Entwicklerteam)
Gleicher LBM-Kern wie MG2, ergänzt um Temperatur als mitgeführte Größe (Advektion-Diffusion, zweite Verteilungsfunktion). Wird erst nach Release 1 entschieden (siehe Dokument 01, offene Entscheidung 1).

---

## MG3 – Fischfabrik (Prozesse)

**Storyline:** Eine Fischfabrik bauen. Der Polar-Express bringt Fischerpinguine an die Küste, die Kutter liefern Fisch – jetzt muss er verarbeitet werden.
**Siemens-Bezug:** Digital Industries – Fabrikplanung mit Digitalem Zwilling (Tecnomatix Plant Simulation).
**Auftrag (Olaf):** „Die Kutter liefern, der Zug bringt Nachschub, aber die Kisten stapeln sich – und das Sushi für die Siedlung kommt zu spät!“
**Kalles Versuch:** Doppelt so viele Robo-Robben → sie stehen sich gegenseitig im Weg.

### Für Spielende
1. **Was ist wichtig? (Schwerpunkt):** Spieler baut die Fabrik aus Bausteinen: Anlegestelle, Kühlregal, Sortier-, Filetier- und Verpackungsstation, Förderband, Robo-Robben, Verladung zum Polar-Express. Wie ein kleines Aufbauspiel.
2. **Welche Regel gilt?** Bild: „Je länger etwas wartet, desto voller wird das Lager.“ (Supermarkt-Kassen-Vergleich)
3. **Alles hängt zusammen:** Pfeile zeigen den Weg des Fischs von Station zu Station.
4. **Der Computer probiert's aus:** Eine Schicht läuft im Zeitraffer (60 s). Wo es sich staut, wachsen Kistenstapel, die Station blinkt.
5. **Zufall (Schwerpunkt) – „Die Möwen-Frage“:** Die Kutter kommen nicht pünktlich, Maschinen machen mal Pause, Möwen klauen Fisch. Knopf „Noch ein Tag“ → gleicher Plan, anderes Ergebnis. Knopf „10 Tage simulieren“ zeigt gute und schlechte Tage als Balken. Lernmoment: Ein einziger Testlauf reicht nicht.
6. **Besser machen:** Mit Budget den Engpass finden und beheben.
7. **So macht's Siemens:** Die Fabrik des Spielers neben dem Digitalen Zwilling einer echten Fertigungslinie.

- **Bewertung:** ★ Sushi im Schnitt pünktlich, ★★ kaum verdorbener Fisch, ★★★ **auch an schlechten Tagen** pünktlich („robust geplant“).
- **Das kannst du jetzt erzählen:** „Bevor eine Fabrik gebaut oder umgebaut wird, lässt man sie im Computer laufen. So findet man Engpässe früh und spart teure Umbauten.“

### Technik (nur Entwicklerteam)
Ereignisdiskrete Simulation (Event-Heap). Quellen: Kutter mit Exponential-Ankünften, Polar-Express im festen Takt (Takt optional aus dem MG2-Ergebnis abgeleitet). Stationen: Dreiecks-/Normalverteilung, Störungen MTBF/MTTR; Puffer mit Kapazität; Robo-Robben mit Wegreservierung; Möwen als zufälliger Verlust. PCG32-Seeds, 10 Replikationen, intern Mittelwert + 90 %-Konfidenzintervall (★★★ = Untergrenze ≥ Ziel). KPIs: Durchsatz, Durchlaufzeit, Auslastung, Verderb.
Derselbe Kern wird in der Erweiterung für MG04 Smart Harbor wiederverwendet.

---

## Transfer-Karte „So macht's Siemens“ (Inhaltsvorgabe)

| Feld | Vorgabe |
|---|---|
| Überschrift | Alltagssprache, z. B. „Was du gemacht hast, macht Siemens für Fabriken auf der ganzen Welt“ |
| Siemens-Geschäft | z. B. „Siemens Digital Industries“ |
| Bild/Video | freigegebenes Material, Quellenangabe |
| Text | max. 60 Wörter, keine Abkürzungen ohne Erklärung |
| Kundennutzen | 1–3 Stichpunkte in Business-Sprache (Zeit, Kosten, Risiko, Energie/CO₂) |
| Produktname | dezent, als Zusatzinfo („Software: Tecnomatix Plant Simulation“) |
| Siemens-Stimme | optionaler 30–45-s-Clip einer Kollegin / eines Kollegen |
| „Das kannst du jetzt erzählen“ | genau ein Satz |
| Mehr erfahren | optionaler Intranet-Link |
| Freigabe | Fachbereich, Kommunikation, Datum (Pflichtfeld, sonst wird die Karte nicht gebaut) |
