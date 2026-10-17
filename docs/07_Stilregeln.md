# Stilregeln: Schattierung, Detailgrad, Proportionen

Alles im Spiel wird per Code aus Grundformen gebaut. Damit es aus einem Guss aussieht, liest jedes Asset seine
Gestaltung aus **einer** Datei: `unity/PinguinKolonie/Assets/_Project/Scripts/Core/ArtStyle.cs`.
Farben stehen in `Palette.cs`. Wer dort einen Wert ändert, ändert alle Figuren und Bauten zugleich.

## Ablauf bei einer Stiländerung

1. Wert in `ArtStyle.cs` (oder `Palette.cs`) ändern.
2. In Unity **Pinguin-Spiel → Stilprobe rendern**. Ohne offenen Editor geht das auch im Batchmode:
   `Unity -batchmode -quit -projectPath unity/PinguinKolonie -executeMethod SFE.Game.EditorTools.StylePreview.Render`
3. Bilder in `unity/PinguinKolonie/Stilprobe/` ansehen (nicht im Repository):

   | Bild | Zeigt |
   |---|---|
   | `figuren.png` | alle Figuren nebeneinander, inkl. ZWILLI |
   | `pinguin.png` | ein Pinguin von vorn, schräg, von der Seite und von hinten |
   | `siedlung.png` | Iglu, Laterne, Schneewall, Pfütze neben Figuren – stimmen die Größen zueinander? |
   | `spielansicht.png` | dieselbe Szene so groß wie im Hub (50°, 24 m) – was hier verschwindet, lohnt kein Detail |

## 1. Schattierung (`ArtStyle.Shading`)

| Regel | Wert | Warum |
|---|---|---|
| **Facetten statt weicher Rundung** | `Flat = true` | Jede Fläche ein Farbton. Kugeln, Würfel und Figuren sprechen dieselbe Formensprache; wenige Segmente wirken gewollt. |
| **Matt** | `Smoothness = 0.12` | Schnee und Federn glänzen nicht. Unitys Vorgabe 0,5 sah nach Plastik aus. |
| **Eine Sonne, kühles Umgebungslicht** | `SunColor`, `SunAngles`, `Ambient` | Warmes Licht von vorn oben (aus Kamerasicht), kühle Schattenseite. Schwarz säuft nicht ab. |
| **Weiche Schatten** | `ShadowStrength = 0.65` | Freundlicher Diorama-Look statt harter Kontraste. |
| **Kein Metall, keine Texturen** | – | Farbe kommt nur aus der Palette; Formen tragen das Bild. |

## 2. Detailgrad (`ArtStyle.Detail`)

Regel: **Facetten sollen auf dem Bildschirm etwa gleich groß wirken.** Große Formen bekommen mehr Segmente,
kleine weniger.

| Stufe | Wert | Wofür |
|---|---|---|
| `FigureSegments` / `FigureRings` | 16 / 14 | Körper der Figuren und Kleidung, die eng anliegt |
| `Segments` | 12 | Bauten und Gegenstände (Iglu, Laterne, Pfütze, Ring, Flossen) |
| `SmallSegments` | 8 | Kleinteile unter einer Handbreit (Augen, Schnabel, Knöpfe, Schopf) |

- `Segments = 0` schaltet zurück auf Unitys glatte Standardformen (zum Vergleich).
- Ein Detail muss in `spielansicht.png` noch erkennbar sein, sonst lieber weglassen oder vergrößern.
  Figuren unterscheiden sich vor allem über **Silhouette und eine Signalfarbe** (Helm, Schal, Kappe), nicht über Kleinkram.
- Neue runde Formen immer über `Prims.Make` oder `Shapes` bauen, nie eigene Segmentzahlen in den Code schreiben.
  Die Stufe wählt man mit `segments: ArtStyle.Detail.SmallSegments` usw.

## 3. Proportionen (`ArtStyle.Proportion`)

- **Der Pinguin ist das Maß der Welt.** `PenguinHeight` (0,76 m bei Größe 1) ist die Bezugsgröße; Türen, Laternen
  und Iglus richten sich danach. *Offen:* docs/04 nennt 0,6 m; die bestehenden Szenen sind auf 0,76 m eingerichtet.
- **Alle Figurenmaße sind Anteile der Körperhöhe** (`PenguinShape`). Größe ändern → alles skaliert mit.
- **Silhouette eines Pinguins:** unten breit, oben schmal, *kein abgesetzter Hals*. Der Umriss steht als Stützpunkte
  in `Profile` (Höhe, Radius, Versatz nach vorn) und wird weich interpoliert.
- **Erkennungszeichen** (alle im Bauplan): heller Bauch bis zur Kehle, dunkler Kopf, spitzer Schnabel,
  Flossen am Körper, Füße mit drei Zehen, gelb-orange Ohrflecken am Hals (Kaiserpinguin).
- **Küken** (`Chick`): rund, großer Kopf, kurze Flossen, graue Daunen, schwarze Kappe mit weißer Maske.
- **Zubehör sitzt auf dem Körper, nicht an festen Koordinaten.** Kleidung ist ein vergrößertes Stück Körperoberfläche
  (`Shell` in `PenguinFigure.cs`), Augen und Gegenstände werden über Höhe + Winkel auf dem Umriss platziert.
  Ändert sich der Umriss, wandert alles mit.

## Für Blender-Assets später (docs/04 Abschnitt 6)

Dieselben Regeln gelten für importierte Modelle: *Shade Flat*, Segmentzahlen nach der Tabelle oben, Maße relativ
zur Pinguinhöhe, Farben nur aus der Palette (Palette-Atlas), kein Glanz.
