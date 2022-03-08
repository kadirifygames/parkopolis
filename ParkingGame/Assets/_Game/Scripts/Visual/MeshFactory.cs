using System.Collections.Generic;
using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// 3D şekilleri (mesh) kodla üretir. Her mesh üçgenlerden oluşur:
    /// köşe noktaları (vertices) + bu noktaları üçer üçer birleştiren liste (triangles).
    /// Üretilen şekiller önbellekte tutulur, aynı şekil ikinci kez hesaplanmaz.
    /// </summary>
    public static class MeshFactory
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        static bool TryCache(string key, out Mesh m) => cache.TryGetValue(key, out m) && m != null;

        // Yardımcı: üçgenleri doğru yöne bakacak şekilde ekleyen basit "mesh yapıcı".
        class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int> t = new List<int>();

            public int Add(Vector3 pos, Vector3 normal, Vector2 tex)
            {
                v.Add(pos); n.Add(normal); uv.Add(tex);
                return v.Count - 1;
            }

            /// <summary>Üçgeni, normali "outward" yönüne bakacak şekilde ekler.</summary>
            public void Tri(int a, int b, int c, Vector3 outward)
            {
                Vector3 normal = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
                if (Vector3.Dot(normal, outward) >= 0f) { t.Add(a); t.Add(b); t.Add(c); }
                else { t.Add(a); t.Add(c); t.Add(b); }
            }

            /// <summary>Düz (flat) gölgelendirilmiş üçgen: low-poly görünüm için.</summary>
            public void FlatTri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                if (Vector3.Dot(normal, outward) < 0f) { (b, c) = (c, b); normal = -normal; }
                int i = v.Count;
                Add(a, normal, Vector2.zero); Add(b, normal, Vector2.right); Add(c, normal, Vector2.up);
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name };
                if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(v);
                m.SetNormals(n);
                m.SetUVs(0, uv);
                m.SetTriangles(t, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        static Vector3 AxisVector(int axis) => axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;

        static float[] BevelCoords(float half, float r, int steps)
        {
            var list = new List<float>();
            for (int i = 0; i <= steps; i++) list.Add(-half + r * i / steps);
            for (int i = 0; i <= steps; i++)
            {
                float c = half - r + r * i / steps;
                if (c > list[list.Count - 1] + 1e-5f) list.Add(c);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Kenarları yuvarlatılmış kutu. taperX/taperZ: üst yüzeyi daraltır (araba kabini gibi).
        /// </summary>
        public static Mesh RoundedBox(Vector3 size, float radius, int steps = 3, float taperX = 1f, float taperZ = 1f)
        {
            string key = $"rb{size}{radius}{steps}{taperX}{taperZ}";
            if (TryCache(key, out var cached)) return cached;

            Vector3 h = size * 0.5f;
            float r = Mathf.Min(radius, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.98f);
            r = Mathf.Max(r, 0.0001f);
            Vector3 inner = h - Vector3.one * r;
            float[][] coords = { BevelCoords(h.x, r, steps), BevelCoords(h.y, r, steps), BevelCoords(h.z, r, steps) };

            var b = new Builder();
            for (int axis = 0; axis < 3; axis++)
            for (int s = -1; s <= 1; s += 2)
            {
                int ua = (axis + 1) % 3, va = (axis + 2) % 3;
                float[] cu = coords[ua], cv = coords[va];
                Vector3 faceNormal = AxisVector(axis) * s;
                int start = b.v.Count;

                for (int j = 0; j < cv.Length; j++)
                for (int i = 0; i < cu.Length; i++)
                {
                    Vector3 p = Vector3.zero;
                    p[axis] = s * h[axis];
                    p[ua] = cu[i];
                    p[va] = cv[j];
                    Vector3 q = new Vector3(
                        Mathf.Clamp(p.x, -inner.x, inner.x),
                        Mathf.Clamp(p.y, -inner.y, inner.y),
                        Mathf.Clamp(p.z, -inner.z, inner.z));
                    Vector3 d = p - q;
                    Vector3 normal = d.sqrMagnitude > 1e-10f ? d.normalized : faceNormal;
                    Vector3 pos = q + normal * r;
                    float ty = (pos.y + h.y) / (2f * h.y);
                    pos.x *= Mathf.Lerp(1f, taperX, ty);
                    pos.z *= Mathf.Lerp(1f, taperZ, ty);
                    b.Add(pos, normal, new Vector2(p[ua], p[va]));
                }

                int w = cu.Length;
                for (int j = 0; j < cv.Length - 1; j++)
                for (int i = 0; i < w - 1; i++)
                {
                    int a0 = start + j * w + i, a1 = a0 + 1, a2 = a0 + w, a3 = a2 + 1;
                    b.Tri(a0, a1, a2, faceNormal);
                    b.Tri(a1, a3, a2, faceNormal);
                }
            }

            var mesh = b.ToMesh("RoundedBox");
            cache[key] = mesh;
            return mesh;
        }

        /// <summary>Y ekseni boyunca silindir. Ortası (0,0,0).</summary>
        public static Mesh Cylinder(float radius, float height, int segments = 18)
        {
            string key = $"cyl{radius}{height}{segments}";
            if (TryCache(key, out var cached)) return cached;
            var b = new Builder();
            float hh = height * 0.5f;

            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 n0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0));
                Vector3 n1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                int b0 = b.Add(n0 * radius + Vector3.down * hh, n0, Vector2.zero);
                int t0 = b.Add(n0 * radius + Vector3.up * hh, n0, Vector2.up);
                int b1 = b.Add(n1 * radius + Vector3.down * hh, n1, Vector2.right);
                int t1 = b.Add(n1 * radius + Vector3.up * hh, n1, Vector2.one);
                Vector3 outward = (n0 + n1).normalized;
                b.Tri(b0, t0, b1, outward);
                b.Tri(b1, t0, t1, outward);

                // kapaklar
                int ct = b.Add(Vector3.up * hh, Vector3.up, Vector2.zero);
                int c0 = b.Add(n0 * radius + Vector3.up * hh, Vector3.up, Vector2.zero);
                int c1 = b.Add(n1 * radius + Vector3.up * hh, Vector3.up, Vector2.zero);
                b.Tri(ct, c0, c1, Vector3.up);
                int cb = b.Add(Vector3.down * hh, Vector3.down, Vector2.zero);
                int d0 = b.Add(n0 * radius + Vector3.down * hh, Vector3.down, Vector2.zero);
                int d1 = b.Add(n1 * radius + Vector3.down * hh, Vector3.down, Vector2.zero);
                b.Tri(cb, d0, d1, Vector3.down);
            }

            var mesh = b.ToMesh("Cylinder");
            cache[key] = mesh;
            return mesh;
        }

        /// <summary>Koni: tabanı y=0'da, ucu yukarıda. Düz gölgeli (low-poly).</summary>
        public static Mesh Cone(float radius, float height, int segments = 8)
        {
            string key = $"cone{radius}{height}{segments}";
            if (TryCache(key, out var cached)) return cached;
            var b = new Builder();
            Vector3 apex = Vector3.up * height;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius;
                Vector3 p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;
                Vector3 mid = (p0 + p1) * 0.5f;
                b.FlatTri(p0, p1, apex, mid + Vector3.up * 0.2f * radius);
                b.FlatTri(Vector3.zero, p0, p1, Vector3.down);
            }
            var mesh = b.ToMesh("Cone");
            cache[key] = mesh;
            return mesh;
        }

        /// <summary>Hafif bozulmuş, köşeli küre. Ağaç yaprakları ve çalılar için.</summary>
        public static Mesh LowPolyBlob(float radius, int seed, float jitter = 0.12f, float squashY = 0.9f)
        {
            string key = $"blob{radius}{seed}{jitter}{squashY}";
            if (TryCache(key, out var cached)) return cached;

            float g = (1f + Mathf.Sqrt(5f)) / 2f;
            var verts = new List<Vector3>
            {
                new Vector3(-1, g, 0), new Vector3(1, g, 0), new Vector3(-1, -g, 0), new Vector3(1, -g, 0),
                new Vector3(0, -1, g), new Vector3(0, 1, g), new Vector3(0, -1, -g), new Vector3(0, 1, -g),
                new Vector3(g, 0, -1), new Vector3(g, 0, 1), new Vector3(-g, 0, -1), new Vector3(-g, 0, 1)
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
            int[] faces =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };

            // Bir kez alt bölümle (80 yüz)
            var midCache = new Dictionary<long, int>();
            int Mid(int a, int c)
            {
                long k = a < c ? ((long)a << 32) | (uint)c : ((long)c << 32) | (uint)a;
                if (midCache.TryGetValue(k, out int idx)) return idx;
                verts.Add(((verts[a] + verts[c]) * 0.5f).normalized);
                midCache[k] = verts.Count - 1;
                return verts.Count - 1;
            }
            var sub = new List<int>();
            for (int i = 0; i < faces.Length; i += 3)
            {
                int a = faces[i], b2 = faces[i + 1], c = faces[i + 2];
                int ab = Mid(a, b2), bc = Mid(b2, c), ca = Mid(c, a);
                sub.AddRange(new[] { a, ab, ca, b2, bc, ab, c, ca, bc, ab, bc, ca });
            }

            var rng = new System.Random(seed);
            for (int i = 0; i < verts.Count; i++)
            {
                float k = 1f + ((float)rng.NextDouble() * 2f - 1f) * jitter;
                Vector3 p = verts[i] * radius * k;
                p.y *= squashY;
                verts[i] = p;
            }

            var b = new Builder();
            for (int i = 0; i < sub.Count; i += 3)
            {
                Vector3 a = verts[sub[i]], c1 = verts[sub[i + 1]], c2 = verts[sub[i + 2]];
                b.FlatTri(a, c1, c2, (a + c1 + c2) / 3f);
            }
            var mesh = b.ToMesh("Blob");
            cache[key] = mesh;
            return mesh;
        }

        /// <summary>Yukarı bakan düz dikdörtgen (zemin, gölge lekesi).</summary>
        public static Mesh Quad(float width, float depth, bool worldUV = false)
        {
            string key = $"quad{width}{depth}{worldUV}";
            if (TryCache(key, out var cached)) return cached;
            var b = new Builder();
            float x = width / 2f, z = depth / 2f;
            // worldUV: doku metre cinsinden tekrarlansın (büyük zeminler için)
            float u = worldUV ? width : 1f, v = worldUV ? depth : 1f;
            int a = b.Add(new Vector3(-x, 0, -z), Vector3.up, new Vector2(0, 0));
            int c = b.Add(new Vector3(x, 0, -z), Vector3.up, new Vector2(u, 0));
            int d = b.Add(new Vector3(-x, 0, z), Vector3.up, new Vector2(0, v));
            int e = b.Add(new Vector3(x, 0, z), Vector3.up, new Vector2(u, v));
            b.Tri(a, d, c, Vector3.up);
            b.Tri(c, d, e, Vector3.up);
            var mesh = b.ToMesh("Quad");
            cache[key] = mesh;
            return mesh;
        }

        /// <summary>Yola boyanmış düz ok işareti, +Z yönünü gösterir.</summary>
        public static Mesh FlatArrow(float length, float width)
        {
            string key = $"arrow{length}{width}";
            if (TryCache(key, out var cached)) return cached;
            var b = new Builder();
            float half = length / 2f, head = length * 0.42f, shaft = width * 0.36f;
            Vector3 up = Vector3.up;
            int s0 = b.Add(new Vector3(-shaft / 2, 0, -half), up, Vector2.zero);
            int s1 = b.Add(new Vector3(shaft / 2, 0, -half), up, Vector2.zero);
            int s2 = b.Add(new Vector3(-shaft / 2, 0, half - head), up, Vector2.zero);
            int s3 = b.Add(new Vector3(shaft / 2, 0, half - head), up, Vector2.zero);
            b.Tri(s0, s2, s1, up);
            b.Tri(s1, s2, s3, up);
            int h0 = b.Add(new Vector3(-width / 2, 0, half - head), up, Vector2.zero);
            int h1 = b.Add(new Vector3(width / 2, 0, half - head), up, Vector2.zero);
            int h2 = b.Add(new Vector3(0, 0, half), up, Vector2.zero);
            b.Tri(h0, h2, h1, up);
            var mesh = b.ToMesh("Arrow");
            cache[key] = mesh;
            return mesh;
        }
    }
}
