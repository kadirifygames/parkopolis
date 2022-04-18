using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>Şehirlere özel ağaç ve bitki şekilleri.</summary>
    public static class TreeShapes
    {
        static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        /// <summary>Çam ağacı. snow = dallarının üstünde kar.</summary>
        public static void Pine(Transform t, Color color, bool snow)
        {
            var trunk = Mats.Lit(Palette.Trunk, 0.15f);
            var pine = Mats.Lit(color, 0.1f);
            var white = Mats.Lit(Palette.Hex("#F7FAFD"), 0.45f);
            Shape.Part("Trunk", t, MeshFactory.Cylinder(0.08f, 0.5f, 6), trunk, new Vector3(0, 0.25f, 0));
            float[] radius = { 0.62f, 0.48f, 0.34f };
            float[] height = { 0.75f, 0.65f, 0.55f };
            float[] y = { 0.35f, 0.75f, 1.12f };
            for (int i = 0; i < 3; i++)
            {
                Shape.Part("Cone", t, MeshFactory.Cone(radius[i], height[i], 7), pine, new Vector3(0, y[i], 0));
                if (snow)
                    Shape.Part("Snow", t, MeshFactory.Cone(radius[i] * 0.62f, height[i] * 0.55f, 7), white,
                        new Vector3(0, y[i] + height[i] * 0.47f, 0), null, false);
            }
        }

        /// <summary>Palmiye: hafif eğik gövde, sarkan uzun yapraklar.</summary>
        public static void Palm(Transform t, System.Random rng, Color leafColor)
        {
            var trunk = Mats.Lit(Palette.Hex("#B98A5A"), 0.2f);
            var ring = Mats.Lit(Palette.Hex("#9C7048"), 0.2f);
            var leaf = Mats.Lit(leafColor, 0.25f);
            float lean = R(rng, -0.25f, 0.25f);
            Vector3 top = Vector3.zero;
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(lean * i * i * 0.04f, 0.15f + i * 0.28f, 0);
                Shape.Part("Trunk", t, MeshFactory.Cylinder(0.09f - i * 0.006f, 0.3f, 7), i % 2 == 0 ? trunk : ring, p,
                    Quaternion.Euler(0, 0, -lean * 12f));
                top = p;
            }
            top += Vector3.up * 0.15f;
            int leaves = 7;
            for (int i = 0; i < leaves; i++)
            {
                var pivot = new GameObject("Frond").transform;
                pivot.SetParent(t, false);
                pivot.localPosition = top;
                pivot.localRotation = Quaternion.Euler(0, i * 360f / leaves + R(rng, -10, 10), 0) * Quaternion.Euler(R(rng, -12f, 0f), 0, 0);
                // İki parçalı yaprak: iç kısım hafif yukarı, dış kısım aşağı sarkar
                Shape.Box("Leaf", pivot, new Vector3(0.22f, 0.04f, 0.5f), 0.02f, leaf, new Vector3(0, 0, 0.24f));
                var outer = new GameObject("Outer").transform;
                outer.SetParent(pivot, false);
                outer.localPosition = new Vector3(0, 0, 0.46f);
                outer.localRotation = Quaternion.Euler(R(rng, 35f, 55f), 0, 0);
                Shape.Box("LeafTip", outer, new Vector3(0.18f, 0.035f, 0.5f), 0.017f, leaf, new Vector3(0, 0, 0.22f));
            }
            Shape.Part("Coconut", t, MeshFactory.LowPolyBlob(0.12f, rng.Next(99), 0.05f, 1f), ring, top + new Vector3(0.05f, -0.1f, 0.05f));
        }

        /// <summary>Kavak: ince, uzun, sivri ağaç (Kapadokya).</summary>
        public static void Poplar(Transform t, System.Random rng, Color leafColor)
        {
            var trunk = Mats.Lit(Palette.Trunk, 0.15f);
            var leaf = Mats.Lit(leafColor, 0.12f);
            Shape.Part("Trunk", t, MeshFactory.Cylinder(0.06f, 0.5f, 6), trunk, new Vector3(0, 0.25f, 0));
            float h = R(rng, 1.7f, 2.3f);
            Shape.Part("Leaves", t, MeshFactory.LowPolyBlob(0.35f, rng.Next(999), 0.1f, h / 0.7f), leaf, new Vector3(0, 0.45f + h * 0.5f, 0));
        }

        /// <summary>Kaktüs / dikenli bitki yerine kullanılabilecek kuru çalı.</summary>
        public static void DryBush(Transform root, Vector3 pos, System.Random rng)
        {
            var c = Mats.Lit(Palette.Hex("#A9A35E"), 0.1f);
            Shape.Part("DryBush", root, MeshFactory.LowPolyBlob(R(rng, 0.2f, 0.3f), rng.Next(999), 0.2f, 0.6f), c, pos + Vector3.up * 0.1f);
        }
    }
}
