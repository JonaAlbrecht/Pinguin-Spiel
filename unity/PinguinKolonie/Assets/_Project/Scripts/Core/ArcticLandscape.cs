using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using L = SFE.Game.ArtStyle.Landscape;

namespace SFE.Game
{
    /// <summary>
    /// Die arktische Landschaft um den Hub (ADR-31, Spieltest 21.10.2026, Jona: „Rand und Hintergrund müssen besser
    /// aussehen – arktische, bergige Landschaft“). Ersetzt die runde Scholle im Meer-Zylinder. Kein Meer: Die Kolonie liegt
    /// weit im Inland (docs/02, Entscheidung Jona) – das Gleis zur Küste läuft durch einen Pass in die Ferne.
    ///
    /// Aufbau, von innen nach außen:
    /// - <b>Eis</b> (eben, y = 0): trägt den begehbaren Hub und reicht bis über den Bildrand; sein Rand liegt unter dem Schnee.
    /// - <b>Gelände</b> (ein Höhenfeld auf einem Polarnetz): liegt innen knapp unter dem Eis, steigt nach außen aus ihm
    ///   heraus – mit unregelmäßigem Rand und Dünen quer zum Wind, dann Felsbuckel und drei gestaffelte Bergketten ringsum.
    ///   Wo es steil ist, Fels (gemaltes Farbbild, <see cref="TerrainMaterial"/>). Nah = mit Schatten, fern = ohne.
    /// - <b>Pass</b>: In Gleisrichtung weichen die Berge einem Tal; die Talsohle steigt zu einem Sattel, über den das Gleis
    ///   aus dem Blick verschwindet (<see cref="TrackHeight"/>). Unter den Schienen ein ebener Damm.
    /// - ein <b>Presseisrücken</b> auf dem Eis im Norden.
    ///
    /// Wenige große Meshes (ein Objekt je Teil), gleiche Eingabe = gleiche Landschaft.
    /// Die Meshes liegen im Cache von <see cref="Shapes"/> – ein zweiter Hub (Stilprobe) baut nichts neu.
    /// </summary>
    public static class ArcticLandscape
    {
        /// <summary>Baut die ganze Landschaft unter <paramref name="parent"/> (Mitte = Hub-Mitte).</summary>
        public static Transform Build(Transform parent)
        {
            var root = new GameObject("Landscape").transform;
            root.SetParent(parent, false);
            var ice = Prims.Mat(Palette.Ice, ArtStyle.Surface.Ice, 1);
            var terrain = LandGrid().Material;
            Part(root, "Ice", IceMesh(), false, ice);
            Part(root, "LandNear", LandMesh(true), true, terrain);
            Part(root, "LandFar", LandMesh(false), false, terrain);
            Part(root, "PressureRidge", RidgeMesh(), true, ice);
            return root;
        }

        /// <summary>Richtung eines Punkts in Grad: 0 = Norden (+Z), 90 = Osten (+X) – wie der Gierwinkel der Kamera.</summary>
        public static float Azimuth(float x, float z) => Mathf.Atan2(x, z) * Mathf.Rad2Deg;

        // ---------- Gleis und Pass ----------

        /// <summary>Richtung des Gleises (waagerecht, Länge 1).</summary>
        private static Vector3 TrackDir => Quaternion.Euler(0f, L.TrackYaw, 0f) * Vector3.forward;

        /// <summary>Punkt auf der Gleismitte, <paramref name="s"/> Meter entlang des Gleises ab <see cref="L.TrackOrigin"/>, auf Gleishöhe.</summary>
        public static Vector3 TrackPoint(float s) => L.TrackOrigin + TrackDir * s + Vector3.up * TrackHeight(s);

        /// <summary>
        /// So weit reicht das Gleis: bis kurz vor den Rand der Landschaft, hinter dem Sattel des Passes – von unten sieht
        /// man es über den Sattel in die Ferne verschwinden, sein Ende nie.
        /// </summary>
        public static float TrackLength
        {
            get
            {
                float s = L.TrackStart;
                while (new Vector2(L.TrackOrigin.x + TrackDir.x * s, L.TrackOrigin.z + TrackDir.z * s).magnitude < L.TrackEndRadius) s += 0.5f;
                return s;
            }
        }

        /// <summary>
        /// Höhe der Schienenauflage: auf dem Eis 0, dann ein niedriger Bahndamm über die Schneeebene, dann die Talsohle
        /// des Passes hinauf zum Sattel und dahinter wieder etwas hinab.
        /// </summary>
        public static float TrackHeight(float s) =>
            L.EmbankmentHeight * Smooth(L.TrackStart, L.TrackStart + L.EmbankmentRamp, s) + PassFloor(s);

        /// <summary>Talsohle des Passes über der Ebene: steigt zum Sattel, fällt dahinter ab.</summary>
        private static float PassFloor(float s) =>
            L.PassHeight * Smooth(L.PassFrom, L.SaddleAt, s) - L.PassDrop * Smooth(L.SaddleAt, L.SaddleAt + 15f, s);

        /// <summary>Lage eines Punkts zum Gleis: entlang (s, Meter ab TrackOrigin) und quer (d, Abstand zur Gleismitte).</summary>
        private static void TrackCoords(float x, float z, out float s, out float d)
        {
            var dir = TrackDir;
            float dx = x - L.TrackOrigin.x, dz = z - L.TrackOrigin.z;
            s = dx * dir.x + dz * dir.z;
            d = Mathf.Abs(dx * dir.z - dz * dir.x);
        }

        /// <summary>Höhe des Geländes (ohne das Eis) an diesem Punkt.</summary>
        public static float Height(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z), az = Azimuth(x, z);
            // Rand der Schneedecke: buchtet unregelmäßig ein und aus, nie näher als FlatRadius
            float edge = Mathf.Max(L.FlatRadius + L.SnowBank, L.SnowEdge + (Fbm(x, z, L.SnowEdgeScale, 3, 5) - 0.5f) * 2f * L.SnowEdgeJag);
            float drift = Drift(x, z) * Smooth(L.FlatRadius - 1f, L.FlatRadius + 3f, r) * Smooth(edge - 3f, edge + 3f, r);
            float plain = Mathf.Lerp(-L.UnderIce, L.SnowLevel, Smooth(-0.5f * L.SnowBank, L.SnowBank, r - edge));
            float knolls = L.KnollHeight * Smooth(0.64f, 0.85f, Fbm(x, z, L.KnollSpacing, 2, 61)) * Smooth(L.KnollRadius, L.KnollRadius + 6f, r);

            // Das Gleis läuft aus dem Hub hinaus durch ein Tal: Dort weichen Berge, Buckel und Dünen, die Talsohle steigt
            // zum Sattel, und unter den Schienen liegt ein ebener Damm. Hinter dem Hub (s < TrackStart) wirkt nichts davon.
            TrackCoords(x, z, out float s, out float d);
            float ahead = Smooth(L.TrackStart - 1f, L.TrackStart, s);
            // Hinter dem Sattel schließt sich das Tal wieder: Dort steht eine ferne Kette quer – durch den Pass trifft der
            // Himmel auf Berge, nicht auf den Rand der Landschaft. Das Gleis endet vorher in der Senke dahinter.
            float carve = ahead * (1f - Smooth(L.SaddleAt + L.PassClose * 0.4f, L.SaddleAt + L.PassClose, s));
            float floorWidth = L.PassFloorWidth + L.PassWiden * 0.3f * s, wallWidth = L.PassWallWidth + L.PassWiden * s;
            float open = 1f - carve * (1f - Smooth(floorWidth, wallWidth, d)); // 0 im Tal, 1 draußen
            float lift = ahead * PassFloor(s) * (1f - Smooth(floorWidth, wallWidth * 1.2f, d));
            float land = plain + lift + L.DriftHeight * drift + (knolls + Mountains(x, z, r, az)) * open;
            float bed = ahead * (1f - Smooth(L.BedHalfWidth, L.BedHalfWidth + L.BedBlend, d));
            return Mathf.Lerp(land, TrackHeight(s) - L.BedDepth, bed);
        }

        // ---------- Formen ----------

        /// <summary>Verwehungen (0…1): Dünenkämme quer zum Wind, darauf Windgangeln längs des Windes, runde Kuppen.</summary>
        private static float Drift(float x, float z)
        {
            float w = L.WindAzimuth * Mathf.Deg2Rad;
            float u = x * Mathf.Sin(w) + z * Mathf.Cos(w), v = x * Mathf.Cos(w) - z * Mathf.Sin(w);
            // Dünen: Kämme in festem Abstand, vom Rauschen verbogen; schmale Kämme, breite Mulden (Quadrat der Welle)
            float bend = 2.5f * Noise(v / L.DuneLength, u / L.DuneLength, 3);
            float wave = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * u / L.DuneSpacing + 2f * Mathf.PI * bend);
            float patches = Smooth(0.3f, 0.65f, Noise(x / (2f * L.DuneLength), z / (2f * L.DuneLength), 7));
            float gangeln = Noise(u / L.DriftLength, v / L.DriftWidth, 4);
            return Mathf.Clamp01(0.75f * wave * wave * patches + 0.35f * Smooth(0.45f, 0.8f, gangeln));
        }

        /// <summary>
        /// Drei gestaffelte Ketten als Stufen der Grundhöhe, darauf Grate aus „Kamm-Rauschen“ (scharfe Grate, runde Täler).
        /// Der Abstand jeder Kette schwankt je Richtung, damit sie keinen Ring um den Hub bilden.
        /// </summary>
        private static float Mountains(float x, float z, float r, float az)
        {
            float a = az * Mathf.Deg2Rad;
            float rj = r + L.RangeJitter * (Fbm(Mathf.Sin(a) * 40f, Mathf.Cos(a) * 40f, 22f, 2, 31) - 0.5f) * 2f;
            float env = L.FoothillHeight * Smooth(L.FoothillRadius - 10f, L.FoothillRadius + 6f, rj)
                + (L.RangeHeight - L.FoothillHeight) * Smooth(L.RangeRadius - 14f, L.RangeRadius + 4f, rj)
                + (L.PeakHeight - L.RangeHeight) * Smooth(L.PeakRadius - 16f, L.PeakRadius, rj);
            if (env <= 0f) return 0f;
            // Vorberge rund und weich (Moränen, verschneite Kuppen), erst die hohen Ketten mit scharfen Graten –
            // überall Grate ließen die Ebene vor den Bergen wie mit Haifischflossen gespickt aussehen.
            float shape = Mathf.Lerp(Fbm(x, z, L.PeakSpacing * 0.5f, 3, 43), Ridged(x, z, L.PeakSpacing, 4, 41), Smooth(L.FoothillHeight, L.RangeHeight, env));
            return env * BackFall(r) * (L.ValleyFloor + (1f - L.ValleyFloor) * shape);
        }

        /// <summary>Hinter dem Hauptkamm fällt das Gelände ab – der Kamm bleibt Silhouette, der Rand des Netzes unsichtbar.</summary>
        private static float BackFall(float r) => 1f - L.BackDrop * Smooth(L.PeakRadius + 6f, L.OuterRadius, r);

        // ---------- Rauschen ----------

        private static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        private static float Noise(float x, float z, int salt) =>
            Mathf.PerlinNoise(x + 37.1f * salt + 3.3f * L.Seed, z - 19.7f * salt + 7.9f * L.Seed);

        private static float Fbm(float x, float z, float scale, int octaves, int salt)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += amp * Noise(x / scale, z / scale, salt + o);
                norm += amp;
                amp *= 0.5f;
                scale *= 0.5f;
            }
            return sum / norm;
        }

        /// <summary>Kamm-Rauschen (0…1): scharfe Grate, feinere Lagen vor allem dort, wo schon ein Grat ist.</summary>
        private static float Ridged(float x, float z, float scale, int octaves, int salt)
        {
            float sum = 0f, amp = 1f, norm = 0f, weight = 1f;
            for (int o = 0; o < octaves; o++)
            {
                float n = 1f - Mathf.Abs(2f * Noise(x / scale, z / scale, salt + o) - 1f);
                n *= n * weight;
                weight = Mathf.Clamp01(n * 2f);
                sum += amp * n;
                norm += amp;
                amp *= 0.5f;
                scale *= 0.5f;
            }
            return Mathf.Clamp01(sum / norm * 1.3f);
        }

        // ---------- Gelände ----------

        /// <summary>
        /// Polarnetz des Geländes: Ringe wachsen mit dem Radius, damit die Zellen etwa quadratisch bleiben. Je Ring eine
        /// Spalte mehr als Segmente: Die letzte liegt auf der ersten, damit das Farbbild an der Naht nicht zurückspringt.
        /// </summary>
        private sealed class Grid
        {
            public int Rings, Cols;
            public Vector3[] Pos, Normal;
            public int Split; // erster Ring der fernen Hälfte
            public Material Material;
        }

        private static Grid _grid;

        private static Grid LandGrid()
        {
            if (_grid != null && _grid.Material != null) return _grid;
            int seg = L.Segments, cols = seg + 1;
            var radii = new List<float>();
            float step = 2f * Mathf.PI / seg;
            for (float r = L.InnerRadius; r < L.OuterRadius; r *= 1f + step) radii.Add(r);
            radii.Add(L.OuterRadius);
            int rings = radii.Count;
            var pos = new Vector3[rings * cols];
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < cols; j++)
                {
                    float a = (j % seg) * step;
                    float x = Mathf.Sin(a) * radii[i], z = Mathf.Cos(a) * radii[i];
                    pos[i * cols + j] = new Vector3(x, Height(x, z), z);
                }
            // Normalen aus dem ganzen Netz, damit nahe und ferne Hälfte an ihrer Naht gleich schattiert sind.
            var nrm = new Vector3[pos.Length];
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < cols; j++)
                {
                    int jn = (j % seg + 1) % seg, jp = (j % seg + seg - 1) % seg;
                    var dr = pos[Mathf.Min(i + 1, rings - 1) * cols + j] - pos[Mathf.Max(i - 1, 0) * cols + j];
                    var dt = pos[i * cols + jn] - pos[i * cols + jp];
                    nrm[i * cols + j] = Vector3.Cross(dr, dt).normalized;
                }
            _grid = new Grid
            {
                Rings = rings, Cols = cols, Pos = pos, Normal = nrm,
                Split = radii.FindIndex(r => r >= L.NearRadius),
                Material = TerrainMaterial(pos, nrm, rings, cols),
            };
            return _grid;
        }

        /// <summary>
        /// Farbbild des Geländes: Schnee oder Fels nach Hangneigung, zwei Bildpunkte je Netzzelle (dazwischen gemittelt).
        /// Die Grafikkarte mischt zwischen den Punkten – Fels geht weich in Schnee über, wie mit Vertex-Farben, die URP Lit
        /// nicht kennt. Fels erst ab den Felsbuckeln: Die steilen Flanken der Verwehungen bleiben Schnee.
        /// </summary>
        private static Material TerrainMaterial(Vector3[] pos, Vector3[] nrm, int rings, int cols)
        {
            int w = TexelsFor(cols), h = TexelsFor(rings);
            var px = new Color[w * h];
            for (int ti = 0; ti < h; ti++)
                for (int tj = 0; tj < w; tj++)
                {
                    int i0 = ti / 2, j0 = tj / 2, i1 = Mathf.Min(i0 + 1, rings - 1), j1 = Mathf.Min(j0 + 1, cols - 1);
                    float fi = ti % 2 * 0.5f, fj = tj % 2 * 0.5f;
                    var p = Vector3.Lerp(Vector3.Lerp(pos[i0 * cols + j0], pos[i0 * cols + j1], fj), Vector3.Lerp(pos[i1 * cols + j0], pos[i1 * cols + j1], fj), fi);
                    var n = Vector3.Lerp(Vector3.Lerp(nrm[i0 * cols + j0], nrm[i0 * cols + j1], fj), Vector3.Lerp(nrm[i1 * cols + j0], nrm[i1 * cols + j1], fj), fi).normalized;
                    float slope = Mathf.Acos(Mathf.Clamp01(n.y)) * Mathf.Rad2Deg;
                    float jitter = (Fbm(p.x, p.z, 9f, 2, 51) - 0.5f) * 4f * L.RockBlend;
                    float mountains = Smooth(L.KnollRadius, L.KnollRadius + 4f, new Vector2(p.x, p.z).magnitude);
                    float rock = Smooth(L.RockSlope - L.RockBlend, L.RockSlope + L.RockBlend, slope + jitter) * mountains;
                    px[ti * w + tj] = Color.Lerp(Palette.Snow, Palette.Rock, rock);
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                name = "Terrain", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4,
            };
            tex.SetPixels(px);
            tex.Apply(true, true);
            var m = Prims.Textured(tex, ArtStyle.Surface.Terrain);
            // Die Grund-Normal-Map läge einmal über das ganze Netz gespannt und zöge Schlieren – flach stellen. Sie bleibt
            // gesetzt, damit der Shader-Schalter derselbe ist wie beim Wasser (Variante im Build vorhanden, ADR-28).
            m.SetFloat("_BumpScale", 0f);
            // Schneekörnung je Netzzelle (die Zellen wachsen nach außen mit, die Körnung auch)
            m.SetTextureScale("_DetailAlbedoMap", new Vector2(w, h) * (0.5f * L.GrainPerCell));
            return m;
        }

        /// <summary>Bildpunkte im Farbbild für so viele Netzpunkte: je Punkt einer, dazwischen einer.</summary>
        private static int TexelsFor(int points) => 2 * points - 1;

        /// <summary>Nahe (mit Schatten) oder ferne Hälfte des Geländes.</summary>
        private static Mesh LandMesh(bool near) => Shapes.Cached("landscape_" + (near ? "near" : "far") + L.Seed, () =>
        {
            var g = LandGrid();
            int cols = g.Cols;
            int i0 = near ? 0 : g.Split, i1 = near ? g.Split : g.Rings - 1;
            int rings = i1 - i0 + 1;
            var verts = new Vector3[rings * cols];
            var norms = new Vector3[verts.Length];
            var uvs = new Vector2[verts.Length];
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < cols; j++)
                {
                    int k = i * cols + j, src = (i0 + i) * cols + j;
                    verts[k] = g.Pos[src];
                    norms[k] = g.Normal[src];
                    // Mitte des eigenen Bildpunkts im Farbbild
                    uvs[k] = new Vector2((2 * j + 0.5f) / TexelsFor(cols), (2 * (i0 + i) + 0.5f) / TexelsFor(g.Rings));
                }
            var tris = new List<int>((rings - 1) * (cols - 1) * 6);
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < cols - 1; j++)
                {
                    int a = i * cols + j, b = a + 1, c = a + cols, d = c + 1;
                    // Im Uhrzeigersinn von oben gesehen (Unity: Vorderseite)
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            var m = new Mesh();
            if (verts.Length > 65000) m.indexFormat = IndexFormat.UInt32;
            m.vertices = verts;
            m.normals = norms;
            m.uv = uvs;
            m.SetTriangles(tris, 0);
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        });


        // ---------- Eis ----------

        /// <summary>
        /// Ebenes Eis (y = 0) unter dem Hub bis <see cref="L.IceRadius"/>. Sein Rand liegt überall unter dem Schnee – man
        /// sieht nur, wo der Schnee aus dem Eis steigt, und das ist unregelmäßig.
        /// </summary>
        private static Mesh IceMesh() => Shapes.Cached("landscape_ice" + L.Seed, () =>
        {
            int seg = L.Segments;
            float[] frac = { 0.3f, 0.55f, 0.75f, 0.9f, 1f };
            var v = new List<Vector3> { Vector3.zero };
            foreach (float f in frac)
                for (int j = 0; j < seg; j++)
                {
                    float a = j * 2f * Mathf.PI / seg;
                    v.Add(new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (L.IceRadius * f));
                }
            var t = new List<int>();
            for (int j = 0; j < seg; j++) { t.Add(0); t.Add(1 + j); t.Add(1 + (j + 1) % seg); }
            for (int k = 0; k < frac.Length - 1; k++)
                for (int j = 0; j < seg; j++)
                {
                    int a = 1 + k * seg + j, b = 1 + k * seg + (j + 1) % seg, c = a + seg, d = b + seg;
                    t.Add(a); t.Add(c); t.Add(b);
                    t.Add(b); t.Add(c); t.Add(d);
                }
            var uv = new Vector2[v.Count];
            float tiles = ArtStyle.Surfaces.Ice.TilesPerMeter;
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(v[i].x, v[i].z) * tiles;
            var m = new Mesh();
            m.SetVertices(v);
            m.uv = uv;
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        });


        // ---------- Presseis ----------

        /// <summary>
        /// Presseisrücken: verkantete Eisblöcke in einem Bogen auf dem Eis vor dem Schneerand – dort, wo sich das Eis unter
        /// Druck aufgeworfen hat. In der Mitte des Rückens höher, an den Enden flach auslaufend; teils vom Schnee verschluckt.
        /// </summary>
        private static Mesh RidgeMesh() => Shapes.Cached("landscape_ridge" + L.Seed, () =>
        {
            var rnd = new System.Random(L.Seed + 1);
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            float a0 = L.RidgeFrom, a1 = L.RidgeTo;
            int count = Mathf.RoundToInt(L.RidgeRadius * (a1 - a0) * Mathf.Deg2Rad * L.RidgeDensity);
            for (int i = 0; i < count; i++)
            {
                float u = (i + (float)rnd.NextDouble()) / count;
                float az = Mathf.Lerp(a0, a1, u);
                float r = L.RidgeRadius + 0.8f * Mathf.Sin(az * 0.21f) + ((float)rnd.NextDouble() - 0.5f) * 1.4f;
                float a = az * Mathf.Deg2Rad;
                float swell = 0.35f + 0.65f * Mathf.Sin(Mathf.PI * u);
                float s = Mathf.Lerp(L.RidgeBlockMin, L.RidgeBlockMax, (float)rnd.NextDouble() * swell);
                var size = new Vector3(s, s * (0.35f + 0.25f * (float)rnd.NextDouble()), s * (0.6f + 0.4f * (float)rnd.NextDouble()));
                var rot = Quaternion.Euler(((float)rnd.NextDouble() - 0.5f) * 90f, (float)rnd.NextDouble() * 360f, ((float)rnd.NextDouble() - 0.5f) * 60f);
                var centre = new Vector3(Mathf.Sin(a) * r, size.y * 0.25f, Mathf.Cos(a) * r);
                Box(v, n, t, centre, rot, size);
            }
            float tiles = ArtStyle.Surfaces.Ice.TilesPerMeter * 3f; // feinere Struktur auf den kleinen Blöcken
            var uv = new List<Vector2>();
            for (int i = 0; i < v.Count; i++)
            {
                // Projektion entlang der Achse, auf die die Fläche am meisten zeigt (wie Shapes.BoxUv)
                var a = new Vector3(Mathf.Abs(n[i].x), Mathf.Abs(n[i].y), Mathf.Abs(n[i].z));
                var p = v[i];
                uv.Add((a.y >= a.x && a.y >= a.z ? new Vector2(p.x, p.z) : a.x >= a.z ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y)) * tiles);
            }
            var m = new Mesh();
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        });

        private static readonly Vector3[] BoxFaces = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        /// <summary>Quader mit eigenen Eckpunkten je Seite (scharfe Kanten), gedreht und verschoben.</summary>
        private static void Box(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 centre, Quaternion rot, Vector3 size)
        {
            foreach (var f in BoxFaces)
            {
                // zwei Achsen in der Fläche, so gewählt, dass die Ecken von außen gesehen im Uhrzeigersinn laufen
                var u = f.y != 0f ? Vector3.right : Vector3.up;
                var w = Vector3.Cross(f, u);
                int b = v.Count;
                for (int k = 0; k < 4; k++)
                {
                    float su = k == 1 || k == 2 ? 1f : -1f, sw = k >= 2 ? 1f : -1f;
                    var local = Vector3.Scale(0.5f * (f + su * u + sw * w), size);
                    v.Add(centre + rot * local);
                    n.Add(rot * f);
                }
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
                t.Add(b); t.Add(b + 2); t.Add(b + 3);
            }
        }

        // ---------- Aufbau ----------

        private static void Part(Transform root, string name, Mesh mesh, bool castShadows, params Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = materials;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = true;
        }
    }
}
