using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// GameUI'nin beşinci parçası: Günün Otoparkı (menü kartı + sonuç ekranı) ve zorluk etiketi.
    /// </summary>
    public partial class GameUI
    {
        public Action OnDailyPuzzle;   // menüden Günün Otoparkı başlat
        public Action OnDailyExit;     // sonuç ekranından ana menüye dön

        static readonly Color Purple = Palette.Hex("#7C4DFF");
        static readonly Color PurpleEdge = Palette.Hex("#5B2EDB");

        Text dpTitleSub, dpDay;
        GameObject dpDone;
        GameObject dailyResultRoot;
        RectTransform dailyResultCard, dpSquares;
        Text dpTime, dpMoves, dpMistakes, dpReward, dpStreak, dpDate;
        string dpShareText;

        GameObject diffTag;
        Text diffText;
        Text levelCaption;

        static readonly string[] Months = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
        static string TodayText => $"{DateTime.Now.Day} {Months[DateTime.Now.Month - 1]}";

        // ================= Menü kartı =================

        void BuildDailyPuzzleCard(Transform area)
        {
            var b = UIKit.CandyButton(area, "DailyPuzzle", Purple, PurpleEdge, 50, new Vector2(780, 150), () => OnDailyPuzzle?.Invoke(), out var face);
            UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 830), new Vector2(780, 150));

            // Takvim simgesi
            var cal = UIKit.Panel(face, "Calendar", Color.white, 18);
            UIKit.Place(cal.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90, 4), new Vector2(100, 100));
            var top = UIKit.Panel(cal.transform, "CalTop", Palette.Hex("#FF5A5F"), 14);
            top.rectTransform.anchorMin = new Vector2(0, 0.7f);
            top.rectTransform.anchorMax = Vector2.one;
            top.rectTransform.offsetMin = top.rectTransform.offsetMax = Vector2.zero;
            dpDay = UIKit.Label(cal.transform, "1", 46, Palette.UiDark);
            dpDay.rectTransform.offsetMax = new Vector2(0, -24);

            TextAt(face, "GÜNÜN OTOPARKI", 46, Color.white, new Vector2(60, 24), new Vector2(560, 60), TextAnchor.MiddleLeft, true);
            dpTitleSub = TextAt(face, "", 28, new Color(1, 1, 1, 0.9f), new Vector2(60, -30), new Vector2(560, 44), TextAnchor.MiddleLeft);

            var done = UIKit.Dot(face, "Done", Palette.UiGreen, 70, Vector2.zero);
            done.rectTransform.anchorMin = done.rectTransform.anchorMax = new Vector2(1, 0.5f);
            done.rectTransform.anchoredPosition = new Vector2(-70, 4);
            var ci = Box(done.transform, "Check", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60));
            CheckIcon(ci, 54, Color.white);
            dpDone = done.gameObject;
        }

        void RefreshDailyPuzzleCard()
        {
            if (dpTitleSub == null) return;
            dpDay.text = DateTime.Now.Day.ToString();
            bool done = Progression.DailyPuzzleDone;
            int streak = Progression.DailyPuzzleStreak;
            dpDone.SetActive(done);
            dpTitleSub.text = done
                ? $"Bugün tamamlandı • Seri: {streak} gün"
                : streak > 0 ? $"Herkese aynı bulmaca • Seri: {streak} gün" : $"Herkese aynı bulmaca • +{Progression.DailyPuzzleReward} coin";
        }

        // ================= Oyun içi =================

        /// <summary>Günün Otoparkı modunda seviye kartı: "GÜNLÜK" + ayın günü.</summary>
        public void SetDailyMode(bool on, int level)
        {
            if (levelCaption == null) levelCaption = hudRoot.Find("LevelPill").GetChild(0).GetComponent<Text>();
            levelCaption.text = on ? "GÜNLÜK" : "SEVİYE";
            levelText.text = on ? DateTime.Now.Day.ToString() : level.ToString();
            if (on) regionText.text = "GÜNÜN OTOPARKI";
        }

        /// <summary>Seviye kartının köşesinde "ZOR" veya "BOSS" etiketi.</summary>
        public void SetDifficulty(Difficulty d)
        {
            if (diffTag == null)
            {
                var pill = hudRoot.Find("LevelPill");
                var tag = UIKit.Panel(pill, "Difficulty", Palette.Hex("#FF5A5F"), 20);
                UIKit.Place(tag.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-6, -4), new Vector2(110, 42));
                tag.rectTransform.localRotation = Quaternion.Euler(0, 0, -10);
                diffText = UIKit.Label(tag.transform, "ZOR", 26, Color.white);
                diffTag = tag.gameObject;
            }
            bool show = d == Difficulty.Hard || d == Difficulty.Boss;
            diffTag.SetActive(show);
            if (!show) return;
            diffText.text = d == Difficulty.Boss ? "BOSS" : "ZOR";
            diffTag.GetComponent<Image>().color = d == Difficulty.Boss ? Palette.Hex("#B91C1C") : Palette.Hex("#FF5A5F");
        }

        // ================= Sonuç ekranı =================

        void BuildDailyResult()
        {
            dailyResultRoot = Overlay("DailyResult", out dailyResultCard, new Vector2(940, 1200));
            var ribbon = UIKit.Panel(dailyResultCard, "Ribbon", Purple, 70);
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 150));
            UIKit.Label(ribbon.transform, "GÜNÜN OTOPARKI", 62, Color.white, TextAnchor.MiddleCenter, true);
            dpDate = TextAt(dailyResultCard, "", 38, Palette.UiGrayEdge, new Vector2(0, 450), new Vector2(800, 60));

            // Üç istatistik kutusu
            Text Stat(float x, string caption)
            {
                var box = UIKit.Panel(dailyResultCard, "Stat", Palette.Hex("#F1F5F9"), 34);
                UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 300), new Vector2(260, 170));
                TextAt(box.transform, caption, 28, Palette.UiGray, new Vector2(0, 46), new Vector2(240, 40));
                return TextAt(box.transform, "", 60, Palette.UiDark, new Vector2(0, -16), new Vector2(240, 80));
            }
            dpTime = Stat(-290, "SÜRE");
            dpMoves = Stat(0, "HAMLE");
            dpMistakes = Stat(290, "HATA");

            // Hamle kareleri (paylaşılan metindeki 🟩🟥🟧 ile aynı)
            dpSquares = Box(dailyResultCard, "Squares", new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(820, 200));

            dpStreak = TextAt(dailyResultCard, "", 40, Palette.UiGoldDark, new Vector2(0, -80), new Vector2(800, 60));
            dpReward = TextAt(dailyResultCard, "", 36, Palette.UiGrayEdge, new Vector2(0, -140), new Vector2(800, 56));

            var copy = UIKit.CandyButton(dailyResultCard, "Copy", Palette.UiBlue, Palette.UiBlueEdge, 50, new Vector2(620, 140), CopyDailyResult, out var cf);
            UIKit.Place((RectTransform)copy.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -280), new Vector2(620, 140));
            UIKit.Label(cf, "SONUCU PAYLAŞ", 50, Color.white, TextAnchor.MiddleCenter, true);
            var menu = UIKit.CandyButton(dailyResultCard, "Menu", Palette.UiGreen, Palette.UiGreenEdge, 50, new Vector2(620, 140), () =>
            {
                dailyResultRoot.SetActive(false);
                OnDailyExit?.Invoke();
            }, out var mf);
            UIKit.Place((RectTransform)menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -450), new Vector2(620, 140));
            UIKit.Label(mf, "ANA MENÜ", 50, Color.white, TextAnchor.MiddleCenter, true);
        }

        /// <summary>events: her hamle için 'G' (çıktı), 'R' (hata), 'O' (yolda çarpışma).</summary>
        public void ShowDailyResult(float seconds, int moves, int mistakes, IList<char> events, bool firstToday, int streak, string shareText)
        {
            dpShareText = shareText;
            int s = Mathf.RoundToInt(seconds);
            dpDate.text = TodayText;
            dpTime.text = $"{s / 60}:{s % 60:00}";
            dpMoves.text = moves.ToString();
            dpMistakes.text = mistakes.ToString();
            dpStreak.text = $"Seri: {streak} gün";
            dpReward.text = firstToday ? $"+{Progression.DailyPuzzleReward} coin kazandın!" : "Bugünün ödülü daha önce alındı.";

            // Kareleri çiz
            var old = new List<GameObject>();
            foreach (Transform c in dpSquares) old.Add(c.gameObject);
            foreach (var go in old) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            int count = Mathf.Min(events.Count, 30);
            int perRow = 10;
            for (int i = 0; i < count; i++)
            {
                Color c = events[i] == 'G' ? Palette.UiGreen : events[i] == 'R' ? Palette.Hex("#FF5A5F") : Palette.UiGoldDark;
                var sq = UIKit.Panel(dpSquares, "Sq", c, 10);
                int row = i / perRow, col = i % perRow;
                int rows = (count + perRow - 1) / perRow;
                UIKit.Place(sq.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((col - (Mathf.Min(count, perRow) - 1) / 2f) * 70f, ((rows - 1) / 2f - row) * 66f), new Vector2(58, 58));
            }

            dailyResultRoot.SetActive(true);
            if (!Application.isPlaying) return;
            dailyResultCard.localScale = Vector3.one * 0.7f;
            StartCoroutine(Tween.Run(0.45f, k => dailyResultCard.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, k), Ease.OutBack, true));
        }

        void CopyDailyResult()
        {
            // Panoya kopyala: oyuncu WhatsApp/Instagram'a yapıştırıp paylaşabilir
            GUIUtility.systemCopyBuffer = dpShareText;
            ShowToast("Sonuç kopyalandı!\nWhatsApp'a yapıştırıp arkadaşlarına gönder.", 2.5f);
        }
    }
}
