# 04 – Technische Spezifikation

> **Umfang:** Das Kernspiel (Release 1) besteht aus **MG1 Huddle, MG2 Polar-Express, MG3 Fischfabrik** (siehe [03_Minigames.md](03_Minigames.md)). Module, die nur die Smart-City-Erweiterung braucht, sind unten mit *(Erweiterung)* markiert. Minigame-Kürzel mit zwei Ziffern (MG01–MG07) beziehen sich auf die Erweiterung ([05_Erweiterung_Pingopolis_SmartCity.md](05_Erweiterung_Pingopolis_SmartCity.md)).

## 1. Leitprinzipien

1. **Simulation zuerst, Engine danach.** Die gesamte Simulationslogik lebt in einer reinen C#-Bibliothek **ohne** `UnityEngine`-Abhängigkeit. Sie läuft und wird getestet mit `dotnet test` – ohne Unity-Lizenz, ohne Editor, in CI und in KI-gestützter Entwicklung.
2. **Python validiert, C# portiert** (aus der Konzeptvorstellung): Jeder Solver existiert zuerst als Python-Referenz; Python erzeugt *Golden Files*, gegen die C# getestet wird.
3. **Full-Code, aber Unity-importierbar.** Alles ist Text: C#-Quellen, JSON-Leveldaten, ink-Dialoge, Blender-Python-Exportskripte, Unity-YAML mit *Force Text*. Szenen und Prefabs werden möglichst per Editor-Skript aus Daten erzeugt, nicht von Hand (oder KI) im YAML editiert.
4. **Unity ist Präsentationsschicht.** Unity rendert, animiert, nimmt Eingaben entgegen und ruft den Core auf.
5. **Zielgruppe Nicht-Techniker.** Technik bleibt unter der Haube: Die Unity-Schicht übersetzt Solver-Ergebnisse in Farben, Ampeln und Sterne. Formeln, Matrizen und Konvergenzplots werden nur im optionalen Modus „Blick unter die Haube“ gerendert (siehe [00_Zielgruppe_und_Siemens-Bezug.md](00_Zielgruppe_und_Siemens-Bezug.md)).
6. **Spec-driven Development.** Diese Dokumente sind der Kontext für KI-gestützte Entwicklung (Token-Budget sparen, weniger Rückfragen).

---

## 2. Technologie-Stack

| Bereich | Wahl | Anmerkung |
|---|---|---|
| Engine | **Unity 6 LTS** (Version im Repo fixieren, `ProjectSettings/ProjectVersion.txt`) | **Lizenz:** Unity Personal ist für Siemens nicht zulässig → Unity Pro/Enterprise/Industry-Seats nötig (offener Punkt) |
| Render-Pipeline | URP | Toon-Shader via Shader Graph, SRP Batcher |
| Zielplattformen | **WebGL** (primär, Browser im Intranet), Windows Standalone (Messe/Events) | WebGPU erst, wenn in Unity stabil |
| Scripting Backend | IL2CPP | WebGL erzwingt IL2CPP |
| Kamera | Cinemachine 3 | Fixe Vogelperspektive, sanftes Follow |
| Input | Input System | Maus/Touch/Gamepad |
| UI | UI Toolkit (UXML/USS = Text, gut für Full-Code) | uGUI nur für World-Space-Sprechblasen, falls nötig |
| Dialoge | **ink** (inkle, MIT) + ink-unity-integration | `.ink`-Dateien sind reiner Text |
| Lokalisierung | Unity Localization | DE (primär), EN |
| JSON | `com.unity.nuget.newtonsoft-json` (nur Unity-Schicht) | Core bleibt frei von Serialisierungs-Abhängigkeiten |
| Tests | NUnit (Core, via `dotnet test`) + Unity Test Framework (EditMode/PlayMode) | Gleiche NUnit-Tests laufen in beiden Welten |
| 3D | **Blender LTS** (4.5 oder neuer) | FBX für Rigs/Animationen; glTF (glTFast) optional für statische Meshes |
| Python-Referenz | Python 3.11+, numpy, scipy, pytest, matplotlib | Nur Validierung/Tooling, nicht im Spiel |
| CI | GitHub Actions | Python + dotnet ohne Unity-Lizenz; Unity-Build via GameCI (benötigt Lizenz-Secrets) |

---

## 3. Repository-Struktur

```
pinguin-spiel/
├─ docs/                              # Diese Spezifikation (KI-Kontext)
├─ sim-core/
│  └─ com.sfe.simcore/                # UPM-Paket UND .NET-Bibliothek
│     ├─ package.json
│     ├─ Runtime/
│     │  ├─ SFE.SimCore.asmdef        # "noEngineReferences": true
│     │  ├─ LinearAlgebra/            # CSR-Matrix, Builder, CG, Cholesky
│     │  ├─ Random/                   # PCG32 (identisch in Python)
│     │  ├─ Thermal/                  # MG1 Huddle + Siedlung (Erweiterung: MG01)
│     │  ├─ Flow/                     # MG2 Polar-Express, MG2-B (Lattice-Boltzmann); Erweiterung: MG02 Quadtree
│     │  ├─ DiscreteEvent/            # MG3 Fischfabrik (Erweiterung: MG04)
│     │  ├─ Optimization/             # Hill-Climbing, GA, Pareto
│     │  ├─ Structure/                # (Erweiterung) MG03 Fachwerk-FEM
│     │  ├─ Rail/                     # (Erweiterung) MG05
│     │  ├─ Grid/                     # (Erweiterung) MG06 DC-Lastfluss
│     │  └─ Coupling/                 # (Erweiterung) MG07 Co-Simulation
│     ├─ Tests/
│     │  ├─ SFE.SimCore.Tests.asmdef  # Unity EditMode-Tests
│     │  └─ *.cs                      # NUnit-Tests (auch von dotnet genutzt)
│     └─ Dotnet~/                     # "~" → wird von Unity ignoriert
│        ├─ SimCore/SFE.SimCore.csproj      # netstandard2.1, kompiliert ../../Runtime/**
│        └─ Tests/SFE.SimCore.Tests.csproj  # net8.0, NUnit, kompiliert ../../Tests/**
├─ sim-reference/                     # Python-Referenzimplementierungen
│  ├─ simref/ (thermal.py, lbm.py, des.py, pcg32.py …; Erweiterung: flow.py, truss.py, grid.py)
│  ├─ tests/
│  └─ generate_golden.py              # schreibt ../golden/*.json
├─ golden/                            # Referenzergebnisse (JSON), von C#-Tests gelesen
├─ unity/PinguinKolonie/             # Unity-Projekt
│  ├─ Packages/manifest.json          # "com.sfe.simcore": "file:../../../sim-core/com.sfe.simcore"
│  └─ Assets/
│     ├─ _Project/
│     │  ├─ Scripts/
│     │  │  ├─ Core/                  # Bootstrap, Services, EventBus, Save
│     │  │  ├─ Hub/                   # Spielerbewegung, NPC, Interaktion
│     │  │  ├─ Minigames/Common/      # IMinigame, Phasen-State-Machine, Transfer
│     │  │  ├─ Minigames/MG1_Huddle/ … # Adapter Core ↔ Visualisierung
│     │  │  ├─ Visualization/         # Heatmap, Stromlinien, Matrix-View
│     │  │  └─ Editor/                # AssetPostprocessor, SceneBuilder, Build-Skripte
│     │  ├─ Resources/Data/Levels/*.json    # Leveldefinitionen (auch von Python lesbar)
│     │  ├─ Resources/Data/Transfer/*.json  # Transfer-Karten
│     │  ├─ Dialogue/*.ink
│     │  ├─ Art/ (Models, Materials, Textures, Animations, Shaders)
│     │  ├─ Scenes/ (Bootstrap, Hub, MG1_Huddle, MG2_PolarExpress, MG3_FishFactory, …)
│     │  └─ UI/ (*.uxml, *.uss)
│     └─ ThirdParty/
├─ blender/
│  ├─ source/*.blend                  # Git LFS
│  ├─ scripts/export_assets.py        # Headless-Export
│  └─ README.md                       # Modellierkonventionen
├─ ASSET_LICENSES.md                  # Herkunft + Lizenz jedes Fremd-Assets
└─ .github/workflows/
```

**Warum `Dotnet~/`:** Unity ignoriert Ordner mit `~`-Suffix. So liegen `.csproj`, `bin/` und `obj/` im Paket, ohne dass Unity generierte `.cs`-Dateien aus `obj/` mitkompiliert.

**Git:** `.gitattributes` mit LFS für `*.blend, *.fbx, *.png, *.psd, *.wav, *.ogg`; Unity-`.gitignore` (Library/, Temp/, Builds/ …); Editor-Einstellungen *Asset Serialization = Force Text*, *Visible Meta Files*.

---

## 4. Sim-Core (reines C#)

### 4.1 Sprach- und Plattformregeln
- Target `netstandard2.1`, `<LangVersion>9.0</LangVersion>`.
- **Nicht verwenden:** `record`, `init`-Accessoren (fehlendes `IsExternalInit` in Unity), `System.Text.Json`, Threads/`Task.Run` (WebGL ist single-threaded), Reflection-lastiger Code (IL2CPP-Stripping).
- Fließkomma: `double` im Core; Konvertierung zu `float` erst in der Unity-Schicht.
- Zufall: ausschließlich `Pcg32` aus dem Core (nie `System.Random`/`UnityEngine.Random`) → bitgenau gleiche Zufallsfolgen in Python und C#, reproduzierbare Replikationen.
- Keine Allokationen in inneren Solver-Schleifen (Arrays vorallokieren).

### 4.2 Schrittweise Solver
Alle Solver sind **steppable**, damit Unity Iterationen über mehrere Frames animieren kann (Phase „Lösung“) und WebGL nie blockiert:

```csharp
namespace SFE.SimCore
{
    public interface IStepSolver
    {
        bool IsConverged { get; }
        int Iteration { get; }
        double Residual { get; }
        void Step(int maxIterations = 1);    // rechnet höchstens n Iterationen
    }
}

namespace SFE.SimCore.LinearAlgebra
{
    public sealed class SparseMatrixBuilder
    {
        public SparseMatrixBuilder(int n) { /* … */ }
        public void Add(int row, int col, double value);  // summiert Duplikate (Assemblierung!)
        public CsrMatrix Build();
    }

    public sealed class ConjugateGradientSolver : IStepSolver
    {
        public ConjugateGradientSolver(CsrMatrix a, double[] b, double[] x0, double tolerance = 1e-8);
        public double[] Solution { get; }
        // IStepSolver …
    }
}
```

Die Assemblierung meldet optional jeden `Add`-Aufruf über einen Callback – so kann die Unity-Schicht in Phase „Systemaufbau“ die Matrix live mitfüllen.

### 4.3 Module (Kurzspezifikation)

| Modul | Kern-API | Größe zur Laufzeit |
|---|---|---|
| `Thermal` | `HuddleModel` (transient, implizites Euler), `SettlementModel(Level) → (CsrMatrix, b)` inkl. `MeltCascade` (Iglu → Wasser, neu lösen, max. 5 Durchläufe) | 24×24 = 576 Unbekannte (Erweiterung MG01: 32×32) |
| `Flow` | `LbmD2Q9(nx, ny, tau)`, `SetObstacle(mask)`, `Step(n)`, `DragForce`, `Streamlines()`; optional `ScalarField` für MG2-B (Temperatur) | 160×64 fein / 80×32 grob |
| `DiscreteEvent` | `Simulator` (Ereignis-Heap), Bausteine `Source, ScheduledSource (Zug), Buffer, Station, Conveyor, Agv, Loss (Möwen), Sink`, `Replications.Run(n, seed)` | ~10⁴ Ereignisse/Schicht |
| `Optimization` | `HillClimber`, `GeneticAlgorithm`, `ParetoFront` – ebenfalls steppable | — |
| *(Erweiterung)* `Flow.Quadtree` | `Quadtree.Refine(cell)`, `PotentialFlowModel.Assemble()`, `VelocityAt(x,y)`, `ErrorVs(reference)` | ≤ 2 000 Zellen |
| *(Erweiterung)* `Structure` | `Truss.AddNode/AddMember`, `Assemble()`, `Solve()`, `MemberForces`, `SafetyFactors` | ≤ 200 FHG |
| *(Erweiterung)* `Rail` | `LineModel`, `BlockingMode {Fixed, Moving}`, `Step(dt)` | 6 Stationen, ≤ 12 Züge |
| *(Erweiterung)* `Grid` | `DcPowerFlow(network, injections) → flows`, `DaySimulation(24)` | ≤ 100 Knoten |
| *(Erweiterung)* `Coupling` | `CoSimulation` mit festen Kopplungsschritten zwischen Modell-Adaptern | — |

Alle Größen sind so gewählt, dass ein Solve in WebGL (single-threaded, ohne Burst) deutlich < 50 ms dauert. Ausnahme `Flow` (LBM): kein einzelner Solve, sondern viele kleine Schritte – Budget ≤ 4 ms pro Frame, ganzer Lauf als Zeitraffer ≤ 10 s.

### 4.4 Validierung
- `sim-reference/` implementiert dieselben Modelle in numpy/scipy (z. B. `scipy.sparse.linalg.cg`, `simpy`-freie eigene DES, damit Ereignisreihenfolge identisch ist).
- `generate_golden.py` schreibt pro Testfall `golden/<modul>/<fall>.json` (Eingabe + erwartete Ausgabe + Toleranz).
- C#-Tests laden diese Dateien; Toleranz für deterministische Solver `1e-6` relativ (LBM: `1e-4`, wegen vieler Zeitschritte), für DES exakte Gleichheit der KPIs bei gleichem Seed.
- Zusätzlich analytische Tests (z. B. 1D-Wärmeleitung linear, Fachwerk-Statik von Hand, M/M/1-Warteschlange gegen Formel).

---

## 5. Unity-Architektur

### 5.1 Szenen
| Szene | Inhalt | Laden |
|---|---|---|
| `Bootstrap` | Services, Save, Localization, Audio | immer geladen |
| `Hub` | Stadt, Spieler, NPCs | additiv |
| `MGxx_*` | Minigame-Umgebung | additiv, Hub wird pausiert/ausgeblendet |
| `Transfer` | Transfer-Screen | additiv als Overlay |

### 5.2 Minigame-Framework

```csharp
public enum DidacticPhase { Modelling, Equation, Assembly, Solution, Optimization, Transfer }

public interface IMinigame
{
    string Id { get; }                          // z. B. "MG1_Huddle"
    IReadOnlyList<DidacticPhase> Phases { get; }
    DidacticPhase FocusPhase { get; }
    void Enter(MinigameContext context);        // Leveldaten, Schwierigkeit, Flag „Blick unter die Haube“
    void BeginPhase(DidacticPhase phase);
    event Action<DidacticPhase> PhaseCompleted;
    MinigameResult Finish();                    // Score, Sterne, Screenshot für Transfer
}
```

- Eine generische `MinigamePhaseController`-State-Machine führt durch die Phasen, zeigt Frieda-Dialoge (ink-Knoten `mg1_equation_intro` usw.) und blendet passende UI ein.
- Jedes Minigame besteht aus **Adapter** (ruft Core), **View** (Visualisierung) und **Input-Tools** (Pinsel, Drag-&-Drop, Regler).

### 5.3 Services
- `GameState` (Fortschritt, Ausbaustufe der Siedlung, Sterne, kosmetische Items; Erweiterung: ZWILLI-Fragmente)
- `SaveService` – JSON in `Application.persistentDataPath`; in WebGL IndexedDB-basiert, nach dem Speichern Dateisystem-Sync sicherstellen.
- `EventBus` – entkoppelt Hub, Minigames, UI.
- `DialogueService` – ink-Runtime, Variablen-Bridge zu `GameState`.
- `TelemetryService` – standardmäßig **aus** (siehe Datenschutz).

### 5.4 Datenformat Level (Beispiel MG1)

```json
{
  "id": "MG1_Huddle_Settlement",
  "grid": { "width": 24, "height": 24, "cellSize": 2.0 },
  "climate": { "airTemp": -30.0, "wind": 8.0, "windDirection": "NW" },
  "materials": "materials_settlement_v1",
  "initialTiles": "base64-or-rle-encoded-tilemap",
  "lantern": { "heat": 1.0, "cost": 100 },
  "goals": { "iglooComfortMin": -8.0, "iglooMelt": 0.0, "iglooTarget": 12 },
  "budget": 1000,
  "rounds": 3
}
```
Dieselbe Datei wird von Python (`simref`) und vom C#-Core gelesen → Level lassen sich vor dem Einbau in Python durchrechnen und balancen.

### 5.5 Visualisierung
- **Heatmap:** Datentextur (24×24 bzw. 32×32, `RGBAHalf` bzw. `R8` normiert als Fallback) + Colormap-LUT im Shader, auf Bodenkacheln projiziert. Farbskalen farbfehlsichtig-tauglich (viridis/cividis), Legende immer sichtbar.
- **Stromlinien und Wirbel (MG2):** GPU-freundliche Partikel (VFX Graph nicht in WebGL → CPU-Partikel/Line Renderer, ≤ 500 Partikel), Wirbelstärke als rote Einfärbung hinter dem Zug.
- **Fabrik (MG3):** wachsende Kistenstapel als Puffer-Anzeige, blinkende Engpass-Station.
- *(Erweiterung)* **Spannungen:** Stab-Mesh mit Vertex-Color + Dicke.
- **Kopplungsfäden:** Linien zwischen Kacheln/Knoten, die beim Assemblieren aufleuchten (Standardmodus statt Matrix).
- **Ampel- und Sterne-Bewertung:** Jedes Minigame liefert KPIs aus dem Core; ein `ResultTranslator` bildet sie auf Ampel (grün/gelb/rot) und Sterne ab. Keine Rohzahlen im Standardmodus.
- **Nur „Blick unter die Haube“:** Matrix-View (Sparsity-Pattern), Konvergenzplot, Pareto-Front, Bildfahrplan, Formeln – UI-Toolkit-Painter2D-Komponenten.
- **Standard-Diagramme:** nur einfache Balken (z. B. „gute und schlechte Tage“ in MG3).

### 5.6 UX-Anforderungen für Nicht-Techniker
- Steuerung ausschließlich Maus/Touch (Gamepad optional), keine Tastenkombinationen nötig.
- Kein Zeitdruck, kein Game Over; Hilfe-System: nach 2 Fehlversuchen Tipp, nach 3 Lösungshilfe.
- Texte: max. 2 Zeilen pro Sprechblase, Vorlesefunktion optional, skalierbare Schrift, WCAG-AA-Kontraste, farbfehlsichtig-taugliche Farben (Ampeln zusätzlich mit Symbol ✔ / ! / ✘).
- Jederzeit speicher- und unterbrechbar; ein Minigame ≤ 10 Min.
- Optionales Vorher-/Nachher-Quiz (5 Fragen, anonym) zur Messung der Lernziele.
- **Text-Lint im Build:** Ein Editor-Check prüft alle Standardmodus-Texte (ink, Lokalisierung, Transfer-Karten) gegen eine Sperrliste (z. B. `Matrix, FEM, CFD, DES, CBTC, Diskretisierung, Gleichungssystem`) und Formelzeichen; Treffer brechen den Build ab.

### 5.7 Transfer-Karten „So macht's Siemens“ (Datenmodell)
```json
{
  "id": "transfer_MG3_factory",
  "minigame": "MG3_FishFactory",
  "siemensBusiness": "Digital Industries",
  "headline": { "de": "Was du gemacht hast, macht Siemens für Fabriken weltweit", "en": "…" },
  "body": { "de": "max. 60 Wörter …", "en": "…" },
  "customerBenefits": [ { "de": "Engpässe vor dem Bau finden" }, { "de": "Teure Umbauten vermeiden" } ],
  "product": "Tecnomatix Plant Simulation",
  "media": { "image": "transfer/mg04_line.png", "video": null, "source": "…" },
  "voiceClip": { "file": null, "speaker": "Name, Funktion", "approved": false },
  "takeaway": { "de": "Bevor eine Fabrik gebaut wird, lässt man sie im Computer laufen …" },
  "moreLink": null,
  "approval": { "department": null, "communications": null, "date": null }
}
```
Der Build schlägt fehl, wenn eine im Spiel referenzierte Karte kein vollständiges `approval` hat (Platzhalter-Karten sind nur in Entwicklungs-Builds erlaubt und deutlich als „Entwurf“ markiert).

### 5.8 Kamera & Look
- Hub: Perspektive, Pitch ~50°, niedriges FOV (~30°) für „Diorama“-Wirkung; optionaler „Curved World“-Vertex-Shader (Animal-Crossing-Effekt).
- Minigames: Wechsel zu Top-Down/Orthografisch per Cinemachine-Blend.
- Toon-Shading (2–3 Lichtstufen, Rim-Light), Outlines per Inverted Hull, weiche Schatten, leichter Bloom/Tilt-Shift.

---

## 6. Blender → Unity Pipeline

### 6.1 Konventionen
| Thema | Regel |
|---|---|
| Einheiten | Metrisch, Unit Scale 1.0, **1 BU = 1 m**; Pinguin (Spieler) ≈ 0,6 m |
| Ausrichtung | Modell schaut nach **−Y** in Blender (wird beim Export zu +Z in Unity) |
| Pivot | Gebäude/Props: Mitte unten; Charaktere: zwischen den Füßen |
| Raster | Stadt-Baukasten auf **2-m-Raster**, Gebäude-Footprints Vielfache von 4 m |
| Transformationen | Vor Export *Apply All Transforms* (Rotation/Skalierung) |
| Benennung | `SM_` statisch, `SK_` skinned, `M_` Material, `T_` Textur, `A_` Animation; LODs `_LOD0.._LOD2`; Kollision `_COL` |
| Materialien | Pro Asset-Familie **1 Material + Palette-Atlas** (256×256 Farbfelder, UVs auf Farbfelder) → wenige Draw Calls, kleiner Download |
| Polybudget | Spieler/NPC ≤ 6 k Tris, Gebäude ≤ 3 k, Props ≤ 500; sichtbar gesamt ≤ 300 k |
| Rig | Einfaches eigenes Pinguin-Rig (~20 Knochen), Unity-Animation-Type **Generic** (nicht Humanoid) |
| Animationen | `idle, waddle, run, belly_slide, jump, talk, cheer, think, huddle, fall_over` als Actions; Export je Clip `SK_Penguin@waddle.fbx` oder alle Actions in einer Datei |
| Geometry Nodes | Erlaubt für Variationen; beim Export realisieren (Modifier anwenden) |

### 6.2 FBX-Exporteinstellungen
`Apply Scalings: FBX Units Scale` · `Forward: -Z` · `Up: Y` · `Apply Unit: ✔` · `Use Space Transform: ✔` · `Apply Transform: ✔` · `Object Types: Armature, Mesh, Empty` · `Add Leaf Bones: ✘` · `Bake Animation: ✔` · `Smoothing: Face`

### 6.3 Headless-Export (Full-Code)
```bash
blender -b blender/source/city_kit.blend \
        --python blender/scripts/export_assets.py -- \
        --out unity/PinguinKolonie/Assets/_Project/Art/Models/Colony
```
`export_assets.py` exportiert jede Collection mit Custom Property `export=true` als eigene FBX-Datei mit obigen Einstellungen. Damit lässt sich der Asset-Export in CI oder per Skript wiederholen.

### 6.4 Unity-Import-Automatik
`AssetPostprocessor` in `Scripts/Editor/`:
- Pfadbasierte Import-Presets (Scale, Animation Type, Materialien extrahieren/remappen auf Projekt-Toon-Material).
- `_COL`-Meshes → `MeshCollider`, Renderer entfernt.
- `_LODn` → automatische `LODGroup`.
- Prüft Namenskonvention und Polybudget, warnt in der Konsole.

### 6.5 Fremd-Assets
Wo möglich **CC0**-Quellen (z. B. Kenney, Quaternius, Poly Haven) als Basis, im eigenen Stil nachkoloriert. Jede Datei wird in `ASSET_LICENSES.md` mit Quelle, Lizenz und Datum erfasst. Schriften nur mit OFL-Lizenz.

---

## 7. Full-Code-Workflow (ohne Unity-Editor arbeiten, in Unity importierbar bleiben)

| Tätigkeit | Ohne Editor möglich? | Wie |
|---|---|---|
| Simulationslogik schreiben/testen | ✔ | `dotnet test sim-core/com.sfe.simcore/Dotnet~/Tests/SFE.SimCore.Tests.csproj` |
| Solver validieren | ✔ | `pytest sim-reference` + `python generate_golden.py` |
| Leveldaten, Dialoge, Transfer-Karten | ✔ | JSON / ink / UXML+USS bearbeiten |
| 3D-Assets exportieren | ✔ | Blender headless (6.3) |
| Unity-Gameplay-Skripte (MonoBehaviours) | ✔ schreiben, Kompilierung prüfen erst in Unity | Unity Batchmode in CI |
| Szenen/Prefabs | ⚠ | Per **SceneBuilder**-Editorskript aus Daten erzeugen, nicht YAML von Hand editieren |
| Tests in Unity | ✔ (Batchmode) | `Unity -batchmode -projectPath unity/PinguinKolonie -runTests -testPlatform EditMode -testResults results.xml` |
| WebGL-Build | ✔ (Batchmode) | `-executeMethod SFE.Build.BuildScript.BuildWebGL` |
| Look & Feel, Animationen tunen | ✘ | im Editor (Mensch) |

Das ist der Hebel für das Token-Budget: Der Großteil der KI-generierten Arbeit (Core, Tests, Daten, Python) entsteht außerhalb von Unity und ist sofort testbar.

---

## 8. Qualität & Tests

| Ebene | Werkzeug | Läuft in |
|---|---|---|
| Python-Referenz | pytest | CI (jeder Push) |
| Core-Unit + Golden-Tests | NUnit via `dotnet test` | CI (jeder Push) |
| Unity EditMode | Unity Test Framework | CI (nightly, Lizenz nötig) |
| Unity PlayMode (Smoke: jede Szene lädt, jedes Minigame startbar) | Unity Test Framework | CI (nightly) |
| Performance | Profiler-Marker im Adapter, Budget-Test „Solve < 50 ms“ im WebGL-Build | manuell pro Meilenstein |
| Didaktik/Usability | Playtests mit Siemens-Kolleg:innen **ohne technischen Hintergrund** (HR, Finance, Vertrieb, Kommunikation …); Erfolg = Abnahme-Kriterien aus Dokument 00 | pro Meilenstein |
| Sprachregeln | Text-Lint (5.6) | jeder Build |

**Performance-Budgets (WebGL, Office-Laptop mit iGPU):** 60 FPS Ziel / 30 FPS Minimum, < 300 Draw Calls, initialer Download ≤ 50 MB (Brotli), Ladezeit Hub ≤ 15 s.

---

## 9. Hosting, Datenschutz, Lizenzen (offene Punkte aus der Konzeptvorstellung)

| Thema | Vorschlag | Zu klären mit |
|---|---|---|
| **Unity-Lizenz** | Unity Pro/Enterprise/Industry für Entwickler-Seats; CI-Build-Lizenz | Einkauf / Lizenzmanagement |
| **Hosting** | WebGL-Build als statische Website im Siemens-Intranet (Brotli-Header korrekt setzen); kein Backend für das Kernspiel nötig | IT / Hosting-Verantwortliche |
| **Leaderboard/Multiplayer** | Optional, eigener kleiner REST-Dienst mit Siemens-SSO; Pseudonyme statt Klarnamen | IT-Security, Datenschutz, **Betriebsrat** (Leistungsvergleich unter Mitarbeitenden) |
| **Telemetrie** | Standardmäßig aus; falls gewünscht nur anonyme, aggregierte Lernmetriken | Datenschutz, Betriebsrat |
| **Siemens-Inhalte in Transfer-Screens** | Nur freigegebene Bilder/Projekte/Zahlen, Quellen pro Karte; je Geschäft eine Ansprechperson für Inhalte und ggf. Videoclip | Fachbereiche (DI, SI, Mobility), Kommunikation |
| **Verteilung** | Einbindung in Lernplattform/Onboarding-Pfade | HR / Learning |
| **Token-Budget** | Spec-driven + Core außerhalb Unity reduziert Kontext und Iterationen | Projektleitung |
| **Klassifizierung** | Dokumente/Build entsprechend „Restricted“ behandeln | Projektleitung |

---

## 10. Meilensteine (grobe Reihenfolge)

| # | Meilenstein | Ergebnis |
|---|---|---|
| M0 | Setup | Repo-Struktur, CI (Python + dotnet), Unity-Projekt mit lokalem `simcore`-Paket, Blender-Exportskript, Stil-Test (1 Pinguin + 1 Iglu + 1 Laterne im Toon-Look) |
| M1 | Sim-Core Vertical Slice | `LinearAlgebra`, `Thermal` (Huddle + Siedlung + Schmelz-Kaskade) in Python + C#, Golden-Tests grün |
| M2 | Vertical Slice spielbar | Hub-Ausschnitt (Huddle-Platz), MG1 mit allen Phasen, 1 Transfer-Screen (Schaltschrank), Prolog-/Akt-1-Dialoge, WebGL-Build intern gehostet |
| M3 | Playtest & Auswertung | Playtest mit 10–15 Kolleg:innen ohne technischen Hintergrund, Vorher-/Nachher-Quiz, Anpassungen Didaktik/UX |
| M4 | Release 1 – Kernspiel | + `Flow` (LBM) und `DiscreteEvent`/`Random`, MG2 Polar-Express, MG3 Fischfabrik, Epilog, Speichern, Lokalisierung EN |
| M5 | Release 2 – Erweiterung (optional) | Smart City Pingopolis gemäß Dokument 05; optional MG2-B, Leaderboard |

---

## 11. Definition of Done (pro Minigame)

- [ ] Python-Referenz + Golden Files vorhanden, C#-Tests grün
- [ ] Alle didaktischen Phasen spielbar, Schwerpunkt-Phase ausführlich
- [ ] Abnahme-Kriterien für Nicht-Techniker aus Dokument 00, Abschnitt 6 erfüllt
- [ ] Standardmodus ohne Formeln/Fachabkürzungen (Text-Lint grün)
- [ ] Transfer-Karte „So macht's Siemens“ vollständig und freigegeben, inkl. Kundennutzen und „Das kannst du jetzt erzählen“
- [ ] Orts-Schild und Siemens-Landkarten-Eintrag vorhanden
- [ ] Optional: „Blick unter die Haube“ zeigt Modellstruktur und Konvergenz
- [ ] DE/EN-Texte, farbfehlsichtig-taugliche Darstellung, kein Zeitdruck-Zwang
- [ ] Solve < 50 ms im WebGL-Build, keine GC-Spikes im Solver
- [ ] Playtest mit mindestens 3 Personen ohne technischen Hintergrund
