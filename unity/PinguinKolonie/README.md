# Unity-Projekt „PinguinKolonie“ – Prototyp v0.1 (Vertical Slice)

## Starten

1. **Unity Hub → Add → Add project from disk** → diesen Ordner `unity/PinguinKolonie` wählen.
   Editor: Unity 6 (6000.x LTS). Beim ersten Öffnen legt Unity `ProjectSettings/` und `Library/` an.
2. Warten, bis der Import fertig ist. Der Simulationskern wird als lokales Paket eingebunden
   (`Packages/manifest.json` → `com.sfe.simcore` aus `../../sim-core/com.sfe.simcore`).
3. Menü **Pinguin-Spiel → Prototyp starten**.
   Das legt beim ersten Mal `Assets/_Project/Scenes/Prototype.unity` an und startet den Play-Modus.

Das ganze Spiel wird zur Laufzeit per Code gebaut (`Scripts/Core/GameRoot.cs`). Es gibt keine Prefabs und
keine von Hand gebauten Szenen. Die Szene enthält nur ein Objekt mit `GameRoot`.

## Steuerung

| Wo | Eingabe |
|---|---|
| Hub (Huddle-Platz) | **W A S D / Pfeiltasten** laufen, **E / Leertaste / Enter** Figur ansprechen – oder hinklicken/-tippen |
| Dialoge | Klick auf die Sprechblase oder **E / Leertaste / Enter** |
| Schritt 2, Schritt 5 | Maus oder Finger: „Nächster Zeitschritt“ klicken, Bausteine setzen |

## Bauen

- Menü **File → Build Profiles** oder per Batchmode:
  ```
  Unity -batchmode -quit -projectPath unity/PinguinKolonie \
        -executeMethod SFE.Game.EditorTools.PrototypeSetup.BuildWebGL
  ```
  (`BuildWindows` für einen Windows-Build). Ergebnis unter `Builds/`.

## Tests im Editor

**Window → General → Test Runner → EditMode**: Die Tests aus `sim-core/com.sfe.simcore/Tests` erscheinen dort
(gleiche NUnit-Tests wie bei `dotnet test`).

## Wenn etwas hakt

| Problem | Lösung |
|---|---|
| Fehler zu `com.unity.test-framework` oder `com.unity.ugui` beim Öffnen | In `Packages/manifest.json` die Version entfernen bzw. im Package Manager die angebotene Version wählen |
| Beim ersten Öffnen fragt Unity, ob das neue Input System aktiviert werden soll | „Yes“ oder „No“ – beides läuft. Das Paket `com.unity.inputsystem` ist eingetragen, der Code unterstützt beide Wege (beide kompiliert, `tools/unity_compile_check`) |
| Kein Ton | Knopf „Ton: an/aus“ oben rechts. Alle Geräusche werden zur Laufzeit erzeugt (`Scripts/Core/Sfx.cs`), es gibt keine Audiodateien |
| URP statt Built-in | Läuft ebenfalls; Materialien werden aus den Standard-Materialien der Primitive abgeleitet |
| Wärmebild im Build rosa/unsichtbar | Einmal **Pinguin-Spiel → Prototyp-Szene erstellen** ausführen – das legt `Resources/Materials/Unlit.mat` an, damit der Shader im Build ist |

## Ordner

```
Assets/_Project/
  Scripts/Core/        GameRoot (Ablauf), Hud (Dialoge, Hinweise, Phasenleiste), Kamera, Eingabe, Grafik-Helfer
  Scripts/Hub/         Huddle-Platz zum Herumlaufen
  Scripts/Minigames/   MG1 „Die erste Siedlung“ (mit Rechenschritt-Tafel `PairLessonView`), Transfer-Screen
  Scripts/Editor/      Szene erzeugen, Builds
  Resources/Data/      Level, Materialien, Dialoge, Inhalte, Transfer-Karte (JSON – auch für Python und dotnet-Tests)
```
