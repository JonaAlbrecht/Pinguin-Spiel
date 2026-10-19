# Spec 004 – MG2 „Polar-Express“ (Strömung, Siemens Mobility)

> Status: **gebaut auf `feature/mg2-polar-express`**, noch nicht im Spielablauf verdrahtet (der Integrator ruft es aus GameRoot/Hub auf, siehe Abschnitt 6).
> Grundlage: docs/00, docs/02 (Akt 2), docs/03 (MG2), docs/04 (Flow), MG1 als Muster (Spec 001/002).
> Jede Anforderung hat eine Prüfart: **auto** (pytest / dotnet test / Lint) oder **manuell** (Editor, Playtest).

---

## 1. Umfang

| Gebaut | Bewusst nicht (noch) |
|---|---|
| Kalles Versuch draußen in echt (eigene kleine Tundra-Szene, Foto) | MG2-B „Kühlluft im Schaltschrank“ |
| Schritte 1–5 im Labor auf ZWILLIS Bildschirm (Windkanal) | Englische Texte |
| Rechentafel „Fünf Kästchen Luft“ (Ausgleichen + Weiterschieben) mit echten Modellzahlen | Speichern über die Sitzung hinaus |
| „Genau hinschauen kostet Zeit“: grob / mittel / fein live gerechnet | Eigene Tilda-Figur (Aussehen) – jetzt `Look.Plain` mit Namensschild |
| Nase mit 4 Ziehpunkten, 2 Wagenübergänge verkleiden, 3 Runden, Ampel + Sterne | Verdrahtung in GameRoot/Hub/Landkarte (Integrator) |
| „So macht's Siemens“ (Entwurf, Symbolbild) + Pips Zug fährt draußen davon | Freigegebenes Bild/Video von Siemens Mobility |

Spielzeit geschätzt 9–11 Minuten (davon ~1,5 Minuten Rechenzeit im Zeitraffer).

---

## 2. Anforderungen

### Simulation (`sim-core/Runtime/Flow`, `sim-reference/simref/flow*.py`)
| ID | Anforderung | Prüfung |
|---|---|---|
| FLOW-01 | THE SYSTEM SHALL die Luft im Längsschnitt mit Lattice-Boltzmann D2Q9, BGK-Stoß rechnen (Kanal 160 × 64 fein, 120 × 48 mittel, 80 × 32 grob); C# und Python stimmen je Schritt überein (≤ 1e-4 relativ). | auto – `FlowGoldenTests.TrainRun_MatchesPythonReference` (9 Fälle: Kraft alle 50 Schritte, Strömungsfeld, Beiwert) |
| FLOW-02 | Ränder: am Zug Bounce-Back (halber Abstand), oben/unten glatt (Spiegelung), links Einströmen mit sanftem Anlauf, rechts Ausströmen mit festem Druck. | auto – `test_bounce_back_reverses_a_population`, `EmptyChannel_CarriesTheInflow_WithoutDrag`, Golden |
| FLOW-03 | Im geschlossenen Kasten SHALL keine Luft verloren gehen; Ausgleichen ändert Luftmenge und Schwung eines Kästchens nicht. | auto – `ClosedBox_KeepsItsAir_AndSettles`, `test_collision_keeps_air_and_push_in_every_cell` |
| FLOW-04 | Luftwiderstand per Impulsaustausch, Widerstandsbeiwert = mittlere Kraft der letzten 500 (fein) Schritte / (½ u² · Zughöhe in Kästchen). | auto – Golden `drag` |
| FLOW-05 | Gleiche Reynolds-Zahl (20) und gleiche Zeit in jeder Auflösung (tau und Schrittzahl skaliert). Grob SHALL Kalles Kiste und die runde Startnase nicht unterscheiden (gleiche Maske), fein schon. | auto – `test_grob_cannot_tell_kalles_box_from_the_rounded_nose`, `Grob_CannotTellKallesBoxFromTheRoundedNose`, `test_finer_resolution_converges` |
| FLOW-06 | Zugnase = kubische Bézier-Kurve aus 4 Ziehpunkten (P0 Stirnhöhe, P3 Nasenlänge, P1/P2 frei, x aufsteigend, geklemmt); Heck = Spiegelbild; Maske = Kästchenmitte unter der Dachlinie. | auto – `test_profile_follows_the_handles`, `Clamp_KeepsHandlesInTheNoseArea`, Golden `mask` |
| FLOW-07 | Offene Wagenübergänge ragen kantig über das Dach (bis Höhe 16 statt 12); verkleidet sind sie bündig und senken den Beiwert. | auto – `test_covered_transitions_lower_the_drag`, Golden `pointy` (ein Übergang offen) |
| FLOW-08 | Plätze = Spalten mit ≥ 80 % Kopfhöhe ohne Übergänge, je 4 Spalten 4 Plätze; lange Nase kostet Plätze. | auto – `test_seats_drop_with_a_long_nose`, `Seats_DropWithALongNose…` |
| FLOW-09 | Fahrzeit aus `F = R + C·(v + w)²` (w Gegenwind) → `v = sqrt((F−R)/C) − w`, mindestens `minSpeed`; Fahrzeit = Strecke / v. | auto – `test_travel_time_follows_the_force_balance`, `Travel_MatchesPythonReference` |
| FLOW-10 | Sterne: ★ Fahrzeit ≤ 5,5 Spiel-Stunden, ★★ dazu Plätze ≥ 64, ★★★ dazu ≤ 4,3 Stunden. Kalles Kiste 0 Sterne und > 2,5-mal so langsam wie der Beispiel-Zug; Beispiel-Zug ★★★; sehr spitze Nase schnell, aber nur ★ (zu wenig Plätze). | auto – `test_kalle_nearly_stops_…`, `test_a_very_pointy_nose_…`, `test_stars_follow_the_rules` |
| FLOW-11 | Rechentafel: fünf Kästchen, geschlossene Enden, dasselbe `Lbm` wie der Kanal. Je Schritt `neu = vorher + kommt an − zieht weiter` exakt; Summe bleibt 5,6 (angezeigt 56,0). | auto – `test_lesson_table_adds_up_exactly`, `test_lesson_air_is_never_lost`, `Lesson_MatchesPythonReference_StepByStep` |
| FLOW-12 | Die angezeigte (gerundete) Tafel SHALL in jeder Zeile aufgehen; kein angezeigter Wert weicht mehr als eine Stelle vom echten ab. | auto – `Lesson_RoundedRowsAddUp` |
| FLOW-13 | Steppable (`IStepSolver`), keine Allokation im Schritt, deterministisch (kein Zufall im Modell). Fein: 1,76 ms je Schritt in .NET Release (2000 Schritte = 3,5 s). | auto – Golden; Zeit: `FlowBenchmark_StepTime` (Explicit) |

### Spiel (Unity, `Scripts/Minigames/MG2_PolarExpress`)
| ID | Anforderung | Prüfung |
|---|---|---|
| MG2-01 | Kalles Versuch läuft draußen in echt (Tundra, Gleis, Schneeflocken im Gegenwind, Kamera auf Augenhöhe, kein Raster, kein ZWILLI); der Schlitten ruckt los und kriecht dann nur noch. Foto für Schritt 4. | auto – `test_kalle_tries_outside_before_the_lab`; manuell |
| MG2-02 | Im Labor ist der Kanal erkennbar ZWILLIS Bildschirm (dunkler Rahmen, Leuchtkante, „ZWILLI – der Zug im Windkanal“), Gegenwind-Pfeil links. | manuell |
| MG2-03 | Schritt 1: fünf Karten (3 richtig, 2 Ablenker mit Hinweis). | auto – `test_cards_follow_docs_03` |
| MG2-04 | Schritt 2: „Hand aus dem Autofenster“ (flach/schräg), danach Rechentafel mit Pfeilen je Kästchen: erst Ausgleichen, dann Weiterschieben; Erhaltungssatz nach Schritt 1; dann rechnet ZWILLI 40 Schritte weiter, bis die Luft fast gleich verteilt ist. Formeln nur im Fenster „Blick unter die Haube“. | auto – `test_lesson_table_follows_the_rule_in_step_2`; manuell |
| MG2-05 | Schritt 3: grob/mittel/fein wählen; ZWILLI rechnet Kalles Kiste und die runde Nase live, Kästchengrenzen sichtbar (grob/mittel); Anzeigen „Wie genau?“ und „Wie lange rechnet's?“ + gemessene Sekunden; „Weiter“ nach mindestens zwei Auflösungen. | manuell |
| MG2-06 | Schritt 4: Kalles Zug fein im Zeitraffer (≤ ~9 s), Wirbel rot, Lichtpunkte als Strömungslinien; rechts das Foto von draußen; danach Erklärung mit echten Zahlen (Kästchen, Schritte) und Ehrlichkeitssatz. | manuell |
| MG2-07 | Schritt 5: vier gelbe Ziehpunkte ziehen (Maus/Touch), zwei Übergänge per Knopf verkleiden, Rückgängig, „Tildas Zug“ (zurücksetzen); Plätze ändern sich sofort (Ampel). 3 gewertete Runden, danach Extra-Runden; Tipp nach Runde 2 ohne Stern (Übergänge/Nase), Beispiel-Zug nach Runde 3 ohne Stern; Ergebnis mit Ampeln (Fahrzeit, Plätze, Übergänge) + Sterne + Vergleich mit der Vorrunde. | manuell |
| MG2-08 | Zoom während des ganzen Minigames (`Rig.ZoomEnabled`), am Ende zurückgesetzt. | auto – `test_zoom_is_switched_on_and_reset`; manuell |
| MG2-09 | Knöpfe und Leisten sind ausgegraut, solange ein Dialog läuft (`Hud.LockWhileBlocking`). | manuell |
| MG2-10 | Nach Schritt 5 fährt Pips bester Zug draußen davon (Tempo nach Ergebnis), Kalle staunt. | manuell |
| MG2-11 | Transfer-Karte `transfer_MG2_velaro.json`: Entwurf (Freigabe leer), Produkt als Vorschlag, Symbolbild erzeugt, links das Strömungsbild der besten Runde. | auto – `test_transfer_card_is_a_draft_with_a_takeaway`, `test_every_transfer_card_is_a_draft_until_approved` |
| MG2-12 | Alle Texte in JSON, Sprachregeln, eigene Schlüssel mit Präfix `mg2`, keine Kollision mit ui_de/dialogue_de. | auto – Text-Lint, `test_ui_keys_are_prefixed_…`, `test_dialogue_ids_…` |
| MG2-13 | Jede Glossar-Phase wird ausgelöst (Luftwiderstand, Erhaltung, Strömen, Auflösung, Wirbel, Windschnittig, Windkanal). | auto – `test_every_glossary_phase_is_triggered` |

---

## 3. Vereinfachungen gegenüber docs/03 und docs/04 (alle ehrlich benannt, im Spiel unter „Blick unter die Haube“)

1. **Reynolds-Zahl 20** (zähe Luft, laminar) statt Millionen; 2D statt 3D. Die Wirbel sind Ablösezonen, keine turbulenten Wirbel.
2. **Glatter Boden** (Spiegelung, kein Haften, kein bewegter Boden im Zugsystem); Zug steht direkt auf dem Boden.
3. **Wagenübergänge als kantiger Wulst über dem Dach**, nicht als Lücke: Eine Lücke nach unten änderte den Beiwert bei Re 20 nicht messbar (geprüft 19.10.2026: 2,989 offen vs. 2,990 verkleidet), ein Wulst schon (+6–9 %).
4. **Heck = Spiegelbild der Nase** (Schnellzüge fahren in beide Richtungen) – eine lange Nase kostet darum doppelt Platz.
5. **Fahrzeit-Modell** mit konstanter Zugkraft, Rollwiderstand und Gegenwind; „Spiel-Stunden“ sind frei kalibriert (Kiste 10,7 h, Beispiel-Zug 4,0 h). Der Faktor `dragScale` 0,675 bildet den Beiwert bei Re 20 auf die Spielwelt ab.
6. **Ausströmen mit festem Druck** statt Kopie der Nachbarspalte: Mit Kopie wanderte der Druck, die Luft strömte 27 % langsamer als eingestellt (Test `EmptyChannel…` fand das).
7. **Sanfter Anlauf** (400 Schritte fein, halbe Kosinuswelle) statt Start mit voller Strömung: sonst laufen starke Druckwellen durch den Kanal und der Widerstand schwingt.
8. **Mittelung über die letzten 500 Schritte** (fein) – der Beiwert ist nach 2000 Schritten auf ~1 % eingeschwungen (Probe mit 4000 Schritten).
9. **Rechentafel mit tau = 1** (gleicht in einem Schritt ganz aus) und eigener kleiner Reihe – dieselben Funktionen wie der Kanal, nur andere Zahlen (wie MG1 „Zwei Felder“).
10. **Gerundete Tafel**: „kommt an“/„zieht weiter“ werden so gerundet, dass jede Zeile aufgeht (≤ 0,1 Abweichung, getestet).
11. **Kein Pcg32 im Modell nötig** – die Lichtpunkte (nur Anzeige) nutzen `System.Random` mit festem Startwert.

## 4. Annahmen

| Thema | Annahme |
|---|---|
| Spielauflösung | fein (`playResolution` im Level-JSON). In WebGL evtl. 2–3-mal langsamer als .NET: Das Budget je Bild steigt dann über 4 ms, damit der Lauf ≤ ~9 s bleibt (Bildrate sinkt während des Laufs). Alternativ `"mittel"` eintragen. |
| Budget in Runde 5 | Es gibt keinen Geld-Topf; der Zielkonflikt ist Nase gegen Plätze (docs/03). Verkleiden ist kostenlos, muss aber entdeckt werden. |
| Tilda | `Prims.Look.Plain` mit Namensschild in Siemens Purple `#805CFF`; eigenes Aussehen (Stoppuhr) macht der Integrator. |
| Ort | Labor-Brett an `origin`, Tundra bei `origin + (0, 0, 120)`. |
| ZWILLI | Startet mit Stottern 0,5 (aus MG1 schon klüger) und wird bis 0,15 stabil. |

## 5. Offene Fragen

1. **Freigabe** Siemens Mobility: Velaro Novo als Beispiel, Bild/Video, Kundennutzen-Sätze (Karte ist ENTWURF).
2. **WebGL-Messung**: Reicht fein im Zeitraffer auf einem Office-Laptop? Sonst `playResolution` = mittel (Rechentafel und Schritt 3 bleiben gleich).
3. Soll MG3 den Zugtakt aus dem MG2-Ergebnis übernehmen (docs/03 MG3 Technik)? `Mg2Progress.BestResult.Speed` steht bereit.
4. Ist „Spiel-Stunden“ als Zeitangabe für Nicht-Techniker verständlich, oder lieber nur Ampel und „schneller/langsamer“?

## 6. Einbau (für den Integrator)

```csharp
// GameRoot.Flow – nach MG1 (oder vom Hub-Gleis aus)
yield return Hud.FadeOut();
_hub.Exit();
yield return PolarExpressMinigame.Create(this, MinigameOrigin).Play();   // Kalles Versuch draußen ist enthalten
Hud.ShowPhases(5);
yield return PolarExpressTransfer.Play(this);
Hud.ShowPhases(-1);
State.MapCards = Mathf.Max(State.MapCards, 2);
State.Takeaway = PolarExpressTransfer.Takeaway();
```
`Mg2Progress.Of(State)` hält beste Runde, Form, Strömungsbild und Kalle-Foto (kann nach GameState wandern).

## 7. Vorgeschlagene ADRs (Nummer vergibt der Integrator)

- **ADR-? MG2 rechnet Lattice-Boltzmann D2Q9 bei Re 20**, nicht Potentialströmung: Nur so gibt es Widerstand und Wirbel. Verworfen: Potentialströmung (kein Widerstand), Navier-Stokes mit Druckkorrektur (zu aufwendig für die Tafel).
- **ADR-? Ausströmen mit festem Druck + sanfter Anlauf** statt Kopie der Nachbarspalte. Verworfen: Kopie (Druckdrift, 27 % zu langsam), Start mit voller Strömung (Druckwellen).
- **ADR-? Wagenübergänge als Wulst**, weil eine Lücke bei Re 20 keinen Unterschied macht. Verworfen: Lücke (wirkungslos), höhere Reynolds-Zahl (instabil in grob, viermal mehr Schritte).
- **ADR-? Quelltabelle für das Strömen** (ein Index je Ziel-Anteil), identisch in Python und C# – macht alle Randregeln an einer Stelle prüfbar.
- **ADR-? Minigame-Zustand per `Mg2Progress`** (an GameState angehängt), damit parallele Pakete GameData.cs nicht ändern.

## 8. Manuelle Prüfung im Editor (ca. 20 Minuten)

1. Minigame starten (Abschnitt 6). Tundra: Gleis, Schlitten mit flacher Front, Schneeflocken fliegen von links nach rechts; Schlitten ruckt an und kriecht; Foto entsteht ohne Sprechblase.
2. Labor (1920 × 1080): Kanal links oben (ungefähr x 100–1300, y 210–650), rechts frei für Tafeln; Pinguine vor dem Bildschirm sichtbar, nicht unter der Sprechblase. Auch 16:10 und 4:3 prüfen.
3. Schritt 1: Karten in zwei Reihen (3 + 2) mittig; falsche Karte → Hinweis.
4. Schritt 2: Hand-Karten mit Windpfeilen; Rechentafel: Pfeile gleichen sich an, wandern, prallen an den Wänden ab; Tabellenzeilen füllen sich; Summe bleibt 56,0; „Blick unter die Haube“ zeigt 4 Formeln lesbar.
5. Schritt 3: grob zeigt Treppen-Nase und Kästchenlinien; Ergebnis „keinen Unterschied“; fein zeigt einen Unterschied; Sekunden plausibel; „Weiter“ erscheint nach zwei Auflösungen.
6. Schritt 4: Zeitraffer ≤ ~9 s, Lichtpunkte folgen der Strömung, rote Zonen hinter Heck und Wülsten; Foto rechts; Zahlen-Hinweis; Formel-Fenster.
7. Schritt 5: Ziehpunkte greifen (auch mit Zoom und Touch), P0 nur senkrecht, P3 nur waagerecht; Plätze-Ampel schaltet bei zu langer Nase auf rot; Übergänge-Knöpfe schalten Farbe und Wulst; Rückgängig/„Tildas Zug“; Ergebnis-Tafel überdeckt den Kanal nicht; Beispiel-Zug nach drei Runden ohne Stern.
8. Danach fährt Pips Zug draußen davon; Transfer-Screen mit ENTWURF-Stempel, Strömungsbild links, Symbolbild rechts, zwei Fragen „Kurz nachgedacht“.
9. Während jedes Dialogs sind Knöpfe ausgegraut; Zoom (Mausrad, Touchpad, +/−) geht und ist am Ende zurückgesetzt.
10. WebGL-Build: Bildrate und Dauer eines feinen Laufs messen (Ziel ≤ 10 s).
