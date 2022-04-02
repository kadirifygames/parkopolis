using System.Collections.Generic;
using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Arabaların 3D görünümünü parça parça oluşturur.
    /// Arabanın önü her zaman yerel +Z yönüne bakar.
    /// </summary>
    public static class CarBuilder
    {
        public const float Width = 0.8f;
        const float WheelRadius = 0.16f;

        public class Result
        {
            public Transform root;
            public readonly List<Transform> wheels = new List<Transform>();
            public float length;
        }

        public static Result Build(Transform parent, int cells, float cellSize, Color color, int variant,
            CarModel model = CarModel.None, float smoothness = 0.86f, float metallic = 0.12f)
        {
            var r = new Result();
            r.root = new GameObject("Visual").transform;
            r.root.SetParent(parent, false);
            float L = cells * cellSize - 0.18f;
            r.length = L;

            // Altındaki yumuşak gölge lekesi
            Shape.Part("Shadow", r.root, MeshFactory.Quad(Width * 1.55f, L * 1.2f), Mats.Blob(),
                new Vector3(0, 0.012f, 0), null, false);

            var paint = Mats.Lit(color, smoothness, metallic);
            if (model != CarModel.None && cells == 2)
            {
                Special(r, L, model);
            }
            else if (cells >= 3)
            {
                if (variant % 2 == 0) Bus(r, L, paint);
                else Truck(r, L, paint);
            }
            else
            {
                Car(r, L, paint, color, variant % 4);
            }
            return r;
        }

        static void Wheels(Result r, float L, float inset)
        {
            var tire = Mats.Lit(Palette.Tire, 0.2f);
            var hub = Mats.Lit(Palette.Hub, 0.88f, 0.9f);
            float z = L / 2f - inset;
            float x = Width / 2f - 0.07f;
            foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                // Pivot: dönme ekseni X olan boş nesne. Tekerlek bunun çocuğu, böylece kolayca döndürürüz.
                var pivot = new GameObject("Wheel").transform;
                pivot.SetParent(r.root, false);
                pivot.localPosition = new Vector3(sx * x, WheelRadius, sz * z);
                var rot = Quaternion.Euler(0, 0, 90);
                Shape.Part("Tire", pivot, MeshFactory.Cylinder(WheelRadius, 0.13f, 16), tire, Vector3.zero, rot);
                Shape.Part("Hub", pivot, MeshFactory.Cylinder(0.075f, 0.145f, 10), hub, Vector3.zero, rot, false);
                r.wheels.Add(pivot);
            }
        }

        static void Lights(Transform root, float L, float y, float spread)
        {
            var head = Mats.Emissive(Palette.HeadLight, 1.6f);
            var tail = Mats.Emissive(Palette.TailLight, 1.2f);
            foreach (float sx in new[] { -1f, 1f })
            {
                Shape.Box("HeadLight", root, new Vector3(0.17f, 0.075f, 0.06f), 0.025f, head,
                    new Vector3(sx * spread, y, L / 2f - 0.015f), null, false);
                Shape.Box("TailLight", root, new Vector3(0.15f, 0.065f, 0.05f), 0.02f, tail,
                    new Vector3(sx * spread, y, -L / 2f + 0.012f), null, false);
            }
        }

        static void Car(Result r, float L, Material paint, Color color, int variant)
        {
            var glass = Mats.Lit(Palette.Glass, 0.96f, 0.35f);
            var trim = Mats.Lit(Palette.Trim, 0.35f);

            // Tip: gövde yüksekliği, kabin uzunluğu, kabin konumu, kabin yüksekliği, ön cam eğimi
            float bodyH = 0.32f, cabLen = 0.55f, cabZ = -0.05f, cabH = 0.27f, slope = 0.72f;
            switch (variant)
            {
                case 1: cabLen = 0.6f; cabZ = -0.12f; cabH = 0.29f; slope = 0.86f; break;          // hatchback
                case 2: bodyH = 0.34f; cabLen = 0.8f; cabZ = -0.06f; cabH = 0.36f; slope = 0.92f; break; // minibüs
                case 3: bodyH = 0.26f; cabLen = 0.44f; cabZ = -0.07f; cabH = 0.2f; slope = 0.62f; break;   // spor
            }

            float bottom = 0.12f;
            float bodyY = bottom + bodyH / 2f;
            float bodyTop = bottom + bodyH;

            Wheels(r, L, 0.34f);

            // Gövde
            Shape.Box("Body", r.root, new Vector3(Width, bodyH, L), 0.15f, paint, new Vector3(0, bodyY, 0));
            Shape.Box("Underbody", r.root, new Vector3(Width * 0.86f, 0.1f, L * 0.82f), 0.04f, trim, new Vector3(0, bottom + 0.01f, 0), null, false);

            // Cam kabin (üstü daralan kutu)
            float cabinLen = L * cabLen;
            float cabinY = bodyTop + cabH / 2f - 0.035f;
            Shape.Part("Cabin", r.root,
                MeshFactory.RoundedBox(new Vector3(Width * 0.84f, cabH, cabinLen), 0.09f, 3, 0.86f, slope),
                glass, new Vector3(0, cabinY, cabZ * L));

            // Tavan: camın üstünü kapatır, kenarlarda cam görünür kalır
            float roofTop = cabinY + cabH / 2f;
            Shape.Box("Roof", r.root, new Vector3(Width * 0.75f, 0.06f, cabinLen * slope * 0.98f), 0.03f, paint,
                new Vector3(0, roofTop - 0.005f, cabZ * L));

            // Tamponlar
            Shape.Box("BumperF", r.root, new Vector3(Width * 0.97f, 0.08f, 0.1f), 0.035f, trim,
                new Vector3(0, bottom + 0.05f, L / 2f - 0.02f));
            Shape.Box("BumperR", r.root, new Vector3(Width * 0.97f, 0.08f, 0.1f), 0.035f, trim,
                new Vector3(0, bottom + 0.05f, -L / 2f + 0.02f));

            Lights(r.root, L, bodyY + 0.03f, Width / 2f - 0.16f);

            // Yan aynalar
            float mirrorZ = cabZ * L + cabinLen / 2f - 0.06f;
            foreach (float sx in new[] { -1f, 1f })
                Shape.Box("Mirror", r.root, new Vector3(0.08f, 0.06f, 0.05f), 0.02f, paint,
                    new Vector3(sx * (Width / 2f + 0.025f), bodyTop + 0.03f, mirrorZ), null, false);

            // Küçük detaylar
            if (variant == 3)
            {
                // Spor araba: kaput üstünde beyaz şerit
                var stripe = Mats.Lit(Color.white, 0.8f);
                Shape.Box("Stripe", r.root, new Vector3(0.12f, 0.02f, L * 0.9f), 0.008f, stripe,
                    new Vector3(0, bodyTop - 0.002f, 0), null, false);
            }
            else if (variant == 0 && color.r > 0.9f && color.g > 0.75f && color.b < 0.4f)
            {
                // Sarı sedan = taksi tabelası
                var sign = Mats.Emissive(Palette.Hex("#FFF7D6"), 0.6f);
                Shape.Box("TaxiSign", r.root, new Vector3(0.3f, 0.09f, 0.12f), 0.03f, sign,
                    new Vector3(0, roofTop + 0.06f, cabZ * L));
            }
            else if (variant == 2)
            {
                // Minibüs: tavan rayı
                foreach (float sx in new[] { -1f, 1f })
                    Shape.Box("Rail", r.root, new Vector3(0.03f, 0.03f, cabinLen * 0.75f), 0.01f, trim,
                        new Vector3(sx * Width * 0.3f, roofTop + 0.03f, cabZ * L), null, false);
            }
        }

        static void Bus(Result r, float L, Material paint)
        {
            var glass = Mats.Lit(Palette.Glass, 0.96f, 0.35f);
            var white = Mats.Lit(Palette.Hex("#F6F6F2"), 0.6f);
            var trim = Mats.Lit(Palette.Trim, 0.35f);
            float bottom = 0.12f, bodyH = 0.6f;

            Wheels(r, L, 0.42f);
            Shape.Box("Body", r.root, new Vector3(Width, bodyH, L), 0.14f, paint, new Vector3(0, bottom + bodyH / 2f, 0));
            // Camlar: gövdenin etrafını saran bant
            Shape.Box("Windows", r.root, new Vector3(Width + 0.014f, 0.19f, L + 0.014f), 0.08f, glass,
                new Vector3(0, bottom + bodyH - 0.17f, 0));
            Shape.Box("Roof", r.root, new Vector3(Width * 0.9f, 0.06f, L * 0.92f), 0.03f, white,
                new Vector3(0, bottom + bodyH + 0.02f, 0));
            Shape.Box("Bumper", r.root, new Vector3(Width * 0.98f, 0.09f, L + 0.04f), 0.04f, trim,
                new Vector3(0, bottom + 0.05f, 0));
            Lights(r.root, L, bottom + 0.15f, Width / 2f - 0.15f);
        }

        static void Truck(Result r, float L, Material paint)
        {
            var glass = Mats.Lit(Palette.Glass, 0.96f, 0.35f);
            var cargo = Mats.Lit(Palette.Hex("#F3F1EA"), 0.45f);
            var trim = Mats.Lit(Palette.Trim, 0.35f);
            float bottom = 0.12f;
            float cabLen = 0.78f;

            Wheels(r, L, 0.4f);
            Shape.Box("Chassis", r.root, new Vector3(Width * 0.9f, 0.1f, L - 0.1f), 0.04f, trim,
                new Vector3(0, bottom + 0.04f, 0));
            // Kabin (önde)
            Shape.Box("Cab", r.root, new Vector3(Width, 0.52f, cabLen), 0.13f, paint,
                new Vector3(0, bottom + 0.26f, L / 2f - cabLen / 2f));
            Shape.Box("Windshield", r.root, new Vector3(Width * 0.88f, 0.18f, cabLen * 0.6f), 0.06f, glass,
                new Vector3(0, bottom + 0.37f, L / 2f - cabLen * 0.3f + 0.012f));
            // Yük kasası (arkada)
            float boxLen = L - cabLen - 0.06f;
            float boxZ = -L / 2f + boxLen / 2f;
            Shape.Box("Cargo", r.root, new Vector3(Width * 1.02f, 0.72f, boxLen), 0.08f, cargo,
                new Vector3(0, bottom + 0.1f + 0.36f, boxZ));
            Shape.Box("CargoStripe", r.root, new Vector3(Width * 1.04f, 0.1f, boxLen * 0.9f), 0.03f, paint,
                new Vector3(0, bottom + 0.42f, boxZ), null, false);
            Lights(r.root, L, bottom + 0.2f, Width / 2f - 0.15f);
        }

        // ================= ÖZEL ARABALAR (Garaj) =================

        struct Dims { public float bodyY, bodyTop, roofTop, cabZ, cabinLen; }

        /// <summary>Car() ile aynı ölçüler: detayları doğru yüksekliğe koymak için.</summary>
        static Dims DimsFor(int variant, float L)
        {
            float bodyH = 0.32f, cabLen = 0.55f, cabZ = -0.05f, cabH = 0.27f;
            switch (variant)
            {
                case 1: cabLen = 0.6f; cabZ = -0.12f; cabH = 0.29f; break;
                case 2: bodyH = 0.34f; cabLen = 0.8f; cabZ = -0.06f; cabH = 0.36f; break;
                case 3: bodyH = 0.26f; cabLen = 0.44f; cabZ = -0.07f; cabH = 0.2f; break;
            }
            float bottom = 0.12f;
            var d = new Dims { bodyY = bottom + bodyH / 2f, bodyTop = bottom + bodyH, cabZ = cabZ * L, cabinLen = L * cabLen };
            float cabinY = d.bodyTop + cabH / 2f - 0.035f;
            d.roofTop = cabinY + cabH / 2f;
            return d;
        }

        static void SideStripe(Transform root, float L, float y, float h, Material m, float lengthK = 0.86f)
        {
            foreach (float sx in new[] { -1f, 1f })
                Shape.Box("Stripe", root, new Vector3(0.02f, h, L * lengthK), 0.008f, m, new Vector3(sx * (Width / 2f + 0.004f), y, 0), null, false);
        }

        /// <summary>Tepe lambası: sönük taban + yanıp sönen parlak üst.</summary>
        static void Beacon(Transform root, Vector3 pos, Color a, Color b)
        {
            var bar = new GameObject("Beacon").transform;
            bar.SetParent(root, false);
            bar.localPosition = pos;
            Shape.Box("Base", bar, new Vector3(0.44f, 0.05f, 0.14f), 0.02f, Mats.Lit(Palette.Trim, 0.4f), Vector3.zero, null, false);
            Shape.Box("DimA", bar, new Vector3(0.18f, 0.07f, 0.12f), 0.025f, Mats.Lit(a * 0.6f, 0.6f), new Vector3(-0.11f, 0.05f, 0), null, false);
            Shape.Box("DimB", bar, new Vector3(0.18f, 0.07f, 0.12f), 0.025f, Mats.Lit(b * 0.6f, 0.6f), new Vector3(0.11f, 0.05f, 0), null, false);
            var la = Shape.Box("LitA", bar, new Vector3(0.19f, 0.08f, 0.13f), 0.025f, Mats.Emissive(a, 3f), new Vector3(-0.11f, 0.05f, 0), null, false);
            var lb = Shape.Box("LitB", bar, new Vector3(0.19f, 0.08f, 0.13f), 0.025f, Mats.Emissive(b, 3f), new Vector3(0.11f, 0.05f, 0), null, false);
            bar.gameObject.AddComponent<Blink>().Setup(la, lb);
        }

        static void Special(Result r, float L, CarModel model)
        {
            var white = Mats.Lit(Palette.Hex("#F5F7FA"), 0.8f, 0.1f);
            var black = Mats.Lit(Palette.Hex("#22262E"), 0.5f);
            var chrome = Mats.Lit(Palette.Hub, 0.92f, 0.9f);
            switch (model)
            {
                case CarModel.Taxi:
                {
                    var c = Palette.Hex("#FFC93C");
                    Car(r, L, Mats.Lit(c, 0.86f, 0.12f), c, 0);
                    var d = DimsFor(0, L);
                    // Damalı şerit
                    for (int i = 0; i < 12; i++)
                        foreach (float sx in new[] { -1f, 1f })
                            Shape.Box("Check", r.root, new Vector3(0.02f, 0.06f, 0.11f), 0.005f, i % 2 == 0 ? black : white,
                                new Vector3(sx * (Width / 2f + 0.004f), d.bodyY + 0.02f, -L * 0.33f + i * L * 0.06f), null, false);
                    break;
                }
                case CarModel.Classic:
                {
                    var c = Palette.Hex("#7FD8BE");
                    Car(r, L, Mats.Lit(c, 0.9f, 0.2f), c, 1);
                    var d = DimsFor(1, L);
                    SideStripe(r.root, L, d.bodyY - 0.02f, 0.03f, chrome, 0.9f);
                    foreach (float sx in new[] { -1f, 1f })
                        Shape.Part("RoundLight", r.root, MeshFactory.Cylinder(0.07f, 0.04f, 14), Mats.Emissive(Palette.HeadLight, 1.8f),
                            new Vector3(sx * (Width / 2f - 0.17f), d.bodyY + 0.03f, L / 2f + 0.005f), Quaternion.Euler(90, 0, 0), false);
                    Shape.Box("WhiteRoof", r.root, new Vector3(Width * 0.76f, 0.065f, d.cabinLen * 0.85f * 0.98f), 0.03f, white,
                        new Vector3(0, d.roofTop, d.cabZ), null, false);
                    break;
                }
                case CarModel.Dolmus:
                {
                    var c = Palette.Hex("#FFD23F");
                    Car(r, L, Mats.Lit(c, 0.86f, 0.12f), c, 2);
                    var d = DimsFor(2, L);
                    SideStripe(r.root, L, d.bodyY, 0.07f, black, 0.8f);
                    Shape.Box("RoofBand", r.root, new Vector3(Width * 0.77f, 0.07f, d.cabinLen * 0.9f), 0.03f, white, new Vector3(0, d.roofTop + 0.005f, d.cabZ));
                    Shape.Box("Sign", r.root, new Vector3(0.36f, 0.1f, 0.12f), 0.03f, Mats.Emissive(Palette.Hex("#FFF2B3"), 0.8f),
                        new Vector3(0, d.roofTop + 0.09f, d.cabZ + d.cabinLen * 0.3f));
                    break;
                }
                case CarModel.IceCream:
                {
                    var c = Palette.Hex("#FFB3C7");
                    Car(r, L, Mats.Lit(c, 0.8f, 0.05f), c, 2);
                    var d = DimsFor(2, L);
                    SideStripe(r.root, L, d.bodyY - 0.03f, 0.05f, white, 0.85f);
                    var top = new Vector3(0, d.roofTop, d.cabZ);
                    Shape.Part("ConeTop", r.root, MeshFactory.Cone(0.16f, 0.36f, 10), Mats.Lit(Palette.Hex("#E0A458"), 0.3f),
                        top + Vector3.up * 0.39f, Quaternion.Euler(180, 0, 0));
                    Shape.Part("Scoop", r.root, MeshFactory.LowPolyBlob(0.19f, 11, 0.06f, 0.95f), Mats.Lit(Palette.Hex("#FF8FB1"), 0.5f), top + Vector3.up * 0.47f);
                    Shape.Part("Scoop2", r.root, MeshFactory.LowPolyBlob(0.15f, 12, 0.06f, 0.95f), Mats.Lit(Palette.Hex("#FFF5E1"), 0.5f), top + Vector3.up * 0.62f);
                    Shape.Part("Cherry", r.root, MeshFactory.LowPolyBlob(0.05f, 13, 0f, 1f), Mats.Lit(Palette.AwningRed, 0.7f), top + Vector3.up * 0.75f);
                    break;
                }
                case CarModel.Ambulance:
                {
                    Car(r, L, white, Color.white, 2);
                    var d = DimsFor(2, L);
                    var red = Mats.Lit(Palette.Hex("#E63946"), 0.6f);
                    SideStripe(r.root, L, d.bodyY, 0.08f, red, 0.92f);
                    var roof = new Vector3(0, d.roofTop + 0.02f, d.cabZ - d.cabinLen * 0.15f);
                    Shape.Box("CrossA", r.root, new Vector3(0.3f, 0.02f, 0.09f), 0.005f, red, roof, null, false);
                    Shape.Box("CrossB", r.root, new Vector3(0.09f, 0.02f, 0.3f), 0.005f, red, roof, null, false);
                    Beacon(r.root, new Vector3(0, d.roofTop + 0.01f, d.cabZ + d.cabinLen * 0.32f), Palette.Hex("#FF3B30"), Palette.Hex("#FF3B30"));
                    break;
                }
                case CarModel.Police:
                {
                    Car(r, L, white, Color.white, 0);
                    var d = DimsFor(0, L);
                    var blue = Mats.Lit(Palette.Hex("#1F3A93"), 0.8f, 0.2f);
                    SideStripe(r.root, L, d.bodyY - 0.01f, 0.13f, blue, 0.6f);
                    Shape.Box("Hood", r.root, new Vector3(Width * 0.7f, 0.02f, L * 0.2f), 0.01f, blue, new Vector3(0, d.bodyTop - 0.004f, L * 0.36f), null, false);
                    Beacon(r.root, new Vector3(0, d.roofTop + 0.01f, d.cabZ), Palette.Hex("#FF3B30"), Palette.Hex("#2F6BFF"));
                    break;
                }
                case CarModel.FireTruck:
                {
                    var c = Palette.Hex("#E63946");
                    Car(r, L, Mats.Lit(c, 0.85f, 0.15f), c, 2);
                    var d = DimsFor(2, L);
                    SideStripe(r.root, L, d.bodyY, 0.05f, white, 0.9f);
                    var metal = Mats.Lit(Palette.Hex("#C9CED6"), 0.7f, 0.6f);
                    float y = d.roofTop + 0.05f;
                    foreach (float sx in new[] { -1f, 1f })
                        Shape.Box("Rail", r.root, new Vector3(0.03f, 0.03f, L * 0.85f), 0.01f, metal, new Vector3(sx * 0.15f, y, -L * 0.04f), null, false);
                    for (int i = 0; i < 7; i++)
                        Shape.Box("Rung", r.root, new Vector3(0.3f, 0.02f, 0.02f), 0.006f, metal, new Vector3(0, y, -L * 0.42f + i * L * 0.125f), null, false);
                    Beacon(r.root, new Vector3(0, d.roofTop + 0.01f, d.cabZ + d.cabinLen * 0.4f), Palette.Hex("#FF3B30"), Palette.Hex("#FFB000"));
                    break;
                }
                case CarModel.Racer:
                {
                    var c = Palette.Hex("#FF7A00");
                    Car(r, L, Mats.Lit(c, 0.92f, 0.25f), c, 3);
                    var d = DimsFor(3, L);
                    foreach (float sx in new[] { -1f, 1f })
                        Shape.Box("RaceStripe", r.root, new Vector3(0.07f, 0.02f, L * 0.92f), 0.008f, black, new Vector3(sx * 0.07f, d.bodyTop - 0.002f, 0), null, false);
                    // Rüzgârlık
                    foreach (float sx in new[] { -1f, 1f })
                        Shape.Box("Post", r.root, new Vector3(0.03f, 0.12f, 0.04f), 0.01f, black, new Vector3(sx * 0.22f, d.bodyTop + 0.05f, -L / 2f + 0.1f), null, false);
                    Shape.Box("Wing", r.root, new Vector3(Width * 0.92f, 0.025f, 0.14f), 0.01f, black, new Vector3(0, d.bodyTop + 0.12f, -L / 2f + 0.1f));
                    // Yarış numarası yuvarlakları
                    foreach (float sx in new[] { -1f, 1f })
                        Shape.Part("Number", r.root, MeshFactory.Cylinder(0.1f, 0.02f, 16), white,
                            new Vector3(sx * (Width / 2f + 0.006f), d.bodyY + 0.01f, 0.05f), Quaternion.Euler(0, 0, 90), false);
                    break;
                }
            }
        }

        /// <summary>Lot içindeki sabit engel: trafik konisi.</summary>
        public static void TrafficCone(Transform parent, Vector3 pos)
        {
            var orange = Mats.Lit(Palette.Hex("#FF7A1A"), 0.55f);
            var white = Mats.Lit(Color.white, 0.6f);
            var root = new GameObject("Cone").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            Shape.Part("Shadow", root, MeshFactory.Quad(0.85f, 0.85f), Mats.Blob(), new Vector3(0, 0.01f, 0), null, false);
            Shape.Box("Base", root, new Vector3(0.48f, 0.06f, 0.48f), 0.03f, orange, new Vector3(0, 0.03f, 0));
            Shape.Part("Body", root, MeshFactory.Cone(0.2f, 0.62f, 12), orange, new Vector3(0, 0.05f, 0));
            Shape.Part("Band", root, MeshFactory.Cylinder(0.125f, 0.08f, 12), white, new Vector3(0, 0.33f, 0), null, false);
        }
    }
}
