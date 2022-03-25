using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Kamerayı, verilen alanı her ekran oranında tam gösterecek şekilde yerleştirir.
    /// Üstte ve altta arayüz için boşluk bırakır.
    /// </summary>
    public static class CameraRig
    {
        public const float Pitch = 43f;   // yere bakış açısı
        public const float Fov = 32f;

        public static void Fit(Camera cam, Bounds area, float topMargin = 0.16f, float bottomMargin = 0.14f)
        {
            cam.fieldOfView = Fov;
            cam.transform.rotation = Quaternion.Euler(Pitch, 0, 0);
            Vector3 target = area.center;
            float d = 20f;

            for (int iter = 0; iter < 5; iter++)
            {
                d = SearchDistance(cam, area, target, topMargin, bottomMargin);
                Place(cam, target, d);
                Range(cam, area, out float minY, out float maxY);
                float want = (bottomMargin + (1f - topMargin)) / 2f;
                float have = (minY + maxY) / 2f;
                float worldPerViewport = 2f * d * Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad) / Mathf.Sin(Pitch * Mathf.Deg2Rad);
                target.z += (have - want) * worldPerViewport;
            }
            Place(cam, target, SearchDistance(cam, area, target, topMargin, bottomMargin));

            // Derinlik efekti: otoparkın arkasından itibaren uzak yerler yumuşakça bulanıklaşır
            var dof = GameBootstrap.Dof;
            if (dof != null)
            {
                Vector3 farEdge = new Vector3(area.center.x, 0, area.max.z);
                float start = Vector3.Distance(cam.transform.position, farEdge) + 0.5f;
                dof.gaussianStart.value = start;
                dof.gaussianEnd.value = start + 9f;
            }
        }

        static void Place(Camera cam, Vector3 target, float distance)
        {
            cam.transform.position = target - cam.transform.forward * distance;
        }

        static float SearchDistance(Camera cam, Bounds area, Vector3 target, float top, float bottom)
        {
            float lo = 3f, hi = 150f;
            for (int i = 0; i < 32; i++)
            {
                float mid = (lo + hi) / 2f;
                Place(cam, target, mid);
                if (Fits(cam, area, top, bottom)) hi = mid; else lo = mid;
            }
            return hi;
        }

        static bool Fits(Camera cam, Bounds b, float top, float bottom)
        {
            foreach (var c in Corners(b))
            {
                Vector3 v = cam.WorldToViewportPoint(c);
                if (v.z <= 0 || v.x < 0.02f || v.x > 0.98f || v.y < bottom || v.y > 1f - top) return false;
            }
            return true;
        }

        static void Range(Camera cam, Bounds b, out float minY, out float maxY)
        {
            minY = float.MaxValue; maxY = float.MinValue;
            foreach (var c in Corners(b))
            {
                float y = cam.WorldToViewportPoint(c).y;
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
        }

        static Vector3[] Corners(Bounds b)
        {
            Vector3 mn = b.min, mx = b.max;
            return new[]
            {
                new Vector3(mn.x, mn.y, mn.z), new Vector3(mx.x, mn.y, mn.z), new Vector3(mn.x, mn.y, mx.z), new Vector3(mx.x, mn.y, mx.z),
                new Vector3(mn.x, mx.y, mn.z), new Vector3(mx.x, mx.y, mn.z), new Vector3(mn.x, mx.y, mx.z), new Vector3(mx.x, mx.y, mx.z)
            };
        }
    }
}
