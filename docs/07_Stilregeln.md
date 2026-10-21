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
   (nur die Hub-Bilder: `…StylePreview.RenderHub`; das Log nennt Objekte und Dreiecke der Landschaft)
3. Die Bilder in `unity/PinguinKolonie/Stilprobe/` ansehen (sie liegen nicht im Repository):

   | Bild | Zeigt |
   |---|---|
   | `figuren.png` | alle Figuren nebeneinander, inkl. ZWILLI |
   | `pinguin.png` | einen Pinguin von vorn, schräg, von der Seite und von hinten |
   | `siedlung.png` | Scholle im Meer mit Iglu, Laterne, Schneewall, Pfütze und Figuren – stimmen Größen und Farben zueinander? |
   | `spielansicht.png` | dieselbe Szene so groß wie im Hub (50°, 24 m) – was hier verschwindet, lohnt kein Detail |
   | `siedlung_sparsam.png` | die Siedlung in der Stufe „Sparsam“ – muss gleich aussehen, nur ohne Bloom, Umgebungsverdeckung und Laternen-Schatten |
   | `kalle_siedlung.png` | Kalles Versuch draußen im Hub: dasselbe Raster, dieselben Bausteine und dieselbe Kamera wie in `HubWorld.PlayKalleAttempt` – stehen Laternen und Iglus einzeln? |
   | `hub.png` | der Hub aus der Spielkamera (50°, 24 m) über dem Huddle-Platz – Figuren, Labor, Gleis wie im Spiel (`HubWorld.BuildScenery`) |
   | `hub_rand.png` | Pip am Nordostrand der begehbaren Fläche, Blick Richtung Gleis und Bucht – darf keine Kante ins Leere zeigen |
   | `hub_west.png` | Pip am Westrand vor Kalles Baustelle – Übergang vom Eis in die verschneite Ebene |
   | `hub_weit.png` | von der Bucht aus auf Hub, Eiskante, Treibeis und die gestaffelten Berge |
   | `hub_bucht.png` | aus Augenhöhe über den Hub auf die Bucht und das andere Ufer – trifft der Himmel überall auf Berge? |
   | `laterne.png` | Laterne und Iglu nah von der Seite |
   | `laterne_brett.png` | dieselben Bausteine so wie auf dem Spielbrett in der Rechentafel (50°, 6,5 m, Feldabstand 1 m) – Laterne von oben erkennbar? |
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
| **Weiche Rundungen** | `Flat = false` | Realistische Formen; Segmentzahlen (Abschnitt 4) hoch genug, dass man keine Kanten sieht. |
| **Physikalische Oberflächen** | `Surfaces`: Schnee, Eis, Wasser, Gefieder, Metall, Glanz, Licht, Gelände | Glanz, Metall, Struktur (prozedurale Normal-Map aus `SurfaceTextures`) und Leuchten je Oberfläche. Eis, Meer, Wasser und Laternenlicht erkennt `Prims` an der Farbe; Schnee, Metall und Augen setzt der Bau mit `Prims.SetSurface`. |
| **Gemalte Farbgrenzen** | Pinguinkörper als Farbbild (`PenguinFigure.TorsoTexture`) | Bauch, Kopf und Maske mit weicher Kante statt Stufen aus Dreiecken („Verpixelung“). |
| **Tiefe Polarsonne** | `SunIntensity = 2.1`, `SunAngles` 32°, `ShadowStrength = 1` | Lange Schatten, Streiflicht auf Schnee; Verhältnisse wie draußen (linearer Farbraum, HDR). Schatten hellt nur das Himmelslicht auf. |
| **Himmelslicht dreifarbig** | `Palette.AmbientSky/Horizon/Ground` | Bläulich von oben, hell vom Schnee zurückgeworfen – ab dem ersten Bild, ohne Hintergrundrechnung. |
| **Himmel und Spiegelungen** | prozeduraler Himmel (`Resources/Materials/Sky.mat`), Cubemap per `RenderToCubemap` | Wasser, Eis und Augen spiegeln den Himmel. |
| **Nachbearbeitung** | `Atmosphere`: ACES-Tonemapping, Bloom, Farbkorrektur, Vignette, SSAO | Filmischer Look; Umgebungsverdeckung dunkelt Fugen und Kontaktflächen ab. |
| **Belichtung** | `PostExposure = -0.35` | Sonnenschnee (≈ 1,5 im HDR-Puffer) behält Zeichnung, statt zu weißen Scheiben auszufressen (ADR-28). |
| **Nur Lichtquellen blühen** | `BloomThreshold = 2.6`, Laternenglas `Emission = 10` | Schwelle über Sonnenschnee, unter Laternenglas. Bei 1,05 blühte jede besonnte Schneefläche (ADR-28). |
| **Dunst nach Kameraabstand** | `FogAtFocus = 0.15` (Dichte = Wert / Abstand zum Blickpunkt) | Am Blickpunkt immer ≈ 2 % Dunst, am Horizont deutlich mehr – auch über dem Spielbrett aus 70 m kein Schleier (ADR-28). |
| **Bewegtes Wasser** | `Surfaces.Water.Flow = 0.35` m/s, zweite Wellenlage `DetailTiling = 2.3` | Zwei gegeneinander laufende Wellenmuster; `RenderLook.Tick` verschiebt sie jedes Bild. |
| **Laternenlicht am Tag** | `Shading.LanternIntensity = 0.6` (× Größe²), `LanternRange = 2.2` m (× Größe) | Nur ein warmer Schein. Bei 2,2 war der Schnee unter der Laterne heller als in der Sonne, Iglus daneben fraßen gelb-weiß aus (ADR-29). |
| **Kein Licht-Symbol im Editor** | `Editor/LightIcons.cs` blendet Unitys Glühbirnen-Gizmo für Lichtquellen aus | Mit „Gizmos“ in der Game-Ansicht verdeckte es jede Laterne. Im fertigen Spiel gab es das Symbol nie (ADR-29). |
| **Laternen-Schatten** | `Shading.LanternShadows = 2` | Nur die zwei Laternen am Blickpunkt werfen Schatten (je sechs Schattenbilder pro Bild); „Sparsam“: keine. |
| **Kantenglättung** | 4× MSAA + SMAA (hoch), 2× MSAA + FXAA + 0,8–0,9 Auflösung (sparsam) | `RenderQuality` ändert eine Laufzeitkopie des Pipeline-Assets. Die Nachbearbeitung bleibt in beiden Stufen an – ohne Tonemapping frisst Schnee aus. |
| **Neue Shader-Schalter** | Varianten-Materialien in `Resources/Materials/Varianten` | Wer in `Surfaces.ApplyTo` einen neuen Schalter (Keyword) setzt, führt danach „Render-Pipeline einrichten (URP)“ aus – sonst fehlt die Variante im Build (ADR-28). |

## 3. Landschaft um den Hub (`ArtStyle.Landscape`, `ArcticLandscape`)

Seit 21.10.2026 (ADR-31, Spieltest: „Rand und Hintergrund müssen besser aussehen“). Vorher: runde Scholle in einem
flachen Meer-Zylinder – aus der Spielkamera sah man die harte Kreiskante und dahinter nur Wasser.

| Regel | Wert | Warum |
|---|---|---|
| **Keine Kante am Weg** | Eis eben bis `FlatRadius` 15 m, Pip läuft bis 13 m (`HubWorld.FloeRadius − 1`) | Das Eis geht über den Bildrand hinaus weiter; die Grenze von Pips Weg sieht man nicht. |
| **Schnee steigt aus dem Eis** | Rand bei `SnowEdge` 19,5 m ± `SnowEdgeJag`, weicher Wulst `SnowBank` | Ein unregelmäßiger Rand statt eines zweiten Kreises; der Schnee liegt im Gelände knapp unter dem Eis und steigt heraus. |
| **Wind formt den Schnee** | Dünen quer zum Nordwestwind (`DuneSpacing` 6 m), Windgangeln längs | Unter der Südostsonne fangen die Kämme Licht und Schatten; nur längs liegende Verwehungen wirkten flach. |
| **Meer auf einer Seite** | Bucht in Richtung Gleis (`SeaAzimuth` 32°), Eiskante bei `CoastRadius` 22 m, Treibeis, Presseisrücken | Das Schild sagt „an die Küste“; das Gleis endet mit Prellbock vor der Eiskante (`HubWorld.TrackEnd`). |
| **Gestaffelte Berge** | Vorberge 36 m / 7 m hoch, Kette 70 m / 22 m, Hauptkamm 108 m / 48 m | Vorne runde Kuppen, hinten scharfe Grate; der Dunst hellt die hinteren Ketten auf. |
| **Fels nach Hangneigung** | ab `RockSlope` 38° ± `RockBlend`, erst ab den Felsbuckeln (`KnollRadius` 23 m) | Gemaltes Farbbild (Oberfläche `Terrain`) statt Dreiecksstufen: Fels geht weich in Schnee über. Farbe `Palette.Rock`. |
| **Bucht geschlossen** | Inseln am anderen Ufer (`FarShoreRadius` 112 m), mindestens `ShoreMin` über Wasser | Der Himmel stößt nie an das Ende des Netzes. Eine Kamera höher als das andere Ufer sähe den Rand. |
| **Grenze für die Minigames** | nichts weiter als `OuterRadius` 130 m | MG1/MG2 bauen bei x = 200 (Tundra ab x = 140), MG3 bei x = 400. Der Hub ist während der Minigames ohnehin ausgeschaltet. |
| **Leistung** | 6 Objekte, 7 Teilmeshes, ≈ 48 500 Dreiecke; nur Nahbereich (bis `NearRadius` 44 m) und Presseis werfen Schatten | Wenige große Meshes im Cache von `Shapes`; das Gleis wird mit `Prims.Merge` zu drei Objekten. |

- Die Spielkamera (50°, Sichtfeld 30°) sieht nie den Horizont: Sie reicht etwa 11 m über Pip hinaus. Berge und Bucht
  zeigen nur die Stilprobe und flachere Kameras; im Spiel sieht man Eis, Schneerand, Dünen, Eiskante und Treibeis.
- Neue Werte nur in `ArtStyle.Landscape`; `ArcticLandscape.Build` baut für Spiel und Stilprobe dasselbe.

## 4. Detailgrad (`ArtStyle.Detail`)

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
- **Helles auf Hellem braucht Form, nicht Farbe.** Iglu (Light Sand) auf Eis: Fugen zwischen den Schneeblöcken in
  `Palette.IglooJoint` und ein dunkler Eingang nach Süden (zur Kamera, weg vom Nordwestwind) – die Kuppelfarbe bleibt
  frei für die Zustände in MG1 (kalt, gemütlich, schmilzt). Laterne: gusseiserner Mast und Dach als Drehprofil
  (`Shapes.Revolve`), leuchtender Glaskasten, das Dach kaum breiter als das Glas, damit man das Licht auch von oben sieht.

## 5. Proportionen (`ArtStyle.Proportion`)

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
