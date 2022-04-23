using UnityEngine;

namespace Kadirify.Parkopolis
{
    public enum TreeStyle { City, Palm, Poplar, SnowPine, Forest }
    public enum SkylineStyle { Istanbul, Coast, Cappadocia, Mountains, Hills }
    public enum Weather { None, Snow, Rain, Seagulls }
    public enum Mechanic { None, Ice, ColorGate }

    /// <summary>
    /// Bir şehrin (bölgenin) görsel kimliği: zemin renkleri, ağaç tipi, binalar, ışık ve arka plan manzarası.
    /// Yeni bir şehir eklemek için All listesine bir tane daha eklemen yeterli.
    /// </summary>
    public class Theme
    {
        public const int LevelsPerRegion = 5;

        public string Name;
        public string Tagline;                         // haritada görünen kısa tanıtım
        public Color Accent = Palette.Hex("#3A86FF");  // haritadaki simge rengi
        public Color Grass, Road, Lot, Sidewalk;
        public Color[] Leaves;
        public Color[] Walls;
        public Color Roof;
        public TreeStyle Trees;
        public SkylineStyle Skyline;
        public bool Night;
        public Weather Weather;
        public Mechanic Mechanic;   // şehre özel oyun kuralı

        // Işık
        public Color SunColor = Palette.Hex("#FFEFD6");
        public float SunIntensity = 1.4f;
        public Vector3 SunEuler = new Vector3(46f, -32f, 0f);
        public Color AmbSky = Palette.Hex("#C9DEFF"), AmbEquator = Palette.Hex("#E9E1CF"), AmbGround = Palette.Hex("#8FA77A");
        public Color SkyZenith = Palette.Hex("#8EC5FF"), SkyHorizon = Palette.Hex("#FFF4E6");
        public Color Filter = Palette.Hex("#FFF9F2");
        public float Exposure = 0.12f;

        public static int RegionIndex(int level) => (Mathf.Max(1, level) - 1) / LevelsPerRegion % All.Length;
        public static Theme For(int level) => All[RegionIndex(level)];
        public static bool IsRegionStart(int level) => level > 1 && (level - 1) % LevelsPerRegion == 0;

        static Color H(string hex) => Palette.Hex(hex);

        public static readonly Theme[] All =
        {
            new Theme
            {
                Name = "İSTANBUL", Tagline = "Boğaz manzarası", Accent = H("#E76F51"),
                Grass = H("#9CCF6E"), Road = H("#4C5466"), Lot = H("#646E82"), Sidewalk = H("#EFEAE0"),
                Leaves = new[] { H("#5DBB63"), H("#4CAF50"), H("#7DCC5C"), H("#3F9F5B") },
                Walls = new[] { H("#F6E7C8"), H("#A8D8F0"), H("#F7C1AE"), H("#CDBEEA"), H("#BFE3C0") },
                Roof = H("#E76F51"),
                Trees = TreeStyle.City, Skyline = SkylineStyle.Istanbul,
            },
            new Theme
            {
                Name = "ANTALYA", Tagline = "Deniz, kum, güneş", Accent = H("#1FB5C9"),
                Grass = H("#F1DDAE"), Road = H("#57606F"), Lot = H("#6E7787"), Sidewalk = H("#FBF5EA"),
                Leaves = new[] { H("#3FAE6A"), H("#52C27A"), H("#2E9C5E") },
                Walls = new[] { H("#FFFFFF"), H("#FFE3B8"), H("#BFE9F2"), H("#FFD0C2") },
                Roof = H("#E0794F"),
                Trees = TreeStyle.Palm, Skyline = SkylineStyle.Coast, Weather = Weather.Seagulls,
                SunColor = H("#FFF3DE"), SunIntensity = 1.5f,
                AmbSky = H("#CDE7FF"), SkyZenith = H("#7CC6FF"),
                Exposure = 0.18f,
            },
            new Theme
            {
                Name = "KAPADOKYA", Tagline = "Peri bacaları ve balonlar", Accent = H("#F4A261"),
                Grass = H("#E2C59A"), Road = H("#5C5A63"), Lot = H("#716F7A"), Sidewalk = H("#F3E6D2"),
                Leaves = new[] { H("#8DAA55"), H("#9DB862"), H("#7E9C4B") },
                Walls = new[] { H("#E8C9A0"), H("#DDB386"), H("#F0D9B5") },
                Roof = H("#B86B4B"),
                Trees = TreeStyle.Poplar, Skyline = SkylineStyle.Cappadocia,
                SunColor = H("#FFE2BD"), SunIntensity = 1.45f, SunEuler = new Vector3(38f, -50f, 0f),
                AmbSky = H("#FFE1C7"), AmbEquator = H("#F2D6B8"), AmbGround = H("#C9A57E"),
                SkyZenith = H("#9CC8F0"), SkyHorizon = H("#FFDDB8"),
                Filter = H("#FFF1E2"),
            },
            new Theme
            {
                Name = "ERZURUM", Tagline = "Karlı dağlar", Accent = H("#4D96FF"),
                Grass = H("#EEF3F8"), Road = H("#4F5868"), Lot = H("#6A7488"), Sidewalk = H("#E3E9F0"),
                Leaves = new[] { H("#2F7D5B"), H("#2A6E52") },
                Walls = new[] { H("#C9A27E"), H("#E8D8C4"), H("#B6C7D9"), H("#D9B7A0") },
                Roof = H("#F7FAFC"),
                Trees = TreeStyle.SnowPine, Skyline = SkylineStyle.Mountains, Weather = Weather.Snow, Mechanic = Mechanic.Ice,
                SunColor = H("#F2F6FF"), SunIntensity = 1.25f, SunEuler = new Vector3(40f, -25f, 0f),
                AmbSky = H("#D8E6FF"), AmbEquator = H("#E4EBF5"), AmbGround = H("#C8D3E0"),
                SkyZenith = H("#A9CBF2"), SkyHorizon = H("#F2F6FC"),
                Filter = H("#F3F8FF"), Exposure = 0.05f,
            },
            new Theme
            {
                Name = "KARADENİZ", Tagline = "Yeşil yaylalar", Accent = H("#3DA35D"),
                Grass = H("#79B85C"), Road = H("#4A5262"), Lot = H("#626C7F"), Sidewalk = H("#E6E8DE"),
                Leaves = new[] { H("#3E8E4E"), H("#2F7A44"), H("#4FA35A"), H("#2A6B3E") },
                Walls = new[] { H("#FFFFFF"), H("#F4E3C1"), H("#D7E7D2"), H("#E9D3C0") },
                Roof = H("#C2483B"),
                Trees = TreeStyle.Forest, Skyline = SkylineStyle.Hills, Weather = Weather.Rain,
                SunColor = H("#FFF6E8"), SunIntensity = 1.3f,
                AmbSky = H("#D5E6E3"), AmbEquator = H("#E1E6D6"), AmbGround = H("#7F9B6E"),
                SkyHorizon = H("#EEF3EE"), Filter = H("#F7FBF5"),
            },
            new Theme
            {
                Name = "İSTANBUL GECE", Tagline = "Işıl ışıl gece", Accent = H("#6C5CE7"),
                Grass = H("#4E7A55"), Road = H("#353B4A"), Lot = H("#454D60"), Sidewalk = H("#9AA0AE"),
                Leaves = new[] { H("#2F6B4A"), H("#295E41"), H("#3A7A52") },
                Walls = new[] { H("#7D86A8"), H("#6F7898"), H("#8A7FA0"), H("#6E8A9C") },
                Roof = H("#4B5068"),
                Trees = TreeStyle.City, Skyline = SkylineStyle.Istanbul, Night = true, Mechanic = Mechanic.ColorGate,
                SunColor = H("#9DB4FF"), SunIntensity = 0.55f, SunEuler = new Vector3(55f, 30f, 0f),
                AmbSky = H("#3C4D86"), AmbEquator = H("#3A3F66"), AmbGround = H("#242A40"),
                SkyZenith = H("#1C2550"), SkyHorizon = H("#5A4A80"),
                Filter = H("#E9ECFF"), Exposure = 0.35f,
            },
        };
    }
}
