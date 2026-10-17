using System.Collections.Generic;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Baut die Platzhalter-Grafik aus Unity-Primitiven. Wird später durch Blender-Assets ersetzt
    /// (docs/04 Abschnitt 6). Materialien werden vom Standard-Material der Primitive abgeleitet,
    /// damit der Prototyp in der Built-in-Pipeline und in URP ohne eigene Shader läuft.
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
                    Object.Destroy(tmp);
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
            Cache[color] = m;
            return m;
        }

        private static Color Quantize(Color c)
        {
            const float steps = 32f;
            return new Color(Mathf.Round(c.r * steps) / steps, Mathf.Round(c.g * steps) / steps,
                Mathf.Round(c.b * steps) / steps, Mathf.Round(c.a * steps) / steps);
        }

        public static GameObject Make(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color,
            string name = null, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        public static void SetColor(GameObject go, Color color) => go.GetComponent<Renderer>().sharedMaterial = Mat(color);

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

        /// <summary>Ein Pinguin aus Primitiven, ca. 0,6 m hoch. Gibt die Wurzel zurück; „Belly“ und „Ring“ sind einfärbbar.</summary>
        public static GameObject Penguin(Transform parent, string name, Look look = Look.Plain, float size = 1f)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * size;
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);

            Color bodyColor = look == Look.Grete ? new Color(0.35f, 0.37f, 0.42f) : Palette.PenguinBlack;
            Make(PrimitiveType.Capsule, body, new Vector3(0, 0.30f, 0), new Vector3(0.42f, 0.30f, 0.38f), bodyColor, "Torso");
            Make(PrimitiveType.Sphere, body, new Vector3(0, 0.27f, 0.09f), new Vector3(0.32f, 0.42f, 0.24f), Palette.PenguinWhite, "Belly");
            Make(PrimitiveType.Sphere, body, new Vector3(0, 0.62f, 0.02f), new Vector3(0.30f, 0.28f, 0.28f), bodyColor, "Head");
            Make(PrimitiveType.Sphere, body, new Vector3(-0.06f, 0.66f, 0.13f), Vector3.one * 0.06f, Color.white, "EyeL");
            Make(PrimitiveType.Sphere, body, new Vector3(0.06f, 0.66f, 0.13f), Vector3.one * 0.06f, Color.white, "EyeR");
            Make(PrimitiveType.Cube, body, new Vector3(0, 0.60f, 0.17f), new Vector3(0.06f, 0.05f, 0.10f), Palette.Beak, "Beak");
            Make(PrimitiveType.Cube, body, new Vector3(-0.08f, 0.02f, 0.06f), new Vector3(0.10f, 0.03f, 0.14f), Palette.Beak, "FootL");
            Make(PrimitiveType.Cube, body, new Vector3(0.08f, 0.02f, 0.06f), new Vector3(0.10f, 0.03f, 0.14f), Palette.Beak, "FootR");
            Make(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.005f, 0), new Vector3(0.62f, 0.005f, 0.62f),
                new Color(1, 1, 1, 0.5f), "Ring");

            switch (look)
            {
                case Look.Pip:
                    Make(PrimitiveType.Cube, body, new Vector3(0, 0.50f, 0.02f), new Vector3(0.34f, 0.06f, 0.32f), new Color(0.85f, 0.15f, 0.20f), "Scarf");
                    Make(PrimitiveType.Cube, body, new Vector3(0.20f, 0.30f, 0.05f), new Vector3(0.04f, 0.14f, 0.10f), new Color(0.2f, 0.8f, 0.9f), "Tablet");
                    break;
                case Look.Frieda:
                    Make(PrimitiveType.Cube, body, new Vector3(-0.10f, 0.75f, 0.02f), new Vector3(0.05f, 0.10f, 0.03f), new Color(1f, 0.85f, 0.1f), "TuftL");
                    Make(PrimitiveType.Cube, body, new Vector3(0.10f, 0.75f, 0.02f), new Vector3(0.05f, 0.10f, 0.03f), new Color(1f, 0.85f, 0.1f), "TuftR");
                    Make(PrimitiveType.Cube, body, new Vector3(0, 0.66f, 0.15f), new Vector3(0.22f, 0.04f, 0.02f), Color.black, "Glasses");
                    Make(PrimitiveType.Capsule, body, new Vector3(0, 0.26f, -0.03f), new Vector3(0.46f, 0.24f, 0.36f), Color.white, "Coat");
                    break;
                case Look.Kalle:
                    root.transform.localScale = Vector3.one * size * 1.25f;
                    Make(PrimitiveType.Sphere, body, new Vector3(0, 0.72f, 0.02f), new Vector3(0.34f, 0.18f, 0.34f), new Color(1f, 0.82f, 0.05f), "Helmet");
                    Make(PrimitiveType.Cube, body, new Vector3(-0.09f, 0.32f, 0.17f), new Vector3(0.03f, 0.30f, 0.02f), new Color(0.75f, 0.2f, 0.15f), "BraceL");
                    Make(PrimitiveType.Cube, body, new Vector3(0.09f, 0.32f, 0.17f), new Vector3(0.03f, 0.30f, 0.02f), new Color(0.75f, 0.2f, 0.15f), "BraceR");
                    break;
                case Look.Grete:
                    Make(PrimitiveType.Cube, body, new Vector3(0, 0.45f, 0.0f), new Vector3(0.40f, 0.14f, 0.36f), new Color(0.55f, 0.30f, 0.60f), "Shawl");
                    break;
                case Look.Theo:
                    // Messpinguin: Thermometer-Stab unter dem Flügel, Kappe in Thermometer-Rot
                    Make(PrimitiveType.Cylinder, body, new Vector3(0.24f, 0.38f, 0.06f), new Vector3(0.035f, 0.20f, 0.035f), Color.white, "Thermometer");
                    Make(PrimitiveType.Sphere, body, new Vector3(0.24f, 0.17f, 0.06f), Vector3.one * 0.08f, new Color(0.85f, 0.15f, 0.15f), "Bulb");
                    Make(PrimitiveType.Cylinder, body, new Vector3(0.24f, 0.32f, 0.075f), new Vector3(0.02f, 0.14f, 0.02f), new Color(0.85f, 0.15f, 0.15f), "Mercury");
                    Make(PrimitiveType.Sphere, body, new Vector3(0, 0.74f, 0.0f), new Vector3(0.28f, 0.10f, 0.28f), new Color(0.75f, 0.2f, 0.2f), "Cap");
                    break;
                case Look.Klara:
                    // Kassenwartin: grüner Augenschirm und Kassenbuch
                    Make(PrimitiveType.Cube, body, new Vector3(0, 0.72f, 0.12f), new Vector3(0.30f, 0.03f, 0.16f), new Color(0.15f, 0.6f, 0.35f), "Visor");
                    Make(PrimitiveType.Cube, body, new Vector3(-0.22f, 0.32f, 0.08f), new Vector3(0.05f, 0.20f, 0.15f), new Color(0.55f, 0.35f, 0.2f), "Ledger");
                    break;
                case Look.Flocke:
                    // Küken: flauschig grau, großer Kopf
                    root.transform.localScale = Vector3.one * size * 0.55f;
                    SetColor(body.Find("Torso").gameObject, new Color(0.62f, 0.64f, 0.68f));
                    SetColor(body.Find("Head").gameObject, new Color(0.62f, 0.64f, 0.68f));
                    body.Find("Head").localScale = new Vector3(0.38f, 0.36f, 0.36f);
                    break;
            }
            return root;
        }

        public static void SetPenguinTint(GameObject penguin, Color belly, Color ring)
        {
            var b = penguin.transform.Find("Body/Belly");
            if (b != null) SetColor(b.gameObject, belly);
            var r = penguin.transform.Find("Ring");
            if (r != null) SetColor(r.gameObject, ring);
        }

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
                r.sharedMaterial = Prims.Mat(r.name == "Belly" ? new Color(0.75f, 0.97f, 1f) : cyan);
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
