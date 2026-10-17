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

**ADR-13 Batterie-Transfer nach dem Prolog, Landkarte mit 4 Karten.** Dieselbe Regel wie im Huddle steckt in der
Batterie-Thermik (Simcenter, Vorschlag). docs/00 hatte drei Karten (je Minigame); der Prolog bekommt eine eigene.
Verworfen: Schaltschrank zweimal zeigen (Wiederholung).

**ADR-14 Schlanke Paketliste, Prüfung gegen den Editor.** Unity schrieb beim ersten Öffnen rund 20 Standardpakete
(Werbung, In-App-Käufe, Xbox-GDK, Analytics …) in die Liste und legte deren Dateien in `Assets/` an. Das Spiel braucht
keines davon; sie sind wieder entfernt. `com.unity.modules.textrendering` gibt es in Unity 6.4 nicht mehr als Paket –
die Prüfung in `check.ps1` hätte das vor dem Öffnen gemeldet.
