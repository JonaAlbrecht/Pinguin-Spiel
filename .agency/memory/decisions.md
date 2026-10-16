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
