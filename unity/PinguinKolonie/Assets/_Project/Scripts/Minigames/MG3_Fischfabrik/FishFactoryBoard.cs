using System;
using System.Collections;
using System.Collections.Generic;
using SFE.SimCore.Process;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Die Fischfabrik als 3D-Raster: ein Feld = 1 Unity-Einheit, Zeile 0 im Norden (+z), Anlegestelle im Westen,
    /// Verladung im Osten. Zwei Looks aus demselben Code:
    /// - Bildschirm (<c>real = false</c>): ZWILLIS Bildschirm wie <see cref="SettlementBoard"/> – dunkler Rahmen,
    ///   Leuchtkante, Gitterlinien, Beschriftung. Alles darauf ist Rechnung.
    /// - Halle (<c>real = true</c>): Olafs echte Halle für Kalles Versuch – Hallenboden, Wände, kein Raster, kein ZWILLI.
    /// Den Zustand der Simulation zeigt <see cref="ShowSim"/>: Kistenstapel wachsen, Lämpchen zeigen Arbeit und Störung,
    /// Stationen mit großem Stapel blinken, Robo-Robben fahren (und zittern, wenn sie sich im Weg stehen).
    /// </summary>
    public sealed class FishFactoryBoard : MonoBehaviour
    {
        private ProcessLevel _level;
        private int _w, _h;
        private bool _real;
        private string _screenLabel;
        private char[] _shown;
        private readonly Dictionary<int, GameObject> _pieces = new Dictionary<int, GameObject>();
        private readonly List<GameObject> _arrows = new List<GameObject>();
        private readonly List<TextMesh> _labels = new List<TextMesh>();
        private GameObject _cursor, _carried;
        private char _carriedSymbol = FactoryLayout.Floor;
        private Dictionary<string, object> _names;

        // Simulation sichtbar
        private FactoryLayout _layout;
        private readonly List<GameObject>[] _stacks = new List<GameObject>[5];
        private readonly TextMesh[] _stackLabels = new TextMesh[5];
        private readonly Vector3[] _stackAnchor = new Vector3[5];
        private readonly List<int>[] _stageCells = new List<int>[3];
        private readonly List<GameObject> _jamming = new List<GameObject>();
        private List<int> _robotCells = new List<int>();
        private int _robotsBusy;
        private bool _crowded;
        private int _traceSeen, _stolenSeen;
        private GameObject _boat, _train;
        private float _boatT = -1f, _trainT = -1f;
        private readonly List<(Transform t, float born, Vector3 dir)> _gulls = new List<(Transform, float, Vector3)>();

        public Vector3 Center => transform.position + new Vector3(_w / 2f, 0, _h / 2f);
        public int Width => _w;
        public int Height => _h;

        public static FishFactoryBoard Create(Transform parent, Vector3 origin, ProcessLevel level, bool real, string screenLabel,
            Dictionary<string, object> stationNames)
        {
            var go = new GameObject(real ? "FishFactoryHall" : "FishFactoryScreen");
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            var b = go.AddComponent<FishFactoryBoard>();
            b._level = level;
            b._w = level.Width;
            b._h = level.Height;
            b._real = real;
            b._screenLabel = screenLabel;
            b._names = stationNames;
            b.Init();
            return b;
        }

        private void Init()
        {
            _shown = new char[_w * _h];
            for (int i = 0; i < _shown.Length; i++) _shown[i] = '\0';
            BuildGround();
            _cursor = Prims.Make(PrimitiveType.Cube, transform, Vector3.zero, new Vector3(1f, 0.04f, 1f), Palette.Selected, "Cursor");
            _cursor.SetActive(false);

            // Wasser im Westen mit Kutter, Gleis im Osten mit dem Polar-Express
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(-2.5f, -0.08f, _h / 2f), new Vector3(5f, 0.1f, _h + (_real ? 6f : 1.6f)),
                Palette.Sea, "Water");
            _boat = FishFactoryPieces.Boat(transform);
            _boat.transform.localPosition = new Vector3(-1.2f, 0f, _h / 2f);
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w + 0.8f, 0.01f, _h / 2f), new Vector3(0.9f, 0.02f, _h + 1f), Palette.Mg3Rail, "Track");
            _train = FishFactoryPieces.TrainSet(transform);
            _train.transform.localPosition = new Vector3(_w + 0.8f, 0f, _h + 6f);
            for (int s = 0; s < _stacks.Length; s++) _stacks[s] = new List<GameObject>();
        }

        private void BuildGround()
        {
            if (_real)
            {
                // Hallenboden aus Platten, Rückwand mit Schild, niedrige Seitenwände – keine Gitterlinien, kein Leuchten
                for (int y = 0; y < _h; y++)
                    for (int x = 0; x < _w; x++)
                        Prims.Make(PrimitiveType.Cube, transform, new Vector3(x + 0.5f, -0.05f, _h - 1 - y + 0.5f), new Vector3(1f, 0.1f, 1f),
                            (x + y) % 2 == 0 ? Palette.Mg3HallFloor : Palette.Mg3HallFloorAlt, "Floor");
                Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w / 2f, 1.4f, _h + 0.15f), new Vector3(_w + 2.6f, 2.8f, 0.3f), Palette.Mg3HallWall, "WallN");
                Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w / 2f, 2.75f, _h + 0.1f), new Vector3(_w + 2.6f, 0.3f, 0.4f), Palette.Mg3HallTrim, "Trim");
                Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w / 2f, 0.25f, -0.25f), new Vector3(_w, 0.5f, 0.2f), Palette.Mg3HallWall, "WallS");
                if (!string.IsNullOrEmpty(_screenLabel))
                {
                    var sign = Prims.Label(transform, _screenLabel, new Vector3(_w / 2f, 2.1f, _h - 0.1f), 0.09f, Palette.Text);
                    sign.fontStyle = FontStyle.Bold;
                }
                return;
            }
            const float rim = 0.9f, lip = 0.12f, glow = 0.08f;
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w / 2f, -0.26f, _h / 2f), new Vector3(_w + rim * 2, 0.5f, _h + rim * 2),
                Palette.ScreenFrame, "ScreenFrame");
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                    Prims.Make(PrimitiveType.Cube, transform, new Vector3(x + 0.5f, -0.01f, _h - 1 - y + 0.5f), new Vector3(1f, 0.02f, 1f),
                        (x + y) % 2 == 0 ? Palette.ScreenIce : Palette.ScreenIceAlt, "Cell");
            for (int x = 0; x <= _w; x++)
                Prims.Make(PrimitiveType.Cube, transform, new Vector3(x, 0.004f, _h / 2f), new Vector3(0.04f, 0.01f, _h), Palette.GridLine, "GridX");
            for (int y = 0; y <= _h; y++)
                Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w / 2f, 0.004f, y), new Vector3(_w, 0.01f, 0.04f), Palette.GridLine, "GridY");
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w / 2f, lip / 2, -glow / 2), new Vector3(_w + glow * 2, lip, glow), Palette.Screen, "GlowS");
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w / 2f, lip / 2, _h + glow / 2), new Vector3(_w + glow * 2, lip, glow), Palette.Screen, "GlowN");
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(-glow / 2, lip / 2, _h / 2f), new Vector3(glow, lip, _h), Palette.Screen, "GlowW");
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w + glow / 2, lip / 2, _h / 2f), new Vector3(glow, lip, _h), Palette.Screen, "GlowE");
            if (string.IsNullOrEmpty(_screenLabel)) return;
            var label = Prims.Label(transform, _screenLabel, new Vector3(_w / 2f, 0.03f, _h + rim / 2), 0.07f, Palette.Screen);
            label.fontStyle = FontStyle.Bold;
            label.transform.rotation = Quaternion.Euler(60, 0, 0);
        }

        // ---------- Koordinaten ----------

        public Vector3 CellCenter(int index)
        {
            int x = index % _w, y = index / _w;
            return transform.position + new Vector3(x + 0.5f, 0, _h - 1 - y + 0.5f);
        }

        public bool CellAtPointer(Camera cam, out int index)
        {
            index = -1;
            if (!InputHelper.PointerOnGround(cam, out var p, transform.position.y)) return false;
            var local = p - transform.position;
            int x = Mathf.FloorToInt(local.x);
            int y = _h - 1 - Mathf.FloorToInt(local.z);
            if (x < 0 || y < 0 || x >= _w || y >= _h) return false;
            index = y * _w + x;
            return true;
        }

        public void ShowCursor(int index, Color color)
        {
            if (index < 0) { _cursor.SetActive(false); return; }
            _cursor.SetActive(true);
            _cursor.transform.position = CellCenter(index) + new Vector3(0, 0.02f, 0);
            Prims.SetColor(_cursor, color);
        }

        /// <summary>Mitte einer Gruppe gleicher Bausteine (z. B. aller Filetier-Stationen) – für Pfeile und Hinweise.</summary>
        public Vector3 GroupCenter(FactoryLayout layout, char symbol)
        {
            var cells = layout.Cells(symbol);
            if (cells.Count == 0) return Center;
            var sum = Vector3.zero;
            foreach (int c in cells) sum += CellCenter(c);
            return sum / cells.Count;
        }

        // ---------- Bausteine ----------

        public void Show(FactoryLayout layout)
        {
            _layout = layout;
            for (int i = 0; i < layout.CellCount; i++)
            {
                char c = layout[i];
                if (_shown[i] == c) continue;
                if (_pieces.TryGetValue(i, out var old)) { Destroy(old); _pieces.Remove(i); }
                _shown[i] = c;
                var go = FishFactoryPieces.Create(c, transform);
                if (go == null) continue;
                go.transform.position = CellCenter(i);
                // Robo-Robben schauen zur Verladung, Kühlregale mit dem Rücken zur Wand
                if (c == FactoryLayout.Robot) go.transform.rotation = Quaternion.Euler(0, 90, 0);
                _pieces[i] = go;
            }
            UpdateNames();
            ResetSim();
        }

        /// <summary>Namensschild über der ersten Station jeder Sorte (Nicht-Techniker sehen die Formen zum ersten Mal).</summary>
        private void UpdateNames()
        {
            foreach (var l in _labels) Destroy(l.gameObject);
            _labels.Clear();
            if (_names == null) return;
            foreach (char sym in new[] { FactoryLayout.Dock, 'S', 'F', 'P', FactoryLayout.Regal, FactoryLayout.Train })
            {
                var cells = _layout.Cells(sym);
                if (cells.Count == 0 || !_names.TryGetValue(sym.ToString(), out object name)) continue;
                int c = sym == FactoryLayout.Dock || sym == FactoryLayout.Train ? cells[cells.Count / 2] : cells[0];
                var at = CellCenter(c) - transform.position + new Vector3(0, sym == FactoryLayout.Regal ? 1.6f : 1.25f, 0.25f);
                var l = Prims.Label(transform, (string)name, at, _real ? 0.032f : 0.026f, _real ? Palette.Text : Palette.TextLight);
                l.fontStyle = FontStyle.Bold;
                _labels.Add(l);
            }
        }

        public void Clear()
        {
            foreach (var go in _pieces.Values) Destroy(go);
            _pieces.Clear();
            for (int i = 0; i < _shown.Length; i++) _shown[i] = '\0';
            ClearArrows();
            ResetSim();
        }

        /// <summary>Ein angehobener Baustein (Werkzeug „Verschieben“) schwebt über dem Feld unter dem Zeiger.</summary>
        public void ShowCarried(char symbol, int cell)
        {
            if (symbol == FactoryLayout.Floor || cell < 0)
            {
                if (_carried != null) _carried.SetActive(false);
                return;
            }
            if (_carried == null || _carriedSymbol != symbol)
            {
                if (_carried != null) Destroy(_carried);
                _carried = FishFactoryPieces.Create(symbol, transform);
                _carriedSymbol = symbol;
            }
            _carried.SetActive(true);
            _carried.transform.position = CellCenter(cell) + Vector3.up * (0.55f + 0.08f * Mathf.Sin(Time.time * 6f));
        }

        // ---------- Pfeile (Schritt 3) ----------

        public void AddArrow(Vector3 from, Vector3 to) => _arrows.Add(FishFactoryPieces.Arrow(transform, from, to, Palette.Mg3Arrow));

        public void ClearArrows()
        {
            foreach (var a in _arrows) Destroy(a);
            _arrows.Clear();
        }

        /// <summary>Kurzes Aufleuchten einer Station (richtig angetippt, Engpass zeigen).</summary>
        public IEnumerator Pulse(int cell, float seconds = 0.5f)
        {
            if (!_pieces.TryGetValue(cell, out var go)) yield break;
            for (float t = 0; t < seconds && go != null; t += Time.deltaTime)
            {
                go.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(t / seconds * Mathf.PI));
                yield return null;
            }
            if (go != null) go.transform.localScale = Vector3.one;
        }

        // ---------- Simulation sichtbar machen ----------

        /// <summary>Stapel leeren, Lämpchen aus, Robo-Robben nach Hause.</summary>
        public void ResetSim()
        {
            foreach (var list in _stacks)
            {
                if (list == null) continue;
                foreach (var c in list) Destroy(c);
                list.Clear();
            }
            foreach (var l in _stackLabels) if (l != null) l.gameObject.SetActive(false);
            _jamming.Clear();
            _robotsBusy = 0;
            _crowded = false;
            _traceSeen = 0;
            _stolenSeen = 0;
            if (_layout == null) return;
            for (int s = 0; s < 3 && s < _level.Stages.Length; s++) _stageCells[s] = _layout.Cells(_level.Stages[s].Symbol);
            _robotCells = _layout.Cells(FactoryLayout.Robot);
            foreach (var kv in _pieces)
            {
                kv.Value.transform.position = CellCenter(kv.Key);
                kv.Value.transform.localScale = Vector3.one;
                var lamp = kv.Value.transform.Find("Lamp");
                if (lamp != null) Prims.SetColor(lamp.gameObject, Palette.Mg3LampIdle);
            }
            ComputeAnchors();
        }

        private void ComputeAnchors()
        {
            // 0 Steg vor dem Sortieren, 1 Kühlregal, 2 vor dem Verpacken, 3 fertige Sushi-Kisten an der Verladung
            var dock = _layout.Cells(FactoryLayout.Dock);
            var sort = _layout.Cells('S');
            var pack = _layout.Cells('P');
            var train = _layout.Cells(FactoryLayout.Train);
            _stackAnchor[0] = CellCenter(Nearest(dock, sort)) + new Vector3(0.05f, 0.06f, 0);
            _stackAnchor[2] = pack.Count > 0 ? CellCenter(pack[0]) + new Vector3(0, 0, -0.42f) : Center;
            _stackAnchor[3] = CellCenter(Nearest(train, pack)) + new Vector3(-0.2f, 0.1f, 0);
        }

        /// <summary>Feld aus <paramref name="from"/>, das <paramref name="to"/> am nächsten liegt.</summary>
        private int Nearest(List<int> from, List<int> to)
        {
            if (from.Count == 0) return 0;
            if (to.Count == 0) return from[from.Count / 2];
            int best = from[0], bestD = int.MaxValue;
            foreach (int a in from)
            {
                int d = _layout.Distance(new List<int> { a }, to);
                if (d < bestD) { bestD = d; best = a; }
            }
            return best;
        }

        /// <summary>Zustand der Simulation zeigen; einmal je Bild aufrufen.</summary>
        public void ShowSim(FactorySimulation sim)
        {
            if (_layout == null) return;
            SetStack(0, sim.Waiting(0), _stackAnchor[0], 8);
            SetRegalStack(sim.Waiting(1));
            SetStack(2, sim.Waiting(2), _stackAnchor[2], 6);
            SetStack(3, sim.Ready, _stackAnchor[3], 8, sushi: true);

            _jamming.Clear();
            for (int s = 0; s < sim.StageCount && s < 3; s++)
            {
                var cells = _stageCells[s];
                for (int m = 0; m < cells.Count && m < sim.Config.Stages[s].Machines; m++)
                {
                    if (!_pieces.TryGetValue(cells[m], out var piece)) continue;
                    var lamp = piece.transform.Find("Lamp");
                    var state = sim.State(s, m);
                    if (lamp != null)
                    {
                        bool blinkOff = state == MachineState.Down && Mathf.Repeat(Time.time * 3f, 1f) > 0.5f;
                        Prims.SetColor(lamp.gameObject, state == MachineState.Busy ? Palette.Mg3LampBusy
                            : state == MachineState.Down ? (blinkOff ? Palette.Mg3LampIdle : Palette.Mg3LampDown) : Palette.Mg3LampIdle);
                    }
                    // Wo es sich staut, blinkt die Station (docs/03 MG3 Schritt 4)
                    if (sim.Waiting(s) >= 5) _jamming.Add(piece);
                    else piece.transform.localScale = Vector3.one;
                }
            }
            _robotsBusy = sim.Config.Robots - sim.RobotsFree;
            _crowded = sim.Config.RobotDelay > 0;

            // Ereignisse für kleine Szenen: Kutter legt an, Zug hält, Möwe klaut
            if (sim.Trace != null)
            {
                for (; _traceSeen < sim.Trace.Count; _traceSeen++)
                {
                    var e = sim.Trace[_traceSeen];
                    if (e.Kind == EventKind.Kutter) _boatT = 0f;
                    else if (e.Kind == EventKind.Train) _trainT = 0f;
                }
            }
            while (_stolenSeen < sim.Stolen)
            {
                _stolenSeen++;
                SpawnGull();
            }
        }

        /// <summary>Rechentafel: nur eine Station – wartende Kisten im Kühlregal, Lämpchen der Station.</summary>
        public void ShowStation(char symbol, int waiting, bool busy)
        {
            if (_layout == null) return;
            SetRegalStack(waiting);
            var cells = _layout.Cells(symbol);
            if (cells.Count == 0 || !_pieces.TryGetValue(cells[0], out var piece)) return;
            var lamp = piece.transform.Find("Lamp");
            if (lamp != null) Prims.SetColor(lamp.gameObject, busy ? Palette.Mg3LampBusy : Palette.Mg3LampIdle);
        }

        private void SetStack(int k, int count, Vector3 anchor, int perColumn, bool sushi = false)
        {
            var list = _stacks[k];
            int shown = Mathf.Min(count, 24);
            while (list.Count < shown)
            {
                var c = sushi ? FishFactoryPieces.SushiBox(transform) : FishFactoryPieces.Crate(transform);
                int i = list.Count, col = i / perColumn, row = i % perColumn;
                c.transform.position = anchor + new Vector3((col % 3) * 0.3f - 0.3f, 0.13f + row * (sushi ? 0.16f : 0.26f), (col / 3) * 0.34f);
                list.Add(c);
            }
            while (list.Count > shown)
            {
                Destroy(list[list.Count - 1]);
                list.RemoveAt(list.Count - 1);
            }
            Count(k, count, anchor + Vector3.up * (0.5f + Mathf.Min(count, perColumn) * 0.26f));
        }

        /// <summary>Kisten im Kühlregal: auf die Regale verteilt, je Regal so viele, wie hineinpassen.</summary>
        private void SetRegalStack(int count)
        {
            var regals = _layout.Cells(FactoryLayout.Regal);
            var list = _stacks[1];
            int shown = Mathf.Min(count, regals.Count * Mathf.Max(1, _level.RegalCapacity));
            while (list.Count < shown)
            {
                int i = list.Count, per = Mathf.Max(1, _level.RegalCapacity);
                int regal = i / per, slot = i % per;
                var c = FishFactoryPieces.Crate(transform);
                c.transform.position = CellCenter(regals[regal]) + new Vector3((slot % 2) * 0.38f - 0.19f, 0.2f + (slot / 2) * 0.32f, 0);
                list.Add(c);
            }
            while (list.Count > shown)
            {
                Destroy(list[list.Count - 1]);
                list.RemoveAt(list.Count - 1);
            }
            if (regals.Count > 0) Count(1, count, CellCenter(regals[0]) + Vector3.up * 1.9f);
        }

        private void Count(int k, int count, Vector3 at)
        {
            if (_stackLabels[k] == null)
            {
                _stackLabels[k] = Prims.Label(transform, "", Vector3.zero, 0.04f, Palette.Text);
                _stackLabels[k].fontStyle = FontStyle.Bold;
            }
            var l = _stackLabels[k];
            l.gameObject.SetActive(count > 0);
            l.text = count.ToString();
            l.color = count >= 5 ? Palette.Mg3Jam : _real ? Palette.Text : Palette.TextLight;
            l.transform.position = at;
        }

        private void SpawnGull()
        {
            var g = FishFactoryPieces.Gull(transform).transform;
            g.position = _stackAnchor[0] + new Vector3(0, 0.4f, 0);
            float a = UnityEngine.Random.Range(-0.6f, 0.6f);
            _gulls.Add((g, Time.time, new Vector3(-Mathf.Cos(a), 0.6f, Mathf.Sin(a))));
            var fish = Prims.Make(PrimitiveType.Capsule, g, new Vector3(0, -0.12f, 0.1f), new Vector3(0.08f, 0.12f, 0.08f), Palette.Mg3Fish, "Fish");
            fish.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        private char _highlight = FactoryLayout.Floor;

        /// <summary>Alle Stationen dieser Sorte blinken (der Engpass beim Planen); null schaltet es aus.</summary>
        public void SetHighlight(char? symbol)
        {
            if (_highlight != FactoryLayout.Floor && _layout != null)
                foreach (int c in _layout.Cells(_highlight))
                    if (_pieces.TryGetValue(c, out var p)) p.transform.localScale = Vector3.one;
            _highlight = symbol ?? FactoryLayout.Floor;
        }

        private void Update()
        {
            // Stauende Stationen pulsieren; beim Planen blinkt der Engpass der letzten Rechnung
            float pulse = 1f + 0.08f * Mathf.Max(0f, Mathf.Sin(Time.time * 8f));
            foreach (var p in _jamming) if (p != null) p.transform.localScale = Vector3.one * pulse;
            if (_highlight != FactoryLayout.Floor && _layout != null)
                foreach (int c in _layout.Cells(_highlight))
                    if (_pieces.TryGetValue(c, out var p)) p.transform.localScale = Vector3.one * pulse;

            // Robo-Robben: die beschäftigten pendeln zwischen ihrem Platz und der Fabrikmitte; im Gedränge zittern sie
            for (int i = 0; i < _robotCells.Count; i++)
            {
                if (!_pieces.TryGetValue(_robotCells[i], out var robot)) continue;
                var home = CellCenter(_robotCells[i]);
                if (i < _robotsBusy)
                {
                    float phase = Mathf.PingPong(Time.time * 0.8f + i * 0.37f, 1f);
                    var target = Vector3.Lerp(home, Center, 0.55f);
                    var pos = Vector3.Lerp(home, target, phase);
                    if (_crowded) pos += new Vector3(Mathf.Sin(Time.time * 37f + i) * 0.06f, 0, Mathf.Cos(Time.time * 29f + i) * 0.06f);
                    robot.transform.position = pos;
                    var dir = target - home;
                    if (dir.sqrMagnitude > 0.01f) robot.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
                else robot.transform.position = home;
            }

            // Kutter legt an: von draußen heran, kurz liegen, wieder weg
            if (_boatT >= 0f)
            {
                _boatT += Time.deltaTime;
                float k = _boatT < 0.4f ? 1f - _boatT / 0.4f : _boatT < 1.2f ? 0f : (_boatT - 1.2f) / 0.6f;
                _boat.transform.localPosition = new Vector3(-1.2f - 3f * Mathf.Clamp01(k), 0.04f * Mathf.Sin(Time.time * 3f), _h / 2f);
                if (_boatT > 1.8f) { _boatT = -1f; _boat.transform.localPosition = new Vector3(-1.2f, 0, _h / 2f); }
            }
            // Polar-Express fährt durch und hält an der Verladung
            if (_trainT >= 0f)
            {
                _trainT += Time.deltaTime;
                float z = _trainT < 0.6f ? Mathf.Lerp(_h + 6f, _h / 2f - 0.75f, _trainT / 0.6f)
                    : _trainT < 1.4f ? _h / 2f - 0.75f : Mathf.Lerp(_h / 2f - 0.75f, -7f, (_trainT - 1.4f) / 0.6f);
                _train.transform.localPosition = new Vector3(_w + 0.8f, 0, z);
                if (_trainT > 2f) { _trainT = -1f; _train.transform.localPosition = new Vector3(_w + 0.8f, 0, _h + 6f); }
            }
            // Möwen fliegen mit ihrem Fisch davon
            for (int i = _gulls.Count - 1; i >= 0; i--)
            {
                var (t, born, dir) = _gulls[i];
                float age = Time.time - born;
                if (t == null || age > 2.5f)
                {
                    if (t != null) Destroy(t.gameObject);
                    _gulls.RemoveAt(i);
                    continue;
                }
                t.position += dir * (3f * Time.deltaTime);
                t.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z), Vector3.up);
                float flap = Mathf.Sin(age * 18f) * 35f;
                t.Find("WingL").localRotation = Quaternion.Euler(0, 0, flap);
                t.Find("WingR").localRotation = Quaternion.Euler(0, 0, -flap);
            }
        }

        /// <summary>
        /// Eine Schicht im Zeitraffer: In <paramref name="seconds"/> Sekunden läuft die Simulation bis
        /// <paramref name="end"/>. Die Uhr läuft gleichmäßig – der Computer springt dabei von Ereignis zu Ereignis.
        /// </summary>
        public IEnumerator Play(FactorySimulation sim, float seconds, double end, Action<FactorySimulation> onFrame = null)
        {
            ResetSim();
            double speed = end / Mathf.Max(0.1f, seconds);
            double t = 0;
            while (t < end)
            {
                t = Math.Min(end, t + speed * Time.deltaTime);
                sim.RunUntil(t);
                ShowSim(sim);
                onFrame?.Invoke(sim);
                yield return null;
            }
            ShowSim(sim);
            onFrame?.Invoke(sim);
        }

        /// <summary>Foto der aktuellen Ansicht ohne Oberfläche (das HUD liegt als Overlay darüber und kommt nicht mit).</summary>
        public static Texture2D Photo(Camera cam, int w = 640, int h = 400)
        {
            var rt = RenderTexture.GetTemporary(w, h, 24);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevTarget;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear };
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }
    }
}
