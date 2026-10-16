# 05 – Erweiterung: „Pingopolis“ – Die Siedlung wird zur Smart City

> **Status: spätere Erweiterung (Release 2, optional).**
> Dieses Dokument war ursprünglich das Hauptkonzept des Spiels. Mit den Ideen aus dem Kolleg:innen-Workshop steht jetzt die **Pinguin-Kolonie** im Zentrum (siehe [01_Konzept_Kolonie.md](01_Konzept_Kolonie.md), [02_Story_und_Charaktere.md](02_Story_und_Charaktere.md), [03_Minigames.md](03_Minigames.md)).
> Die Angaben „Vertical Slice“, „R1“ und „R2“ in Teil A und Teil C stammen aus der früheren Planung. Für die Erweiterung gilt eine neue Reihenfolge, die nach Release 1 des Kernspiels festgelegt wird.
> Die Smart City bleibt vollständig erhalten und wird zur **Fortsetzung**: Die Siedlung aus dem Kernspiel wächst zur Stadt Pingopolis heran.
>
> **Abgleich mit dem Kernspiel** (gilt vor dem Text unten):
> - Der hier beschriebene **Prolog / MG00 Huddle** ist durch **MG1 Huddle** im Kernspiel ersetzt. Pingopolis startet nach dem Epilog des Kernspiels.
> - **MG04 Smart Harbor** ist die Stadt-Version der **MG3 Fischfabrik** aus dem Kernspiel. In der Erweiterung baut man die Fabrik zum Hafen aus (gleicher Simulationskern).
> - **MG05 Pinguin-Express** baut auf dem **MG2 Polar-Express** auf: Im Kernspiel geht es um die Form des Zuges (Strömung), hier um den Betrieb vieler Züge (Takt, Zugsteuerung).
> - **MG01 Hitzeinsel** nutzt denselben Wärme-Simulationskern wie **MG1 Huddle**.
> - Figuren aus dem Kernspiel (Pip, Oma Grete, Frieda, Kalle, Olaf, Tilda, ZWILLI) kommen mit in die Stadt.
> - Verweise auf „Dokument 03“ im Text unten meinen **Teil C** dieses Dokuments.

---

## Teil A – Konzept Smart City

> Weiterentwicklung von *Simulation for Everyone – Konzeptvorstellung* (30.09.2026).
> Ziel bleibt unverändert: **Ein Spiel für Siemens-Mitarbeiter, das spielerisch erklärt, wie Simulationen funktionieren und wie sie in realen Siemens-Use-Cases angewendet werden.**
> Geändert wird das Setting: von der Pinguinkolonie in der Antarktis zur **Pinguin-Smart-City**.
> **Zielgruppe: Siemens-Mitarbeitende ohne technischen Hintergrund.** Zielgruppe, Lernziele und Siemens-Bezug sind in [00_Zielgruppe_und_Siemens-Bezug.md](00_Zielgruppe_und_Siemens-Bezug.md) festgelegt und haben Vorrang vor allen anderen Dokumenten.

---

### 1. Warum Smart City statt Antarktis?

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

### 2. Design-Säulen

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

### 3. Didaktisches Grundgerüst (unverändert)

```
Modellierung → Physikalische Gleichung → Systemaufbau → Lösung → Optimierung → (Transfer)
```

Im Spiel heißen die Schritte alltagssprachlich: **Was ist wichtig? → Welche Regel gilt? → Alles hängt zusammen → Der Computer probiert's aus → Besser machen → So macht's Siemens** (Details in Dokument 00, Abschnitt 5).

Jedes Minigame durchläuft alle Schritte, **betont aber einen Schwerpunkt** (siehe Teil C). Nach jedem Minigame folgt ein **Transfer-Screen „So macht's Siemens“**: links das Ergebnis des Spielers, rechts ein echtes Siemens-Beispiel mit derselben Methode, dazu der Kundennutzen in Business-Sprache.

**Zentrale Erkenntnis aus der Konzeptvorstellung** – bleibt der rote Faden:
Der *Systemaufbau* (lokale Beziehungen → Gesamtsystem) steckt in jeder Simulation.
- Kontinuierliche Probleme (Luft, Wärme im Asphalt, Stahl) müssen erst **diskretisiert** werden → Simcenter-Welt.
- Natürlich diskrete Systeme (Fabrik, Bahnbetrieb, Stromnetzknoten) sind es bereits → Plant-Simulation-Welt.

Die Smart City macht das besonders sichtbar: Wärmenetz eines Viertels, Stromnetz und Brückenfachwerk folgen **derselben Logik** (intern: dieselbe Matrixstruktur). Im Spiel wird das ohne Mathematik inszeniert: ZWILLI erkennt das Muster wieder („Strom verteilt sich wie Wärme!“). Botschaft für Nicht-Techniker: *Wer Simulation einmal verstanden hat, erkennt sie überall – und darum kann Siemens sie in so vielen Branchen einsetzen.*

---

### 4. Spielablauf (Core Loop)

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

### 5. Umfang & Vertical Slice

Die Konzeptvorstellung sieht 2–3 Minigames für den ersten Umfang vor. Empfehlung:

| Paket | Inhalt | Begründung |
|---|---|---|
| **Vertical Slice** | Hub-Ausschnitt (Zentrum + 2 Viertel), **MG1 Hitzeinsel** + **MG4 Smart Harbor**, 2 Transfer-Screens, Dialoge Akt 1 | Zwei Siemens-Geschäfte (Smart Infrastructure, Digital Industries), beide aus dem Alltag verständlich (Hitze, Stau); intern deckt es die Trennung *kontinuierlich (Simcenter) vs. diskret (Plant Simulation)* ab |
| **Release 1** | + Prolog (Huddle), **MG6 Stromnetz** | Stromnetz nutzt denselben Solver wie MG1 → günstig, und liefert das „Strom verteilt sich wie Wärme“-Aha |
| **Release 2** | + MG2 Windschneise, MG3 Hochbahn-Brücke, MG5 Pinguin-Express, Finale | Vollständige Story |

---

### 6. Dokumente

| Datei | Inhalt |
|---|---|
| [00_Zielgruppe_und_Siemens-Bezug.md](00_Zielgruppe_und_Siemens-Bezug.md) | **Leitplanke:** Zielgruppe, Lernziele, Siemens-Landkarte, Sprachregeln |
| Teil A | Vision, Säulen, Umfang |
| Teil B | Welt, Storyline, Charaktere, Dialogton |
| Teil C | Alle Minigames inkl. Simulationsmodell, Spielablauf, Transfer |
| [04_Technische_Spezifikation.md](04_Technische_Spezifikation.md) | Architektur, Unity/Blender-Pipeline, Full-Code-Workflow, Tests, Hosting |

---

## Teil B – Story & Charaktere Pingopolis

### 1. Die Welt: Pingopolis

> Zielgruppe: Siemens-Mitarbeitende ohne technischen Hintergrund. Deshalb ist auch die Spielfigur **keine** Ingenieurin bzw. kein Ingenieur – sie lernt alles gemeinsam mit den Spielenden.

Als die Eisschollen der Kolonie immer kleiner werden, bauen die Pinguine etwas Neues: **Pingopolis**, eine schwimmende Smart City am Rand des Packeises. Alles ist vernetzt – Gebäude regeln ihr Klima selbst, Drohnen liefern Fisch, eine Hochbahn verbindet die Viertel. Das Herz der Stadt ist der **Zwillingsturm** mit **ZWILLI**, dem Digitalen Zwilling der Stadt: Er simuliert alles, bevor es gebaut oder verändert wird.

#### Viertel (Hub-Struktur)

```
                     ┌────────────────────────┐
                     │ Windkante              │  Hochhäuser, Drohnen
                     │ MG2 · DI Software      │
                     └───────────┬────────────┘
 ┌──────────────────┐ ┌─────────┴──────────┐ ┌──────────────────────┐
 │ Frostgarten      │─│ Zwillingsturm      │─│ Voltkai              │
 │ MG1 · Smart Inf- │ │ Zentrum / Finale   │ │ MG6 · Smart Infra-   │
 │ rastructure      │ │ Siemens Xcelerator │ │ structure (Netze)    │
 └──────────────────┘ └─────────┬──────────┘ └──────────────────────┘
 ┌──────────────────┐ ┌─────────┴──────────┐ ┌──────────────────────┐
 │ Kanalbogen       │─│ Fischhafen         │─│ Express-Ring         │
 │ MG3 · DI Soft-   │ │ MG4 · Digital      │ │ MG5 · Siemens        │
 │ ware + Mobility  │ │ Industries         │ │ Mobility             │
 └──────────────────┘ └────────────────────┘ └──────────────────────┘
```

Im Prolog: **Die alte Scholle** (Antarktis, MG0 Huddle).

Jedes Viertel trägt am Eingang ein Schild mit seinem Siemens-Geschäft. Zusammen ergeben sie die **Siemens-Landkarte**, die der Spieler im Notizbuch sammelt.

---

### 2. Storyline

**Leitmotiv:** *„Erst simulieren, dann bauen.“*
**Zweites Motiv:** *Von der Kolonie zur Stadt – die Naturgesetze bleiben dieselben.*

#### Prolog – Die alte Scholle (Tutorial, ~5 Min.)
Ein Schneesturm zieht über die Kolonie. Der Spieler (Pip) organisiert den Huddle so, dass kein Pinguin auskühlt (= ursprüngliches Huddle-Minigame, vereinfacht). Die Großmutter **Oma Grete** erklärt: „Jeder wärmt seinen Nachbarn – so überlebt die ganze Kolonie.“ Am Ende bricht die Scholle; die Kolonie folgt dem Licht von Pingopolis.
*Didaktik:* erste Begegnung mit „lokale Beziehung → Gesamtsystem“.

#### Akt 1 – Ankunft im Blackout (~10 Min.)
Pip kommt in Pingopolis an – doch der **Polarsturm „Boreas“** hat gerade den Zwillingsturm getroffen. ZWILLI ist in sechs Fragmente zerfallen; die Stadt läuft „blind“. Bürgermeisterin **Emma Eisberg** braucht Hilfe, Bauunternehmer **Kalle Klotz** will alles „einfach schnell wieder aufbauen – wird schon halten!“.
**Prof. Dr. Frieda Frost**, Leiterin des Simulationslabors, braucht dringend helfende Flossen – und nimmt Pip auf, obwohl Pip „noch nie etwas mit Technik zu tun hatte“. Frieda: „Perfekt. Dann erklären wir es so, dass es jeder versteht.“ Pip bekommt das ZWILLI-Tablet. Auftrag: In jedem Viertel das Simulationsmodell neu aufbauen und so ein ZWILLI-Fragment zurückgewinnen.
→ Hub wird freigeschaltet, Frostgarten und Fischhafen sind offen (= Vertical Slice).

#### Akt 2 – Die Viertel (frei wählbare Reihenfolge, ~60 Min.)
Jedes Viertel folgt demselben Muster:
1. **Problem**: Ein NPC hat ein akutes Problem (Hitze, Stau, Überlast …).
2. **Kalles Versuch**: Kalle probiert eine Lösung ohne Simulation – sie scheitert sichtbar und komisch (Brücke wackelt, Drohne landet im Kanal, Sicherung fliegt).
3. **Minigame**: Pip simuliert, findet eine robuste Lösung.
4. **So macht's Siemens**: Frieda öffnet am Labor-Bildschirm ein „Fenster in die Menschenwelt“ und zeigt, wo Siemens genau das für echte Kunden macht – optional mit einer kurzen Videobotschaft einer echten Siemens-Kollegin oder eines Kollegen.
5. **Belohnung**: Viertel verwandelt sich, ZWILLI-Fragment kehrt zurück, eine neue Karte der Siemens-Landkarte wird aufgedeckt, und Pip notiert den Satz „Das kannst du jetzt erzählen“.

Wendepunkte innerhalb von Akt 2:
- **Nach dem 2. Fragment:** ZWILLI erkennt, dass sich Strom im Netz verteilt wie Wärme im Viertel → Aha-Moment *Wer Simulation einmal verstanden hat, erkennt sie überall* (deshalb nutzt Siemens sie in so vielen Geschäften).
- **Nach dem 4. Fragment:** Kalle bittet Pip heimlich um Hilfe – er hat verstanden, dass Simulation Geld und Nerven spart. Er wird vom Gegenspieler zum Verbündeten.

#### Akt 3 – Die Hitzewelle (Finale, ~10 Min.)
ZWILLI ist wieder ganz – gerade rechtzeitig: Eine **Hitzewelle** rollt auf Pingopolis zu (für Pinguine eine Katastrophe!). Im Leitstand des Zwillingsturms sind alle Viertel-Modelle **gekoppelt**: Hitze → mehr Kühlbedarf → Netzlast steigt → Hafen muss Produktion verschieben → Hochbahn braucht mehr Takt, weil alle ins kühle Hafenbad wollen. Pip trifft Entscheidungen, ZWILLI simuliert die Folgen im Zeitraffer, bevor sie umgesetzt werden.
*Didaktik:* Co-Simulation / Digitaler Zwilling als Gesamtsystem.

#### Epilog
Fest auf dem Zentrumsplatz. Kalle eröffnet sein neues Bauunternehmen „Klotz & Zwilling – wir simulieren zuerst“. Frieda überreicht Pip die vollständige **Siemens-Landkarte**: Jedes gerettete Viertel steht für ein Siemens-Geschäft, und Pip kann zu jedem einen Satz erzählen. Oma Grete besucht die Stadt: „Ihr macht ja dasselbe wie wir damals beim Huddle – nur mit mehr Kabeln.“ Freispiel: alle Minigames im Optimierungsmodus, Bestenliste.

---

### 3. Charaktere

#### Hauptfiguren

| Figur | Rolle | Didaktische Funktion | Persönlichkeit | Look |
|---|---|---|---|---|
| **Pip** (Spieler) | Neu in der Stadt, **ohne Technik-Hintergrund**, wird Helfer:in im Zwillingsturm | Identifikationsfigur für die Zielgruppe; stellt die Fragen, die sich Spielende auch stellen („Und wozu braucht man das?“) | neugierig, mutig, etwas tollpatschig | Kaiserpinguin-Küken (anpassbar: Mütze, Schal, Farbe), Werkzeuggürtel mit Tablet |
| **Prof. Dr. Frieda Frost** | Leiterin Simulationslabor | Übersetzt Technik in Alltagssprache (Vergleiche: Waage, Gartenschlauch, Supermarktkasse); präsentiert „So macht's Siemens“ | warmherzig, präzise, trockener Humor, liebt Kaffee mit Eiswürfeln | Felsenpinguin mit gelben Federbüscheln, Laborkittel, Brille |
| **ZWILLI** | Digitaler Zwilling der Stadt (KI-Hologramm) | Fortschrittsanzeige (Fragmente), stellt Querbezüge zwischen Minigames her | anfangs stotternd/glitchig, wird mit jedem Fragment klüger und witziger | Blau leuchtendes Wireframe-Hologramm eines Pinguins; je mehr Fragmente, desto vollständiger das Mesh |
| **Kalle Klotz** | Bauunternehmer, freundlicher Gegenspieler | Verkörpert „Trial & Error“ – zeigt, was ohne Simulation passiert | laut, optimistisch, „Wird schon halten!“, gutes Herz | Stämmiger Königspinguin, gelber Bauhelm, Hosenträger |
| **Emma Eisberg** | Bürgermeisterin | Auftraggeberin, gibt Ziele/KPIs vor (Budget, Komfort, CO₂) | pragmatisch, unter Druck, fair | Adeliepinguin, Amtskette aus Fischgräten |
| **Oma Grete** | Älteste der Kolonie | Prolog/Epilog, verbindet Natur und Technik | weise, verschmitzt | Alt, zerzaust, Strickjacke |

#### Viertel-NPCs (Auftraggeber der Minigames)

| Figur | Viertel | Problem | Kurzprofil |
|---|---|---|---|
| **Flora Federweich** | Frostgarten | Hitzeinseln auf dem Spielplatz | Stadtgärtnerin, redet mit Pflanzen |
| **Dr. Rotor** | Windkante | Lieferdrohnen stürzen in Böen ab | Drohnen-Logistikerin, nervös, spricht in Flugcodes |
| **Bruno Bolzen** | Kanalbogen | Hochbahn braucht neue Brücke | Brückenbauer alter Schule, Kalles Rivale |
| **Hafenmeister Olaf** | Fischhafen | Fischkisten stauen sich | Seebär, Pfeife (ohne Tabak), liebt Ordnung |
| **Tilda Takt** | Express-Ring | Bahnsteige überfüllt | Fahrdienstleiterin, Stoppuhr um den Hals |
| **Volta** | Voltkai | Leitungen überlasten mittags/abends | Netzwartin, Funken in den Federn |

#### Nebenfiguren / Running Gags
- **Die Möwen** – stehlen Fisch, tauchen unvorhersehbar auf. Sie personifizieren **Zufall/Stochastik** (MG4, MG5): „Die Möwen kommen *im Mittel* alle 5 Minuten – aber eben nur im Mittel.“
- **Robo-Robben** – autonome Transportroboter (AGVs) im Hafen, piepsen beleidigt, wenn sie warten müssen.

---

### 4. Ton & Dialog-Richtlinien

- **Sprache:** Deutsch (primär) und Englisch; kurze Sätze, max. 2 Zeilen pro Sprechblase.
- **Alltag zuerst, Begriff danach**, höchstens ein neuer Fachbegriff pro Phase; er landet mit Alltags-Erklärung im Notizbuch. Keine Formeln, keine unerklärten Abkürzungen (Sprachregeln: Dokument 00, Abschnitt 5).
- **Humor:** freundlich, nie herablassend gegenüber Spielenden ohne Technik-Hintergrund. Kalle scheitert, aber wird nie lächerlich gemacht.
- **Siemens-Bezug:** klar und wiederkehrend, aber dosiert über fünf feste Berührungspunkte (Viertel-Schild, „So macht's Siemens“, Siemens-Stimme, Siemens-Landkarte, „Das kannst du jetzt erzählen“ – siehe Dokument 00, Abschnitt 4). NPCs in der Stadt machen keine Werbung.

Beispielszene (MG3, Kanalbogen):

> **Kalle:** Brücke? Kein Problem! Drei Balken, viel Kleber – wird schon halten!
> *(Testzug rollt drauf. Die Brücke biegt sich wie Spaghetti. Zug hält, Passagiere steigen genervt aus.)*
> **Bruno:** Klotz, du Kiesel …
> **Frieda:** Pip, bevor wir echten Stahl verbauen – lass uns die Brücke erst im Rechner belasten.
> **Pip:** Im Rechner belasten? Wie soll das gehen?
> **Frieda:** Stell dir jeden Balken wie eine Feder vor. Der Computer drückt auf alle gleichzeitig und schaut, welche nachgibt.
> **ZWILLI:** F-f-federn … alle verbunden … *bzzt* … das Muster kenne ich!

*(Nach dem Minigame, „So macht's Siemens“)*
> **Frieda:** Genau so prüfen Ingenieur:innen bei Siemens Bauteile, bevor sie gebaut werden. Weniger Prototypen, mehr Sicherheit.
> **Pip:** Das kann ich ja jetzt sogar erklären!

---

## Teil C – Minigames Pingopolis

> Zielgruppe: Siemens-Mitarbeitende **ohne technischen Hintergrund** (siehe [00_Zielgruppe_und_Siemens-Bezug.md](00_Zielgruppe_und_Siemens-Bezug.md)).
> Jedes Minigame ist deshalb zweigeteilt:
> **„Für Spielende“** beschreibt, was man sieht und tut – ohne Formeln.
> **„Technik (nur Entwicklerteam)“** beschreibt das Modell im Hintergrund. Davon sieht der Spieler nichts, außer im optionalen Modus „Blick unter die Haube“.

### Übersicht

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

#### Gemeinsames Phasen-Gerüst

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

### MG00 – Huddle (Prolog)

#### Für Spielende
Schneesturm auf der Eisscholle. Pip schiebt Pinguine so zusammen, dass niemand friert. Pinguine am Rand werden blau, in der Mitte rot-warm. Der Wind dreht – die Gruppe muss sich bewegen.
- **Ziel:** 60 Sekunden lang friert kein Pinguin (Zeit läuft nur, wenn der Spieler nichts tut – kein Stress).
- **Aha:** Jeder Pinguin wärmt nur seine direkten Nachbarn, trotzdem entsteht ein warmer Kern → „Alles hängt zusammen“.
- **Das kannst du jetzt erzählen:** „Auch komplizierte Systeme bestehen aus vielen kleinen Teilen, die sich gegenseitig beeinflussen – so rechnen Simulationen.“

#### Technik (nur Entwicklerteam)
Wärmenetzwerk, Pinguin = Knoten: `C·dT_i/dt = q + Σ_j G_ij (T_j − T_i) − h_i (T_i − T_luft)`, implizites Euler. Natürlich diskret, keine Diskretisierung nötig.

---

### MG01 – Hitzeinsel — *Vertical Slice*

**Siemens-Bezug:** Smart Infrastructure (intelligente Gebäude) und Simulationssoftware von Digital Industries.
**Auftrag (Flora):** „Der Spielplatz im Frostgarten wird mittags so heiß, dass die Küken nicht mehr rauswollen!“
**Kalles Versuch:** Stellt einen Riesen-Ventilator auf → bläst nur heiße Luft herum, die Sicherung fliegt.

#### Für Spielende
Der Stadtblock ist ein Raster aus Kacheln (Asphalt, Pflaster, Rasen, Baum, Gründach, weißes Dach, Wasserbecken, Solar-Sonnensegel).
1. **Was ist wichtig?** Karten: Sonne ✔, Bodenbelag ✔, Wind ✔, Schatten ✔ – *Farbe der Parkbänke* ✘, *Anzahl der Laternen* ✘.
2. **Welche Regel gilt? (Schwerpunkt)** Eine Kachel als Waage: Rein: Sonne, warme Nachbarn. Raus: Wind, Schatten, Verdunstung (Bäume „schwitzen“). Spieler legt Symbole auf die richtige Seite; die Kachel wird wärmer oder kühler.
3. **Alles hängt zusammen (Schwerpunkt):** Spieler zieht Wärmefäden zwischen benachbarten Kacheln; ein heißer Asphaltfleck „färbt“ sichtbar auf die Nachbarn ab. Dann verbindet der Computer den ganzen Block automatisch – Hunderte Fäden in Sekunden („Genau dafür braucht man Computer“).
4. **Der Computer probiert's aus:** Eine Wärmekarte legt sich über die Stadt, die Farben „pendeln sich ein“.
5. **Besser machen:** 3 Runden, Budget 1000 Fisch-Taler. Kacheln tauschen, neu simulieren.
6. **So macht's Siemens:** Spieler-Wärmekarte neben einer echten Gebäude-Simulation (z. B. Temperaturverteilung in einem Bürogebäude) + Bezug zu intelligenter Gebäudetechnik.

- **Bewertung:** Ampel für „Spielplatz angenehm“ und „Keine heißen Flecken“. ★ Ziel erreicht, ★★ unter 80 % Budget, ★★★ zusätzlich Solarstrom erzeugt.
- **Das kannst du jetzt erzählen:** „Mit Simulation sieht man, wo es in einem Gebäude oder Stadtviertel zu warm wird – bevor gebaut wird. Das spart Energie und Geld.“

#### Technik (nur Entwicklerteam)
2D-Finite-Volumen-Gitter 32 × 32 (4 m), stationäre Bilanz je Zelle:
`0 = α_i·S − h_i (T_i − T_luft) − e_i + Σ_N k (T_j − T_i)` → `A·T = b`, A dünnbesetzt, SPD (5-Punkt-Stern) → Conjugate Gradient. Zielwerte: Mittel Zielzone ≤ 28 °C, Max ≤ 35 °C.

---

### MG02 – Windschneise

**Siemens-Bezug:** Simulationssoftware von Digital Industries – Strömungssimulation wird z. B. für Fahrzeuge, Flugzeuge, Gebäude und Maschinen eingesetzt.
**Auftrag (Dr. Rotor):** „Zwischen den Türmen gibt es fiese Böen. Ich brauche eine Windkarte für sichere Drohnenrouten!“
**Kalles Versuch:** Route „nach Gefühl“ → Drohne wird weggeweht, der Fisch landet bei den Möwen.

#### Für Spielende
1. **Was ist wichtig?** Welche Häuser stehen im Weg? Woher kommt der Wind?
2. **Welche Regel gilt?** Bild: „Luft verschwindet nicht – wenn sie sich durch eine enge Gasse quetscht, wird sie schneller.“ (Gartenschlauch-Vergleich)
3. **Alles hängt zusammen (Schwerpunkt) – „Genau hinschauen kostet Zeit“:** Der Computer teilt die Luft in Kästchen. Spieler malt mit einem Pinsel, wo die Kästchen klein (genau) sein sollen. Ein Kästchen-Budget steht für Rechenzeit. Zwei Anzeigen: „Wie genau?“ und „Wie lange rechnet's?“.
   Lernmoment: An Hausecken lohnt sich Genauigkeit, auf freier Fläche nicht.
4. **Der Computer probiert's aus:** Windpfeile und fließende Partikel zeigen den Wind.
5. **Besser machen / Test:** Drei Lieferdrohnen fliegen nach der Windkarte des Spielers. Zu grob geplant → eine Drohne wackelt in eine Böe (lustig, nicht schlimm, neuer Versuch).
6. **So macht's Siemens:** Spieler-Kästchen neben einem echten Simulationsnetz, z. B. um ein Auto oder eine Schiffsschraube.

- **Das kannst du jetzt erzählen:** „Statt alles im Windkanal zu testen, kann man Luft im Computer strömen lassen – Siemens-Software wird dafür von vielen Herstellern genutzt.“

#### Technik (nur Entwicklerteam)
2D-Potentialströmung (Stromfunktion `∇²ψ = 0`) auf Quadtree (Basis 8 × 8, max. 5 Stufen, 2:1-Balance); gleicher CG-Solver wie MG01. Fehler gegen vorberechnete Referenz (feines Gitter bzw. Python-LBM). Vereinfachung wird im Spiel ehrlich benannt.

---

### MG03 – Hochbahn-Brücke

**Siemens-Bezug:** Simulationssoftware (Digital Industries) und Siemens Mobility – z. B. Belastungsberechnung von Bauteilen in Schienenfahrzeugen (Beispiel durch Fachbereich zu bestätigen).
**Auftrag (Bruno):** „Die Hochbahn muss über den Kanal. Die alte Brücke hat der Sturm mitgenommen.“
**Kalles Versuch:** Drei Balken, viel Kleber → Brücke biegt sich wie Spaghetti, Zug bleibt stehen.

#### Für Spielende
1. **Was ist wichtig?** Wo liegt die Brücke auf? Wie schwer ist der Zug? Welches Material?
2. **Welche Regel gilt?** Jeder Balken ist wie eine Feder: Je mehr man drückt, desto mehr gibt er nach.
3. **Alles hängt zusammen:** Spieler baut die Brücke aus Balken (Holz günstig, Stahl stabil, Carbon teuer & leicht).
4. **Der Computer probiert's aus (Schwerpunkt: Ergebnis verstehen):** Der Zug rollt virtuell drüber. Balken leuchten: blau = wird zusammengedrückt, rot = wird auseinandergezogen, dick = stark belastet. Durchbiegung wird übertrieben gezeigt.
   **Detektiv-Fragen:** „Welcher Balken gibt zuerst nach?“ – „Wo würdest du verstärken?“ – „Biegt sich die echte Brücke wirklich so stark?“ (Nein – das Bild übertreibt, damit man's sieht.)
5. **Besser machen:** Möglichst günstig bauen, aber mit Sicherheitsreserve (Anzeige als Ampel „sicher / knapp / zu schwach“).
6. **So macht's Siemens:** Farbbild des Spielers neben einer echten Belastungssimulation eines Bauteils.

- **Das kannst du jetzt erzählen:** „Bevor ein Bauteil gebaut wird, wird es im Computer belastet. So werden Züge, Maschinen und Brücken sicher – und man braucht weniger Prototypen.“

#### Technik (nur Entwicklerteam)
2D-Fachwerk-FEM (Direkte Steifigkeitsmethode), `K·u = f`, ≤ 200 Freiheitsgrade, Cholesky/CG; Wanderlast in 5 Positionen; Versagen bei Spannung > Grenzwert oder Euler-Knicken; Sicherheitsfaktor-Ziel 1,5.

---

### MG04 – Smart Harbor — *Vertical Slice*

Weiterentwicklung der ursprünglichen *Fisch-Fabrik*.

**Siemens-Bezug:** Digital Industries – Fabrikautomatisierung und Fabrikplanung mit Digitalem Zwilling (Tecnomatix Plant Simulation).
**Auftrag (Olaf):** „Die Kutter liefern, die Robo-Robben fahren, aber die Kisten stapeln sich – und das Sushi kommt zu spät!“
**Kalles Versuch:** Kauft doppelt so viele Robo-Robben → sie stehen sich gegenseitig im Weg.

#### Für Spielende
1. **Was ist wichtig? (Schwerpunkt):** Spieler baut die Fischhalle aus Bausteinen: Anlegestelle, Kühlregal, Sortier-, Filetier- und Verpackungsstation, Förderband, Robo-Robben. Wie ein kleines Aufbauspiel.
2. **Welche Regel gilt?** Bild: „Je länger etwas wartet, desto voller wird das Lager.“ (Supermarkt-Kassen-Vergleich)
3. **Alles hängt zusammen:** Pfeile zeigen den Weg des Fischs von Station zu Station.
4. **Der Computer probiert's aus:** Eine Schicht läuft im Zeitraffer (60 s). Wo es sich staut, wachsen sichtbar Kistenstapel, die Station blinkt.
5. **Zufall (Schwerpunkt) – „Die Möwen-Frage“:** Die Kutter kommen nicht pünktlich, Maschinen machen mal Pause. Knopf „Noch ein Tag“ → gleicher Plan, anderes Ergebnis. Knopf „10 Tage simulieren“ zeigt gute und schlechte Tage als Balken. Lernmoment: Ein einziger Testlauf reicht nicht.
6. **Besser machen:** Mit Budget Engpass finden und beheben.
7. **So macht's Siemens:** Spieler-Halle neben dem Digitalen Zwilling einer echten Fertigungslinie.

- **Bewertung:** ★ Sushi im Schnitt pünktlich, ★★ kaum verdorbener Fisch, ★★★ **auch an schlechten Tagen** pünktlich („robust geplant“).
- **Das kannst du jetzt erzählen:** „Bevor eine Fabrik gebaut oder umgebaut wird, lässt man sie im Computer laufen. So findet man Engpässe früh und spart teure Umbauten.“

#### Technik (nur Entwicklerteam)
Ereignisdiskrete Simulation (Event-Heap). Quelle: Exponential-Ankünfte; Stationen: Dreiecks-/Normalverteilung, Störungen MTBF/MTTR; Puffer mit Kapazität; AGVs mit Wegreservierung. PCG32-Seeds, 10 Replikationen, intern Mittelwert + 90 %-Konfidenzintervall (★★★ = Untergrenze ≥ Ziel). KPIs: Durchsatz, Durchlaufzeit, Auslastung, Verderb.

---

### MG05 – Pinguin-Express

**Siemens-Bezug:** Siemens Mobility – Zugbeeinflussung und Betriebssteuerung für Metros und Bahnen.
**Auftrag (Tilda):** „Zur Rushhour sind die Bahnsteige voll, und die Züge stehen vor roten Signalen.“
**Kalles Versuch:** Mehr Züge reinschicken → sie blockieren sich gegenseitig.

#### Für Spielende
1. **Was ist wichtig?** Wie viele Züge? Wie lange halten sie? Wie viel Abstand brauchen sie?
2. **Welche Regel gilt?** Bild: Ein Zug braucht Abstand, damit er rechtzeitig bremsen kann – je schneller, desto mehr.
3. **Alles hängt zusammen:** Ein verspäteter Zug bremst alle dahinter aus (Dominoeffekt sichtbar).
4. **Der Computer probiert's aus:** Ein Morgen im Zeitraffer; Bahnsteige füllen sich sichtbar mit Pinguinen.
5. **Besser machen (Schwerpunkt):** Regler für Zuganzahl, Takt und Haltezeit. Zwei Ziele gleichzeitig: wenig Warten und wenig Energie. Dann: Knopf „Lass den Computer suchen“ – viele Varianten werden automatisch ausprobiert und als Punkte gezeigt; der Spieler sieht, wo seine eigene Lösung liegt.
   **Upgrade-Moment:** „Intelligente Zugsteuerung“ einschalten – Züge dürfen näher hintereinander fahren, weil sie miteinander kommunizieren → plötzlich passen mehr Züge auf die Strecke.
6. **So macht's Siemens:** Echte Metro-Linie mit Siemens-Zugsteuerung.

- **Das kannst du jetzt erzählen:** „Mit intelligenter Zugsteuerung von Siemens Mobility fahren mehr Züge sicher auf derselben Strecke – ohne neue Gleise zu bauen.“

#### Technik (nur Entwicklerteam)
Ringlinie, 6 Stationen, Δt = 1 s. Festblock vs. Moving Block (Abstand = Reaktionsweg + `v²/2a` + Marge). Poisson-Fahrgastankünfte mit Tagesganglinie. Optimierer: Hill-Climbing/GA, Pareto-Front Wartezeit vs. Energie (im Spiel nur als Punktwolke ohne Fachbegriff).

---

### MG06 – Netz-Balance

**Siemens-Bezug:** Smart Infrastructure – Stromnetze planen und betreiben, z. B. für mehr Solar und E-Mobilität.
**Auftrag (Volta):** „Mittags speisen alle Solardächer ein, abends kochen alle Fischsuppe – und meine Leitungen glühen!“
**Kalles Versuch:** Überall dickere Kabel → Budget weg, Problem nur verschoben.

#### Für Spielende
1. **Was ist wichtig?** Wer verbraucht wann Strom, wer erzeugt wann Strom?
2. **Welche Regel gilt?** Wie Wasser in Rohren: Was in eine Kreuzung reinfließt, muss auch wieder raus.
3. **Alles hängt zusammen (Schwerpunkt):** Spieler verlegt Leitungen zwischen Häusern, Solaranlagen, Batterien und Umspannwerk.
   **ZWILLI-Aha:** „Moment – das Muster kenne ich! Strom verteilt sich wie Wärme im Frostgarten.“ Ein Vergleichsbild zeigt beide Netze nebeneinander: dieselbe Logik. Botschaft: Wer Simulation einmal verstanden hat, erkennt sie überall.
4. **Der Computer probiert's aus:** Ein Tag im Zeitraffer, Leitungen leuchten grün → gelb → rot je nach Belastung.
5. **Besser machen:** Batterien aufstellen, einzelne Leitungen verstärken, Fischsuppen-Kocher auf später verschieben.
6. **So macht's Siemens:** Spielernetz neben der Planung eines echten städtischen Stromnetzes.

- **Das kannst du jetzt erzählen:** „Netzbetreiber simulieren mit Siemens-Software ihr Stromnetz, damit Solaranlagen und Ladesäulen angeschlossen werden können, ohne dass Leitungen überlasten.“

#### Technik (nur Entwicklerteam)
DC-Lastfluss `B·θ = P`, Leitungsfluss `P_ij = B_ij (θ_i − θ_j)`, Slack = Umspannwerk; 24 quasistationäre Stunden, Batterie koppelt Zeitschritte. Gleicher Graph-Laplace-Operator und CG-Solver wie MG01.

---

### MG07 – Hitzewelle (Finale)

**Siemens-Bezug:** Siemens Xcelerator / Digitaler Zwilling – alles verbunden.

#### Für Spielende
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

#### Technik (nur Entwicklerteam)
Co-Simulation mit festen Kopplungsschritten aus vereinfachten, schnellen Varianten von MG01, MG04, MG05, MG06.

---

### Transfer-Karte „So macht's Siemens“ (Inhaltsvorgabe)

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
