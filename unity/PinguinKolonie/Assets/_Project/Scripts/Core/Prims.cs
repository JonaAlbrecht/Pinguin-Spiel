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
        private static readonly Dictionary<Color, Material> Cache = new Dictionary<Color, Material>();

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
        /// Ein Material je Farbe, geteilt. Farben werden auf 1/32 gerundet: Der Prolog färbt Pinguine
        /// stufenlos nach Temperatur, ohne Rundung entstanden so rund 100 neue Materialien pro Sekunde,
        /// die der Cache nie freigab (Review zu PR #2). 32 Stufen je Kanal sieht man nicht als Sprung.
        /// </summary>
        public static Material Mat(Color color)
        {
            color = Quantize(color);
            if (Cache.TryGetValue(color, out var m) && m != null) return m;
            m = new Material(LitBase) { color = color };
            ArtStyle.Shading.ApplyTo(m);
            Cache[color] = m;
            return m;
        }

        private static Color Quantize(Color c)
        {
            const float steps = 32f;
            return new Color(Mathf.Round(c.r * steps) / steps, Mathf.Round(c.g * steps) / steps,
                Mathf.Round(c.b * steps) / steps, Mathf.Round(c.a * steps) / steps);
        }

        /// <param name="segments">Segmente rundum; 0 = <see cref="ArtStyle.Detail.Segments"/> (Bauten und Gegenstände).</param>
        public static GameObject Make(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color,
            string name = null, bool keepCollider = false, int segments = 0)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            if (!keepCollider) Kill(go.GetComponent<Collider>());
            // Der Collider bleibt Unitys glatte Form; nur das Sichtbare wird facettiert.
            var mesh = Shapes.ForPrimitive(type, segments > 0 ? segments : ArtStyle.Detail.Segments);
            if (mesh != null) go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        public static void SetColor(GameObject go, Color color) => go.GetComponent<Renderer>().sharedMaterial = Mat(color);

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

        public enum Look { Plain, Pip, Frieda, Kalle, Grete, Theo, Klara, Flocke }

        /// <summary>Ein Pinguin nach den Stilregeln, ca. 0,76 m hoch (Größe 1). Bauch und „Ring“ sind einfärbbar.</summary>
        public static GameObject Penguin(Transform parent, string name, Look look = Look.Plain, float size = 1f) =>
            PenguinFigure.Build(parent, name, look, size);

        public static void SetPenguinTint(GameObject penguin, Color belly, Color ring) => PenguinFigure.Tint(penguin, belly, ring);

        // ---------- Bausteine der Siedlung ----------

        public static GameObject Igloo(Transform parent, float scale = 1f)
        {
            var root = new GameObject("Igloo");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;
            Make(PrimitiveType.Sphere, root.transform, new Vector3(0, 0f, 0), new Vector3(0.80f, 0.62f, 0.80f), Palette.Igloo, "Dome");
            Make(PrimitiveType.Cube, root.transform, new Vector3(0, 0.10f, 0.36f), new Vector3(0.24f, 0.22f, 0.16f), Palette.Igloo, "Entrance");
            Make(PrimitiveType.Cube, root.transform, new Vector3(0, 0.09f, 0.445f), new Vector3(0.14f, 0.16f, 0.02f), new Color(0.15f, 0.2f, 0.3f), "Door");
            return root;
        }

        public static GameObject Lantern(Transform parent, float scale = 1f, bool withLight = true)
        {
            var root = new GameObject("Lantern");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;
            Make(PrimitiveType.Cube, root.transform, new Vector3(0, 0.03f, 0), new Vector3(0.30f, 0.06f, 0.30f), Palette.LanternPole, "Base");
            Make(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.45f, 0), new Vector3(0.07f, 0.42f, 0.07f), Palette.LanternPole, "Pole");
            Make(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.92f, 0), Vector3.one * 0.26f, Palette.LanternGlow, "Lamp");
            Make(PrimitiveType.Cube, root.transform, new Vector3(0, 1.07f, 0), new Vector3(0.30f, 0.05f, 0.30f), Palette.LanternPole, "Cap");
            if (withLight)
            {
                var l = new GameObject("Light").AddComponent<Light>();
                l.transform.SetParent(root.transform, false);
                l.transform.localPosition = new Vector3(0, 0.9f, 0);
                l.type = LightType.Point;
                l.color = Palette.LanternGlow;
                l.range = 2.5f * scale;
                l.intensity = 1.2f;
            }
            return root;
        }

        public static GameObject SnowWall(Transform parent, float scale = 1f)
        {
            var root = new GameObject("SnowWall");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;
            Make(PrimitiveType.Cube, root.transform, new Vector3(0, 0.22f, 0), new Vector3(0.92f, 0.44f, 0.92f), Palette.Wall, "Block");
            Make(PrimitiveType.Cube, root.transform, new Vector3(0, 0.46f, 0), new Vector3(0.70f, 0.06f, 0.70f), Palette.Snow, "Top");
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
            var cyan = new Color(0.25f, 0.85f, 0.95f);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = Prims.Mat(r.name == "Torso" && i == PenguinFigure.BellySlot ? new Color(0.75f, 0.97f, 1f) : cyan);
                r.sharedMaterials = mats;
            }
            Prims.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0, -0.02f, 0), new Vector3(0.75f, 0.02f, 0.75f),
                new Color(0.1f, 0.4f, 0.5f), "Projector");
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
