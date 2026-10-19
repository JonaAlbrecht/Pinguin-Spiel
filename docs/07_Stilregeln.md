# Stilregeln: Farben, Schattierung, Detailgrad, Proportionen

Alles im Spiel wird per Code aus Grundformen gebaut. Damit es aus einem Guss aussieht, liest jedes Asset seine
Gestaltung aus **zwei** Dateien unter `unity/PinguinKolonie/Assets/_Project/Scripts/Core/`:

- `Palette.cs` – alle Farben, abgeleitet aus dem Siemens-Farbschema
- `ArtStyle.cs` – Schattierung, Detailgrad, Proportionen

Wer dort einen Wert ändert, ändert alle Figuren, Bauten und Fenster zugleich. `python tools/style_lint.py` prüft,
dass keine Farbe an der Palette vorbeigeht.

## Ablauf bei einer Stiländerung

1. Wert in `Palette.cs` oder `ArtStyle.cs` ändern.
2. In Unity **Pinguin-Spiel → Stilprobe rendern** wählen. Ohne offenen Editor geht das auch im Batchmode:
   `Unity -batchmode -quit -projectPath unity/PinguinKolonie -executeMethod SFE.Game.EditorTools.StylePreview.Render`
3. Die Bilder in `unity/PinguinKolonie/Stilprobe/` ansehen (sie liegen nicht im Repository):

   | Bild | Zeigt |
   |---|---|
   | `figuren.png` | alle Figuren nebeneinander, inkl. ZWILLI |
   | `pinguin.png` | einen Pinguin von vorn, schräg, von der Seite und von hinten |
   | `siedlung.png` | Scholle im Meer mit Iglu, Laterne, Schneewall, Pfütze und Figuren – stimmen Größen und Farben zueinander? |
   | `spielansicht.png` | dieselbe Szene so groß wie im Hub (50°, 24 m) – was hier verschwindet, lohnt kein Detail |
   | `oberflaeche.png` | Titel, Knöpfe, Fenster, Sprechernamen, Ampeln, Wärmeskala, Siemens-Primärfarben – beschriftet mit den Namen in `Palette.cs` |

## 1. Farben (`Palette.cs`)

Quelle: Siemens-Farbschema *sie-colors-overview* V1.4.4 (Stand 2025-02-17), Bildschirmwerte. Die Datei liegt
im Hauptordner des Repos. Alle Werte stehen exakt in `Palette.Siemens`.

| Regel | Warum |
|---|---|
| **Wo es geht Primärfarben**: Petrol, Light Petrol, Bold/Soft Green, Bold/Soft Blue, Deep Blue, Light Sand | Das Spiel soll als Siemens-Produkt erkennbar sein. |
| **Sonst Sekundär- und Zusatzfarben** (Sand-, Gelb-, Rot-, Orange-, Lila-Töne, Deep-Blue-Abstufungen), **nie eine Farbe außerhalb des Schemas** | Ein Pinguinschnabel braucht Orange, Warnungen brauchen Rot. |
| **Aufhellen nur mit Light Sand** (`Palette.Tint`), **Abdunkeln nur mit Deep Blue** (`Palette.Shade`) | Große Flächen wie Himmel und Eis würden in voller Sättigung erschlagen. So bleiben auch Mischfarben in der Siemens-Familie. *Offen: mit Brand-Verantwortlichen abstimmen.* |
| **Jede Spielfarbe ist ein Name in `Palette.cs`** (`Ice`, `Button`, `KalleHelmet` …), der auf eine Siemens-Farbe zeigt | Eine Änderung an einer Stelle wirkt überall; der Stil-Lint verbietet `new Color(…)` im übrigen Code. |
| **Schrift auf hellem Grund nur in dunklen oder kräftigen Tönen** | Gelb oder Bold Green auf Light Sand ist nicht lesbar. Darum haben die Sprecher eigene Farben: Grete Dark Purple, Frieda Blue, Pip Red, Kalle Dark Orange, ZWILLI Petrol, Theo Dark Blue, Klara Dark Green, Flocke Deep Blue 60 %. |

Wichtige Zuordnungen:

| Bereich | Siemens-Farbe |
|---|---|
| Hauptknopf, Labordach, ZWILLIs Name | Petrol |
| Titelbild, dunkle Fenster, Schrift | Deep Blue |
| helle Fenster, Bauch, Schnee, Iglu | Light Sand |
| Pinguin-Rücken | Deep Blue 85 % (reines Deep Blue zeigt keine Form mehr) |
| Eis, Himmel | Soft Blue bzw. Blue, mit Light Sand aufgehellt |
| Meer, kalt im Wärmebild | Dark Blue |
| ZWILLI (digitaler Zwilling) | Bold Blue + Bold Green – die Verlaufsfarben *Bold Dynamic Petrol* |
| Wärmeskala | Dark Blue → Soft Blue · Soft Yellow → Dark Yellow · Dark Orange → Red (abgedunkelt) |
| Ampeln | Green / Dark Yellow / Red |

## 2. Schattierung, Oberflächen, Licht (`ArtStyle.Shading`, `Surfaces`, `Atmosphere`)

Seit 19.10.2026 **realistisch** (ADR-27, Wunsch von Jona: „ultra realistisch“). Render-Pipeline ist URP
(`Assets/_Project/Settings/URP`, angelegt über „Pinguin-Spiel → Render-Pipeline einrichten (URP)“).
Vorher: matte Facetten im Diorama-Look (ADR-16) – zurück mit `Flat = true` und alten Detailwerten.

| Regel | Wert | Warum |
|---|---|---|
| **Weiche Rundungen** | `Flat = false` | Realistische Formen; Segmentzahlen (Abschnitt 3) hoch genug, dass man keine Kanten sieht. |
| **Physikalische Oberflächen** | `Surfaces`: Schnee, Eis, Wasser, Gefieder, Metall, Glanz, Licht | Glanz, Metall, Struktur (prozedurale Normal-Map aus `SurfaceTextures`) und Leuchten je Oberfläche. Eis, Meer, Wasser und Laternenlicht erkennt `Prims` an der Farbe; Schnee, Metall und Augen setzt der Bau mit `Prims.SetSurface`. |
| **Gemalte Farbgrenzen** | Pinguinkörper als Farbbild (`PenguinFigure.TorsoTexture`) | Bauch, Kopf und Maske mit weicher Kante statt Stufen aus Dreiecken („Verpixelung“). |
| **Tiefe Polarsonne** | `SunIntensity = 2.1`, `SunAngles` 32° | Lange Schatten, Streiflicht auf Schnee; Verhältnisse wie draußen (linearer Farbraum, HDR). |
| **Himmelslicht dreifarbig** | `Palette.AmbientSky/Horizon/Ground` | Bläulich von oben, hell vom Schnee zurückgeworfen – ab dem ersten Bild, ohne Hintergrundrechnung. |
| **Himmel und Spiegelungen** | prozeduraler Himmel (`Resources/Materials/Sky.mat`), Cubemap per `RenderToCubemap` | Wasser, Eis und Augen spiegeln den Himmel. |
| **Nachbearbeitung** | `Atmosphere`: ACES-Tonemapping, Bloom, Farbkorrektur, Vignette, Dunst, SSAO | Filmischer Look; Laternen leuchten über die Bloom-Schwelle, Umgebungsverdeckung dunkelt Fugen und Kontaktflächen ab. |
| **Kantenglättung** | 4× MSAA + SMAA (hoch), 2× + 0,8–0,9 Auflösung (sparsam) | `RenderQuality` ändert eine Laufzeitkopie des Pipeline-Assets. |

## 3. Detailgrad (`ArtStyle.Detail`)

Regel: **Rundungen sollen auf dem Bildschirm glatt wirken.**

| Für | Wert | Wie |
|---|---|---|
| Bauten und Gegenstände (`Prims.Make`) | `FacetLength = 0.08` m | Die Segmentzahl ergibt sich aus dem Umfang in der Welt (`SegmentsFor`), zwischen `MinSegments` 20 und `MaxSegments` 128. |
| Figurenkörper und eng anliegende Kleidung | `FigureSegments` / `FigureRings` = 48 / 40 | feste Stufe, damit Kleidung und Körper zusammenpassen |
| mittlere Teile der Figuren (Flossen, Brille) | `Segments` = 32 | |
| Kleinteile unter einer Handbreit (Augen, Schnabel, Schopf) | `SmallSegments` = 20 | |

- `FacetLength = 0` schaltet die Bauten zurück auf Unitys glatte Standardformen (zum Vergleich).
- Ein Detail muss in `spielansicht.png` noch erkennbar sein, sonst lieber weglassen oder vergrößern.
  Figuren unterscheiden sich vor allem über **Silhouette und eine Signalfarbe** (Helm, Schal, Kappe), nicht über Kleinkram.
- Neue runde Formen immer über `Prims.Make` oder `Shapes` bauen; keine eigenen Segmentzahlen oder Glanzwerte in den Code schreiben.

## 4. Proportionen (`ArtStyle.Proportion`)

- **Der Pinguin ist das Maß der Welt.** `PenguinHeight` (0,76 m bei Größe 1) ist die Bezugsgröße; Türen, Laternen
  und Iglus richten sich danach. *Offen:* docs/04 nennt 0,6 m; die bestehenden Szenen sind auf 0,76 m eingerichtet.
- **Alle Figurenmaße sind Anteile der Körperhöhe** (`PenguinShape`). Größe ändern → alles skaliert mit.
- **Silhouette eines Pinguins:** unten breit, oben schmal, *kein abgesetzter Hals*. Der Umriss steht als Stützpunkte
  in `Profile` (Höhe, Radius, Versatz nach vorn) und wird weich interpoliert.
- **Erkennungszeichen** (alle im Bauplan): heller Bauch bis zur Kehle, dunkler Kopf, spitzer Schnabel,
  Flossen am Körper, Füße mit drei Zehen, gelbe Ohrflecken am Hals (Kaiserpinguin).
- **Küken** (`Chick`): rund, großer Kopf, kurze Flossen, graue Daunen, dunkle Kappe mit heller Maske.
- **Zubehör sitzt auf dem Körper, nicht an festen Koordinaten.** Kleidung ist ein vergrößertes Stück Körperoberfläche
  (`Shell` in `PenguinFigure.cs`). Augen und Gegenstände werden über Höhe und Winkel auf dem Umriss platziert.
  Ändert sich der Umriss, wandert alles mit.

## Für Blender-Assets später (docs/04 Abschnitt 6)

Dieselben Regeln gelten für importierte Modelle: *Shade Smooth*, Oberflächen nach `ArtStyle.Surfaces`, Maße relativ
zur Pinguinhöhe, Farben nur aus `Palette.Siemens` (Palette-Atlas).
