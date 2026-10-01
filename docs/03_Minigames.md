# 03 – Minigames

## Übersicht

| # | Minigame | Viertel | Domäne | Schwerpunkt-Schritt | Simulationsmodell (im Spiel) | Siemens-Transfer (Vorschlag*) | Paket |
|---|---|---|---|---|---|---|---|
| 00 | **Huddle** (Prolog) | Alte Scholle | Wärme | Systemaufbau (Einführung) | Wärmenetzwerk, Pinguin = Knoten | Simcenter 3D Thermal | R1 |
| 01 | **Hitzeinsel** | Frostgarten | Wärme | Gleichung + Systemaufbau | 2D-Wärmeleitung mit Quellen/Senken, FV-Gitter | Simcenter 3D / Simcenter FLOEFD (Gebäude-/Stadtklima) | **VS** |
| 02 | **Windschneise** | Windkante | Strömung | Diskretisierung / Meshing | 2D-Potentialströmung auf Quadtree-Gitter | Simcenter STAR-CCM+ | R2 |
| 03 | **Hochbahn-Brücke** | Kanalbogen | Struktur | Lösung + Interpretation | 2D-Fachwerk-FEM (Direkte Steifigkeitsmethode) | Simcenter 3D / Simcenter Nastran | R2 |
| 04 | **Smart Harbor** | Fischhafen | Prozesse | Modellierung + Stochastik | Ereignisdiskrete Simulation (DES) | Tecnomatix Plant Simulation | **VS** |
| 05 | **Pinguin-Express** | Express-Ring | Bahnbetrieb | Optimierung | Zeitdiskrete Zugfolge- + Fahrgastsimulation | Siemens Mobility (CBTC / Betriebssimulation) | R2 |
| 06 | **Netz-Balance** | Voltkai | Energie | Systemaufbau (Analogie) | DC-Lastfluss (Kirchhoff) | PSS®SINCAL / Gridscale X | R1 |
| 07 | **Hitzewelle** (Finale) | Zwillingsturm | Gekoppelt | Co-Simulation / Digital Twin | Kopplung der Modelle 01, 04, 05, 06 | Siemens Xcelerator / Digital Twin, Simcenter Amesim | R2 |

\* Transfer-Beispiele sind Vorschläge und müssen mit den jeweiligen Siemens-Fachbereichen abgestimmt werden (Freigabe von Bildern/Projekten).
VS = Vertical Slice, R1/R2 = Release 1/2.

### Gemeinsames Phasen-Gerüst (für alle Minigames)

| Phase | Was der Spieler tut | UI-Element |
|---|---|---|
| **1 Modellierung** | Wählt aus „Einfluss-Karten“ die relevanten Faktoren (inkl. Ablenker wie „Farbe der Parkbank“) | Kartenauswahl, Frieda kommentiert |
| **2 Gleichung** | Puzzle: Bausteine einer Bilanzgleichung in Slots legen (Standard: Symbole/Icons; Ingenieursmodus: Formel) | Drag-&-Drop-Gleichung |
| **3 Systemaufbau** | Sieht/baut die Kopplung der Knoten; Matrix füllt sich live mit | Overlay „Kopplungsfäden“ + Mini-Matrix |
| **4 Lösung** | Startet den Solver, beobachtet Iterationen (Heatmap breitet sich aus, Residuum fällt) | Solver-Animation, Konvergenzplot |
| **5 Optimierung** | Ändert Parameter in Runden mit Budget, vergleicht Ergebnisse | Rundenzähler, Bestwert, optional Leaderboard |
| **6 Transfer** | Liest Transfer-Karte: eigenes Ergebnis ↔ Siemens-Beispiel | Split-Screen, Karte ins Notizbuch |

Der *Schwerpunkt-Schritt* ist jeweils ausführlich spielbar, die anderen Phasen sind kurz (oft nur ein Klick oder eine Erklärung).

---

## MG00 – Huddle (Prolog)

Das ursprüngliche Konzept, als Tutorial gestrafft.

- **Szene:** Eisscholle im Schneesturm, 12–20 Pinguine.
- **Modell:** Jeder Pinguin ist ein Knoten mit Temperatur `T_i`. Wärmeaustausch mit Nachbarn (Kontakt) und Verlust an die Luft (Wind abhängig von Position am Rand).
  `C·dT_i/dt = q_körper + Σ_j G_ij (T_j − T_i) − h_i (T_i − T_luft)`
- **Spieler:** schiebt Pinguine in Formation; Windrichtung dreht sich.
- **Ziel:** Kein Pinguin unter Grenztemperatur für 60 s.
- **Lernziel:** „Jeder Knoten kennt nur seine Nachbarn – trotzdem ergibt sich ein Gesamtbild.“ Pinguine sind *von Natur aus diskret* – keine Diskretisierung nötig.

---

## MG01 – Hitzeinsel (Wärme) — *Vertical Slice*

**Auftrag (Flora):** „Der Spielplatz im Frostgarten wird mittags so heiß, dass die Küken nicht mehr rauswollen!“
**Kalles Versuch:** Stellt einen riesigen Ventilator auf → bläst nur heiße Luft herum, Strom fällt aus.

### Simulationsmodell
- **Domäne:** Stadtblock als 2D-Gitter, 32 × 32 Zellen à 4 m (Finite-Volumen).
- **Stationäre Energiebilanz je Zelle:**
  `0 = α_i·S  −  h_i (T_i − T_luft)  −  e_i  +  Σ_Nachbarn k (T_j − T_i)`
  - `α_i·S` Sonneneinstrahlung × Absorptionsgrad des Materials (Asphalt hoch, weißes Dach niedrig)
  - `h_i` Wärmeübergang (Wind, Verschattung)
  - `e_i` Verdunstungskühlung (Bäume, Gründach, Wasser)
  - `k` Kopplung zu Nachbarzellen (Leitung + vereinfachte Durchmischung)
- **System:** `A·T = b`, A ist dünnbesetzt, symmetrisch positiv definit (5-Punkt-Stern) → **Conjugate Gradient**.
- **Materialien (Kacheln):** Asphalt, Pflaster, Rasen, Baum, Gründach, weißes Dach, Wasserbecken, Solarüberdachung (verschattet + liefert Strom → Querverweis MG06).

### Spielablauf
1. **Modellierung:** Karten wählen: Sonne ✔, Material ✔, Wind ✔, Schatten ✔, *Farbe der Parkbank* ✘, *Anzahl der Laternen* ✘.
2. **Gleichung (Schwerpunkt):** Bilanz einer Zelle als Waage: „Rein“ (Sonne, warme Nachbarn) vs. „Raus“ (Wind, Verdunstung, kalte Nachbarn). Spieler legt Icons auf die richtige Seite.
3. **Systemaufbau (Schwerpunkt):** Zoom auf 3×3-Zellen: Spieler verbindet Nachbarn mit „Wärmefäden“; die zugehörigen Matrixeinträge leuchten auf. Danach wird automatisch der ganze Block gekoppelt (Animation: Matrix füllt sich, Bandstruktur wird sichtbar).
4. **Lösung:** Solver startet. Heatmap wird iterationsweise über die Stadt gelegt (Iterationen sind sichtbar → „Simulation rechnet sich ein“).
5. **Optimierung:** 3 Runden, Budget 1000 Fisch-Taler; Kacheln umbauen, neu rechnen.
6. **Transfer:** Spieler-Heatmap neben thermischer Gebäude-/Stadtklima-Simulation.

### Ziele & Score
- Hauptziel: Mittlere Temperatur auf der Spielplatzfläche ≤ 28 °C, keine Zelle > 35 °C.
- Score = Komfortpunkte − Kosten + Bonus für Effizienz (wenige Runden).
- **Sterne:** ★ Ziel erreicht, ★★ unter 80 % Budget, ★★★ zusätzlich Solarstrom ≥ X kWh.

---

## MG02 – Windschneise (Strömung / Meshing)

**Auftrag (Dr. Rotor):** „Zwischen den Türmen der Windkante gibt es fiese Böen. Ich brauche eine Windkarte für sichere Drohnenrouten!“
**Kalles Versuch:** Legt die Route „nach Gefühl“ → Drohne wird weggeweht, Fisch landet bei den Möwen.

### Simulationsmodell
- **Domäne:** 2D-Schnitt durch die Straßenschluchten (Draufsicht), 128 m × 128 m.
- **Physik (vereinfacht):** Potentialströmung, Stromfunktion `ψ` mit `∇²ψ = 0`, Gebäude als Hindernisse (Randbedingung `ψ = const`), Anströmung am Rand. Geschwindigkeit `u = ∂ψ/∂y, v = −∂ψ/∂x`.
  - Ehrlich kommuniziert: echte CFD löst Navier-Stokes (Wirbel, Turbulenz). Optional: vorberechnetes LBM-Referenzfeld aus Python für „So sähe es mit Turbulenz aus“.
- **Gitter:** Quadtree (Basis 8 × 8, max. 5 Verfeinerungsstufen), 2:1-Balance.
- **Solver:** derselbe CG-Solver wie MG01 (Laplace-Operator) → Wiederverwendung im Code und als Lernbotschaft.
- **Fehlermaß:** Abweichung zur vorberechneten Referenzlösung auf feinem Gitter.

### Spielablauf
1. **Modellierung:** Welche Gebäude sind relevant? Was ist die Anströmung?
2. **Gleichung:** kurz – „Luft geht nicht verloren“ (Massenerhaltung) als Bild.
3. **Diskretisierung (Schwerpunkt):** Spieler „malt“ Verfeinerung mit dem Pinsel. **Zellbudget** (z. B. 600 Zellen) = Rechenzeit. Eine Anzeige „Rechenzeit“ und „Genauigkeit“ schlägt bei jeder Änderung aus.
   - Lernmoment: Verfeinern an Gebäudeecken bringt viel, auf freier Fläche wenig.
4. **Lösung:** Stromlinien erscheinen als animierte Partikel.
5. **Optimierung/Test:** Drohnen fliegen die Route, die auf der *Spieler*-Windkarte basiert, durch das *Referenz*-Windfeld. Zu grobes Gitter → Drohne wird von unerwarteter Böe getroffen.
6. **Transfer:** Spieler-Quadtree neben einem echten CFD-Mesh (z. B. Polyeder-Mesh um eine Schiffsschraube oder ein Gebäude).

### Ziele & Score
- Alle 3 Lieferdrohnen erreichen ihr Ziel; Score = Genauigkeit × Effizienz (1 / Zellen).

---

## MG03 – Hochbahn-Brücke (Struktur)

**Auftrag (Bruno):** „Die Hochbahn muss über den Kanal. Die alte Brücke hat Boreas mitgenommen.“
**Kalles Versuch:** Drei Balken, viel Kleber → Brücke biegt sich, Zug bleibt stehen.

### Simulationsmodell
- **2D-Fachwerk-FEM**, Stäbe nur Zug/Druck.
  Element-Steifigkeit `k_e = (E·A/L)·[…]` (4×4 in globalen Koordinaten), Assemblierung zu `K·u = f`.
- Lagerbedingungen an den Ufern, Lasten = Zuggewicht an den Knoten der Fahrbahn (Wanderlast in 5 Positionen).
- **Solver:** Cholesky (klein, < 200 Freiheitsgrade) oder CG.
- **Versagen:** Spannung > Grenzspannung oder Knicken (Euler-Knicklast für Druckstäbe, vereinfacht).
- **Materialien:** Holz (billig, schwach), Stahl, Carbon (teuer, leicht).

### Spielablauf
1. **Modellierung:** Lager, Lasten, Material festlegen.
2. **Gleichung:** „Feder-Analogie“: Ein Stab ist eine Feder `F = k·Δx`.
3. **Systemaufbau:** Spieler zieht Stäbe zwischen Knoten (Poly-Bridge-artig); Matrix wächst mit.
4. **Lösung + Interpretation (Schwerpunkt):** Belastungstest; Stäbe färben sich (blau = Druck, rot = Zug, Dicke = Betrag), Verformung überhöht dargestellt.
   - **Interpretations-Quiz:** „Welcher Stab versagt zuerst?“, „Wo lohnt sich mehr Material?“, „Ist die Verformung realistisch oder überhöht?“ – Bonuspunkte.
5. **Optimierung:** Gewicht/Kosten minimieren bei Sicherheitsfaktor ≥ 1,5.
6. **Transfer:** Spannungsplot des Spielers neben einer echten FEM-Analyse (z. B. Brückenträger oder Drehgestell).

### Ziele & Score
- Zug fährt in allen Laststellungen sicher; Score = Sicherheitsfaktor-Ziel erfüllt + Kostenersparnis.

---

## MG04 – Smart Harbor (Prozesse / ereignisdiskret) — *Vertical Slice*

Weiterentwicklung der ursprünglichen *Fisch-Fabrik*.

**Auftrag (Olaf):** „Die Kutter liefern, die Robo-Robben fahren, aber die Kisten stapeln sich – und das Sushi für die Stadt kommt zu spät!“
**Kalles Versuch:** Kauft einfach doppelt so viele Robo-Robben → sie blockieren sich gegenseitig, Durchsatz sinkt.

### Simulationsmodell
- **Ereignisdiskrete Simulation** (Event-Queue, Simulationszeit springt von Ereignis zu Ereignis).
- **Objekte:** Quelle (Kutter, Ankunft ~ Exponentialverteilung), Puffer (Kühlregale, Kapazität), Stationen (Sortieren, Filetieren, Verpacken; Bearbeitungszeit ~ Dreiecks-/Normalverteilung, Störungen mit MTBF/MTTR), Förderer, AGVs (Robo-Robben, einfache Wegnetz-Reservierung), Senke (Auslieferung).
- **Stochastik:** Seed-basierter PCG32-Zufallsgenerator; mehrere **Replikationen** pro Szenario → Mittelwert und Konfidenzintervall.
- **KPIs:** Durchsatz/Stunde, Durchlaufzeit, Auslastung pro Station, Pufferfüllstände, Verderb (Fisch zu lange im Puffer).

### Spielablauf
1. **Modellierung (Schwerpunkt):** Spieler baut das Layout aus Bausteinen (Quelle, Puffer, Station, Förderband, AGV-Ladepunkt) – Plant-Simulation-artig, aber als 3D-Baukasten in der Halle.
2. **Gleichung:** Little's Law als Bild: *Bestand = Durchsatz × Durchlaufzeit*.
3. **Systemaufbau:** Verbindungen = Materialfluss; System ist schon diskret (Hinweis auf zentrale Erkenntnis).
4. **Lösung:** Simulation läuft im Zeitraffer (1 Schicht = 60 s). Engpass blinkt, Warteschlangen wachsen sichtbar.
5. **Stochastik (Schwerpunkt):** „Würfel nochmal“ – gleiches Layout, anderer Seed → anderes Ergebnis. Spieler lernt: ein Lauf reicht nicht. Knopf „10 Replikationen“ zeigt Verteilung als Histogramm.
6. **Optimierung:** Budget für Stationen/Puffer/AGVs; Ziel-Durchsatz bei minimalem Verderb.
7. **Transfer:** Spieler-Layout neben einem Plant-Simulation-Modell einer echten Fertigungslinie (Sankey/Gantt).

### Ziele & Score
- Durchsatz ≥ Zielwert im Mittel über 10 Replikationen, Verderb < 5 %.
- ★★★: zusätzlich untere Grenze des 90 %-Konfidenzintervalls ≥ Ziel („robust geplant“).

---

## MG05 – Pinguin-Express (Bahnbetrieb / Optimierung)

**Auftrag (Tilda):** „Zur Rushhour sind die Bahnsteige voll, und die Züge stehen im Stau vor den Signalen.“
**Kalles Versuch:** Mehr Züge reinschicken → Züge blockieren sich in den Blockabschnitten.

### Simulationsmodell
- Ringlinie mit 6 Stationen, zeitdiskrete Simulation (Δt = 1 s).
- **Zugfolge:** Festblock (feste Abschnitte, ein Zug pro Block) vs. **Moving Block** (Abstand = Bremsweg + Sicherheitsmarge) – Anknüpfung an CBTC.
- **Fahrdynamik:** vereinfachtes Beschleunigungs-/Bremsmodell, Energiebedarf ~ Beschleunigungen.
- **Fahrgäste:** Poisson-Ankünfte je Station mit Tagesganglinie, Haltezeit hängt von Ein-/Aussteigern ab.

### Spielablauf
1. **Modellierung:** Welche Größen sind Stellschrauben? (Zuganzahl, Takt, Haltezeit, Blocklänge, Höchstgeschwindigkeit)
2. **Gleichung:** Abstand = Reaktionsweg + Bremsweg `v²/(2a)`.
3. **Systemaufbau:** Züge sind Agenten auf einem Gleisnetz.
4. **Lösung:** Simulation eines Morgens im Zeitraffer, Zeit-Weg-Diagramm (Bildfahrplan) wächst live mit.
5. **Optimierung (Schwerpunkt):**
   - Spieler optimiert manuell (Regler) gegen zwei Ziele: Wartezeit ↓ und Energie ↓.
   - Danach: „Optimierer“-Knopf – ein einfacher Algorithmus (z. B. Hill-Climbing / genetischer Algorithmus, sichtbar als Punktwolke) sucht automatisch. Ergebnisse als **Pareto-Front**; Spieler sieht, wo sein eigener Punkt liegt.
   - Upgrade-Moment: Umstieg Festblock → Moving Block verschiebt die Pareto-Front sichtbar.
6. **Transfer:** Bildfahrplan des Spielers ↔ Betriebssimulation eines realen Metro-Systems mit CBTC.

### Ziele & Score
- Mittlere Wartezeit ≤ 3 min, keine Station überfüllt; Score = Abstand zur Pareto-Front.

---

## MG06 – Netz-Balance (Energie / Systemaufbau-Analogie)

**Auftrag (Volta):** „Mittags speisen alle Solardächer ein, abends kochen alle Fischsuppe – und meine Leitungen glühen!“
**Kalles Versuch:** Dickeres Kabel überall → Budget weg, Problem nur verschoben.

### Simulationsmodell
- **DC-Lastfluss:** Knoten (Häuser, PV, Batterie, Umspannwerk = Slack-Knoten), Leitungen mit Leitwert `B_ij`.
  `B·θ = P` (Knotenadmittanz-/Suszeptanzmatrix), Leitungsfluss `P_ij = B_ij (θ_i − θ_j)`.
- **Gleiche Struktur wie MG01:** gewichteter Graph-Laplace-Operator → ZWILLI-Aha: „Temperatur ↔ Spannungswinkel, Wärmeleitung ↔ Leitwert, Wärmequelle ↔ Einspeisung.“
- **Zeitverlauf:** 24-h-Profil in 1-h-Schritten (quasistationär), Batterie als zeitkoppelnder Speicher.
- **Solver:** CG (aus MG01) nach Elimination des Slack-Knotens.

### Spielablauf
1. **Modellierung:** Lastprofile und PV-Profile zuordnen.
2. **Gleichung:** Kirchhoff: „Was in einen Knoten fließt, fließt wieder hinaus.“
3. **Systemaufbau (Schwerpunkt):** Spieler verlegt Leitungen; **Split-Screen** zeigt die Matrix *neben* der Matrix aus MG01 – gleiche Muster. Mini-Quiz: „Welche Größe im Stromnetz entspricht der Temperatur?“
4. **Lösung:** Leitungen glühen nach Auslastung (grün → gelb → rot), Tag-Nacht-Zeitraffer.
5. **Optimierung:** Batterien platzieren, Leitungen verstärken, flexible Lasten (Fischsuppen-Kocher!) verschieben.
6. **Transfer:** Spielernetz ↔ Netzplanung eines städtischen Verteilnetzes.

### Ziele & Score
- Keine Leitung > 100 % in allen 24 Stunden; Score = Kostenersparnis + Eigenverbrauchsquote.

---

## MG07 – Hitzewelle (Finale / Co-Simulation)

- **Leitstand-Ansicht** im Zwillingsturm, Stadt als Miniatur-Diorama.
- Gekoppelte Modelle (vereinfachte, schnelle Varianten von MG01, MG04, MG05, MG06):
  ```
  Außentemperatur ─▶ MG01 Wärme ─▶ Kühlbedarf ─▶ MG06 Netzlast
                                       │              │
                                       ▼              ▼
                           MG05 Fahrgastaufkommen   MG04 Produktionsplan (Lastverschiebung)
  ```
- **Spieler:** trifft 4–5 Entscheidungen über 3 simulierte Tage (z. B. „Hafen-Schicht nachts statt mittags“, „Mehr Züge zum Hafenbad“). Vor jeder Entscheidung: Vorschau-Simulation durch ZWILLI („Was wäre, wenn …“).
- **Lernziel:** Digitaler Zwilling = gekoppelte Modelle + Was-wäre-wenn-Analysen vor dem Eingriff in die reale Welt.
- **Transfer:** Digital-Twin-Konzept (Siemens Xcelerator), System-/Multiphysik-Simulation (Simcenter Amesim).

---

## Transfer-Karten (Datenstruktur, inhaltlich)

Jede Transfer-Karte enthält:
- Titel + Ein-Satz-Botschaft („Was du gerade gemacht hast, heißt *Meshing* – so sieht es bei Siemens aus.“)
- Spieler-Screenshot (zur Laufzeit erzeugt)
- Freigegebenes Siemens-Bild/Video + Quellenangabe
- Werkzeug, Branche, 2–3 Fakten
- Link ins Intranet (optional, konfigurierbar)
