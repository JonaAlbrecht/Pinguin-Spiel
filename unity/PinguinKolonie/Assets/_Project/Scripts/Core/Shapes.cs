using System;
using System.Collections.Generic;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Erzeugt Low-Poly-Formen per Code nach den Stilregeln in <see cref="ArtStyle"/>. Gleiche Form und gleiche
    /// Feinheit ergeben dasselbe Mesh (Cache) – hundert Pinguine teilen sich einen Körper.
    /// Dreh- und Profilformen werden als Ringe (Höhe, Radius, Versatz nach vorn) von unten nach oben beschrieben;
    /// Winkel in Grad, 0 = vorn (+Z), positiv zur Seite +X.
    /// </summary>
    public static class Shapes
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        public static Mesh Cached(string key, Func<Mesh> build)
        {
            key += ArtStyle.Shading.Flat ? "|flat" : "|smooth";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = build();
            m.name = key;
            Cache[key] = m;
            return m;
        }

        /// <summary>Ersatz für Unitys Kugel, Zylinder und Kapsel in denselben Maßen; null = Unitys Mesh behalten (Würfel, Ebene).</summary>
        public static Mesh ForPrimitive(PrimitiveType type, int segments)
        {
            if (segments < 3) return null;
            switch (type)
            {
                case PrimitiveType.Sphere: return Sphere(segments);
                case PrimitiveType.Cylinder: return Cylinder(segments);
                case PrimitiveType.Capsule: return Capsule(segments);
                default: return null;
            }
        }

        /// <summary>Kugel mit Durchmesser 1 um den Ursprung (wie Unitys Kugel).</summary>
        public static Mesh Sphere(int segments) => Cached("sphere" + segments, () =>
        {
            int n = Mathf.Max(2, segments / 2);
            var rings = new Vector3[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float p = Mathf.PI * i / n;
                rings[i] = new Vector3(-0.5f * Mathf.Cos(p), 0.5f * Mathf.Sin(p), 0f);
            }
            return BoxUv(Revolve(rings, segments));
        });

        /// <summary>Zylinder mit Radius 0,5 und Höhe 2 um den Ursprung (wie Unitys Zylinder).</summary>
        public static Mesh Cylinder(int segments) => Cached("cylinder" + segments, () =>
            // Doppelte Ringe an den Kanten trennen Deckel und Mantel, damit weiche Schattierung die Kante nicht verwischt.
            BoxUv(Revolve(new[]
            {
                new Vector3(-1f, 0f, 0f), new Vector3(-1f, 0.5f, 0f), new Vector3(-1f, 0.5f, 0f),
                new Vector3(1f, 0.5f, 0f), new Vector3(1f, 0.5f, 0f), new Vector3(1f, 0f, 0f),
            }, segments)));

        /// <summary>Kapsel mit Radius 0,5 und Höhe 2 um den Ursprung (wie Unitys Kapsel).</summary>
        public static Mesh Capsule(int segments) => Cached("capsule" + segments, () =>
        {
            int h = Mathf.Max(2, segments / 4);
            var rings = new List<Vector3>();
            for (int i = 0; i <= h; i++)
            {
                float p = 0.5f * Mathf.PI * i / h;
                rings.Add(new Vector3(-0.5f - 0.5f * Mathf.Cos(p), 0.5f * Mathf.Sin(p), 0f));
            }
            for (int i = 0; i <= h; i++)
            {
                float p = 0.5f * Mathf.PI * i / h;
                rings.Add(new Vector3(0.5f + 0.5f * Mathf.Sin(p), 0.5f * Mathf.Cos(p), 0f));
            }
            return BoxUv(Revolve(rings, segments));
        });

        /// <summary>
        /// Ersatzformen für Unitys Grundkörper tragen Struktur (Schnee, Eis, Wellen): UV als Projektion entlang der
        /// Achse, auf die die Fläche am meisten zeigt. Die Dreh-UV aus <see cref="Revolve"/> (Winkel × Höhe, für die
        /// gemalten Pinguinkörper) ist auf einem flachen Deckel konstant in der Höhe – Meer, Scholle und Pfützen zeigten
        /// darum strahlenförmige Streifen (ADR-28). 0…1 überspannt den Durchmesser, passend zu <see cref="Prims.TilingFor"/>.
        /// </summary>
        private static Mesh BoxUv(Mesh m)
        {
            var v = m.vertices;
            var n = m.normals;
            var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                var a = new Vector3(Mathf.Abs(n[i].x), Mathf.Abs(n[i].y), Mathf.Abs(n[i].z));
                uv[i] = a.y >= a.x && a.y >= a.z ? new Vector2(v[i].x, v[i].z)
                    : a.x >= a.z ? new Vector2(v[i].z, v[i].y) : new Vector2(v[i].x, v[i].y);
                uv[i] += new Vector2(0.5f, 0.5f);
            }
            m.uv = uv;
            m.RecalculateTangents();
            return m;
        }

        /// <summary>Leicht gewölbter Kegel entlang +Y, Grundfläche Durchmesser 1 bei 0, Spitze bei 1 (Schnabel).</summary>
        public static Mesh Cone(int segments) => Cached("cone" + segments, () =>
            Revolve(new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0.5f, 0f), new Vector3(0f, 0.5f, 0f),
                new Vector3(0.45f, 0.36f, 0f), new Vector3(1f, 0f, 0f),
            }, segments));

        /// <summary>Flacher Ring (Brillenfassung), Außendurchmesser 1, Achse Y, Dicke 0,2.</summary>
        public static Mesh Ring(int segments, float inner) => Cached("ring" + segments + "_" + inner, () =>
            Revolve(new[]
            {
                new Vector3(-0.1f, inner * 0.5f, 0f), new Vector3(-0.1f, 0.5f, 0f), new Vector3(-0.1f, 0.5f, 0f),
                new Vector3(0.1f, 0.5f, 0f), new Vector3(0.1f, 0.5f, 0f), new Vector3(0.1f, inner * 0.5f, 0f),
                new Vector3(0.1f, inner * 0.5f, 0f), new Vector3(-0.1f, inner * 0.5f, 0f),
            }, segments));

        /// <summary>Flosse: hängt vom Ursprung nach −Y (Länge 1), breit in Z (bis 1), dünn in X (bis 1); per Skalierung anpassen.</summary>
        public static Mesh Flipper(int segments) => Cached("flipper" + segments, () =>
            Revolve(new[]
            {
                new Vector3(-1f, 0f, 0f), new Vector3(-0.9f, 0.17f, 0f), new Vector3(-0.6f, 0.36f, 0f),
                new Vector3(-0.25f, 0.5f, 0f), new Vector3(-0.06f, 0.42f, 0f), new Vector3(0f, 0f, 0f),
            }, segments));

        /// <summary>Pinguinfuß mit drei Zehen nach +Z: Länge 1, Breite etwa 1, Dicke 1; per Skalierung anpassen.</summary>
        public static Mesh Foot() => Cached("foot", () => Extrude(new[]
        {
            new Vector2(0f, -0.35f), new Vector2(0.32f, -0.27f), new Vector2(0.46f, 0f), new Vector2(0.48f, 0.52f),
            new Vector2(0.22f, 0.36f), new Vector2(0f, 0.65f), new Vector2(-0.22f, 0.36f), new Vector2(-0.48f, 0.52f),
            new Vector2(-0.46f, 0f), new Vector2(-0.32f, -0.27f),
        }, new Vector2(0f, 0.05f)));

        /// <summary>
        /// Drehkörper aus Ringen (Höhe, Radius, Versatz nach vorn), von unten nach oben. <paramref name="depth"/>
        /// staucht den Querschnitt nach vorn/hinten, <paramref name="a0"/>..<paramref name="a1"/> begrenzt ihn auf einen
        /// Winkelbereich (Kleidung, Streifen). <paramref name="region"/> ordnet jeder Facette anhand von Höhe und Winkel
        /// ihrer Mitte ein Teilmesh zu – so bekommt ein Körper Bauch- und Rückenfarbe ohne zweite Form. Ganze Facetten
        /// statt einzelner Dreiecke: Die Farbgrenze verläuft dann in klaren Stufen statt im Sägezahn.
        /// </summary>
        public static Mesh Revolve(IList<Vector3> rings, int segments, float depth = 1f, float a0 = -180f, float a1 = 180f,
            Func<float, float, int> region = null, int submeshes = 1, float grow = 1f)
        {
            float range = a1 - a0;
            int steps = Mathf.Max(1, Mathf.CeilToInt(segments * range / 360f - 0.001f));
            int cols = steps + 1;
            var grid = new Vector3[rings.Count * cols];
            // UV: u = Winkel über den Bereich, v = Höhe (Anteil der Körperhöhe) – für gemalte Farbgrenzen (TorsoTexture)
            var uv = new Vector2[grid.Length];
            for (int i = 0; i < rings.Count; i++)
                for (int j = 0; j < cols; j++)
                {
                    float a = (a0 + range * j / steps) * Mathf.Deg2Rad;
                    float r = rings[i].y * grow;
                    grid[i * cols + j] = new Vector3(Mathf.Sin(a) * r, rings[i].x, rings[i].z + Mathf.Cos(a) * r * depth);
                    uv[i * cols + j] = new Vector2(j / (float)steps, rings[i].x);
                }

            var b = new Builder(submeshes, ArtStyle.Shading.Flat, grid, uv);
            for (int i = 0; i < rings.Count - 1; i++)
            {
                float zc = 0.5f * (rings[i].z + rings[i + 1].z);
                for (int j = 0; j < steps; j++)
                {
                    int v00 = i * cols + j, v01 = v00 + 1, v10 = v00 + cols, v11 = v10 + 1;
                    int sub = 0;
                    if (region != null)
                    {
                        var m = 0.25f * (grid[v00] + grid[v01] + grid[v10] + grid[v11]);
                        sub = region(m.y, Mathf.Atan2(m.x, (m.z - zc) / depth) * Mathf.Rad2Deg);
                    }
                    // Reihenfolge so, dass die Vorderseite nach außen zeigt (Unity: im Uhrzeigersinn von außen gesehen).
                    b.Tri(v00, v01, v10, sub);
                    b.Tri(v01, v11, v10, sub);
                }
            }
            return b.ToMesh();
        }

        /// <summary>Flache Platte aus einem sternförmigen Umriss (von <paramref name="center"/> aus sichtbar), Dicke 1 nach +Y.</summary>
        public static Mesh Extrude(Vector2[] outline, Vector2 center)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            var c0 = new Vector3(center.x, 0f, center.y);
            var c1 = new Vector3(center.x, 1f, center.y);
            for (int k = 0; k < outline.Length; k++)
            {
                var p = outline[k];
                var q = outline[(k + 1) % outline.Length];
                Vector3 p0 = new Vector3(p.x, 0f, p.y), q0 = new Vector3(q.x, 0f, q.y);
                Vector3 p1 = p0 + Vector3.up, q1 = q0 + Vector3.up;
                AddTri(v, t, c1, p1, q1, Vector3.up);
                AddTri(v, t, c0, p0, q0, Vector3.down);
                var side = 0.5f * (p0 + q0) - c0;
                AddTri(v, t, p0, q0, q1, side);
                AddTri(v, t, p0, q1, p1, side);
            }
            var m = new Mesh();
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static void AddTri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) { var x = b; b = c; c = x; }
            int k = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            t.Add(k); t.Add(k + 1); t.Add(k + 2);
        }

        /// <summary>Sammelt Dreiecke je Teilmesh; facettiert = eigene Eckpunkte je Dreieck, weich = geteilte Eckpunkte.</summary>
        private sealed class Builder
        {
            private readonly List<int>[] _tris;
            private readonly bool _flat;
            private readonly Vector3[] _grid;
            private readonly Vector2[] _gridUv;
            private readonly List<Vector3> _verts = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();

            public Builder(int submeshes, bool flat, Vector3[] grid, Vector2[] uv = null)
            {
                _tris = new List<int>[Mathf.Max(1, submeshes)];
                for (int i = 0; i < _tris.Length; i++) _tris[i] = new List<int>();
                _flat = flat;
                _grid = grid;
                _gridUv = uv;
                if (!flat)
                {
                    _verts.AddRange(grid);
                    if (uv != null) _uvs.AddRange(uv);
                }
            }

            public void Tri(int a, int b, int c, int sub)
            {
                Vector3 pa = _grid[a], pb = _grid[b], pc = _grid[c];
                if (Vector3.Cross(pb - pa, pc - pa).sqrMagnitude < 1e-12f) return;   // Pol: Dreieck ohne Fläche
                var list = _tris[Mathf.Clamp(sub, 0, _tris.Length - 1)];
                if (_flat)
                {
                    int k = _verts.Count;
                    _verts.Add(pa); _verts.Add(pb); _verts.Add(pc);
                    if (_gridUv != null) { _uvs.Add(_gridUv[a]); _uvs.Add(_gridUv[b]); _uvs.Add(_gridUv[c]); }
                    list.Add(k); list.Add(k + 1); list.Add(k + 2);
                }
                else
                {
                    list.Add(a); list.Add(b); list.Add(c);
                }
            }

            public Mesh ToMesh()
            {
                var m = new Mesh();
                if (_verts.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(_verts);
                if (_uvs.Count == _verts.Count) m.SetUVs(0, _uvs);
                m.subMeshCount = _tris.Length;
                for (int i = 0; i < _tris.Length; i++) m.SetTriangles(_tris[i], i);
                m.RecalculateNormals();
                // Normal-Maps brauchen Tangenten, sonst kippt die Struktur je nach Blickrichtung (URP Lit).
                if (_uvs.Count == _verts.Count) m.RecalculateTangents();
                m.RecalculateBounds();
                return m;
            }
        }
    }

    /// <summary>
    /// Weicher Umriss eines Drehkörpers aus wenigen Stützpunkten (Höhe, Radius, Versatz nach vorn), von unten nach oben.
    /// Liefert Ringe in gleichmäßigem Abstand entlang des Umrisses und den Radius in jeder Höhe – damit Kleidung und
    /// Augen auf dem Körper sitzen, auch wenn sich die Proportionen ändern.
    /// </summary>
    public sealed class Outline
    {
        private readonly Vector3[] _dense;

        public Outline(Vector3[] points, int density = 240)
        {
            _dense = new Vector3[density + 1];
            int segs = points.Length - 1;
            for (int s = 0; s <= density; s++)
            {
                float t = (float)s / density * segs;
                int k = Mathf.Min((int)t, segs - 1);
                float u = t - k;
                Vector3 p0 = points[Mathf.Max(k - 1, 0)], p1 = points[k], p2 = points[k + 1], p3 = points[Mathf.Min(k + 2, segs)];
                var p = 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u);
                p.y = Mathf.Max(0f, p.y);
                _dense[s] = p;
            }
            _dense[0] = points[0];
            _dense[density] = points[segs];
        }

        /// <summary>(Höhe, Radius, Versatz nach vorn) in der Höhe <paramref name="y"/>.</summary>
        public Vector3 At(float y)
        {
            if (y <= _dense[0].x) return _dense[0];
            for (int i = 1; i < _dense.Length; i++)
                if (_dense[i].x >= y)
                {
                    float u = Mathf.InverseLerp(_dense[i - 1].x, _dense[i].x, y);
                    return Vector3.Lerp(_dense[i - 1], _dense[i], u);
                }
            return _dense[_dense.Length - 1];
        }

        /// <summary><paramref name="count"/> Ringe zwischen den Höhen <paramref name="y0"/> und <paramref name="y1"/>, gleich weit entlang des Umrisses.</summary>
        public Vector3[] Rings(int count, float y0 = float.MinValue, float y1 = float.MaxValue)
        {
            var pts = new List<Vector3>();
            if (y0 > _dense[0].x) pts.Add(At(y0));
            foreach (var p in _dense) if (p.x > y0 && p.x < y1) pts.Add(p);
            if (y1 < _dense[_dense.Length - 1].x) pts.Add(At(y1));
            else pts.Add(_dense[_dense.Length - 1]);
            if (y0 <= _dense[0].x && (pts.Count == 0 || pts[0] != _dense[0])) pts.Insert(0, _dense[0]);

            var len = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++)
                len[i] = len[i - 1] + Vector2.Distance(new Vector2(pts[i].x, pts[i].y), new Vector2(pts[i - 1].x, pts[i - 1].y));
            count = Mathf.Max(2, count);
            var rings = new Vector3[count];
            int seg = 1;
            for (int r = 0; r < count; r++)
            {
                float s = len[len.Length - 1] * r / (count - 1);
                while (seg < len.Length - 1 && len[seg] < s) seg++;
                float u = len[seg] > len[seg - 1] ? (s - len[seg - 1]) / (len[seg] - len[seg - 1]) : 0f;
                rings[r] = Vector3.Lerp(pts[seg - 1], pts[seg], Mathf.Clamp01(u));
            }
            return rings;
        }

        /// <summary>Steigung des Radius nach der Höhe – für die Neigung der Fläche (Augen, Flecken).</summary>
        public float Slope(float y)
        {
            const float h = 0.01f;
            return (At(y + h).y - At(y - h).y) / (2f * h);
        }
    }
}
