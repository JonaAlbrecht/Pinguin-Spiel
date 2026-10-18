using System.Collections;
using System.Collections.Generic;
using SFE.SimCore.Thermal;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// 3D-Darstellung des Siedlungsrasters: Eisfläche mit Feldern, Bausteine, Wärmebild, Wärmefäden.
    /// Eine Zelle = 1 Unity-Einheit (im Modell 2 m). Zeile 0 liegt im Norden (+z).
    /// Das Brett ist ZWILLIS Bildschirm (Rahmen, Leuchtkante, Beschriftung): alles darauf ist Rechnung, nichts echt.
    /// Echt passiert nur Kalles Versuch draußen im Hub (<see cref="HubWorld.PlayKalleAttempt"/>).
    /// </summary>
    public sealed class SettlementBoard : MonoBehaviour
    {
        private GameObject _gridLines;

        private SettlementLevel _level;
        private int _w, _h;
        private Texture2D _ground;
        private Color[] _pixels;
        private readonly Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();
        private char[] _shown;
        private GameObject _cursor;
        private GameObject _threadsGo;
        private Mesh _threadsMesh;
        private readonly List<LineRenderer> _handThreads = new List<LineRenderer>();
        private IglooState[] _states;
        private bool _duckPlaced;

        public Vector3 Center => transform.position + new Vector3(_w / 2f, 0, _h / 2f);
        public int Width => _w;
        public int Height => _h;

        private string _windLabel;
        private string _screenLabel;

        public static SettlementBoard Create(Transform parent, Vector3 origin, SettlementLevel level, string windLabel, string screenLabel)
        {
            var go = new GameObject("SettlementBoard");
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            var b = go.AddComponent<SettlementBoard>();
            b._windLabel = windLabel;
            b._screenLabel = screenLabel;
            b.Init(level);
            return b;
        }

        private void Init(SettlementLevel level)
        {
            _level = level;
            _w = level.Width;
            _h = level.Height;
            _shown = new char[_w * _h];
            for (int i = 0; i < _shown.Length; i++) _shown[i] = Cell.Ice;

            // Bildschirm statt Eissockel: dunkler Rahmen mit Leuchtkante und Beschriftung
            BuildScreen();

            // Bodentextur mit Feldern
            _ground = new Texture2D(_w, _h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            _pixels = new Color[_ground.width * _ground.height];
            BuildGridLines();
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "Ground";
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = new Vector3(_w / 2f, 0.002f, _h / 2f);
            quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale = new Vector3(_w, _h, 1);
            var mat = new Material(Prims.Unlit) { mainTexture = _ground, color = Color.white };
            quad.GetComponent<Renderer>().sharedMaterial = mat;
            SetHeat(null);

            // Zeiger-Markierung
            _cursor = Prims.Make(PrimitiveType.Cube, transform, Vector3.zero, new Vector3(1f, 0.04f, 1f), Palette.Selected, "Cursor");
            _cursor.SetActive(false);

            // Windpfeil (Nordwesten)
            BuildWindArrow();
        }

        private void BuildScreen()
        {
            const float rim = 0.9f, lip = 0.12f, glow = 0.08f;
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w / 2f, -0.26f, _h / 2f), new Vector3(_w + rim * 2, 0.5f, _h + rim * 2),
                Palette.ScreenFrame, "ScreenFrame");
            // Leuchtkante direkt am Raster – vier Leisten, knapp über dem Boden
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w / 2f, lip / 2, -glow / 2), new Vector3(_w + glow * 2, lip, glow), Palette.Screen, "GlowS");
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w / 2f, lip / 2, _h + glow / 2), new Vector3(_w + glow * 2, lip, glow), Palette.Screen, "GlowN");
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(-glow / 2, lip / 2, _h / 2f), new Vector3(glow, lip, _h), Palette.Screen, "GlowW");
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(_w + glow / 2, lip / 2, _h / 2f), new Vector3(glow, lip, _h), Palette.Screen, "GlowE");
            if (string.IsNullOrEmpty(_screenLabel)) return;
            var label = Prims.Label(transform, _screenLabel, new Vector3(_w / 2f, 0.03f, _h + rim / 2), 0.07f, Palette.Screen);
            label.fontStyle = FontStyle.Bold;
            label.transform.rotation = Quaternion.Euler(60, 0, 0);
        }

        private void BuildWindArrow()
        {
            WindDirection.BlowsTowards(_level.WindFrom, out double e, out double n);
            var dir = new Vector3((float)e, 0, (float)n).normalized;
            var arrow = new GameObject("WindArrow").transform;
            arrow.SetParent(transform, false);
            arrow.localPosition = new Vector3(_w / 2f, 0.6f, _h / 2f) - dir * (_w / 2f + 2.2f);
            arrow.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            var c = Palette.Wind;
            Prims.Make(PrimitiveType.Cube, arrow, new Vector3(0, 0, -0.4f), new Vector3(0.25f, 0.12f, 1.6f), c, "Shaft");
            var head = Prims.Make(PrimitiveType.Cube, arrow, new Vector3(0, 0, 0.5f), new Vector3(0.7f, 0.12f, 0.7f), c, "Head");
            head.transform.localRotation = Quaternion.Euler(0, 45, 0);
            var label = Prims.Label(arrow, _windLabel, new Vector3(0, 0.5f, -0.6f), 0.06f, Palette.WindLabel);
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
            _cursor.transform.position = CellCenter(index) + new Vector3(0, 0.01f, 0);
            Prims.SetColor(_cursor, color);
        }

        // ---------- Bausteine ----------

        public void Show(SettlementGrid grid)
        {
            for (int i = 0; i < grid.CellCount; i++)
            {
                char c = grid[i];
                if (_shown[i] == c) continue;
                if (_objects.TryGetValue(i, out var old)) { Destroy(old); _objects.Remove(i); }
                _shown[i] = c;
                var go = CreatePiece(c);
                if (go != null)
                {
                    go.transform.position = CellCenter(i);
                    _objects[i] = go;
                }
            }
            _states = null;
        }

        private GameObject CreatePiece(char c)
        {
            switch (c)
            {
                case Cell.Igloo: return Prims.Igloo(transform);
                case Cell.Lantern: return Prims.Lantern(transform, 1f, true);
                case Cell.Wall: return Prims.SnowWall(transform);
                case Cell.Water: return Prims.Puddle(transform);
                default: return null;
            }
        }

        private GameObject _carried;
        private char _carriedSymbol = Cell.Ice;

        /// <summary>
        /// Ein angehobener Baustein (Werkzeug „Verschieben“): schwebt über dem Feld unter dem Zeiger.
        /// <c>Cell.Ice</c> oder ein Feld kleiner 0 blendet ihn aus.
        /// </summary>
        public void ShowCarried(char symbol, int cell)
        {
            if (symbol == Cell.Ice || cell < 0)
            {
                if (_carried != null) _carried.SetActive(false);
                return;
            }
            if (_carried == null || _carriedSymbol != symbol)
            {
                if (_carried != null) Destroy(_carried);
                _carried = CreatePiece(symbol);
                _carriedSymbol = symbol;
            }
            _carried.SetActive(true);
            _carried.transform.position = CellCenter(cell) + Vector3.up * (0.55f + 0.08f * Mathf.Sin(Time.time * 6f));
        }

        public void Clear()
        {
            foreach (var go in _objects.Values) Destroy(go);
            _objects.Clear();
            for (int i = 0; i < _shown.Length; i++) _shown[i] = Cell.Ice;
            _duckPlaced = false;
            SetIglooStates(null);
        }

        /// <summary>Zustand der Iglus sichtbar machen – nicht nur über Farbe (Barrierefreiheit).</summary>
        public void SetIglooStates(IglooState[] states)
        {
            _states = states;
            foreach (var kv in _objects)
            {
                if (_shown[kv.Key] != Cell.Igloo) continue;
                var dome = kv.Value.transform.Find("Dome");
                var door = kv.Value.transform.Find("Door");
                if (states == null)
                {
                    Prims.SetColor(dome.gameObject, Palette.Igloo);
                    Prims.SetColor(door.gameObject, Palette.Door);
                    continue;
                }
                switch (states[kv.Key])
                {
                    case IglooState.Cold:
                        Prims.SetColor(dome.gameObject, Palette.IglooCold);
                        Prims.SetColor(door.gameObject, Palette.Door);
                        break;
                    case IglooState.Comfortable:
                        Prims.SetColor(dome.gameObject, Palette.IglooCozy);
                        Prims.SetColor(door.gameObject, Palette.LanternGlow);
                        break;
                    case IglooState.Melting:
                        Prims.SetColor(dome.gameObject, Palette.IglooMelting);
                        break;
                }
            }
        }

        private void Update()
        {
            if (_states == null) return;
            // Frierende Iglus zittern leicht, gemütliche „atmen“ warm.
            foreach (var kv in _objects)
            {
                if (_shown[kv.Key] != Cell.Igloo) continue;
                var t = kv.Value.transform;
                var basePos = CellCenter(kv.Key);
                if (_states[kv.Key] == IglooState.Cold)
                    t.position = basePos + new Vector3(Mathf.Sin(Time.time * 40f + kv.Key) * 0.025f, 0, 0);
                else
                    t.position = basePos;
            }
        }

        /// <summary>Iglus sacken zusammen und werden zu Pfützen. In der ersten Pfütze planscht ein Küken.</summary>
        public IEnumerator Melt(int[] cells, float seconds = 0.9f)
        {
            var targets = new List<Transform>();
            foreach (int i in cells) if (_objects.TryGetValue(i, out var go)) targets.Add(go.transform);
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float k = 1f - t / seconds;
                foreach (var tr in targets)
                {
                    if (tr == null) continue;
                    tr.localScale = new Vector3(1f + 0.25f * (1 - k), Mathf.Max(0.05f, k), 1f + 0.25f * (1 - k));
                }
                yield return null;
            }
            if (cells.Length > 0) Sfx.Play(Sfx.Sound.Splash);
            foreach (int i in cells)
            {
                if (_objects.TryGetValue(i, out var go)) Destroy(go);
                var p = Prims.Puddle(transform);
                p.transform.position = CellCenter(i);
                _objects[i] = p;
                _shown[i] = Cell.Water;
                if (!_duckPlaced)
                {
                    _duckPlaced = true;
                    // Flocke (docs/02: das planschende Küken) – seit v0.2 eine eigene Figur mit Pfützen-Sammlung
                    var chick = Prims.Penguin(p.transform, "Flocke", Prims.Look.Flocke, 0.8f);
                    chick.transform.localPosition = new Vector3(0.1f, 0f, 0f);
                    chick.AddComponent<Bobber>();
                }
            }
        }

        // ---------- Wärmebild ----------

        /// <summary>
        /// Färbt die Felder nach Temperatur. <c>null</c> = normales Eis. Ein Texel je Feld, ohne Glättung: Die Felder
        /// bleiben bei jeder Vergrößerung scharfe Quadrate. Die Gitterlinien sind eigene 3D-Streifen (<see cref="BuildGridLines"/>),
        /// die die Kantenglättung sauber zeichnet – in v0.1 waren sie 1 Texel einer 8-Pixel-Zelle und wirkten pixelig.
        /// </summary>
        public void SetHeat(double[] temperatures)
        {
            for (int y = 0; y < _h; y++)
            {
                for (int x = 0; x < _w; x++)
                {
                    int i = y * _w + x;
                    _pixels[(_h - 1 - y) * _w + x] = temperatures == null // Texturzeile 0 liegt unten (Süden)
                        ? ((x + y) % 2 == 0 ? Palette.ScreenIce : Palette.ScreenIceAlt)
                        : HeatColors.For(temperatures[i], _level.AirTemp, _level.ComfortMin, _level.Melt);
                }
            }
            _ground.SetPixels(_pixels);
            _ground.Apply(false);
            if (_gridLines != null) Prims.SetColor(_gridLines, temperatures == null ? Palette.GridLine : Palette.GridLineOnHeat);
        }

        public Texture2D GroundTexture => _ground;

        /// <summary>
        /// Wärmebild für „So macht's Siemens“: hochaufgelöst, mit Gitterlinien, aus dem aktuellen Feldbild gerechnet.
        /// Einmal je Runde – darum darf es groß sein, anders als die Bodentextur, die sich in jedem Bild ändert.
        /// </summary>
        public Texture2D RenderSnapshot(int pxPerCell = 16)
        {
            int tw = _w * pxPerCell, th = _h * pxPerCell;
            var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[tw * th];
            for (int ty = 0; ty < th; ty++)
            {
                for (int tx = 0; tx < tw; tx++)
                {
                    var c = _pixels[(ty / pxPerCell) * _w + tx / pxPerCell];
                    bool edge = tx % pxPerCell == 0 || ty % pxPerCell == 0;
                    px[ty * tw + tx] = edge ? Palette.Shade(c, 0.18f) : c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Gitterlinien als dünne Streifen knapp über dem Boden – ein Netz, ein Material.</summary>
        private void BuildGridLines()
        {
            const float half = 0.025f, lift = 0.004f;
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            void Strip(Vector3 a, Vector3 b, Vector3 side)
            {
                int k = verts.Count;
                verts.Add(a - side); verts.Add(a + side); verts.Add(b + side); verts.Add(b - side);
                // Unity zeichnet nur die Vorderseite (Normale = Kreuzprodukt der Kanten). Senkrechte und waagerechte
                // Streifen haben entgegengesetzte Wickelrichtung – darum je Streifen so drehen, dass er nach oben zeigt.
                bool up = Vector3.Cross(verts[k + 1] - verts[k], verts[k + 2] - verts[k]).y > 0;
                if (up) { tris.Add(k); tris.Add(k + 1); tris.Add(k + 2); tris.Add(k); tris.Add(k + 2); tris.Add(k + 3); }
                else { tris.Add(k); tris.Add(k + 2); tris.Add(k + 1); tris.Add(k); tris.Add(k + 3); tris.Add(k + 2); }
            }
            for (int x = 0; x <= _w; x++) Strip(new Vector3(x, lift, 0), new Vector3(x, lift, _h), new Vector3(half, 0, 0));
            for (int z = 0; z <= _h; z++) Strip(new Vector3(0, lift, z), new Vector3(_w, lift, z), new Vector3(0, 0, half));
            var mesh = new Mesh { name = "GridLines" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            _gridLines = new GameObject("GridLines");
            _gridLines.transform.SetParent(transform, false);
            _gridLines.AddComponent<MeshFilter>().sharedMesh = mesh;
            _gridLines.AddComponent<MeshRenderer>().sharedMaterial = Prims.Mat(Palette.GridLine);
            _gridLines.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---------- Wärmefäden ----------

        public void AddHandThread(int a, int b)
        {
            var go = new GameObject("Thread");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Prims.Unlit;
            lr.startColor = lr.endColor = Palette.Thread;
            lr.startWidth = lr.endWidth = 0.09f;
            lr.positionCount = 12;
            Vector3 pa = CellCenter(a) + Vector3.up * 0.15f, pb = CellCenter(b) + Vector3.up * 0.15f;
            for (int k = 0; k < 12; k++)
            {
                float t = k / 11f;
                lr.SetPosition(k, Vector3.Lerp(pa, pb, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.35f);
            }
            _handThreads.Add(lr);
        }

        public void ClearThreads()
        {
            foreach (var lr in _handThreads) if (lr != null) Destroy(lr.gameObject);
            _handThreads.Clear();
            if (_threadsGo != null) Destroy(_threadsGo);
            _threadsGo = null;
        }

        /// <summary>Zeichnet alle Kopplungen nacheinander ein („der Computer übernimmt“).</summary>
        public IEnumerator DrawAllThreads(List<(int, int)> couplings, float seconds, System.Action<int> onProgress)
        {
            if (_threadsGo != null) Destroy(_threadsGo);
            _threadsGo = new GameObject("AllThreads");
            _threadsGo.transform.SetParent(transform, false);
            _threadsMesh = new Mesh { name = "Threads" };
            var verts = new Vector3[couplings.Count * 2];
            var cols = new Color[verts.Length];
            for (int k = 0; k < couplings.Count; k++)
            {
                verts[2 * k] = CellCenter(couplings[k].Item1) - transform.position + Vector3.up * 0.08f;
                verts[2 * k + 1] = CellCenter(couplings[k].Item2) - transform.position + Vector3.up * 0.08f;
                cols[2 * k] = cols[2 * k + 1] = Palette.Thread;
            }
            _threadsMesh.vertices = verts;
            _threadsMesh.colors = cols;
            _threadsMesh.bounds = new Bounds(new Vector3(_w / 2f, 0, _h / 2f), new Vector3(_w + 2, 2, _h + 2));
            _threadsGo.AddComponent<MeshFilter>().sharedMesh = _threadsMesh;
            _threadsGo.AddComponent<MeshRenderer>().sharedMaterial = Prims.Unlit;

            int shown = 0;
            for (float t = 0; shown < couplings.Count; t += Time.deltaTime)
            {
                int target = Mathf.Min(couplings.Count, Mathf.CeilToInt(couplings.Count * Mathf.Clamp01(t / seconds)));
                if (target != shown)
                {
                    shown = target;
                    var idx = new int[shown * 2];
                    for (int k = 0; k < idx.Length; k++) idx[k] = k;
                    _threadsMesh.SetIndices(idx, MeshTopology.Lines, 0);
                    onProgress?.Invoke(shown);
                }
                yield return null;
            }
        }
    }

    /// <summary>Kleines Auf-und-ab (planschendes Küken, Hinweis-Marker).</summary>
    public sealed class Bobber : MonoBehaviour
    {
        public float Amplitude = 0.05f;
        public float Speed = 5f;
        private Vector3 _base;
        private void Start() => _base = transform.localPosition;
        private void Update() => transform.localPosition = _base + Vector3.up * (Mathf.Sin(Time.time * Speed) * Amplitude);
    }
}
