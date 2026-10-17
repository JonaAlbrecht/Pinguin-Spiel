using System.Collections;
using System.Collections.Generic;
using SFE.SimCore.Data;
using SFE.SimCore.Thermal;
using UnityEngine;
using UnityEngine.UI;

namespace SFE.Game
{
    /// <summary>
    /// MG1 Teil A „Der letzte Huddle“ (Prolog/Tutorial).
    /// Entscheidungen für den Prototyp (docs/06): D-01 12 Pinguine · D-02 einzeln ziehen · D-03 der Balken
    /// „Alle warm“ füllt sich, solange niemand friert, und pausiert sonst (kein Zurücksetzen) ·
    /// D-04 Wind dreht alle 20 s mit 5 s Vorwarnung · D-05 kein Scheitern, Oma Grete hilft ·
    /// D-06 eigene Szene im selben Unity-Szenenobjekt · D-07 beim zweiten Durchlauf überspringbar.
    /// </summary>
    public sealed class PrologueHuddle : MonoBehaviour
    {
        private GameRoot _root;
        private HuddleLevel _level;
        private HuddleModel _model;
        private readonly List<GameObject> _penguins = new List<GameObject>();
        private Transform _windArrow;
        private readonly List<Transform> _flakes = new List<Transform>();
        private int _windIndex;
        private float _windTimer;
        private bool _warned;
        private double _windE, _windN;
        private float _accumulated;
        private float _progress;
        private int _dragging = -1;
        private float _frozenFor;
        private int _hintsGiven;
        private Image _barFill;
        private Text _barText;
        private GameObject _ui;
        private bool _running;

        public static PrologueHuddle Create(GameRoot root, Vector3 origin)
        {
            var go = new GameObject("MG1A_LastHuddle");
            go.transform.position = origin;
            var p = go.AddComponent<PrologueHuddle>();
            p._root = root;
            p._level = root.Data.Huddle;
            return p;
        }

        public IEnumerator Play()
        {
            BuildWorld();
            yield return PairLesson();
            BuildUi();
            _root.Rig.LookAt(transform.position, 62f, _root.Rig.DistanceToFit((float)_level.ArenaRadius * 2f + 2f), 0f);
            yield return _root.Hud.FadeIn();
            yield return _root.Hud.Say("prologue_intro");
            _running = true;
            while (_progress < _level.TargetSeconds)
            {
                if (_root.Hud.Blocking) { yield return null; continue; }
                yield return null;
            }
            _running = false;
            Sfx.Wind(0.15f);
            Sfx.Play(Sfx.Sound.Success);
            _root.Hud.Toast(_root.Data.Ui("prologueAllWarm"), 3f);
            foreach (var p in _penguins) p.AddComponent<Bobber>().Amplitude = 0.08f;
            yield return new WaitForSeconds(1.5f);
            _root.State.PrologueSnapshot = Snapshot();
            yield return _root.Hud.Say("prologue_done");
            yield return _root.Hud.FadeOut();
            Sfx.Wind(0f);
            Destroy(_ui);
            Destroy(gameObject);
        }

        private void BuildWorld()
        {
            float r = (float)_level.ArenaRadius;
            Prims.Make(PrimitiveType.Cylinder, transform, new Vector3(0, -0.25f, 0), new Vector3(r * 2f + 1f, 0.25f, r * 2f + 1f), Palette.Ice, "Floe");
            Prims.Make(PrimitiveType.Cylinder, transform, new Vector3(0, -0.6f, 0), new Vector3(70, 0.1f, 70), Palette.Sea, "Sea");

            _model = new HuddleModel(_level.Model, _level.Penguins, _level.StartTemp);
            var rnd = new System.Random(4);
            for (int i = 0; i < _level.Penguins; i++)
            {
                // zufällig verstreut, ohne Überlappung
                for (int attempt = 0; attempt < 200; attempt++)
                {
                    double a = rnd.NextDouble() * Mathf.PI * 2, d = 1.5 + rnd.NextDouble() * (_level.SpawnRadius - 1.5);
                    double x = System.Math.Cos(a) * d, y = System.Math.Sin(a) * d;
                    bool ok = true;
                    for (int j = 0; j < i; j++)
                        if ((x - _model.X[j]) * (x - _model.X[j]) + (y - _model.Y[j]) * (y - _model.Y[j]) < 1.4) ok = false;
                    if (!ok) continue;
                    _model.X[i] = x;
                    _model.Y[i] = y;
                    break;
                }
                var pg = Prims.Penguin(transform, "Penguin" + i);
                _penguins.Add(pg);
            }

            _windArrow = new GameObject("Wind").transform;
            _windArrow.SetParent(transform, false);
            var c = new Color(0.35f, 0.55f, 0.85f);
            Prims.Make(PrimitiveType.Cube, _windArrow, new Vector3(0, 0, -0.5f), new Vector3(0.3f, 0.12f, 1.8f), c, "Shaft");
            var head = Prims.Make(PrimitiveType.Cube, _windArrow, new Vector3(0, 0, 0.5f), new Vector3(0.8f, 0.12f, 0.8f), c, "Head");
            head.transform.localRotation = Quaternion.Euler(0, 45, 0);

            for (int i = 0; i < 70; i++)
            {
                var f = Prims.Make(PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one * 0.08f, Palette.Snow, "Flake");
                f.transform.localPosition = new Vector3(Random.Range(-r - 2, r + 2), Random.Range(0.3f, 2f), Random.Range(-r - 2, r + 2));
                _flakes.Add(f.transform);
            }
            SetWind(0);
            UpdateVisuals();
        }

        private void BuildUi()
        {
            _ui = new GameObject("PrologueUI");
            var rt = UIKit.Rect(_ui, _root.Hud.Root);
            UIKit.Fill(rt);
            var bar = UIKit.Panel(rt, new Color(0.1f, 0.14f, 0.22f, 0.85f), "Bar");
            UIKit.PlaceFixed(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(760, 64));
            _barFill = UIKit.Panel(bar.transform, HeatColors.Comfy, "Fill");
            UIKit.Place(_barFill.rectTransform, 0, 0, 0, 1, 4, 4, 0, 4);
            _barText = UIKit.Label(bar.transform, "", 28, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(_barText.rectTransform);
            var legend = UIKit.Label(rt, _root.Data.Ui("prologueLegend"), 24, Palette.Text, TextAnchor.MiddleCenter);
            UIKit.PlaceFixed(legend.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -112), new Vector2(1400, 36));
            if (_root.State.PrologueDone)
            {
                var skip = UIKit.Button(rt, _root.Data.Ui("prologueSkip"), () => _progress = (float)_level.TargetSeconds, Palette.ButtonAlt, 24);
                UIKit.PlaceFixed((RectTransform)skip.transform, new Vector2(1f, 1f), new Vector2(-130, -60), new Vector2(220, 56));
            }
        }

        // ---------- Zwei Pinguine: die Rechenregel vor dem Huddle ----------

        /// <summary>
        /// Erst die Regel, dann das System: Grete (warm, im Wind) und Pip (durchgefroren, in ihrem Windschatten)
        /// tauschen Wärme, Zeitschritt für Zeitschritt. Die Zahlen kommen aus demselben <see cref="HuddleModel"/>
        /// wie das Huddle danach (Golden <c>huddle_pair</c>) – nur mit längerem Zeitschritt, damit sie lesbar sind.
        /// Die Formel selbst steht nur im Modus „Blick unter die Haube“ (docs/00: keine Formeln im Standardspiel).
        /// </summary>
        private IEnumerator PairLesson()
        {
            var lesson = _level.PairLesson;
            if (lesson == null) yield break;
            var data = _root.Data;
            var model = lesson.CreateModel(_level.Model);
            foreach (var p in _penguins) p.SetActive(false);
            foreach (var f in _flakes) f.gameObject.SetActive(false);

            var pair = new[] { Prims.Penguin(transform, "Grete", Prims.Look.Grete, 1.3f), Prims.Penguin(transform, "Pip", Prims.Look.Pip, 1.3f) };
            string[] names = { data.Speakers["grete"].Name, data.Speakers["pip"].Name };
            var heads = new TextMesh[2];
            var wind = new Vector3((float)lesson.WindEast, 0, (float)lesson.WindNorth).normalized;
            for (int i = 0; i < 2; i++)
            {
                pair[i].transform.localPosition = new Vector3((float)model.X[i], 0, (float)model.Y[i]);
                pair[i].transform.localRotation = Quaternion.LookRotation(wind, Vector3.up); // Rücken zum Wind
                heads[i] = Prims.Label(pair[i].transform, "", new Vector3(0, 1.15f, 0), 0.03f, Palette.Text);
                heads[i].fontStyle = FontStyle.Bold;
            }
            _windArrow.localPosition = -wind * 1.9f + Vector3.up * 0.4f;
            _windArrow.localRotation = Quaternion.LookRotation(wind, Vector3.up);
            _root.Rig.LookAt(transform.position + new Vector3(0, 0, 0.9f), 48f, 7f, 0f);

            // Rechentafel oben: Bild-Gleichung + je eine Zeile für Grete und Pip
            var layer = new GameObject("PairLesson");
            var rt = UIKit.Rect(layer, _root.Hud.Root);
            UIKit.Fill(rt);
            rt.SetSiblingIndex(0);
            var panel = UIKit.Panel(rt, Palette.PanelLight, "Board");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -330), new Vector2(1500, 420));
            var title = UIKit.Label(panel.transform, data.Ui("pairTitle"), 30, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.PlaceFixed(title.rectTransform, new Vector2(0, 1), new Vector2(430, -35), new Vector2(800, 44));
            var stepLabel = UIKit.Label(panel.transform, "", 24, new Color(0.35f, 0.4f, 0.5f), TextAnchor.MiddleRight);
            UIKit.PlaceFixed(stepLabel.rectTransform, new Vector2(1, 1), new Vector2(-180, -35), new Vector2(320, 44));

            // Spalten: Name | vorher | + | selbst | + | Nachbar | - | Wind | ergibt | neu
            float[] colX = { 90, 270, 375, 480, 585, 690, 795, 900, 1010, 1130 };
            string[] heads0 = { "", data.Ui("pairBefore"), data.Ui("pairPlus"), data.Ui("pairOwn"), data.Ui("pairPlus"), data.Ui("pairNeighbour"),
                data.Ui("pairMinus"), data.Ui("pairWind"), data.Ui("pairResult"), data.Ui("pairAfter") };
            Color[] tint = { Color.clear, new Color(0.85f, 0.88f, 0.94f), Color.clear, HeatColors.Comfy, Color.clear, Palette.Thread,
                Color.clear, HeatColors.Cold, Color.clear, new Color(0.85f, 0.88f, 0.94f) };
            for (int c = 1; c < colX.Length; c++)
            {
                bool op = tint[c] == Color.clear;
                if (!op)
                {
                    var box = UIKit.Panel(panel.transform, tint[c], "Head");
                    UIKit.PlaceFixed(box.rectTransform, new Vector2(0, 1), new Vector2(colX[c] + 40, -100), new Vector2(170, 56));
                }
                var t = UIKit.Label(panel.transform, heads0[c], op ? 30 : 21, Palette.Text, TextAnchor.MiddleCenter, op ? FontStyle.Bold : FontStyle.Normal);
                UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(colX[c] + 40, -100), new Vector2(op ? 80 : 165, 56));
            }
            var cells = new Text[2, 10];
            for (int r = 0; r < 2; r++)
            {
                float y = -165 - r * 58;
                var n = UIKit.Label(panel.transform, names[r], 24, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.PlaceFixed(n.rectTransform, new Vector2(0, 1), new Vector2(colX[0] + 40, y), new Vector2(170, 50));
                foreach (int c in new[] { 1, 3, 5, 7, 9 })
                {
                    cells[r, c] = UIKit.Label(panel.transform, data.Ui("pairUnknown"), 28, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIKit.PlaceFixed(cells[r, c].rectTransform, new Vector2(0, 1), new Vector2(colX[c] + 40, y), new Vector2(170, 50));
                }
            }
            var conservation = UIKit.Label(panel.transform, "", 22, new Color(0.55f, 0.3f, 0.05f), TextAnchor.MiddleLeft, FontStyle.Italic);
            UIKit.PlaceFixed(conservation.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -280), new Vector2(1420, 44));

            // Blick unter die Haube: die Formel hinter der Bild-Gleichung (optional, docs/00)
            var p0 = _level.Model;
            var tech = UIKit.Label(panel.transform, data.Tech("pairFormula") + "\n" + data.Tech("pairParams", lesson.Dt, p0.Capacity, p0.Heat,
                p0.Contact, p0.HExposed, p0.HShielded, p0.AirTemp), 17, new Color(0.15f, 0.35f, 0.45f), TextAnchor.UpperLeft);
            UIKit.PlaceFixed(tech.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(1420, 56));
            tech.gameObject.SetActive(false);
            var techBtn = UIKit.Button(panel.transform, data.Ui("techButton"), () => tech.gameObject.SetActive(!tech.gameObject.activeSelf),
                new Color(0.25f, 0.3f, 0.4f), 20);
            UIKit.PlaceFixed((RectTransform)techBtn.transform, new Vector2(0, 0), new Vector2(180, 38), new Vector2(300, 52));

            bool go = false;
            var next = UIKit.Button(panel.transform, data.Ui("pairNext"), () => go = true, Palette.Button, 26);
            UIKit.PlaceFixed((RectTransform)next.transform, new Vector2(1, 0), new Vector2(-190, 38), new Vector2(340, 60));

            void ShowState(int step)
            {
                stepLabel.text = data.Ui("pairStep", step, lesson.Steps);
                for (int i = 0; i < 2; i++)
                {
                    double temp = model.Temperatures[i];
                    heads[i].text = data.Ui("pairHead", names[i], temp);
                    var col = HeatColors.Penguin(temp, _level.FreezeBelow);
                    Prims.SetPenguinTint(pair[i], Color.Lerp(Palette.PenguinWhite, col, 0.75f), col);
                }
            }

            ShowState(0);
            for (int i = 0; i < 2; i++) cells[i, 1].text = data.Ui("pairValue", model.Temperatures[i]);
            yield return _root.Hud.FadeIn();
            yield return _root.Hud.Say("pair_intro");

            for (int step = 1; step <= lesson.Steps; step++)
            {
                go = false;
                while (!go) yield return null;
                Sfx.Play(Sfx.Sound.Click);
                var before = (double[])model.Temperatures.Clone();
                model.Step(lesson.Dt, lesson.WindEast, lesson.WindNorth);
                var terms = model.LastStepTerms(lesson.Dt);
                // Zeile füllt sich von links nach rechts – erst die Teile, dann das Ergebnis
                for (int i = 0; i < 2; i++)
                {
                    cells[i, 1].text = data.Ui("pairValue", before[i]);
                    cells[i, 3].text = cells[i, 5].text = cells[i, 7].text = cells[i, 9].text = data.Ui("pairUnknown");
                }
                foreach (int c in new[] { 3, 5, 7, 9 })
                {
                    yield return new WaitForSeconds(0.35f);
                    for (int i = 0; i < 2; i++)
                        cells[i, c].text = c == 3 ? data.Ui("pairSigned", terms.Own[i])
                            : c == 5 ? data.Ui("pairSigned", terms.FromNeighbours[i])
                            : c == 7 ? data.Ui("pairValue", terms.ToWind[i])
                            : data.Ui("pairValue", model.Temperatures[i]);
                    Sfx.Play(c == 9 ? Sfx.Sound.Ding : Sfx.Sound.Plop, 0.4f);
                }
                ShowState(step);
                conservation.text = data.Ui("pairConservation", names[1], terms.FromNeighbours[1], names[0], terms.FromNeighbours[0],
                    terms.FromNeighbours[0] + terms.FromNeighbours[1]);
                if (step == 1) yield return _root.Hud.Say("pair_conservation");
            }
            next.gameObject.SetActive(false);
            yield return _root.Hud.Say("pair_done");
            AddGlossary("timestep");
            yield return _root.Hud.FadeOut();

            Destroy(layer);
            foreach (var p in pair) Destroy(p);
            foreach (var p in _penguins) p.SetActive(true);
            foreach (var f in _flakes) f.gameObject.SetActive(true);
            SetWind(0);
        }

        private void AddGlossary(string phase)
        {
            foreach (var g in _root.Data.Mg1Content.Arr("glossary"))
            {
                var o = (Dictionary<string, object>)g;
                if (o.Str("phase") != phase) continue;
                string entry = $"{o.Str("term")}: {o.Str("text")}";
                if (_root.State.Glossary.Contains(entry)) continue;
                _root.State.Glossary.Add(entry);
                _root.Hud.Toast(_root.Data.Ui("glossaryNew", entry), 4f);
                Sfx.Play(Sfx.Sound.Ding, 0.4f);
            }
        }

        /// <summary>Wärmebild des fertigen Huddles von oben – für „So macht's Siemens“ neben dem Akku.</summary>
        private Texture2D Snapshot()
        {
            const int size = 96;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            // Ausschnitt um den Huddle, wo immer der Spieler ihn hingeschoben hat – mittig und mit Rand.
            // (Vorher ein fester Ausschnitt der Scholle: Ein Huddle am Rand klebte in der Bildecke, Spieltest 17.10.2026.)
            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            for (int i = 0; i < _model.Count; i++)
            {
                minX = System.Math.Min(minX, _model.X[i]); maxX = System.Math.Max(maxX, _model.X[i]);
                minY = System.Math.Min(minY, _model.Y[i]); maxY = System.Math.Max(maxY, _model.Y[i]);
            }
            float midX = (float)(minX + maxX) / 2, midY = (float)(minY + maxY) / 2;
            float r = (float)System.Math.Max(maxX - minX, maxY - minY) / 2 + 3f * (float)_level.PenguinRadius; // halbe Bildbreite in Metern
            var air = HeatColors.Penguin(_level.Model.AirTemp, _level.FreezeBelow);
            var px = new Color[size * size];
            for (int k = 0; k < px.Length; k++) px[k] = Color.Lerp(air, Palette.Ice, 0.55f);
            for (int i = 0; i < _model.Count; i++)
            {
                var col = HeatColors.Penguin(_model.Temperatures[i], _level.FreezeBelow);
                float cx = (((float)_model.X[i] - midX) / r * 0.5f + 0.5f) * size, cy = (((float)_model.Y[i] - midY) / r * 0.5f + 0.5f) * size;
                float pr = (float)_level.PenguinRadius / r * 0.5f * size;
                for (int y = Mathf.Max(0, (int)(cy - pr)); y < Mathf.Min(size, (int)(cy + pr) + 1); y++)
                    for (int x = Mathf.Max(0, (int)(cx - pr)); x < Mathf.Min(size, (int)(cx + pr) + 1); x++)
                        if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= pr * pr) px[y * size + x] = col;
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private void SetWind(int index)
        {
            _windIndex = index;
            string from = _level.WindSequence[index % _level.WindSequence.Length];
            WindDirection.BlowsTowards(from, out _windE, out _windN);
            var dir = new Vector3((float)_windE, 0, (float)_windN);
            _windArrow.localPosition = -dir * ((float)_level.ArenaRadius + 1.8f) + Vector3.up * 0.4f;
            _windArrow.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            _windTimer = 0f;
            _warned = false;
        }

        private void Update()
        {
            if (!_running) return;
            HandleDrag();
            Separate();

            if (!_root.Hud.Blocking)
            {
                _windTimer += Time.deltaTime;
                if (!_warned && _windTimer > _level.WindChangeEvery - _level.WindWarning)
                {
                    _warned = true;
                    _root.Hud.Toast(_root.Data.Ui("prologueWindTurns"), (float)_level.WindWarning);
                }
                if (_windTimer >= _level.WindChangeEvery) SetWind(_windIndex + 1);

                _accumulated += Time.deltaTime * (float)_level.SimSecondsPerSecond;
                while (_accumulated >= _level.SimStep)
                {
                    _model.Step(_level.SimStep, _windE, _windN);
                    _accumulated -= (float)_level.SimStep;
                }

                int frozen = 0;
                for (int i = 0; i < _model.Count; i++) if (_model.Temperatures[i] < _level.FreezeBelow) frozen++;
                // Je mehr Pinguine frieren, desto lauter pfeift der Wind – hörbar, wer ungeschützt steht.
                Sfx.Wind(0.25f + 0.75f * frozen / (float)_model.Count);
                if (frozen == 0)
                {
                    _progress += Time.deltaTime;
                    _frozenFor = 0f;
                }
                else
                {
                    // Gezählt wird nur Spielzeit: Lief die Uhr während eines Tipps weiter, kam der nächste
                    // Tipp sofort nach dem Wegklicken (Review zu PR #2).
                    _frozenFor += Time.deltaTime;
                    if (_frozenFor > 8f && _hintsGiven < 3)
                    {
                        _hintsGiven++;
                        _frozenFor = 0f;
                        _root.Hud.Run(_root.Hud.Say("prologue_hint_freeze"));
                    }
                }
            }
            UpdateVisuals();
            if (_root.Hud.Blocking) Sfx.Wind(0.15f); // Während Grete spricht, ruht auch der Sturm (Wind steht still)
            _barFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_progress / (float)_level.TargetSeconds), 1);
            _barText.text = _root.Data.Ui("prologueBar", Mathf.FloorToInt(_progress), Mathf.RoundToInt((float)_level.TargetSeconds));
        }

        private void HandleDrag()
        {
            if (_root.Hud.Blocking) { _dragging = -1; return; }
            if (!InputHelper.PointerOnGround(_root.Rig.Cam, out var p)) return;
            var local = p - transform.position;
            if (InputHelper.PointerDown && !InputHelper.PointerOverUI)
            {
                double best = 0.7;
                _dragging = -1;
                for (int i = 0; i < _model.Count; i++)
                {
                    double dx = _model.X[i] - local.x, dy = _model.Y[i] - local.z;
                    double d = System.Math.Sqrt(dx * dx + dy * dy);
                    if (d < best) { best = d; _dragging = i; }
                }
            }
            if (_dragging >= 0 && InputHelper.PointerHeld)
            {
                var v = new Vector2(local.x, local.z);
                float max = (float)(_level.ArenaRadius - _level.PenguinRadius);
                if (v.magnitude > max) v = v.normalized * max;
                _model.X[_dragging] = v.x;
                _model.Y[_dragging] = v.y;
            }
            if (InputHelper.PointerUp) _dragging = -1;
        }

        /// <summary>Pinguine überlappen nicht (einfaches Auseinanderschieben).</summary>
        private void Separate()
        {
            double min = 2 * _level.PenguinRadius;
            for (int it = 0; it < 3; it++)
            {
                for (int i = 0; i < _model.Count; i++)
                {
                    for (int j = i + 1; j < _model.Count; j++)
                    {
                        double dx = _model.X[j] - _model.X[i], dy = _model.Y[j] - _model.Y[i];
                        double d = System.Math.Sqrt(dx * dx + dy * dy);
                        if (d >= min || d < 1e-6) continue;
                        double push = (min - d) / 2 / d;
                        double wi = i == _dragging ? 0 : 1, wj = j == _dragging ? 0 : 1;
                        if (wi + wj == 0) continue;
                        double fi = 2 * wi / (wi + wj), fj = 2 * wj / (wi + wj);
                        _model.X[i] -= dx * push * fi;
                        _model.Y[i] -= dy * push * fi;
                        _model.X[j] += dx * push * fj;
                        _model.Y[j] += dy * push * fj;
                    }
                }
            }
        }

        private void UpdateVisuals()
        {
            var dir = new Vector3((float)_windE, 0, (float)_windN);
            for (int i = 0; i < _penguins.Count; i++)
            {
                var t = _penguins[i].transform;
                double temp = _model.Temperatures[i];
                bool frozen = temp < _level.FreezeBelow;
                var basePos = new Vector3((float)_model.X[i], 0, (float)_model.Y[i]);
                float shiver = frozen ? Mathf.Sin(Time.time * 45f + i) * 0.03f : 0f;
                t.localPosition = basePos + new Vector3(shiver, i == _dragging ? 0.25f : 0f, 0);
                t.localRotation = Quaternion.LookRotation(dir, Vector3.up); // Rücken zum Wind
                var col = HeatColors.Penguin(temp, _level.FreezeBelow);
                Prims.SetPenguinTint(_penguins[i], Color.Lerp(Palette.PenguinWhite, col, 0.75f), col);
            }
            float r = (float)_level.ArenaRadius + 2f;
            foreach (var f in _flakes)
            {
                var p = f.localPosition + dir * (Time.deltaTime * 6f);
                if (Mathf.Abs(p.x) > r) p.x = -Mathf.Sign(p.x) * r;
                if (Mathf.Abs(p.z) > r) p.z = -Mathf.Sign(p.z) * r;
                f.localPosition = p;
            }
            if (_warned) _windArrow.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Time.time * 10f));
            else _windArrow.localScale = Vector3.one;
        }
    }
}
