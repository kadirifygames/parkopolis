using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Oyunun tüm renkleri tek yerde. Bir rengi değiştirmek istersen sadece burayı düzenle.
    /// </summary>
    public static class Palette
    {
        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // Zemin ve yollar
        public static readonly Color Grass = Hex("#9CCF6E");
        public static readonly Color GrassDark = Hex("#7CC25A");
        public static readonly Color Sidewalk = Hex("#EFEAE0");
        public static readonly Color Road = Hex("#4C5466");
        public static readonly Color Lot = Hex("#646E82");
        public static readonly Color Line = Hex("#F7F4EA");
        public static readonly Color LineSoft = Hex("#7F889B");
        public static readonly Color LineYellow = Hex("#FFC94A");

        // Doğa
        public static readonly Color Trunk = Hex("#8B5E3C");
        public static readonly Color[] Leaves = { Hex("#5DBB63"), Hex("#4CAF50"), Hex("#7DCC5C"), Hex("#3F9F5B") };
        public static readonly Color Pine = Hex("#2F8F5B");
        public static readonly Color[] Flowers = { Hex("#FF8FB1"), Hex("#FFE066"), Hex("#FFFFFF"), Hex("#C3A6FF") };

        // Araba parçaları
        public static readonly Color Glass = Hex("#2C4263");
        public static readonly Color Tire = Hex("#262A33");
        public static readonly Color Hub = Hex("#E3E7EE");
        public static readonly Color Trim = Hex("#3A404D");
        public static readonly Color HeadLight = Hex("#FFF4D2");
        public static readonly Color TailLight = Hex("#FF3B3B");

        public static readonly Color[] Cars =
        {
            Hex("#FF5A5F"), // kırmızı
            Hex("#FF9F1C"), // turuncu
            Hex("#FFD23F"), // sarı
            Hex("#3DDC97"), // nane yeşili
            Hex("#2EC4B6"), // turkuaz
            Hex("#4D96FF"), // mavi
            Hex("#9B5DE5"), // mor
            Hex("#F15BB5"), // pembe
            Hex("#F4F4F0"), // beyaz
            Hex("#FF7B54"), // mercan
        };

        // Binalar
        public static readonly Color[] Walls = { Hex("#F6E7C8"), Hex("#A8D8F0"), Hex("#F7C1AE"), Hex("#CDBEEA"), Hex("#BFE3C0") };
        public static readonly Color Roof = Hex("#E76F51");
        public static readonly Color RoofDark = Hex("#5C677D");
        public static readonly Color Window = Hex("#8EC9EE");
        public static readonly Color Frame = Hex("#FFFFFF");
        public static readonly Color AwningRed = Hex("#E63946");
        public static readonly Color Metal = Hex("#4A5160");

        // Arayüz
        public static readonly Color UiDark = Hex("#1E293B");
        public static readonly Color UiGreen = Hex("#34C759");
        public static readonly Color UiGreenEdge = Hex("#23A047");
        public static readonly Color UiBlue = Hex("#3A86FF");
        public static readonly Color UiBlueEdge = Hex("#2563EB");
        public static readonly Color UiGold = Hex("#FFC93C");
        public static readonly Color UiGoldDark = Hex("#F59E0B");
        public static readonly Color UiWhiteEdge = Hex("#C9D3E0");
        public static readonly Color UiGray = Hex("#94A3B8");
        public static readonly Color UiGrayEdge = Hex("#64748B");
    }
}
