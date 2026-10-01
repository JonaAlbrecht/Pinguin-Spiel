# 03 – Minigames

> Zielgruppe: Siemens-Mitarbeitende **ohne technischen Hintergrund** (siehe [00_Zielgruppe_und_Siemens-Bezug.md](00_Zielgruppe_und_Siemens-Bezug.md)).
> Jedes Minigame ist deshalb zweigeteilt:
> **„Für Spielende“** beschreibt, was man sieht und tut – ohne Formeln.
> **„Technik (nur Entwicklerteam)“** beschreibt das Modell im Hintergrund. Davon sieht der Spieler nichts, außer im optionalen Modus „Blick unter die Haube“.

## Übersicht

| # | Minigame | Siemens-Geschäft | Kernbotschaft (1 Satz) | Paket |
|---|---|---|---|---|
| 00 | **Huddle** (Prolog) | – (Einstieg) | Wenn jeder nur seine Nachbarn wärmt, bleibt trotzdem die ganze Kolonie warm – alles hängt zusammen. | R1 |
| 01 | **Hitzeinsel** | Smart Infrastructure (Gebäude) + DI Software | Wer vorher simuliert, plant kühle Plätze und sparsame Gebäude. | **VS** |
| 02 | **Windschneise** | Digital Industries Software | Der Computer kann Wind sichtbar machen – genauer hinschauen, wo es wichtig ist. | R2 |
| 03 | **Hochbahn-Brücke** | DI Software + Mobility | Bauteile werden virtuell belastet, bevor echter Stahl verbaut wird. | R2 |
| 04 | **Smart Harbor** | Digital Industries (Fabrik) | Eine Fabrik erst im Computer laufen lassen zeigt Staus, bevor sie gebaut ist. | **VS** |
| 05 | **Pinguin-Express** | Siemens Mobility | Kluge Steuerung bringt mehr Züge pünktlich auf dieselbe Strecke. | R2 |
| 06 | **Netz-Balance** | Smart Infrastructure (Netze) | Netzbetreiber sehen Engpässe voraus, bevor die Leitung glüht. | R1 |
| 07 | **Hitzewelle** (Finale) | Siemens Xcelerator / Digital Twin | Viele Modelle zusammen = Digitaler Zwilling: erst virtuell entscheiden, dann real handeln. | R2 |

VS = Vertical Slice, R1/R2 = Release 1/2. Produkt-Zuordnungen der Transfer-Screens: siehe Tabelle in Dokument 00, Abschnitt 3 (alle freigabepflichtig).

### Gemeinsames Phasen-Gerüst

| Phase im Spiel | Was der Spieler tut | Darstellung |
|---|---|---|
| **1 Was ist wichtig?** | Wählt aus Karten die Dinge, die das Problem beeinflussen (mit lustigen Ablenkern) | Kartenauswahl, Frieda kommentiert |
| **2 Welche Regel gilt?** | Legt Bild-Symbole auf eine **Waage**: was kommt rein, was geht raus | Waage kippt sichtbar, kein Text-Rechnen |
| **3 Alles hängt zusammen** | Verbindet Teile mit **Fäden** und sieht, wie eine Änderung durchs Netz läuft | Fäden leuchten, Wellen breiten sich aus |
| **4 Der Computer probiert's aus** | Drückt „Simulieren“ und schaut zu | Farben, Pfeile, Figuren bewegen sich |
| **5 Besser machen** | Ändert in 3 Runden mit Budget, vergleicht | Ampel-Bewertung, Sterne |
| **6 So macht's Siemens** | Sieht das echte Siemens-Beispiel und den Satz zum Mitnehmen | Split-Screen, optional Kolleg:innen-Clip |

Pro Minigame ist **eine** Phase der Schwerpunkt (ausführlich spielbar), die anderen dauern je 30–60 Sekunden.

**Regeln für alle Minigames:** kein Zeitdruck, kein Scheitern ohne Ausweg (nach 2 Fehlversuchen bietet Frieda einen Tipp, nach 3 eine Lösungshilfe), nur Maus/Touch, Ergebnis immer als Ampel + Sterne statt Zahlenkolonnen.

---

## MG00 – Huddle (Prolog)

### Für Spielende
Schneesturm auf der Eisscholle. Pip schiebt Pinguine so zusammen, dass niemand friert. Pinguine am Rand werden blau, in der Mitte rot-warm. Der Wind dreht – die Gruppe muss sich bewegen.
- **Ziel:** 60 Sekunden lang friert kein Pinguin (Zeit läuft nur, wenn der Spieler nichts tut – kein Stress).
- **Aha:** Jeder Pinguin wärmt nur seine direkten Nachbarn, trotzdem entsteht ein warmer Kern → „Alles hängt zusammen“.
- **Das kannst du jetzt erzählen:** „Auch komplizierte Systeme bestehen aus vielen kleinen Teilen, die sich gegenseitig beeinflussen – so rechnen Simulationen.“

### Technik (nur Entwicklerteam)
Wärmenetzwerk, Pinguin = Knoten: `C·dT_i/dt = q + Σ_j G_ij (T_j − T_i) − h_i (T_i − T_luft)`, implizites Euler. Natürlich diskret, keine Diskretisierung nötig.

---

## MG01 – Hitzeinsel — *Vertical Slice*

**Siemens-Bezug:** Smart Infrastructure (intelligente Gebäude) und Simulationssoftware von Digital Industries.
**Auftrag (Flora):** „Der Spielplatz im Frostgarten wird mittags so heiß, dass die Küken nicht mehr rauswollen!“
**Kalles Versuch:** Stellt einen Riesen-Ventilator auf → bläst nur heiße Luft herum, die Sicherung fliegt.

### Für Spielende
Der Stadtblock ist ein Raster aus Kacheln (Asphalt, Pflaster, Rasen, Baum, Gründach, weißes Dach, Wasserbecken, Solar-Sonnensegel).
1. **Was ist wichtig?** Karten: Sonne ✔, Bodenbelag ✔, Wind ✔, Schatten ✔ – *Farbe der Parkbänke* ✘, *Anzahl der Laternen* ✘.
2. **Welche Regel gilt? (Schwerpunkt)** Eine Kachel als Waage: Rein: Sonne, warme Nachbarn. Raus: Wind, Schatten, Verdunstung (Bäume „schwitzen“). Spieler legt Symbole auf die richtige Seite; die Kachel wird wärmer oder kühler.
3. **Alles hängt zusammen (Schwerpunkt):** Spieler zieht Wärmefäden zwischen benachbarten Kacheln; ein heißer Asphaltfleck „färbt“ sichtbar auf die Nachbarn ab. Dann verbindet der Computer den ganzen Block automatisch – Hunderte Fäden in Sekunden („Genau dafür braucht man Computer“).
4. **Der Computer probiert's aus:** Eine Wärmekarte legt sich über die Stadt, die Farben „pendeln sich ein“.
5. **Besser machen:** 3 Runden, Budget 1000 Fisch-Taler. Kacheln tauschen, neu simulieren.
6. **So macht's Siemens:** Spieler-Wärmekarte neben einer echten Gebäude-Simulation (z. B. Temperaturverteilung in einem Bürogebäude) + Bezug zu intelligenter Gebäudetechnik.

- **Bewertung:** Ampel für „Spielplatz angenehm“ und „Keine heißen Flecken“. ★ Ziel erreicht, ★★ unter 80 % Budget, ★★★ zusätzlich Solarstrom erzeugt.
- **Das kannst du jetzt erzählen:** „Mit Simulation sieht man, wo es in einem Gebäude oder Stadtviertel zu warm wird – bevor gebaut wird. Das spart Energie und Geld.“

### Technik (nur Entwicklerteam)
2D-Finite-Volumen-Gitter 32 × 32 (4 m), stationäre Bilanz je Zelle:
`0 = α_i·S − h_i (T_i − T_luft) − e_i + Σ_N k (T_j − T_i)` → `A·T = b`, A dünnbesetzt, SPD (5-Punkt-Stern) → Conjugate Gradient. Zielwerte: Mittel Zielzone ≤ 28 °C, Max ≤ 35 °C.

---

## MG02 – Windschneise

**Siemens-Bezug:** Simulationssoftware von Digital Industries – Strömungssimulation wird z. B. für Fahrzeuge, Flugzeuge, Gebäude und Maschinen eingesetzt.
**Auftrag (Dr. Rotor):** „Zwischen den Türmen gibt es fiese Böen. Ich brauche eine Windkarte für sichere Drohnenrouten!“
**Kalles Versuch:** Route „nach Gefühl“ → Drohne wird weggeweht, der Fisch landet bei den Möwen.

### Für Spielende
1. **Was ist wichtig?** Welche Häuser stehen im Weg? Woher kommt der Wind?
2. **Welche Regel gilt?** Bild: „Luft verschwindet nicht – wenn sie sich durch eine enge Gasse quetscht, wird sie schneller.“ (Gartenschlauch-Vergleich)
3. **Alles hängt zusammen (Schwerpunkt) – „Genau hinschauen kostet Zeit“:** Der Computer teilt die Luft in Kästchen. Spieler malt mit einem Pinsel, wo die Kästchen klein (genau) sein sollen. Ein Kästchen-Budget steht für Rechenzeit. Zwei Anzeigen: „Wie genau?“ und „Wie lange rechnet's?“.
   Lernmoment: An Hausecken lohnt sich Genauigkeit, auf freier Fläche nicht.
4. **Der Computer probiert's aus:** Windpfeile und fließende Partikel zeigen den Wind.
5. **Besser machen / Test:** Drei Lieferdrohnen fliegen nach der Windkarte des Spielers. Zu grob geplant → eine Drohne wackelt in eine Böe (lustig, nicht schlimm, neuer Versuch).
6. **So macht's Siemens:** Spieler-Kästchen neben einem echten Simulationsnetz, z. B. um ein Auto oder eine Schiffsschraube.

- **Das kannst du jetzt erzählen:** „Statt alles im Windkanal zu testen, kann man Luft im Computer strömen lassen – Siemens-Software wird dafür von vielen Herstellern genutzt.“

### Technik (nur Entwicklerteam)
2D-Potentialströmung (Stromfunktion `∇²ψ = 0`) auf Quadtree (Basis 8 × 8, max. 5 Stufen, 2:1-Balance); gleicher CG-Solver wie MG01. Fehler gegen vorberechnete Referenz (feines Gitter bzw. Python-LBM). Vereinfachung wird im Spiel ehrlich benannt.

---

## MG03 – Hochbahn-Brücke

**Siemens-Bezug:** Simulationssoftware (Digital Industries) und Siemens Mobility – z. B. Belastungsberechnung von Bauteilen in Schienenfahrzeugen (Beispiel durch Fachbereich zu bestätigen).
**Auftrag (Bruno):** „Die Hochbahn muss über den Kanal. Die alte Brücke hat der Sturm mitgenommen.“
**Kalles Versuch:** Drei Balken, viel Kleber → Brücke biegt sich wie Spaghetti, Zug bleibt stehen.

### Für Spielende
1. **Was ist wichtig?** Wo liegt die Brücke auf? Wie schwer ist der Zug? Welches Material?
2. **Welche Regel gilt?** Jeder Balken ist wie eine Feder: Je mehr man drückt, desto mehr gibt er nach.
3. **Alles hängt zusammen:** Spieler baut die Brücke aus Balken (Holz günstig, Stahl stabil, Carbon teuer & leicht).
4. **Der Computer probiert's aus (Schwerpunkt: Ergebnis verstehen):** Der Zug rollt virtuell drüber. Balken leuchten: blau = wird zusammengedrückt, rot = wird auseinandergezogen, dick = stark belastet. Durchbiegung wird übertrieben gezeigt.
   **Detektiv-Fragen:** „Welcher Balken gibt zuerst nach?“ – „Wo würdest du verstärken?“ – „Biegt sich die echte Brücke wirklich so stark?“ (Nein – das Bild übertreibt, damit man's sieht.)
5. **Besser machen:** Möglichst günstig bauen, aber mit Sicherheitsreserve (Anzeige als Ampel „sicher / knapp / zu schwach“).
6. **So macht's Siemens:** Farbbild des Spielers neben einer echten Belastungssimulation eines Bauteils.

- **Das kannst du jetzt erzählen:** „Bevor ein Bauteil gebaut wird, wird es im Computer belastet. So werden Züge, Maschinen und Brücken sicher – und man braucht weniger Prototypen.“

### Technik (nur Entwicklerteam)
2D-Fachwerk-FEM (Direkte Steifigkeitsmethode), `K·u = f`, ≤ 200 Freiheitsgrade, Cholesky/CG; Wanderlast in 5 Positionen; Versagen bei Spannung > Grenzwert oder Euler-Knicken; Sicherheitsfaktor-Ziel 1,5.

---

## MG04 – Smart Harbor — *Vertical Slice*

Weiterentwicklung der ursprünglichen *Fisch-Fabrik*.

**Siemens-Bezug:** Digital Industries – Fabrikautomatisierung und Fabrikplanung mit Digitalem Zwilling (Tecnomatix Plant Simulation).
**Auftrag (Olaf):** „Die Kutter liefern, die Robo-Robben fahren, aber die Kisten stapeln sich – und das Sushi kommt zu spät!“
**Kalles Versuch:** Kauft doppelt so viele Robo-Robben → sie stehen sich gegenseitig im Weg.

### Für Spielende
1. **Was ist wichtig? (Schwerpunkt):** Spieler baut die Fischhalle aus Bausteinen: Anlegestelle, Kühlregal, Sortier-, Filetier- und Verpackungsstation, Förderband, Robo-Robben. Wie ein kleines Aufbauspiel.
2. **Welche Regel gilt?** Bild: „Je länger etwas wartet, desto voller wird das Lager.“ (Supermarkt-Kassen-Vergleich)
3. **Alles hängt zusammen:** Pfeile zeigen den Weg des Fischs von Station zu Station.
4. **Der Computer probiert's aus:** Eine Schicht läuft im Zeitraffer (60 s). Wo es sich staut, wachsen sichtbar Kistenstapel, die Station blinkt.
5. **Zufall (Schwerpunkt) – „Die Möwen-Frage“:** Die Kutter kommen nicht pünktlich, Maschinen machen mal Pause. Knopf „Noch ein Tag“ → gleicher Plan, anderes Ergebnis. Knopf „10 Tage simulieren“ zeigt gute und schlechte Tage als Balken. Lernmoment: Ein einziger Testlauf reicht nicht.
6. **Besser machen:** Mit Budget Engpass finden und beheben.
7. **So macht's Siemens:** Spieler-Halle neben dem Digitalen Zwilling einer echten Fertigungslinie.

- **Bewertung:** ★ Sushi im Schnitt pünktlich, ★★ kaum verdorbener Fisch, ★★★ **auch an schlechten Tagen** pünktlich („robust geplant“).
- **Das kannst du jetzt erzählen:** „Bevor eine Fabrik gebaut oder umgebaut wird, lässt man sie im Computer laufen. So findet man Engpässe früh und spart teure Umbauten.“

### Technik (nur Entwicklerteam)
Ereignisdiskrete Simulation (Event-Heap). Quelle: Exponential-Ankünfte; Stationen: Dreiecks-/Normalverteilung, Störungen MTBF/MTTR; Puffer mit Kapazität; AGVs mit Wegreservierung. PCG32-Seeds, 10 Replikationen, intern Mittelwert + 90 %-Konfidenzintervall (★★★ = Untergrenze ≥ Ziel). KPIs: Durchsatz, Durchlaufzeit, Auslastung, Verderb.

---

## MG05 – Pinguin-Express

**Siemens-Bezug:** Siemens Mobility – Zugbeeinflussung und Betriebssteuerung für Metros und Bahnen.
**Auftrag (Tilda):** „Zur Rushhour sind die Bahnsteige voll, und die Züge stehen vor roten Signalen.“
**Kalles Versuch:** Mehr Züge reinschicken → sie blockieren sich gegenseitig.

### Für Spielende
1. **Was ist wichtig?** Wie viele Züge? Wie lange halten sie? Wie viel Abstand brauchen sie?
2. **Welche Regel gilt?** Bild: Ein Zug braucht Abstand, damit er rechtzeitig bremsen kann – je schneller, desto mehr.
3. **Alles hängt zusammen:** Ein verspäteter Zug bremst alle dahinter aus (Dominoeffekt sichtbar).
4. **Der Computer probiert's aus:** Ein Morgen im Zeitraffer; Bahnsteige füllen sich sichtbar mit Pinguinen.
5. **Besser machen (Schwerpunkt):** Regler für Zuganzahl, Takt und Haltezeit. Zwei Ziele gleichzeitig: wenig Warten und wenig Energie. Dann: Knopf „Lass den Computer suchen“ – viele Varianten werden automatisch ausprobiert und als Punkte gezeigt; der Spieler sieht, wo seine eigene Lösung liegt.
   **Upgrade-Moment:** „Intelligente Zugsteuerung“ einschalten – Züge dürfen näher hintereinander fahren, weil sie miteinander kommunizieren → plötzlich passen mehr Züge auf die Strecke.
6. **So macht's Siemens:** Echte Metro-Linie mit Siemens-Zugsteuerung.

- **Das kannst du jetzt erzählen:** „Mit intelligenter Zugsteuerung von Siemens Mobility fahren mehr Züge sicher auf derselben Strecke – ohne neue Gleise zu bauen.“

### Technik (nur Entwicklerteam)
Ringlinie, 6 Stationen, Δt = 1 s. Festblock vs. Moving Block (Abstand = Reaktionsweg + `v²/2a` + Marge). Poisson-Fahrgastankünfte mit Tagesganglinie. Optimierer: Hill-Climbing/GA, Pareto-Front Wartezeit vs. Energie (im Spiel nur als Punktwolke ohne Fachbegriff).

---

## MG06 – Netz-Balance

**Siemens-Bezug:** Smart Infrastructure – Stromnetze planen und betreiben, z. B. für mehr Solar und E-Mobilität.
**Auftrag (Volta):** „Mittags speisen alle Solardächer ein, abends kochen alle Fischsuppe – und meine Leitungen glühen!“
**Kalles Versuch:** Überall dickere Kabel → Budget weg, Problem nur verschoben.

### Für Spielende
1. **Was ist wichtig?** Wer verbraucht wann Strom, wer erzeugt wann Strom?
2. **Welche Regel gilt?** Wie Wasser in Rohren: Was in eine Kreuzung reinfließt, muss auch wieder raus.
3. **Alles hängt zusammen (Schwerpunkt):** Spieler verlegt Leitungen zwischen Häusern, Solaranlagen, Batterien und Umspannwerk.
   **ZWILLI-Aha:** „Moment – das Muster kenne ich! Strom verteilt sich wie Wärme im Frostgarten.“ Ein Vergleichsbild zeigt beide Netze nebeneinander: dieselbe Logik. Botschaft: Wer Simulation einmal verstanden hat, erkennt sie überall.
4. **Der Computer probiert's aus:** Ein Tag im Zeitraffer, Leitungen leuchten grün → gelb → rot je nach Belastung.
5. **Besser machen:** Batterien aufstellen, einzelne Leitungen verstärken, Fischsuppen-Kocher auf später verschieben.
6. **So macht's Siemens:** Spielernetz neben der Planung eines echten städtischen Stromnetzes.

- **Das kannst du jetzt erzählen:** „Netzbetreiber simulieren mit Siemens-Software ihr Stromnetz, damit Solaranlagen und Ladesäulen angeschlossen werden können, ohne dass Leitungen überlasten.“

### Technik (nur Entwicklerteam)
DC-Lastfluss `B·θ = P`, Leitungsfluss `P_ij = B_ij (θ_i − θ_j)`, Slack = Umspannwerk; 24 quasistationäre Stunden, Batterie koppelt Zeitschritte. Gleicher Graph-Laplace-Operator und CG-Solver wie MG01.

---

## MG07 – Hitzewelle (Finale)

**Siemens-Bezug:** Siemens Xcelerator / Digitaler Zwilling – alles verbunden.

### Für Spielende
- Leitstand im Zwillingsturm, die ganze Stadt als Miniatur.
- Eine Hitzewelle kommt. Alles hängt zusammen:
  ```
  Hitze ─▶ Gebäude brauchen mehr Kühlung ─▶ Stromnetz wird voll
                         │                          │
                         ▼                          ▼
          Alle wollen ins Hafenbad (Bahn)    Hafen verlegt Schicht in die Nacht
  ```
- Spieler trifft 4–5 Entscheidungen über 3 Tage. Vor jeder Entscheidung zeigt ZWILLI: „Was wäre, wenn …?“ – erst virtuell, dann echt.
- **Das kannst du jetzt erzählen:** „Ein Digitaler Zwilling verbindet viele Simulationen zu einem Abbild der echten Welt. Damit können Siemens-Kunden Entscheidungen erst virtuell testen.“
- **Abschluss:** Die vollständige **Siemens-Landkarte** wird gezeigt – jedes Viertel mit seinem Siemens-Geschäft.

### Technik (nur Entwicklerteam)
Co-Simulation mit festen Kopplungsschritten aus vereinfachten, schnellen Varianten von MG01, MG04, MG05, MG06.

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
