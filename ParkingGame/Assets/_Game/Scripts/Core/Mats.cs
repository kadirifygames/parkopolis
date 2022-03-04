using System.Collections.Generic;
using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Renge göre malzeme üretir ve önbellekte tutar.
    /// Aynı renk için tek malzeme kullanmak, telefonda çizimi hızlandırır.
    /// </summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Texture2D blobTexture, softTexture, squareTexture;

        static Material Template(string kind)
        {
            var a = GameAssets.I;
            if (a != null)
            {
                Material m = kind switch
                {
                    "lit" => a.lit,
                    "emissive" => a.emissive,
                    "unlit" => a.unlit,
                    "transparent" => a.transparent,
                    "particles" => a.particles,
                    _ => null
                };
                if (m != null) return m;
            }

            // Kurulum henüz yapılmadıysa yedek yol
            string shader = kind switch
            {
                "unlit" => "Universal Render Pipeline/Unlit",
                "transparent" => "Universal Render Pipeline/Unlit",
                "particles" => "Universal Render Pipeline/Particles/Unlit",
                _ => "Universal Render Pipeline/Lit"
            };
            return new Material(Shader.Find(shader));
        }

        static bool TryGet(string key, out Material m) => cache.TryGetValue(key, out m) && m != null;

        /// <summary>Işık alan, gölge düşen normal yüzey.</summary>
        public static Material Lit(Color c, float smoothness = 0.25f, float metallic = 0f)
        {
            string key = $"lit{c}{smoothness}{metallic}";
            if (TryGet(key, out var m)) return m;
            m = new Material(Template("lit")) { name = "Lit_" + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            cache[key] = m;
            return m;
        }

        /// <summary>Parlayan yüzey (far, lamba). Bloom efektiyle hafif ışıma yapar.</summary>
        public static Material Emissive(Color c, float intensity)
        {
            string key = $"emi{c}{intensity}";
            if (TryGet(key, out var m)) return m;
            m = new Material(Template("emissive")) { name = "Emissive_" + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor("_BaseColor", c);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            cache[key] = m;
            return m;
        }

        /// <summary>
        /// Dokulu yüzey: renk + hafif desen (asfalt pürüzü, çim, kaldırım taşı).
        /// tiling = dokunun 1 metrede kaç kez tekrarlandığı.
        /// </summary>
        public static Material LitTex(Color c, Texture2D tex, float tiling, float smoothness = 0.15f)
        {
            string key = $"littex{c}{tex.name}{tiling}{smoothness}";
            if (TryGet(key, out var m)) return m;
            m = new Material(Template("lit")) { name = "LitTex_" + tex.name };
            m.SetColor("_BaseColor", c);
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", 0f);
            cache[key] = m;
            return m;
        }

        public static Material Unlit(Color c)
        {
            string key = $"unlit{c}";
            if (TryGet(key, out var m)) return m;
            m = new Material(Template("unlit"));
            m.SetColor("_BaseColor", c);
            cache[key] = m;
            return m;
        }

        /// <summary>Arabaların altındaki yumuşak gölge lekesi. Arabaları zemine "oturtur".</summary>
        public static Material Blob()
        {
            if (TryGet("blob", out var m)) return m;
            m = new Material(Template("transparent")) { name = "BlobShadow" };
            m.SetTexture("_BaseMap", BlobTexture);
            m.SetColor("_BaseColor", new Color(0.05f, 0.07f, 0.12f, 0.45f));
            cache["blob"] = m;
            return m;
        }

        /// <summary>Zemine düşen renkli ışık halkası (gece lambaları).</summary>
        public static Material Glow(Color c, float alpha)
        {
            string key = $"glow{c}{alpha}";
            if (TryGet(key, out var m)) return m;
            m = new Material(Template("transparent")) { name = "Glow" };
            m.SetTexture("_BaseMap", SoftTexture);
            m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, alpha));
            cache[key] = m;
            return m;
        }

        public static Material Particle(bool square)
        {
            string key = square ? "partSq" : "partSoft";
            if (TryGet(key, out var m)) return m;
            m = new Material(Template("particles")) { name = key };
            m.SetTexture("_BaseMap", square ? SquareTexture : SoftTexture);
            m.SetColor("_BaseColor", Color.white);
            cache[key] = m;
            return m;
        }

        // --- Kodla üretilen küçük dokular (texture) ---

        public static Texture2D BlobTexture => blobTexture != null ? blobTexture : (blobTexture = Radial(64, 2.2f, 0.2f));
        public static Texture2D SoftTexture => softTexture != null ? softTexture : (softTexture = Radial(64, 1.6f, 0f));

        public static Texture2D SquareTexture
        {
            get
            {
                if (squareTexture != null) return squareTexture;
                squareTexture = new Texture2D(8, 8, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[64];
                for (int i = 0; i < 64; i++) px[i] = new Color32(255, 255, 255, 255);
                squareTexture.SetPixels32(px);
                squareTexture.Apply();
                return squareTexture;
            }
        }

        // --- Zemin dokuları: tekrarlandığında dikiş belli olmayan (tileable) desenler ---

        static Texture2D asphalt, grass, paver;

        public static Texture2D Asphalt => asphalt != null ? asphalt : (asphalt = Noise("Asphalt", 256, 8, 0.07f, 0.09f, 11, 0));
        public static Texture2D GrassTex => grass != null ? grass : (grass = Noise("Grass", 256, 4, 0.12f, 0.05f, 23, 0));
        public static Texture2D Paver => paver != null ? paver : (paver = Noise("Paver", 128, 4, 0.04f, 0.04f, 37, 4));

        /// <summary>
        /// Değer gürültüsü (value noise) + ince benekler. grout > 0 ise kenarlara taş derzi çizer.
        /// </summary>
        static Texture2D Noise(string name, int size, int cells, float blotch, float speckle, int seed, int grout)
        {
            var rng = new System.Random(seed);
            var grid = new float[cells, cells];
            for (int y = 0; y < cells; y++)
            for (int x = 0; x < cells; x++) grid[x, y] = (float)rng.NextDouble();

            float Sample(float u, float v)
            {
                float fx = u * cells, fy = v * cells;
                int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
                float tx = fx - x0, ty = fy - y0;
                tx = tx * tx * (3 - 2 * tx);
                ty = ty * ty * (3 - 2 * ty);
                int x1 = (x0 + 1) % cells, y1 = (y0 + 1) % cells;
                x0 %= cells; y0 %= cells;
                float a = Mathf.Lerp(grid[x0, y0], grid[x1, y0], tx);
                float b = Mathf.Lerp(grid[x0, y1], grid[x1, y1], tx);
                return Mathf.Lerp(a, b, ty);
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float n = Sample(u, v) * 0.65f + Sample(u * 2f % 1f, v * 2f % 1f) * 0.35f;
                float value = 1f - blotch + n * blotch;
                value -= (float)rng.NextDouble() * speckle;
                if (grout > 0 && (x < grout / 2 || x >= size - grout / 2 || y < grout / 2 || y >= size - grout / 2))
                    value *= 0.86f;
                byte b8 = (byte)(Mathf.Clamp01(value) * 255);
                px[y * size + x] = new Color32(b8, b8, b8, 255);
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        /// <summary>
        /// Gökyüzü yansıması: parlak yüzeyler (araba boyası, camlar) bu küpü yansıtır.
        /// Üstte açık mavi, ufukta sıcak beyaz, altta yeşilimsi gri.
        /// </summary>
        static readonly Dictionary<string, Cubemap> skies = new Dictionary<string, Cubemap>();

        public static Cubemap SkyCubemap() => SkyCubemap(Palette.Hex("#8EC5FF"), Palette.Hex("#FFF4E6"), Palette.Hex("#7E8E78"));

        public static Cubemap SkyCubemap(Color zenith, Color horizon, Color ground)
        {
            string key = $"{zenith}{horizon}{ground}";
            if (skies.TryGetValue(key, out var cached) && cached != null) return cached;
            const int size = 64;
            var cube = new Cubemap(size, TextureFormat.RGBA32, true) { name = "SkyReflection" };
            skies[key] = cube;
            foreach (CubemapFace face in System.Enum.GetValues(typeof(CubemapFace)))
            {
                if (face == CubemapFace.Unknown) continue;
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                    Vector3 d = face switch
                    {
                        CubemapFace.PositiveX => new Vector3(1, -v, -u),
                        CubemapFace.NegativeX => new Vector3(-1, -v, u),
                        CubemapFace.PositiveY => new Vector3(u, 1, v),
                        CubemapFace.NegativeY => new Vector3(u, -1, -v),
                        CubemapFace.PositiveZ => new Vector3(u, -v, 1),
                        _ => new Vector3(-u, -v, -1)
                    };
                    float h = d.normalized.y;
                    Color c = h >= 0 ? Color.Lerp(horizon, zenith, Mathf.Pow(h, 0.6f)) : Color.Lerp(horizon, ground, Mathf.Pow(-h, 0.4f));
                    px[y * size + x] = c;
                }
                cube.SetPixels(px, face);
            }
            cube.Apply(true);
            return cube;
        }

        /// <summary>Ortası dolu, kenara doğru solan yuvarlak doku.</summary>
        static Texture2D Radial(int size, float power, float core)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = d <= core ? 1f : Mathf.Pow(Mathf.Clamp01(1f - (d - core) / (1f - core)), power);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
