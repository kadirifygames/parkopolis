using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>Özel araba modelleri. None = normal rastgele araba.</summary>
    public enum CarModel { None, Taxi, Classic, Dolmus, IceCream, Ambulance, Police, FireTruck, Racer }

    public class ModelItem
    {
        public CarModel model;
        public string name;
        public int price;
        public int speedClass;   // hangi normal arabanın yerine geçer (0 sedan, 1 hatch, 2 minibüs, 3 spor)
    }

    public class PaintItem
    {
        public string id, name;
        public int price;
        public float smoothness = 0.86f, metallic = 0.12f;
        public Color[] colors;
    }

    /// <summary>
    /// Garaj: satın alınan özel arabalar ve seçilen boya paleti.
    /// Sahiplik PlayerPrefs'te "g_own_..." anahtarlarıyla tutulur.
    /// </summary>
    public static class Garage
    {
        static Color H(string h) => Palette.Hex(h);

        public static readonly ModelItem[] Models =
        {
            new ModelItem { model = CarModel.Taxi, name = "TAKSİ", price = 300, speedClass = 0 },
            new ModelItem { model = CarModel.Classic, name = "KLASİK", price = 400, speedClass = 1 },
            new ModelItem { model = CarModel.Dolmus, name = "DOLMUŞ", price = 500, speedClass = 2 },
            new ModelItem { model = CarModel.IceCream, name = "DONDURMACI", price = 600, speedClass = 2 },
            new ModelItem { model = CarModel.Ambulance, name = "AMBULANS", price = 750, speedClass = 2 },
            new ModelItem { model = CarModel.Police, name = "POLİS", price = 900, speedClass = 3 },
            new ModelItem { model = CarModel.FireTruck, name = "İTFAİYE", price = 1000, speedClass = 2 },
            new ModelItem { model = CarModel.Racer, name = "YARIŞ", price = 1200, speedClass = 3 },
        };

        public static readonly PaintItem[] Paints =
        {
            new PaintItem { id = "classic", name = "KLASİK", price = 0, colors = Palette.Cars },
            new PaintItem { id = "pastel", name = "PASTEL", price = 300, smoothness = 0.7f, metallic = 0f, colors = new[]
            {
                H("#FFADAD"), H("#FFD6A5"), H("#FDFFB6"), H("#CAFFBF"), H("#9BF6FF"),
                H("#A0C4FF"), H("#BDB2FF"), H("#FFC6FF"), H("#FFFFFC"), H("#F7C59F")
            }},
            new PaintItem { id = "neon", name = "NEON", price = 500, smoothness = 0.9f, metallic = 0.05f, colors = new[]
            {
                H("#FF1E56"), H("#FF9F00"), H("#F5FF00"), H("#00FF9C"), H("#00E5FF"),
                H("#2979FF"), H("#B000FF"), H("#FF00C8"), H("#F2F2F2"), H("#FF5E00")
            }},
            new PaintItem { id = "metal", name = "METALİK", price = 800, smoothness = 0.93f, metallic = 0.65f, colors = new[]
            {
                H("#C0392B"), H("#D68910"), H("#D4AC0D"), H("#1E8449"), H("#148F77"),
                H("#2E86C1"), H("#7D3C98"), H("#C2185B"), H("#D5D8DC"), H("#566573")
            }},
            new PaintItem { id = "night", name = "GECE", price = 600, smoothness = 0.9f, metallic = 0.3f, colors = new[]
            {
                H("#7A1F2B"), H("#8A4B12"), H("#8C7A12"), H("#1F5E3B"), H("#145A5A"),
                H("#1F3A6B"), H("#3F2A6B"), H("#6B1F4E"), H("#2B2F36"), H("#5A2E1A")
            }},
        };

        // ---------------- Sahiplik ----------------

        public static bool Owns(CarModel m) => PlayerPrefs.GetInt("g_own_" + m, 0) == 1;
        public static bool OwnsPaint(string id) => id == "classic" || PlayerPrefs.GetInt("g_paint_" + id, 0) == 1;

        public static bool Buy(ModelItem item)
        {
            if (Owns(item.model) || SaveData.Coins < item.price) return false;
            SaveData.Coins -= item.price;
            PlayerPrefs.SetInt("g_own_" + item.model, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static bool BuyPaint(PaintItem p)
        {
            if (OwnsPaint(p.id) || SaveData.Coins < p.price) return false;
            SaveData.Coins -= p.price;
            PlayerPrefs.SetInt("g_paint_" + p.id, 1);
            SelectedPaintId = p.id;
            return true;
        }

        public static string SelectedPaintId
        {
            get => PlayerPrefs.GetString("g_paint_sel", "classic");
            set { PlayerPrefs.SetString("g_paint_sel", value); PlayerPrefs.Save(); }
        }

        public static PaintItem SelectedPaint
        {
            get
            {
                string id = SelectedPaintId;
                foreach (var p in Paints) if (p.id == id && OwnsPaint(id)) return p;
                return Paints[0];
            }
        }

        // ---------------- Levelde kullanım ----------------

        /// <summary>Seçili paletten arabanın rengi.</summary>
        public static Color CarColor(int colorIndex)
        {
            var c = SelectedPaint.colors;
            return c[Mathf.Abs(colorIndex) % c.Length];
        }

        /// <summary>
        /// Bu park yerindeki araba özel bir modele dönüşsün mü? Sadece satın alınmış ve aynı hız sınıfındaki
        /// modellerden seçilir; aynı level her açılışta aynı görünür (konuma göre sabit "rastgelelik").
        /// </summary>
        public static CarModel ModelFor(CarSpec spec, int level)
        {
            if (spec.length >= 3) return CarModel.None;
            int cls = spec.variant % 4;
            int count = 0;
            foreach (var m in Models) if (m.speedClass == cls && Owns(m.model)) count++;
            if (count == 0) return CarModel.None;

            int hash = (level * 73856093) ^ (spec.x * 19349663) ^ (spec.y * 83492791);
            var rng = new System.Random(hash);
            if (rng.NextDouble() > 0.35) return CarModel.None; // her 3 arabadan ~1'i
            int pick = rng.Next(count);
            foreach (var m in Models)
            {
                if (m.speedClass != cls || !Owns(m.model)) continue;
                if (pick-- == 0) return m.model;
            }
            return CarModel.None;
        }
    }
}
