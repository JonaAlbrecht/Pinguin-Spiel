using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Baut einen Pinguin aus den Stilregeln (<see cref="ArtStyle"/>): ein durchgehender Körper ohne Hals mit hellem
    /// Bauch bis zur Kehle, Flossen, spitzer Schnabel, Augen mit Pupille, Füße mit drei Zehen. Zubehör der Figuren
    /// sitzt an Punkten auf dem Körperumriss statt an festen Koordinaten – ändern sich die Proportionen, rutscht es mit.
    /// Aufbau: Wurzel → „Body“ (wird zum Watscheln geneigt) → Teile. „Ring“ hängt an der Wurzel.
    /// </summary>
    public sealed class PenguinFigure
    {
        /// <summary>Teilmeshes des Körpers „Torso“: 0 Rücken, 1 Bauch (einfärbbar), danach ggf. Kopf und Gesicht.</summary>
        public const int BackSlot = 0, BellySlot = 1;

        private readonly PenguinShape _shape;
        private readonly Outline _outline;
        private readonly float _h;
        private readonly Transform _body;

        private PenguinFigure(PenguinShape shape, Transform body)
        {
            _shape = shape;
            _outline = OutlineOf(shape);
            _h = ArtStyle.Proportion.PenguinHeight;
            _body = body;
        }

        public static GameObject Build(Transform parent, string name, Prims.Look look, float size)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * size;
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);

            bool chick = look == Prims.Look.Flocke;
            var shape = chick ? ArtStyle.Proportion.Chick : ArtStyle.Proportion.Adult;
            var f = new PenguinFigure(shape, body);

            Color back = Palette.PenguinBlack, belly = Palette.PenguinWhite, head = back, face = belly;
            if (look == Prims.Look.Grete) back = head = Palette.PenguinElder;
            if (chick) { back = Palette.ChickDown; belly = Palette.ChickBelly; head = Palette.PenguinBlack; face = Palette.PenguinWhite; }

            f.Torso(back, belly, head, face);
            Color flipper = back;
            float sleeve = 1f;
            f.Dress(look, ref flipper, ref sleeve);
            f.Face(chick ? Palette.PenguinBlack : Palette.PenguinWhite);
            f.Flippers(flipper, sleeve);
            f.Feet();

            Prims.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.005f, 0), new Vector3(0.62f, 0.005f, 0.62f),
                Palette.Ring, "Ring");
            if (look == Prims.Look.Kalle) root.transform.localScale = Vector3.one * size * 1.25f;
            if (chick) root.transform.localScale = Vector3.one * size * 0.55f;
            return root;
        }

        /// <summary>Färbt den Bauch und den Ring darunter (Prolog: Temperatur je Pinguin).</summary>
        public static void Tint(GameObject penguin, Color belly, Color ring)
        {
            var t = penguin.transform.Find("Body/Torso");
            if (t != null)
            {
                var r = t.GetComponent<Renderer>();
                var mats = r.sharedMaterials;
                if (mats.Length > BellySlot) { mats[BellySlot] = Prims.Mat(belly); r.sharedMaterials = mats; }
            }
            var ringT = penguin.transform.Find("Ring");
            if (ringT != null) Prims.SetColor(ringT.gameObject, ring);
        }

        // ---------- Körper ----------

        private static Outline OutlineOf(PenguinShape s)
        {
            // Ein Umriss je Bauplan genügt; er ändert sich nur, wenn jemand die Stilregeln ändert.
            if (s.CachedOutline == null) s.CachedOutline = new Outline(s.Profile);
            return s.CachedOutline;
        }

        private void Torso(Color back, Color belly, Color head, Color face)
        {
            // Kopf und Gesicht bekommen nur dann eigene Teilmeshes, wenn sie anders gefärbt sind – spart Zeichenaufrufe.
            int headSlot = head == back ? BackSlot : 2;
            int faceSlot = face == belly ? BellySlot : (headSlot == 2 ? 3 : 2);
            bool hasFace = _shape.FaceWidth > 0f;
            int slots = Mathf.Max(2, Mathf.Max(headSlot, hasFace ? faceSlot : 0) + 1);
            var s = _shape;
            int segs = ArtStyle.Detail.FigureSegments, rings = ArtStyle.Detail.FigureRings;
            string key = $"penguin{s.GetHashCode()}_{segs}_{rings}_{headSlot}{faceSlot}{slots}";
            var mesh = Shapes.Cached(key, () => Shapes.Revolve(_outline.Rings(rings), segs, s.Depth,
                region: (y, a) => Region(s, y, a, headSlot, hasFace ? faceSlot : headSlot), submeshes: slots));

            var go = Part("Torso", mesh, Vector3.zero, Vector3.one * _h, Quaternion.identity, back);
            var mats = new Material[slots];
            for (int i = 0; i < slots; i++) mats[i] = Prims.Mat(back);
            mats[BellySlot] = Prims.Mat(belly);
            if (headSlot != BackSlot) mats[headSlot] = Prims.Mat(head);
            if (hasFace && faceSlot >= 2) mats[faceSlot] = Prims.Mat(face);
            go.GetComponent<Renderer>().sharedMaterials = mats;
        }

        private static int Region(PenguinShape s, float y, float a, int headSlot, int faceSlot)
        {
            float abs = Mathf.Abs(a);
            if (y >= s.HeadFrom)
                return s.FaceWidth > 0f && y >= s.FaceFrom && y <= s.FaceTo && abs < s.FaceWidth ? faceSlot : headSlot;
            if (y > s.BellyTop) return BackSlot;
            float w = Mathf.Lerp(s.BellyWidthLow, s.BellyWidthHigh, y / s.BellyTop);
            // Oben rund statt eckig auslaufen: die letzten 10 % der Höhe schließt der Bauch wie ein Bogen.
            float top = s.BellyTop * 0.9f;
            if (y > top) w *= Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((y - top) / (s.BellyTop - top), 2f)));
            return abs < w ? BellySlot : BackSlot;
        }

        // ---------- Gesicht ----------

        private void Face(Color pupil)
        {
            var s = _shape;
            for (int side = -1; side <= 1; side += 2)
            {
                string n = side < 0 ? "L" : "R";
                var at = Surface(s.EyeHeight, side * s.EyeAngle, out var normal);
                var turn = Quaternion.LookRotation(normal, Vector3.up);
                float d = s.EyeSize * _h;
                if (s.Sclera)
                {
                    var eye = Prims.Make(PrimitiveType.Sphere, _body, at, new Vector3(d, d, d * 0.45f), Palette.PenguinWhite, "Eye" + n,
                        segments: ArtStyle.Detail.SmallSegments);
                    eye.transform.localRotation = turn;
                    var p = Prims.Make(PrimitiveType.Sphere, eye.transform, new Vector3(0, -0.02f, 0.42f), new Vector3(0.55f, 0.6f, 0.5f),
                        Palette.PenguinBlack, "Pupil", segments: ArtStyle.Detail.SmallSegments);
                    p.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    var eye = Prims.Make(PrimitiveType.Sphere, _body, at, new Vector3(d * 0.7f, d * 0.8f, d * 0.4f), pupil, "Eye" + n,
                        segments: ArtStyle.Detail.SmallSegments);
                    eye.transform.localRotation = turn;
                }

                if (s.EarPatches)
                {
                    // Am Halsansatz hinter dem Auge, schräg nach vorn unten auslaufend – nicht auf Augenhöhe, sonst wirkt es wie ein Kopfhörer.
                    var ear = Surface(s.EyeHeight - 0.10f, side * 92f, out var en);
                    var patch = Prims.Make(PrimitiveType.Sphere, _body, ear, new Vector3(0.05f, 0.11f, 0.02f) * _h,
                        Palette.PenguinAccent, "Ear" + n, segments: ArtStyle.Detail.SmallSegments);
                    patch.transform.localRotation = Quaternion.LookRotation(en, Vector3.up) * Quaternion.Euler(0, 0, side * 30f);
                }
            }

            var front = Surface(s.BeakHeight, 0f, out _);
            Part("Beak", Shapes.Cone(ArtStyle.Detail.SmallSegments), front - new Vector3(0, 0, 0.01f),
                new Vector3(s.BeakWidth, s.BeakLength, s.BeakWidth * 0.8f) * _h,
                Quaternion.Euler(90f + s.BeakTilt, 0, 0), Palette.Beak);
        }

        // ---------- Flossen und Füße ----------

        private void Flippers(Color color, float sleeve)
        {
            var s = _shape;
            var at = _outline.At(s.FlipperTop);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (at.y * sleeve + s.FlipperThickness * 0.3f) * _h;
                Part(side < 0 ? "FlipperL" : "FlipperR", Shapes.Flipper(ArtStyle.Detail.Segments),
                    new Vector3(x, s.FlipperTop * _h, at.z * _h),
                    new Vector3(s.FlipperThickness, s.FlipperLength, s.FlipperWidth) * _h,
                    Quaternion.Euler(0, 0, side * s.FlipperSpread), color);
            }
        }

        private void Feet()
        {
            var s = _shape;
            float len = s.FootLength * _h;
            var bottom = _outline.At(0.08f);
            for (int side = -1; side <= 1; side += 2)
                Part(side < 0 ? "FootL" : "FootR", Shapes.Foot(),
                    new Vector3(side * 0.075f * _h, 0f, (bottom.z + 0.10f) * _h),
                    new Vector3(len * 0.75f, 0.03f * _h, len),
                    Quaternion.Euler(0, side * s.FootSpread, 0), Palette.Beak);
        }

        // ---------- Zubehör der Figuren ----------

        private void Dress(Prims.Look look, ref Color flipper, ref float sleeve)
        {
            var s = _shape;
            switch (look)
            {
                case Prims.Look.Pip:
                {
                    var red = Palette.PipScarf;
                    Shell("Scarf", 0.70f, 0.79f, -180f, 180f, 1.10f, red);
                    var tail = Surface(0.66f, 140f, out var n);
                    Prims.Make(PrimitiveType.Cube, _body, tail + n * 0.02f, new Vector3(0.06f, 0.16f, 0.02f), red, "ScarfTail")
                        .transform.localRotation = Quaternion.LookRotation(n, Vector3.up) * Quaternion.Euler(0, 0, 12f);
                    Held("Tablet", PrimitiveType.Cube, 0.40f, 62f, new Vector3(0.04f, 0.14f, 0.10f), Palette.PipTablet);
                    break;
                }
                case Prims.Look.Frieda:
                {
                    // Felsenpinguin-Schopf, Brille und offener Laborkittel mit weißen Ärmeln
                    var yellow = Palette.FriedaTuft;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var at = Surface(0.93f, side * 36f, out _);
                        var dir = new Vector3(side * 0.6f, 0.55f, -0.6f).normalized;
                        float len = 0.12f * _h;
                        var tuft = Prims.Make(PrimitiveType.Capsule, _body, at + dir * len * 0.5f, new Vector3(0.025f * _h, len * 0.5f, 0.025f * _h),
                            yellow, side < 0 ? "TuftL" : "TuftR", segments: ArtStyle.Detail.SmallSegments);
                        tuft.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);

                        var eye = Surface(s.EyeHeight, side * s.EyeAngle, out var n);
                        float d = s.EyeSize * _h * 1.7f;
                        Part(side < 0 ? "GlassL" : "GlassR", Shapes.Ring(ArtStyle.Detail.Segments, 0.72f), eye + n * 0.012f,
                            new Vector3(d, d * 0.35f, d), Quaternion.FromToRotation(Vector3.up, n), Palette.Glasses);
                    }
                    var bridge = Surface(s.EyeHeight + 0.01f, 0f, out var bn);
                    Prims.Make(PrimitiveType.Cube, _body, bridge + bn * 0.02f, new Vector3(0.06f, 0.008f, 0.008f), Palette.Glasses, "Bridge");
                    Shell("Coat", 0.10f, 0.70f, 26f, 334f, 1.08f, Palette.FriedaCoat);
                    flipper = Palette.FriedaCoat;
                    sleeve = 1.08f;
                    break;
                }
                case Prims.Look.Kalle:
                {
                    // Bauhelm mit Krempe, Hosenträger über dem Bauch
                    var yellow = Palette.KalleHelmet;
                    Shell("Helmet", 0.89f, 1.0f, -180f, 180f, 1.14f, yellow);
                    var rim = _outline.At(0.89f);
                    Prims.Make(PrimitiveType.Cylinder, _body, new Vector3(0, 0.89f * _h, rim.z * _h),
                        new Vector3(rim.y * 2.6f * _h, 0.008f, rim.y * 2.6f * _h * s.Depth), yellow, "Brim");
                    var red = Palette.KalleBraces;
                    Shell("BraceL", 0.12f, 0.78f, -22f, -12f, 1.03f, red);
                    Shell("BraceR", 0.12f, 0.78f, 12f, 22f, 1.03f, red);
                    break;
                }
                case Prims.Look.Grete:
                {
                    var purple = Palette.GreteShawl;
                    Shell("Shawl", 0.56f, 0.75f, 38f, 322f, 1.09f, purple);
                    var knot = Surface(0.72f, 0f, out var n);
                    Prims.Make(PrimitiveType.Sphere, _body, knot + n * 0.03f, Vector3.one * 0.06f, purple, "Knot",
                        segments: ArtStyle.Detail.SmallSegments);
                    break;
                }
                case Prims.Look.Theo:
                {
                    // Messpinguin: Thermometer in der Flosse, Kappe in Thermometer-Rot
                    var red = Palette.ThermometerRed;
                    var holder = Held("Thermometer", PrimitiveType.Cylinder, 0.40f, 64f, new Vector3(0.035f, 0.20f, 0.035f), Palette.Thermometer);
                    Prims.Make(PrimitiveType.Sphere, holder.transform, new Vector3(0, -1.05f, 0), new Vector3(2.2f, 0.4f, 2.2f), red, "Bulb",
                        segments: ArtStyle.Detail.SmallSegments);
                    Prims.Make(PrimitiveType.Cylinder, holder.transform, new Vector3(0.35f, -0.3f, 0), new Vector3(0.55f, 0.7f, 0.55f), red, "Mercury",
                        segments: ArtStyle.Detail.SmallSegments);
                    Shell("Cap", 0.90f, 1.0f, -180f, 180f, 1.08f, Palette.TheoCap);
                    var peak = Surface(0.92f, 0f, out var n);
                    Prims.Make(PrimitiveType.Cube, _body, peak + n * 0.04f, new Vector3(0.14f, 0.012f, 0.08f), Palette.TheoCapPeak, "Peak");
                    break;
                }
                case Prims.Look.Klara:
                {
                    // Kassenwartin: grüner Augenschirm und Kassenbuch
                    var green = Palette.KlaraVisor;
                    Shell("Band", 0.90f, 0.95f, -180f, 180f, 1.07f, green);
                    var peak = Surface(0.93f, 0f, out var n);
                    Prims.Make(PrimitiveType.Cube, _body, peak + n * 0.05f, new Vector3(0.20f, 0.012f, 0.11f), green, "Visor")
                        .transform.localRotation = Quaternion.Euler(14f, 0, 0);
                    Held("Ledger", PrimitiveType.Cube, 0.40f, -62f, new Vector3(0.05f, 0.20f, 0.15f), Palette.Ledger);
                    break;
                }
            }
        }

        /// <summary>Gegenstand in Flossenhöhe vor dem Körper (Tablet, Buch, Thermometer); Maße in Metern wie bisher.</summary>
        private GameObject Held(string name, PrimitiveType type, float y, float angle, Vector3 scale, Color color)
        {
            var at = Surface(y, angle, out var n);
            var go = Prims.Make(type, _body, at + n * (scale.x + 0.03f), scale, color, name);
            go.transform.localRotation = Quaternion.LookRotation(new Vector3(n.x, 0, n.z), Vector3.up) * Quaternion.Euler(0, 90f, 0);
            return go;
        }

        /// <summary>Kleidung, die eng am Körper sitzt: ein Stück Körperoberfläche zwischen zwei Höhen und Winkeln, etwas vergrößert.</summary>
        private void Shell(string name, float y0, float y1, float a0, float a1, float grow, Color color)
        {
            var s = _shape;
            int segs = ArtStyle.Detail.FigureSegments;
            int rings = Mathf.Max(2, Mathf.RoundToInt(ArtStyle.Detail.FigureRings * (y1 - y0)) + 1);
            string key = $"shell{s.GetHashCode()}_{y0}_{y1}_{a0}_{a1}_{grow}_{segs}_{rings}";
            var mesh = Shapes.Cached(key, () => Shapes.Revolve(_outline.Rings(rings, y0, y1), segs, s.Depth, a0, a1, grow: grow));
            Part(name, mesh, Vector3.zero, Vector3.one * _h, Quaternion.identity, color);
        }

        /// <summary>Punkt auf der Körperoberfläche (Höhe als Anteil, Winkel in Grad, 0 = vorn) und die Richtung nach außen.</summary>
        private Vector3 Surface(float y, float angleDeg, out Vector3 normal)
        {
            var p = _outline.At(y);
            float a = angleDeg * Mathf.Deg2Rad;
            float sx = Mathf.Sin(a), cz = Mathf.Cos(a);
            normal = new Vector3(sx, -_outline.Slope(y), cz / _shape.Depth).normalized;
            return new Vector3(sx * p.y, y, p.z + cz * p.y * _shape.Depth) * _h;
        }

        private GameObject Part(string name, Mesh mesh, Vector3 pos, Vector3 scale, Quaternion rot, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_body, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Prims.Mat(color);
            return go;
        }
    }
}
