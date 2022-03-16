using System.Collections.Generic;
using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Otoparktan çıkan arabanın yolda izleyeceği rotayı hesaplar.
    /// Çevre yol TEK YÖN (saat yönünün tersine): altta sağa, sağda yukarı, üstte sola, solda aşağı.
    /// Araba çıkışa (kuzey ortası) ancak bu yönde ilerleyerek ulaşır; gerekirse bütün turu dolaşır.
    /// </summary>
    public static class RoadPath
    {
        public static List<Vector3> Build(Vector3 lanePoint, RoadLayout L, float y)
        {
            float a = L.laneX, b = L.laneZ;
            float perimeter = 4f * a + 4f * b;
            var corners = new[] { new Vector2(-a, -b), new Vector2(a, -b), new Vector2(a, b), new Vector2(-a, b) };
            float[] cornerParam = { 0f, 2f * a, 2f * a + 2f * b, 4f * a + 2f * b };

            // Dikdörtgen üzerindeki bir noktanın "kaçıncı metrede" olduğunu bul (sol alttan saat yönünün tersine)
            float Param(Vector2 p)
            {
                float eps = 0.05f;
                if (Mathf.Abs(p.y + b) < eps) return p.x + a;
                if (Mathf.Abs(p.x - a) < eps) return 2f * a + (p.y + b);
                if (Mathf.Abs(p.y - b) < eps) return 2f * a + 2f * b + (a - p.x);
                return 4f * a + 2f * b + (b - p.y);
            }

            var start = new Vector2(lanePoint.x, lanePoint.z);
            var exit = new Vector2(-a, b); // çıkış: sol üst köşe
            float sP = Param(start), sE = Param(exit);
            float dist = Mathf.Repeat(sE - sP, perimeter);

            var picked = new List<(float d, Vector2 p)>();
            for (int i = 0; i < 4; i++)
            {
                float d = Mathf.Repeat(cornerParam[i] - sP, perimeter);
                if (d > 0.01f && d < dist - 0.01f) picked.Add((d, corners[i]));
            }
            picked.Sort((p, q) => p.d.CompareTo(q.d));

            var pts = new List<Vector3> { lanePoint };
            foreach (var c in picked) pts.Add(new Vector3(c.p.x, y, c.p.y));
            pts.Add(new Vector3(-a, y, b));
            pts.Add(new Vector3(-a, y, L.walkHalfZ + 3f));
            pts.Add(new Vector3(-a, y, L.walkHalfZ + 9f));
            return Smooth(pts, 1.0f);
        }

        /// <summary>Renkli kapılı levellerde 3 parçalı rota.</summary>
        public class GatedRoute
        {
            public List<Vector3> entry;     // yola girişten kapı önündeki karar noktasına
            public List<Vector3> exitTail;  // karar noktasından kapıdan geçip uzaklaşma
            public List<Vector3> loop;      // kapı izin vermezse: bütün turu atıp karar noktasına dönüş
        }

        /// <summary>
        /// Karar noktası D, sol üst köşeden biraz önce (üst yolda). Araba D'ye gelince kapının rengine bakar:
        /// uyuyorsa exitTail ile çıkar, uymuyorsa loop ile bir tur daha atar.
        /// </summary>
        public static GatedRoute BuildGated(Vector3 lanePoint, RoadLayout L, float y)
        {
            float a = L.laneX, b = L.laneZ;
            float perimeter = 4f * a + 4f * b;
            var corners = new[] { new Vector2(-a, -b), new Vector2(a, -b), new Vector2(a, b), new Vector2(-a, b) };
            float[] cornerParam = { 0f, 2f * a, 2f * a + 2f * b, 4f * a + 2f * b };
            float Param(Vector2 p)
            {
                float eps = 0.05f;
                if (Mathf.Abs(p.y + b) < eps) return p.x + a;
                if (Mathf.Abs(p.x - a) < eps) return 2f * a + (p.y + b);
                if (Mathf.Abs(p.y - b) < eps) return 2f * a + 2f * b + (a - p.x);
                return 4f * a + 2f * b + (b - p.y);
            }

            var D = new Vector3(-a + 1.2f, y, b);
            float sP = Param(new Vector2(lanePoint.x, lanePoint.z));
            float sD = Param(new Vector2(D.x, D.z));
            float dist = Mathf.Repeat(sD - sP, perimeter);
            var picked = new List<(float d, Vector2 p)>();
            for (int i = 0; i < 4; i++)
            {
                float d = Mathf.Repeat(cornerParam[i] - sP, perimeter);
                if (d > 0.01f && d < dist - 0.01f) picked.Add((d, corners[i]));
            }
            picked.Sort((p, q) => p.d.CompareTo(q.d));
            var entry = new List<Vector3> { lanePoint };
            foreach (var c in picked) entry.Add(new Vector3(c.p.x, y, c.p.y));
            entry.Add(D);

            Vector3 TL = new Vector3(-a, y, b), BL = new Vector3(-a, y, -b), BR = new Vector3(a, y, -b), TR = new Vector3(a, y, b);
            return new GatedRoute
            {
                entry = Smooth(entry, 1.0f),
                exitTail = Smooth(new List<Vector3> { D, TL, new Vector3(-a, y, L.walkHalfZ + 3f), new Vector3(-a, y, L.walkHalfZ + 9f) }, 1.0f),
                loop = Smooth(new List<Vector3> { D, TL, BL, BR, TR, D }, 1.0f),
            };
        }

        /// <summary>Keskin köşeleri yumuşak kavislere çevirir (Bezier eğrisi).</summary>
        static List<Vector3> Smooth(List<Vector3> pts, float radius)
        {
            if (pts.Count < 3) return pts;
            var outPts = new List<Vector3> { pts[0] };
            for (int i = 1; i < pts.Count - 1; i++)
            {
                Vector3 prev = pts[i - 1], cur = pts[i], next = pts[i + 1];
                float r = Mathf.Min(radius, Vector3.Distance(prev, cur) * 0.5f, Vector3.Distance(cur, next) * 0.5f);
                Vector3 p0 = cur + (prev - cur).normalized * r;
                Vector3 p2 = cur + (next - cur).normalized * r;
                for (int k = 0; k <= 6; k++)
                {
                    float t = k / 6f;
                    outPts.Add((1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * cur + t * t * p2);
                }
            }
            outPts.Add(pts[pts.Count - 1]);
            return outPts;
        }

        /// <summary>Yol üzerinde, baştan "distance" metre ilerideki nokta.</summary>
        public static Vector3 Sample(List<Vector3> pts, float[] cumulative, float distance)
        {
            if (distance <= 0) return pts[0];
            for (int i = 1; i < pts.Count; i++)
            {
                if (distance <= cumulative[i])
                {
                    float seg = cumulative[i] - cumulative[i - 1];
                    float t = seg > 0 ? (distance - cumulative[i - 1]) / seg : 0;
                    return Vector3.Lerp(pts[i - 1], pts[i], t);
                }
            }
            Vector3 last = pts[pts.Count - 1], dir = (last - pts[pts.Count - 2]).normalized;
            return last + dir * (distance - cumulative[cumulative.Length - 1]);
        }

        public static float[] Cumulative(List<Vector3> pts)
        {
            var c = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++) c[i] = c[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);
            return c;
        }
    }
}
