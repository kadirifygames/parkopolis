using System.Collections.Generic;
using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Ekranın üstünde görünen arka plan manzarası. Her şehrin kendine özgü silueti var.
    /// Uzakta oldukları için derinlik efekti (DOF) onları hafif bulanık gösterir.
    /// </summary>
    public static class SkylineBuilder
    {
        static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static float ex; // çıkış yolunun x konumu: oraya bina koyma

        public static void Build(Transform root, RoadLayout L, Theme t, System.Random rng, List<Rect> blocked)
        {
            ex = L.exitX;
            var sky = new GameObject("Skyline").transform;
            sky.SetParent(root, false);
            float z0 = L.walkHalfZ;

            switch (t.Skyline)
            {
                case SkylineStyle.Istanbul: Istanbul(sky, z0, t, rng); break;
                case SkylineStyle.Coast: Coast(sky, L, t, rng); break;
                case SkylineStyle.Cappadocia: Cappadocia(sky, z0, t, rng); break;
                case SkylineStyle.Mountains: Mountains(sky, z0, t, rng); break;
                case SkylineStyle.Hills: Hills(sky, z0, t, rng); break;
            }
        }

        // ---------------- İstanbul: gökdelenler + kule + cami ----------------

        static void Istanbul(Transform root, float z0, Theme t, System.Random rng)
        {
            Color[] towerColors = t.Night
                ? new[] { Palette.Hex("#4A5378"), Palette.Hex("#3F4868"), Palette.Hex("#56507A") }
                : new[] { Palette.Hex("#C9D6E8"), Palette.Hex("#A9BCD6"), Palette.Hex("#E3E8F0"), Palette.Hex("#B7C9C2") };
            var glass = Mats.Lit(t.Night ? Palette.Hex("#2A3350") : Palette.Hex("#7FB2E5"), 0.9f, 0.3f);
            var lit = Mats.Emissive(Palette.Hex("#FFD98A"), 1.8f);

            for (float x = -34f; x <= 34f; x += R(rng, 2.6f, 3.6f))
            {
                if (Mathf.Abs(x - ex) < 3.2f) continue;
                if (x > 3.4f && x < 11.8f) continue;  // caminin yeri
                if (Mathf.Abs(x - (ex - 5.6f)) < 2.6f) continue; // kulenin yeri
                float h = R(rng, 4f, 11f);
                float w = R(rng, 2f, 3f), d = R(rng, 2f, 3f);
                float z = z0 + R(rng, 4.5f, 7.5f);
                Tower(root, new Vector3(x, 0, z), new Vector3(w, h, d), towerColors[rng.Next(towerColors.Length)], glass, lit, t.Night, rng);
            }

            // Galata benzeri kule
            Vector3 k = new Vector3(ex - 5.6f, 0, z0 + 3.4f);
            var stone = Mats.Lit(t.Night ? Palette.Hex("#8C86A0") : Palette.Hex("#E7D9C2"), 0.2f);
            var cap = Mats.Lit(t.Night ? Palette.Hex("#3E4766") : Palette.Hex("#5C6B86"), 0.4f);
            Shape.Part("TowerBody", root, MeshFactory.Cylinder(1.3f, 7f, 14), stone, k + Vector3.up * 3.5f);
            Shape.Part("TowerBand", root, MeshFactory.Cylinder(1.45f, 0.35f, 14), cap, k + Vector3.up * 6.4f);
            Shape.Part("TowerRoof", root, MeshFactory.Cone(1.45f, 2.4f, 14), cap, k + Vector3.up * 7.1f);
            if (t.Night) Shape.Part("TowerLights", root, MeshFactory.Cylinder(1.32f, 0.5f, 14), lit, k + Vector3.up * 5.8f, null, false);

            // Cami silueti: büyük kubbe, yarım kubbeler, minareler
            Vector3 m = new Vector3(7.6f, 0, z0 + 2.9f);
            var wall = Mats.Lit(t.Night ? Palette.Hex("#8E8AA6") : Palette.Hex("#EDE4D3"), 0.2f);
            var dome = Mats.Lit(t.Night ? Palette.Hex("#5B6488") : Palette.Hex("#8FA3BC"), 0.5f, 0.2f);
            Shape.Box("MosqueBase", root, new Vector3(6f, 2.6f, 5f), 0.1f, wall, m + Vector3.up * 1.3f);
            Shape.Part("Drum", root, MeshFactory.Cylinder(1.9f, 0.8f, 16), wall, m + Vector3.up * 3f);
            Shape.Part("Dome", root, MeshFactory.LowPolyBlob(2f, 5, 0.0f, 0.75f), dome, m + Vector3.up * 3.4f);
            foreach (float sx in new[] { -1f, 1f })
                Shape.Part("HalfDome", root, MeshFactory.LowPolyBlob(1.2f, 6, 0f, 0.7f), dome, m + new Vector3(sx * 2.1f, 2.6f, 0));
            foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                Vector3 p = m + new Vector3(sx * 3.4f, 0, sz * 2.8f);
                Shape.Part("Minaret", root, MeshFactory.Cylinder(0.22f, 7.5f, 10), wall, p + Vector3.up * 3.75f);
                Shape.Part("Balcony", root, MeshFactory.Cylinder(0.34f, 0.15f, 10), wall, p + Vector3.up * 5.6f);
                Shape.Part("Spire", root, MeshFactory.Cone(0.24f, 1.3f, 10), dome, p + Vector3.up * 7.5f);
            }
            if (t.Night)
                Shape.Part("MosqueGlow", root, MeshFactory.Quad(9f, 8f), Mats.Glow(Palette.Hex("#FFD27A"), 0.35f), m + Vector3.up * 0.05f, null, false);
        }

        static void Tower(Transform root, Vector3 pos, Vector3 size, Color color, Material glass, Material lit, bool night, System.Random rng)
        {
            var body = Mats.Lit(color, 0.35f);
            Shape.Box("Tower", root, size, 0.12f, body, pos + Vector3.up * size.y / 2f);
            // Pencere bantları (ön yüz)
            float front = pos.z - size.z / 2f - 0.02f;
            for (float y = 0.9f; y < size.y - 0.4f; y += 0.85f)
            {
                var m = night && rng.NextDouble() < 0.55 ? lit : glass;
                Shape.Box("Band", root, new Vector3(size.x * 0.86f, 0.36f, 0.05f), 0.02f, m, new Vector3(pos.x, y, front), null, false);
            }
            if (rng.NextDouble() < 0.4)
                Shape.Box("Roof", root, new Vector3(size.x * 0.5f, 0.5f, size.z * 0.5f), 0.08f, body, pos + Vector3.up * (size.y + 0.25f));
            if (rng.NextDouble() < 0.25)
            {
                Shape.Part("Antenna", root, MeshFactory.Cylinder(0.04f, 1.6f, 6), Mats.Lit(Palette.Metal, 0.4f), pos + Vector3.up * (size.y + 0.8f), null, false);
                Shape.Part("AntennaLight", root, MeshFactory.LowPolyBlob(0.08f, 1, 0f, 1f), Mats.Emissive(Palette.Hex("#FF4D4D"), 3f), pos + Vector3.up * (size.y + 1.6f), null, false);
            }
        }

        // ---------------- Antalya: deniz + yelkenliler + deniz feneri ----------------

        static void Coast(Transform root, RoadLayout L, Theme t, System.Random rng)
        {
            float shore = L.walkHalfZ + 2.6f;
            var sea = Mats.Lit(Palette.Hex("#3FB7E0"), 0.95f, 0.1f);
            var shallow = Mats.Lit(Palette.Hex("#7FD8E8"), 0.9f);
            var foam = Mats.Lit(Color.white, 0.6f);
            Shape.Part("Sea", root, MeshFactory.Quad(220f, 100f), sea, new Vector3(0, 0.02f, shore + 51.5f), null, false);
            Shape.Part("Shallow", root, MeshFactory.Quad(220f, 3f), shallow, new Vector3(0, 0.025f, shore + 1.5f), null, false);
            Shape.Part("Foam", root, MeshFactory.Quad(220f, 0.25f), foam, new Vector3(0, 0.03f, shore), null, false);

            // İskele: çıkış yolu denize uzanır, altına ahşap ayaklar
            var wood = Mats.Lit(Palette.Hex("#A87B52"), 0.25f);
            for (float z = shore + 1f; z < shore + 20f; z += 2f)
                foreach (float sx in new[] { -1f, 1f })
                    Shape.Part("Pile", root, MeshFactory.Cylinder(0.1f, 0.6f, 6), wood, new Vector3(ex + sx * (L.exitHalf + 0.1f), -0.1f, z));

            // Şemsiyeler ve şezlonglar
            Color[] umbrella = { Palette.Hex("#FF6B6B"), Palette.Hex("#FFD166"), Palette.Hex("#4DA8DA"), Palette.Hex("#06D6A0") };
            for (int i = 0; i < 9; i++)
            {
                float x = R(rng, -22f, 22f);
                if (Mathf.Abs(x - ex) < 3.5f) continue;
                var p = new Vector3(x, 0, shore - R(rng, 0.8f, 1.8f));
                var c = Mats.Lit(umbrella[rng.Next(umbrella.Length)], 0.4f);
                Shape.Part("Pole", root, MeshFactory.Cylinder(0.03f, 1.1f, 6), Mats.Lit(Color.white, 0.4f), p + Vector3.up * 0.55f, null, false);
                Shape.Part("Umbrella", root, MeshFactory.Cone(0.6f, 0.3f, 10), c, p + Vector3.up * 1.0f);
                Shape.Box("Towel", root, new Vector3(0.45f, 0.02f, 0.9f), 0.01f, c, p + new Vector3(0.5f, 0.02f, 0.2f), null, false);
            }

            // Yelkenliler
            for (int i = 0; i < 6; i++)
            {
                float x = R(rng, -26f, 26f);
                var p = new Vector3(x, 0.02f, shore + R(rng, 2f, 6f));
                var boat = new GameObject("Boat").transform;
                boat.SetParent(root, false);
                boat.localPosition = p;
                boat.localRotation = Quaternion.Euler(0, R(rng, 0, 360), 0);
                Shape.Box("Hull", boat, new Vector3(0.7f, 0.3f, 1.8f), 0.14f, Mats.Lit(Color.white, 0.6f), new Vector3(0, 0.1f, 0));
                Shape.Part("Mast", boat, MeshFactory.Cylinder(0.03f, 1.8f, 6), Mats.Lit(Palette.Metal, 0.4f), new Vector3(0, 1.1f, 0.1f));
                Shape.Box("Sail", boat, new Vector3(0.03f, 1.5f, 0.9f), 0.01f, Mats.Lit(i % 2 == 0 ? Color.white : Palette.Hex("#FFB4A2"), 0.3f),
                    new Vector3(0, 1.05f, -0.35f), Quaternion.Euler(0, 0, 0));
                boat.gameObject.AddComponent<Floaty>().Setup(0.05f, R(rng, 1f, 1.6f), 3f);
            }

            // Deniz feneri
            var lh = new Vector3(L.walkHalfX - 1.2f, 0, shore + 2.2f);
            var red = Mats.Lit(Palette.AwningRed, 0.4f);
            var white = Mats.Lit(Color.white, 0.4f);
            Shape.Part("Rock", root, MeshFactory.LowPolyBlob(1.6f, 3, 0.2f, 0.5f), Mats.Lit(Palette.Hex("#9A9387"), 0.15f), lh);
            for (int i = 0; i < 5; i++)
                Shape.Part("LhBand", root, MeshFactory.Cylinder(0.75f - i * 0.06f, 1.1f, 14), i % 2 == 0 ? white : red, lh + Vector3.up * (0.9f + i * 1.1f));
            Shape.Part("LhLamp", root, MeshFactory.Cylinder(0.45f, 0.6f, 12), Mats.Emissive(Palette.Hex("#FFE9A8"), 2f), lh + Vector3.up * 6.4f);
            Shape.Part("LhRoof", root, MeshFactory.Cone(0.6f, 0.7f, 12), red, lh + Vector3.up * 6.7f);
        }

        // ---------------- Kapadokya: peri bacaları + balonlar ----------------

        static void Cappadocia(Transform root, float z0, Theme t, System.Random rng)
        {
            var rock = Mats.Lit(Palette.Hex("#E6C59B"), 0.1f);
            var rockDark = Mats.Lit(Palette.Hex("#D2A97C"), 0.1f);
            var capM = Mats.Lit(Palette.Hex("#8E6A55"), 0.15f);
            for (float x = -32f; x <= 32f; x += R(rng, 2.2f, 3.4f))
            {
                if (Mathf.Abs(x - ex) < 4.8f) continue;
                float h = R(rng, 4f, 9f);
                float r = R(rng, 0.9f, 1.6f);
                var p = new Vector3(x, 0, z0 + R(rng, 2.2f, 7f));
                Shape.Part("Chimney", root, MeshFactory.Cone(r, h, 9), rng.NextDouble() < 0.5 ? rock : rockDark, p);
                Shape.Part("Cap", root, MeshFactory.LowPolyBlob(r * 0.55f, rng.Next(999), 0.1f, 0.6f), capM, p + Vector3.up * (h * 0.88f));
                if (rng.NextDouble() < 0.4)
                    Shape.Box("Door", root, new Vector3(0.3f, 0.5f, 0.05f), 0.05f, Mats.Lit(Palette.Hex("#5A4033"), 0.2f),
                        p + new Vector3(0, 0.5f, -r * 0.8f), null, false);
            }
            // Arka tepe
            Shape.Part("Ridge", root, MeshFactory.LowPolyBlob(14f, 4, 0.1f, 0.35f), rockDark, new Vector3(-14f, -1f, z0 + 10f));
            Shape.Part("Ridge", root, MeshFactory.LowPolyBlob(16f, 9, 0.1f, 0.3f), rock, new Vector3(16f, -1f, z0 + 11f));

            // Sıcak hava balonları
            Color[] colors = { Palette.Hex("#FF6B6B"), Palette.Hex("#FFD166"), Palette.Hex("#4D96FF"), Palette.Hex("#9B5DE5"), Palette.Hex("#06D6A0"), Palette.Hex("#FF9F1C") };
            for (int i = 0; i < 9; i++)
            {
                var b = new GameObject("Balloon").transform;
                b.SetParent(root, false);
                b.localPosition = new Vector3(R(rng, -12f, 12f), R(rng, 3.2f, 6f), z0 + R(rng, -1f, 4f));
                float s = R(rng, 0.7f, 1.2f);
                b.localScale = Vector3.one * s;
                var c = colors[rng.Next(colors.Length)];
                var c2 = colors[rng.Next(colors.Length)];
                Shape.Part("Envelope", b, MeshFactory.LowPolyBlob(0.9f, rng.Next(999), 0.02f, 1.15f), Mats.Lit(c, 0.35f), Vector3.zero);
                Shape.Part("Stripe", b, MeshFactory.Cylinder(0.86f, 0.3f, 14), Mats.Lit(c2, 0.35f), new Vector3(0, -0.15f, 0), null, false);
                Shape.Part("Skirt", b, MeshFactory.Cone(0.5f, 0.55f, 10), Mats.Lit(c, 0.35f), new Vector3(0, -0.6f, 0), Quaternion.Euler(180f, 0, 0));
                Shape.Box("Basket", b, new Vector3(0.32f, 0.26f, 0.32f), 0.04f, Mats.Lit(Palette.Hex("#8B5E3C"), 0.2f), new Vector3(0, -1.35f, 0));
                b.gameObject.AddComponent<Floaty>().Setup(0.35f, R(rng, 0.25f, 0.5f), R(rng, 0, 6f));
            }
        }

        // ---------------- Erzurum: karlı dağlar ----------------

        static void Mountains(Transform root, float z0, Theme t, System.Random rng)
        {
            var rock = Mats.Lit(Palette.Hex("#9AAAC2"), 0.15f);
            var rockDark = Mats.Lit(Palette.Hex("#8293AE"), 0.15f);
            var snow = Mats.Lit(Palette.Hex("#F8FBFF"), 0.5f);
            for (int i = 0; i < 9; i++)
            {
                float x = -30f + i * 7.5f + R(rng, -1.5f, 1.5f);
                float r = R(rng, 3.6f, 5.2f), h = R(rng, 5.5f, 8f);
                var p = new Vector3(x, -0.5f, z0 + R(rng, 5.5f, 7.5f));
                Shape.Part("Mountain", root, MeshFactory.Cone(r, h, 7), i % 2 == 0 ? rock : rockDark, p);
                Shape.Part("SnowCap", root, MeshFactory.Cone(r * 0.42f, h * 0.42f, 7), snow, p + Vector3.up * (h * 0.585f));
            }
            // Ön tarafta karlı çam ormanı
            for (float x = -30f; x <= 30f; x += R(rng, 1.2f, 2f))
            {
                if (Mathf.Abs(x - ex) < 4.8f) continue;
                var tr = new GameObject("Tree").transform;
                tr.SetParent(root, false);
                tr.localPosition = new Vector3(x, 0, z0 + R(rng, 1.2f, 4f));
                tr.localScale = Vector3.one * R(rng, 1.2f, 1.9f);
                TreeShapes.Pine(tr, t.Leaves[rng.Next(t.Leaves.Length)], true);
            }
        }

        // ---------------- Karadeniz: yeşil tepeler + orman ----------------

        static void Hills(Transform root, float z0, Theme t, System.Random rng)
        {
            Color[] greens = { Palette.Hex("#5FA35A"), Palette.Hex("#6FB466"), Palette.Hex("#4E9150"), Palette.Hex("#86BF73") };
            for (int i = 0; i < 8; i++)
            {
                float x = -40f + i * 11f + R(rng, -3f, 3f);
                var p = new Vector3(x, -2f, z0 + R(rng, 6f, 10f));
                Shape.Part("Hill", root, MeshFactory.LowPolyBlob(R(rng, 4.5f, 6.5f), rng.Next(999), 0.08f, 0.5f), Mats.Lit(greens[i % greens.Length], 0.08f), p);
            }
            // Çay bahçesi sıraları (ön tepe)
            var tea = Mats.Lit(Palette.Hex("#3F8A47"), 0.1f);
            for (int row = 0; row < 4; row++)
            for (float x = -28f; x <= 28f; x += 1.1f)
            {
                if (Mathf.Abs(x - ex) < 4.8f) continue;
                Shape.Part("Tea", root, MeshFactory.LowPolyBlob(0.45f, (int)(x * 10) + row, 0.1f, 0.6f), tea,
                    new Vector3(x, 0.2f, z0 + 1.2f + row * 1.1f));
            }
            // Orman
            for (float x = -30f; x <= 30f; x += R(rng, 1.3f, 2.2f))
            {
                if (Mathf.Abs(x - ex) < 4.8f) continue;
                var tr = new GameObject("Tree").transform;
                tr.SetParent(root, false);
                tr.localPosition = new Vector3(x, 0, z0 + R(rng, 5.6f, 8f));
                tr.localScale = Vector3.one * R(rng, 1.4f, 2.1f);
                TreeShapes.Pine(tr, t.Leaves[rng.Next(t.Leaves.Length)], false);
            }
        }
    }

    /// <summary>Hafifçe yukarı aşağı süzülme (balonlar, tekneler).</summary>
    public class Floaty : MonoBehaviour
    {
        float amp, speed, phase;
        Vector3 basePos;

        public void Setup(float amplitude, float speedHz, float phaseOffset)
        {
            amp = amplitude;
            speed = speedHz;
            phase = phaseOffset;
            basePos = transform.localPosition;
        }

        void Update()
        {
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * speed * Mathf.PI * 2f + phase) * amp;
        }
    }
}
