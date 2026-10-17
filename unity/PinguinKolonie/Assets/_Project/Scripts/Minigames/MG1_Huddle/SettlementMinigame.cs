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
    /// E-09/E-10 3 gewertete Runden, danach Lösungshilfe (Beispiel-Siedlung per Knopf) und beliebig viele Extra-Runden ·
    /// E-11 nach jeder Runde steht wieder der geplante Aufbau da („im Computer schmilzt nichts wirklich“) ·
    /// E-12 Windpfeil am Feldrand · F-02 Sterne = beste Runde, auch über „nochmal planen“ hinweg.
    /// v0.2: ZWILLI als sichtbarer Digitaler Zwilling, „Was wäre, wenn …“ ohne Wind (Schritt 4),
    /// Waage je Iglu nach jeder Runde (Schritt 5), Rundenvergleich mit Klara, Geräusche.
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
        private string[] _beforeStroke;

        private GameObject _ui;
        private readonly Dictionary<Tool, Button> _toolButtons = new Dictionary<Tool, Button>();
        private Text _roundLabel;
        private Button _simulateButton;
        private Button _exampleButton;
        private GameObject _techPanel;
        private Text _techText;
        private bool _techVisible;
        private Hologram _zwilli;

        private int _round;
        /// <summary>Beste Sterne dieses Durchlaufs – steuert Friedas Tipps. Die beste Runde über alle Durchläufe liegt im GameState.</summary>
        private int _runBest = -1;
        private bool _successShown;
        private bool _solutionOffered;
        private bool _balanceHintShown;
        private bool _finished;
        private SettlementResult _previous;

        // Ergebnis der letzten Runde für die Waage (Iglu antippen, solange das Ergebnis offen ist)
        private bool _resultOpen;
        private SettlementSimulation _lastSim;
        private SettlementBalance _lastBalance;
        private double _balanceScale;
        private bool _oneStarShown;
        private string[] _lastPlanned;
        private GameObject _balancePanel;

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
        private GameData Data => _root.Data;
        private Camera Cam => _root.Rig.Cam;

        public IEnumerator Play()
        {
            _board = SettlementBoard.Create(transform, transform.position, _level, Data.Ui("signWind"));
            var side = new Vector3(-2.2f, 0, _level.Height / 2f);
            Stand(Prims.Penguin(transform, "Frieda", Prims.Look.Frieda, 1.3f), side + new Vector3(0, 0, 1.2f));
            Stand(Prims.Penguin(transform, "Kalle", Prims.Look.Kalle, 1.3f), side + new Vector3(0, 0, -1.2f));
            Stand(Prims.Penguin(transform, "Theo", Prims.Look.Theo, 1.2f), side + new Vector3(0, 0, -3.4f));
            Stand(Prims.Penguin(transform, "Klara", Prims.Look.Klara, 1.2f), side + new Vector3(0, 0, -5.4f));
            var zw = Hologram.Create(transform, 1.2f);
            zw.transform.position = transform.position + side + new Vector3(0, 0, 3.4f);
            zw.transform.rotation = Quaternion.Euler(0, 90, 0);
            _zwilli = zw.GetComponent<Hologram>();
            SetGlitch(0.8f);

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
                _root.State.Puddles += pass.Length;
                yield return _board.Melt(pass, 1.4f);
                yield return new WaitForSeconds(0.4f);
            }
            yield return Hud.Say("kalle_after");

            yield return PhaseCards();
            yield return PhaseScale();
            yield return PhaseThreads();
            yield return PhaseSimulateKalle();
            yield return PhaseWhatIf();
            yield return PhaseBuild();
            yield return Hud.FadeOut();
            Hud.ShowPhases(-1);
            if (_ui != null) Destroy(_ui);
            Destroy(gameObject);
        }

        private void Stand(GameObject penguin, Vector3 offset)
        {
            penguin.transform.position = transform.position + offset;
            penguin.transform.rotation = Quaternion.Euler(0, 90, 0);
        }

        /// <summary>ZWILLI stottert weniger, je weiter der Spieler kommt – Bild und Stimme gemeinsam.</summary>
        private void SetGlitch(float g)
        {
            Hud.ZwilliGlitch = g;
            if (_zwilli != null) _zwilli.Glitch = g;
        }

        private void BoardView(bool snap = false) =>
            _root.Rig.LookAt(_board.Center + new Vector3(0, 0, -1.5f), 64f, _root.Rig.DistanceToFit(_level.Width + 6f), 0f, snap);

        private void AddGlossary(string phase)
        {
            foreach (var g in Data.Mg1Content.Arr("glossary"))
            {
                var o = (Dictionary<string, object>)g;
                if (o.Str("phase") != phase) continue;
                string entry = $"{o.Str("term")}: {o.Str("text")}";
                if (_root.State.Glossary.Contains(entry)) continue; // bei „nochmal planen“ nicht erneut als „neu“ melden
                _root.State.Glossary.Add(entry);
                Hud.Toast(Data.Ui("glossaryNew", entry), 4f);
                Sfx.Play(Sfx.Sound.Ding, 0.4f);
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
            var title = UIKit.Label(panel.transform, Data.Ui("cardsTitle"), 36, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 20, -80, 20, 10);

            var cards = Data.Mg1Content.Arr("cards");
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
                    UIKit.SetButtonLabel(b, selected[idx] ? Data.Ui("cardsChecked", c.Str("text")) : c.Str("text"));
                    Sfx.Play(Sfx.Sound.Click);
                });
                int col = i % 3, row = i / 3;
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(-370 + col * 370, 70 - row * 170), new Vector2(340, 140));
            }

            bool done = false;
            var finish = UIKit.Button(panel.transform, Data.Ui("finish"), () =>
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    var c = (Dictionary<string, object>)cards[i];
                    if (selected[i] && !(bool)c["correct"]) { Sfx.Play(Sfx.Sound.Wrong); Hud.Toast(c.Str("feedback"), 4.5f); return; }
                }
                for (int i = 0; i < cards.Count; i++)
                {
                    var c = (Dictionary<string, object>)cards[i];
                    if (!selected[i] && (bool)c["correct"]) { Sfx.Play(Sfx.Sound.Wrong); Hud.Toast(Data.Content("cardsMissing"), 4.5f); return; }
                }
                done = true;
            });
            UIKit.PlaceFixed((RectTransform)finish.transform, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(300, 80));
            while (!done) yield return null;
            Destroy(_ui);
            Sfx.Play(Sfx.Sound.Success, 0.5f);
            AddGlossary("modelling");
            yield return Hud.Say("phase1_done");
            if (_root.State.Glossary.Count == 1) yield return Hud.Say("notebook_first");
        }

        // ---------- Schritt 2: Welche Regel gilt? ----------

        private IEnumerator PhaseScale()
        {
            Hud.ShowPhases(1);
            yield return Hud.Say("phase2_intro");
            var layer = NewUiLayer("Scale");
            var panel = UIKit.Panel(layer, Palette.Panel, "ScalePanel");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1300, 620));
            var title = UIKit.Label(panel.transform, Data.Ui("scaleTitle"), 36, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            string[] panTitles = { Data.Ui("scaleIn"), Data.Ui("scaleOut") };
            for (int s = 0; s < 2; s++)
            {
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

            var tokens = Data.Mg1Content.Arr("scaleTokens");
            int selectedToken = -1;
            int placed = 0;
            int weightIn = 0, weightOut = 0;
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
                    Sfx.Play(Sfx.Sound.Click);
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
                    if (selectedToken < 0) { Hud.Toast(Data.Ui("scalePickFirst"), 3f); return; }
                    var t = (Dictionary<string, object>)tokens[selectedToken];
                    bool correct = (t.Str("side") == "in") == (side == 0);
                    if (!correct) { Sfx.Play(Sfx.Sound.Wrong); Hud.Toast(Data.Content("scaleWrong"), 3.5f); return; }
                    var b = tokenButtons[selectedToken];
                    b.onClick.RemoveAllListeners();
                    placedFlags[selectedToken] = true;
                    b.GetComponent<Image>().color = side == 0 ? HeatColors.Comfy : HeatColors.Cold;
                    b.transform.SetParent(panLists[side], false);
                    var le = b.gameObject.AddComponent<LayoutElement>();
                    le.preferredHeight = 60;
                    selectedToken = -1;
                    placed++;
                    if (side == 0) weightIn++; else weightOut++;
                    Sfx.Play(Sfx.Sound.Plop);
                });
            }
            // Die Waage neigt sich nach dem, was wirklich auf ihr liegt (v0.1 wackelte nur nach einem Zeitplan).
            // Fertig, wenn alles liegt und die Waage zur Ruhe gekommen ist – auch bei ungleich vielen Kärtchen.
            float shown = 0f, target = 0f;
            while (placed < tokens.Count || Mathf.Abs(shown - target) > 0.05f)
            {
                target = (weightOut - weightIn) * 7f;
                shown = Mathf.Lerp(shown, target, 6f * Time.deltaTime);
                beamRt.localRotation = Quaternion.Euler(0, 0, shown);
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
            SetGlitch(0.65f);
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
            Hud.Toast(string.Format(Data.Content("threadsCount"), 0, 4), 4f);
            while (done.Count < 4)
            {
                if (!Hud.Blocking && _board.CellAtPointer(Cam, out int cell))
                {
                    _board.ShowCursor(first >= 0 ? first : cell, Palette.Selected);
                    if (InputHelper.PointerDown && !InputHelper.PointerOverUI)
                    {
                        if (first < 0) { first = cell; Sfx.Play(Sfx.Sound.Click); }
                        else
                        {
                            int a = first, b = cell;
                            first = -1;
                            int other = a == center ? b : b == center ? a : -1;
                            if (other >= 0 && neighbours.Contains(other) && !done.Contains(other))
                            {
                                done.Add(other);
                                _board.AddHandThread(center, other);
                                Sfx.Play(Sfx.Sound.Plop);
                                Hud.Toast(string.Format(Data.Content("threadsCount"), done.Count, 4), 3f);
                            }
                            else if (a != b) { Sfx.Play(Sfx.Sound.Wrong); Hud.Toast(Data.Content("threadsWrong"), 3.5f); }
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
                n => Hud.Toast(string.Format(GameData.German, Data.Content("computerThreads"), n.ToString("N0", GameData.German)), 1f));
            yield return new WaitForSeconds(0.6f);
            AddGlossary("assembly");
            yield return Hud.Say("phase3_done");
            _board.ClearThreads();
        }

        // ---------- Schritt 4: Der Computer probiert's aus ----------

        private IEnumerator PhaseSimulateKalle()
        {
            Hud.ShowPhases(3);
            SetGlitch(0.5f);
            var kalleGrid = SettlementGrid.FromRows(_level.KalleTiles);
            _board.Clear();
            _board.Show(kalleGrid);
            yield return Hud.Say("phase4_intro");

            var layer = NewUiLayer("SimulateKalle");
            bool go = false;
            var b = UIKit.Button(layer, Data.Ui("simulate"), () => go = true, Palette.Button, 36);
            UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(360, 100));
            Hud.LockWhileBlocking(b);
            while (!go) yield return null;
            b.gameObject.SetActive(false);

            yield return Animate(kalleGrid, _level);
            AddGlossary("solution");
            yield return Hud.Say("phase4_done");
            Destroy(_ui);
        }

        /// <summary>
        /// „Was wäre, wenn wir den Wind weggelassen hätten?“ – dieselbe kleine Siedlung, zweimal gerechnet.
        /// Ohne Wind verspricht das Modell gemütliche Iglus, mit Wind frieren alle (Golden Files whatif_*).
        /// Macht die Wahl aus Schritt 1 folgenreich und zeigt Lernziel 5: Ein Modell ist nur so gut wie das, was drin ist.
        /// Eigene Siedlung statt der Beispiel-Siedlung, damit Schritt 5 nicht vorweggenommen wird.
        /// </summary>
        private IEnumerator PhaseWhatIf()
        {
            if (!_level.WhatIf.TryGetValue("noWind", out var whatIf)) yield break;
            SetGlitch(0.35f);
            var grid = SettlementGrid.FromRows(whatIf.Tiles);
            _board.Clear();
            _board.SetHeat(null);
            _board.Show(grid);
            yield return Hud.Say("whatif_intro");

            var layer = NewUiLayer("WhatIf");
            var banner = UIKit.Panel(layer, Palette.Panel, "Banner");
            UIKit.PlaceFixed(banner.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(900, 120));
            var text = UIKit.Label(banner.transform, Data.Ui("whatIfNoWind"), 32, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(text.rectTransform);

            SettlementSimulation noWind = null;
            yield return Animate(grid, _level.With(whatIf.Overrides), s => noWind = s);
            if (noWind != null)
            {
                var r = noWind.Result();
                text.text = Data.Ui("whatIfNoWind") + "\n" + Data.Ui("whatIfCosy", r.Comfortable, r.IgloosTotal);
            }
            yield return new WaitForSeconds(0.8f);
            yield return Hud.Say("whatif_nowind");

            _board.Clear();
            _board.SetHeat(null);
            _board.Show(grid);
            text.text = Data.Ui("whatIfReal");
            SettlementSimulation real = null;
            yield return Animate(grid, _level, s => real = s);
            if (real != null)
            {
                var r = real.Result();
                text.text = Data.Ui("whatIfReal") + "\n" + Data.Ui("whatIfCold", r.Cold, r.IgloosTotal);
                Sfx.Play(Sfx.Sound.Wrong, 0.5f);
            }
            yield return new WaitForSeconds(0.8f);
            yield return Hud.Say("whatif_done");
            AddGlossary("twin");
            Destroy(_ui);
        }

        /// <summary>
        /// Zeigt die Rechnung Schritt für Schritt: Wärmebild baut sich auf, dann schmelzen Iglus.
        /// Bleibt der Löser hängen, endet die Animation mit einem Hinweis statt mit einer Ausnahme –
        /// sonst blieb in v0.1 der „Simulieren“-Knopf für immer gesperrt.
        /// </summary>
        private IEnumerator Animate(SettlementGrid grid, SettlementLevel level, System.Action<SettlementSimulation> onFinished = null)
        {
            var sim = new SettlementSimulation(grid, level, _mats);
            var pending = new Queue<int[]>();
            sim.Melted += cells => pending.Enqueue(cells);
            while (!sim.IsConverged)
            {
                try { sim.Step(2); }
                catch (System.InvalidOperationException)
                {
                    Hud.Toast(Data.Ui("solverStuck"), 4f);
                    yield break;
                }
                _board.SetHeat(sim.Temperatures);
                UpdateTech(sim);
                while (pending.Count > 0)
                {
                    _board.SetHeat(sim.Temperatures);
                    var cells = pending.Dequeue();
                    _root.State.Puddles += cells.Length;
                    yield return _board.Melt(cells);
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
            SetGlitch(0.2f);
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
                else if (_resultOpen) HandleBalanceTap();
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
            Hud.LockWhileBlocking(bar); // ausgegraut, solange jemand spricht
            var tools = new[] { Tool.Igloo, Tool.Lantern, Tool.Wall, Tool.Erase };
            for (int i = 0; i < tools.Length; i++)
            {
                var tool = tools[i];
                var b = UIKit.Button(bar.transform, "", () => SelectTool(tool), Palette.ButtonAlt, 26);
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0, 0.5f), new Vector2(150 + i * 250, 0), new Vector2(230, 96));
                _toolButtons[tool] = b;
            }
            UIKit.SetButtonLabel(_toolButtons[Tool.Erase], Data.Ui("toolErase"));
            var undo = UIKit.Button(bar.transform, Data.Ui("undo"), Undo, new Color(0.4f, 0.4f, 0.5f), 24);
            UIKit.PlaceFixed((RectTransform)undo.transform, new Vector2(0, 0.5f), new Vector2(1150, 0), new Vector2(200, 80));
            _simulateButton = UIKit.Button(bar.transform, Data.Ui("simulate"), () => StartCoroutine(SimulateRound()), Palette.Button, 34);
            UIKit.PlaceFixed((RectTransform)_simulateButton.transform, new Vector2(1, 0.5f), new Vector2(-190, 0), new Vector2(330, 100));
            _roundLabel = UIKit.Label(bar.transform, "", 26, Palette.TextLight);
            UIKit.PlaceFixed(_roundLabel.rectTransform, new Vector2(1, 0.5f), new Vector2(-470, 0), new Vector2(220, 90));

            // Lösungshilfe erst nach Runde 3 ohne Stern – und nur auf Wunsch (UX-09: „kann geladen werden“).
            // Links unter der Legende: rechts liegt das Ergebnis, unten die Werkzeugleiste.
            _exampleButton = UIKit.Button(layer, Data.Ui("loadExample"), LoadExample, new Color(0.45f, 0.32f, 0.2f), 24);
            UIKit.PlaceFixed((RectTransform)_exampleButton.transform, new Vector2(0, 1), new Vector2(170, -300), new Vector2(300, 64));
            _exampleButton.gameObject.SetActive(false);

            // Legende
            var legend = UIKit.Panel(layer, new Color(0.1f, 0.14f, 0.22f, 0.8f), "Legend");
            UIKit.PlaceFixed(legend.rectTransform, new Vector2(0, 1), new Vector2(170, -170), new Vector2(300, 170));
            var lg = Data.Mg1Content.Obj("legend");
            AddLegendRow(legend.transform, 0, HeatColors.Cold, lg.Str("cold"));
            AddLegendRow(legend.transform, 1, HeatColors.Comfy, lg.Str("comfy"));
            AddLegendRow(legend.transform, 2, HeatColors.Melt, lg.Str("melt"));

            // Blick unter die Haube (optional, Frage B-06)
            var techBtn = UIKit.Button(layer, Data.Ui("techButton"), () => { _techVisible = !_techVisible; _techPanel.SetActive(_techVisible); },
                new Color(0.25f, 0.3f, 0.4f), 22);
            // Unter Notizbuch- und Ton-Knopf; der Kasten selbst links, damit ihn das Ergebnis rechts nicht verdeckt.
            UIKit.PlaceFixed((RectTransform)techBtn.transform, new Vector2(1, 1), new Vector2(-170, -240), new Vector2(300, 56));
            Hud.LockWhileBlocking(techBtn);
            _techPanel = UIKit.Panel(layer, new Color(0.03f, 0.05f, 0.1f, 0.9f), "Tech").gameObject;
            UIKit.PlaceFixed((RectTransform)_techPanel.transform, new Vector2(0, 1), new Vector2(300, -560), new Vector2(560, 340));
            var techInk = new Color(0.7f, 0.95f, 1f);
            _techText = UIKit.Label(_techPanel.transform, Data.Tech("techNotYet"), 22, techInk, TextAnchor.UpperLeft);
            UIKit.Place(_techText.rectTransform, 0, 0, 1, 1, 16, 90, 16, 12);
            // Die Gleichung je Feld wie LaTeX gesetzt, unter dem Text (die eingebaute Schrift kann keine Indizes)
            var equation = UIKit.Formula(_techPanel.transform, "settlement", techInk, 0.42f).rectTransform;
            equation.anchorMin = equation.anchorMax = new Vector2(0, 0);
            equation.anchoredPosition = new Vector2(20, 48);
            _techPanel.SetActive(false);
            Hud.TechOnly(techBtn.gameObject, () => { _techVisible = false; _techPanel.SetActive(false); });

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
            UIKit.SetButtonLabel(_toolButtons[Tool.Igloo], Data.Ui("toolIgloo", _grid.Count(Cell.Igloo), _level.IglooLimit));
            UIKit.SetButtonLabel(_toolButtons[Tool.Lantern], Data.Ui("toolLantern", _grid.Count(Cell.Lantern), _level.MaxLanterns));
            UIKit.SetButtonLabel(_toolButtons[Tool.Wall], Data.Ui("toolWall", _grid.Count(Cell.Wall), _level.WallLimit));
            int shownRound = _round + 1;
            _roundLabel.text = shownRound <= _level.Rounds ? Data.Ui("round", shownRound, _level.Rounds) : Data.Ui("extraRound", shownRound);
        }

        private void HandlePainting()
        {
            if (Hud.Blocking) return;
            bool onBoard = _board.CellAtPointer(Cam, out int cell) && !InputHelper.PointerOverUI;
            _board.ShowCursor(onBoard ? cell : -1, _tool == Tool.Erase ? Palette.Bad : Palette.Selected);
            if (InputHelper.PointerUp) EndStroke();
            if (!onBoard) return;
            if (InputHelper.PointerDown)
            {
                _painting = true;
                _beforeStroke = _grid.ToRows();
                // Klick auf ein Feld mit demselben Baustein entfernt ihn (Ziehen entfernt dann weiter)
                _paintRemoves = _tool == Tool.Erase || _grid[cell] == SymbolFor(_tool);
            }
            if (_painting && InputHelper.PointerHeld && cell != _lastPainted)
            {
                _lastPainted = cell;
                Apply(cell);
            }
        }

        /// <summary>
        /// Ein Rückgängig-Schritt je Strich – aber nur, wenn sich etwas geändert hat. In v0.1 legte jeder Klick
        /// einen Schritt an, auch ein abgelehnter (Grenze erreicht); „Rückgängig“ schien dann nichts zu tun.
        /// </summary>
        private void EndStroke()
        {
            if (_painting && _beforeStroke != null && SettlementRounds.Changed(_beforeStroke, _grid.ToRows())) _undo.Push(_beforeStroke);
            _painting = false;
            _lastPainted = -1;
            _beforeStroke = null;
        }

        private static char SymbolFor(Tool t) =>
            t == Tool.Igloo ? Cell.Igloo : t == Tool.Lantern ? Cell.Lantern : t == Tool.Wall ? Cell.Wall : Cell.Ice;

        private void Apply(int cell)
        {
            // Entfernen trifft nur den Baustein des gewählten Werkzeugs; der Radierer alles.
            // (v0.1: Klick auf ein Iglu und Weiterziehen löschte auch Laternen und Wälle.)
            if (_paintRemoves && _tool != Tool.Erase && _grid[cell] != SymbolFor(_tool)) return;
            char target = _paintRemoves ? Cell.Ice : SymbolFor(_tool);
            if (_grid[cell] == target) return;
            if (target == Cell.Igloo && _grid.Count(Cell.Igloo) >= _level.IglooLimit)
            { Hud.Toast(Data.Ui("limitIgloo", _level.IglooLimit, _level.IglooTarget), 3f); return; }
            if (target == Cell.Lantern && _grid.Count(Cell.Lantern) >= _level.MaxLanterns)
            { Hud.Toast(Data.Ui("limitLantern", _level.MaxLanterns), 3f); return; }
            if (target == Cell.Wall && _grid.Count(Cell.Wall) >= _level.WallLimit)
            { Hud.Toast(Data.Ui("limitWall", _level.WallLimit), 3f); return; }
            _grid[cell] = target;
            _board.Show(_grid);
            Sfx.Play(target == Cell.Ice ? Sfx.Sound.Erase : Sfx.Sound.Plop, 0.5f);
            UpdateToolbar();
        }

        private void Undo()
        {
            if (_undo.Count == 0 || !_editing) return;
            _grid = SettlementGrid.FromRows(_undo.Pop());
            _board.Show(_grid);
            Sfx.Play(Sfx.Sound.Erase, 0.4f);
            UpdateToolbar();
        }

        private void LoadExample()
        {
            if (!_editing) return;
            _undo.Push(_grid.ToRows());
            _grid = SettlementGrid.FromRows(_level.HintTiles);
            _board.Show(_grid);
            UpdateToolbar();
            Hud.Toast(Data.Ui("exampleLoaded"), 4.5f);
            _exampleButton.gameObject.SetActive(false);
        }

        private IEnumerator SimulateRound()
        {
            if (!_editing) yield break;
            if (_grid.Count(Cell.Igloo) == 0) { Hud.Toast(Data.Ui("needIgloos"), 3f); yield break; }
            _editing = false;
            _simulateButton.interactable = false;
            _board.ShowCursor(-1, Color.clear);
            var planned = _grid.ToRows();

            SettlementSimulation finished = null;
            yield return Animate(_grid, _level, s => finished = s);
            if (finished == null)
            {
                // Rechnung hängen geblieben: zurück zum Planen, die Runde zählt nicht.
                RestorePlanned(planned);
                yield break;
            }
            var r = finished.Result();
            _round++;
            for (int s = 0; s < r.Stars; s++) { Sfx.Play(Sfx.Sound.Ding, 0.5f); yield return new WaitForSeconds(0.18f); }
            if (r.Stars >= 2) Sfx.Play(Sfx.Sound.Success, 0.5f);

            KeepIfBest(r, planned, finished);
            _runBest = Mathf.Max(_runBest, r.Stars);
            _lastSim = finished;
            _lastPlanned = planned;
            _lastBalance = SettlementBalance.Compute(finished.Grid, _level, _mats, finished.Temperatures);
            _balanceScale = SettlementRounds.BalanceScale(finished.Grid, _lastBalance);

            // Erst sprechen, dann die Knöpfe zeigen: In v0.1 konnte „Fertig“ während eines Dialogs gedrückt
            // werden, dann liefen zwei Dialoge gleichzeitig (Review zu PR #2).
            var panel = ShowResult(r);
            _resultOpen = true;
            yield return new WaitForSeconds(0.3f);
            if (r.Stars >= 2 && !_successShown)
            {
                _successShown = true;
                yield return Hud.Say(r.Stars == 3 ? "phase5_three_stars" : "phase5_success");
            }
            else if (r.Stars == 1 && !_oneStarShown && !_successShown)
            {
                // Ein Stern heißt: Ziel erreicht, aber etwas ist geschmolzen – Friedas Lob für zwei Sterne passt hier nicht.
                _oneStarShown = true;
                yield return Hud.Say("phase5_one_star");
            }
            else if (_runBest == 0 && _round == 2)
            {
                yield return Hud.Say(r.Melted > 0 ? "phase5_hint_melt" : r.Walls == 0 ? "phase5_hint_wind" : "phase5_hint_cold");
            }
            else if (_runBest == 0 && _round >= _level.Rounds && !_solutionOffered)
            {
                _solutionOffered = true;
                _exampleButton.gameObject.SetActive(true);
                yield return Hud.Say("phase5_solution");
            }
            if (!_balanceHintShown)
            {
                _balanceHintShown = true;
                yield return Hud.Say("balance_hint");
            }

            bool closed = false;
            AddResultButtons(panel, () => closed = true, () => { closed = true; _finished = true; });
            while (!closed) yield return null;
            _resultOpen = false;
            CloseBalance();
            _previous = r;
            if (_finished) yield break;
            RestorePlanned(planned);
        }

        /// <summary>Zurück zum geplanten Aufbau – im Computer ist nichts wirklich geschmolzen.</summary>
        private void RestorePlanned(string[] planned)
        {
            _board.Clear();
            _board.SetHeat(null);
            _grid = SettlementGrid.FromRows(planned);
            _board.Show(_grid);
            UpdateToolbar();
            _simulateButton.interactable = true;
            _editing = true;
        }

        /// <summary>F-02: Es zählt die beste Runde – auch wenn „nochmal planen“ ein neues Minigame startet.</summary>
        private void KeepIfBest(SettlementResult r, string[] planned, SettlementSimulation finished)
        {
            var st = _root.State;
            if (!SettlementRounds.IsBetter(r, st.Mg1BestResult)) return;
            st.Mg1BestStars = r.Stars;
            st.Mg1BestLayout = planned;
            st.Mg1BestFinalRows = finished.Grid.ToRows();
            st.Mg1BestResult = r;
            st.Mg1BestTemperatures = (double[])finished.Temperatures.Clone();
            if (st.Mg1Snapshot != null) Destroy(st.Mg1Snapshot);
            st.Mg1Snapshot = _board.RenderSnapshot(); // hochaufgelöst mit Gitterlinien statt Kopie der 24×24-Bodentextur
        }

        private RectTransform ShowResult(SettlementResult r)
        {
            var panel = UIKit.Panel(_ui.transform, Palette.PanelLight, "Result");
            // Oberkante unter dem Knopf „Blick unter die Haube“, Unterkante über der Werkzeugleiste (1920 × 1080).
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(1, 0.5f), new Vector2(-300, -70), new Vector2(560, 680));
            var title = UIKit.Label(panel.transform, Data.Ui("resultTitle", _round), 34, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 10, -70, 10, 10);

            var lights = Data.Mg1Content.Obj("lights");
            int target = _level.IglooTarget;
            AddLight(panel.transform, 0, r.Comfortable >= target ? 2 : r.Comfortable >= target - 3 ? 1 : 0,
                Data.Ui("resultLight", lights.Str("comfy"), r.Comfortable, target));
            AddLight(panel.transform, 1, r.Melted == 0 ? 2 : r.Melted <= 2 ? 1 : 0,
                r.Melted == 0 ? Data.Ui("resultNothingMelted", lights.Str("melt")) : Data.Ui("resultMelted", lights.Str("melt"), r.Melted));
            AddLight(panel.transform, 2, r.Cost <= 0.8 * _level.Budget ? 2 : r.Cost <= _level.Budget ? 1 : 0,
                Data.Ui("resultLanterns", lights.Str("budget"), r.Lanterns, _level.MaxLanterns));

            // Optimieren heißt vergleichen: was hat die letzte Änderung gebracht?
            if (_previous != null)
            {
                var cmp = UIKit.Label(panel.transform, CompareText(_previous, r), 22, Palette.Text, TextAnchor.UpperLeft);
                UIKit.PlaceFixed(cmp.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 320), new Vector2(540, 110));
            }

            for (int s = 0; s < 3; s++)
            {
                var star = UIKit.Panel(panel.transform, s < r.Stars ? Palette.Selected : new Color(0.75f, 0.78f, 0.85f), "Star");
                UIKit.PlaceFixed(star.rectTransform, new Vector2(0.5f, 0), new Vector2(-90 + s * 90, 230), new Vector2(54, 54));
                star.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            var starText = UIKit.Label(panel.transform, StarText(r.Stars), 24, Palette.Text);
            UIKit.PlaceFixed(starText.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 175), new Vector2(560, 40));
            var tap = UIKit.Label(panel.transform, Data.Ui("resultTapIgloo"), 20, new Color(0.35f, 0.4f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Italic);
            UIKit.PlaceFixed(tap.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 135), new Vector2(560, 34));
            return panel.rectTransform;
        }

        private void AddResultButtons(RectTransform panel, System.Action onNext, System.Action onFinish)
        {
            bool canFinish = _runBest >= 1 || _round >= _level.Rounds;
            var nextB = UIKit.Button(panel, Data.Ui("buildOn"), () => { Destroy(panel.gameObject); onNext(); }, Palette.ButtonAlt, 26);
            UIKit.PlaceFixed((RectTransform)nextB.transform, new Vector2(0.5f, 0), new Vector2(canFinish ? -140 : 0, 65), new Vector2(260, 80));
            if (canFinish)
            {
                var fin = UIKit.Button(panel, Data.Ui("finish"), () => { Destroy(panel.gameObject); onFinish(); }, Palette.Button, 26);
                UIKit.PlaceFixed((RectTransform)fin.transform, new Vector2(0.5f, 0), new Vector2(140, 65), new Vector2(260, 80));
            }
        }

        /// <summary>Unterschiede zur Vorrunde in Worten – ohne Pfeilzeichen, die die eingebaute Schrift nicht hat.</summary>
        private string CompareText(SettlementResult before, SettlementResult now)
        {
            var c = SettlementRounds.Compare(before, now);
            var parts = new List<string>();
            if (c.CosyDelta > 0) parts.Add(Data.Ui("compareCosyMore", c.CosyDelta));
            if (c.CosyDelta < 0) parts.Add(Data.Ui("compareCosyLess", -c.CosyDelta));
            if (c.MeltDelta < 0) parts.Add(Data.Ui("compareMeltLess", -c.MeltDelta));
            if (c.MeltDelta > 0) parts.Add(Data.Ui("compareMeltMore", c.MeltDelta));
            if (c.LanternDelta < 0) parts.Add(Data.Ui("compareLanternsLess", -c.LanternDelta));
            if (c.LanternDelta > 0) parts.Add(Data.Ui("compareLanternsMore", c.LanternDelta));
            if (c.Unchanged) parts.Add(Data.Ui("compareSame"));
            return Data.Ui("compareTitle", _round - 1) + " " + string.Join(", ", parts) + "\n" + Data.Ui("klara" + c.Verdict);
        }

        // ---------- Waage je Iglu (nach jeder Runde) ----------

        private void HandleBalanceTap()
        {
            if (Hud.Blocking || _lastSim == null) return;
            if (!InputHelper.PointerDown || InputHelper.PointerOverUI) return;
            if (!_board.CellAtPointer(Cam, out int cell)) return;
            ShowBalance(cell);
        }

        /// <summary>
        /// Die Waage aus Schritt 2 als Werkzeug: Für das angetippte Iglu zeigt sie, was reinkommt und was rausgeht –
        /// als Balken ohne Zahlen, berechnet aus genau der Lösung, die das Wärmebild zeigt (<see cref="SettlementBalance"/>).
        /// </summary>
        private void ShowBalance(int cell)
        {
            CloseBalance();
            var bal = Data.Mg1Content.Obj("balance");
            var final = _lastSim.Grid;
            bool wasIgloo = _lastPlanned != null && _lastPlanned[cell / _level.Width][cell % _level.Width] == Cell.Igloo;

            _balancePanel = UIKit.Panel(_ui.transform, Palette.PanelLight, "Balance").gameObject;
            var prt = (RectTransform)_balancePanel.transform;
            // Links unten, über der Werkzeugleiste; der Technik-Kasten teilt sich den Platz und weicht solange.
            UIKit.PlaceFixed(prt, new Vector2(0, 0), new Vector2(320, 380), new Vector2(560, 480));
            if (_techPanel != null) _techPanel.SetActive(false);
            Sfx.Play(Sfx.Sound.Click);

            if (final[cell] != Cell.Igloo)
            {
                var msg = UIKit.Label(prt, wasIgloo ? bal.Str("puddle") : bal.Str("notIgloo"), 26, Palette.Text, TextAnchor.MiddleCenter);
                UIKit.Place(msg.rectTransform, 0, 0, 1, 1, 30, 30, 30, 30);
                return;
            }

            var title = UIKit.Label(prt, bal.Str("title"), 30, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 10, -60, 10, 10);
            var b = _lastBalance;
            double inSum = b.In(cell), outSum = b.Out(cell);
            // Eingeschwungen ist JEDE Waage im Gleichgewicht (rein = raus). Was ein kaltes von einem gemütlichen
            // Iglu unterscheidet, ist, wie viel Wärme hindurchfließt – darum eine gemeinsame Skala für alle Iglus
            // der Runde statt „volle Breite je Iglu“ (QA-Befund: sonst sahen alle Waagen gleich aus).
            double max = System.Math.Max(1e-9, _balanceScale);
            AddBar(prt, 0, bal.Str("in"), bal.Str("source"), b.Source[cell], bal.Str("fromNeighbours"), b.FromNeighbours[cell], max, HeatColors.Comfy);
            AddBar(prt, 1, bal.Str("out"), bal.Str("toNeighbours"), b.ToNeighbours[cell], bal.Str("toAir"), b.ToAir[cell], max, HeatColors.Cold);

            var state = SettlementModel.Classify(_lastSim.Temperatures[cell], _level);
            string verdict = state == IglooState.Comfortable ? bal.Str("comfy") : state == IglooState.Cold ? bal.Str("cold") : bal.Str("melting");
            var v = UIKit.Label(prt, bal.Str("explain") + "\n" + verdict, 20, Palette.Text, TextAnchor.UpperLeft);
            UIKit.Place(v.rectTransform, 0, 0, 1, 0, 24, 14, 24, -150);
            if (_techVisible)
            {
                var nums = UIKit.Label(prt, Data.Tech("techBalance", inSum, outSum, _lastSim.Temperatures[cell]), 18,
                    new Color(0.3f, 0.45f, 0.55f), TextAnchor.LowerRight);
                UIKit.Place(nums.rectTransform, 0, 0, 1, 0, 24, 4, 24, -30);
            }
        }

        /// <summary>Ein Balken aus zwei Teilen (z. B. eigene Wärme + von den Nachbarn), Länge relativ zur größeren Seite.</summary>
        private static void AddBar(RectTransform parent, int row, string label, string partA, double a, string partB, double b, double max, Color color)
        {
            const float full = 480f;
            float y = -110 - row * 120;
            var head = UIKit.Label(parent, label, 24, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.PlaceFixed(head.rectTransform, new Vector2(0, 1), new Vector2(280, y), new Vector2(500, 34));
            float wa = (float)(a / max) * full, wb = (float)(b / max) * full;
            var barA = UIKit.Panel(parent, color, "BarA");
            barA.rectTransform.anchorMin = barA.rectTransform.anchorMax = new Vector2(0, 1);
            barA.rectTransform.pivot = new Vector2(0, 0.5f);
            barA.rectTransform.anchoredPosition = new Vector2(40, y - 40);
            barA.rectTransform.sizeDelta = new Vector2(Mathf.Max(2, wa), 34);
            var barB = UIKit.Panel(parent, Color.Lerp(color, Color.white, 0.45f), "BarB");
            barB.rectTransform.anchorMin = barB.rectTransform.anchorMax = new Vector2(0, 1);
            barB.rectTransform.pivot = new Vector2(0, 0.5f);
            barB.rectTransform.anchoredPosition = new Vector2(40 + wa, y - 40);
            barB.rectTransform.sizeDelta = new Vector2(Mathf.Max(2, wb), 34);
            var parts = UIKit.Label(parent, partA + "  +  " + partB, 18, new Color(0.3f, 0.35f, 0.45f), TextAnchor.MiddleLeft);
            UIKit.PlaceFixed(parts.rectTransform, new Vector2(0, 1), new Vector2(280, y - 76), new Vector2(500, 28));
        }

        private void CloseBalance()
        {
            if (_balancePanel != null) Destroy(_balancePanel);
            _balancePanel = null;
            if (_techPanel != null) _techPanel.SetActive(_techVisible);
        }

        private string StarText(int stars) => Data.Ui("stars" + Mathf.Clamp(stars, 0, 3));

        /// <summary>Ampel mit Farbe UND Zeichen (OK / ! / X), damit sie nicht nur über Farbe lesbar ist.</summary>
        private static void AddLight(Transform parent, int row, int level, string text)
        {
            var color = level == 2 ? Palette.Good : level == 1 ? Palette.Warn : Palette.Bad;
            var lamp = UIKit.Panel(parent, color, "Lamp");
            UIKit.PlaceFixed(lamp.rectTransform, new Vector2(0, 1), new Vector2(70, -125 - row * 73), new Vector2(64, 64));
            var sym = UIKit.Label(lamp.transform, level == 2 ? "OK" : level == 1 ? "!" : "X", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(sym.rectTransform);
            var t = UIKit.Label(parent, text, 28, Palette.Text, TextAnchor.MiddleLeft);
            UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(330, -125 - row * 73), new Vector2(440, 70));
        }

        private void UpdateTech(SettlementSimulation sim)
        {
            if (_techText == null || !_techVisible) return;
            int n = _level.Width * _level.Height;
            _techText.text = string.Join("\n", new[]
            {
                Data.Tech("techTitle"),
                Data.Tech("techCells", n),
                Data.Tech("techCouplings", 2 * n - _level.Width - _level.Height),
                Data.Tech("techSolver", sim.Iteration),
                Data.Tech("techResidual", sim.Residual),
                Data.Tech("techSolves", sim.SolveCount, sim.MeltPasses.Count),
                Data.Tech("techEquation"),
            });
        }
    }
}
