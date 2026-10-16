using System.Collections;
using System.Collections.Generic;
using SFE.SimCore.Data;
using SFE.SimCore.Thermal;
using UnityEngine;
using UnityEngine.UI;

namespace SFE.Game
{
    /// <summary>
    /// MG1 Teil B „Die erste Siedlung“ mit den didaktischen Schritten 1–5 (Schritt 6 = Transfer-Screen).
    /// Entscheidungen für den Prototyp (docs/06): E-01 6 Karten, 4 richtig · E-02 antippen + Waagschale antippen ·
    /// E-03 4 Fäden von Hand · E-04 jeder Baustein belegt 1 Feld · E-05 leeres Feld · E-06 nur Laternen kosten Strom,
    /// max. 16 Iglus und 12 Schneewälle · E-07 Werkzeugleiste, Klicken/Ziehen, Rückgängig · E-08 „Simulieren“ von Hand ·
    /// E-09/E-10 3 gewertete Runden, danach Lösungshilfe (Beispiel-Siedlung laden) und beliebig viele Extra-Runden ·
    /// E-11 nach jeder Runde steht wieder der geplante Aufbau da („im Computer schmilzt nichts wirklich“) ·
    /// E-12 Windpfeil am Feldrand · F-02 Sterne = beste Runde.
    /// </summary>
    public sealed class SettlementMinigame : MonoBehaviour
    {
        private enum Tool { Igloo, Lantern, Wall, Erase }

        private GameRoot _root;
        private SettlementLevel _level;
        private SettlementMaterialSet _mats;
        private SettlementBoard _board;
        private SettlementGrid _grid;
        private readonly Stack<string[]> _undo = new Stack<string[]>();
        private Tool _tool = Tool.Igloo;
        private bool _editing;
        private int _lastPainted = -1;
        private bool _painting;
        private bool _paintRemoves;

        private GameObject _ui;
        private readonly Dictionary<Tool, Button> _toolButtons = new Dictionary<Tool, Button>();
        private Text _roundLabel;
        private Button _simulateButton;
        private GameObject _techPanel;
        private Text _techText;
        private bool _techVisible;

        private int _round;
        private int _bestStars = -1;
        private bool _successShown;
        private bool _solutionOffered;
        private bool _finished;

        public static SettlementMinigame Create(GameRoot root, Vector3 origin)
        {
            var go = new GameObject("MG1B_Settlement");
            go.transform.position = origin;
            var m = go.AddComponent<SettlementMinigame>();
            m._root = root;
            m._level = root.Data.Settlement;
            m._mats = root.Data.Materials;
            return m;
        }

        private Hud Hud => _root.Hud;
        private Camera Cam => _root.Rig.Cam;

        public IEnumerator Play()
        {
            _board = SettlementBoard.Create(transform, transform.position, _level);
            var side = new Vector3(-2.2f, 0, _level.Height / 2f);
            var frieda = Prims.Penguin(transform, "Frieda", Prims.Look.Frieda, 1.3f);
            frieda.transform.position = transform.position + side + new Vector3(0, 0, 1.2f);
            frieda.transform.rotation = Quaternion.Euler(0, 90, 0);
            var kalle = Prims.Penguin(transform, "Kalle", Prims.Look.Kalle, 1.3f);
            kalle.transform.position = transform.position + side + new Vector3(0, 0, -1.2f);
            kalle.transform.rotation = Quaternion.Euler(0, 90, 0);

            BoardView(snap: true);
            yield return Hud.FadeIn();
            yield return Hud.Say("mg1_intro");

            // Kalles Versuch – „in echt“, ohne Wärmebild
            yield return Hud.Say("kalle_intro");
            var kalleGrid = SettlementGrid.FromRows(_level.KalleTiles);
            _board.Show(kalleGrid);
            yield return new WaitForSeconds(1.2f);
            var kalleRun = new SettlementSimulation(kalleGrid, _level, _mats);
            kalleRun.RunToEnd();
            foreach (var pass in kalleRun.MeltPasses)
            {
                yield return _board.Melt(pass, 1.4f);
                yield return new WaitForSeconds(0.4f);
            }
            yield return Hud.Say("kalle_after");

            yield return PhaseCards();
            yield return PhaseScale();
            yield return PhaseThreads();
            yield return PhaseSimulateKalle();
            yield return PhaseBuild();
            yield return Hud.FadeOut();
            Hud.ShowPhases(-1);
            if (_ui != null) Destroy(_ui);
            Destroy(gameObject);
        }

        private void BoardView(bool snap = false) =>
            _root.Rig.LookAt(_board.Center + new Vector3(0, 0, -1.5f), 64f, _root.Rig.DistanceToFit(_level.Width + 6f), 0f, snap);

        private void AddGlossary(string phase)
        {
            foreach (var g in _root.Data.Mg1Content.Arr("glossary"))
            {
                var o = (Dictionary<string, object>)g;
                if (o.Str("phase") != phase) continue;
                string entry = $"{o.Str("term")}: {o.Str("text")}";
                if (!_root.State.Glossary.Contains(entry)) _root.State.Glossary.Add(entry);
                Hud.Toast($"Neu im Notizbuch – {entry}", 4f);
            }
        }

        private RectTransform NewUiLayer(string name)
        {
            if (_ui != null) Destroy(_ui);
            _ui = new GameObject(name);
            var rt = UIKit.Rect(_ui, Hud.Root);
            UIKit.Fill(rt);
            rt.SetSiblingIndex(0);
            return rt;
        }

        // ---------- Schritt 1: Was ist wichtig? ----------

        private IEnumerator PhaseCards()
        {
            Hud.ShowPhases(0);
            yield return Hud.Say("phase1_intro");
            var layer = NewUiLayer("Cards");
            var panel = UIKit.Panel(layer, Palette.Panel, "CardPanel");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1200, 560));
            var title = UIKit.Label(panel.transform, "Was bestimmt, wie warm ein Iglu wird?", 36, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 20, -80, 20, 10);

            var cards = _root.Data.Mg1Content.Arr("cards");
            var selected = new bool[cards.Count];
            var images = new Image[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                var c = (Dictionary<string, object>)cards[i];
                int idx = i;
                var b = UIKit.Button(panel.transform, c.Str("text"), null, Palette.ButtonAlt, 30);
                images[i] = b.GetComponent<Image>();
                b.onClick.AddListener(() =>
                {
                    selected[idx] = !selected[idx];
                    images[idx].color = selected[idx] ? Palette.Button : Palette.ButtonAlt;
                    var lbl = b.GetComponentInChildren<Text>();
                    lbl.text = (selected[idx] ? "[x] " : "") + c.Str("text");
                });
                int col = i % 3, row = i / 3;
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(-370 + col * 370, 70 - row * 170), new Vector2(340, 140));
            }

            bool done = false;
            var finish = UIKit.Button(panel.transform, "Fertig", () =>
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    var c = (Dictionary<string, object>)cards[i];
                    if (selected[i] && !(bool)c["correct"]) { Hud.Toast(c.Str("feedback"), 4.5f); return; }
                }
                for (int i = 0; i < cards.Count; i++)
                {
                    var c = (Dictionary<string, object>)cards[i];
                    if (!selected[i] && (bool)c["correct"]) { Hud.Toast(_root.Data.Content("cardsMissing"), 4.5f); return; }
                }
                done = true;
            });
            UIKit.PlaceFixed((RectTransform)finish.transform, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(300, 80));
            while (!done) yield return null;
            Destroy(_ui);
            AddGlossary("modelling");
            yield return Hud.Say("phase1_done");
        }

        // ---------- Schritt 2: Welche Regel gilt? ----------

        private IEnumerator PhaseScale()
        {
            Hud.ShowPhases(1);
            yield return Hud.Say("phase2_intro");
            var layer = NewUiLayer("Scale");
            var panel = UIKit.Panel(layer, Palette.Panel, "ScalePanel");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1300, 620));
            var title = UIKit.Label(panel.transform, "Ein Iglu als Waage", 36, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 20, -70, 20, 10);

            // Waagbalken mit zwei Schalen
            var pivot = new GameObject("Beam");
            var beamRt = UIKit.Rect(pivot, panel.transform);
            UIKit.PlaceFixed(beamRt, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(900, 16));
            pivot.AddComponent<Image>().color = new Color(0.8f, 0.85f, 0.95f);
            var post = UIKit.Panel(panel.transform, new Color(0.8f, 0.85f, 0.95f), "Post");
            UIKit.PlaceFixed(post.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(24, 220));

            var pans = new Button[2];
            var panLists = new RectTransform[2];
            string[] panTitles = { "Wärme kommt rein", "Wärme geht raus" };
            for (int s = 0; s < 2; s++)
            {
                int side = s;
                var pan = UIKit.Button(beamRt, panTitles[s], null, new Color(0.25f, 0.32f, 0.45f), 28);
                var prt = (RectTransform)pan.transform;
                prt.anchorMin = prt.anchorMax = new Vector2(s == 0 ? 0.15f : 0.85f, 0.5f);
                prt.pivot = new Vector2(0.5f, 1f);
                prt.anchoredPosition = new Vector2(0, -20);
                prt.sizeDelta = new Vector2(380, 230);
                var lbl = pan.GetComponentInChildren<Text>();
                lbl.alignment = TextAnchor.UpperCenter;
                pans[s] = pan;
                var list = new GameObject("List");
                panLists[s] = UIKit.Rect(list, prt);
                UIKit.Place(panLists[s], 0, 0, 1, 1, 10, 10, 10, 50);
                var v = list.AddComponent<VerticalLayoutGroup>();
                v.spacing = 6;
                v.childControlHeight = true;
                v.childControlWidth = true;
                v.childForceExpandHeight = false;
            }

            var tokens = _root.Data.Mg1Content.Arr("scaleTokens");
            int selectedToken = -1;
            int placed = 0;
            var tokenButtons = new Button[tokens.Count];
            var placedFlags = new bool[tokens.Count];
            for (int i = 0; i < tokens.Count; i++)
            {
                var t = (Dictionary<string, object>)tokens[i];
                int idx = i;
                var b = UIKit.Button(panel.transform, t.Str("text"), null, Palette.ButtonAlt, 26);
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(-450 + i * 300, -140), new Vector2(280, 80));
                b.onClick.AddListener(() =>
                {
                    selectedToken = idx;
                    for (int k = 0; k < tokenButtons.Length; k++)
                        if (!placedFlags[k])
                            tokenButtons[k].GetComponent<Image>().color = k == idx ? Palette.Selected : Palette.ButtonAlt;
                });
                tokenButtons[i] = b;
            }
            for (int s = 0; s < 2; s++)
            {
                int side = s;
                pans[s].onClick.AddListener(() =>
                {
                    if (selectedToken < 0) { Hud.Toast("Tipp erst ein Kärtchen an, dann die Waagschale.", 3f); return; }
                    var t = (Dictionary<string, object>)tokens[selectedToken];
                    bool correct = (t.Str("side") == "in") == (side == 0);
                    if (!correct) { Hud.Toast(_root.Data.Content("scaleWrong"), 3.5f); return; }
                    var b = tokenButtons[selectedToken];
                    b.onClick.RemoveAllListeners();
                    placedFlags[selectedToken] = true;
                    b.GetComponent<Image>().color = side == 0 ? HeatColors.Comfy : HeatColors.Cold;
                    b.transform.SetParent(panLists[side], false);
                    var le = b.gameObject.AddComponent<LayoutElement>();
                    le.preferredHeight = 60;
                    selectedToken = -1;
                    placed++;
                });
            }
            while (placed < tokens.Count) yield return null;

            // Waage kippt leicht nach „warm“ – Gleichgewicht ist das Ziel
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                beamRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 6f) * 6f * (1.2f - t));
                yield return null;
            }
            beamRt.localRotation = Quaternion.identity;
            AddGlossary("equation");
            yield return Hud.Say("phase2_done");
            Destroy(_ui);
        }

        // ---------- Schritt 3: Alles hängt zusammen ----------

        private IEnumerator PhaseThreads()
        {
            Hud.ShowPhases(2);
            _board.Clear();
            int cx = _level.Width / 2, cy = _level.Height / 2;
            int center = cy * _level.Width + cx;
            var demo = new SettlementGrid(_level.Width, _level.Height);
            demo[cx, cy] = Cell.Igloo;
            demo[cx, cy - 1] = Cell.Lantern;
            demo[cx, cy + 1] = Cell.Igloo;
            _board.Show(demo);
            _root.Rig.LookAt(_board.CellCenter(center), 60f, _root.Rig.DistanceToFit(6f));
            yield return Hud.Say("phase3_intro");

            var neighbours = new HashSet<int> { center - 1, center + 1, center - _level.Width, center + _level.Width };
            var done = new HashSet<int>();
            int first = -1;
            Hud.Toast(string.Format(_root.Data.Content("threadsCount"), 0, 4), 4f);
            while (done.Count < 4)
            {
                if (!Hud.DialogOpen && _board.CellAtPointer(Cam, out int cell))
                {
                    _board.ShowCursor(first >= 0 ? first : cell, Palette.Selected);
                    if (InputHelper.PointerDown && !InputHelper.PointerOverUI)
                    {
                        if (first < 0) first = cell;
                        else
                        {
                            int a = first, b = cell;
                            first = -1;
                            int other = a == center ? b : b == center ? a : -1;
                            if (other >= 0 && neighbours.Contains(other) && !done.Contains(other))
                            {
                                done.Add(other);
                                _board.AddHandThread(center, other);
                                Hud.Toast(string.Format(_root.Data.Content("threadsCount"), done.Count, 4), 3f);
                            }
                            else if (a != b) Hud.Toast(_root.Data.Content("threadsWrong"), 3.5f);
                        }
                    }
                }
                yield return null;
            }
            _board.ShowCursor(-1, Color.clear);
            yield return Hud.Say("phase3_computer");

            BoardView();
            yield return new WaitForSeconds(0.8f);
            var couplings = new List<(int, int)>();
            SettlementModel.Assemble(demo, _level, _mats, out _, (i, j, k) => couplings.Add((i, j)));
            yield return _board.DrawAllThreads(couplings, 2f,
                n => Hud.Toast(string.Format(_root.Data.Content("computerThreads"), n.ToString("N0")), 1f));
            yield return new WaitForSeconds(0.6f);
            AddGlossary("assembly");
            yield return Hud.Say("phase3_done");
            _board.ClearThreads();
        }

        // ---------- Schritt 4: Der Computer probiert's aus ----------

        private IEnumerator PhaseSimulateKalle()
        {
            Hud.ShowPhases(3);
            var kalleGrid = SettlementGrid.FromRows(_level.KalleTiles);
            _board.Clear();
            _board.Show(kalleGrid);
            yield return Hud.Say("phase4_intro");

            var layer = NewUiLayer("SimulateKalle");
            bool go = false;
            var b = UIKit.Button(layer, "Simulieren", () => go = true, Palette.Button, 36);
            UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(360, 100));
            while (!go) yield return null;
            b.gameObject.SetActive(false);

            yield return Animate(kalleGrid);
            AddGlossary("solution");
            yield return Hud.Say("phase4_done");
            Destroy(_ui);
        }

        /// <summary>Zeigt die Rechnung Schritt für Schritt: Wärmebild baut sich auf, dann schmelzen Iglus.</summary>
        private IEnumerator Animate(SettlementGrid grid, System.Action<SettlementSimulation> onFinished = null)
        {
            var sim = new SettlementSimulation(grid, _level, _mats);
            var pending = new Queue<int[]>();
            sim.Melted += cells => pending.Enqueue(cells);
            while (!sim.IsConverged)
            {
                sim.Step(2);
                _board.SetHeat(sim.Temperatures);
                UpdateTech(sim);
                while (pending.Count > 0)
                {
                    _board.SetHeat(sim.Temperatures);
                    yield return _board.Melt(pending.Dequeue());
                }
                yield return null;
            }
            _board.SetHeat(sim.Temperatures);
            var r = sim.Result();
            _board.SetIglooStates(r.IglooStates);
            UpdateTech(sim);
            onFinished?.Invoke(sim);
        }

        // ---------- Schritt 5: Besser machen ----------

        private IEnumerator PhaseBuild()
        {
            Hud.ShowPhases(4);
            _board.Clear();
            _board.SetHeat(null);
            _grid = SettlementGrid.FromRows(_level.InitialTiles);
            _board.Show(_grid);
            BoardView();
            yield return Hud.Say("phase5_intro");
            BuildToolbar();
            _editing = true;

            while (!_finished)
            {
                if (_editing) HandlePainting();
                yield return null;
            }
            AddGlossary("optimization");
            yield return Hud.Say("phase5_done");
        }

        private void BuildToolbar()
        {
            var layer = NewUiLayer("BuildUI");
            var bar = UIKit.Panel(layer, Palette.Panel, "Toolbar");
            UIKit.Place(bar.rectTransform, 0, 0, 1, 0, 0, 0, 0, -130);
            var tools = new[] { (Tool.Igloo, "Iglu"), (Tool.Lantern, "Laterne"), (Tool.Wall, "Schneewall"), (Tool.Erase, "Radierer") };
            for (int i = 0; i < tools.Length; i++)
            {
                var (tool, label) = tools[i];
                var b = UIKit.Button(bar.transform, label, () => SelectTool(tool), Palette.ButtonAlt, 26);
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0, 0.5f), new Vector2(150 + i * 250, 0), new Vector2(230, 96));
                _toolButtons[tool] = b;
            }
            var undo = UIKit.Button(bar.transform, "Rückgängig", Undo, new Color(0.4f, 0.4f, 0.5f), 24);
            UIKit.PlaceFixed((RectTransform)undo.transform, new Vector2(0, 0.5f), new Vector2(1150, 0), new Vector2(200, 80));
            _simulateButton = UIKit.Button(bar.transform, "Simulieren", () => StartCoroutine(SimulateRound()), Palette.Button, 34);
            UIKit.PlaceFixed((RectTransform)_simulateButton.transform, new Vector2(1, 0.5f), new Vector2(-190, 0), new Vector2(330, 100));
            _roundLabel = UIKit.Label(bar.transform, "", 26, Palette.TextLight);
            UIKit.PlaceFixed(_roundLabel.rectTransform, new Vector2(1, 0.5f), new Vector2(-470, 0), new Vector2(220, 90));

            // Legende
            var legend = UIKit.Panel(layer, new Color(0.1f, 0.14f, 0.22f, 0.8f), "Legend");
            UIKit.PlaceFixed(legend.rectTransform, new Vector2(0, 1), new Vector2(170, -170), new Vector2(300, 170));
            var lg = _root.Data.Mg1Content.Obj("legend");
            AddLegendRow(legend.transform, 0, HeatColors.Cold, lg.Str("cold"));
            AddLegendRow(legend.transform, 1, HeatColors.Comfy, lg.Str("comfy"));
            AddLegendRow(legend.transform, 2, HeatColors.Melt, lg.Str("melt"));

            // Blick unter die Haube (optional, Frage B-06)
            var techBtn = UIKit.Button(layer, "Blick unter die Haube", () => { _techVisible = !_techVisible; _techPanel.SetActive(_techVisible); },
                new Color(0.25f, 0.3f, 0.4f), 22);
            UIKit.PlaceFixed((RectTransform)techBtn.transform, new Vector2(1, 1), new Vector2(-170, -130), new Vector2(300, 56));
            _techPanel = UIKit.Panel(layer, new Color(0.03f, 0.05f, 0.1f, 0.9f), "Tech").gameObject;
            UIKit.PlaceFixed((RectTransform)_techPanel.transform, new Vector2(1, 1), new Vector2(-300, -330), new Vector2(560, 330));
            _techText = UIKit.Label(_techPanel.transform, "Noch nicht simuliert.", 22, new Color(0.7f, 0.95f, 1f), TextAnchor.UpperLeft);
            UIKit.Place(_techText.rectTransform, 0, 0, 1, 1, 16, 12, 16, 12);
            _techPanel.SetActive(false);

            SelectTool(Tool.Igloo);
            UpdateToolbar();
        }

        private static void AddLegendRow(Transform parent, int row, Color color, string text)
        {
            var sw = UIKit.Panel(parent, color, "Swatch");
            UIKit.PlaceFixed(sw.rectTransform, new Vector2(0, 1), new Vector2(40, -35 - row * 50), new Vector2(40, 36));
            var t = UIKit.Label(parent, text, 26, Palette.TextLight, TextAnchor.MiddleLeft);
            UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(170, -35 - row * 50), new Vector2(200, 40));
        }

        private void SelectTool(Tool t)
        {
            _tool = t;
            foreach (var kv in _toolButtons) kv.Value.GetComponent<Image>().color = kv.Key == t ? Palette.Button : Palette.ButtonAlt;
        }

        private void UpdateToolbar()
        {
            UIKit.SetButtonLabel(_toolButtons[Tool.Igloo], $"Iglu  {_grid.Count(Cell.Igloo)}/{_level.IglooLimit}");
            UIKit.SetButtonLabel(_toolButtons[Tool.Lantern], $"Laterne  {_grid.Count(Cell.Lantern)}/{_level.MaxLanterns}");
            UIKit.SetButtonLabel(_toolButtons[Tool.Wall], $"Schneewall  {_grid.Count(Cell.Wall)}/{_level.WallLimit}");
            int shownRound = _round + 1;
            _roundLabel.text = shownRound <= _level.Rounds ? $"Runde {shownRound} von {_level.Rounds}" : $"Extra-Runde {shownRound}";
        }

        private void HandlePainting()
        {
            if (Hud.DialogOpen) return;
            bool onBoard = _board.CellAtPointer(Cam, out int cell) && !InputHelper.PointerOverUI;
            _board.ShowCursor(onBoard ? cell : -1, _tool == Tool.Erase ? Palette.Bad : Palette.Selected);
            if (InputHelper.PointerUp) { _painting = false; _lastPainted = -1; }
            if (!onBoard) return;
            if (InputHelper.PointerDown)
            {
                _painting = true;
                _undo.Push(_grid.ToRows());
                // Klick auf ein Feld mit demselben Baustein entfernt ihn (Ziehen entfernt dann weiter)
                _paintRemoves = _tool == Tool.Erase || _grid[cell] == SymbolFor(_tool);
            }
            if (_painting && InputHelper.PointerHeld && cell != _lastPainted)
            {
                _lastPainted = cell;
                Apply(cell);
            }
        }

        private static char SymbolFor(Tool t) =>
            t == Tool.Igloo ? Cell.Igloo : t == Tool.Lantern ? Cell.Lantern : t == Tool.Wall ? Cell.Wall : Cell.Ice;

        private void Apply(int cell)
        {
            char target = _paintRemoves ? Cell.Ice : SymbolFor(_tool);
            if (_grid[cell] == target) return;
            if (target == Cell.Igloo && _grid.Count(Cell.Igloo) >= _level.IglooLimit)
            { Hud.Toast($"Die Kolonie braucht {_level.IglooLimit} Iglus – mehr Familien gibt es nicht.", 3f); return; }
            if (target == Cell.Lantern && _grid.Count(Cell.Lantern) >= _level.MaxLanterns)
            { Hud.Toast($"Der Strom reicht nur für {_level.MaxLanterns} Laternen.", 3f); return; }
            if (target == Cell.Wall && _grid.Count(Cell.Wall) >= _level.WallLimit)
            { Hud.Toast($"Mehr als {_level.WallLimit} Schneewälle schaffen wir heute nicht.", 3f); return; }
            _grid[cell] = target;
            _board.Show(_grid);
            UpdateToolbar();
        }

        private void Undo()
        {
            if (_undo.Count == 0 || !_editing) return;
            _grid = SettlementGrid.FromRows(_undo.Pop());
            _board.Show(_grid);
            UpdateToolbar();
        }

        private IEnumerator SimulateRound()
        {
            if (!_editing) yield break;
            if (_grid.Count(Cell.Igloo) == 0) { Hud.Toast("Bau zuerst ein paar Iglus.", 3f); yield break; }
            _editing = false;
            _simulateButton.interactable = false;
            _board.ShowCursor(-1, Color.clear);
            var planned = _grid.ToRows();

            SettlementSimulation finished = null;
            yield return Animate(_grid, s => finished = s);
            var r = finished.Result();
            _round++;

            bool improved = r.Stars > _bestStars ||
                            (r.Stars == _bestStars && _root.State.Mg1BestResult != null && r.Comfortable > _root.State.Mg1BestResult.Comfortable);
            if (_bestStars < 0 || improved)
            {
                _bestStars = r.Stars;
                _root.State.Mg1BestStars = r.Stars;
                _root.State.Mg1BestLayout = planned;
                _root.State.Mg1BestResult = r;
                _root.State.Mg1BestTemperatures = (double[])finished.Temperatures.Clone();
                var tex = _board.GroundTexture;
                var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                copy.SetPixels(tex.GetPixels());
                copy.Apply();
                _root.State.Mg1Snapshot = copy;
            }

            bool closed = false;
            bool next = false;
            ShowResult(r, () => { closed = true; next = true; }, () => { closed = true; _finished = true; });
            yield return new WaitForSeconds(0.3f);

            if (r.Stars >= 1 && !_successShown)
            {
                _successShown = true;
                yield return Hud.Say(r.Stars == 3 ? "phase5_three_stars" : "phase5_success");
            }
            else if (_bestStars == 0 && _round == 2)
            {
                yield return Hud.Say(r.Melted > 0 ? "phase5_hint_melt" : r.Walls == 0 ? "phase5_hint_wind" : "phase5_hint_cold");
            }
            else if (_bestStars == 0 && _round >= _level.Rounds && !_solutionOffered)
            {
                _solutionOffered = true;
                yield return Hud.Say("phase5_solution");
            }
            while (!closed) yield return null;
            if (_finished) yield break;

            // Zurück zum geplanten Aufbau – im Computer ist nichts wirklich geschmolzen.
            _board.Clear();
            _board.SetHeat(null);
            if (next && _solutionOffered && _bestStars == 0)
            {
                _undo.Push(planned);
                _grid = SettlementGrid.FromRows(_level.HintTiles);
                Hud.Toast("Beispiel-Siedlung geladen. Mit „Rückgängig“ kommst du zu deiner zurück.", 4.5f);
            }
            else _grid = SettlementGrid.FromRows(planned);
            _board.Show(_grid);
            UpdateToolbar();
            _simulateButton.interactable = true;
            _editing = true;
        }

        private void ShowResult(SettlementResult r, System.Action onNext, System.Action onFinish)
        {
            var panel = UIKit.Panel(_ui.transform, Palette.PanelLight, "Result");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(1, 0.5f), new Vector2(-330, 60), new Vector2(600, 600));
            var title = UIKit.Label(panel.transform, $"Ergebnis Runde {_round}", 34, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 10, -70, 10, 10);

            var lights = _root.Data.Mg1Content.Obj("lights");
            int target = _level.IglooTarget;
            AddLight(panel.transform, 0, r.Comfortable >= target ? 2 : r.Comfortable >= target - 3 ? 1 : 0,
                $"{lights.Str("comfy")}: {r.Comfortable} von {target}");
            AddLight(panel.transform, 1, r.Melted == 0 ? 2 : r.Melted <= 2 ? 1 : 0,
                $"{lights.Str("melt")}: {(r.Melted == 0 ? "ja" : r.Melted + " geschmolzen")}");
            AddLight(panel.transform, 2, r.Cost <= 0.8 * _level.Budget ? 2 : r.Cost <= _level.Budget ? 1 : 0,
                $"{lights.Str("budget")}: {r.Lanterns} von {_level.MaxLanterns} Laternen");

            for (int s = 0; s < 3; s++)
            {
                var star = UIKit.Panel(panel.transform, s < r.Stars ? Palette.Selected : new Color(0.75f, 0.78f, 0.85f), "Star");
                UIKit.PlaceFixed(star.rectTransform, new Vector2(0.5f, 0), new Vector2(-90 + s * 90, 210), new Vector2(54, 54));
                star.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            var starText = UIKit.Label(panel.transform, StarText(r.Stars), 24, Palette.Text);
            UIKit.PlaceFixed(starText.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 160), new Vector2(560, 40));

            bool canFinish = _bestStars >= 1 || _round >= _level.Rounds;
            var nextB = UIKit.Button(panel.transform, "Weiter bauen", () => { Destroy(panel.gameObject); onNext(); }, Palette.ButtonAlt, 26);
            UIKit.PlaceFixed((RectTransform)nextB.transform, new Vector2(0.5f, 0), new Vector2(canFinish ? -140 : 0, 70), new Vector2(260, 80));
            if (canFinish)
            {
                var fin = UIKit.Button(panel.transform, "Fertig", () => { Destroy(panel.gameObject); onFinish(); }, Palette.Button, 26);
                UIKit.PlaceFixed((RectTransform)fin.transform, new Vector2(0.5f, 0), new Vector2(140, 70), new Vector2(260, 80));
            }
        }

        private static string StarText(int stars)
        {
            switch (stars)
            {
                case 3: return "3 Sterne: gemütlich, nichts geschmolzen, sparsam geplant";
                case 2: return "2 Sterne: gemütlich und nichts geschmolzen";
                case 1: return "1 Stern: Ziel erreicht – aber etwas ist geschmolzen";
                default: return "Noch kein Stern – probier es noch einmal";
            }
        }

        /// <summary>Ampel mit Farbe UND Zeichen (OK / ! / X), damit sie nicht nur über Farbe lesbar ist.</summary>
        private static void AddLight(Transform parent, int row, int level, string text)
        {
            var color = level == 2 ? Palette.Good : level == 1 ? Palette.Warn : Palette.Bad;
            var lamp = UIKit.Panel(parent, color, "Lamp");
            UIKit.PlaceFixed(lamp.rectTransform, new Vector2(0, 1), new Vector2(70, -130 - row * 85), new Vector2(64, 64));
            var sym = UIKit.Label(lamp.transform, level == 2 ? "OK" : level == 1 ? "!" : "X", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(sym.rectTransform);
            var t = UIKit.Label(parent, text, 28, Palette.Text, TextAnchor.MiddleLeft);
            UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(330, -130 - row * 85), new Vector2(440, 70));
        }

        private void UpdateTech(SettlementSimulation sim)
        {
            if (_techText == null || !_techVisible) return;
            _techText.text =
                "Blick unter die Haube\n" +
                $"Felder (Unbekannte): {_level.Width * _level.Height}\n" +
                $"Kopplungen zwischen Nachbarn: {2 * _level.Width * _level.Height - _level.Width - _level.Height}\n" +
                $"Gleichung je Feld: 0 = q - h (T - T_Luft) + Summe k (T_Nachbar - T)\n" +
                $"Löser: konjugierte Gradienten, Iteration {sim.Iteration}\n" +
                $"Restfehler: {sim.Residual:0.0e+0}\n" +
                $"Lösungen: {sim.SolveCount}, Schmelz-Durchläufe: {sim.MeltPasses.Count}";
        }
    }
}
