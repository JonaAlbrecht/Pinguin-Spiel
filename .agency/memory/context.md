# Projektstand

**Stand 2026-10-16:** v0.2 auf `feature/v0.2-lernen-und-leben`, Spec `specs/002-v0.2-lernen-und-leben/spec.md`.

| Teil | Stand |
|---|---|
| Python-Referenz | 50 Tests grün (Modell, Waage, 8 Windrichtungen, Kaskade, Budget, Was-wäre-wenn, Lint, Inhaltsdaten) |
| C#-Kern | 52 Tests grün; Mutationen Kaskade / Windtabelle / Budget / beste Runde / Klaras Lob werden rot |
| Unity-Schicht | kompiliert gegen Unity 6000.4.10f1 (alt + neu Input, Editor); **nie im Editor gespielt** |
| Texte | alle in JSON, Lint inkl. C#-Literale und Schlüssel-Verweise |
| Grafik (19.10.) | URP, realistischer Look (ADR-27), abgestimmt mit ADR-28; nur per Stilprobe im Batchmode geprüft, kein WebGL-Build, keine Messung |
| Hub-Landschaft (21.10.) | Festeis, Schneeebene, Berge, Bucht in Richtung Gleis (ADR-31); Stilprobe `hub*.png`; Play-Modus, WebGL und Bildrate ungeprüft; Widerspruch zu docs/02 (Inland) offen |

Nächste Schritte: Spieltest nach Spec 002 Abschnitt 4 · `.meta`-Dateien nach dem ersten Import committen ·
offene Fragen (45 s vs. 60 s, Igluwärme) mit dem Team klären · Freigabe der Siemens-Inhalte.
