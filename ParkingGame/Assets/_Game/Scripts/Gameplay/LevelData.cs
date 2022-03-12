using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>Tek bir arabanın level içindeki tanımı.</summary>
    [Serializable]
    public struct CarSpec
    {
        public int x, y;            // sol-alt hücre
        public int length;          // kaç hücre kaplıyor (2 = araba, 3 = otobüs/kamyon)
        public bool horizontal;     // yatay mı dikey mi
        public bool facePositive;   // önü +X/+Z yönüne mi bakıyor
        public int colorIndex;
        public int variant;

        public Vector2Int Cell(int i) => horizontal ? new Vector2Int(x + i, y) : new Vector2Int(x, y + i);
        public int ForwardSign => facePositive ? 1 : -1;
    }

    /// <summary>Zorluk dalgası: her şehrin 5 levelinde kolay → normal → zor → rahat → boss.</summary>
    public enum Difficulty { Normal, Easy, Hard, Relax, Boss }

    /// <summary>Bir levelin tamamı: ızgara boyutu, arabalar ve engeller.</summary>
    [Serializable]
    public class LevelLayout
    {
        public int width, height;
        public List<CarSpec> cars = new List<CarSpec>();
        public List<Vector2Int> obstacles = new List<Vector2Int>();

        // Mekanikler
        public List<Vector2Int> ice = new List<Vector2Int>(); // buzlu kareler (Erzurum)
        public bool colorGates;                                  // renkli kapı (Gece)
        public bool boss;                                        // boss level: büyük otopark + süre
        public float timeLimit;                                  // saniye (sadece boss)
        public Difficulty difficulty;

        public bool IsIce(Vector2Int c) => ice.Contains(c);
    }

    /// <summary>
    /// Izgara işlemleri. Her hücrede: -1 = boş, -2 = engel, 0+ = o numaralı araba.
    /// </summary>
    public static class Grid
    {
        public const int Empty = -1;
        public const int Obstacle = -2;

        public struct Probe
        {
            public bool exits;   // yol açık, araba çıkabilir
            public int blocker;  // önünü kapatan şey (araba numarası veya Obstacle)
            public int steps;    // engele kadar kaç boş hücre var
        }

        public static int[,] Build(LevelLayout l, bool[] removed = null)
        {
            var g = new int[l.width, l.height];
            for (int x = 0; x < l.width; x++)
            for (int y = 0; y < l.height; y++) g[x, y] = Empty;
            foreach (var o in l.obstacles) g[o.x, o.y] = Obstacle;
            for (int i = 0; i < l.cars.Count; i++)
            {
                if (removed != null && removed[i]) continue;
                Mark(g, l.cars[i], i);
            }
            return g;
        }

        public static void Mark(int[,] g, CarSpec s, int value)
        {
            for (int i = 0; i < s.length; i++)
            {
                var c = s.Cell(i);
                g[c.x, c.y] = value;
            }
        }

        public static bool IsFree(int[,] g, CarSpec s)
        {
            int w = g.GetLength(0), h = g.GetLength(1);
            for (int i = 0; i < s.length; i++)
            {
                var c = s.Cell(i);
                if (c.x < 0 || c.y < 0 || c.x >= w || c.y >= h || g[c.x, c.y] != Empty) return false;
            }
            return true;
        }

        /// <summary>Araba "sign" yönünde hareket ederse ne olur?</summary>
        public static Probe Look(int[,] g, CarSpec s, int sign)
        {
            int w = g.GetLength(0), h = g.GetLength(1);
            Vector2Int step = s.horizontal ? new Vector2Int(sign, 0) : new Vector2Int(0, sign);
            Vector2Int c = sign > 0 ? s.Cell(s.length - 1) + step : s.Cell(0) + step;
            int steps = 0;
            while (c.x >= 0 && c.y >= 0 && c.x < w && c.y < h)
            {
                if (g[c.x, c.y] != Empty) return new Probe { exits = false, blocker = g[c.x, c.y], steps = steps };
                steps++;
                c += step;
            }
            return new Probe { exits = true, blocker = Empty, steps = steps };
        }

        /// <summary>Araba ileri giderek otoparktan çıkabilir mi? (Geri gitmek yok.)</summary>
        public static bool CanExit(int[,] g, CarSpec s) => Look(g, s, s.ForwardSign).exits;
    }

    /// <summary>
    /// Çözücü: Levelin çözülüp çözülemeyeceğini ve kaç "tur" sürdüğünü bulur.
    /// Her turda çıkabilen tüm arabalar çıkarılır. Hiç kimse çıkamıyorsa level çözülemez.
    /// Tur sayısı ne kadar fazlaysa level o kadar zordur.
    /// </summary>
    public static class Solver
    {
        public static int Rounds(LevelLayout l)
        {
            int n = l.cars.Count;
            var removed = new bool[n];
            var g = Grid.Build(l);
            int remaining = n, rounds = 0;
            var exitNow = new List<int>();
            while (remaining > 0)
            {
                exitNow.Clear();
                for (int i = 0; i < n; i++)
                    if (!removed[i] && Grid.CanExit(g, l.cars[i])) exitNow.Add(i);
                if (exitNow.Count == 0) return -1;
                foreach (int i in exitNow)
                {
                    removed[i] = true;
                    Grid.Mark(g, l.cars[i], Grid.Empty);
                    remaining--;
                }
                rounds++;
            }
            return rounds;
        }
    }
}
