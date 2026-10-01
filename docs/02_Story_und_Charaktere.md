# 02 – Story & Charaktere

## 1. Die Welt: Pingopolis

Als die Eisschollen der Kolonie immer kleiner werden, bauen die Pinguine etwas Neues: **Pingopolis**, eine schwimmende Smart City am Rand des Packeises. Alles ist vernetzt – Gebäude regeln ihr Klima selbst, Drohnen liefern Fisch, eine Hochbahn verbindet die Viertel. Das Herz der Stadt ist der **Zwillingsturm** mit **ZWILLI**, dem Digitalen Zwilling der Stadt: Er simuliert alles, bevor es gebaut oder verändert wird.

### Viertel (Hub-Struktur)

```
                 ┌──────────────┐
                 │  Windkante   │  Hochhäuser, Drohnen-Lieferrouten   (MG2 Strömung)
                 └──────┬───────┘
 ┌────────────┐  ┌──────┴───────┐  ┌──────────────┐
 │ Frostgarten│──│ Zwillingsturm│──│  Voltkai     │  Solar, Batterien, Umspannwerk (MG6 Netz)
 │ Wohnviertel│  │  (Zentrum,   │  └──────────────┘
 │ (MG1 Wärme)│  │   Hub)       │
 └────────────┘  └──────┬───────┘
 ┌────────────┐  ┌──────┴───────┐  ┌──────────────┐
 │ Kanalbogen │──│  Fischhafen  │──│ Express-Ring │  Hochbahn-Linie            (MG5 Bahn)
 │(MG3 Brücke)│  │ (MG4 Fabrik) │  └──────────────┘
 └────────────┘  └──────────────┘
```

Im Prolog: **Die alte Scholle** (Antarktis, MG0 Huddle).

---

## 2. Storyline

**Leitmotiv:** *„Erst simulieren, dann bauen.“*
**Zweites Motiv:** *Von der Kolonie zur Stadt – die Naturgesetze bleiben dieselben.*

### Prolog – Die alte Scholle (Tutorial, ~5 Min.)
Ein Schneesturm zieht über die Kolonie. Der Spieler (Pip) organisiert den Huddle so, dass kein Pinguin auskühlt (= ursprüngliches Huddle-Minigame, vereinfacht). Die Großmutter **Oma Grete** erklärt: „Jeder wärmt seinen Nachbarn – so überlebt die ganze Kolonie.“ Am Ende bricht die Scholle; die Kolonie folgt dem Licht von Pingopolis.
*Didaktik:* erste Begegnung mit „lokale Beziehung → Gesamtsystem“.

### Akt 1 – Ankunft im Blackout (~10 Min.)
Pip kommt in Pingopolis an – doch der **Polarsturm „Boreas“** hat gerade den Zwillingsturm getroffen. ZWILLI ist in sechs Fragmente zerfallen; die Stadt läuft „blind“. Bürgermeisterin **Emma Eisberg** braucht Hilfe, Bauunternehmer **Kalle Klotz** will alles „einfach schnell wieder aufbauen – wird schon halten!“.
**Prof. Dr. Frieda Frost**, Leiterin des Simulationslabors, nimmt Pip als Junior-Simulationsingenieur auf. Auftrag: In jedem Viertel das Simulationsmodell neu aufbauen und so ein ZWILLI-Fragment zurückgewinnen.
→ Hub wird freigeschaltet, Frostgarten und Fischhafen sind offen (= Vertical Slice).

### Akt 2 – Die Viertel (frei wählbare Reihenfolge, ~60 Min.)
Jedes Viertel folgt demselben Muster:
1. **Problem**: Ein NPC hat ein akutes Problem (Hitze, Stau, Überlast …).
2. **Kalles Versuch**: Kalle probiert eine Lösung ohne Simulation – sie scheitert sichtbar und komisch (Brücke wackelt, Drohne landet im Kanal, Sicherung fliegt).
3. **Minigame**: Pip simuliert, findet eine robuste Lösung.
4. **Transfer**: Frieda zeigt am Labor-Bildschirm das echte Siemens-Beispiel.
5. **Belohnung**: Viertel verwandelt sich, ZWILLI-Fragment kehrt zurück und kommentiert („Diese Matrix kenne ich doch …“).

Wendepunkte innerhalb von Akt 2:
- **Nach dem 2. Fragment:** ZWILLI erkennt, dass Wärmenetz und Stromnetz „dieselbe Gleichung“ haben → Aha-Moment *Systemaufbau ist universell*.
- **Nach dem 4. Fragment:** Kalle bittet Pip heimlich um Hilfe – er hat verstanden, dass Simulation Geld und Nerven spart. Er wird vom Gegenspieler zum Verbündeten.

### Akt 3 – Die Hitzewelle (Finale, ~10 Min.)
ZWILLI ist wieder ganz – gerade rechtzeitig: Eine **Hitzewelle** rollt auf Pingopolis zu (für Pinguine eine Katastrophe!). Im Leitstand des Zwillingsturms sind alle Viertel-Modelle **gekoppelt**: Hitze → mehr Kühlbedarf → Netzlast steigt → Hafen muss Produktion verschieben → Hochbahn braucht mehr Takt, weil alle ins kühle Hafenbad wollen. Pip trifft Entscheidungen, ZWILLI simuliert die Folgen im Zeitraffer, bevor sie umgesetzt werden.
*Didaktik:* Co-Simulation / Digitaler Zwilling als Gesamtsystem.

### Epilog
Fest auf dem Zentrumsplatz. Kalle eröffnet sein neues Bauunternehmen „Klotz & Zwilling – wir simulieren zuerst“. Oma Grete besucht die Stadt: „Ihr macht ja dasselbe wie wir damals beim Huddle – nur mit mehr Kabeln.“ Freispiel: alle Minigames im Optimierungsmodus, Bestenliste.

---

## 3. Charaktere

### Hauptfiguren

| Figur | Rolle | Didaktische Funktion | Persönlichkeit | Look |
|---|---|---|---|---|
| **Pip** (Spieler) | Junior-Simulationsingenieur:in, frisch aus der Kolonie | Identifikationsfigur, stellt die „naiven“ Fragen | neugierig, mutig, etwas tollpatschig | Kaiserpinguin-Küken (anpassbar: Mütze, Schal, Farbe), Werkzeuggürtel mit Tablet |
| **Prof. Dr. Frieda Frost** | Leiterin Simulationslabor | Erklärt Theorie; präsentiert Transfer-Screens | warmherzig, präzise, trockener Humor, liebt Kaffee mit Eiswürfeln | Felsenpinguin mit gelben Federbüscheln, Laborkittel, Brille |
| **ZWILLI** | Digitaler Zwilling der Stadt (KI-Hologramm) | Fortschrittsanzeige (Fragmente), stellt Querbezüge zwischen Minigames her | anfangs stotternd/glitchig, wird mit jedem Fragment klüger und witziger | Blau leuchtendes Wireframe-Hologramm eines Pinguins; je mehr Fragmente, desto vollständiger das Mesh |
| **Kalle Klotz** | Bauunternehmer, freundlicher Gegenspieler | Verkörpert „Trial & Error“ – zeigt, was ohne Simulation passiert | laut, optimistisch, „Wird schon halten!“, gutes Herz | Stämmiger Königspinguin, gelber Bauhelm, Hosenträger |
| **Emma Eisberg** | Bürgermeisterin | Auftraggeberin, gibt Ziele/KPIs vor (Budget, Komfort, CO₂) | pragmatisch, unter Druck, fair | Adeliepinguin, Amtskette aus Fischgräten |
| **Oma Grete** | Älteste der Kolonie | Prolog/Epilog, verbindet Natur und Technik | weise, verschmitzt | Alt, zerzaust, Strickjacke |

### Viertel-NPCs (Auftraggeber der Minigames)

| Figur | Viertel | Problem | Kurzprofil |
|---|---|---|---|
| **Flora Federweich** | Frostgarten | Hitzeinseln auf dem Spielplatz | Stadtgärtnerin, redet mit Pflanzen |
| **Dr. Rotor** | Windkante | Lieferdrohnen stürzen in Böen ab | Drohnen-Logistikerin, nervös, spricht in Flugcodes |
| **Bruno Bolzen** | Kanalbogen | Hochbahn braucht neue Brücke | Brückenbauer alter Schule, Kalles Rivale |
| **Hafenmeister Olaf** | Fischhafen | Fischkisten stauen sich | Seebär, Pfeife (ohne Tabak), liebt Ordnung |
| **Tilda Takt** | Express-Ring | Bahnsteige überfüllt | Fahrdienstleiterin, Stoppuhr um den Hals |
| **Volta** | Voltkai | Leitungen überlasten mittags/abends | Netzwartin, Funken in den Federn |

### Nebenfiguren / Running Gags
- **Die Möwen** – stehlen Fisch, tauchen unvorhersehbar auf. Sie personifizieren **Zufall/Stochastik** (MG4, MG5): „Die Möwen kommen *im Mittel* alle 5 Minuten – aber eben nur im Mittel.“
- **Robo-Robben** – autonome Transportroboter (AGVs) im Hafen, piepsen beleidigt, wenn sie warten müssen.

---

## 4. Ton & Dialog-Richtlinien

- **Sprache:** Deutsch (primär) und Englisch; kurze Sätze, max. 2 Zeilen pro Sprechblase.
- **Fachbegriffe** werden beim ersten Auftreten markiert und landen automatisch im Simulations-Notizbuch.
- **Humor:** freundlich, nie herablassend gegenüber Spielenden ohne Technik-Hintergrund. Kalle scheitert, aber wird nie lächerlich gemacht.
- **Siemens-Bezug:** nur in Transfer-Screens und bei Frieda – nicht in jedem Dialog (kein Werbespiel).

Beispielszene (MG3, Kanalbogen):

> **Kalle:** Brücke? Kein Problem! Drei Balken, viel Kleber – wird schon halten!
> *(Testzug rollt drauf. Die Brücke biegt sich wie Spaghetti. Zug hält, Passagiere steigen genervt aus.)*
> **Bruno:** Klotz, du Kiesel …
> **Frieda:** Pip, bevor wir echten Stahl verbauen – lass uns die Brücke erst im Rechner belasten.
> **ZWILLI:** K-k-kräfte an den Knoten … Steifigkeitsmatrix … *bzzt* … das Muster kenne ich!
