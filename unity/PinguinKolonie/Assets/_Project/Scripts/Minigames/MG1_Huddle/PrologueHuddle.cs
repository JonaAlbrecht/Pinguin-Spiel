using System.Collections;
using System.Collections.Generic;
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
        private float _frozenSince = -1f;
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
            BuildUi();
            _root.Rig.LookAt(transform.position, 62f, _root.Rig.DistanceToFit((float)_level.ArenaRadius * 2f + 2f), 0f);
            yield return _root.Hud.FadeIn();
            yield return _root.Hud.Say("prologue_intro");
            _running = true;
            while (_progress < _level.TargetSeconds)
            {
                if (_root.Hud.DialogOpen) { yield return null; continue; }
                yield return null;
            }
            _running = false;
            _root.Hud.Toast("Alle sind warm geblieben!", 3f);
            foreach (var p in _penguins) p.AddComponent<Bobber>().Amplitude = 0.08f;
            yield return new WaitForSeconds(1.5f);
            yield return _root.Hud.Say("prologue_done");
            yield return _root.Hud.FadeOut();
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
            var legend = UIKit.Label(rt, "Blau = friert   ·   Gelb/Orange = warm   ·   Pinguine mit Maus oder Finger ziehen", 24,
                Palette.Text, TextAnchor.MiddleCenter);
            UIKit.PlaceFixed(legend.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -112), new Vector2(1400, 36));
            if (_root.State.PrologueDone)
            {
                var skip = UIKit.Button(rt, "Überspringen", () => _progress = (float)_level.TargetSeconds, Palette.ButtonAlt, 24);
                UIKit.PlaceFixed((RectTransform)skip.transform, new Vector2(1f, 1f), new Vector2(-130, -60), new Vector2(220, 56));
            }
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

            if (!_root.Hud.DialogOpen)
            {
                _windTimer += Time.deltaTime;
                if (!_warned && _windTimer > _level.WindChangeEvery - _level.WindWarning)
                {
                    _warned = true;
                    _root.Hud.Toast("Achtung, der Wind dreht gleich!", (float)_level.WindWarning);
                }
                if (_windTimer >= _level.WindChangeEvery) SetWind(_windIndex + 1);

                _accumulated += Time.deltaTime * (float)_level.SimSecondsPerSecond;
                while (_accumulated >= _level.SimStep)
                {
                    _model.Step(_level.SimStep, _windE, _windN);
                    _accumulated -= (float)_level.SimStep;
                }

                bool anyFrozen = false;
                for (int i = 0; i < _model.Count; i++) if (_model.Temperatures[i] < _level.FreezeBelow) anyFrozen = true;
                if (!anyFrozen)
                {
                    _progress += Time.deltaTime;
                    _frozenSince = -1f;
                }
                else
                {
                    if (_frozenSince < 0) _frozenSince = Time.time;
                    if (Time.time - _frozenSince > 8f && _hintsGiven < 3)
                    {
                        _hintsGiven++;
                        _frozenSince = Time.time;
                        _root.Hud.Run(_root.Hud.Say("prologue_hint_freeze"));
                    }
                }
            }
            UpdateVisuals();
            _barFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_progress / (float)_level.TargetSeconds), 1);
            _barText.text = $"Alle warm: {Mathf.FloorToInt(_progress)} von {Mathf.RoundToInt((float)_level.TargetSeconds)} Sekunden";
        }

        private void HandleDrag()
        {
            if (_root.Hud.DialogOpen) { _dragging = -1; return; }
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
