using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Level üretici. Aynı level numarası her zaman aynı leveli üretir (seed sayesinde),
    /// böylece her oyuncu aynı Level 12'yi oynar.
    /// </summary>
    public static class LevelGenerator
    {
        /// <param name="salt">Aynı level numarasından farklı bir otopark üretmek için (Günün Otoparkı).</param>
        public static LevelLayout Create(int level, int salt = 0)
        {
            if (level <= 1) return Tutorial();

            int w = Mathf.Clamp(4 + (level + 1) / 4, 4, 7);
            int h = Mathf.Clamp(5 + level / 3, 5, 9);
            float fill = Mathf.Lerp(0.5f, 0.82f, Mathf.Clamp01((level - 2) / 24f));
            int targetRounds = Mathf.Clamp(2 + level / 4, 2, 8);

            // Boss level: her şehrin 5. leveli, daha büyük ve dolu otopark
            bool boss = level >= Theme.LevelsPerRegion && level % Theme.LevelsPerRegion == 0;
            if (boss)
            {
                w = Mathf.Min(w + 1, 8);
                h = Mathf.Min(h + 1, 10);
                fill = Mathf.Min(fill + 0.05f, 0.85f);
                targetRounds += 1;
            }

            // Zorluk dalgası (ilk şehirden sonra): kolay → normal → ZOR → rahat → boss
            var difficulty = boss ? Difficulty.Boss : Difficulty.Normal;
            if (!boss && level > Theme.LevelsPerRegion)
            {
                switch ((level - 1) % Theme.LevelsPerRegion)
                {
                    case 0: difficulty = Difficulty.Easy; fill -= 0.1f; targetRounds -= 1; break;
                    case 2: difficulty = Difficulty.Hard; fill += 0.05f; targetRounds += 1; break;
                    case 3: difficulty = Difficulty.Relax; fill -= 0.15f; targetRounds -= 2; break;
                }
                fill = Mathf.Clamp(fill, 0.42f, 0.86f);
                targetRounds = Mathf.Max(2, targetRounds);
            }

            LevelLayout best = null;
            int bestScore = int.MinValue;
            for (int attempt = 0; attempt < 14; attempt++)
            {
                var rng = new System.Random(level * 7919 + attempt * 104729 + salt * 15485863);
                var l = Fill(w, h, fill, level, rng);
                int rounds = Solver.Rounds(l);
                if (rounds < 0) continue;
                int score = -Mathf.Abs(rounds - targetRounds) * 10 + l.cars.Count;
                if (score > bestScore) { bestScore = score; best = l; }
            }
            if (best == null) return Tutorial();
            best.difficulty = difficulty;
            ApplyMechanics(best, level, boss);
            return best;
        }

        /// <summary>Şehrin mekaniğini levele ekler (buz kareleri, renkli kapı renkleri, boss süresi).</summary>
        static void ApplyMechanics(LevelLayout l, int level, bool boss)
        {
            var theme = Theme.For(level);
            var rng = new System.Random(level * 31 + 5);
            l.boss = boss;
            l.timeLimit = boss ? 45f + l.cars.Count * 3.5f : 0f;

            if (theme.Mechanic == Mechanic.Ice)
            {
                for (int x = 0; x < l.width; x++)
                for (int y = 0; y < l.height; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (!l.obstacles.Contains(c) && rng.NextDouble() < 0.34) l.ice.Add(c);
                }
            }

            if (theme.Mechanic == Mechanic.ColorGate)
            {
                // Sadece 3 renk: kırmızı, mavi, sarı (kapı bunlar arasında döner)
                int[] gateColors = { 0, 5, 2 };
                l.colorGates = true;
                for (int i = 0; i < l.cars.Count; i++)
                {
                    var s = l.cars[i];
                    s.colorIndex = gateColors[rng.Next(gateColors.Length)];
                    l.cars[i] = s;
                }
            }
        }

        static LevelLayout Fill(int w, int h, float fill, int level, System.Random rng)
        {
            var l = new LevelLayout { width = w, height = h };
            var g = Grid.Build(l);

            int obstacles = level >= 8 ? rng.Next(0, Mathf.Min(3, level / 8) + 1) : 0;
            for (int i = 0; i < obstacles; i++)
            {
                var c = new Vector2Int(rng.Next(w), rng.Next(h));
                if (g[c.x, c.y] != Grid.Empty) continue;
                g[c.x, c.y] = Grid.Obstacle;
                l.obstacles.Add(c);
            }

            int target = Mathf.RoundToInt(w * h * fill);
            int filled = l.obstacles.Count;
            int lastColor = -1;
            for (int tries = 0; tries < 1200 && filled < target; tries++)
            {
                int len = level >= 4 && rng.NextDouble() < 0.16 ? 3 : 2;
                bool hor = rng.NextDouble() < 0.5;
                int maxX = hor ? w - len : w - 1;
                int maxY = hor ? h - 1 : h - len;
                if (maxX < 0 || maxY < 0) continue;

                int color;
                do color = rng.Next(Palette.Cars.Length); while (color == lastColor);

                var spec = new CarSpec
                {
                    x = rng.Next(maxX + 1),
                    y = rng.Next(maxY + 1),
                    length = len,
                    horizontal = hor,
                    facePositive = rng.NextDouble() < 0.5,
                    colorIndex = color,
                    variant = rng.Next(4)
                };
                if (!Grid.IsFree(g, spec)) continue;

                l.cars.Add(spec);
                if (Solver.Rounds(l) < 0)
                {
                    l.cars.RemoveAt(l.cars.Count - 1);
                    continue;
                }
                Grid.Mark(g, spec, l.cars.Count - 1);
                filled += len;
                lastColor = color;
            }
            return l;
        }

        /// <summary>İlk level: elle tasarlandı, oyunu öğretir.</summary>
        static LevelLayout Tutorial()
        {
            var l = new LevelLayout { width = 4, height = 5 };
            l.cars.Add(new CarSpec { x = 1, y = 1, length = 2, horizontal = false, facePositive = true, colorIndex = 0, variant = 0 });
            l.cars.Add(new CarSpec { x = 0, y = 3, length = 2, horizontal = true, facePositive = true, colorIndex = 5, variant = 1 });
            l.cars.Add(new CarSpec { x = 1, y = 0, length = 2, horizontal = true, facePositive = false, colorIndex = 2, variant = 0 });
            l.cars.Add(new CarSpec { x = 3, y = 1, length = 2, horizontal = false, facePositive = false, colorIndex = 3, variant = 3 });
            return l;
        }
    }
}
