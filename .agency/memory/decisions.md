# Entscheidungen (ADR)

Format: Entscheidung · Grund · verworfene Alternative.

## 2026-10-16 – v0.2

**ADR-01 Was-wäre-wenn als Level-Überschreibung, nicht als neues Modell.**
„Ohne Wind“ = dasselbe Modell mit Wind 2 statt 8 (`whatIf.noWind.overrides`). Grund: keine neue Physik, also nichts,
was zwischen Python und C# auseinanderlaufen kann; die Golden Files decken es ab.
Verworfen: eigener „Modell ohne Wind“-Schalter im Kern (h ohne Windfaktor) – mehr Code, gleiche Aussage.
Wind 0 wäre singulär (keine Wärmesenke), darum „stille Luft“ statt „gar kein Wind“.

**ADR-02 Eigene Siedlung für Was-wäre-wenn.** 4 Iglus, 2 Laternen: mit Wind alle zu kalt, ohne Wind alle gemütlich.
Grund: Die Beispiel-Siedlung ist die Lösung von Schritt 5 und würde sie vorwegnehmen.
Verworfen: Kalles Siedlung (schmilzt mit und ohne Wind gleich – keine Aussage).

**ADR-03 Waage aus der gelösten Temperatur, im Kern.** `SettlementBalance` liest dieselben Leitwerte wie `Assemble`.
Grund: Gesetz 7 – das Spiel zeigt, was das Modell rechnet. Verworfen: Waage in der Unity-Schicht nachrechnen
(zweite Wahrheit, ungetestet).

**ADR-04 Texte in `ui_de.json`, Lint prüft auch C#.** Grund: ~50 Spieltexte standen im Code und entgingen den
Sprachregeln. Verworfen: Lint auf alle C#-Literale (zu viele Fehlalarme bei Objektnamen und Debug-Texten) – geprüft
werden nur Toast/Label/Button/SetButtonLabel/`.text =`.

**ADR-05 Ton per `AudioClip.Create`.** Grund: keine Lizenzfrage, kein LFS, läuft in WebGL. Verworfen: Audiodateien
(Freigabe, Größe) – später für Musik möglich.

**ADR-06 Farben auf 1/32 runden statt `MaterialPropertyBlock`.** Grund: eine Zeile, behebt das Leck, funktioniert in
Built-in und URP gleich. Verworfen: PropertyBlocks (je Pipeline anderer Farbname `_Color`/`_BaseColor`).

**ADR-07 Input-System-Paket im Manifest.** Grund: Ohne Paket scheitert die Kompilierung, falls ein Projekt auf
„nur neues Input System“ steht. Der Code unterstützt beide Wege; beide sind kompiliert (TXT-04).

**ADR-08 Kalle bleibt Rivale.** Sein Wendepunkt gehört laut docs/02 in Akt 3; in MG1 lernt er nur ein Stück
(„vielleicht rechne ich vorher“).

**ADR-09 Rundenregeln im Kern, ohne Python-Referenz.** Beste Runde, Rundenvergleich, Klaras Kommentar, Skala der
Waage und „Rückgängig nur bei Änderung“ liegen in `SettlementRounds` (sim-core) mit NUnit-Tests. Grund: in der
Unity-Schicht waren sie ungetestet. Keine Python-Referenz, weil es Spielregeln sind, keine Physik (Gesetz 2 gilt für
Rechenregeln des Modells).

**ADR-10 Waage: gemeinsame Skala, keine Wärme-Behauptung.** Eingeschwungen ist rein = raus für jedes Feld; der
Durchfluss sagt nicht, wie warm ein Iglu ist (kalte Iglus zwischen Laternen leiten mehr weiter). Die Waage zeigt darum
Herkunft und Verbleib der Wärme auf einer gemeinsamen Skala; der Zustand kommt aus der Temperatur.
Verworfen: volle Breite je Iglu (alle Waagen sahen gleich aus) und „lange Balken = warm“ (falsch, Test belegt).

## 2026-10-17 – nach dem ersten Spieltest

**ADR-11 Tastatur zusätzlich zum Klicken.** WASD/Pfeiltasten im Hub, kamerabezogen; Klicken bleibt (Tablets, C-01).
E/Leertaste/Enter sprechen an und blättern im Dialog. Das EventSystem sendet keine Navigation mehr – sonst drückte
Leertaste/Enter den zuletzt geklickten Knopf erneut. Verworfen: Achse „Horizontal/Vertical“ des Input Managers
(geglättet, Pip rutscht nach; im neuen Input System nicht vorhanden).

**ADR-12 Rechenregel als Bild-Gleichung, Formel unter der Haube.** Entscheidung von Jona: docs/00 („keine Formeln im
Standard-Spiel“) bleibt. Die Zahlen kommen aus `HuddleModel.LastStepTerms` – implizites Euler, die Teile mit den neuen
Werten, darum geht die Zerlegung exakt auf und der Austausch summiert exakt zu null. Längerer Zeitschritt (5 statt 1),
damit die Zahlen lesbar sind. Verworfen: eigene, explizite Lehr-Rechnung (zweite Wahrheit neben dem Huddle-Modell).
Ebenfalls Jonas Wahl: nur der Zwei-Pinguine-Schritt vor dem Huddle, nicht der ganze Prolog nach fünf Phasen.

**ADR-13 Batterie-Transfer nach dem Prolog, Landkarte mit 4 Karten.** *(Ersetzt durch ADR-20.)* Dieselbe Regel wie im Huddle steckt in der
Batterie-Thermik (Simcenter, Vorschlag). docs/00 hatte drei Karten (je Minigame); der Prolog bekommt eine eigene.
Verworfen: Schaltschrank zweimal zeigen (Wiederholung).

**ADR-14 Schlanke Paketliste, Prüfung gegen den Editor.** Unity schrieb beim ersten Öffnen rund 20 Standardpakete
(Werbung, In-App-Käufe, Xbox-GDK, Analytics …) in die Liste und legte deren Dateien in `Assets/` an. Das Spiel braucht
keines davon; sie sind wieder entfernt. `com.unity.modules.textrendering` gibt es in Unity 6.4 nicht mehr als Paket –
die Prüfung in `check.ps1` hätte das vor dem Öffnen gemeldet.

**ADR-15 Bildqualität in der eingebauten Pipeline, URP später.** *(Umstieg erfolgt mit ADR-27.)* Ursachen der Pixeligkeit (Spieltest 17.10.): MSAA nur 2×
(WebGL 0×), 8-Texel-Felder mit 1-Texel-Gitterlinien stark vergrößert, nur 2–4 Pixel-Lichter bei bis zu 13 Laternen,
TextMesh mit fontSize 64, 96-px-Symbolbilder. Behoben per Code (`RenderQuality`, Felder als 1 Texel ohne Glättung +
Gitterlinien als 3D-Streifen, Schrift in 160 gerastert, Bilder 256–288 px). Verworfen für jetzt: Umstieg auf URP
(Bloom für Laternen, bessere Kantenglättung, Nachbearbeitung) – lohnt sich, braucht aber einen Spieltest im Editor,
weil Materialien dabei leicht rosa werden. Vorschlag für den nächsten Schritt.

## 2026-10-17 – Stilregeln und neue Pinguine

**ADR-16 Ein Stil aus Code statt fremder Assets.** *(Schattierung und Detailgrad ersetzt durch ADR-27; „aus Code statt fremder Assets“ gilt weiter.)* Die Pinguine waren Kugeln und Würfel („Schneemann“). Statt freier
Modelle (OpenGameArt CC0, Poly Pizza CC-BY, Asset Store) baut `PenguinFigure` sie aus einem Drehprofil – Entscheidung
von Jona, damit alles aus einem Guss bleibt. Schattierung, Detailgrad und Proportionen stehen zentral in `ArtStyle.cs`
(docs/07); alle runden Grundformen laufen über `Shapes` und werden facettiert, Materialien matt. Prüfen per
„Stilprobe rendern“ (vier Bilder). Verworfen: fremde Modelle (Stil, Lizenz, Blender-Umweg, Einfärbung über
„Belly“/„Ring“ hätte neu gebaut werden müssen) und weiche Rundungen (wenige Segmente wirkten dann grob statt gewollt).

**ADR-17 Siemens-Farbschema als einzige Farbquelle.** Alle Farben kommen aus `Palette.Siemens` (sie-colors-overview V1.4.4, exakte Bildschirmwerte); Spielfarben sind Namen, die darauf zeigen. Wo es geht Primärfarben, sonst Sekundär-/Zusatzfarben; helle Flächen per Aufhellung mit Light Sand, Abdunkeln nur mit Deep Blue (mit Brand abzustimmen). `tools/style_lint.py` verbietet Farben außerhalb der Palette und prüft die Sprecherfarben. Dazu Farbtreue im Licht (Sonne 0,85, Umgebung ≈ 0,5 statt 1,1/0,7 – sonst überstrahlte Dark Blue zu Knallblau) und Segmentzahl der Bauten nach Größe. Verworfen: nur die Primärfarben (kein Orange für Schnäbel, kein Rot für Warnungen) und Farben frei wählen und „ähnlich“ halten (driftet, ist nicht prüfbar).

## 2026-10-17 – Formeln, Einstellungen

**ADR-18 Formeln wie LaTeX, „Blick unter die Haube“ als eigenes Fenster.** Wunsch von Jona (17.10.): Der Knopf öffnet
ein Pop-up (`Hud.OpenPopup`, hält das Spiel an wie das Notizbuch) statt Text auf der Rechentafel. Die eingebaute
Schrift kann keine Brüche und Indizes, darum stehen die Formeln als LaTeX-Quelle in `ui_de.json` (`underTheHood.formulas`)
und `tools/render_formulas.py` setzt sie mit matplotlib-Mathtext (Computer Modern) zu Deckkraft-Bildern
(`Resources/Formulas/*.bytes`, roh deflate). Unity lädt sie als TextAsset (`UIKit.Formula`) und färbt sie ein.
Verworfen: PNG-Import (Unity skaliert NPOT-Bilder und komprimiert sie unscharf, Importeinstellungen bräuchten
handgeschriebene .meta), `Texture2D.LoadImage` (braucht das Paket imageconversion, Paketliste nur bei geschlossenem
Unity ändern), TextMeshPro/eigener Formelsatz (viel Aufwand für wenig Formeln).

**ADR-19 Einstellungen-Menü, Bedienung während Dialogen gesperrt.** Spieltest 17.10.: Während Grete sprach, sah
„Nächster Zeitschritt“ klickbar aus, weiter ging es aber nur in der Sprechblase. Jetzt graut `Hud.LockWhileBlocking`
Knöpfe und Leisten aus, solange ein Dialog oder Fenster offen ist, und „Weiter »“ ist ein farbiger Knopf.
Der Ton-Knopf wurde zum Menü „Einstellungen“ (`Settings`): Ton, Lautstärke, Tempo im Huddle, Bildqualität
(hoch/sparsam), Knopf „Blick unter die Haube“ an/aus, Vollbild (nicht im Editor). Nur für die Sitzung, nichts wird
gespeichert (Titelbildschirm verspricht das). Verworfen: PlayerPrefs.

## 2026-10-18 – Prolog raus, Rechentafel in die Siedlung

**ADR-20 Kein Prolog „Der letzte Huddle“, die Rechentafel steht in MG1 Schritt 2.** Entscheidung von Jona: Der
Huddle-Prolog (12 Pinguine ziehen) und der Akku-Transfer danach entfallen; die Landkarte hat wieder 3 Karten (ersetzt
ADR-13). Behalten wird sein erster Schritt, die Tafel „vorher + selbst + vom Nachbarn − an den Wind = neu“: jetzt mit
zwei Feldern der Siedlung, Laterne und Iglu, direkt nach der Waage (`PairLessonView`). Modell: dieselbe Zellbilanz
wie die Siedlung mit Speicherglied, implizites Euler (`SettlementPairLesson`, `simref/settlement_pair.py`, Golden
`pair_settlement`); eigene, lesbare Werte im Level-JSON. Gegen eine zweite Wahrheit (ADR-12) prüft ein Test, dass der
Siedlungs-Löser auf dem Raster „L I“ genau den Gleichgewichtspunkt der Tafel liefert. Die Tafel endet mit diesem
Punkt – Brücke zu Schritt 3 (alle Felder) und Schritt 5. Einstellung „Tempo im Huddle“ entfällt mit. Verworfen:
Schritt 3 als Ort (Jona: direkt nach der Waage) und den Akku-Transfer mit neuem Bild behalten.

**ADR-21 Kalles Versuch draußen, das Brett ist ZWILLIS Bildschirm.** Hinweis von Jona (18.10.): Kalle baute auf
demselben Raster, auf dem später simuliert wird – es sah aus, als hätte auch Kalle simuliert, und die Botschaft
„erst simulieren, dann bauen“ verschwamm. Jetzt baut Kalle draußen auf der Scholle neben seiner Baustelle
(`HubWorld.PlayKalleAttempt`, Schmelzen nach demselben Modell = die Wirklichkeit des Spiels), Flocke planscht in der
Pfütze, ein Foto der Szene steht in Schritt 4 neben der Rechnung. Das Labor-Brett hat Rahmen, Leuchtkante, kühles
„Rechen-Eis“ und die Beschriftung „ZWILLI – die Siedlung im Computer“. Verworfen: nur das Wärmebild als Unterschied
(zu schwach) und ein gezeichnetes Symbolbild statt Foto (zeigt nicht Kalles echte Siedlung).

## 2026-10-19 – Wärmewelle statt Wärmefäden, Verschieben, Zoom

**ADR-22 Schritt 3 zeigt die Wärmewelle statt Fäden ziehen zu lassen.** Jona verstand den Fäden-Schritt nicht: vier
Fäden ziehen, danach passierte nichts. Jetzt sieht man die Wirkung – eine Laterne, Iglus nah und fern, die Wärme
wandert Schritt für Schritt von Feld zu Feld und wird schwächer (Bezug zur Karte „Abstand“). Modell: Siedlungsmatrix
mit Speicherglied, implizites Euler (`SettlementWave`, `simref/settlement_wave.py`, Golden `wave_settlement`), mit
dem echten Wind – Endzustand ist die Rechnung der Siedlung, ein Test prüft das. Mit echtem Wind wird auch das nahe
Iglu nicht gemütlich; das ist gewollt (eine Laterne allein reicht nicht). Verworfen: Schritt streichen und
windstille Sonderwerte (zeigt mehr Reichweite, als die Siedlung später hat). Dazu Werkzeug „Verschieben“ und Zoom
auf den Zeiger (Mausrad, Touchpad, Pinch, + und −; Pfeiltasten schwenken) – beides Wünsche von Jona.

## 2026-10-19 – MG2 Polar-Express und MG3 Fischfabrik (Pakete aus zwei Unteragenten)

**ADR-23 Jedes Minigame nach dem Muster aus MG1, in eigenen Dateien.** Wunsch von Jona: Rechentafel von Hand (echte
Zahlen aus demselben Modell), Versuchsaufbau (Kalles Versuch in echt, dann Bauen auf ZWILLIS Bildschirm),
Erklärung der Simulation, Siemens-Transfer. Damit zwei Pakete parallel bauen konnten, lädt das Spiel je Minigame
eigene Text-, Dialog-, Inhalts- und Farbdateien (`ui_mg2_de.json`, `dialogue_mg3_de.json`, `Palette.Mg2.cs` …),
Golden-Erzeuger `simref/golden_*.py`; den Ablauf (GameRoot, Hub, Landkarte) verbindet der Integrator. Der Zustand
der Pakete liegt vorerst in `Mg2Progress` und `FishFactoryState` statt in `GameState`.

**ADR-24 MG2 rechnet Lattice-Boltzmann D2Q9 bei Reynolds-Zahl 20.** Nur so gibt es Widerstand und Wirbel.
Ausströmen mit festem Druck und sanftem Anlauf (Kopie der Nachbarspalte ließ den Druck driften, 27 % zu langsam).
Wagenübergänge als Wulst auf dem Dach – eine Lücke wirkt bei Re 20 nicht. Strömen über eine Quelltabelle, gleich in
Python und C#. Verworfen: Potentialströmung (kein Widerstand), höhere Reynolds-Zahl (instabil in grob), Navier-Stokes
mit Druckkorrektur (zu aufwendig für die Tafel). Details: specs/004.

**ADR-25 MG3 als Ereignis-Simulation mit PCG32-Strömen je Zweck.** Gleiche Zufallszahlen in Python und C#, Runden
vergleichbar. Robo-Robben-Gedränge als feste Zusatzzeit statt Wegreservierung (viel Code, Verklemmungen, schwer zu
erklären). Die Rechentafel nutzt dieselbe Simulation mit einer Station. Die Möwen-Frage ist eine eigene Phase
(7 Schritte in der Leiste). Verworfen: `System.Random`/`random`. Details: specs/005.

**ADR-26 Spielablauf über drei Minigames.** Siedlung → Hub → Tilda am Gleis (MG2) → Hub → Olaf an der Küste (MG3) →
Abschluss mit drei Karten; danach lässt sich jedes Minigame wiederholen. Tilda und Olaf erscheinen im Hub erst, wenn
sie dran sind, und haben eigene Looks. Der Zugtakt von MG3 kommt (noch) nicht aus dem MG2-Ergebnis.

## 2026-10-19 – Realistischer Look

**ADR-27 URP und realistische Darstellung statt Diorama-Facetten.** Wunsch von Jona: „Verpixelung und Grafikqualität
ändern, das Spiel soll ultra realistisch aussehen“. Umgesetzt, ohne fremde Modelle oder Texturen (ADR-16 bleibt
insoweit): Universal Render Pipeline 17.4 (Forward+, HDR, 4× MSAA + SMAA, 4096er weiche Schatten, SSAO, linearer
Farbraum), ACES-Tonemapping, Bloom für Laternen, Farbkorrektur, Vignette, Dunst, prozeduraler Himmel mit
Himmels-Cubemap für Spiegelungen, dreifarbiges Himmelslicht. Weiche Rundungen mit hoher Segmentzahl, physikalische
Oberflächen (`ArtStyle.Surfaces`: Schnee, Eis, Wasser, Gefieder, Metall, Glanz, Licht) mit prozeduralen
Normal-Maps (`SurfaceTextures`), Pinguinkörper mit gemaltem Farbbild statt Stufen-Grenzen. Pipeline-Assets entstehen
reproduzierbar über `RenderPipelineSetup` (Menü/Batchmode); `RenderQuality` ändert nur eine Laufzeitkopie.
Geprüft per Stilprobe im Batchmode (eigener Worktree, damit der offene Editor unberührt bleibt). Grenzen: Die Figuren
bleiben stilisierte Pinguine aus Drehprofilen – „fotorealistisch“ ginge nur mit modellierten, texturierten Assets
(Blender, Federn, Fell-Shader). Verworfen: HDRP (kein WebGL), Post Processing Stack v2 in der eingebauten Pipeline
(veraltet), Umgebungslicht per `DynamicGI.UpdateEnvironment` und Reflexionssonde (asynchron – erstes Bild zufällig
getönt).

## 2026-10-19 – Grafik-Abstimmung nach Audit

**ADR-28 Realistischer Look abgestimmt und Fehler aus ADR-27 behoben.** Grundlage: `GRAPHICS_AUDIT.md` (2. Fassung)
und Stilproben vor/nach. Behoben, was ADR-27 verdeckte: (1) `Surfaces.ByColor` verglich die auf 1/32 gerundete Farbe mit
der ungerundeten Palettenfarbe und traf nie – Meer, Eis, Wasser und Laternenglas blieben „Default“ (kein Wellenbild,
kein Leuchten); jetzt werden beide Seiten gerundet. (2) Kugel, Zylinder und Kapsel trugen seit ADR-27 die Dreh-UV des
Pinguinkörpers (Winkel × Höhe) – auf flachen Deckeln strahlenförmige Streifen; jetzt Box-Projektion plus Tangenten.
(3) Die Shader-Schalter der Laufzeit-Materialien (_NORMALMAP, _EMISSION, _DETAIL_MULX2) wären im Build entfernt
worden; `RenderPipelineSetup` legt je Oberfläche ein Material in `Resources/Materials/Varianten` an. Abgestimmt:
Bloom-Schwelle 2,6 statt 1,05 (sonst blühte jeder Sonnenschnee), Laternenglas Emission 10, Belichtung −0,35,
Schattenstärke 1, Dunst nach Kameraabstand (über dem Spielbrett kein Schleier mehr), bewegtes Wasser mit zweiter
Wellenlage. „Sparsam“ behält die Nachbearbeitung (ohne Tonemapping fraß Schnee aus) und spart Bloom, SSAO,
Laternen-Schatten, 4096er-Schattenkarte, SMAA → FXAA. Laternen-Schatten nur für die zwei Laternen am Blickpunkt.
Verworfen: SSAO-Downsample und MSAA-oder-SMAA ohne Messung auf dem Referenzgerät; Tonemapping-Wechsel (ACES vs.
Neutral, Datenfarben im Wärmebild) und Reflexionssonden – offen für Jona bzw. einen Spieltest.

## 2026-10-21 – Laternen und Iglus lesbar (Spieltest)

**ADR-29 Laterne als echtes Asset, Iglu mit Schneeblock-Fugen, Kalles Siedlung luftiger.** Rückmeldung von Jona:
„Laternen stehen zu nah zusammen“, „Iglus vor dem Schmelzen gegen den weißen Hintergrund kaum zu sehen“, „Laterne ist
nur ein Glühbirnen-Symbol vor einem Ständer“. Ursachen: (1) Das große gelbe Glühbirnen-Symbol mit Strahlen war Unitys
Editor-Gizmo für Punktlichtquellen (jede Laterne mit Licht trägt eins), sichtbar mit eingeschalteten Gizmos in der
Game-Ansicht – kein Spielinhalt, ohne Lernfunktion, im Build nie vorhanden. `Editor/LightIcons.cs` blendet nur dieses
Symbol einmal je Editor-Sitzung aus. (2) Laternenlicht 2,2 bei 3,2 m Reichweite: Schnee und Iglus unter einer Laterne
wurden heller als in der Sonne und fraßen gelb-weiß aus; in Kalles Siedlung (zehn Laternen) die ganze Fläche. Jetzt
`Shading.LanternIntensity` 0,6 × Größe², `LanternRange` 2,2 m × Größe. (3) Die alte Laterne (Würfelsockel, Stab, Kugel,
Würfeldeckel) verdeckte ihr Licht von oben mit dem Deckel. Neu: gusseiserner Mast und geschwungenes Dach als Drehprofil,
leuchtender Glaskasten mit Eckstreben, Dach kaum breiter als das Glas. (4) Iglu: Fugen in `Palette.IglooJoint`
(Deep Blue 40 %) und Tunnel mit dunklem Eingang, jetzt nach Süden (zur Kamera, weg vom Nordwestwind der Leveldaten).
Kuppelfarbe bleibt Light Sand, weil sie in MG1 den Zustand zeigt. (5) Kalles Siedlung: Feldabstand 0,9 statt 0,7 m,
Bausteine 0,9 statt 0,91, Kamera 10 statt 8,5 m – Raster und Leveldaten unverändert. Geprüft mit neuen Stilproben
`kalle_siedlung.png`, `laterne.png`, `laterne_brett.png`. Verworfen: Iglu kühler blau tönen (verwechselbar mit dem
Zustand „kalt“ = `IglooCold`), Laternen in Kalles Siedlung nur verkleinern (das Symbol bliebe, und neben dem Iglu wirkte
die Laterne wie Spielzeug), Glasleuchten auf 3 senken (blieb im Tonemapping trotzdem fast weiß, ADR-28 bleibt).

## 2026-10-21 – Dialog-Review

**ADR-30 Auftakt und Rechentafel nach Dialog-Review (Jona).** Grete: Früher wärmte der Huddle, heute gibt es Iglus und
Strom; Frieda: Die Kolonie wächst, und sie nennt die Frage von MG1 gleich selbst („zu weit weg kalt, zu nah schmilzt“) –
Labor und Ausprobieren erst nach Kalles Versuch, sonst sagte Frieda dasselbe zweimal. Flockes Frage nach dem
geschmolzenen Iglu beantwortet jetzt Frieda statt Gretes Spruch. Rechentafel: Die Formel wird in Alltagssprache erklärt
(Sparschwein: Bestand plus Zugang minus Abgang, dann die vier Kästchen in Tafel-Reihenfolge); in `pair_done` begründet
Frieda, warum jeder Schritt weniger bringt (kleinerer Temperaturunterschied). Der Fachsatz dazu steht im „Blick unter die
Haube“ (`pairFlowNote`), das Popup ist dafür 1040 statt 960 hoch (der Schließen-Knopf lag über der letzten Zeile).
Gestrichen: der Musik-Satz in `whatif_done` (die Karte „Musik im Dorf“ hat schon ihre Rückmeldung). Verworfen: das Wort
„Grenznutzen“ im Dialog (Fachwort aus der Wirtschaft, und physikalisch ist der Grund der kleinere Temperaturunterschied).

## 2026-10-21 – Arktische Landschaft um den Hub (Spieltest)

**ADR-31 Hub in einer Inland-Landschaft statt runder Scholle im Meer-Zylinder.** Rückmeldung von Jona: „Der Rand und
Hintergrund müssen besser aussehen. Mache hier eine arktische, bergige Landschaft und auf der einen Seite das Meer.“
Vorher sah man aus der Hub-Kamera die harte Kreiskante der Scholle (r = 14 m) und dahinter flaches Wasser. Ein erster
Entwurf legte eine Bucht in Gleisrichtung an; Jona hat danach entschieden: „dann gibt es nur Berge und kein Wasser, da
die Kolonie ja im Inland liegt“ (docs/02 bleibt). Neu (`Core/ArcticLandscape.cs`, Werte in `ArtStyle.Landscape`):
ebenes Eis bis weit über Pips Weg hinaus (Laufgrenze `HubWorld.FloeRadius − 1` unverändert), daraus steigt mit
unregelmäßigem Rand eine verschneite Ebene mit Dünen quer zum Nordwestwind; Felsbuckel, dann drei gestaffelte Bergketten
ringsum (Höhenfeld auf einem Polarnetz, Kamm-Rauschen, Fels nach Hangneigung als gemaltes Farbbild – Oberfläche
`Terrain`, gleiche Shader-Schalter wie Wasser, Varianten-Material per „Render-Pipeline einrichten“); ein Presseisrücken
im Norden. Das Gleis führt „an die Küste“, die tausende Kilometer entfernt ist: Es läuft auf einem niedrigen Damm durch
ein Tal, dessen Sohle zu einem Sattel steigt, und verschwindet darüber in der Ferne; dahinter schließen die Berge das Tal
wieder, das Gleisende liegt in der Senke hinter dem Sattel und ist nie zu sehen. Gleishöhe und -länge kommen aus der
Landschaft (`ArcticLandscape.TrackHeight`/`TrackLength`), das Gleis wird mit `Prims.Merge` zusammengefasst. Spiel und
Stilprobe bauen den Hub über `HubWorld.BuildScenery` (Bilder `hub.png`, `hub_rand.png`, `hub_west.png`, `hub_weit.png`,
`hub_pass.png`). Kosten: Landschaft 4 Objekte, ≈ 44 800 Dreiecke; Gleis 3 Objekte, ≈ 2 800 Dreiecke; Schatten nur im
Nahbereich. Alles liegt innerhalb 130 m – die Minigames bei x = 200/400 bleiben frei (und der Hub ist während der
Minigames ausgeschaltet). Grenze: Die Spielkamera (50°, Sichtfeld 30°) zeigt nie den Horizont; im Spiel wirken Eis,
Schneerand, Dünen, Presseis und Bahndamm, die Berge nur aus flacheren Blickwinkeln.
Verworfen: Bucht mit Eiskante und Treibeis am Hub (widerspricht docs/02: Kolonie weit im Inland, tausende Kilometer zum
Meer), Gleis mit Prellbock vor dem Hub (das Gleis soll sichtbar in die Ferne führen), Unitys Terrain-System (eigene Shader
und Splatmaps, schwer für WebGL, nicht über `Palette`/`ArtStyle` steuerbar), ein gemaltes Berg-Panorama im Himmel (fremde
Textur, keine Tiefe, ADR-16/27), Fels als eigenes Teilmesh je Dreieck (harte Dreiecksstufen), Hub-Kamera flacher stellen,
damit die Berge im Bild sind (ändert das Spielgefühl – Frage an Jona).
