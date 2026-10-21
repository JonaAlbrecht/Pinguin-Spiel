using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using L = SFE.Game.ArtStyle.Landscape;

namespace SFE.Game
{
    /// <summary>
    /// Die arktische Landschaft um den Hub (ADR-31, Spieltest 21.10.2026, Jona: „Rand und Hintergrund müssen besser
    /// aussehen – arktische, bergige Landschaft, auf der einen Seite das Meer“). Ersetzt die runde Scholle im Meer-Zylinder.
    ///
    /// Aufbau, von innen nach außen:
    /// - <b>Festeis</b> (eben, y = 0): trägt den begehbaren Hub und reicht bis über den Bildrand. Zur Bucht hin endet es an
    ///   einer ausgefransten Eiskante (<see cref="CoastAt"/>), sonst verschwindet es unter dem Schnee.
    /// - <b>Gelände</b> (ein Höhenfeld auf einem Polarnetz): liegt innen knapp unter dem Eis, steigt nach außen aus ihm
    ///   heraus – mit unregelmäßigem Rand und Dünen quer zum Wind, dann Felsbuckel und drei gestaffelte Bergketten.
    ///   Wo es steil ist, Fels (gemaltes Farbbild, <see cref="TerrainMaterial"/>). In Richtung Bucht sinkt es unter den Wasserspiegel und
    ///   steigt erst am anderen Ufer wieder als Inseln auf. Nah = mit Schatten, fern = ohne.
    /// - <b>Meer</b>, <b>Treibeis</b> und ein <b>Presseisrücken</b> auf dem Festeis vor der Kante.
    ///
    /// Wenige große Meshes (ein Objekt je Teil, zusammen etwa 48 500 Dreiecke), gleiche Eingabe = gleiche Landschaft.
    /// Die Meshes liegen im Cache von <see cref="Shapes"/> – ein zweiter Hub (Stilprobe) baut nichts neu.
    /// </summary>
    public static class ArcticLandscape
    {
        /// <summary>Baut die ganze Landschaft unter <paramref name="parent"/> (Mitte = Hub-Mitte).</summary>
        public static Transform Build(Transform parent)
        {
            var root = new GameObject("Landscape").transform;
            root.SetParent(parent, false);
            var snow = Prims.Mat(Palette.Snow, ArtStyle.Surface.Snow, 1);
            var ice = Prims.Mat(Palette.Ice, ArtStyle.Surface.Ice, 1);
            var terrain = LandGrid().Material;
            Part(root, "FastIce", IceMesh(), false, ice);
            Part(root, "LandNear", LandMesh(true), true, terrain);
            Part(root, "LandFar", LandMesh(false), false, terrain);
            Part(root, "DriftIce", FloeMesh(), false, snow, ice);
            Part(root, "PressureRidge", RidgeMesh(), true, ice);
            var sea = Prims.Make(PrimitiveType.Cylinder, root, new Vector3(0, L.SeaLevel - 0.05f, 0),
                new Vector3(L.OuterRadius * 2f, 0.05f, L.OuterRadius * 2f), Palette.Sea, "Sea");
            sea.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return root;
        }

        /// <summary>Richtung eines Punkts in Grad: 0 = Norden (+Z), 90 = Osten (+X) – wie der Gierwinkel der Kamera.</summary>
        public static float Azimuth(float x, float z) => Mathf.Atan2(x, z) * Mathf.Rad2Deg;

        /// <summary>
        /// Abstand der Eiskante zur Hub-Mitte in dieser Richtung. Mitten in der Bucht etwa <see cref="L.CoastRadius"/>,
        /// zu ihren Seiten hin weicht sie zurück, bis sie unter dem Land liegt. Das Gleis endet davor (HubWorld).
        /// </summary>
        public static float CoastAt(float azimuth)
        {
            float a = azimuth * Mathf.Deg2Rad;
            // Rauschen auf einem Kreis: rundum stetig, ohne Naht bei 0°/360°
            float cx = Mathf.Sin(a) * L.CoastRadius, cz = Mathf.Cos(a) * L.CoastRadius;
            float jag = (Fbm(cx, cz, 5f, 3, 11) - 0.5f) * 2f * L.CoastJag;
            return L.CoastRadius + jag + (1f - BayDirection(azimuth)) * L.CoastRecede;
        }

        /// <summary>Höhe des Geländes (ohne Festeis und Meer) an diesem Punkt.</summary>
        public static float Height(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z), az = Azimuth(x, z);
            // Rand der Schneedecke: buchtet unregelmäßig ein und aus, nie näher als FlatRadius
            float edge = Mathf.Max(L.FlatRadius + L.SnowBank, L.SnowEdge + (Fbm(x, z, L.SnowEdgeScale, 3, 5) - 0.5f) * 2f * L.SnowEdgeJag);
            float dunes = Drift(x, z) * Smooth(L.FlatRadius - 1f, L.FlatRadius + 3f, r);
            float drift = dunes * Smooth(edge - 3f, edge + 3f, r);

            // Land: unter dem Eis, dann ein weicher Schneewulst auf dem Eis, Verwehungen, Felsbuckel, dann Berge
            float plain = Mathf.Lerp(-L.UnderIce, L.SnowLevel, Smooth(-0.5f * L.SnowBank, L.SnowBank, r - edge));
            float knolls = L.KnollHeight * Smooth(0.64f, 0.85f, Fbm(x, z, L.KnollSpacing, 2, 61)) * Smooth(L.KnollRadius, L.KnollRadius + 6f, r);
            float land = plain + L.DriftHeight * drift + knolls + Mountains(x, z, r, az);

            // Bucht: unter dem Festeis nur die höchsten Verwehungen als Schneeflecken, hinter der Kante Meeresgrund,
            // am anderen Ufer Inseln
            float coast = CoastAt(az);
            float open = Smooth(coast - 1.5f, coast + 0.5f, r);
            float fast = -L.UnderIce + L.FastIceSnow * L.DriftHeight * dunes;
            // Das andere Ufer ragt überall aus dem Wasser (sonst sähe man durch eine Lücke das Ende des Netzes) und steigt
            // bis zum Rand an – sein Kamm ist die Silhouette. In der Mitte der Bucht niedriger: Dort öffnet sie sich zum Meer.
            float mouth = Mathf.Lerp(L.IslandMouth, 1f, Smooth(0f, L.SeaHalfWidth, Mathf.Abs(Mathf.DeltaAngle(az, L.SeaAzimuth))));
            float peaks = Ridged(x, z, L.PeakSpacing * 0.8f, 4, 23);
            float islands = Smooth(L.FarShoreRadius - 16f, L.FarShoreRadius + 4f, r)
                * (L.SeaLevel - L.SeaFloor + L.ShoreMin + L.IslandHeight * mouth * peaks * peaks);
            float bay = Mathf.Lerp(fast, L.SeaFloor, open) + islands;

            float inBay = BayDirection(az) * Smooth(L.FlatRadius - 1f, L.FlatRadius + 2f, r);
            return Mathf.Lerp(land, bay, inBay);
        }

        // ---------- Formen ----------

        /// <summary>1 in Richtung Bucht, 0 über Land, weich dazwischen.</summary>
        private static float BayDirection(float azimuth)
        {
            float off = Mathf.Abs(Mathf.DeltaAngle(azimuth, L.SeaAzimuth));
            return 1f - Smooth(L.SeaHalfWidth - L.SeaFeather, L.SeaHalfWidth + L.SeaFeather, off);
        }

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


        // ---------- Festeis ----------

        /// <summary>
        /// Ebenes Eis (y = 0) bis zur Eiskante bzw. unter das Land, an der Kante eine fast senkrechte Eiswand bis unter den
        /// Wasserspiegel. Die Wand hat eigene Eckpunkte, damit die Kante scharf bleibt statt weich verschliffen.
        /// </summary>
        private static Mesh IceMesh() => Shapes.Cached("landscape_ice" + L.Seed, () =>
        {
            int seg = L.Segments;
            float[] frac = { 0.3f, 0.55f, 0.75f, 0.87f, 0.94f, 0.98f, 1f };
            int top = frac.Length;
            var v = new List<Vector3> { Vector3.zero };
            for (int k = 0; k < top; k++)
                for (int j = 0; j < seg; j++) v.Add(OnIce(j, frac[k], 0f));
            int lip = v.Count;
            for (int j = 0; j < seg; j++) v.Add(OnIce(j, 1f, 0f));
            for (int j = 0; j < seg; j++) v.Add(OnIce(j, 1f, L.CliffSlant) + Vector3.up * (L.SeaLevel - L.CliffDepth));
            var t = new List<int>();
            for (int j = 0; j < seg; j++) { t.Add(0); t.Add(1 + j); t.Add(1 + (j + 1) % seg); }
            for (int k = 0; k < top - 1; k++) Band(t, 1 + k * seg, seg);
            Band(t, lip, seg);
            var uv = new Vector2[v.Count];
            float tiles = ArtStyle.Surfaces.Ice.TilesPerMeter;
            for (int i = 0; i < uv.Length; i++)
                uv[i] = i >= lip ? new Vector2(v[i].x + v[i].z, v[i].y) * tiles : new Vector2(v[i].x, v[i].z) * tiles;
            var m = new Mesh();
            m.SetVertices(v);
            m.uv = uv;
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        });

        /// <summary>Punkt auf dem Festeis in Spalte <paramref name="j"/>: Anteil des Kantenabstands plus Zugabe in Metern.</summary>
        private static Vector3 OnIce(int j, float frac, float extra)
        {
            float az = j * 360f / L.Segments;
            float edge = Mathf.Min(L.IceRadius, CoastAt(az));
            float r = edge * frac + extra, a = az * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
        }

        /// <summary>Zwei Ringe ab <paramref name="first"/> (je <paramref name="seg"/> Punkte) zu einem Band verbinden.</summary>
        private static void Band(List<int> t, int first, int seg)
        {
            for (int j = 0; j < seg; j++)
            {
                int a = first + j, b = first + (j + 1) % seg, c = a + seg, d = b + seg;
                t.Add(a); t.Add(c); t.Add(b);
                t.Add(b); t.Add(c); t.Add(d);
            }
        }

        // ---------- Treibeis ----------

        /// <summary>Treibende Schollen in der Bucht: verschneit (Teilmesh 0), mit Eiskanten (Teilmesh 1), dicht an der Kante kleiner.</summary>
        private static Mesh FloeMesh() => Shapes.Cached("landscape_floes" + L.Seed, () =>
        {
            var rnd = new System.Random(L.Seed);
            var placed = new List<Vector3>(); // x, z, Radius
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var snow = new List<int>();
            var ice = new List<int>();
            float tiles = ArtStyle.Surfaces.Ice.TilesPerMeter;
            float yTop = L.SeaLevel + L.FloeThickness / 3f, yBottom = yTop - L.FloeThickness;
            for (int tries = 0; tries < L.FloeCount * 20 && placed.Count < L.FloeCount; tries++)
            {
                float az = L.SeaAzimuth + ((float)rnd.NextDouble() * 2f - 1f) * L.SeaHalfWidth * 0.8f;
                float coast = CoastAt(az);
                float near = (float)rnd.NextDouble();
                float r = Mathf.Lerp(coast + 2.5f, L.FloeReach, near * near);
                float size = Mathf.Lerp(L.FloeMin, L.FloeMax, Mathf.Lerp((float)rnd.NextDouble(), near, 0.5f));
                float a = az * Mathf.Deg2Rad;
                var c = new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
                bool free = true;
                foreach (var p in placed)
                    if (new Vector2(p.x - c.x, p.y - c.z).magnitude < p.z + size + 0.6f) { free = false; break; }
                // nur im offenen Wasser, nicht auf Inseln oder Ufer
                for (int s = 0; s < 4 && free; s++)
                {
                    float sa = s * Mathf.PI / 2f;
                    if (Height(c.x + Mathf.Sin(sa) * size * 1.5f, c.z + Mathf.Cos(sa) * size * 1.5f) > L.SeaLevel - 0.4f) free = false;
                }
                if (!free) continue;
                placed.Add(new Vector3(c.x, c.z, size));

                // Umriss: gestreckt und mit Ausbuchtungen verschiedener Größe (Wellen 2., 3. und 5. Ordnung), dazu
                // kleine Zacken – gleichmäßige Vielecke wirkten wie Pfannkuchen.
                const int n = 18;
                float spin = (float)rnd.NextDouble() * Mathf.PI * 2f, stretch = 0.55f + 0.45f * (float)rnd.NextDouble();
                float p2 = (float)rnd.NextDouble() * 6.3f, p3 = (float)rnd.NextDouble() * 6.3f, p5 = (float)rnd.NextDouble() * 6.3f;
                var rim = new Vector3[n];
                for (int k = 0; k < n; k++)
                {
                    float ang = (k + ((float)rnd.NextDouble() - 0.5f) * 0.5f) * Mathf.PI * 2f / n;
                    float rr = 1f + 0.16f * Mathf.Sin(2f * ang + p2) + 0.12f * Mathf.Sin(3f * ang + p3)
                        + 0.07f * Mathf.Sin(5f * ang + p5) + 0.1f * ((float)rnd.NextDouble() - 0.5f);
                    float lx = Mathf.Sin(ang) * rr * size, lz = Mathf.Cos(ang) * rr * size * stretch;
                    rim[k] = c + new Vector3(lx * Mathf.Cos(spin) + lz * Mathf.Sin(spin), 0f, -lx * Mathf.Sin(spin) + lz * Mathf.Cos(spin));
                }
                // Deckel: Fächer um die Mitte
                int centre = v.Count;
                v.Add(c + Vector3.up * yTop);
                for (int k = 0; k < n; k++) v.Add(rim[k] + Vector3.up * yTop);
                for (int k = 0; k < n; k++) { snow.Add(centre); snow.Add(centre + 1 + k); snow.Add(centre + 1 + (k + 1) % n); }
                // Seiten: eigene Eckpunkte je Fläche, damit die Kante scharf bleibt
                for (int k = 0; k < n; k++)
                {
                    var p0 = rim[k]; var p1 = rim[(k + 1) % n];
                    int b = v.Count;
                    v.Add(p0 + Vector3.up * yTop); v.Add(p1 + Vector3.up * yTop);
                    v.Add(p0 + Vector3.up * yBottom); v.Add(p1 + Vector3.up * yBottom);
                    ice.Add(b); ice.Add(b + 2); ice.Add(b + 1);
                    ice.Add(b + 1); ice.Add(b + 2); ice.Add(b + 3);
                }
            }
            foreach (var p in v) uv.Add(new Vector2(p.x + p.y, p.z + p.y) * tiles);
            var m = new Mesh();
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.subMeshCount = 2;
            m.SetTriangles(snow, 0);
            m.SetTriangles(ice, 1);
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        });

        // ---------- Presseis ----------

        /// <summary>
        /// Presseisrücken: verkantete Eisblöcke in einer Linie auf dem Festeis, parallel zur Eiskante – dort, wo Schollen
        /// gegeneinander gedrückt wurden. In der Mitte des Rückens höher, an den Enden flach auslaufend.
        /// </summary>
        private static Mesh RidgeMesh() => Shapes.Cached("landscape_ridge" + L.Seed, () =>
        {
            var rnd = new System.Random(L.Seed + 1);
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            float a0 = L.SeaAzimuth + L.RidgeFrom, a1 = L.SeaAzimuth + L.RidgeTo;
            float mid = CoastAt(0.5f * (a0 + a1)) - L.RidgeInset;
            int count = Mathf.RoundToInt(mid * (a1 - a0) * Mathf.Deg2Rad * L.RidgeDensity);
            for (int i = 0; i < count; i++)
            {
                float u = (i + (float)rnd.NextDouble()) / count;
                float az = Mathf.Lerp(a0, a1, u);
                float r = CoastAt(az) - L.RidgeInset + ((float)rnd.NextDouble() - 0.5f) * 1.4f;
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
