using System.Collections;
using System.Collections.Generic;
using SFE.SimCore.Data;
using SFE.SimCore.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace SFE.Game
{
    /// <summary>
    /// MG2 „Polar-Express“ (Strömung, Siemens Mobility) nach dem Muster von MG1 (<see cref="SettlementMinigame"/>):
    /// Kalles Versuch draußen in echt (<see cref="PolarExpressOutdoor"/>) → im Labor ZWILLIS Windkanal mit
    /// 1 Was ist wichtig? (Karten) · 2 Welche Regel gilt? (Hand aus dem Autofenster, dann die Rechentafel „Fünf Kästchen
    /// Luft“, <see cref="RowLessonView"/>) · 3 Alles hängt zusammen – „Genau hinschauen kostet Zeit“ (grob/mittel/fein) ·
    /// 4 Der Computer probiert's aus (Kalles Zug, Foto von draußen daneben, Erklärung der Rechnung) ·
    /// 5 Besser machen (Nase mit vier Ziehpunkten formen, Übergänge verkleiden, 3 Runden, Ampel + Sterne).
    /// Schritt 6 „So macht's Siemens“: <see cref="PolarExpressTransfer.Play"/>.
    /// Danach fährt Pips Zug draußen davon. Modell: <see cref="TrainRun"/> (Lattice-Boltzmann, Golden <c>flow_*</c>).
    /// </summary>
    public sealed class PolarExpressMinigame : MonoBehaviour
    {
        /// <summary>Die Tundra draußen liegt hier relativ zum Labor (eigener Ort, weit weg vom Hub).</summary>
        private static readonly Vector3 OutdoorOffset = new Vector3(0f, 0f, 120f);
        /// <summary>Zeitraffer: ganzer Lauf höchstens so lange (docs/03), Rechenbudget je Bild mindestens so viel.</summary>
        private const float RunSeconds = 9f;
        private const double FrameBudgetMs = 4.0;

        /// <summary>Kalles Versuch draußen überspringen (z. B. wenn der Hub ihn schon gezeigt hat).</summary>
        public bool SkipOutdoor;

        private GameRoot _root;
        private PolarExpressLevel _lv;
        private Dictionary<string, object> _content;
        private FlowBoard _board;
        private Hologram _zwilli;
        private GameObject _ui;
        private Mg2Progress _progress;

        // Schritt 5
        private TrainShape _shape;
        private readonly Stack<TrainShape> _undo = new Stack<TrainShape>();
        private bool _editing;
        private int _drag = -1;
        private TrainShape _beforeDrag;
        private Button[] _transitionButtons;
        private Button _simulateButton, _exampleButton;
        private Text _roundLabel, _seatsText;
        private Image _seatsLamp;
        private GameObject _infoPanel;
        private int _round;
        private int _runBest = -1;
        private bool _successShown, _oneStarShown, _solutionOffered, _finished;
        private TrainResult _previous;
        private TrainRun _lastRun;

        public static PolarExpressMinigame Create(GameRoot root, Vector3 origin)
        {
            var go = new GameObject("MG2_PolarExpress");
            go.transform.position = origin;
            var m = go.AddComponent<PolarExpressMinigame>();
            m._root = root;
            m._lv = PolarExpressLevel.FromJson(GameData.Text("Data/Levels/MG2_PolarExpress"));
            m._content = root.Data.MinigameContent("mg2");
            return m;
        }

        private Hud Hud => _root.Hud;
        private GameData Data => _root.Data;
        private Camera Cam => _root.Rig.Cam;
        private FlowResolution PlayRes => _lv.Resolution(_lv.PlayResolution);

        public IEnumerator Play()
        {
            _progress = Mg2Progress.Of(_root.State);
            Hud.SetPhaseTitles(_content.Arr("phases"));

            // Kalles Versuch – in echt, draußen in der Tundra. Danach geht es ins Labor zu ZWILLIS Windkanal.
            if (!SkipOutdoor)
            {
                var outdoor = PolarExpressOutdoor.Create(_root, transform.position + OutdoorOffset);
                yield return outdoor.PlayKalleAttempt(_progress);
                Destroy(outdoor.gameObject);
            }

            _board = FlowBoard.Create(transform, transform.position, _lv, Data.Ui("mg2BoardScreen"), Data.Ui("mg2BoardWind"));
            BuildLab();
            BoardView(snap: true);
            yield return Hud.FadeIn();
            yield return Hud.Say("mg2_intro");
            _root.Rig.ZoomEnabled = true;
            Hud.Toast(Data.Ui("zoomHint"), 6f);

            yield return PhaseCards();
            yield return PhaseRule();
            yield return PhaseResolution();
            yield return PhaseSimulateKalle();
            yield return PhaseBuild();

            yield return Hud.FadeOut();
            Hud.ShowPhases(-1);
            _root.Rig.ZoomEnabled = false;
            _root.Rig.ResetZoom();
            if (_ui != null) Destroy(_ui);
            _board.gameObject.SetActive(false);

            // In echt: Pips Zug fährt durch den Gegenwind davon
            if (_progress.BestResult != null)
            {
                var outdoor = PolarExpressOutdoor.Create(_root, transform.position + OutdoorOffset);
                yield return outdoor.PlayPlayerTrain(_progress.BestShape, _lv, _progress.BestResult);
                Destroy(outdoor.gameObject);
            }
            _progress.Done = true;
            Destroy(gameObject);
        }

        private void BuildLab()
        {
            var t = transform;
            // Labortisch unter dem Bildschirm
            Prims.Make(PrimitiveType.Cube, t, new Vector3(_board.Width / 2f, -0.8f, _board.Depth / 2f),
                new Vector3(_board.Width + 4f, 0.6f, _board.Depth + 4f), Palette.Desk, "Desk");
            void Stand(GameObject p, float x, string speaker)
            {
                p.transform.localPosition = new Vector3(x, 0, -2.0f);
                p.transform.localRotation = Quaternion.Euler(0, 180, 0);
                if (speaker != null && Data.Speakers.TryGetValue(speaker, out var sp))
                {
                    var label = Prims.Label(p.transform, sp.Name, new Vector3(0, 1.15f, 0), 0.022f, sp.Color);
                    label.fontStyle = FontStyle.Bold;
                }
            }
            Stand(Prims.Penguin(t, "Tilda", Prims.Look.Plain, 1.2f), 2.0f, "tilda");
            Stand(Prims.Penguin(t, "Frieda", Prims.Look.Frieda, 1.2f), 4.0f, null);
            Stand(Prims.Penguin(t, "Kalle", Prims.Look.Kalle, 1.2f), 6.0f, null);
            var zw = Hologram.Create(t, 1.1f);
            zw.transform.position = transform.position + new Vector3(8.2f, 0, -2.0f);
            zw.transform.rotation = Quaternion.Euler(0, 180, 0);
            _zwilli = zw.GetComponent<Hologram>();
            SetGlitch(0.5f);
        }

        /// <summary>ZWILLI stottert weniger, je weiter der Spieler kommt (aus MG1 schon klüger: Start bei 0,5).</summary>
        private void SetGlitch(float g)
        {
            Hud.ZwilliGlitch = g;
            if (_zwilli != null) _zwilli.Glitch = g;
        }

        /// <summary>
        /// Bildschirm links oben, rechts eine Spalte für die Tafeln (Referenz 1920 × 1080: Kanal ~x 100…1300, y 210…650;
        /// Tafeln rechts x 1350…1890, y 200…820; unten Sprechblase und Werkzeugleiste).
        /// </summary>
        private void BoardView(bool snap = false)
        {
            float visibleWidth = 1.6f * _board.Width;
            float dist = _root.Rig.DistanceToFit(visibleWidth / Mathf.Max(1f, Cam.aspect), 1f);
            var focus = _board.Center + new Vector3(0.217f * _board.Width, 0, -0.27f * _board.Depth);
            _root.Rig.LookAt(focus, 62f, dist, 0f, snap);
        }

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

        /// <summary>Tafel rechts neben dem Bildschirm (überdeckt den Kanal nicht).</summary>
        private Image SidePanel(Transform parent, string name, float height = 620f, Color? color = null)
        {
            var p = UIKit.Panel(parent, color ?? Palette.PanelLight, name);
            UIKit.PlaceFixed(p.rectTransform, new Vector2(1, 0.5f), new Vector2(-300, 30 + (620f - height) / 2f), new Vector2(540, height));
            return p;
        }

        // ---------- Schritt 1: Was ist wichtig? ----------

        private IEnumerator PhaseCards()
        {
            Hud.ShowPhases(0);
            yield return Hud.Say("mg2_phase1_intro");
            var layer = NewUiLayer("Cards");
            var panel = UIKit.Panel(layer, Palette.Panel, "CardPanel");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1200, 560));
            var title = UIKit.Label(panel.transform, Data.Ui("mg2CardsTitle"), 36, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 20, -80, 20, 10);

            var cards = _content.Arr("cards");
            var selected = new bool[cards.Count];
            var images = new Image[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                var c = (Dictionary<string, object>)cards[i];
                int idx = i;
                Button b = null;
                b = UIKit.Button(panel.transform, c.Str("text"), () =>
                {
                    selected[idx] = !selected[idx];
                    images[idx].color = selected[idx] ? Palette.Button : Palette.ButtonAlt;
                    UIKit.SetButtonLabel(b, selected[idx] ? Data.Ui("cardsChecked", c.Str("text")) : c.Str("text"));
                    Sfx.Play(Sfx.Sound.Click);
                }, Palette.ButtonAlt, 30);
                images[i] = b.GetComponent<Image>();
                int col = i % 3, row = i / 3;
                float rowWidth = row == 0 ? 3 : cards.Count - 3;
                float x = (col - (rowWidth - 1) / 2f) * 370f;
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(x, 70 - row * 170), new Vector2(340, 140));
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
            Hud.LockWhileBlocking(panel);
            while (!done) yield return null;
            Destroy(_ui);
            Sfx.Play(Sfx.Sound.Success, 0.5f);
            AddGlossary("modelling");
            yield return Hud.Say("mg2_phase1_done");
        }

        // ---------- Schritt 2: Welche Regel gilt? ----------

        /// <summary>„Halt die Hand aus dem Autofenster“: Wo drückt der Wind stärker? Danach die Rechentafel.</summary>
        private IEnumerator PhaseRule()
        {
            Hud.ShowPhases(1);
            yield return Hud.Say("mg2_phase2_intro");
            var layer = NewUiLayer("Hand");
            var panel = UIKit.Panel(layer, Palette.Panel, "HandPanel");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1300, 600));
            var hand = _content.Obj("hand");
            var title = UIKit.Label(panel.transform, hand.Str("question"), 34, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 20, -90, 20, 10);

            var options = hand.Arr("options");
            bool done = false;
            for (int i = 0; i < options.Count; i++)
            {
                var o = (Dictionary<string, object>)options[i];
                float x = (i - (options.Count - 1) / 2f) * 560f;
                var card = UIKit.Button(panel.transform, "", null, Palette.ButtonAlt, 28);
                UIKit.PlaceFixed((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(x, -20), new Vector2(500, 380));
                var label = card.GetComponentInChildren<Text>();
                label.text = o.Str("text");
                label.alignment = TextAnchor.LowerCenter;
                DrawHand(card.transform, (float)o.Num("angle"));
                card.onClick.AddListener(() =>
                {
                    if (done) return;
                    Hud.Toast(o.Str("feedback"), 4.5f);
                    if ((bool)o["correct"])
                    {
                        card.GetComponent<Image>().color = Palette.Good;
                        Sfx.Play(Sfx.Sound.Success, 0.5f);
                        done = true;
                    }
                    else
                    {
                        card.GetComponent<Image>().color = Palette.ButtonOff;
                        Sfx.Play(Sfx.Sound.Wrong, 0.5f);
                    }
                });
            }
            Hud.LockWhileBlocking(panel);
            while (!done) yield return null;
            yield return new WaitForSeconds(1.2f);
            Destroy(_ui);
            AddGlossary("equation");
            yield return Hud.Say("mg2_phase2_done");

            // Die Regel in Zahlen: fünf Kästchen Luft, Schritt für Schritt
            _board.gameObject.SetActive(false);
            yield return RowLessonView.Play(_root, _lv);
            _board.gameObject.SetActive(true);
            AddGlossary("timestep");
        }

        /// <summary>Eine Hand (Balken) im Wind (drei Pfeile von links), flach oder schräg gehalten.</summary>
        private static void DrawHand(Transform parent, float angle)
        {
            for (int k = 0; k < 3; k++)
            {
                var shaft = UIKit.Panel(parent, Palette.Mg2ArrowRight, "Wind");
                shaft.raycastTarget = false;
                UIKit.PlaceFixed(shaft.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-150, 90 - k * 55), new Vector2(110, 10));
                var head = UIKit.Panel(parent, Palette.Mg2ArrowRight, "Head");
                head.raycastTarget = false;
                UIKit.PlaceFixed(head.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-95, 90 - k * 55), new Vector2(20, 20));
                head.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            var palm = UIKit.Panel(parent, Palette.Mg2Handle, "Hand");
            palm.raycastTarget = false;
            UIKit.PlaceFixed(palm.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40, 35), new Vector2(170, 34));
            palm.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        // ---------- Schritt 3: Alles hängt zusammen – „Genau hinschauen kostet Zeit“ ----------

        private sealed class ResolutionTry
        {
            public double Kalle, Round, Seconds;
        }

        private IEnumerator PhaseResolution()
        {
            Hud.ShowPhases(2);
            SetGlitch(0.4f);
            var kalle = _lv.Shapes["kalle"];
            var round = _lv.Shapes["start"];
            _board.ShowShape(round);
            BoardView();
            yield return Hud.Say("mg2_phase3_intro");

            var layer = NewUiLayer("Resolution");
            var panel = SidePanel(layer, "ResolutionPanel");
            var title = UIKit.Label(panel.transform, Data.Ui("mg2ResTitle"), 28, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.PlaceFixed(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(520, 44));
            Hud.LockWhileBlocking(panel);

            var tries = new Dictionary<string, ResolutionTry>();
            var fein = _lv.Resolution("fein");
            long maxWork = new TrainRun(round, _lv, fein).Work;
            var rows = new Dictionary<string, Text>();
            bool running = false, cont = false;
            string requested = null;
            for (int i = 0; i < _lv.Resolutions.Count; i++)
            {
                var r = _lv.Resolutions[i];
                float y = -110 - i * 120;
                var b = UIKit.Button(panel.transform, Data.Ui("mg2Res_" + r.Id), () => { if (!running) requested = r.Id; }, Palette.ButtonAlt, 24);
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0, 1), new Vector2(85, y), new Vector2(140, 90));
                // Wie genau? (Kästchen über die Zughöhe) · Wie lange rechnet's? (Kästchen mal Schritte)
                var probe = new TrainRun(round, _lv, r);
                float accuracy = (float)(r.Scale / fein.Scale), cost = (float)probe.Work / maxWork;
                Meter(panel.transform, 175, y + 22, Data.Ui("mg2ResAccuracy", (int)System.Math.Round(_lv.Height * r.Scale)), accuracy, Palette.Good);
                Meter(panel.transform, 175, y - 18, Data.Ui("mg2ResCost"), cost, Palette.Warn);
                var row = UIKit.Label(panel.transform, "", 19, Palette.TextMuted, TextAnchor.UpperLeft);
                UIKit.PlaceFixed(row.rectTransform, new Vector2(0, 1), new Vector2(175 + 175, y - 52), new Vector2(350, 30));
                rows[r.Id] = row;
            }
            var compare = UIKit.Label(panel.transform, Data.Ui("mg2ResPick"), 21, Palette.Text, TextAnchor.UpperLeft);
            UIKit.PlaceFixed(compare.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(500, 100));
            var next = UIKit.Button(panel.transform, Data.Ui("next"), () => cont = true, Palette.Button, 26);
            UIKit.PlaceFixed((RectTransform)next.transform, new Vector2(0.5f, 0), new Vector2(0, 55), new Vector2(260, 70));
            next.gameObject.SetActive(false);

            while (!cont)
            {
                if (requested == null) { yield return null; continue; }
                string id = requested;
                requested = null;
                running = true;
                var res = _lv.Resolution(id);
                _board.SetResolution(res);
                _board.ShowCells = id != "fein";
                double computeMs = 0;
                var t = new ResolutionTry();
                foreach (var shape in new[] { kalle, round })
                {
                    var run = new TrainRun(shape, _lv, res);
                    yield return RunAnimated(run, 5f, 12.0, ms => computeMs += ms);
                    if (shape == kalle) t.Kalle = run.Drag; else t.Round = run.Drag;
                }
                t.Seconds = computeMs / 1000.0;
                tries[id] = t;
                rows[id].text = Data.Ui("mg2ResRow", t.Kalle, t.Round, t.Seconds);
                double diff = (t.Kalle - t.Round) / t.Kalle * 100.0;
                compare.text = diff < 0.5 ? Data.Ui("mg2ResSame", Data.Ui("mg2Res_" + id)) : Data.Ui("mg2ResDiff", Data.Ui("mg2Res_" + id), diff);
                Sfx.Play(Sfx.Sound.Ding, 0.5f);
                yield return Hud.Say("mg2_phase3_" + id);
                _board.ShowShape(round);
                running = false;
                if (tries.Count >= 2 && !next.gameObject.activeSelf)
                {
                    next.gameObject.SetActive(true);
                    Hud.Toast(Data.Ui("mg2ResEnough"), 4f);
                }
            }
            AddGlossary("assembly");
            yield return Hud.Say("mg2_phase3_done");
            _board.ShowCells = false;
            _board.SetResolution(PlayRes);
            Destroy(_ui);
        }

        private static void Meter(Transform parent, float x, float y, string label, float value, Color color)
        {
            var back = UIKit.Panel(parent, Palette.StarOff, "MeterBack");
            back.rectTransform.anchorMin = back.rectTransform.anchorMax = new Vector2(0, 1);
            back.rectTransform.pivot = new Vector2(0, 0.5f);
            back.rectTransform.anchoredPosition = new Vector2(x, y);
            back.rectTransform.sizeDelta = new Vector2(150, 18);
            var bar = UIKit.Panel(back.transform, color, "Meter");
            bar.rectTransform.anchorMin = new Vector2(0, 0);
            bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp(value, 0.03f, 1f), 1);
            bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
            var t = UIKit.Label(parent, label, 19, Palette.Text, TextAnchor.MiddleLeft);
            UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(x + 160 + 100, y), new Vector2(200, 30));
        }

        /// <summary>
        /// Zeitraffer einer Rechnung: so viele Schritte je Bild, wie ins Budget passen (mindestens
        /// <see cref="FrameBudgetMs"/>, mehr nur, wenn der Lauf sonst länger als <paramref name="seconds"/> dauerte).
        /// Das Ergebnis hängt nicht davon ab, wie die Schritte auf Bilder verteilt werden.
        /// </summary>
        private IEnumerator RunAnimated(TrainRun run, float seconds, double minBudgetMs, System.Action<double> onComputeMs = null)
        {
            _board.ShowRun(run);
            float start = Time.realtimeSinceStartup;
            double perStep = 1.0;
            var sw = new System.Diagnostics.Stopwatch();
            while (!run.IsConverged)
            {
                float left = Mathf.Max(0.3f, seconds - (Time.realtimeSinceStartup - start));
                int stepsLeft = run.TotalSteps - run.Iteration;
                double frames = left * 60.0;
                double budget = System.Math.Max(minBudgetMs, stepsLeft * perStep / frames);
                sw.Restart();
                int done = 0;
                do { run.Step(1); done++; } while (!run.IsConverged && sw.Elapsed.TotalMilliseconds < budget);
                double ms = sw.Elapsed.TotalMilliseconds;
                perStep = 0.7 * perStep + 0.3 * (ms / done);
                onComputeMs?.Invoke(ms);
                _board.Tick(Time.deltaTime);
                yield return null;
            }
            // Noch einen Augenblick Strömungslinien zeigen
            for (float t = 0; t < 0.6f; t += Time.deltaTime) { _board.Tick(Time.deltaTime); yield return null; }
            _lastRun = run;
        }

        // ---------- Schritt 4: Der Computer probiert's aus ----------

        private IEnumerator PhaseSimulateKalle()
        {
            Hud.ShowPhases(3);
            SetGlitch(0.3f);
            _board.ShowShape(_lv.Shapes["kalle"]);
            BoardView();
            yield return Hud.Say("mg2_phase4_intro");

            var layer = NewUiLayer("SimulateKalle");
            bool go = false;
            var b = UIKit.Button(layer, Data.Ui("simulate"), () => go = true, Palette.Button, 36);
            UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(360, 100));
            Hud.LockWhileBlocking(b);
            while (!go) yield return null;
            b.gameObject.SetActive(false);

            var run = new TrainRun(_lv.Shapes["kalle"], _lv, PlayRes);
            yield return RunAnimated(run, RunSeconds, FrameBudgetMs);
            var r = run.Result();
            ShowKallePhoto(layer, r);
            AddGlossary("solution");
            yield return Hud.Say("mg2_phase4_done");

            // Was hat der Computer gemacht? Brücke von der Rechentafel zum ganzen Kanal
            Hud.Toast(Data.Ui("mg2ExplainNumbers", (PlayRes.Nx * PlayRes.Ny).ToString("N0", GameData.German),
                run.TotalSteps.ToString("N0", GameData.German),
                ((long)PlayRes.Nx * PlayRes.Ny * run.TotalSteps).ToString("N0", GameData.German)), 8f);
            var tech = UIKit.Button(layer, Data.Ui("techButton"), () => ShowModelFormula(run), Palette.ButtonQuiet, 22);
            UIKit.PlaceFixed((RectTransform)tech.transform, new Vector2(1, 0.5f), new Vector2(-300, -180), new Vector2(320, 56));
            Hud.TechOnly(tech.gameObject);
            Hud.LockWhileBlocking(tech);
            yield return Hud.Say("mg2_explain");
            bool cont = false;
            var next = UIKit.Button(layer, Data.Ui("next"), () => cont = true, Palette.Button, 30);
            UIKit.PlaceFixed((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(300, 90));
            Hud.LockWhileBlocking(next);
            while (!cont) yield return null;
            Destroy(_ui);
        }

        /// <summary>Rechts: das Foto von draußen (in echt) über ZWILLIS Ergebnis für denselben Zug.</summary>
        private void ShowKallePhoto(RectTransform layer, TrainResult r)
        {
            var frame = SidePanel(layer, "KallePhoto", 480f);
            var photo = _progress.KallePhoto;
            if (photo != null)
            {
                var img = new GameObject("Image").AddComponent<RawImage>();
                UIKit.Rect(img.gameObject, frame.transform);
                UIKit.Place(img.rectTransform, 0, 0, 1, 1, 14, 150, 14, 60);
                img.texture = photo;
            }
            var cap = UIKit.Label(frame.transform, Data.Ui("mg2KallePhotoCaption"), 22, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(cap.rectTransform, 0, 1, 1, 1, 10, -54, 10, 6);
            var verdict = UIKit.Label(frame.transform, Data.Ui("mg2KalleVerdict", r.Time), 22, Palette.Text, TextAnchor.MiddleCenter);
            UIKit.Place(verdict.rectTransform, 0, 0, 1, 0, 16, 20, 16, -130);
            Sfx.Play(Sfx.Sound.Click, 0.5f);
        }

        // ---------- Schritt 5: Besser machen ----------

        private IEnumerator PhaseBuild()
        {
            Hud.ShowPhases(4);
            SetGlitch(0.15f);
            _shape = _lv.Shapes["start"].Clone();
            _board.ShowShape(_shape);
            _board.ShowHandles(true);
            BoardView();
            yield return Hud.Say("mg2_phase5_intro");
            BuildToolbar();
            _editing = true;
            while (!_finished)
            {
                if (_editing) HandleDrag();
                yield return null;
            }
            _board.ShowHandles(false);
            AddGlossary("optimization");
            yield return Hud.Say("mg2_phase5_done");
        }

        private void BuildToolbar()
        {
            var layer = NewUiLayer("BuildUI");
            var bar = UIKit.Panel(layer, Palette.Panel, "Toolbar");
            UIKit.Place(bar.rectTransform, 0, 0, 1, 0, 0, 0, 0, -130);
            Hud.LockWhileBlocking(bar);
            _transitionButtons = new Button[_shape.Covered.Length];
            for (int i = 0; i < _transitionButtons.Length; i++)
            {
                int idx = i;
                var b = UIKit.Button(bar.transform, "", () => ToggleTransition(idx), Palette.ButtonAlt, 22);
                UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0, 0.5f), new Vector2(115 + i * 215, 0), new Vector2(200, 96));
                _transitionButtons[i] = b;
            }
            int k = _transitionButtons.Length;
            var undo = UIKit.Button(bar.transform, Data.Ui("undo"), Undo, Palette.ButtonQuiet, 24);
            UIKit.PlaceFixed((RectTransform)undo.transform, new Vector2(0, 0.5f), new Vector2(115 + k * 215, 0), new Vector2(200, 80));
            var reset = UIKit.Button(bar.transform, Data.Ui("mg2ResetTrain"), ResetShape, Palette.ButtonQuiet, 22);
            UIKit.PlaceFixed((RectTransform)reset.transform, new Vector2(0, 0.5f), new Vector2(115 + (k + 1) * 215, 0), new Vector2(200, 80));
            var tech = UIKit.Button(bar.transform, Data.Ui("techButton"), () => ShowModelFormula(_lastRun), Palette.ButtonQuiet, 20);
            UIKit.PlaceFixed((RectTransform)tech.transform, new Vector2(0, 0.5f), new Vector2(115 + (k + 2) * 215, 0), new Vector2(200, 80));
            Hud.TechOnly(tech.gameObject);
            _simulateButton = UIKit.Button(bar.transform, Data.Ui("simulate"), () => StartCoroutine(SimulateRound()), Palette.Button, 34);
            UIKit.PlaceFixed((RectTransform)_simulateButton.transform, new Vector2(1, 0.5f), new Vector2(-190, 0), new Vector2(330, 100));
            _roundLabel = UIKit.Label(bar.transform, "", 26, Palette.TextLight);
            UIKit.PlaceFixed(_roundLabel.rectTransform, new Vector2(1, 0.5f), new Vector2(-470, 0), new Vector2(220, 90));

            // Beispiel-Zug erst nach Runde 3 ohne Stern – und nur auf Wunsch (wie MG1 UX-09)
            _exampleButton = UIKit.Button(layer, Data.Ui("mg2LoadExample"), LoadExample, Palette.ButtonSide, 24);
            UIKit.PlaceFixed((RectTransform)_exampleButton.transform, new Vector2(1, 0), new Vector2(-300, 175), new Vector2(320, 64));
            _exampleButton.gameObject.SetActive(false);

            // Rechts: Plätze im Zug (ändern sich beim Formen sofort) und kurze Anleitung
            _infoPanel = SidePanel(layer, "TrainInfo", 360f).gameObject;
            var info = (RectTransform)_infoPanel.transform;
            var title = UIKit.Label(info, Data.Ui("mg2InfoTitle"), 28, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 10, -60, 10, 10);
            _seatsLamp = UIKit.Panel(info, Palette.Good, "Lamp");
            UIKit.PlaceFixed(_seatsLamp.rectTransform, new Vector2(0, 1), new Vector2(60, -110), new Vector2(54, 54));
            _seatsText = UIKit.Label(info, "", 26, Palette.Text, TextAnchor.MiddleLeft);
            UIKit.PlaceFixed(_seatsText.rectTransform, new Vector2(0, 1), new Vector2(310, -110), new Vector2(420, 60));
            var how = UIKit.Label(info, Data.Ui("mg2InfoHow"), 21, Palette.TextMuted, TextAnchor.UpperLeft);
            UIKit.Place(how.rectTransform, 0, 0, 1, 1, 30, 130, 30, 150);
            var legend = _content.Obj("legend");
            AddLegendRow(info, 0, Palette.Mg2AirFast, legend.Str("fast"));
            AddLegendRow(info, 1, Palette.Mg2Swirl, legend.Str("swirl"));
            AddLegendRow(info, 2, Palette.Mg2TransitionOpen, legend.Str("open"));
            UpdateToolbar();
        }

        private static void AddLegendRow(RectTransform parent, int row, Color color, string text)
        {
            var sw = UIKit.Panel(parent, color, "Swatch");
            UIKit.PlaceFixed(sw.rectTransform, new Vector2(0, 0), new Vector2(50, 110 - row * 38), new Vector2(34, 26));
            var t = UIKit.Label(parent, text, 20, Palette.Text, TextAnchor.MiddleLeft);
            UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 0), new Vector2(290, 110 - row * 38), new Vector2(440, 34));
        }

        private void UpdateToolbar()
        {
            for (int i = 0; i < _transitionButtons.Length; i++)
            {
                bool cov = _shape.Covered[i];
                UIKit.SetButtonLabel(_transitionButtons[i], Data.Ui(cov ? "mg2TransitionCovered" : "mg2TransitionOpen", i + 1));
                _transitionButtons[i].GetComponent<Image>().color = cov ? Palette.Button : Palette.ButtonAlt;
            }
            int seats = TrainGeometry.Seats(_shape, _lv);
            bool enough = seats >= _lv.Passengers;
            _seatsText.text = Data.Ui("mg2InfoSeats", seats, _lv.Passengers);
            _seatsLamp.color = enough ? Palette.Good : Palette.Bad;
            int shown = _round + 1;
            _roundLabel.text = shown <= _lv.Rounds ? Data.Ui("round", shown, _lv.Rounds) : Data.Ui("extraRound", shown);
        }

        /// <summary>Ziehpunkte der Nase: antippen und ziehen. Ein Zug ist ein Rückgängig-Schritt.</summary>
        private void HandleDrag()
        {
            if (Hud.Blocking) return;
            if (_drag < 0)
            {
                if (!InputHelper.PointerDown || InputHelper.PointerOverUI) return;
                if (!_board.PointerToChannel(Cam, out double x, out double y)) return;
                _drag = _board.HandleNear(x, y);
                if (_drag < 0) return;
                _beforeDrag = _shape.Clone();
                _board.SetActiveHandle(_drag);
                Sfx.Play(Sfx.Sound.Click, 0.5f);
                return;
            }
            if (InputHelper.PointerHeld)
            {
                if (!_board.PointerToChannel(Cam, out double x, out double y)) return;
                var s = _shape.Clone();
                // P0 sitzt auf der Stirn (nur die Höhe), P3 auf dem Dach (nur die Länge)
                if (_drag != 0) s.X[_drag] = x;
                if (_drag != 3) s.Y[_drag] = y;
                s = s.Clamped(_lv);
                if (!s.SameAs(_shape))
                {
                    _shape = s;
                    _board.ShowShape(_shape);
                    UpdateToolbar();
                }
                return;
            }
            // losgelassen
            if (_beforeDrag != null && !_beforeDrag.SameAs(_shape)) { _undo.Push(_beforeDrag); Sfx.Play(Sfx.Sound.Plop, 0.4f); }
            _beforeDrag = null;
            _drag = -1;
            _board.SetActiveHandle(-1);
        }

        private void Change(TrainShape next)
        {
            if (!_editing || next.SameAs(_shape)) return;
            _undo.Push(_shape);
            _shape = next;
            _board.ShowShape(_shape);
            UpdateToolbar();
        }

        private void ToggleTransition(int i)
        {
            var s = _shape.Clone();
            s.Covered[i] = !s.Covered[i];
            Change(s);
            Sfx.Play(s.Covered[i] ? Sfx.Sound.Plop : Sfx.Sound.Erase, 0.5f);
        }

        private void ResetShape() => Change(_lv.Shapes["start"].Clone());

        private void LoadExample()
        {
            Change(_lv.Shapes["hint"].Clone());
            Hud.Toast(Data.Ui("mg2ExampleLoaded"), 4.5f);
            _exampleButton.gameObject.SetActive(false);
        }

        private void Undo()
        {
            if (!_editing || _undo.Count == 0) return;
            _shape = _undo.Pop();
            _board.ShowShape(_shape);
            Sfx.Play(Sfx.Sound.Erase, 0.4f);
            UpdateToolbar();
        }

        private IEnumerator SimulateRound()
        {
            if (!_editing || _drag >= 0) yield break;
            _editing = false;
            _simulateButton.interactable = false;
            _board.ShowHandles(false);
            var planned = _shape.Clone();
            var run = new TrainRun(planned, _lv, PlayRes);
            yield return RunAnimated(run, RunSeconds, FrameBudgetMs);
            var r = run.Result();
            _round++;
            for (int s = 0; s < r.Stars; s++) { Sfx.Play(Sfx.Sound.Ding, 0.5f); yield return new WaitForSeconds(0.18f); }
            if (r.Stars >= 2) Sfx.Play(Sfx.Sound.Success, 0.5f);
            KeepIfBest(r, planned);
            _runBest = Mathf.Max(_runBest, r.Stars);

            _infoPanel.SetActive(false);
            var panel = ShowResult(r);
            yield return new WaitForSeconds(0.3f);
            if (r.Stars >= 2 && !_successShown)
            {
                _successShown = true;
                yield return Hud.Say(r.Stars == 3 ? "mg2_phase5_three_stars" : "mg2_phase5_success");
            }
            else if (r.Stars == 1 && !_oneStarShown && !_successShown)
            {
                _oneStarShown = true;
                yield return Hud.Say("mg2_phase5_one_star");
            }
            else if (_runBest == 0 && _round == 2)
                yield return Hud.Say(r.OpenTransitions > 0 ? "mg2_phase5_hint_cover" : "mg2_phase5_hint_nose");
            else if (_runBest == 0 && _round >= _lv.Rounds && !_solutionOffered)
            {
                _solutionOffered = true;
                _exampleButton.gameObject.SetActive(true);
                yield return Hud.Say("mg2_phase5_solution");
            }

            bool closed = false;
            AddResultButtons(panel, () => closed = true, () => { closed = true; _finished = true; });
            while (!closed) yield return null;
            _previous = r;
            if (_finished) yield break;
            _infoPanel.SetActive(true);
            _board.ShowShape(_shape);
            _board.ShowHandles(true);
            UpdateToolbar();
            _simulateButton.interactable = true;
            _editing = true;
        }

        /// <summary>Es zählt die beste Runde – auch über „nochmal“ hinweg (wie MG1 F-02).</summary>
        private void KeepIfBest(TrainResult r, TrainShape planned)
        {
            if (!Travel.IsBetter(r, _progress.BestResult)) return;
            _progress.BestResult = r;
            _progress.BestShape = planned.Clone();
            if (_progress.Snapshot != null) Destroy(_progress.Snapshot);
            _progress.Snapshot = _board.Snapshot();
        }

        private RectTransform ShowResult(TrainResult r)
        {
            var panel = SidePanel(_ui.transform, "Result");
            var title = UIKit.Label(panel.transform, Data.Ui("resultTitle", _round), 32, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 10, -64, 10, 8);
            var lights = _content.Obj("lights");
            AddLight(panel.transform, 0, r.Time <= _lv.BestTime ? 2 : r.Time <= _lv.TargetTime ? 1 : 0,
                Data.Ui("mg2ResultTime", lights.Str("time"), r.Time, _lv.TargetTime));
            AddLight(panel.transform, 1, r.Seats >= _lv.Passengers ? 2 : 0, Data.Ui("mg2ResultSeats", lights.Str("seats"), r.Seats, _lv.Passengers));
            AddLight(panel.transform, 2, r.OpenTransitions == 0 ? 2 : 1,
                r.OpenTransitions == 0 ? Data.Ui("mg2ResultSmooth", lights.Str("smooth")) : Data.Ui("mg2ResultOpen", lights.Str("smooth"), r.OpenTransitions));
            if (_previous != null)
            {
                var cmp = UIKit.Label(panel.transform, CompareText(_previous, r), 21, Palette.Text, TextAnchor.UpperLeft);
                UIKit.PlaceFixed(cmp.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 285), new Vector2(500, 90));
            }
            for (int s = 0; s < 3; s++)
            {
                var star = UIKit.Panel(panel.transform, s < r.Stars ? Palette.Selected : Palette.StarOff, "Star");
                UIKit.PlaceFixed(star.rectTransform, new Vector2(0.5f, 0), new Vector2(-90 + s * 90, 205), new Vector2(50, 50));
                star.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            var starText = UIKit.Label(panel.transform, Data.Ui("mg2Stars" + Mathf.Clamp(r.Stars, 0, 3)), 22, Palette.Text);
            UIKit.PlaceFixed(starText.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(520, 44));
            return panel.rectTransform;
        }

        private void AddResultButtons(RectTransform panel, System.Action onNext, System.Action onFinish)
        {
            bool canFinish = _runBest >= 1 || _round >= _lv.Rounds;
            var nextB = UIKit.Button(panel, Data.Ui("mg2BuildOn"), () => { Destroy(panel.gameObject); onNext(); }, Palette.ButtonAlt, 24);
            UIKit.PlaceFixed((RectTransform)nextB.transform, new Vector2(0.5f, 0), new Vector2(canFinish ? -135 : 0, 65), new Vector2(250, 76));
            if (!canFinish) return;
            var fin = UIKit.Button(panel, Data.Ui("finish"), () => { Destroy(panel.gameObject); onFinish(); }, Palette.Button, 24);
            UIKit.PlaceFixed((RectTransform)fin.transform, new Vector2(0.5f, 0), new Vector2(135, 65), new Vector2(250, 76));
        }

        /// <summary>Unterschiede zur Vorrunde in Worten (ohne Pfeilzeichen, die die Schrift nicht hat).</summary>
        private string CompareText(TrainResult before, TrainResult now)
        {
            var parts = new List<string>();
            double dt = now.Time - before.Time;
            if (dt < -0.05) parts.Add(Data.Ui("mg2CompareFaster", -dt));
            else if (dt > 0.05) parts.Add(Data.Ui("mg2CompareSlower", dt));
            if (now.Seats > before.Seats) parts.Add(Data.Ui("mg2CompareSeatsMore", now.Seats - before.Seats));
            if (now.Seats < before.Seats) parts.Add(Data.Ui("mg2CompareSeatsLess", before.Seats - now.Seats));
            if (parts.Count == 0) parts.Add(Data.Ui("compareSame"));
            return Data.Ui("compareTitle", _round - 1) + " " + string.Join(", ", parts);
        }

        /// <summary>Ampel mit Farbe UND Zeichen (OK / ! / X), damit sie nicht nur über Farbe lesbar ist.</summary>
        private static void AddLight(Transform parent, int row, int level, string text)
        {
            var color = level == 2 ? Palette.Good : level == 1 ? Palette.Warn : Palette.Bad;
            var lamp = UIKit.Panel(parent, color, "Lamp");
            UIKit.PlaceFixed(lamp.rectTransform, new Vector2(0, 1), new Vector2(60, -125 - row * 72), new Vector2(60, 60));
            var sym = UIKit.Label(lamp.transform, level == 2 ? "OK" : level == 1 ? "!" : "X", 26, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(sym.rectTransform);
            var t = UIKit.Label(parent, text, 22, Palette.Text, TextAnchor.MiddleLeft);
            UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(310, -125 - row * 72), new Vector2(420, 70));
        }

        // ---------- Blick unter die Haube ----------

        /// <summary>Das ganze Modell: D2Q9-BGK, Ränder, Impulsaustausch, Fahrzeit – mit den Werten des letzten Laufs.</summary>
        private void ShowModelFormula(TrainRun run)
        {
            var page = Hud.OpenPopup(Data.Tech("mg2TechTitle"), new Vector2(1400, 960));
            var ink = Palette.Text;
            void Heading(string text, float y)
            {
                var t = UIKit.Label(page, text, 26, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(700, y), new Vector2(1280, 40));
            }
            void Formula(string name, float x, float y)
            {
                var f = UIKit.Formula(page, name, ink, 0.5f).rectTransform;
                f.anchorMin = f.anchorMax = new Vector2(0, 1);
                f.anchoredPosition = new Vector2(x, y);
            }
            void Line(string text, float y, int size = 20)
            {
                var t = UIKit.Label(page, text, size, ink, TextAnchor.MiddleLeft);
                UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(700, y), new Vector2(1280, 50));
            }
            var res = PlayRes;
            Heading(Data.Tech("mg2TechModel"), -120);
            Formula("mg2_bgk", 90, -180);
            Formula("mg2_drag", 90, -250);
            Line(Data.Tech("mg2TechBoundaries"), -310);
            Heading(Data.Tech("mg2TechTravel"), -370);
            Formula("mg2_travel", 90, -435);
            Line(Data.Tech("mg2TechTravelNote"), -495);
            Heading(Data.Tech("mg2TechNumbers"), -555);
            Line(Data.Tech("mg2TechGrid", res.Nx, res.Ny, _lv.Tau(res), _lv.Reynolds, _lv.StepsFor(res)), -605);
            if (run != null && run.IsConverged)
                Line(Data.Tech("mg2TechLastRun", run.Drag, run.Residual, run.Result().Speed, run.Result().Time), -655);
            else Line(Data.Tech("techNotYet"), -655);
            Line(Data.Tech("mg2TechHonest"), -735, 20);
            Line(Data.Tech("mg2TechHonest2"), -805, 20);
        }
    }
}
