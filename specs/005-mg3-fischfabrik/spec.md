# Spec 005 – MG3 „Fischfabrik“ (Prozesse)

> Status: **umgesetzt auf `feature/mg3-fischfabrik`**, noch nicht in den Spielablauf eingehängt (macht der Integrator, Abschnitt 6).
> Grundlage: docs/00, docs/03 (MG3), docs/04 (4.3 `DiscreteEvent`, 5.5, 5.7), docs/07. Muster: MG1 (Spec 001/002, ADR-20).
> Jede Anforderung hat eine Prüfart: **auto** (pytest / dotnet test / Lint) oder **manuell** (Editor, Playtest).

---

## 1. Umfang

| Drin | Bewusst nicht drin |
|---|---|
| Kalles Versuch „in echt“ in Olafs Halle (eigene Szene, kein Raster, kein ZWILLI) | Takt des Polar-Express aus dem MG2-Ergebnis (docs/03 „optional“) |
| Schritt 1 „Fabrik bauen“ (Schwerpunkt): Karten „Was ist wichtig?“, dann Bausteine auf ZWILLIS Bildschirm | Normalverteilte Arbeitszeiten (nur Dreieck, siehe 3.2) |
| Schritt 2: Supermarkt-Kasse, dann Rechentafel einer Station, Ereignis für Ereignis | echte Wegreservierung der Robo-Robben (vereinfacht, siehe 3.2) |
| Schritt 3: Weg des Fischs als Pfeile (Stationen der Reihe nach antippen) | Englische Texte |
| Schritt 4: eine Schicht im Zeitraffer, Engpass blinkt, Foto aus der Halle daneben, Erklärung der Simulation | Speichern (GameState-Felder liegen vorerst in `FishFactoryState`) |
| Schritt 5 (Schwerpunkt) „Die Möwen-Frage“: „Noch ein Tag“, „10 Tage simulieren“ als Balken | Hub-Ort „Küste“, Ortsschild, Kartenzähler (Integrator) |
| Schritt 6 „Besser machen“: Budget, je Runde 10 Tage, Ampeln + Sterne, Rundenvergleich, Lösungshilfe | |
| Schritt 7 „So macht's Siemens“: Transfer-Karte (ENTWURF), Symbolbild, Foto der besten Fabrik | |
| „Blick unter die Haube“: Ereignisliste, Replikationen, Konfidenzintervall, Formeln (LaTeX) | |

Spielzeit-Schätzung: 9–12 Minuten (Zeitraffer 30 s + 12 s je „Noch ein Tag“ + 8 s je Runde).

---

## 2. Anforderungen

### Simulation (`sim-core/.../Runtime/Process`, Referenz `sim-reference/simref/process*.py`)
| ID | Anforderung | Prüfung |
|---|---|---|
| P-SIM-01 | THE SYSTEM SHALL Zufallszahlen mit PCG32 (XSH RR) ziehen, bitgenau in Python und C#, mit eigenem Strom je Zweck (Kutter, Möwen, jede Station, jede Maschine). | auto – Golden `pcg32`, `Pcg32_MatchesTheReferenceDemo`, pytest `test_pcg32_matches_the_reference_implementation` |
| P-SIM-02 | THE SYSTEM SHALL Ereignisse aus einem Heap nach (Zeit, Einfügenummer) abarbeiten; Python und C# erzeugen dieselbe Ereignisliste. | auto – Golden `trace_olaf`, `trace_kalle`, `trace_hint` (Zeit 1e-9, Art/Kennzahlen exakt) |
| P-SIM-03 | WHEN eine Maschine während der Arbeit ausfällt, THE SYSTEM SHALL die Kiste nach der Reparatur fertig machen und die veraltete Fertig-Meldung verwerfen. | auto – Golden `trace_failures`, pytest `test_interrupted_work_is_finished_after_the_repair` |
| P-SIM-04 | WHEN eine Kiste ins volle Kühlregal soll, THE SYSTEM SHALL sie als verdorben zählen. | auto – Golden `trace_tiny_regal`, pytest `test_full_regal_spoils_what_does_not_fit` |
| P-SIM-05 | THE SYSTEM SHALL jede ankommende Kiste mit der Möwen-Wahrscheinlichkeit als geklaut zählen. | auto – Golden `trace_gulls`, pytest `test_gulls_steal_about_their_share` |
| P-SIM-06 | THE SYSTEM SHALL Wege aus dem Raster ableiten: Nachbarn übergeben, ein Förderband (kürzester Weg) fährt, sonst trägt eine Robo-Robbe hin und zurück; mehr Robben als Parkplätze verlängern jede Fahrt. | auto – Golden `layout`, pytest `test_neighbours_hand_over…`, `test_belt_takes_the_shortest_way…`, `test_more_robots_than_parking…` |
| P-SIM-07 | Keine Kiste SHALL verschwinden: angekommen = geklaut + verdorben + an Stationen + unterwegs + bereit + verladen (zu jeder Zeit). | auto – pytest `test_no_box_vanishes`, NUnit `NoBoxVanishes` |
| P-SIM-08 | THE SYSTEM SHALL 10 Tage (Seed Basis + Tag) rechnen, Mittelwert und 90-%-Vertrauensbereich (t-Quantil) der pünktlichen Kisten bilden. | auto – Golden `day_*`, pytest `test_confidence_interval_uses_the_t_quantile`, `test_t_quantiles_match_scipy` |
| P-SIM-09 | Sterne: ★ Mittel ≥ Ziel (70), ★★ dazu Mittel verdorben ≤ 1, ★★★ dazu Untergrenze ≥ Ziel. Ampeln grün/gelb (≥ 85 %)/rot. | auto – pytest `test_stars_follow_the_rules`, NUnit `Stars_FollowTheRules` |
| P-SIM-10 | Die Rechentafel SHALL dieselbe Simulation mit einer Station sein; jede Zeile geht auf (vorher + an − fertig = neu). | auto – Golden `lesson`, pytest `test_lesson_is_the_same_simulation_as_the_factory`, `test_every_row_adds_up` |
| P-SIM-11 | Analytische Probe: Für eine Station gilt im Mittel L = λ W (Little). | auto – pytest `test_littles_law_holds_for_one_station` |
| P-SIM-12 | Das Level SHALL die Geschichte tragen: Kalles Verdopplung liefert deutlich weniger als Olafs Fabrik; Olafs Engpass ist das Filetieren; die Beispiel-Fabrik erreicht ★★★ im Budget; Tage streuen sichtbar. | auto – pytest `test_kalles_double_robots_make_it_worse`, `test_olafs_factory_stacks_up…`, `test_example_reaches_three_stars…`, `test_one_day_is_not_enough` |

Kennzahlen des Levels (Seed 2026, 10 Tage, aus `golden/process/day_*.json`):

| Aufbau | Mittel pünktlich | Untergrenze | verdorben | Sterne |
|---|---|---|---|---|
| Olafs Fabrik (3 Robo-Robben) | 58,5 | 54,5 | 34,4 | 0 |
| Kalle (6 Robo-Robben) | 36,4 | 33,8 | 11,8 | 0 |
| kompakt, je 1 Station | 58,8 | 54,9 | 34,3 | 0 |
| 2 Filetier-Stationen, 2 Regale, nur 2 Robben | 60,3 | 57,2 | 0,0 | 0 |
| Beispiel (2 Filetieren, 2 Regale, Band) | 93,6 | 83,4 | 0,1 | 3 |

### Spiel (Unity, `Scripts/Minigames/MG3_Fischfabrik/`)
| ID | Anforderung | Prüfung |
|---|---|---|
| P-UX-01 | Kalles Versuch SHALL in echt laufen: Hallen-Look ohne Raster und ohne ZWILLI, Kisten stapeln sich sichtbar, Robben zittern im Gedränge, danach ein Foto. | manuell; auto – `test_kalle_attempt_runs_in_the_real_hall_before_the_lab` |
| P-UX-02 | Bauen und Rechnen SHALL auf ZWILLIS Bildschirm stattfinden (dunkler Rahmen, Leuchtkante, Gitter, Beschriftung). | manuell |
| P-UX-03 | Die Werkzeugleiste SHALL Sortieren, Filetieren, Verpacken, Kühlregal, Förderband, Robo-Robbe, Verschieben, Radierer, Rückgängig und das Muschel-Budget zeigen; Grenzen und Budget erklären sich per Hinweis. | manuell |
| P-UX-04 | „Fabrik fertig“ / „10 Tage simulieren“ SHALL erst laufen, wenn der Fisch überall durchkommt; sonst nennt ein Hinweis, was fehlt. | manuell; auto – `MissingParts_AreReported_AndCannotRun` |
| P-UX-05 | Die Rechentafel SHALL Zeile für Zeile von links nach rechts füllen, nach der ersten fertigen Kiste „Kisten verschwinden nicht“ zeigen; Formeln nur im Pop-up „Blick unter die Haube“. | manuell; auto – Text-Lint |
| P-UX-06 | Im Zeitraffer SHALL eine Schicht ≤ 30 s dauern; Kistenstapel wachsen, Stationen mit ≥ 5 wartenden Kisten blinken, Lämpchen zeigen Arbeit/Störung. | manuell |
| P-UX-07 | „Noch ein Tag“ SHALL denselben Plan mit dem nächsten Seed zeigen; „10 Tage simulieren“ SHALL die Tage als Balken mit Bestell-Strich zeigen (Tag 1 = der Tag aus Schritt 4). | manuell |
| P-UX-08 | Ampeln SHALL Farbe UND Zeichen (OK / ! / X) haben. | manuell |
| P-UX-09 | Knöpfe SHALL gesperrt sein, solange ein Dialog oder Fenster offen ist (`Hud.LockWhileBlocking`). | manuell |
| P-UX-10 | Zoom SHALL während MG3 an sein und am Ende zurückgesetzt werden. | auto – `test_zoom_is_switched_on_and_reset`; manuell |
| P-UX-11 | Nach Runde 2 ohne Stern SHALL ein passender Tipp kommen (Engpass / Kühlregal / Robo-Robben), nach Runde 3 ohne Stern die Beispiel-Fabrik ladbar sein. | manuell |
| P-UX-12 | Die Transfer-Karte SHALL ohne Freigabe als ENTWURF erscheinen; Tecnomatix Plant Simulation nur als Vorschlag. | auto – `test_every_transfer_card_is_a_draft_until_approved`, `test_transfer_card_is_complete` |
| P-UX-13 | Alle Texte SHALL in JSON stehen, ohne Formeln/Fachwörter im Standardmodus; Sprechblasen ≤ 140 Zeichen. | auto – `tools/text_lint.py` |
| P-UX-14 | Farben SHALL nur aus dem Siemens-Schema kommen (`Palette.Mg3.cs`), Formen nur über `Prims.Make`. | auto – `tools/style_lint.py` |

---

## 3. Modell

### 3.1 Ablauf einer Kiste
Anlegestelle (D) → Sortieren (S) → Kühlregal → Filetieren (F) → Verpacken (P) → Verladung (T).
Kutter: Abstände exponentiell (Mittel 20 min) bis Minute 400, je 4 Kisten. Polar-Express: alle 60 min (ab 60) – nimmt alle fertigen Sushi-Kisten mit (das ist „pünktlich“) und lädt 2 Nachschub-Kisten ab. Schicht 480 min (6:00–14:00). Arbeitszeiten dreieckverteilt, Störungen je Maschine (MTBF/MTTR exponentiell). Kühlregal: 6 Plätze je Regal vor dem Filetieren; was nicht hineinpasst, verdirbt.

### 3.2 Vereinfachungen (auch im Technik-Text `mg3TechSimplified`)
| Vereinfachung | Warum |
|---|---|
| Robo-Robben ohne echte Wegreservierung: Abstand Manhattan, je Fahrt + 2,5 min für jede Robbe über 3 Parkplätzen | Zeigt Kalles Fehler („stehen sich im Weg“) mit einer Zahl statt Wegnetz, Kollisionen und Verklemmungen |
| Nur Dreiecksverteilung, keine Normalverteilung | Python und C# rechnen nur `log` und `sqrt`; Winkelfunktionen (Box-Muller) könnten in der letzten Stelle abweichen |
| Puffer unbegrenzt außer dem Kühlregal; kein Blockieren | Ein Ort, an dem Verderb entsteht und den der Spieler mit Bausteinen beeinflusst |
| Ein Regalplatz je Kiste, egal wo das Regal steht | Lage des Regals ist Dekoration; Lage der Stationen zählt |
| Störungen laufen auch, wenn die Maschine wartet | einfacher, üblich als „zeitbasiert“ |
| „Pünktlich“ = vom Zug mitgenommen; letzte Abfahrt um 14:00 | kein eigenes Bestellsystem |
| ★★★ = Untergrenze des 90-%-Vertrauensbereichs des Mittelwerts ≥ Ziel (docs/03) | im Spiel „auch an schlechten Tagen“ – streng genommen eine Aussage über den Mittelwert, nicht über jeden Tag |

### 3.3 Bausteine, Kosten, Grenzen
Sortieren 300, Filetieren 400, Verpacken 300, Kühlregal 100, Förderband 20 je Feld, Robo-Robbe 100 (Muscheln). Startgeld 1 500, für „Besser machen“ 2 400. Höchstens 3 je Station, 4 Regale, 40 Bandfelder, 6 Robben. Raster 12 × 7, Anlegestelle links, Verladung rechts, zwei Säulen.

---

## 4. Annahmen (bitte bestätigen oder ändern)
| ID | Annahme |
|---|---|
| A3-01 | Olaf ist Fischer („Fischer Olaf“), Sprecherfarbe Siemens Purple #805CFF, Figur vorerst `Prims.Look.Plain` mit Namensschild. |
| A3-02 | Kalles Versuch ist Teil von `FishFactoryMinigame.Play()` (Feld `PlayHall`); die Halle liegt 60 m südlich des Labors. |
| A3-03 | Geld heißt „Muscheln“. |
| A3-04 | Schritt 1 ist „Fabrik bauen“ (Schwerpunkt) und beginnt mit sechs Karten wie MG1. |
| A3-05 | Die Möwen-Frage ist eine eigene Phase in der Leiste (7 Schritte statt 6). |
| A3-06 | Zeitraffer: 24 s (Halle), 30 s (Schritt 4), 12 s („Noch ein Tag“), 8 s (je Runde in Schritt 6). |

## 5. Offene Fragen
- Soll der Takt des Polar-Express aus dem MG2-Ergebnis kommen (docs/03)? Dann braucht MG3 eine Schnittstelle zu MG2.
- Figur und Kleidung für Olaf (Integrator / Stilprobe).
- Freigabe der Transfer-Karte (Digital Industries, Kommunikation); echtes Bild statt Symbolbild.
- Reicht Little als analytische Probe, oder zusätzlich M/M/1 (bräuchte exponentielle Arbeitszeiten)?

## 6. Integration (geteilte Dateien – nicht in diesem Paket geändert)
- `GameRoot.Flow`: nach MG1/MG2 z. B.
  `yield return Hud.FadeOut(); _hub.Exit(); yield return FishFactoryMinigame.Create(this, new Vector3(400f, 0f, 0f)).Play();`
  `yield return FishFactoryTransfer.Play(this); State.MapCards = Mathf.Max(State.MapCards, 3);` (bzw. +1 je Reihenfolge)
- Hub: Figur `olaf` an der Küste mit `SetIdleNode("olaf", "mg3_hub_olaf")`; Ortsschild „Küste – Siemens Digital Industries“.
- `GameState`: Felder aus `FishFactoryState` (Done, BestStars, BestLayout, Snapshot, HallPhoto) übernehmen.
- `Prims.Look`/`PenguinFigure`: eigener Look für Olaf (z. B. Fischerhut), dann in `FishFactoryHall.Figure` statt `Look.Plain`.

## 7. Manuelle Prüfung im Editor
1. Halle: Wirkt sie „echt“ (Boden, Wände, Schild, kein Gitter, kein ZWILLI)? Stapeln sich Kisten am Steg und im Kühlregal, zittern die Robben, legen Kutter an, fährt der Zug? Foto erscheint in Schritt 4 links.
2. Labor: Brett links der Mitte, Team links auf Laborboden; nichts wird von Ergebnisfeld (rechts) oder Werkzeugleiste (unten) verdeckt (1920 × 1080 und 1366 × 768).
3. Werkzeugleiste: Beschriftungen „Name / Preis“ passen in die Knöpfe; Budget-Anzeige lesbar; Verschieben (antippen/ziehen), Radierer, Rückgängig, Grenzen- und Budget-Hinweise.
4. Rechentafel: Tafel oben verdeckt die Station nicht; Zeilen füllen sich, nach 4 Zeilen rollt die Tafel; Notiz nach der ersten fertigen Kiste.
5. Pfeile: Stationen der Reihe nach antippen, Fehlhinweis, nach drei Fehlern zeichnet ZWILLI selbst.
6. Zeitraffer: Uhr läuft, Stapel wachsen, Lämpchen grün/rot, Engpass blinkt, Möwen fliegen mit Fisch davon.
7. Möwen-Frage: Balken erscheinen nacheinander, Strich bei 70, Farben grün/orange.
8. Besser machen: Ergebnisfeld ohne Überlappung (Ampeln, Balken, Vergleich, Sterne, Knöpfe), Tipps nach Runde 2, Beispiel-Fabrik nach Runde 3.
9. Blick unter die Haube (Einstellungen an/aus): Pop-up mit Ereignisliste, Konfidenzintervall, Formeln lesbar.
10. Transfer: ENTWURF-Stempel, Symbolbild, Foto der besten Fabrik, Fragen „Kurz nachgedacht“.

## 8. ADR-Vorschläge (Nummern vergibt der Integrator)
- **ADR-? PCG32 mit Strömen je Zweck** statt eines Generators für alles – gemeinsame Zufallszahlen machen Runden vergleichbar. Verworfen: `System.Random`/`random` (nicht gleich in beiden Sprachen).
- **ADR-? Robo-Robben-Gedränge als feste Zusatzzeit** statt Wegreservierung. Verworfen: Wegnetz mit Reservierung (viel Code, schwer zu erklären, Verklemmungen).
- **ADR-? Rechentafel MG3 auf derselben Simulation** (eine Station, gleiche Klasse) – keine zweite Wahrheit (wie ADR-12).
- **ADR-? Möwen-Frage als eigene Phase** in der Leiste (7 statt 6 Schritte).
