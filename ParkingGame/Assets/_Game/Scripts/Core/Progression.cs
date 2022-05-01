using System;
using UnityEngine;

namespace Kadirify.Parkopolis
{
    public enum MissionType { Cars, Levels, NoCrash, Combo, Perfect, Chest }

    /// <summary>Bir görev tanımı: türü, hedef sayısı ve coin ödülü.</summary>
    public class MissionDef
    {
        public MissionType type;
        public int target;
        public int reward;

        public string Title => type switch
        {
            MissionType.Cars => $"{target} araba çıkar",
            MissionType.Levels => $"{target} level bitir",
            MissionType.NoCrash => $"Çarpışmadan {target} level bitir",
            MissionType.Combo => $"{target} kez x3 kombo yap",
            MissionType.Perfect => $"Kalp kaybetmeden {target} level bitir",
            _ => $"{target} sandık aç"
        };

        public Color Color => type switch
        {
            MissionType.Cars => Palette.UiBlue,
            MissionType.Levels => Palette.UiGreen,
            MissionType.NoCrash => Palette.Hex("#FF5A5F"),
            MissionType.Combo => Palette.UiGoldDark,
            MissionType.Perfect => Palette.Hex("#F15BB5"),
            _ => Palette.Hex("#B8682E")
        };
    }

    /// <summary>
    /// Günlük ödül takvimi ve günlük görevler. Tüm veriler PlayerPrefs'te tarih ile birlikte saklanır.
    /// Tarih "yyyyMMdd" biçiminde tutulur (örn. 20261023), böylece "bugün mü, dün mü" kontrolü kolaylaşır.
    /// </summary>
    public static class Progression
    {
        static string DateKey(DateTime d) => d.ToString("yyyyMMdd");
        static string Today => DateKey(DateTime.Now);
        static string Yesterday => DateKey(DateTime.Now.AddDays(-1));

        // ================= GÜNLÜK ÖDÜL =================

        public static readonly int[] DailyRewards = { 50, 75, 100, 125, 150, 200, 400 };

        /// <summary>Bugünün ödülü henüz alınmadı mı?</summary>
        public static bool DailyAvailable => PlayerPrefs.GetString("d_last", "") != Today;

        /// <summary>
        /// Takvimde bugün hangi gün (0-6)? Dün aldıysan seri devam eder,
        /// bir gün kaçırdıysan baştan (0) başlar. Bugün aldıysan aldığın gün.
        /// </summary>
        public static int DailyDay
        {
            get
            {
                string last = PlayerPrefs.GetString("d_last", "");
                int streak = PlayerPrefs.GetInt("d_day", -1);
                if (last == Today) return Mathf.Max(0, streak);
                if (last == Yesterday) return (streak + 1) % DailyRewards.Length;
                return 0;
            }
        }

        public static int ClaimDaily()
        {
            if (!DailyAvailable) return 0;
            int day = DailyDay;
            PlayerPrefs.SetInt("d_day", day);
            PlayerPrefs.SetString("d_last", Today);
            int reward = DailyRewards[day];
            SaveData.Coins += reward; // SaveData.Coins kaydeder (PlayerPrefs.Save)
            return reward;
        }

        // ================= GÜNLÜK GÖREVLER =================

        public const int MissionCount = 3;

        static readonly MissionDef[] Pool =
        {
            new MissionDef { type = MissionType.Cars, target = 25, reward = 50 },
            new MissionDef { type = MissionType.Cars, target = 50, reward = 100 },
            new MissionDef { type = MissionType.Levels, target = 3, reward = 60 },
            new MissionDef { type = MissionType.Levels, target = 6, reward = 120 },
            new MissionDef { type = MissionType.NoCrash, target = 2, reward = 80 },
            new MissionDef { type = MissionType.Combo, target = 3, reward = 70 },
            new MissionDef { type = MissionType.Perfect, target = 2, reward = 90 },
            new MissionDef { type = MissionType.Chest, target = 1, reward = 60 },
        };

        /// <summary>Görev tamamlanınca çağrılır (oyun içi bildirim için).</summary>
        public static Action<MissionDef> OnCompleted;

        /// <summary>Gün değiştiyse 3 yeni görev seç (her gün herkes için aynı: tarih tohum olarak kullanılır).</summary>
        static void EnsureDay()
        {
            if (PlayerPrefs.GetString("m_date", "") == Today) return;
            var rng = new System.Random(int.Parse(Today));
            var usedTypes = new System.Collections.Generic.HashSet<MissionType>();
            int picked = 0, guard = 0;
            while (picked < MissionCount && guard++ < 100)
            {
                int i = rng.Next(Pool.Length);
                if (!usedTypes.Add(Pool[i].type)) continue;
                PlayerPrefs.SetInt($"m_i{picked}", i);
                PlayerPrefs.SetInt($"m_p{picked}", 0);
                PlayerPrefs.SetInt($"m_c{picked}", 0);
                picked++;
            }
            PlayerPrefs.SetString("m_date", Today);
            PlayerPrefs.Save();
        }

        public static MissionDef Get(int slot)
        {
            EnsureDay();
            return Pool[Mathf.Clamp(PlayerPrefs.GetInt($"m_i{slot}", 0), 0, Pool.Length - 1)];
        }

        public static int Progress(int slot) { EnsureDay(); return PlayerPrefs.GetInt($"m_p{slot}", 0); }
        public static bool Claimed(int slot) { EnsureDay(); return PlayerPrefs.GetInt($"m_c{slot}", 0) == 1; }
        public static bool Done(int slot) => Progress(slot) >= Get(slot).target;

        public static int ClaimableCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MissionCount; i++) if (Done(i) && !Claimed(i)) n++;
                return n;
            }
        }

        /// <summary>Oyunda bir şey olunca çağrılır: ilgili görevlerin ilerlemesini artırır.</summary>
        public static void Track(MissionType type, int amount = 1)
        {
            EnsureDay();
            for (int i = 0; i < MissionCount; i++)
            {
                var def = Get(i);
                if (def.type != type || Claimed(i)) continue;
                int before = Progress(i);
                if (before >= def.target) continue;
                int now = Mathf.Min(def.target, before + amount);
                PlayerPrefs.SetInt($"m_p{i}", now);
                if (now >= def.target) OnCompleted?.Invoke(def);
            }
            PlayerPrefs.Save();
        }

        public static int ClaimMission(int slot)
        {
            if (!Done(slot) || Claimed(slot)) return 0;
            PlayerPrefs.SetInt($"m_c{slot}", 1);
            int reward = Get(slot).reward;
            SaveData.Coins += reward;
            return reward;
        }

        /// <summary>Gece yarısına kalan süre (görevler o zaman yenilenir).</summary>
        public static TimeSpan UntilReset => DateTime.Today.AddDays(1) - DateTime.Now;

        // ================= GÜNÜN OTOPARKI =================
        // Herkese aynı bulmaca: tarih tohum (seed) olarak kullanılır.

        public const int DailyPuzzleReward = 100;
        public static int DailyPuzzleSeed => int.Parse(Today);
        public static bool DailyPuzzleDone => PlayerPrefs.GetString("dp_last", "") == Today;

        /// <summary>Kaç gün üst üste Günün Otoparkı bitirildi.</summary>
        public static int DailyPuzzleStreak
        {
            get
            {
                string last = PlayerPrefs.GetString("dp_last", "");
                return last == Today || last == Yesterday ? PlayerPrefs.GetInt("dp_streak", 0) : 0;
            }
        }

        /// <summary>Bitirince çağrılır. Günün ilk bitirişi ise seri artar, ödül verilir ve true döner.</summary>
        public static bool CompleteDailyPuzzle(string resultText)
        {
            if (DailyPuzzleDone) return false;
            int streak = PlayerPrefs.GetString("dp_last", "") == Yesterday ? PlayerPrefs.GetInt("dp_streak", 0) + 1 : 1;
            PlayerPrefs.SetInt("dp_streak", streak);
            PlayerPrefs.SetString("dp_last", Today);
            PlayerPrefs.SetString("dp_result", resultText);
            SaveData.Coins += DailyPuzzleReward;
            return true;
        }

        public static string DailyPuzzleResult => PlayerPrefs.GetString("dp_result", "");

        public static void ResetDailyPuzzle()
        {
            PlayerPrefs.DeleteKey("dp_last");
            PlayerPrefs.Save();
        }

        // ================= TEST =================

        public static void ResetDaily()
        {
            PlayerPrefs.DeleteKey("d_last");
            PlayerPrefs.DeleteKey("d_day");
            PlayerPrefs.Save();
        }

        public static void ResetMissions()
        {
            PlayerPrefs.DeleteKey("m_date");
            PlayerPrefs.Save();
        }
    }
}
