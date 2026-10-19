using SFE.SimCore.Flow;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// ZWILLIS Windkanal als Bildschirm auf dem Labortisch (wie <see cref="SettlementBoard"/>: dunkler Rahmen,
    /// Leuchtkante, Beschriftung) – alles darauf ist Rechnung. Der Längsschnitt liegt flach: x = Zuglänge (nach rechts),
    /// Höhe über dem Boden = in die Tiefe (+z). Eine Fein-Einheit = <see cref="WorldPerUnit"/> Unity-Einheiten.
    /// Zeigt den Zug als Maske der gewählten Auflösung (Treppenstufen sichtbar), das Strömungsbild (Helligkeit =
    /// Geschwindigkeit, Rot = Wirbel), wandernde Lichtpunkte als Strömungslinien und die vier Ziehpunkte der Nase.
    /// </summary>
    public sealed class FlowBoard : MonoBehaviour
    {
        public const float WorldPerUnit = 0.15f;

        private PolarExpressLevel _lv;
        private FlowResolution _res;
        private int _px;
        private Texture2D _tex;
        private Color32[] _pixels;
        private Color32[] _cells;
        private float[] _trail;
        private bool[] _mask;
        private LbmD2Q9 _lbm;
        private TrainShape _shape;
        private Material _mat;

        private readonly Vector2[] _particles = new Vector2[260];
        private readonly float[] _age = new float[260];
        private System.Random _rnd = new System.Random(7);

        private GameObject _handleRoot;
        private readonly GameObject[] _handles = new GameObject[4];
        private readonly GameObject[] _handleLines = new GameObject[3];
        private int _activeHandle = -1;

        /// <summary>Kästchengrenzen einzeichnen (Schritt 3, grob und mittel – fein sind die Kästchen zu klein dafür).</summary>
        public bool ShowCells;

        public float Width => (float)_lv.ChannelLength * WorldPerUnit;
        public float Depth => (float)_lv.ChannelHeight * WorldPerUnit;
        public Vector3 Center => transform.position + new Vector3(Width / 2f, 0, Depth / 2f);
        public FlowResolution Resolution => _res;
        public Texture2D Texture => _tex;

        public static FlowBoard Create(Transform parent, Vector3 origin, PolarExpressLevel level, string screenLabel, string windLabel)
        {
            var go = new GameObject("FlowBoard");
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            var b = go.AddComponent<FlowBoard>();
            b._lv = level;
            b.BuildScreen(screenLabel, windLabel);
            b.BuildHandles();
            b.SetResolution(level.Resolution(level.PlayResolution));
            return b;
        }

        private void BuildScreen(string screenLabel, string windLabel)
        {
            const float rim = 0.7f, lip = 0.1f, glow = 0.07f;
            float w = Width, d = Depth;
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(w / 2f, -0.26f, d / 2f), new Vector3(w + rim * 2, 0.5f, d + rim * 2),
                Palette.ScreenFrame, "ScreenFrame");
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(w / 2f, lip / 2, -glow / 2), new Vector3(w + glow * 2, lip, glow), Palette.Screen, "GlowS");
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(w / 2f, lip / 2, d + glow / 2), new Vector3(w + glow * 2, lip, glow), Palette.Screen, "GlowN");
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(-glow / 2, lip / 2, d / 2f), new Vector3(glow, lip, d), Palette.Screen, "GlowW");
            Prims.Make(PrimitiveType.Cube, transform, new Vector3(w + glow / 2, lip / 2, d / 2f), new Vector3(glow, lip, d), Palette.Screen, "GlowE");
            var label = Prims.Label(transform, screenLabel, new Vector3(w / 2f, 0.03f, d + rim / 2), 0.06f, Palette.Screen);
            label.fontStyle = FontStyle.Bold;

            // Gegenwind-Pfeil links neben dem Kanal (die Luft kommt von links, der Zug fährt nach links)
            var arrow = new GameObject("WindArrow").transform;
            arrow.SetParent(transform, false);
            arrow.localPosition = new Vector3(-rim - 1.2f, 0.3f, d * 0.6f);
            Prims.Make(PrimitiveType.Cube, arrow, new Vector3(-0.3f, 0, 0), new Vector3(1.2f, 0.1f, 0.22f), Palette.Mg2WindArrow, "Shaft");
            var head = Prims.Make(PrimitiveType.Cube, arrow, new Vector3(0.35f, 0, 0), new Vector3(0.5f, 0.1f, 0.5f), Palette.Mg2WindArrow, "Head");
            head.transform.localRotation = Quaternion.Euler(0, 45, 0);
            Prims.Label(arrow, windLabel, new Vector3(0, 0.3f, -0.7f), 0.045f, Palette.Mg2WindArrow);
        }

        private void BuildHandles()
        {
            _handleRoot = new GameObject("NoseHandles");
            _handleRoot.transform.SetParent(transform, false);
            for (int i = 0; i < 3; i++)
                _handleLines[i] = Prims.Make(PrimitiveType.Cube, _handleRoot.transform, Vector3.zero, Vector3.one, Palette.Mg2HandleLine, "HandleLine");
            for (int i = 0; i < 4; i++)
                _handles[i] = Prims.Make(PrimitiveType.Sphere, _handleRoot.transform, Vector3.zero, Vector3.one * 0.42f, Palette.Mg2Handle, "Handle" + i);
            _handleRoot.SetActive(false);
        }

        // ---------- Koordinaten ----------

        /// <summary>Punkt im Kanal (Fein-Einheiten, y = Höhe) als Weltpunkt knapp über dem Bildschirm.</summary>
        public Vector3 ToWorld(double x, double y, float lift = 0.05f) =>
            transform.position + new Vector3((float)x * WorldPerUnit, lift, (float)y * WorldPerUnit);

        /// <summary>Zeiger auf dem Bildschirm → Kanal-Koordinaten (Fein-Einheiten). False außerhalb des Kanals.</summary>
        public bool PointerToChannel(Camera cam, out double x, out double y)
        {
            x = y = 0;
            if (!InputHelper.PointerOnGround(cam, out var p, transform.position.y)) return false;
            var local = p - transform.position;
            x = local.x / WorldPerUnit;
            y = local.z / WorldPerUnit;
            return x >= -4 && y >= -4 && x <= _lv.ChannelLength + 4 && y <= _lv.ChannelHeight + 4;
        }

        // ---------- Inhalt ----------

        public void SetResolution(FlowResolution r)
        {
            _res = r;
            _px = Mathf.Max(2, Mathf.RoundToInt(2f / (float)r.Scale));
            int tw = r.Nx * _px, th = r.Ny * _px;
            if (_tex != null) Destroy(_tex);
            _tex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            _pixels = new Color32[tw * th];
            _trail = new float[tw * th];
            _cells = new Color32[r.Nx * r.Ny];
            if (_mat == null)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(quad.GetComponent<Collider>());
                quad.name = "Channel";
                quad.transform.SetParent(transform, false);
                quad.transform.localPosition = new Vector3(Width / 2f, 0.002f, Depth / 2f);
                quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
                quad.transform.localScale = new Vector3(Width, Depth, 1);
                _mat = new Material(Prims.Unlit) { color = Color.white };
                quad.GetComponent<Renderer>().sharedMaterial = _mat;
            }
            _mat.mainTexture = _tex;
            _lbm = null;
            if (_shape != null) ShowShape(_shape);
        }

        /// <summary>Zeigt den Zug (Maske der aktuellen Auflösung) in ruhender Luft.</summary>
        public void ShowShape(TrainShape shape)
        {
            _shape = shape.Clone();
            _mask = TrainGeometry.Mask(_shape, _lv, _res);
            _lbm = null;
            System.Array.Clear(_trail, 0, _trail.Length);
            Redraw();
            if (_handleRoot.activeSelf) PlaceHandles();
        }

        /// <summary>Zeigt eine laufende Rechnung; <see cref="Tick"/> malt sie jedes Bild neu.</summary>
        public void ShowRun(TrainRun run)
        {
            if (run.Resolution.Id != _res.Id) SetResolution(run.Resolution);
            _shape = run.Shape.Clone();
            _mask = run.Lbm.Solid;
            _lbm = run.Lbm;
            _rnd = new System.Random(7);
            for (int i = 0; i < _particles.Length; i++) Respawn(i, true);
            System.Array.Clear(_trail, 0, _trail.Length);
            Redraw();
        }

        public void Clear()
        {
            _lbm = null;
            System.Array.Clear(_trail, 0, _trail.Length);
            Redraw();
        }

        public void Tick(float dt)
        {
            if (_lbm == null) return;
            MoveParticles(Mathf.Min(dt, 0.05f));
            Redraw();
        }

        // ---------- Ziehpunkte ----------

        public void ShowHandles(bool on)
        {
            _handleRoot.SetActive(on);
            if (on) PlaceHandles();
        }

        public void SetActiveHandle(int i)
        {
            _activeHandle = i;
            for (int k = 0; k < 4; k++) Prims.SetColor(_handles[k], k == i ? Palette.Mg2HandleActive : Palette.Mg2Handle);
        }

        /// <summary>Nächster Ziehpunkt unter dem Zeiger (−1: keiner in Reichweite).</summary>
        public int HandleNear(double x, double y, double radius = 4.0)
        {
            if (_shape == null) return -1;
            int best = -1;
            double bestD = radius * radius;
            for (int i = 0; i < 4; i++)
            {
                double dx = _shape.X[i] - x, dy = _shape.Y[i] - y;
                double d = dx * dx + dy * dy;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        private void PlaceHandles()
        {
            if (_shape == null) return;
            for (int i = 0; i < 4; i++) _handles[i].transform.position = ToWorld(_shape.X[i], _shape.Y[i], 0.25f);
            for (int i = 0; i < 3; i++)
            {
                var a = _handles[i].transform.position;
                var b = _handles[i + 1].transform.position;
                var line = _handleLines[i].transform;
                float len = Vector3.Distance(a, b);
                line.gameObject.SetActive(len > 0.01f);
                if (len <= 0.01f) continue;
                line.position = (a + b) / 2f;
                line.rotation = Quaternion.LookRotation(b - a, Vector3.up);
                line.localScale = new Vector3(0.06f, 0.03f, len);
            }
        }

        // ---------- Malen ----------

        private void Respawn(int i, bool anywhere)
        {
            double x = anywhere ? _rnd.NextDouble() * _lv.ChannelLength * 0.95 : _rnd.NextDouble() * 2.0;
            double y = 0.5 + _rnd.NextDouble() * (_lv.ChannelHeight * 0.6 - 0.5); // dort, wo der Zug die Luft lenkt
            _particles[i] = new Vector2((float)x, (float)y);
            _age[i] = (float)(_rnd.NextDouble() * 3.0);
        }

        /// <summary>Lichtpunkte treiben mit der gerechneten Luft (Kästchen unter dem Punkt), Spur verblasst.</summary>
        private void MoveParticles(float dt)
        {
            const float unitsPerSecond = 40f / 0.1f; // bei voller Einströmgeschwindigkeit ~40 Fein-Einheiten je Sekunde
            int nx = _res.Nx, ny = _res.Ny, tw = nx * _px;
            for (int i = 0; i < _trail.Length; i++) _trail[i] *= 0.86f;
            for (int i = 0; i < _particles.Length; i++)
            {
                var p = _particles[i];
                int cx = Mathf.FloorToInt(p.x * (float)_res.Scale), cy = Mathf.FloorToInt(p.y * (float)_res.Scale);
                _age[i] += dt;
                if (cx < 0 || cy < 0 || cx >= nx || cy >= ny || _mask[cy * nx + cx] || _age[i] > 7f) { Respawn(i, false); continue; }
                _lbm.Moments(cy * nx + cx, out _, out double ux, out double uy);
                p.x += (float)ux * unitsPerSecond * dt;
                p.y += (float)uy * unitsPerSecond * dt;
                _particles[i] = p;
                int px = Mathf.FloorToInt(p.x * (float)_res.Scale * _px), py = Mathf.FloorToInt(p.y * (float)_res.Scale * _px);
                if (px >= 0 && py >= 0 && px < tw && py < ny * _px) _trail[py * tw + px] = 1f;
            }
        }

        private void Redraw()
        {
            int nx = _res.Nx, ny = _res.Ny;
            double u = _lv.U;
            for (int y = 0; y < ny; y++)
            {
                for (int x = 0; x < nx; x++)
                {
                    int c = y * nx + x;
                    if (_mask != null && _mask[c])
                    {
                        double xc = (x + 0.5) / _res.Scale;
                        int t = TrainGeometry.TransitionAt(_lv, xc);
                        bool open = t >= 0 && !_shape.Covered[t];
                        _cells[c] = open ? Palette.Mg2TransitionOpen : Palette.Mg2Train;
                        continue;
                    }
                    if (_lbm == null) { _cells[c] = Palette.Mg2AirStill; continue; }
                    _lbm.Moments(c, out _, out double ux, out double uy);
                    _cells[c] = AirColor(ux / u, uy / u);
                }
            }
            int tw = nx * _px;
            Color32 particle = Palette.Mg2Particle, line = Palette.Mg2CellLine;
            bool cells = ShowCells && _px >= 3;
            for (int py = 0; py < ny * _px; py++)
            {
                int cy = py / _px;
                for (int px = 0; px < tw; px++)
                {
                    int i = py * tw + px;
                    var col = _cells[cy * nx + px / _px];
                    if (cells && (px % _px == 0 || py % _px == 0)) col = Color32.Lerp(col, line, 0.6f);
                    float tr = _trail[i];
                    if (tr > 0.02f) col = Color32.Lerp(col, particle, tr);
                    _pixels[i] = col;
                }
            }
            _tex.SetPixels32(_pixels);
            _tex.Apply(false);
        }

        /// <summary>Helligkeit = Geschwindigkeit (relativ zur Einströmung), Rot = Luft strömt zurück (Wirbel).</summary>
        private static Color32 AirColor(double rx, double ry)
        {
            if (rx < -0.01) return Color.Lerp(Palette.Mg2AirSlow, Palette.Mg2Swirl, Mathf.Clamp01((float)(-rx * 6.0) + 0.35f));
            float s = Mathf.Sqrt((float)(rx * rx + ry * ry));
            if (s < 0.5f) return Color.Lerp(Palette.Mg2AirStill, Palette.Mg2AirSlow, s / 0.5f);
            if (s < 1f) return Color.Lerp(Palette.Mg2AirSlow, Palette.Mg2AirFast, (s - 0.5f) / 0.5f);
            return Color.Lerp(Palette.Mg2AirFast, Palette.Mg2AirFastest, Mathf.Clamp01((s - 1f) / 0.4f));
        }

        /// <summary>Strömungsbild für „So macht's Siemens“: Kopie des aktuellen Bildes, weich gefiltert.</summary>
        public Texture2D Snapshot()
        {
            var tex = new Texture2D(_tex.width, _tex.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(_tex.GetPixels32());
            tex.Apply();
            return tex;
        }

        private void OnDestroy()
        {
            if (_tex != null) Destroy(_tex);
            if (_mat != null) Destroy(_mat);
        }
    }
}
