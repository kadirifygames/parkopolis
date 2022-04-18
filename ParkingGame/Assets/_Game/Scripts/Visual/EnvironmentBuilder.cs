using System.Collections.Generic;
using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>Yol ve otopark ölçüleri. Arabaların çıkış rotası bu ölçülerden hesaplanır.</summary>
    public class RoadLayout
    {
        public float lotHalfX, lotHalfZ;     // otopark zemininin yarı genişliği/derinliği
        public float laneX, laneZ;           // çevre yolun şerit ortası
        public float roadHalfX, roadHalfZ;   // yolun dış kenarı
        public float walkHalfX, walkHalfZ;   // kaldırımın dış kenarı
        public float exitHalf;               // kuzey çıkış yolunun yarı genişliği
        public float exitX;                  // çıkış yolunun x konumu (sol üst köşe)
        public Bounds framing;               // kameranın göstermesi gereken alan
        public Barrier barrier;
    }

    /// <summary>
    /// Otoparkı ve çevresini (yol, kaldırım, bina, ağaç, lamba...) kodla kurar.
    /// </summary>
    public static class EnvironmentBuilder
    {
        const float RoadWidth = 1.8f;
        const float WalkWidth = 1.1f;
        const float LotTop = 0.07f;   // otopark zemininin üst yüksekliği
        const float RoadTop = 0.06f;
        const float WalkTop = 0.13f;

        static readonly List<Rect> blocked = new List<Rect>();
        static Theme T;   // şu anki şehir

        public static RoadLayout Build(Transform root, int cols, int rows, float cell, int seed, Theme theme)
        {
            T = theme;
            var L = new RoadLayout();
            L.lotHalfX = cols * cell / 2f + 0.3f;
            L.lotHalfZ = rows * cell / 2f + 0.3f;
            L.laneX = L.lotHalfX + RoadWidth / 2f;
            L.laneZ = L.lotHalfZ + RoadWidth / 2f;
            L.roadHalfX = L.lotHalfX + RoadWidth;
            L.roadHalfZ = L.lotHalfZ + RoadWidth;
            L.walkHalfX = L.roadHalfX + WalkWidth;
            L.walkHalfZ = L.roadHalfZ + WalkWidth;
            L.exitHalf = 1.1f;
            L.exitX = -L.laneX; // çıkış sol üst köşede: üst yol sağdan sola akıp buraya ulaşır

            var rng = new System.Random(seed);
            blocked.Clear();
            // Yolların ve kaldırımların bulunduğu alanlara ağaç koyma
            blocked.Add(Rect.MinMaxRect(-L.walkHalfX - 0.3f, -L.walkHalfZ - 0.3f, L.walkHalfX + 0.3f, L.walkHalfZ + 0.3f));
            blocked.Add(Rect.MinMaxRect(L.exitX - L.exitHalf - WalkWidth - 0.3f, L.walkHalfZ - 1f, L.exitX + L.exitHalf + WalkWidth + 0.3f, L.walkHalfZ + 60f));

            // Antalya: kuzeyde deniz var, oraya ağaç koyma
            if (T.Skyline == SkylineStyle.Coast)
                blocked.Add(Rect.MinMaxRect(-200f, L.walkHalfZ + 3.5f, 200f, 200f));

            // Üst şerit (otoparkın arkası) şehir manzarasına ayrıldı: oraya ağaç koyma
            blocked.Add(Rect.MinMaxRect(-200f, L.walkHalfZ + 0.6f, 200f, 200f));

            Ground(root, L, rng);
            Surfaces(root, L);
            Markings(root, L, cols, rows, cell);
            L.barrier = Gate(root, L);
            Buildings(root, L, rng);
            Nature(root, L, rng);
            Props(root, L, rng);
            SkylineBuilder.Build(root, L, T, rng, blocked);

            L.framing = new Bounds(new Vector3(0, 0.3f, 0.1f),
                new Vector3(L.roadHalfX * 2f - 0.2f, 0.6f, L.roadHalfZ * 2f + 0.2f));
            return L;
        }

        static bool IsFree(Vector2 p, float radius)
        {
            foreach (var r in blocked)
            {
                var grown = Rect.MinMaxRect(r.xMin - radius, r.yMin - radius, r.xMax + radius, r.yMax + radius);
                if (grown.Contains(p)) return false;
            }
            return true;
        }

        static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static Color Wall(int i) => T.Walls[i % T.Walls.Length];
        static Color RoofAlt => T.Night ? Palette.Hex("#3A3F55") : (T.Trees == TreeStyle.SnowPine ? T.Roof : Palette.RoofDark);

        // ---------------- Zemin ----------------

        static void Ground(Transform root, RoadLayout L, System.Random rng)
        {
            // Doğal görünen, hafif lekeli çim dokusu (her 4 metrede bir tekrar eder)
            Shape.Part("Grass", root, MeshFactory.Quad(220f, 220f, true), Mats.LitTex(T.Grass, Mats.GrassTex, 0.25f, T.Trees == TreeStyle.SnowPine ? 0.35f : 0.05f),
                new Vector3(0, 0, 30f), null, false);
        }

        static void Surfaces(Transform root, RoadLayout L)
        {
            float wet = T.Weather == Weather.Rain ? 0.62f : 0f; // yağmurda ıslak, parlak asfalt
            var lot = Mats.LitTex(T.Lot, Mats.Asphalt, 0.4f, Mathf.Max(0.2f, wet));
            var road = Mats.LitTex(T.Road, Mats.Asphalt, 0.4f, Mathf.Max(0.18f, wet));
            var walk = Mats.LitTex(T.Sidewalk, Mats.Paver, 1.6f, 0.1f);

            // Otopark zemini
            Shape.Box("Lot", root, new Vector3(L.lotHalfX * 2f, LotTop, L.lotHalfZ * 2f), 0.04f, lot,
                new Vector3(0, LotTop / 2f, 0), null, false);

            // Çevre yol: 4 şerit
            float rw = RoadWidth;
            Shape.Box("RoadS", root, new Vector3(L.roadHalfX * 2f, RoadTop, rw), 0.01f, road, new Vector3(0, RoadTop / 2f, -(L.lotHalfZ + rw / 2f)), null, false);
            Shape.Box("RoadN", root, new Vector3(L.roadHalfX * 2f, RoadTop, rw), 0.01f, road, new Vector3(0, RoadTop / 2f, L.lotHalfZ + rw / 2f), null, false);
            Shape.Box("RoadW", root, new Vector3(rw, RoadTop, L.lotHalfZ * 2f), 0.01f, road, new Vector3(-(L.lotHalfX + rw / 2f), RoadTop / 2f, 0), null, false);
            Shape.Box("RoadE", root, new Vector3(rw, RoadTop, L.lotHalfZ * 2f), 0.01f, road, new Vector3(L.lotHalfX + rw / 2f, RoadTop / 2f, 0), null, false);

            // Kuzeye giden çıkış yolu
            Shape.Box("ExitRoad", root, new Vector3(L.exitHalf * 2f, RoadTop, 60f), 0.01f, road,
                new Vector3(L.exitX, RoadTop / 2f, L.roadHalfZ + 30f), null, false);

            // Kaldırımlar (yoldan biraz yüksek, kenarları yuvarlak)
            float ww = WalkWidth;
            Shape.Box("WalkS", root, new Vector3(L.walkHalfX * 2f, WalkTop, ww), 0.05f, walk, new Vector3(0, WalkTop / 2f, -(L.roadHalfZ + ww / 2f)), null, false);
            Shape.Box("WalkW", root, new Vector3(ww, WalkTop, L.roadHalfZ * 2f), 0.05f, walk, new Vector3(-(L.roadHalfX + ww / 2f), WalkTop / 2f, 0), null, false);
            Shape.Box("WalkE", root, new Vector3(ww, WalkTop, L.roadHalfZ * 2f), 0.05f, walk, new Vector3(L.roadHalfX + ww / 2f, WalkTop / 2f, 0), null, false);
            float gapL = L.exitX - L.exitHalf, gapR = L.exitX + L.exitHalf;
            float wl = gapL + L.walkHalfX, wr = L.walkHalfX - gapR;
            if (wl > 0.05f) Shape.Box("WalkNW", root, new Vector3(wl, WalkTop, ww), 0.04f, walk, new Vector3(-L.walkHalfX + wl / 2f, WalkTop / 2f, L.roadHalfZ + ww / 2f), null, false);
            Shape.Box("WalkNE", root, new Vector3(wr, WalkTop, ww), 0.05f, walk, new Vector3(gapR + wr / 2f, WalkTop / 2f, L.roadHalfZ + ww / 2f), null, false);
            Shape.Box("WalkExitW", root, new Vector3(ww, WalkTop, 60f), 0.05f, walk, new Vector3(gapL - ww / 2f, WalkTop / 2f, L.walkHalfZ + 30f), null, false);
            Shape.Box("WalkExitE", root, new Vector3(ww, WalkTop, 60f), 0.05f, walk, new Vector3(gapR + ww / 2f, WalkTop / 2f, L.walkHalfZ + 30f), null, false);
        }

        static void Markings(Transform root, RoadLayout L, int cols, int rows, float cell)
        {
            var white = Mats.Lit(Palette.Line, 0.3f);
            var soft = Mats.Lit(Palette.LineSoft, 0.2f);
            float y = LotTop + 0.004f;

            // Otopark çerçevesi
            float gx = cols * cell / 2f + 0.12f, gz = rows * cell / 2f + 0.12f, w = 0.07f;
            Shape.Paint("Border", root, new Vector3(gx * 2f + w, 0.01f, w), white, new Vector3(0, y, -gz));
            Shape.Paint("Border", root, new Vector3(gx * 2f + w, 0.01f, w), white, new Vector3(0, y, gz));
            Shape.Paint("Border", root, new Vector3(w, 0.01f, gz * 2f), white, new Vector3(-gx, y, 0));
            Shape.Paint("Border", root, new Vector3(w, 0.01f, gz * 2f), white, new Vector3(gx, y, 0));

            // Park yeri çizgileri: ince, düşük kontrastlı ızgara
            for (int i = 1; i < cols; i++)
                Shape.Paint("GridV", root, new Vector3(0.035f, 0.01f, rows * cell), soft, new Vector3((i - cols / 2f) * cell, y, 0));
            for (int j = 1; j < rows; j++)
                Shape.Paint("GridH", root, new Vector3(cols * cell, 0.01f, 0.035f), soft, new Vector3(0, y, (j - rows / 2f) * cell));

            // Yoldaki yön okları (çıkışa doğru), yola karışan yumuşak renkte
            float ay = RoadTop + 0.004f;
            var arrow = MeshFactory.FlatArrow(0.8f, 0.42f);
            var arrowMat = Mats.Lit(Palette.Hex("#9AA3B4"), 0.25f);
            void Arrow(float x, float z, float yaw) =>
                Shape.Part("Arrow", root, arrow, arrowMat, new Vector3(x, ay, z), Quaternion.Euler(0, yaw, 0), false);
            // Tek yön: solda aşağı, sağda yukarı, altta sağa, üstte sola
            Arrow(-L.laneX, -L.lotHalfZ * 0.4f, 180);
            Arrow(-L.laneX, L.lotHalfZ * 0.45f, 180);
            Arrow(L.laneX, -L.lotHalfZ * 0.4f, 0);
            Arrow(L.laneX, L.lotHalfZ * 0.45f, 0);
            Arrow(-L.lotHalfX * 0.55f, -L.laneZ, 90);
            Arrow(L.lotHalfX * 0.55f, -L.laneZ, 90);
            Arrow(-L.lotHalfX * 0.6f, L.laneZ, -90);
            Arrow(L.lotHalfX * 0.6f, L.laneZ, -90);

            // Yaya geçitleri
            for (int i = 0; i < 5; i++)
            {
                float x = -0.84f + i * 0.42f;
                Shape.Paint("Zebra", root, new Vector3(0.22f, 0.01f, 0.85f), white, new Vector3(L.exitX + x, ay, L.roadHalfZ + WalkWidth / 2f));
            }
            for (int i = 0; i < 4; i++)
            {
                float z = -(L.lotHalfZ + 0.45f + i * 0.43f);
                Shape.Paint("Zebra", root, new Vector3(0.9f, 0.01f, 0.22f), white, new Vector3(0, ay, z));
            }

            // Çıkış yolunda kesik orta çizgi
            var yellow = Mats.Lit(Palette.LineYellow, 0.3f);
            for (int i = 0; i < 12; i++)
                Shape.Paint("Dash", root, new Vector3(0.08f, 0.01f, 0.6f), yellow, new Vector3(L.exitX, ay, L.walkHalfZ + 2.4f + i * 1.4f));
        }

        // ---------------- Bariyer ----------------

        static Barrier Gate(Transform root, RoadLayout L)
        {
            var yellow = Mats.Lit(Palette.UiGold, 0.5f);
            var red = Mats.Lit(Palette.AwningRed, 0.5f);
            var white = Mats.Lit(Color.white, 0.5f);
            var dark = Mats.Lit(Palette.Metal, 0.4f);

            float z = L.walkHalfZ + 0.9f;
            float postX = L.exitX + L.exitHalf + 0.28f;
            Shape.Box("GatePost", root, new Vector3(0.24f, 0.75f, 0.24f), 0.05f, yellow, new Vector3(postX, WalkTop + 0.375f, z));
            Shape.Box("GateCap", root, new Vector3(0.28f, 0.08f, 0.28f), 0.03f, dark, new Vector3(postX, WalkTop + 0.77f, z));

            var pivot = new GameObject("GateArm").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(postX, WalkTop + 0.62f, z);
            float armLen = L.exitHalf * 2f + 0.3f;
            int segs = 5;
            for (int i = 0; i < segs; i++)
            {
                float seg = armLen / segs;
                Shape.Box("ArmSeg", pivot, new Vector3(seg, 0.09f, 0.09f), 0.02f, i % 2 == 0 ? red : white,
                    new Vector3(-seg * (i + 0.5f) - 0.1f, 0, 0));
            }
            var barrier = pivot.gameObject.AddComponent<Barrier>();

            // Gişe kulübesi
            var booth = new GameObject("Booth").transform;
            booth.SetParent(root, false);
            booth.localPosition = new Vector3(L.exitX + L.exitHalf + WalkWidth + 0.95f, 0, L.walkHalfZ + 1.3f);
            Shape.Part("Shadow", booth, MeshFactory.Quad(2f, 1.9f), Mats.Blob(), new Vector3(0, 0.01f, 0), null, false);
            Shape.Box("Body", booth, new Vector3(1.2f, 1.0f, 1.1f), 0.08f, Mats.Lit(Palette.Walls[0], 0.2f), new Vector3(0, 0.5f, 0));
            Shape.Box("Glass", booth, new Vector3(1.22f, 0.34f, 1.12f), 0.06f, Mats.Lit(Palette.Window, 0.85f, 0.1f), new Vector3(0, 0.68f, 0));
            Shape.Box("Roof", booth, new Vector3(1.5f, 0.14f, 1.4f), 0.06f, Mats.Lit(Palette.Hex("#FF8C42"), 0.35f), new Vector3(0, 1.07f, 0));
            blocked.Add(new Rect(booth.localPosition.x - 0.9f, booth.localPosition.z - 0.9f, 1.8f, 1.8f));
            return barrier;
        }

        // ---------------- Binalar ----------------

        static void Buildings(Transform root, RoadLayout L, System.Random rng)
        {
            float z0 = L.walkHalfZ + 0.4f;

            // Sol üst: çizgili tenteli kafe
            var cafeSize = new Vector3(4.2f, 2.3f, 3.0f);
            var cafePos = new Vector3(-(L.exitHalf + WalkWidth + 0.35f + cafeSize.x / 2f), 0, z0 + cafeSize.z / 2f);
            // (Üst şerit artık şehir manzarasına ayrıldı, kafe yan tarafa taşındı)
            Building(root, new Vector3(-(L.walkHalfX + 0.7f + cafeSize.x / 2f), 0, -L.lotHalfZ * 0.95f), cafeSize, Wall(0), T.Roof, rng, awning: true);

            // Sağ üst: ofis binası
            var officeSize = new Vector3(3.8f, 3.6f, 3.4f);
            var officePos = new Vector3(L.exitHalf + WalkWidth + 2.2f + officeSize.x / 2f, 0, z0 + 0.6f + officeSize.z / 2f);


            // Kenarlarda, ekranın yanlarından görünen evler
            var houseSize = new Vector3(3.0f, 2.0f, 3.4f);
            Building(root, new Vector3(-(L.walkHalfX + 0.6f + houseSize.x / 2f), 0, L.lotHalfZ * 0.15f), houseSize,
                Wall(2), T.Roof, rng, awning: false);
            Building(root, new Vector3(L.walkHalfX + 0.6f + houseSize.x / 2f, 0, -L.lotHalfZ * 0.55f), houseSize,
                Wall(3), RoofAlt, rng, awning: false);
            Building(root, new Vector3(L.walkHalfX + 0.8f + 1.6f, 0, L.lotHalfZ * 0.75f + 1f), new Vector3(3.2f, 2.6f, 2.8f),
                Wall(4), T.Roof, rng, awning: true);
        }

        static void Building(Transform root, Vector3 pos, Vector3 size, Color wall, Color roofColor, System.Random rng, bool awning)
        {
            var b = new GameObject("Building").transform;
            b.SetParent(root, false);
            b.localPosition = pos;
            blocked.Add(new Rect(pos.x - size.x / 2f, pos.z - size.z / 2f, size.x, size.z));

            var wallM = Mats.Lit(wall, 0.15f);
            var roofM = Mats.Lit(roofColor, 0.3f);
            var glass = Mats.Lit(T.Night ? Palette.Hex("#2A3350") : Palette.Window, 0.88f, 0.1f);
            var litGlass = Mats.Emissive(Palette.Hex("#FFD98A"), 1.6f);
            var frame = Mats.Lit(Palette.Frame, 0.4f);
            var dark = Mats.Lit(Palette.Metal, 0.3f);

            Shape.Box("Walls", b, size, 0.1f, wallM, new Vector3(0, size.y / 2f, 0));
            Shape.Box("Roof", b, new Vector3(size.x + 0.22f, 0.18f, size.z + 0.22f), 0.07f, roofM, new Vector3(0, size.y + 0.05f, 0));
            Shape.Box("Base", b, new Vector3(size.x + 0.08f, 0.16f, size.z + 0.08f), 0.04f, dark, new Vector3(0, 0.08f, 0), null, false);

            // Çatı üstü klima kutuları
            int ac = 1 + rng.Next(2);
            for (int i = 0; i < ac; i++)
                Shape.Box("AC", b, new Vector3(0.5f, 0.3f, 0.4f), 0.05f, Mats.Lit(Palette.Hex("#D9DEE5"), 0.4f),
                    new Vector3(Range(rng, -size.x * 0.3f, size.x * 0.3f), size.y + 0.29f, Range(rng, -size.z * 0.25f, size.z * 0.25f)));

            // Ön (güney) cephedeki pencereler
            float front = -size.z / 2f - 0.02f;
            int floors = Mathf.Max(1, Mathf.FloorToInt((size.y - (awning ? 1.2f : 0.3f)) / 0.95f));
            int colsW = Mathf.Max(1, Mathf.FloorToInt(size.x / 0.95f));
            float startY = awning ? 1.65f : 0.75f;
            for (int f = 0; f < floors; f++)
            for (int c = 0; c < colsW; c++)
            {
                float x = (c - (colsW - 1) / 2f) * (size.x / colsW);
                float y = startY + f * 0.95f;
                if (y + 0.35f > size.y) continue;
                Shape.Box("Frame", b, new Vector3(0.62f, 0.66f, 0.05f), 0.03f, frame, new Vector3(x, y, front), null, false);
                Shape.Box("Window", b, new Vector3(0.5f, 0.54f, 0.06f), 0.03f, T.Night && rng.NextDouble() < 0.65 ? litGlass : glass, new Vector3(x, y, front - 0.01f), null, false);
            }

            if (awning)
            {
                // Dükkan vitrini + çizgili tente
                Shape.Box("ShopWindow", b, new Vector3(size.x * 0.7f, 0.75f, 0.06f), 0.04f, T.Night ? litGlass : glass, new Vector3(-size.x * 0.08f, 0.62f, front - 0.01f), null, false);
                Shape.Box("Door", b, new Vector3(0.55f, 0.95f, 0.06f), 0.04f, Mats.Lit(Palette.Hex("#8B5E3C"), 0.3f), new Vector3(size.x * 0.36f, 0.55f, front - 0.01f), null, false);
                var tent = new GameObject("Awning").transform;
                tent.SetParent(b, false);
                tent.localPosition = new Vector3(0, 1.28f, front - 0.32f);
                tent.localRotation = Quaternion.Euler(-22f, 0, 0);
                int stripes = 9;
                float sw = size.x * 0.92f / stripes;
                var red = Mats.Lit(Palette.AwningRed, 0.35f);
                var white = Mats.Lit(Color.white, 0.35f);
                for (int i = 0; i < stripes; i++)
                    Shape.Box("Stripe", tent, new Vector3(sw, 0.05f, 0.7f), 0.015f, i % 2 == 0 ? red : white,
                        new Vector3((i - (stripes - 1) / 2f) * sw, 0, 0));

                // Kafe masaları
                for (int i = 0; i < 2; i++)
                {
                    var tablePos = new Vector3((i == 0 ? -1 : 1) * size.x * 0.25f, 0, front - 1.05f);
                    Shape.Part("TableLeg", b, MeshFactory.Cylinder(0.04f, 0.4f, 8), dark, tablePos + Vector3.up * 0.2f);
                    Shape.Part("TableTop", b, MeshFactory.Cylinder(0.24f, 0.04f, 14), white, tablePos + Vector3.up * 0.42f);
                    Shape.Part("Umbrella", b, MeshFactory.Cone(0.45f, 0.22f, 10), i == 0 ? red : Mats.Lit(Palette.UiGold, 0.4f), tablePos + Vector3.up * 0.95f);
                    Shape.Part("Pole", b, MeshFactory.Cylinder(0.02f, 0.6f, 6), white, tablePos + Vector3.up * 0.7f, null, false);
                }
            }
            else
            {
                Shape.Box("Door", b, new Vector3(0.6f, 0.95f, 0.06f), 0.04f, Mats.Lit(Palette.Hex("#3D5A80"), 0.4f), new Vector3(0, 0.55f, front - 0.01f), null, false);
            }
        }

        // ---------------- Doğa ----------------

        static void Tree(Transform root, Vector3 pos, System.Random rng)
        {
            var t = new GameObject("Tree").transform;
            t.SetParent(root, false);
            t.localPosition = pos;
            float s = Range(rng, 0.85f, 1.25f);
            t.localScale = Vector3.one * s;
            t.localRotation = Quaternion.Euler(0, Range(rng, 0, 360), 0);
            Shape.Part("Shadow", t, MeshFactory.Quad(1.6f, 1.6f), Mats.Blob(), new Vector3(0, 0.01f, 0), null, false);
            var trunk = Mats.Lit(Palette.Trunk, 0.15f);
            var leafColor = T.Leaves[rng.Next(T.Leaves.Length)];

            // Şehre göre ağaç tipi
            switch (T.Trees)
            {
                case TreeStyle.Palm: TreeShapes.Palm(t, rng, leafColor); return;
                case TreeStyle.SnowPine: TreeShapes.Pine(t, leafColor, true); return;
                case TreeStyle.Poplar:
                    if (rng.NextDouble() < 0.6) { TreeShapes.Poplar(t, rng, leafColor); return; }
                    break;
                case TreeStyle.Forest:
                    if (rng.NextDouble() < 0.55) { TreeShapes.Pine(t, leafColor, false); return; }
                    break;
            }

            if (T.Trees == TreeStyle.City && rng.NextDouble() < 0.28)
            {
                TreeShapes.Pine(t, Palette.Pine, false);
            }
            else
            {
                // Yuvarlak yapraklı ağaç
                var leaf = Mats.Lit(leafColor, 0.12f);
                Shape.Part("Trunk", t, MeshFactory.Cylinder(0.085f, 0.75f, 7), trunk, new Vector3(0, 0.37f, 0));
                Shape.Part("Leaves", t, MeshFactory.LowPolyBlob(0.55f, rng.Next(1000)), leaf, new Vector3(0, 1.0f, 0));
                if (rng.NextDouble() < 0.6)
                    Shape.Part("Leaves2", t, MeshFactory.LowPolyBlob(0.36f, rng.Next(1000)), leaf, new Vector3(0.32f, 0.8f, 0.1f));
                if (rng.NextDouble() < 0.4)
                    Shape.Part("Leaves3", t, MeshFactory.LowPolyBlob(0.32f, rng.Next(1000)), leaf, new Vector3(-0.28f, 1.25f, -0.1f));
            }
        }

        static void Bush(Transform root, Vector3 pos, System.Random rng)
        {
            // Erzurum'da çalı yerine kar yığını
            bool snow = T.Trees == TreeStyle.SnowPine;
            var leaf = snow ? Mats.Lit(Palette.Hex("#F7FAFD"), 0.4f) : Mats.Lit(T.Leaves[rng.Next(T.Leaves.Length)], 0.12f);
            float r = Range(rng, 0.22f, 0.34f);
            if (snow)
            {
                Shape.Part("Snow", root, MeshFactory.LowPolyBlob(r * 1.2f, rng.Next(1000), 0.1f, 0.45f), leaf, pos + Vector3.up * r * 0.2f);
                return;
            }
            Shape.Part("Bush", root, MeshFactory.LowPolyBlob(r, rng.Next(1000), 0.15f, 0.75f), leaf, pos + Vector3.up * r * 0.6f);
            int flowers = rng.Next(0, 4);
            for (int i = 0; i < flowers; i++)
            {
                var c = Mats.Lit(Palette.Flowers[rng.Next(Palette.Flowers.Length)], 0.3f);
                var off = new Vector3(Range(rng, -r, r), r * 0.9f + Range(rng, -0.05f, 0.08f), Range(rng, -r, r) * 0.6f);
                Shape.Part("Flower", root, MeshFactory.LowPolyBlob(0.055f, rng.Next(1000), 0.05f, 1f), c, pos + off, null, false);
            }
        }

        static void Nature(Transform root, RoadLayout L, System.Random rng)
        {
            // Sol ve sağ şeritler
            for (float z = -L.walkHalfZ - 4f; z < L.walkHalfZ + 6f; z += 1.5f)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    if (rng.NextDouble() < 0.25) continue;
                    var p = new Vector2(side * (L.walkHalfX + Range(rng, 0.8f, 4.5f)), z + Range(rng, -0.5f, 0.5f));
                    if (IsFree(p, 0.6f)) Tree(root, new Vector3(p.x, 0, p.y), rng);
                }
            }
            // Alt kısım: ağaçlar ve küçük park
            for (float x = -L.walkHalfX - 3f; x < L.walkHalfX + 3f; x += 1.45f)
            {
                for (int row = 0; row < 2; row++)
                {
                    if (rng.NextDouble() < 0.3) continue;
                    var p = new Vector2(x + Range(rng, -0.4f, 0.4f), -L.walkHalfZ - 1.0f - row * 1.6f - Range(rng, 0, 0.6f));
                    if (IsFree(p, 0.6f)) Tree(root, new Vector3(p.x, 0, p.y), rng);
                }
            }
            // Kaldırım dış kenarı boyunca çalılar ve çiçekler
            for (float x = -L.walkHalfX + 0.4f; x < L.walkHalfX; x += 0.62f)
            {
                if (Mathf.Abs(x) < 1.2f || rng.NextDouble() < 0.2) continue;
                Bush(root, new Vector3(x, 0, -L.walkHalfZ - 0.35f), rng);
            }
            for (float z = -L.walkHalfZ + 0.4f; z < L.walkHalfZ - 0.4f; z += 0.65f)
            {
                if (rng.NextDouble() < 0.35) continue;
                Bush(root, new Vector3(-L.walkHalfX - 0.35f, 0, z), rng);
                Bush(root, new Vector3(L.walkHalfX + 0.35f, 0, z), rng);
            }
            // Üst taraf
            for (int i = 0; i < 10; i++)
            {
                var p = new Vector2(Range(rng, -L.walkHalfX - 6f, L.walkHalfX + 6f), Range(rng, L.walkHalfZ + 1f, L.walkHalfZ + 9f));
                if (IsFree(p, 0.7f)) Tree(root, new Vector3(p.x, 0, p.y), rng);
            }
        }

        // ---------------- Sokak eşyaları ----------------

        static void Lamp(Transform root, Vector3 pos, float yaw)
        {
            var metal = Mats.Lit(Palette.Metal, 0.5f, 0.3f);
            var l = new GameObject("Lamp").transform;
            l.SetParent(root, false);
            l.localPosition = pos;
            l.localRotation = Quaternion.Euler(0, yaw, 0);
            Shape.Part("Pole", l, MeshFactory.Cylinder(0.045f, 1.9f, 8), metal, new Vector3(0, 0.95f, 0));
            Shape.Box("Arm", l, new Vector3(0.05f, 0.05f, 0.45f), 0.02f, metal, new Vector3(0, 1.88f, 0.2f));
            Shape.Box("Head", l, new Vector3(0.26f, 0.09f, 0.34f), 0.04f, metal, new Vector3(0, 1.85f, 0.42f));
            Shape.Box("Bulb", l, new Vector3(0.18f, 0.03f, 0.24f), 0.012f, Mats.Emissive(Palette.HeadLight, T.Night ? 4f : 2.2f), new Vector3(0, 1.8f, 0.42f), null, false);
            if (T.Night)
            {
                // Gece: lambanın yere düşen sıcak ışık halkası
                Shape.Part("Glow", l, MeshFactory.Quad(3.2f, 3.2f), Mats.Glow(Palette.Hex("#FFC870"), 0.45f),
                    new Vector3(0, 0.015f, 0.6f), null, false);
            }
        }

        static void Bench(Transform root, Vector3 pos, float yaw)
        {
            var wood = Mats.Lit(Palette.Hex("#C0814A"), 0.3f);
            var metal = Mats.Lit(Palette.Metal, 0.4f);
            var b = new GameObject("Bench").transform;
            b.SetParent(root, false);
            b.localPosition = pos;
            b.localRotation = Quaternion.Euler(0, yaw, 0);
            Shape.Box("Seat", b, new Vector3(0.95f, 0.06f, 0.32f), 0.02f, wood, new Vector3(0, 0.3f, 0));
            Shape.Box("Back", b, new Vector3(0.95f, 0.22f, 0.05f), 0.02f, wood, new Vector3(0, 0.48f, 0.15f));
            foreach (float x in new[] { -0.4f, 0.4f })
                Shape.Box("Leg", b, new Vector3(0.05f, 0.3f, 0.3f), 0.01f, metal, new Vector3(x, 0.15f, 0));
        }

        static void Props(Transform root, RoadLayout L, System.Random rng)
        {
            float cx = L.roadHalfX + 0.3f, cz = L.roadHalfZ + 0.3f;
            Lamp(root, new Vector3(-cx, WalkTop, -cz), 45);
            Lamp(root, new Vector3(cx, WalkTop, -cz), -45);
            Lamp(root, new Vector3(-cx, WalkTop, cz * 0.1f), 90);
            Lamp(root, new Vector3(cx, WalkTop, cz * 0.1f), -90);
            Lamp(root, new Vector3(L.exitX - L.exitHalf - 0.35f, WalkTop, L.walkHalfZ + 3f), 90);

            var bin = Mats.Lit(Palette.Hex("#3A8F5C"), 0.35f);
            var binLid = Mats.Lit(Palette.Hex("#2C6E47"), 0.35f);
            foreach (var p in new[] { new Vector3(-cx + 0.45f, WalkTop, -cz), new Vector3(cx, WalkTop, cz * 0.1f + 0.5f) })
            {
                Shape.Part("Bin", root, MeshFactory.Cylinder(0.15f, 0.38f, 12), bin, p + Vector3.up * 0.19f);
                Shape.Part("BinLid", root, MeshFactory.Cylinder(0.17f, 0.05f, 12), binLid, p + Vector3.up * 0.4f);
            }

            Bench(root, new Vector3(-L.walkHalfX * 0.45f, 0, -L.walkHalfZ - 1.2f), 180);
            Bench(root, new Vector3(L.walkHalfX * 0.45f, 0, -L.walkHalfZ - 1.2f), 180);

            // Yangın musluğu
            var red = Mats.Lit(Palette.AwningRed, 0.45f);
            var hp = new Vector3(L.roadHalfX + 0.35f, WalkTop, -L.lotHalfZ * 0.5f);
            Shape.Part("Hydrant", root, MeshFactory.Cylinder(0.08f, 0.32f, 10), red, hp + Vector3.up * 0.16f);
            Shape.Part("HydrantTop", root, MeshFactory.LowPolyBlob(0.09f, 3, 0.02f, 1f), red, hp + Vector3.up * 0.34f);
        }
    }
}
