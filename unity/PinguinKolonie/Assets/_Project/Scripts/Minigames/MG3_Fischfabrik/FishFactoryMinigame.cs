using System.Collections;
using System.Collections.Generic;
using SFE.SimCore.Data;
using SFE.SimCore.Process;
using UnityEngine;
using UnityEngine.UI;

namespace SFE.Game
{
    /// <summary>
    /// MG3 „Fischfabrik“ (Prozesse, docs/03): Olafs Fabrik erst im Computer planen, dann umbauen.
    /// Ablauf nach dem Muster von MG1 (<see cref="SettlementMinigame"/>):
    /// Kalles Versuch in echt (<see cref="FishFactoryHall"/>) → 1 Fabrik bauen (Schwerpunkt, Bausteine auf ZWILLIS
    /// Bildschirm) → 2 Welche Regel gilt? (Supermarkt-Kasse, dann Rechentafel einer Station, <see cref="StationLessonView"/>)
    /// → 3 Alles hängt zusammen (Weg des Fischs als Pfeile) → 4 Der Computer probiert's aus (eine Schicht im Zeitraffer,
    /// Erklärung der Simulation) → 5 Die Möwen-Frage (Schwerpunkt: „Noch ein Tag“, „10 Tage simulieren“) → 6 Besser machen
    /// (Budget, je Runde 10 Tage, Ampeln und Sterne). Schritt 7 „So macht's Siemens“: <see cref="FishFactoryTransfer"/>.
    /// Alle Zahlen aus <c>SFE.SimCore.Process</c> (Golden Files golden/process/).
    /// </summary>
    public sealed class FishFactoryMinigame : MonoBehaviour
    {
        private enum Tool { Sort, Fillet, Pack, Regal, Belt, Robot, Move, Erase }

        private static readonly Tool[] Tools = { Tool.Sort, Tool.Fillet, Tool.Pack, Tool.Regal, Tool.Belt, Tool.Robot, Tool.Move, Tool.Erase };
        /// <summary>Kalles Halle liegt neben dem Labor, damit beide Szenen nebeneinander stehen können.</summary>
        private static readonly Vector3 HallOffset = new Vector3(0f, 0f, -60f);

        /// <summary>Aus, wenn Kalles Versuch schon vorher (z. B. im Hub) gezeigt wurde.</summary>
        public bool PlayHall = true;

        private GameRoot _root;
        private ProcessLevel _level;
        private Dictionary<string, object> _content;
        private Dictionary<string, object> _names;
        private FishFactoryBoard _board;
        private Hologram _zwilli;
        private GameObject _ui;

        // Bauen
        private FactoryLayout _layout;
        private string[] _plan;
        private readonly Stack<string[]> _undo = new Stack<string[]>();
        private Tool _tool = Tool.Sort;
        private bool _editing, _painting, _paintRemoves, _strokeWarned;
        private int _lastPainted = -1;
        private string[] _beforeStroke;
        private char _carry = FactoryLayout.Floor;
        private int _carryFrom = -1;
        private bool _carryJustLifted, _moveHintShown;
        private int _budget;
        private readonly Dictionary<Tool, Button> _toolButtons = new Dictionary<Tool, Button>();
        private Text _budgetLabel, _roundLabel;
        private Button _mainButton, _exampleButton;

        // Rechnen
        private FactorySimulation _lastSim;
        private DaysSummary _lastSummary;
        private int _bottleneck = -1;

        // Besser machen
        private int _round, _runBest = -1;
        private bool _finished, _successShown, _solutionOffered, _hintShown;
        private DaysSummary _previous;
        private int _previousStars = -1;

        public static FishFactoryMinigame Create(GameRoot root, Vector3 origin)
        {
            var go = new GameObject("MG3_FishFactory");
            go.transform.position = origin;
            var m = go.AddComponent<FishFactoryMinigame>();
            m._root = root;
            m._level = FishFactoryState.Level;
            m._content = root.Data.MinigameContent("mg3");
            m._names = m._content.Obj("stations");
            return m;
        }

        private Hud Hud => _root.Hud;
        private GameData Data => _root.Data;
        private Camera Cam => _root.Rig.Cam;

        public IEnumerator Play()
        {
            Hud.SetPhaseTitles(_content.Arr("phases"));
            if (PlayHall) yield return FishFactoryHall.Play(_root, transform.position + HallOffset);

            _board = FishFactoryBoard.Create(transform, transform.position, _level, false, Data.Ui("mg3BoardScreen"), _names);
            // Das Team steht links neben dem Bildschirm, hinter dem Wasser, auf einem Stück Laborboden
            var side = new Vector3(-6.4f, 0, _level.Height / 2f);
            Prims.Make(PrimitiveType.Cube, transform, side + new Vector3(0, -0.06f, -0.4f), new Vector3(2.2f, 0.12f, 10.5f), Palette.LabFloor, "TeamFloor");
            FishFactoryHall.Figure(_root, transform, "frieda", Prims.Look.Frieda, transform.position + side + new Vector3(0, 0, 1.4f), 90);
            FishFactoryHall.Figure(_root, transform, "olaf", Prims.Look.Olaf, transform.position + side + new Vector3(0, 0, -0.6f), 90);
            FishFactoryHall.Figure(_root, transform, "kalle", Prims.Look.Kalle, transform.position + side + new Vector3(0, 0, -2.4f), 90);
            FishFactoryHall.Figure(_root, transform, "klara", Prims.Look.Klara, transform.position + side + new Vector3(0, 0, -4.2f), 90);
            var zw = Hologram.Create(transform, 1.2f);
            zw.transform.position = transform.position + side + new Vector3(0, 0, 3.4f);
            zw.transform.rotation = Quaternion.Euler(0, 90, 0);
            _zwilli = zw.GetComponent<Hologram>();
            SetGlitch(0.7f);

            _layout = FactoryLayout.FromRows(_level.InitialTiles);
            _board.Show(_layout);
            BoardView(snap: true);
            yield return Hud.FadeIn();
            yield return Hud.Say("mg3_lab_intro");
            _root.Rig.ZoomEnabled = true;
            Hud.Toast(Data.Ui("zoomHint"), 6f);

            yield return PhaseBuild();
            yield return PhaseRule();
            yield return PhaseFlow();
            yield return PhaseRun();
            yield return PhaseGulls();
            yield return PhaseImprove();

            yield return Hud.FadeOut();
            Hud.ShowPhases(-1);
            _root.Rig.ZoomEnabled = false;
            _root.Rig.ResetZoom();
            FishFactoryState.Done = true;
            if (_ui != null) Destroy(_ui);
            Destroy(gameObject);
        }

        /// <summary>ZWILLI stottert weniger, je weiter der Spieler kommt.</summary>
        private void SetGlitch(float g)
        {
            Hud.ZwilliGlitch = g;
            if (_zwilli != null) _zwilli.Glitch = g;
        }

        /// <summary>Brett etwas links der Bildmitte: rechts liegt das Ergebnis, unten die Werkzeugleiste.</summary>
        private void BoardView(bool snap = false) =>
            _root.Rig.LookAt(_board.Center + new Vector3(2.2f, 0, -1.2f), 62f, _root.Rig.DistanceToFit(_level.Width + 8f), 0f, snap);

        private void AddGlossary(string phase)
        {
            foreach (var g in _content.Arr("glossary"))
            {
                var o = (Dictionary<string, object>)g;
                if (o.Str("phase") != phase) continue;
                string entry = $"{o.Str("term")}: {o.Str("text")}";
                if (_root.State.Glossary.Contains(entry)) continue;
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

        private string Name(char symbol) => _names.TryGetValue(symbol.ToString(), out object n) ? (string)n : symbol.ToString();

        private string StageName(int stage) => stage >= 0 && stage < _level.Stages.Length ? Name(_level.Stages[stage].Symbol) : "";

        // ================================================================ Schritt 1: Fabrik bauen

        private IEnumerator PhaseBuild()
        {
            Hud.ShowPhases(0);
            yield return Hud.Say("mg3_cards_intro");
            yield return Cards();
            yield return Hud.Say("mg3_cards_done");

            yield return Hud.Say("mg3_build_intro");
            _budget = _level.BuildBudget;
            bool done = false;
            BuildToolbar(Data.Ui("mg3BuildDone"), () =>
            {
                if (!_editing) return;
                PutBack();
                var problems = _layout.Problems(_level);
                if (problems.Count > 0) { Sfx.Play(Sfx.Sound.Wrong); Hud.Toast(ProblemText(problems[0]), 5f); return; }
                done = true;
            });
            _editing = true;
            while (!done)
            {
                if (_editing) HandleEditing();
                yield return null;
            }
            _editing = false;
            _board.ShowCursor(-1, Color.clear);
            _plan = _layout.ToRows();
            Sfx.Play(Sfx.Sound.Success, 0.5f);
            Hud.Toast(Data.Ui("mg3BuildOk"), 3f);
            Destroy(_ui);
            AddGlossary("modelling");
            yield return Hud.Say("mg3_build_done");
        }

        private string ProblemText(LayoutProblem p)
        {
            if (p.Kind == LayoutProblem.Missing) return Data.Ui("mg3Missing", Name(p.Symbol));
            var hops = _content.Arr("hops");
            return Data.Ui("mg3NoRobot", p.Hop >= 0 && p.Hop < hops.Count ? (string)hops[p.Hop] : "");
        }

        /// <summary>Was ist wichtig? Sechs Karten, vier richtig – wie in MG1, nur für die Fabrik.</summary>
        private IEnumerator Cards()
        {
            var layer = NewUiLayer("MG3_Cards");
            var panel = UIKit.Panel(layer, Palette.Panel, "CardPanel");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1200, 560));
            var title = UIKit.Label(panel.transform, Data.Ui("mg3CardsTitle"), 34, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 20, -80, 20, 10);
            var cards = _content.Arr("cards");
            var selected = new bool[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                var c = (Dictionary<string, object>)cards[i];
                int idx = i;
                var b = UIKit.Button(panel.transform, c.Str("text"), null, Palette.ButtonAlt, 28);
                var img = b.GetComponent<Image>();
                b.onClick.AddListener(() =>
                {
                    selected[idx] = !selected[idx];
                    img.color = selected[idx] ? Palette.Button : Palette.ButtonAlt;
                    UIKit.SetButtonLabel(b, selected[idx] ? Data.Ui("cardsChecked", c.Str("text")) : c.Str("text"));
                    Sfx.Play(Sfx.Sound.Click);
                });
                Hud.LockWhileBlocking(b);
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
                    if (!selected[i] && (bool)c["correct"]) { Sfx.Play(Sfx.Sound.Wrong); Hud.Toast(_content.Str("cardsMissing"), 4.5f); return; }
                }
                done = true;
            });
            UIKit.PlaceFixed((RectTransform)finish.transform, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(300, 80));
            Hud.LockWhileBlocking(finish);
            while (!done) yield return null;
            Destroy(_ui);
            Sfx.Play(Sfx.Sound.Success, 0.5f);
        }

        // ---------------------------------------------------------------- Werkzeugleiste (Bauen und Besser machen)

        private void BuildToolbar(string mainLabel, System.Action onMain, bool withRound = false)
        {
            var layer = NewUiLayer("MG3_Build");
            _toolButtons.Clear();
            var bar = UIKit.Panel(layer, Palette.Panel, "Toolbar");
            UIKit.Place(bar.rectTransform, 0, 0, 1, 0, 0, 0, 0, -130);
            Hud.LockWhileBlocking(bar);
            for (int i = 0; i < Tools.Length; i++)
            {
                var tool = Tools[i];
                var b = UIKit.Button(bar.transform, "", () => SelectTool(tool), Palette.ButtonAlt, 21);
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0, 0.5f), new Vector2(88 + i * 152, 0), new Vector2(146, 96));
                _toolButtons[tool] = b;
            }
            var undo = UIKit.Button(bar.transform, Data.Ui("undo"), Undo, Palette.ButtonQuiet, 21);
            UIKit.PlaceFixed((RectTransform)undo.transform, new Vector2(0, 0.5f), new Vector2(1300, 0), new Vector2(140, 80));
            _budgetLabel = UIKit.Label(bar.transform, "", 22, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.PlaceFixed(_budgetLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(1475, 12), new Vector2(190, 70));
            _roundLabel = UIKit.Label(bar.transform, "", 18, Palette.TextLight);
            UIKit.PlaceFixed(_roundLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(1475, -34), new Vector2(190, 28));
            _roundLabel.gameObject.SetActive(withRound);
            _mainButton = UIKit.Button(bar.transform, mainLabel, () => onMain(), Palette.Button, 28);
            UIKit.PlaceFixed((RectTransform)_mainButton.transform, new Vector2(1, 0.5f), new Vector2(-170, 0), new Vector2(300, 100));

            // Lösungshilfe erst nach Runde 3 ohne Stern und nur auf Wunsch (wie MG1)
            _exampleButton = UIKit.Button(layer, Data.Ui("mg3LoadExample"), LoadExample, Palette.ButtonSide, 24);
            UIKit.PlaceFixed((RectTransform)_exampleButton.transform, new Vector2(0, 1), new Vector2(170, -200), new Vector2(300, 64));
            _exampleButton.gameObject.SetActive(false);
            Hud.LockWhileBlocking(_exampleButton);

            AddTechButton(layer);
            SelectTool(Tool.Sort);
            UpdateToolbar();
        }

        private static char SymbolFor(Tool t)
        {
            switch (t)
            {
                case Tool.Sort: return 'S';
                case Tool.Fillet: return 'F';
                case Tool.Pack: return 'P';
                case Tool.Regal: return FactoryLayout.Regal;
                case Tool.Belt: return FactoryLayout.Belt;
                case Tool.Robot: return FactoryLayout.Robot;
                default: return FactoryLayout.Floor;
            }
        }

        private void SelectTool(Tool t)
        {
            PutBack();
            _tool = t;
            if (t == Tool.Move && !_moveHintShown)
            {
                _moveHintShown = true;
                Hud.Toast(Data.Ui("moveHint"), 4.5f);
            }
            foreach (var kv in _toolButtons) kv.Value.GetComponent<Image>().color = kv.Key == t ? Palette.Button : Palette.ButtonAlt;
        }

        private void UpdateToolbar()
        {
            foreach (var kv in _toolButtons)
            {
                char sym = SymbolFor(kv.Key);
                string label = kv.Key == Tool.Move ? Data.Ui("toolMove")
                    : kv.Key == Tool.Erase ? Data.Ui("toolErase")
                    : Data.Ui("mg3Tool", Name(sym), _level.Cost(sym));
                UIKit.SetButtonLabel(kv.Value, label);
            }
            if (_budgetLabel != null) _budgetLabel.text = Data.Ui("mg3Budget", _layout.Cost(_level), _budget);
            if (_roundLabel != null)
            {
                int shown = _round + 1;
                _roundLabel.text = shown <= _level.Rounds ? Data.Ui("round", shown, _level.Rounds) : Data.Ui("extraRound", shown);
            }
        }

        private void HandleEditing()
        {
            if (Hud.Blocking) return;
            bool onBoard = _board.CellAtPointer(Cam, out int cell) && !InputHelper.PointerOverUI;
            if (_tool == Tool.Move) { HandleMove(onBoard, cell); return; }
            _board.ShowCursor(onBoard ? cell : -1, _tool == Tool.Erase ? Palette.Bad : Palette.Selected);
            if (InputHelper.PointerUp) EndStroke();
            if (!onBoard) return;
            if (InputHelper.PointerDown)
            {
                _painting = true;
                _strokeWarned = false;
                _beforeStroke = _layout.ToRows();
                _paintRemoves = _tool == Tool.Erase || _layout[cell] == SymbolFor(_tool);
            }
            if (_painting && InputHelper.PointerHeld && cell != _lastPainted)
            {
                _lastPainted = cell;
                Apply(cell);
            }
        }

        /// <summary>Ein Hinweis je Strich – beim Ziehen über viele Felder nicht bei jedem Feld.</summary>
        private void Warn(string text)
        {
            if (_strokeWarned) return;
            _strokeWarned = true;
            Sfx.Play(Sfx.Sound.Wrong, 0.4f);
            Hud.Toast(text, 3.5f);
        }

        private void Apply(int cell)
        {
            char here = _layout[cell];
            if (FactoryLayout.IsFixed(here)) { Warn(Data.Ui("mg3Fixed")); return; }
            if (_paintRemoves)
            {
                // Entfernen trifft nur den Baustein des gewählten Werkzeugs; der Radierer alles.
                if (here == FactoryLayout.Floor || (_tool != Tool.Erase && here != SymbolFor(_tool))) return;
                _layout[cell] = FactoryLayout.Floor;
                Sfx.Play(Sfx.Sound.Erase, 0.5f);
            }
            else
            {
                char sym = SymbolFor(_tool);
                if (here == sym) return;
                if (here != FactoryLayout.Floor) { Warn(Data.Ui("mg3Occupied")); return; }
                if (_layout.Count(sym) >= _level.Limit(sym)) { Warn(Data.Ui("mg3Limit", _level.Limit(sym), Name(sym))); return; }
                int left = _budget - _layout.Cost(_level);
                if (_level.Cost(sym) > left) { Warn(Data.Ui("mg3OverBudget", left)); return; }
                _layout[cell] = sym;
                Sfx.Play(Sfx.Sound.Plop, 0.5f);
            }
            _board.Show(_layout);
            UpdateToolbar();
        }

        /// <summary>Werkzeug „Verschieben“ wie in MG1: antippen hebt an, freies Feld antippen setzt ab – oder ziehen.</summary>
        private void HandleMove(bool onBoard, int cell)
        {
            _board.ShowCursor(onBoard ? cell : -1, Palette.Selected);
            if (_carry != FactoryLayout.Floor) _board.ShowCarried(_carry, onBoard ? cell : _carryFrom);
            if (InputHelper.PointerDown)
            {
                if (_carry == FactoryLayout.Floor)
                {
                    if (!onBoard) return;
                    char c = _layout[cell];
                    if (c == FactoryLayout.Floor) return;
                    if (FactoryLayout.IsFixed(c)) { Sfx.Play(Sfx.Sound.Wrong, 0.4f); Hud.Toast(Data.Ui("mg3Fixed"), 3f); return; }
                    _beforeStroke = _layout.ToRows();
                    _carry = c;
                    _carryFrom = cell;
                    _carryJustLifted = true;
                    _layout[cell] = FactoryLayout.Floor;
                    _board.Show(_layout);
                    _board.ShowCarried(_carry, cell);
                    Sfx.Play(Sfx.Sound.Click, 0.6f);
                    return;
                }
                if (!onBoard) PutBack();
                else TryDrop(cell);
            }
            else if (InputHelper.PointerUp && _carry != FactoryLayout.Floor)
            {
                bool lifted = _carryJustLifted;
                _carryJustLifted = false;
                if (lifted && onBoard && cell != _carryFrom) TryDrop(cell);
            }
        }

        private void TryDrop(int cell)
        {
            if (cell != _carryFrom && _layout[cell] != FactoryLayout.Floor)
            {
                Sfx.Play(Sfx.Sound.Wrong, 0.4f);
                Hud.Toast(Data.Ui("moveOccupied"), 2.5f);
                return;
            }
            _layout[cell] = _carry;
            _carry = FactoryLayout.Floor;
            _carryFrom = -1;
            _board.ShowCarried(FactoryLayout.Floor, -1);
            _board.Show(_layout);
            Sfx.Play(Sfx.Sound.Plop, 0.5f);
            if (_beforeStroke != null && !FactoryLayout.Same(_beforeStroke, _layout.ToRows())) _undo.Push(_beforeStroke);
            _beforeStroke = null;
            UpdateToolbar();
        }

        private void PutBack()
        {
            if (_carry == FactoryLayout.Floor) return;
            _layout[_carryFrom] = _carry;
            _carry = FactoryLayout.Floor;
            _carryFrom = -1;
            _carryJustLifted = false;
            _beforeStroke = null;
            _board.ShowCarried(FactoryLayout.Floor, -1);
            _board.Show(_layout);
        }

        /// <summary>Ein Rückgängig-Schritt je Strich – nur, wenn sich etwas geändert hat.</summary>
        private void EndStroke()
        {
            if (_painting && _beforeStroke != null && !FactoryLayout.Same(_beforeStroke, _layout.ToRows())) _undo.Push(_beforeStroke);
            _painting = false;
            _lastPainted = -1;
            _beforeStroke = null;
        }

        private void Undo()
        {
            PutBack();
            if (_undo.Count == 0 || !_editing) return;
            _layout = FactoryLayout.FromRows(_undo.Pop());
            _board.Show(_layout);
            Sfx.Play(Sfx.Sound.Erase, 0.4f);
            UpdateToolbar();
        }

        private void LoadExample()
        {
            if (!_editing) return;
            PutBack();
            _undo.Push(_layout.ToRows());
            _layout = FactoryLayout.FromRows(_level.HintTiles);
            _board.Show(_layout);
            UpdateToolbar();
            Hud.Toast(Data.Ui("mg3ExampleLoaded"), 4.5f);
            _exampleButton.gameObject.SetActive(false);
        }

        // ================================================================ Schritt 2: Welche Regel gilt?

        private IEnumerator PhaseRule()
        {
            Hud.ShowPhases(1);
            SetGlitch(0.6f);
            yield return Hud.Say("mg3_rule_intro");

            // Supermarkt-Kasse: Kommen mehr, als die Kasse schafft, wächst die Schlange
            var layer = NewUiLayer("MG3_Rule");
            var panel = UIKit.Panel(layer, Palette.PanelLight, "Rule");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1100, 640));
            var title = UIKit.Label(panel.transform, Data.Ui("mg3RuleTitle"), 32, Palette.TextAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 20, -70, 20, 15);
            var q = UIKit.Label(panel.transform, _content.Str("ruleQuestion"), 30, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(q.rectTransform, 0, 1, 1, 1, 50, -190, 50, 75);
            var feedback = UIKit.Label(panel.transform, Data.Ui("mg3RuleHint"), 24, Palette.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);
            UIKit.Place(feedback.rectTransform, 0, 0, 1, 0, 40, 25, 40, -95);
            var options = _content.Arr("ruleOptions");
            var buttons = new List<Button>();
            bool right = false;
            for (int i = 0; i < options.Count; i++)
            {
                var o = (Dictionary<string, object>)options[i];
                Button b = null;
                b = UIKit.Button(panel.transform, o.Str("text"), () =>
                {
                    if (right) return;
                    feedback.text = o.Str("feedback");
                    if ((bool)o["correct"])
                    {
                        right = true;
                        b.GetComponent<Image>().color = Palette.Good;
                        foreach (var other in buttons) other.interactable = false;
                        Sfx.Play(Sfx.Sound.Success, 0.5f);
                    }
                    else
                    {
                        b.GetComponent<Image>().color = Palette.ButtonOff;
                        b.interactable = false;
                        Sfx.Play(Sfx.Sound.Wrong, 0.5f);
                    }
                }, Palette.ButtonAlt, 26);
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0, -270 - i * 100), new Vector2(860, 84));
                Hud.LockWhileBlocking(b);
                buttons.Add(b);
            }
            while (!right) yield return null;
            yield return new WaitForSeconds(1.2f);
            Destroy(_ui);
            AddGlossary("equation");
            yield return Hud.Say("mg3_rule_done");

            // Die Regel in Zahlen: eine Station, Ereignis für Ereignis
            yield return StationLessonView.Play(_root, _board, _level);
            AddGlossary("event");
            _layout = FactoryLayout.FromRows(_plan);
            _board.Show(_layout);
            BoardView();
        }

        // ================================================================ Schritt 3: Alles hängt zusammen

        private IEnumerator PhaseFlow()
        {
            Hud.ShowPhases(2);
            SetGlitch(0.5f);
            yield return Hud.Say("mg3_flow_intro");
            var order = FactoryLayout.GroupSymbols(_level);
            int arrows = order.Length - 1;
            int next = 0, wrong = 0;
            Hud.Toast(Data.Ui("mg3FlowCount", 0, arrows), 4f);
            while (next < order.Length)
            {
                yield return null;
                // Nach drei Fehlversuchen zeigt ZWILLI den nächsten Pfeil selbst (docs/03: nach 3 eine Lösungshilfe)
                if (wrong < 3)
                {
                    if (Hud.Blocking) continue;
                    bool on = _board.CellAtPointer(Cam, out int cell) && !InputHelper.PointerOverUI;
                    _board.ShowCursor(on ? cell : -1, Palette.Selected);
                    if (!on || !InputHelper.PointerDown) continue;
                    if (_layout[cell] != order[next])
                    {
                        if (_layout[cell] == FactoryLayout.Floor) continue;
                        wrong++;
                        Sfx.Play(Sfx.Sound.Wrong, 0.5f);
                        Hud.Toast(wrong >= 2 && next > 0 ? Data.Ui("mg3FlowHint", Name(order[next - 1]), Name(order[next])) : Data.Ui("mg3FlowWrong"), 3.5f);
                        continue;
                    }
                }
                if (next > 0)
                {
                    _board.AddArrow(_board.GroupCenter(_layout, order[next - 1]), _board.GroupCenter(_layout, order[next]));
                    Hud.Toast(Data.Ui("mg3FlowCount", next, arrows), 3f);
                }
                foreach (int c in _layout.Cells(order[next])) StartCoroutine(_board.Pulse(c));
                Sfx.Play(Sfx.Sound.Plop, 0.6f);
                next++;
                wrong = 0;
            }
            _board.ShowCursor(-1, Color.clear);
            yield return new WaitForSeconds(0.6f);
            AddGlossary("assembly");
            yield return Hud.Say("mg3_flow_done");
            _board.ClearArrows();
        }

        // ================================================================ Schritt 4: Der Computer probiert's aus

        private IEnumerator PhaseRun()
        {
            Hud.ShowPhases(3);
            SetGlitch(0.4f);
            BoardView();
            yield return Hud.Say("mg3_run_intro");
            var layer = NewUiLayer("MG3_Run");
            var status = StatusBar(layer);
            AddTechButton(layer);
            bool go = false;
            var start = UIKit.Button(layer, Data.Ui("mg3RunStart"), () => go = true, Palette.Button, 34);
            UIKit.PlaceFixed((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(380, 100));
            Hud.LockWhileBlocking(start);
            while (!go) yield return null;
            start.gameObject.SetActive(false);

            var config = _layout.ToConfig(_level);
            var sim = new FactorySimulation(config, _level.Seed, trace: true);
            yield return _board.Play(sim, 30f, _level.ShiftLength, s => status.text = FishFactoryHall.Status(Data, _level, s));
            _lastSim = sim;
            var day = sim.Result();
            _bottleneck = day.Bottleneck;
            Sfx.Play(day.Delivered >= _level.Target ? Sfx.Sound.Success : Sfx.Sound.Wrong, 0.5f);
            ShowDayPanel(layer, day);
            ShowHallPhoto(layer);
            AddGlossary("solution");
            yield return Hud.Say("mg3_run_done");
            yield return Hud.Say("mg3_explain");
            AddGlossary("twin");
            Destroy(_ui);
        }

        private Text StatusBar(RectTransform layer)
        {
            var bar = UIKit.Panel(layer, Palette.PanelSoft, "Status");
            UIKit.PlaceFixed(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(-120, -112), new Vector2(1150, 52));
            var t = UIKit.Label(bar.transform, "", 26, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(t.rectTransform);
            return t;
        }

        private void ShowDayPanel(RectTransform layer, DayResult day)
        {
            var panel = ResultPanel(layer, Data.Ui("mg3DayTitle"));
            var lights = _content.Obj("lights");
            int t = _level.Target;
            AddLight(panel, 0, day.Delivered >= t ? 2 : day.Delivered >= 0.85 * t ? 1 : 0, Data.Ui("mg3OnTime", lights.Str("onTime"), day.Delivered, t));
            AddLight(panel, 1, day.Spoiled <= _level.SpoilMax ? 2 : day.Spoiled <= 2 * _level.SpoilMax ? 1 : 0,
                Data.Ui("mg3Fresh", lights.Str("fresh"), day.Spoiled));
            var b = UIKit.Label(panel, Data.Ui("mg3Bottleneck", StageName(day.Bottleneck)), 28, Palette.TextWarm, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.PlaceFixed(b.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(520, 50));
        }

        /// <summary>Links das Foto aus der echten Halle (Kalles Versuch), in der Mitte ZWILLIS Rechnung – wie MG1 Schritt 4.</summary>
        private void ShowHallPhoto(RectTransform layer)
        {
            var photo = FishFactoryState.HallPhoto;
            if (photo == null) return;
            var frame = UIKit.Panel(layer, Palette.PanelLight, "HallPhoto");
            UIKit.PlaceFixed(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(230, 60), new Vector2(400, 330));
            var img = new GameObject("Image").AddComponent<RawImage>();
            UIKit.Rect(img.gameObject, frame.transform);
            UIKit.Place(img.rectTransform, 0, 0, 1, 1, 12, 12, 12, 70);
            img.texture = photo;
            var cap = UIKit.Label(frame.transform, Data.Ui("mg3HallPhotoCaption"), 22, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(cap.rectTransform, 0, 1, 1, 1, 10, -62, 10, 6);
        }

        // ================================================================ Schritt 5: Die Möwen-Frage

        private IEnumerator PhaseGulls()
        {
            Hud.ShowPhases(4);
            SetGlitch(0.3f);
            _board.ResetSim();
            yield return Hud.Say("mg3_gulls_intro");
            var layer = NewUiLayer("MG3_Gulls");
            var status = StatusBar(layer);
            AddTechButton(layer);
            var config = _layout.ToConfig(_level);

            var captionBox = UIKit.Panel(layer, Palette.PanelLight, "DayCaption");
            UIKit.PlaceFixed(captionBox.rectTransform, new Vector2(0.5f, 0f), new Vector2(-200, 215), new Vector2(980, 64));
            var caption = UIKit.Label(captionBox.transform, Data.Ui("mg3DayCaption", 1, _lastSim.Delivered, _lastSim.Spoiled), 26, Palette.Text,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(caption.rectTransform);

            int day = 1;
            bool busy = false, anotherShown = false, daysDone = false, cont = false;
            Button another = null, ten = null, next = null;
            another = UIKit.Button(layer, Data.Ui("mg3AnotherDay"), () =>
            {
                if (busy) return;
                StartCoroutine(AnotherDay());
            }, Palette.ButtonAlt, 30);
            UIKit.PlaceFixed((RectTransform)another.transform, new Vector2(0.5f, 0f), new Vector2(-420, 100), new Vector2(340, 96));
            Hud.LockWhileBlocking(another);
            ten = UIKit.Button(layer, Data.Ui("mg3TenDays"), () =>
            {
                if (busy) return;
                StartCoroutine(TenDays());
            }, Palette.Button, 30);
            UIKit.PlaceFixed((RectTransform)ten.transform, new Vector2(0.5f, 0f), new Vector2(-30, 100), new Vector2(380, 96));
            Hud.LockWhileBlocking(ten);
            ten.gameObject.SetActive(false);
            next = UIKit.Button(layer, Data.Ui("next"), () => cont = true, Palette.Button, 30);
            UIKit.PlaceFixed((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(330, 100), new Vector2(260, 96));
            Hud.LockWhileBlocking(next);
            next.gameObject.SetActive(false);

            IEnumerator AnotherDay()
            {
                busy = true;
                another.interactable = false;
                var sim = new FactorySimulation(config, Replications.DaySeed(_level.Seed, day), trace: true);
                yield return _board.Play(sim, 12f, _level.ShiftLength, s => status.text = FishFactoryHall.Status(Data, _level, s));
                _lastSim = sim;
                day++;
                captionBox.gameObject.SetActive(true);
                caption.text = Data.Ui("mg3DayCaption", day, sim.Delivered, sim.Spoiled);
                Sfx.Play(Sfx.Sound.Ding, 0.5f);
                another.interactable = true;
                busy = false;
                if (!anotherShown)
                {
                    anotherShown = true;
                    yield return Hud.Say("mg3_gulls_another");
                    ten.gameObject.SetActive(true);
                }
            }

            IEnumerator TenDays()
            {
                busy = true;
                ten.interactable = false;
                _lastSummary = Replications.Run(config, _level.Seed, _level.Replications);
                var panel = ResultPanel(layer, Data.Ui("mg3BarsTitle"));
                yield return Bars(panel, _lastSummary, new Vector2(0, 175), 0.18f);
                var mean = UIKit.Label(panel, Data.Ui("mg3BarsMean", _lastSummary.MeanDelivered, _lastSummary.MaxDelivered, _lastSummary.MinDelivered),
                    22, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.PlaceFixed(mean.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(520, 60));
                var note = UIKit.Label(panel, Data.Ui("mg3BarsNote"), 20, Palette.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);
                UIKit.PlaceFixed(note.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(520, 56));
                captionBox.gameObject.SetActive(false);
                if (!daysDone)
                {
                    daysDone = true;
                    yield return Hud.Say("mg3_gulls_days");
                    AddGlossary("randomness");
                    next.gameObject.SetActive(true);
                }
                busy = false;
            }

            while (!cont) yield return null;
            Destroy(_ui);
            _board.ResetSim();
        }

        // ================================================================ Schritt 6: Besser machen

        private IEnumerator PhaseImprove()
        {
            Hud.ShowPhases(5);
            SetGlitch(0.15f);
            _layout = FactoryLayout.FromRows(_plan);
            _undo.Clear();
            _board.Show(_layout);
            BoardView();
            _budget = _level.Budget;
            HighlightBottleneck();
            yield return Hud.Say("mg3_improve_intro");
            BuildToolbar(Data.Ui("mg3TenDays"), () => { if (_editing) StartCoroutine(SimulateRound()); }, withRound: true);
            _editing = true;
            while (!_finished)
            {
                if (_editing) HandleEditing();
                yield return null;
            }
            _board.SetHighlight(null);
            AddGlossary("optimization");
            yield return Hud.Say("mg3_improve_done");
        }

        /// <summary>Die Station, die zuletzt am stärksten ausgelastet war, blinkt – dort fängt man an (Frieda).</summary>
        private void HighlightBottleneck()
        {
            if (_bottleneck < 0) { _board.SetHighlight(null); return; }
            _board.SetHighlight(_level.Stages[_bottleneck].Symbol);
        }

        private IEnumerator SimulateRound()
        {
            PutBack();
            var problems = _layout.Problems(_level);
            if (problems.Count > 0) { Sfx.Play(Sfx.Sound.Wrong); Hud.Toast(ProblemText(problems[0]), 5f); yield break; }
            _editing = false;
            _mainButton.interactable = false;
            _board.ShowCursor(-1, Color.clear);
            _board.SetHighlight(null);
            var planned = _layout.ToRows();
            var config = _layout.ToConfig(_level);

            // Ein Tag im schnellen Zeitraffer zum Zusehen, dann rechnet ZWILLI alle zehn Tage
            Hud.Toast(Data.Ui("mg3Running"), 2.5f);
            var sim = new FactorySimulation(config, _level.Seed, trace: true);
            yield return _board.Play(sim, 8f, _level.ShiftLength);
            _lastSim = sim;
            var s = Replications.Run(config, _level.Seed, _level.Replications);
            _lastSummary = s;
            _bottleneck = sim.Result().Bottleneck;
            int stars = Replications.Stars(s, _level);
            _round++;
            for (int i = 0; i < stars; i++) { Sfx.Play(Sfx.Sound.Ding, 0.5f); yield return new WaitForSeconds(0.18f); }
            if (stars >= 2) Sfx.Play(Sfx.Sound.Success, 0.5f);
            KeepIfBest(s, stars, planned);
            _runBest = Mathf.Max(_runBest, stars);

            var panel = ShowRoundResult(s, stars, config);
            yield return new WaitForSeconds(0.3f);
            // Erst sprechen, dann die Knöpfe zeigen – sonst liefen zwei Dialoge gleichzeitig (Review zu MG1).
            if (stars == 3 && !_successShown) { _successShown = true; yield return Hud.Say("mg3_improve_three_stars"); }
            else if (stars == 2 && _previousStars < 2) yield return Hud.Say("mg3_improve_two_stars");
            else if (stars == 1 && _previousStars < 1) yield return Hud.Say("mg3_improve_one_star");
            else if (_runBest == 0 && _round == 2 && !_hintShown)
            {
                _hintShown = true;
                yield return Hud.Say(HintNode(s, config));
            }
            else if (_runBest == 0 && _round >= _level.Rounds && !_solutionOffered)
            {
                _solutionOffered = true;
                _exampleButton.gameObject.SetActive(true);
                yield return Hud.Say("mg3_solution");
            }

            bool closed = false;
            AddResultButtons(panel, () => closed = true, () => { closed = true; _finished = true; });
            while (!closed) yield return null;
            _previous = s;
            _previousStars = stars;
            if (_finished) yield break;
            _board.ResetSim();
            _layout = FactoryLayout.FromRows(planned);
            _board.Show(_layout);
            HighlightBottleneck();
            UpdateToolbar();
            _mainButton.interactable = true;
            _editing = true;
        }

        /// <summary>Tipp nach Runde 2 ohne Stern – passend zu dem, was schiefging.</summary>
        private string HintNode(DaysSummary s, FactoryConfig config)
        {
            if (config.RobotDelay > 0 || (_lastSim != null && _lastSim.WaitingForRobot > 3)) return "mg3_hint_robots";
            if (s.MeanSpoiled > 2 * _level.SpoilMax && s.MeanDelivered >= 0.85 * _level.Target) return "mg3_hint_spoil";
            return "mg3_hint_bottleneck";
        }

        /// <summary>Die beste Runde zählt (wie MG1 F-02); ihr Bild kommt in „So macht's Siemens“.</summary>
        private void KeepIfBest(DaysSummary s, int stars, string[] planned)
        {
            var best = FishFactoryState.BestSummary;
            bool better = best == null || stars > FishFactoryState.BestStars ||
                          (stars == FishFactoryState.BestStars && s.MeanDelivered > best.MeanDelivered);
            if (!better) return;
            FishFactoryState.BestStars = stars;
            FishFactoryState.BestLayout = planned;
            FishFactoryState.BestSummary = s;
            if (FishFactoryState.Snapshot != null) Destroy(FishFactoryState.Snapshot);
            FishFactoryState.Snapshot = FishFactoryBoard.Photo(Cam, 480, 480);
        }

        private RectTransform ShowRoundResult(DaysSummary s, int stars, FactoryConfig config)
        {
            var panel = ResultPanel(_ui.GetComponent<RectTransform>(), Data.Ui("mg3ResultTitle", _round));
            var lights = _content.Obj("lights");
            var l = Replications.Lights(s, _level);
            AddLight(panel, 0, l[0], Data.Ui("mg3OnTimeMean", lights.Str("onTime"), s.MeanDelivered, _level.Target));
            AddLight(panel, 1, l[1], Data.Ui("mg3FreshMean", lights.Str("fresh"), s.MeanSpoiled));
            AddLight(panel, 2, l[2], l[2] == 2 ? Data.Ui("mg3Yes", lights.Str("robust")) : Data.Ui("mg3NotYet", lights.Str("robust")));
            // Von unten (Feld 680 hoch): Knöpfe 25–105 · Sterntext 115–155 · Sterne 162–208 · Vergleich 217–273 ·
            // Balken 285–390 · Ampeln ab 400 (dritte Ampel 402–464)
            StartCoroutine(Bars(panel, s, new Vector2(0, 285), 0.05f, 105f));
            if (_previous != null)
            {
                var cmp = UIKit.Label(panel, CompareText(_previous, s, stars), 19, Palette.Text, TextAnchor.UpperLeft);
                UIKit.PlaceFixed(cmp.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 245), new Vector2(520, 56));
            }
            for (int i = 0; i < 3; i++)
            {
                var star = UIKit.Panel(panel, i < stars ? Palette.Selected : Palette.StarOff, "Star");
                UIKit.PlaceFixed(star.rectTransform, new Vector2(0.5f, 0), new Vector2(-90 + i * 90, 185), new Vector2(40, 40));
                star.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            var starText = UIKit.Label(panel, Data.Ui("mg3Stars" + Mathf.Clamp(stars, 0, 3)), 21, Palette.Text);
            UIKit.PlaceFixed(starText.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 135), new Vector2(540, 40));
            return panel;
        }

        private string CompareText(DaysSummary before, DaysSummary now, int stars)
        {
            var parts = new List<string>();
            double dd = now.MeanDelivered - before.MeanDelivered;
            double ds = now.MeanSpoiled - before.MeanSpoiled;
            if (dd >= 0.5) parts.Add(Data.Ui("mg3CompareMore", dd));
            if (dd <= -0.5) parts.Add(Data.Ui("mg3CompareLess", -dd));
            if (ds <= -0.05) parts.Add(Data.Ui("mg3CompareSpoilLess", -ds));
            if (ds >= 0.05) parts.Add(Data.Ui("mg3CompareSpoilMore", ds));
            if (parts.Count == 0) parts.Add(Data.Ui("compareSame"));
            string verdict = stars > _previousStars || (stars == _previousStars && dd >= 2) ? "Better"
                : stars < _previousStars || dd <= -2 ? "Worse" : "Same";
            return Data.Ui("mg3CompareTitle", _round - 1) + " " + string.Join(", ", parts) + "\n" + Data.Ui("klara" + verdict);
        }

        private void AddResultButtons(RectTransform panel, System.Action onNext, System.Action onFinish)
        {
            bool canFinish = _runBest >= 1 || _round >= _level.Rounds;
            var nextB = UIKit.Button(panel, Data.Ui("buildOn"), () => { Destroy(panel.gameObject); onNext(); }, Palette.ButtonAlt, 26);
            UIKit.PlaceFixed((RectTransform)nextB.transform, new Vector2(0.5f, 0), new Vector2(canFinish ? -140 : 0, 65), new Vector2(260, 80));
            Hud.LockWhileBlocking(nextB);
            if (!canFinish) return;
            var fin = UIKit.Button(panel, Data.Ui("finish"), () => { Destroy(panel.gameObject); onFinish(); }, Palette.Button, 26);
            UIKit.PlaceFixed((RectTransform)fin.transform, new Vector2(0.5f, 0), new Vector2(140, 65), new Vector2(260, 80));
            Hud.LockWhileBlocking(fin);
        }

        // ---------------------------------------------------------------- Ergebnisfeld, Ampeln, Balken

        /// <summary>Rechts, zwischen „Blick unter die Haube“ oben und der Werkzeugleiste unten (1920 × 1080, wie MG1).</summary>
        private RectTransform ResultPanel(RectTransform layer, string title)
        {
            var old = layer.Find("Result");
            if (old != null) Destroy(old.gameObject);
            var panel = UIKit.Panel(layer, Palette.PanelLight, "Result");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(1, 0.5f), new Vector2(-300, -70), new Vector2(560, 680));
            var t = UIKit.Label(panel.transform, title, 32, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(t.rectTransform, 0, 1, 1, 1, 10, -70, 10, 10);
            return panel.rectTransform;
        }

        /// <summary>Ampel mit Farbe UND Zeichen (OK / ! / X), damit sie nicht nur über Farbe lesbar ist.</summary>
        private static void AddLight(RectTransform parent, int row, int level, string text)
        {
            var color = level == 2 ? Palette.Good : level == 1 ? Palette.Warn : Palette.Bad;
            var lamp = UIKit.Panel(parent, color, "Lamp");
            UIKit.PlaceFixed(lamp.rectTransform, new Vector2(0, 1), new Vector2(60, -115 - row * 66), new Vector2(56, 56));
            var sym = UIKit.Label(lamp.transform, level == 2 ? "OK" : level == 1 ? "!" : "X", 26, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(sym.rectTransform);
            var t = UIKit.Label(parent, text, 24, Palette.Text, TextAnchor.MiddleLeft);
            UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(315, -115 - row * 66), new Vector2(450, 62));
        }

        /// <summary>
        /// „Gute und schlechte Tage“ als Balken (docs/04 5.5: nur einfache Balken im Standardmodus). Grün = genug Sushi,
        /// orange = zu wenig; der Strich zeigt, was bestellt ist. Die Balken erscheinen nacheinander.
        /// </summary>
        private IEnumerator Bars(RectTransform panel, DaysSummary s, Vector2 bottomCenter, float delay, float height = 260f)
        {
            const float width = 480f;
            var area = new GameObject("Bars");
            var rt = UIKit.Rect(area, panel);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = bottomCenter;
            rt.sizeDelta = new Vector2(width, height);
            int n = s.Days.Length;
            double max = System.Math.Max(_level.Target * 1.35, s.MaxDelivered * 1.05);
            float slot = width / n;
            float targetY = (float)(_level.Target / max) * (height - 30);
            var line = UIKit.Panel(rt, Palette.Mg3TargetLine, "Target");
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(0, 0);
            line.rectTransform.pivot = new Vector2(0, 0.5f);
            line.rectTransform.anchoredPosition = new Vector2(0, 30 + targetY);
            line.rectTransform.sizeDelta = new Vector2(width, 4);
            for (int d = 0; d < n; d++)
            {
                if (rt == null) yield break;
                int v = s.Days[d].Delivered;
                var bar = UIKit.Panel(rt, v >= _level.Target ? Palette.Mg3BarGood : Palette.Mg3BarBad, "Day");
                bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(0, 0);
                bar.rectTransform.pivot = new Vector2(0.5f, 0);
                bar.rectTransform.anchoredPosition = new Vector2(slot * (d + 0.5f), 30);
                bar.rectTransform.sizeDelta = new Vector2(slot * 0.7f, Mathf.Max(2f, (float)(v / max) * (height - 30)));
                var label = UIKit.Label(rt, Data.Ui("mg3DayLabel", d + 1), 16, Palette.TextMuted);
                UIKit.PlaceFixed(label.rectTransform, new Vector2(0, 0), new Vector2(slot * (d + 0.5f), 12), new Vector2(slot, 24));
                line.transform.SetAsLastSibling();
                if (delay > 0) { Sfx.Play(Sfx.Sound.Plop, 0.3f); yield return new WaitForSeconds(delay); }
            }
        }

        // ---------------------------------------------------------------- Blick unter die Haube

        private void AddTechButton(RectTransform layer)
        {
            var techBtn = UIKit.Button(layer, Data.Ui("techButton"), ShowTech, Palette.ButtonQuiet, 22);
            UIKit.PlaceFixed((RectTransform)techBtn.transform, new Vector2(1, 1), new Vector2(-170, -240), new Vector2(300, 56));
            Hud.LockWhileBlocking(techBtn);
            Hud.TechOnly(techBtn.gameObject);
        }

        /// <summary>
        /// Was der Computer wirklich getan hat: Ereignisliste der letzten Schicht, Kennzahlen, Replikationen mit
        /// Konfidenzintervall, Vereinfachungen und die Formeln. Fachwörter nur hier (Data.Tech).
        /// </summary>
        private void ShowTech()
        {
            var page = Hud.OpenPopup(Data.Tech("techTitle"), new Vector2(1560, 960));
            var ink = Palette.Text;
            void Line(string text, float x, float y, float w, int size = 20, FontStyle style = FontStyle.Normal)
            {
                var t = UIKit.Label(page, text, size, ink, TextAnchor.UpperLeft, style);
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0, 1);
                t.rectTransform.pivot = new Vector2(0, 1);
                t.rectTransform.anchoredPosition = new Vector2(x, y);
                t.rectTransform.sizeDelta = new Vector2(w, 40);
            }
            Line(Data.Tech("mg3TechModel"), 60, -110, 1440, 22, FontStyle.Bold);
            if (_lastSim == null)
            {
                Line(Data.Tech("techNotYet"), 60, -160, 1440);
                return;
            }
            var day = _lastSim.Result();
            Line(Data.Tech("mg3TechDay", day.Events, day.Arrived, day.Delivered, day.Spoiled, day.Stolen, day.LeadTime), 60, -150, 1440);
            var util = new List<string>();
            for (int s = 0; s < day.Utilization.Length; s++) util.Add(Data.Tech("mg3TechUtilPart", StageName(s), day.Utilization[s] * 100));
            Line(Data.Tech("mg3TechUtil", string.Join(", ", util)), 60, -185, 1440);

            // links: Ereignisliste
            var trace = _lastSim.Trace;
            const int shown = 16;
            if (trace != null)
            {
                Line(Data.Tech("mg3TechEvents", Mathf.Min(shown, trace.Count), trace.Count), 60, -235, 760, 20, FontStyle.Bold);
                int first = Mathf.Max(0, trace.Count - shown);
                for (int i = first; i < trace.Count; i++) Line(EventText(trace[i]), 70, -270 - (i - first) * 30, 760, 18);
            }

            // rechts: viele Tage, Formeln, Vereinfachungen
            const float x = 860;
            if (_lastSummary != null)
            {
                var s = _lastSummary;
                int n = s.Days.Length;
                Line(Data.Tech("mg3TechReplications", n, _level.Seed, Replications.DaySeed(_level.Seed, n - 1), s.MeanDelivered, s.SdDelivered), x, -235, 660);
                Line(Data.Tech("mg3TechCi", s.Lower, s.MeanDelivered + s.HalfWidth, _level.Target), x, -300, 660);
            }
            else Line(Data.Tech("mg3TechNoDays"), x, -235, 660);
            Line(Data.Tech("mg3TechFormulas"), x, -380, 660, 20, FontStyle.Bold);
            void Formula(string name, float y, float scale)
            {
                var f = UIKit.Formula(page, name, ink, scale).rectTransform;
                f.anchorMin = f.anchorMax = new Vector2(0, 1);
                f.anchoredPosition = new Vector2(x + 10, y);
            }
            Formula("mg3_balance", -440, 0.45f);
            Formula("mg3_next_event", -500, 0.4f);
            Formula("mg3_ci", -575, 0.45f);
            Formula("mg3_little", -645, 0.45f);
            Line(Data.Tech("mg3TechSimplified", _level.RobotParking + 1), x, -700, 640, 18, FontStyle.Italic);
        }

        private string EventText(SimEvent e)
        {
            switch (e.Kind)
            {
                case EventKind.Kutter: return Data.Tech("mg3TechEventKutter", e.Time, e.A);
                case EventKind.Train: return Data.Tech("mg3TechEventTrain", e.Time, e.A);
                case EventKind.Done: return Data.Tech("mg3TechEventDone", e.Time, e.A, e.B);
                case EventKind.Arrive: return Data.Tech("mg3TechEventArrive", e.Time, e.A, e.B);
                case EventKind.RobotFree: return Data.Tech("mg3TechEventRobot", e.Time);
                case EventKind.Fail: return Data.Tech("mg3TechEventFail", e.Time, e.A, e.B);
                default: return Data.Tech("mg3TechEventRepair", e.Time, e.A, e.B);
            }
        }
    }
}
