# Spec 002 – v0.2 „Lernen und Leben“

> Status: **umgesetzt, im Editor noch nicht gespielt** (Branch `feature/v0.2-lernen-und-leben`).
> Grundlage: Review von v0.1 → `.agency/reviews/2026-10-16_review_v0.1.md`. Baut auf Spec 001 auf; was dort steht, gilt weiter.
> Prüfart: **auto** (läuft in CI oder in `tools/unity_compile_check`) oder **manuell** (Editor, Spieltest – mit Schritten).

## 1. Ziel

v0.1 erklärte Simulation, ließ aber zwei Lernziele aus und fühlte sich still an. v0.2 schließt
**Lernziel 2 (Digitaler Zwilling)** und **Lernziel 5 (Grenzen des Modells)**, macht die Schritte 1 und 2
folgenreich, fragt die Abnahme-Kriterien aus docs/00 im Spiel ab und gibt dem Spiel Ton und Leben.

## 2. Neue Figuren

| Figur | Rolle fürs Lernen | Wo |
|---|---|---|
| **Theo Thermo** – Messpinguin mit Thermometer | Lernziel 5: misst bei Kalle nach – „fast so wie gerechnet, aber nicht ganz“. Ersetzt das falsche „genau“. | Schritt 4, Hub |
| **Klara Krill** – Kassenwartin | Lernziel 4 (Nutzen): kommentiert jede Runde im Vergleich, rechnet Kosten in „im Computer kostet es nichts“ um. Spiegel für Finanz/HR-Publikum. | Schritt 5, Transfer, Hub |
| **Flocke** – das Küken aus der Pfütze (docs/02) | stellt die „Warum?“-Fragen, die Pip nicht stellt; sammelt Pfützen (Notizbuch). | Kalles Szene, Was-wäre-wenn, Hub |
| **ZWILLI** (neu sichtbar) | Lernziel 2: Hologramm-Pinguin, stellt sich als „Zwilling eurer Siedlung, nur im Computer“ vor; flackert und stottert weniger, je weiter man kommt. | MG1 ab Start |

## 3. Anforderungen

### Simulation und Tests
| ID | Anforderung | Prüfung |
|---|---|---|
| SIM-09 | Die Schmelz-Kaskade über mehr als einen Durchlauf und die Durchlauf-Grenze sind gegen die Python-Referenz geprüft. | auto – Golden `melt_two_passes` ([2, 1]) und `melt_cap_1` ([2]); Mutation „Kaskade nach 1 Durchlauf“ wird rot |
| SIM-10 | Der Windschatten stimmt für **alle acht** Windrichtungen. | auto – Golden `wind_*`, pytest `test_wall_shields_downwind_for_every_direction`; Mutation „S/SE vertauscht“ wird rot |
| SIM-11 | Über dem Budget gibt es keinen Stern, auch wenn alles gemütlich ist. | auto – Golden `over_budget`, `Stars_AreZero_WhenOverBudget…`; Mutation wird rot |
| SIM-12 | Die **Waage je Feld** (`SettlementBalance`) stimmt mit der Referenz überein, und je Feld gilt *rein = raus*. | auto – `balance` in jedem Golden, `Balance_ClosesForEveryCell…`, pytest `test_balance_closes_for_every_cell` |
| SIM-13 | „Was wäre, wenn“ ohne Wind: Level-Überschreibung `whatIf.noWind` (Wind 8 → 2) macht aus „alle 4 Iglus zu kalt“ ein „alle 4 gemütlich“; eigene Siedlung, nicht die Lösung aus Schritt 5. | auto – Golden `whatif_real`/`whatif_no_wind`, `WhatIf_NoWind_PromisesCosyIgloos…` |
| SIM-14 | Unbekannte Level-Überschreibungen sind ein Fehler (kein stilles Weiterrechnen mit dem Original). | auto – `With_RejectsUnknownKeys` |

### Texte und Werkzeuge
| ID | Anforderung | Prüfung |
|---|---|---|
| TXT-01 | Alle Spieltexte stehen in JSON (`Content/ui_de.json`, Dialoge, Inhalte). Ein Textliteral in Toast/Label/Button im C#-Code bricht den Lint. | auto – `text_lint.lint_csharp` |
| TXT-02 | Der Lint findet Fachwörter auch in zusammengesetzten Wörtern, deutsche Fachwörter („Löser“), Formelzeichen ohne „=“, Einheiten und Zeichen außerhalb der eingebauten Schrift. | auto – `test_lint_catches_what_slipped_through_before` |
| TXT-03 | Jeder Text- und Dialogschlüssel, den der Code anfordert, existiert. | auto – `text_lint.lint_references` (143 Verweise) |
| TST-01 | Spielregeln der Runden (beste Runde, Vergleich, Klaras Kommentar, Skala der Waage, Rückgängig) liegen im Kern und sind getestet; Mutationen werden rot. | auto – `SettlementRoundsTests` |
| TST-02 | Inhaltsdaten sind stimmig: Sprecher, eindeutige Knoten, Farben, Raster 24 × 24, je Frage genau eine richtige Antwort, Platzhalter ab {0}, jede Begriffs-Phase wird ausgelöst. | auto – `test_content.py` |
| TXT-04 | Die Spielschicht kompiliert gegen die DLLs des installierten Unity 6 – altes und neues Input System, Editor-Skripte, ohne veraltete API. | auto (lokal) – `tools/unity_compile_check/check.ps1` |

### Spiel
| ID | Anforderung | Prüfung |
|---|---|---|
| UX-13 | ZWILLI ist als Hologramm sichtbar und stellt sich in Schritt 4 als Digitaler Zwilling vor; Begriff „Digitaler Zwilling“ landet im Notizbuch. | manuell |
| UX-14 | Nach Kalles Simulation misst Theo nach; der Text sagt „ziemlich gut“ und „stark vereinfacht“, nicht „genau“. | manuell + auto (Dialogtext) |
| UX-15 | Was-wäre-wenn: Banner „ZWILLI rechnet fast ohne Wind (nur stille Luft)“ → 4 von 4 gemütlich → Kalle jubelt → „mit Wind“ → 4 von 4 zu kalt. Text und Banner sagen „fast ohne“, weil das Modell mit Wind 2 statt 8 rechnet (Gesetz 7). | manuell |
| UX-16 | Die Waage in Schritt 2 neigt sich nach den gelegten Kärtchen (2 rein, 2 raus → Gleichgewicht). | manuell |
| UX-17 | Nach jeder Runde: Iglu antippen zeigt seine Waage (rein: eigene Wärme + Nachbarn; raus: Nachbarn + Wind/Luft) als Balken ohne Zahlen, **auf einer gemeinsamen Skala für alle Iglus der Runde**. Der Text sagt ehrlich: rein und raus sind eingeschwungen immer gleich – die Balken zeigen, woher die Wärme kommt und wohin sie geht, nicht wie warm das Iglu ist (Test `Throughput_DoesNotTellHowWarmAnIglooIs`). Pfütze → „hier ist ein Iglu geschmolzen“. Im Modus „Blick unter die Haube“ zusätzlich Zahlen. | manuell + auto (Skala, Text) |
| UX-18 | Ab Runde 2 zeigt das Ergebnis den Vergleich zur Vorrunde in Worten plus Klaras Kommentar. | auto – `SettlementRoundsTests` (Vergleich, Kommentar) + manuell (Anzeige) |
| UX-26 | Bei einem Stern lobt Frieda nicht „nichts geschmolzen“, sondern sagt, dass etwas geschmolzen ist (`phase5_one_star`). | manuell |

### Nach dem ersten Spieltest (17.10.2026)
| ID | Anforderung | Prüfung |
|---|---|---|
| UX-27 | Im Hub läuft Pip mit **W A S D / Pfeiltasten** (kamerabezogen); Klicken bleibt für Tablets. Steht Pip bei einer Figur, zeigt sie „E: ansprechen“; **E / Leertaste / Enter** sprechen an und blättern im Dialog weiter. Leertaste/Enter drücken keinen zuletzt geklickten Knopf erneut (EventSystem ohne Navigation). Pip läuft nicht durch Figuren und nicht über den Rand. | manuell |
| UX-28 | Das Simulationslabor ist begehbar: Tür zum Platz, Arbeitstisch; drinnen blenden Dach, Südwand und Schild aus. Wände sind fest. | manuell |
| UX-29 | **Vor dem Huddle die Rechenregel mit zwei Pinguinen:** Grete (warm, im Wind) und Pip (kalt, im Windschatten). Bild-Gleichung *Wärme vorher + selbst erzeugt + vom Nachbarn − an den Wind ergibt Wärme neu*, 5 Zeitschritte per Knopf, Zahlen aus dem Huddle-Modell. Nach Schritt 1: „Was Pip bekommt, gibt Grete ab – zusammen 0“. Formel nur unter „Blick unter die Haube“ (docs/00 Regel 2). Begriff „Zeitschritt“ im Notizbuch. **Seit 18.10.2026 (ADR-20):** dieselbe Tafel mit zwei Feldern, Laterne und Iglu, am Ende von MG1 Schritt 2, 6 Zeitschritte bis nahe an den Gleichgewichtspunkt. | auto – Golden `pair_settlement`, `SettlementPairTests`, pytest `test_settlement_pair.py` + `test_content.py::test_pair_lesson_follows_the_scale_in_step_2` + manuell (Anzeige) |
| UX-30 | **Nach dem Huddle „So macht's Siemens“ mit Batteriezellen im Akku** (Entwurf, Simcenter als Vorschlag): Huddle-Wärmebild neben Akku-Symbolbild, Übersetzungstabelle, eine Kurz-nachgedacht-Frage. Siemens-Landkarte: 4 Karten (Akku, Schaltschrank, MG2, MG3). | auto – `test_both_transfer_screens_are_reached`, `test_every_transfer_card_is_a_draft_until_approved` + manuell |
| TXT-05 | Die Unity-Prüfung kennt auch die Paketliste: Jedes Paket in `manifest.json` muss der installierte Editor kennen (sonst bricht Unity beim Öffnen ab, wie bei `com.unity.modules.textrendering` in Unity 6.4). | auto (lokal) – `tools/unity_compile_check/check.ps1` |
| UX-19 | Die Beispiel-Siedlung lädt nur per Knopf und nur einmal (Spec 001 UX-09 korrigiert). | manuell |
| UX-20 | Ergebnis-Knöpfe erscheinen erst, wenn Frieda fertig gesprochen hat. | manuell |
| UX-21 | „Siedlung nochmal planen“: Eine schlechtere Runde überschreibt die beste nicht (Hub, Endkarte). | auto – `SettlementRounds.IsBetter` + manuell |
| UX-22 | Notizbuch oben rechts ab dem Hub-Auftakt jederzeit: Begriffe, Landkarte n von 3, Satz zum Erzählen, Flockes Pfützen. Offenes Notizbuch pausiert das Spiel. | manuell |
| UX-23 | Ton: ~~Wind im Prolog (lauter, je mehr Pinguine frieren)~~ (entfallen 18.10.2026), Plopp beim Bauen, Platschen beim Schmelzen, Ding je Stern, ZWILLI-Piepen; Knopf „Ton: an/aus“. Kein Audio-Asset, alles erzeugt. | manuell |
| UX-24 | Transfer: „Kurz nachgedacht“ – Satz und Geschäftsfeld aus je 3 Antworten wählen; falsche Antwort wird erklärt, keine Punkte; die richtige steht nicht immer an derselben Stelle. | auto – `test_content.py` (Daten) + manuell |
| UX-25 | Hub: Theo, Klara, Flocke ansprechbar; Kolonie-Pinguine watscheln; in der eigenen Siedlung leuchten die Türen gemütlicher Iglus, frierende sind bläulich. | manuell |

## 4. Manuelle Prüfung (ca. 20 Minuten)

1. Projekt öffnen; erscheint ein Dialog zum neuen Input System, „Yes“ oder „No“ – beides läuft (TXT-04).
2. **Pinguin-Spiel → Prototyp starten.** Titel ist deutsch („Simulation für alle“).
3. ~~Prolog: Ton an? Wind wird lauter, wenn Pinguine am Rand frieren.~~ (entfallen 18.10.2026) Notizbuch-Knopf ist ab dem Hub da (UX-22).
4. MG1: ZWILLI schwebt türkis neben Frieda und flackert (UX-13). Kalles Szene: Flocke planscht und fragt „warum?“.
5. Schritt 2: Kärtchen legen – die Waage neigt sich und steht am Ende gerade (UX-16).
6. Schritt 4: Theo misst nach (UX-14), danach Was-wäre-wenn (UX-15). Notizbuch öffnen: „Digitaler Zwilling“ steht drin.
7. Schritt 5: Runde 1 bauen → Iglus antippen, Waage prüfen (UX-17); Runde 2 → Vergleich + Klara (UX-18).
   Drei Runden ohne Stern → Knopf „Beispiel-Siedlung“ erscheint, lädt einmal (UX-19). Während Frieda spricht, keine Knöpfe (UX-20).
8. Transfer: zwei Fragen beantworten, einmal absichtlich falsch (UX-24).
9. Hub: Theo, Klara, Flocke ansprechen; eigene Siedlung prüfen (UX-25). „Siedlung nochmal planen“, schlechter bauen → Hub zeigt weiter die bessere (UX-21).

## 5. Bekannte Grenzen / offen

- **Nicht im Editor gespielt** – kompiliert gegen Unity 6, Verhalten zur Laufzeit ungeprüft.
- Offene Fragen an das Team: ~~Prolog-Ziel 45 s vs. 60 s (docs/03)~~ (Prolog entfallen); Igluwärme q = 0,3 vs. „nur Laternen“ (docs/03).
- Leveldaten werden noch nicht validiert (Wind 0 oder k = 0 führen erst nach vielen Iterationen zu einer Meldung).
- `.meta`-Dateien sind nicht im Repository – nach dem ersten Import committen, sonst brechen GUIDs.
- Siemens-Inhalte (auch die Antworten in „Kurz nachgedacht“) bleiben Entwurf bis zur Freigabe.
