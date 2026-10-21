using System.Collections.Generic;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Baut die Grafik aus Grundformen. Runde Formen kommen aus <see cref="Shapes"/> und folgen damit den
    /// Stilregeln in <see cref="ArtStyle"/> (Facetten, Segmentzahl). Materialien werden vom Standard-Material der
    /// Primitive abgeleitet, damit das Spiel in der Built-in-Pipeline und in URP ohne eigene Shader läuft.
    /// </summary>
    public static class Prims
    {
        private static Material _lit;
        private static Material _unlit;
        private static readonly Dictionary<(Color, ArtStyle.Surface, int), Material> Cache = new Dictionary<(Color, ArtStyle.Surface, int), Material>();
        /// <summary>Welche Oberfläche ein Material hat – damit Umfärben (SetColor) Schnee Schnee bleiben lässt.</summary>
        private static readonly Dictionary<Material, (ArtStyle.Surface, int)> SurfaceOf = new Dictionary<Material, (ArtStyle.Surface, int)>();

        private static Material LitBase
        {
            get
            {
                if (_lit == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _lit = tmp.GetComponent<Renderer>().sharedMaterial;
                    Kill(tmp);
                }
                return _lit;
            }
        }

        /// <summary>Unbeleuchtetes Material mit Vertex-Farben (für Wärmebild, Fäden, Markierungen).</summary>
        public static Material Unlit
        {
            get
            {
                if (_unlit == null)
                {
                    // Vom Editor-Menü „Prototyp-Szene erstellen“ angelegt, damit der Shader im Build enthalten ist.
                    var asset = Resources.Load<Material>("Materials/Unlit");
                    if (asset != null) return _unlit = asset;
                    var shader = Shader.Find("Sprites/Default");
                    if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null) shader = Shader.Find("Unlit/Texture");
                    _unlit = shader != null ? new Material(shader) : new Material(LitBase);
                }
                return _unlit;
            }
        }

        /// <summary>
        /// Ein Material je Farbe, geteilt. Farben werden auf 1/32 gerundet: Der frühere Prolog färbte Pinguine
        /// stufenlos nach Temperatur, ohne Rundung entstanden so rund 100 neue Materialien pro Sekunde,
        /// die der Cache nie freigab (Review zu PR #2). 32 Stufen je Kanal sieht man nicht als Sprung.
        /// </summary>
        public static Material Mat(Color color) => Mat(color, ArtStyle.Surfaces.ByColor(Quantize(color)), 1);

        /// <summary>
        /// Material mit physikalischer Oberfläche (ADR-27). <paramref name="tiling"/> = Wiederholungen der Struktur
        /// über das ganze Teil – aus der Größe in der Welt, damit Schneekörner auf Scholle und Iglu gleich groß sind.
        /// </summary>
        public static Material Mat(Color color, ArtStyle.Surface surface, int tiling)
        {
            color = Quantize(color);
            var key = (color, surface, tiling);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(LitBase) { color = color };
            ArtStyle.Surfaces.ApplyTo(m, surface, tiling);
            Cache[key] = m;
            SurfaceOf[m] = (surface, tiling);
            if (ArtStyle.Surfaces.For(surface).Flow > 0f) RenderLook.AddFlowing(m, ArtStyle.Surfaces.For(surface));
            return m;
        }

        private static readonly Dictionary<Texture, Material> TexturedCache = new Dictionary<Texture, Material>();

        /// <summary>Material mit Farbbild statt Einzelfarbe (Pinguinkörper mit gemalten Farbgrenzen, ADR-27).</summary>
        public static Material Textured(Texture2D albedo)
        {
            if (TexturedCache.TryGetValue(albedo, out var m) && m != null) return m;
            m = new Material(LitBase) { color = Color.white, mainTexture = albedo };
            ArtStyle.Surfaces.ApplyTo(m, ArtStyle.Surface.Feather);
            TexturedCache[albedo] = m;
            return m;
        }

        /// <summary>Wiederholungen der Struktur für ein Teil dieser Größe (Meter).</summary>
        public static int TilingFor(ArtStyle.Surface surface, float size) =>
            Mathf.Max(1, Mathf.RoundToInt(size * ArtStyle.Surfaces.For(surface).TilesPerMeter));

        /// <summary>Oberfläche eines gebauten Teils (und aller Kinder) setzen, Farbe bleibt.</summary>
        public static void SetSurface(GameObject go, ArtStyle.Surface surface)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponent<TextMesh>() != null) continue; // Schilder behalten ihre Schrift
                var b = r.bounds.size;
                r.sharedMaterial = Mat(r.sharedMaterial.color, surface, TilingFor(surface, Mathf.Max(b.x, b.z)));
            }
        }

        internal static Color Quantize(Color c)
        {
            const float steps = 32f;
            return new Color(Mathf.Round(c.r * steps) / steps, Mathf.Round(c.g * steps) / steps,   // Stil-Lint: Rechnung
                Mathf.Round(c.b * steps) / steps, Mathf.Round(c.a * steps) / steps);
        }

        /// <param name="segments">Segmente rundum; 0 = nach Größe in der Welt (<see cref="ArtStyle.Detail.SegmentsFor"/>).</param>
        public static GameObject Make(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color,
            string name = null, bool keepCollider = false, int segments = 0)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            if (!keepCollider) Kill(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            // Der Collider bleibt Unitys glatte Form; nur das Sichtbare wird facettiert. Der Umfang zählt in der Welt,
            // damit eine Eisscholle von 20 m mehr Segmente bekommt als eine Laternenkugel.
            if (segments <= 0)
            {
                var s = go.transform.lossyScale;
                float diameter = type == PrimitiveType.Sphere ? Mathf.Max(s.x, s.y, s.z) : Mathf.Max(s.x, s.z);
                segments = ArtStyle.Detail.SegmentsFor(diameter);
            }
            var mesh = Shapes.ForPrimitive(type, segments);
            if (mesh != null) go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var surface = ArtStyle.Surfaces.ByColor(Quantize(color));
            var ws = go.transform.lossyScale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color, surface, TilingFor(surface, Mathf.Max(ws.x, ws.z)));
            return go;
        }

        /// <summary>Teil mit eigener Form aus <see cref="Shapes"/> (Drehprofil), sonst wie <see cref="Make"/>.</summary>
        private static GameObject Shaped(Mesh mesh, Transform parent, Vector3 localPos, Vector3 scale, Color color, string name)
        {
            var go = Make(PrimitiveType.Cube, parent, localPos, scale, color, name);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            return go;
        }

        /// <summary>Umfärben – die Oberfläche (Schnee, Eis …) bleibt, nur die Farbe wechselt.</summary>
        public static void SetColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            var (surface, tiling) = r.sharedMaterial != null && SurfaceOf.TryGetValue(r.sharedMaterial, out var st)
                ? st : (ArtStyle.Surfaces.ByColor(Quantize(color)), 1);
            r.sharedMaterial = Mat(color, surface, tiling);
        }

        /// <summary>Destroy im Spiel, DestroyImmediate im Editor (Stilprobe baut Figuren ohne Play-Modus).</summary>
        public static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        public static TextMesh Label(Transform parent, string text, Vector3 localPos, float size, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = UIKit.Font;
            tm.text = text;
            // Die Schrift wird in fontSize gerastert und mit characterSize skaliert. 64 wirkte vergrößert unscharf;
            // 160 bei entsprechend kleinerem characterSize ergibt dieselbe Größe auf dem Bildschirm, aber scharf.
            const int raster = 160;
            tm.fontSize = raster;
            tm.characterSize = size * 64f / raster;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = UIKit.Font.material;
            go.AddComponent<Billboard>();
            return tm;
        }

        // ---------- Figuren ----------

        public enum Look { Plain, Pip, Frieda, Kalle, Grete, Theo, Klara, Flocke, Tilda, Olaf }

        /// <summary>Ein Pinguin nach den Stilregeln, ca. 0,76 m hoch (Größe 1).</summary>
        public static GameObject Penguin(Transform parent, string name, Look look = Look.Plain, float size = 1f) =>
            PenguinFigure.Build(parent, name, look, size);

        // ---------- Bausteine der Siedlung ----------

        /// <summary>Kuppel des Iglus: Halbachsen in Metern bei Größe 1 (Mitte auf dem Boden, untere Hälfte im Eis).</summary>
        private const float IglooRadius = 0.40f, IglooHeight = 0.31f;
        /// <summary>Schneeblock-Reihen übereinander und angestrebte Blocklänge (Meter) – daraus die Fugen.</summary>
        private const int IglooCourses = 4;
        private const float IglooBlockLength = 0.22f, IglooJointWidth = 0.018f;

        /// <summary>
        /// Iglu aus Schneeblöcken. Ein heller Iglu auf hellem Eis las man nur am Schatten (Spieltest 21.10.2026). Darum
        /// zeichnen dunklere Fugen (<see cref="Palette.IglooJoint"/>) die Blockreihen nach – wie bei einem echten Iglu,
        /// dessen Fugen im Streiflicht dunkel stehen –, und ein Tunnel mit dunklem Eingang zeigt die Vorderseite.
        /// Die Kuppel bleibt hell: Ihre Farbe zeigt in MG1 den Zustand (kalt, gemütlich, schmilzt) und heißt „Dome“.
        /// </summary>
        public static GameObject Igloo(Transform parent, float scale = 1f)
        {
            var root = new GameObject("Igloo");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;
            var t = root.transform;
            SetSurface(Make(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(2f * IglooRadius, 2f * IglooHeight, 2f * IglooRadius), Palette.Igloo, "Dome"),
                ArtStyle.Surface.Snow);

            // Waagerechte Fugen: dünne Scheiben, die nur mit ihrem Rand aus der Kuppel schauen.
            for (int k = 1; k < IglooCourses; k++)
            {
                float y = IglooHeight * k / IglooCourses, r = IglooRing(y) + IglooJointWidth * 0.35f;
                Make(PrimitiveType.Cylinder, t, new Vector3(0, y, 0), new Vector3(2f * r, IglooJointWidth * 0.35f, 2f * r), Palette.IglooJoint, "Joint");
            }
            // Senkrechte Fugen, je Reihe um einen halben Block versetzt; über dem Tunnel und im Schlussstein keine.
            for (int c = 0; c < IglooCourses - 1; c++)
            {
                float y0 = IglooHeight * c / IglooCourses, y1 = IglooHeight * (c + 1) / IglooCourses;
                float r0 = IglooRing(y0), r1 = IglooRing(y1), ym = 0.5f * (y0 + y1), rm = IglooRing(ym);
                int n = Mathf.Max(4, Mathf.RoundToInt(2f * Mathf.PI * rm / IglooBlockLength));
                float len = new Vector2(r1 - r0, y1 - y0).magnitude;
                for (int i = 0; i < n; i++)
                {
                    float a = (i + 0.5f * (c % 2)) * 360f / n;
                    if (Mathf.Abs(Mathf.DeltaAngle(a, 180f)) < 24f) continue;
                    var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                    var along = (d * (r1 - r0) + Vector3.up * (y1 - y0)).normalized;
                    var outward = Vector3.Cross(along, Vector3.Cross(d, along)).normalized;
                    if (Vector3.Dot(outward, d) < 0f) outward = -outward;
                    var joint = Make(PrimitiveType.Cube, t, d * rm + Vector3.up * ym + outward * 0.004f,
                        new Vector3(IglooJointWidth * 0.8f, len, IglooJointWidth * 0.5f), Palette.IglooJoint, "Joint");
                    joint.transform.localRotation = Quaternion.LookRotation(outward, along);
                }
            }

            // Tunnel: halber Schneezylinder vor der Kuppel, darin der dunkle Eingang (heißt „Door“: MG1 färbt ihn warm).
            // Nach Süden (−z): weg vom Wind aus Nordwest (Leveldaten) und zur Spielkamera hin – früher zeigte er nach
            // Norden und man sah ihn nie.
            var tunnel = Make(PrimitiveType.Cylinder, t, new Vector3(0, 0.01f, -0.36f), new Vector3(0.28f, 0.11f, 0.32f), Palette.Igloo, "Entrance");
            tunnel.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            SetSurface(tunnel, ArtStyle.Surface.Snow);
            var door = Make(PrimitiveType.Cylinder, t, new Vector3(0, 0.01f, -0.473f), new Vector3(0.17f, 0.004f, 0.22f), Palette.Door, "Door");
            door.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            return root;
        }

        /// <summary>Radius der Iglukuppel in Höhe <paramref name="y"/> (Ellipse aus <see cref="IglooRadius"/>, <see cref="IglooHeight"/>).</summary>
        private static float IglooRing(float y) => IglooRadius * Mathf.Sqrt(Mathf.Max(0f, 1f - (y / IglooHeight) * (y / IglooHeight)));

        /// <summary>
        /// Gusseiserner Mast als Drehprofil (Höhe, Radius) in Metern bei Größe 1, von unten nach oben: runder Sockel mit
        /// Stufe, ausgestellter Fuß, schlanker Schaft, Kragen unter dem Kopf. Doppelte Punkte = harte Kante.
        /// </summary>
        private static readonly Vector3[] LanternPost =
        {
            new Vector3(0f, 0f), new Vector3(0f, 0.14f), new Vector3(0f, 0.14f), new Vector3(0.045f, 0.14f), new Vector3(0.045f, 0.14f),
            new Vector3(0.045f, 0.115f), new Vector3(0.045f, 0.115f), new Vector3(0.075f, 0.098f), new Vector3(0.13f, 0.078f),
            new Vector3(0.19f, 0.062f), new Vector3(0.22f, 0.062f), new Vector3(0.245f, 0.045f), new Vector3(0.72f, 0.032f),
            new Vector3(0.735f, 0.05f), new Vector3(0.755f, 0.05f), new Vector3(0.78f, 0.07f), new Vector3(0.78f, 0.07f), new Vector3(0.78f, 0f),
        };
        /// <summary>Geschwungenes Laternendach mit Knauf, gleiche Schreibweise; sitzt auf der Deckplatte.</summary>
        private static readonly Vector3[] LanternRoof =
        {
            new Vector3(0f, 0f), new Vector3(0f, 0.118f), new Vector3(0f, 0.118f), new Vector3(0.012f, 0.118f), new Vector3(0.04f, 0.088f),
            new Vector3(0.075f, 0.045f), new Vector3(0.098f, 0.022f), new Vector3(0.105f, 0.018f), new Vector3(0.115f, 0.026f),
            new Vector3(0.13f, 0.022f), new Vector3(0.14f, 0f),
        };

        /// <summary>
        /// Laterne nach Art einer Straßenlaterne, ca. 1,15 m bei Größe 1: Mast und Dach aus Gusseisen (Metall), dazwischen
        /// ein Glaskasten, der leuchtet, mit Eckstreben. Das Dach ist kaum breiter als das Glas, damit man das Licht auch
        /// von schräg oben (Spielbrett, 50–64°) sieht. „Lamp“ ist das Glas.
        /// </summary>
        public static GameObject Lantern(Transform parent, float scale = 1f, bool withLight = true)
        {
            var root = new GameObject("Lantern");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;
            var t = root.transform;
            int segs = ArtStyle.Detail.SegmentsFor(0.28f * scale);
            SetSurface(Shaped(Shapes.Cached("lanternPost" + segs, () => Shapes.Revolve(LanternPost, segs)), t, Vector3.zero, Vector3.one,
                Palette.LanternPole, "Pole"), ArtStyle.Surface.Metal);
            // Boden und Glas werfen keinen Schatten: Das Licht sitzt im Glas, der Boden darunter würde sonst einen
            // dunklen Fleck unter die eigene Laterne legen.
            var tray = Make(PrimitiveType.Cube, t, new Vector3(0, 0.79f, 0), new Vector3(0.19f, 0.02f, 0.19f), Palette.LanternPole, "Base");
            SetSurface(tray, ArtStyle.Surface.Metal);
            tray.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var glass = Make(PrimitiveType.Cube, t, new Vector3(0, 0.895f, 0), new Vector3(0.13f, 0.19f, 0.13f), Palette.LanternGlow, "Lamp");
            glass.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < 4; i++)
            {
                var corner = new Vector3(i % 2 == 0 ? -0.07f : 0.07f, 0.895f, i < 2 ? -0.07f : 0.07f);
                SetSurface(Make(PrimitiveType.Cube, t, corner, new Vector3(0.022f, 0.21f, 0.022f), Palette.LanternPole, "Strut"), ArtStyle.Surface.Metal);
            }
            SetSurface(Make(PrimitiveType.Cube, t, new Vector3(0, 1.0f, 0), new Vector3(0.19f, 0.02f, 0.19f), Palette.LanternPole, "Cap"), ArtStyle.Surface.Metal);
            SetSurface(Shaped(Shapes.Cached("lanternRoof" + segs, () => Shapes.Revolve(LanternRoof, segs)), t, new Vector3(0, 1.01f, 0), Vector3.one,
                Palette.LanternPole, "Roof"), ArtStyle.Surface.Metal);
            if (withLight)
            {
                var l = new GameObject("Light").AddComponent<Light>();
                l.transform.SetParent(t, false);
                l.transform.localPosition = new Vector3(0, 0.895f, 0);
                l.type = LightType.Point;
                l.color = Palette.LanternGlow;
                l.range = ArtStyle.Shading.LanternRange * scale;
                l.intensity = ArtStyle.Shading.LanternIntensity * scale * scale;
                // Schatten bekommen nur die Laternen nahe am Blickpunkt – das entscheidet RenderLook pro Bild (Audit B-02).
                l.shadows = LightShadows.None;
                RenderLook.AddLantern(l);
            }
            return root;
        }

        public static GameObject SnowWall(Transform parent, float scale = 1f)
        {
            var root = new GameObject("SnowWall");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;
            SetSurface(Make(PrimitiveType.Cube, root.transform, new Vector3(0, 0.22f, 0), new Vector3(0.92f, 0.44f, 0.92f), Palette.Wall, "Block"), ArtStyle.Surface.Snow);
            SetSurface(Make(PrimitiveType.Cube, root.transform, new Vector3(0, 0.46f, 0), new Vector3(0.70f, 0.06f, 0.70f), Palette.Snow, "Top"), ArtStyle.Surface.Snow);
            return root;
        }

        public static GameObject Puddle(Transform parent, float scale = 1f)
        {
            var root = new GameObject("Puddle");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;
            Make(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.01f, 0), new Vector3(0.85f, 0.01f, 0.75f), Palette.Water, "Water");
            return root;
        }
    }

    /// <summary>
    /// ZWILLI als Hologramm: ein leuchtend türkiser Pinguin, der flackert und zittert. Je kleiner
    /// <see cref="Glitch"/>, desto stabiler – ZWILLI „wächst“ mit jedem Schritt (docs/02).
    /// Ohne Transparenz gebaut: Durchsichtige Materialien bräuchten je nach Render-Pipeline eigene Shader.
    /// </summary>
    public sealed class Hologram : MonoBehaviour
    {
        public float Glitch = 0.6f;
        private Renderer[] _parts;
        private Vector3 _base;
        private float _nextFlicker;

        public static GameObject Create(Transform parent, float size = 1f)
        {
            var root = Prims.Penguin(parent, "ZWILLI", Prims.Look.Plain, size);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = Prims.Mat(r.name == "Torso" && i == PenguinFigure.BellySlot ? Palette.HologramBelly : Palette.Hologram);
                r.sharedMaterials = mats;
            }
            Prims.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0, -0.02f, 0), new Vector3(0.75f, 0.02f, 0.75f),
                Palette.HologramProjector, "Projector");
            root.AddComponent<Hologram>();
            return root;
        }

        private void Start()
        {
            _parts = GetComponentsInChildren<Renderer>();
            _base = transform.localPosition;
        }

        private void Update()
        {
            transform.localPosition = _base + Vector3.up * (0.12f + Mathf.Sin(Time.time * 2f) * 0.04f);
            if (Time.time < _nextFlicker) return;
            _nextFlicker = Time.time + Random.Range(0.05f, 0.3f);
            bool glitchNow = Random.value < Glitch * 0.5f;
            foreach (var r in _parts) if (r.name != "Projector") r.enabled = !glitchNow || Random.value > 0.5f;
            transform.localPosition += glitchNow ? new Vector3(Random.Range(-0.05f, 0.05f), 0, 0) * Glitch : Vector3.zero;
        }
    }

    /// <summary>Kolonie-Pinguine im Hub watscheln gemächlich umher und bleiben in ihrem Kreis.</summary>
    public sealed class Wanderer : MonoBehaviour
    {
        public Vector3 Home;
        public float Radius = 2.5f;
        private Vector3 _target;
        private float _wait;
        private Transform _body;

        private void Start()
        {
            Home = transform.localPosition;
            _target = Home;
            _body = transform.Find("Body");
        }

        private void Update()
        {
            if (_wait > 0) { _wait -= Time.deltaTime; if (_body != null) _body.localRotation = Quaternion.identity; return; }
            var to = _target - transform.localPosition;
            to.y = 0;
            if (to.magnitude < 0.05f)
            {
                var r = Random.insideUnitCircle * Radius;
                _target = Home + new Vector3(r.x, 0, r.y);
                _wait = Random.Range(1.5f, 4f);
                return;
            }
            transform.localPosition += to.normalized * Mathf.Min(0.8f * Time.deltaTime, to.magnitude);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.LookRotation(to.normalized, Vector3.up), 5f * Time.deltaTime);
            if (_body != null) _body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 12f) * 7f);
        }
    }

    /// <summary>Beschriftungen drehen sich immer zur Kamera.</summary>
    public sealed class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
