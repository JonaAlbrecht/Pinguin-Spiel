# Spec 001 – Vertical Slice „Vom Huddle zur Siedlung“

> Status: **Prototyp v0.1 umgesetzt** (Branch `claude/prototyp-vertical-slice`).
> Grundlage: docs/00–05. Offene Fragen: docs/06. Wo docs/06 noch keine Antwort hat, steht hier die **Prototyp-Annahme**. Die Annahmen sind Vorschläge, keine Entscheidungen – das Team bestätigt oder ändert sie.
> Jede Anforderung hat eine ID und eine Prüfart: **auto** (läuft in CI) oder **manuell** (Editor, Playtest).

---

## 1. Umfang

| Im Prototyp | Nicht im Prototyp (bewusst) |
|---|---|
| Titelbild, Hub-Ausschnitt „Huddle-Platz“ (laufen, Figuren ansprechen) | MG2 Polar-Express, MG3 Fischfabrik (nur Hinweisschild im Hub) |
| ~~Prolog „Der letzte Huddle“ (MG1 Teil A)~~ *(entfallen 18.10.2026 mit dem Prolog, ADR-20)* | Speichern/Fortsetzen (R-01) |
| MG1 Teil B „Die erste Siedlung“ mit Schritten 1–5 | Englisch (K-01), Vorlesefunktion (K-03) |
| „So macht's Siemens“ als **Entwurf** (Schritt 6) | Vorher-/Nachher-Quiz im Spiel (L-02: besser extern, z. B. Forms) |
| Abschluss: Siemens-Landkarte (Karte 1 von 3), Notizbuch-Begriffe | Kosmetik, Pip anpassen (H-03), Musik/Sound (J-04) |
| Optionaler Modus „Blick unter die Haube“ | ink-Dialoge (Texte liegen vorerst als JSON vor, P-01) |
| Text-Lint, Golden-File-Tests, CI | Blender-Assets (Platzhalter aus Unity-Primitiven), URP-Toon-Look |

Spielzeit: ca. 15 Minuten (A-05).

---

## 2. Anforderungen

### Simulation (Kern, `sim-core`)
| ID | Anforderung | Prüfung |
|---|---|---|
| SIM-01 | WHEN die Siedlung simuliert wird, THE SYSTEM SHALL die stationäre Wärmebilanz je Feld wie in docs/03 (MG1 Technik) lösen; Abweichung zur Python-Referenz ≤ 1e-6 relativ. | auto – `SettlementGoldenTests` |
| SIM-02 | WHEN ein Iglu nach dem Lösen ≥ 0 °C hat, THE SYSTEM SHALL es zu Wasser machen und neu lösen, höchstens 5 Durchläufe. | auto – Golden `kalle`, `hint_with_walls`, `random_*` |
| SIM-03 | THE SYSTEM SHALL Zellen im Lee eines Schneewalls (Abstand ≤ 3,5 Felder, Kegel cos ≥ 0,7 zur Windrichtung) mit 0,4 × Wärmeverlust rechnen. | auto – Golden `shielded`, pytest `test_wall_shields_downwind_only` |
| SIM-04 | THE SYSTEM SHALL den Huddle (Teil A) transient mit implizitem Euler rechnen; Abweichung zur Referenz ≤ 1e-6. | auto – `HuddleGoldenTests` *(entfallen 18.10.2026 mit dem Prolog, ADR-20)* |
| SIM-05 | THE SYSTEM SHALL alle Löser schrittweise ausführbar machen (`IStepSolver`), damit das Wärmebild sich sichtbar aufbaut (O-10: echte Iterationen, kein Fake). | auto – `ConjugateGradient_IsSteppable` |
| SIM-06 | THE SYSTEM SHALL Sterne vergeben: ★ ≥ 12 gemütliche Iglus im Budget, ★★ dazu nichts geschmolzen, ★★★ dazu ≤ 80 % Budget. | auto – `Stars_FollowTheRules` |
| SIM-07 | Das Level SHALL lösbar sein: Beispiel-Siedlung erreicht ★★ mit 9 Laternen; ★★★ (8 Laternen) ist erreichbar. | auto – pytest `test_hint_layout_reaches_two_stars`; ★★★ per Suche in der Balancing-Sitzung gefunden (O-06) |
| SIM-08 | Kalles Siedlung SHALL sichtbar schmelzen (≥ 5 Iglus). | auto – pytest `test_kalle_layout_melts` |

### Spiel (Unity)
| ID | Anforderung | Prüfung |
|---|---|---|
| UX-01 | WHEN der Spieler auf den Boden klickt/tippt, THE SYSTEM SHALL Pip dorthin laufen lassen; ein Klick auf eine Figur startet das Gespräch. | manuell |
| UX-02 | THE SYSTEM SHALL anzeigen, wer als Nächstes eine Aufgabe hat („!“ über der Figur). | manuell |
| UX-03 | Im Prolog SHALL der Balken „Alle warm“ nur wachsen, solange kein Pinguin friert, und sonst pausieren (kein Zurücksetzen, kein Scheitern). *(entfallen 18.10.2026 mit dem Prolog, ADR-20)* | manuell |
| UX-04 | WHEN ein Pinguin länger als 8 s friert, THE SYSTEM SHALL einen Tipp von Oma Grete zeigen (max. 3-mal). *(entfallen 18.10.2026 mit dem Prolog, ADR-20)* | manuell |
| UX-05 | MG1 SHALL die Schritte 1–6 in fester Reihenfolge zeigen; die Phasenleiste oben zeigt den aktuellen Schritt. | manuell |
| UX-06 | Iglu-Zustand SHALL nicht nur über Farbe erkennbar sein (frierend: zittert, bläulich; gemütlich: Tür leuchtet; schmilzt: sackt zusammen → Pfütze). Ampeln zeigen zusätzlich „OK / ! / X“. | manuell |
| UX-07 | WHEN der Spieler mehr Laternen/Iglus/Schneewälle setzen will als erlaubt, THE SYSTEM SHALL einen erklärenden Hinweis zeigen. | manuell |
| UX-08 | Nach jeder Runde SHALL wieder der geplante Aufbau dastehen („im Computer schmilzt nichts wirklich“). | manuell |
| UX-09 | Nach Runde 2 ohne Stern SHALL Frieda einen passenden Tipp geben; nach Runde 3 ohne Stern SHALL eine Beispiel-Siedlung geladen werden können. | manuell |
| UX-10 | Standardtexte SHALL keine Formeln, Fachwörter der Sperrliste oder unerklärten Abkürzungen enthalten; Sprechblasen ≤ 140 Zeichen; Transfer-Text ≤ 60 Wörter. | auto – `tools/text_lint.py` |
| UX-11 | WHEN die Transfer-Karte keine vollständige Freigabe hat, THE SYSTEM SHALL sie als „ENTWURF – nicht freigegeben“ kennzeichnen. | manuell |
| UX-12 | Der Prototyp SHALL in der Built-in-Pipeline und in URP laufen, mit altem und neuem Input System. | manuell (Editor); Kompilierung der Legacy-Input-Variante geprüft |

---

## 3. Prototyp-Annahmen zu den offenen Fragen (docs/06)

| ID | Annahme im Prototyp |
|---|---|
| 0-01 | Vertical Slice wie oben |
| 0-02 | Unity 6 (Version beim Öffnen wählen); Lizenzfrage bleibt offen |
| 0-03 | Arbeitsstand liegt auf GitHub-Branch (Wunsch des Teams); Klassifizierung bleibt offen – siehe Hinweis in der README |
| A-06 | Arbeitstitel „Vom Huddle zur Siedlung“ |
| C-01 | Klicken/Tippen zum Laufen |
| C-02 | Platz mit Schild, Friedas Labor, Kalles Baustelle, Laternen, 3 Kolonie-Pinguine, Hinweisschild zu Release 1 |
| C-03 | Figur anklicken → Pip läuft hin → Gespräch |
| C-04 | Ja: die beste geplante Siedlung des Spielers steht danach im Hub |
| C-05 | Nur Titelbild mit „Spiel starten“ |
| D-01…D-05 | 12 Pinguine, einzeln ziehen, Balken pausiert statt Reset, Wind dreht alle 20 s mit 5 s Vorwarnung, kein Scheitern |
| D-06 | Eigene Spielfläche (gleiche Unity-Szene, eigener Bereich) |
| E-01 | 6 Karten (4 richtig); falsche Wahl → lustiger Hinweis |
| E-02 | Kärtchen antippen, dann Waagschale antippen (touch-tauglich) |
| E-03 | 4 Fäden von Hand, dann zeichnet der Computer alle 1 104 Kopplungen |
| E-04 | Jeder Baustein belegt genau 1 Feld |
| E-05 | Leeres Eisfeld |
| E-06 | Nur Laternen kosten Strom (100 je Laterne, Budget 1 000); max. 16 Iglus („16 Familien“), max. 12 Schneewälle |
| E-07 | Werkzeugleiste, Klicken/Ziehen malt, Klick auf gleichen Baustein entfernt, Radierer, Rückgängig |
| E-08 | „Simulieren“ von Hand pro Runde |
| E-09/E-10 | 3 gewertete Runden, danach Beispiel-Siedlung und Extra-Runden |
| E-11 | Geschmolzene Iglus kehren nach der Runde zurück (nur Simulation) |
| E-12 | Windpfeil am Feldrand, Wind aus Nordwest |
| F-01/F-02 | Sterne wie SIM-06, Ziel 12 Iglus; es zählt die beste Runde |
| F-04 | Ampel „Iglus gemütlich“: grün ≥ 12, gelb ≥ 9, sonst rot · „Nichts schmilzt“: grün 0, gelb 1–2, sonst rot · „Strom gespart“: grün ≤ 8, gelb ≤ 10 Laternen |
| G-01 | SIMARIS therm + Simcenter 3D als Vorschlag, deutlich als Entwurf markiert |
| G-02 | Kein freigegebenes Bild: erzeugtes Symbolbild eines Schaltschranks |
| G-05 | Entwurfskarte im Prototyp erlaubt |
| G-07 | Kein Siemens-Logo, keine Siemens-Schrift |
| H-01/H-02 | Notizbuch nur als Liste am Ende; 1 Begriff je Schritt (Modell, Bilanz, Raster, Simulation, Optimieren, Schaltschrank) |
| I-02 | Pip ohne Geschlecht |
| I-03 | Nur Text |
| I-05 | Pip, Frieda, ZWILLI (nur im Text), Kalle, Oma Grete; Kalles Versuch als kurze Szene auf dem Spielfeld |
| O-01…O-05 | Werte in `Resources/Data/Materials/materials_settlement_v1.json` und `Levels/*.json`; Iglus haben kleine Eigenwärme (0,3) |
| O-07/O-08 | Huddle: frei bewegliche Pinguine, Kontakt ≤ 0,85 m, Windschutz durch Nachbarn im Luv ≤ 1,0 m |
| O-09 | Python-Referenz + Golden Files sind dabei |
| P-01…P-03 | Schlanke Architektur: ein `GameRoot` baut alles per Code; eine Szene, per Menü erzeugt |
| P-04 | Eigene Kamera-Steuerung statt Cinemachine |
| P-05 | Farbskala blau → hellgelb/gold → orange/rot (ohne Rot-Grün-Gegensatz), plus Formen/Animationen |
| P-06 | uGUI per Code (kein UI Toolkit, kein TextMeshPro-Import) |
| R-02/R-03 | Keine Telemetrie, kein Leaderboard |

---

## 4. Bekannte Grenzen von v0.1

- **Nicht im Unity-Editor getestet.** Die Spielschicht wurde nur gegen Unity-Referenz-Assemblies kompiliert (Unity 2021 API, UI 2018) – das Verhalten zur Laufzeit ist ungeprüft. Der Simulationskern ist dagegen vollständig getestet.
- Grafik nur aus Unity-Primitiven, kein Toon-Shader, kein Sound.
- Die neue Input-System-Variante ist geschrieben (Compiler-Schalter), aber nicht kompiliert.
- Balancing ist ein erster Wurf und muss im Playtest bestätigt werden.
