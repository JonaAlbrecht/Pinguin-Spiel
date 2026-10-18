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

**ADR-15 Bildqualität in der eingebauten Pipeline, URP später.** Ursachen der Pixeligkeit (Spieltest 17.10.): MSAA nur 2×
(WebGL 0×), 8-Texel-Felder mit 1-Texel-Gitterlinien stark vergrößert, nur 2–4 Pixel-Lichter bei bis zu 13 Laternen,
TextMesh mit fontSize 64, 96-px-Symbolbilder. Behoben per Code (`RenderQuality`, Felder als 1 Texel ohne Glättung +
Gitterlinien als 3D-Streifen, Schrift in 160 gerastert, Bilder 256–288 px). Verworfen für jetzt: Umstieg auf URP
(Bloom für Laternen, bessere Kantenglättung, Nachbearbeitung) – lohnt sich, braucht aber einen Spieltest im Editor,
weil Materialien dabei leicht rosa werden. Vorschlag für den nächsten Schritt.

## 2026-10-17 – Stilregeln und neue Pinguine

**ADR-16 Ein Stil aus Code statt fremder Assets.** Die Pinguine waren Kugeln und Würfel („Schneemann“). Statt freier
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
