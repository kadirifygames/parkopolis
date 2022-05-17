using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// GameUI'nin üçüncü parçası: Günlük Ödül takvimi, Günlük Görevler ve menüdeki rozetler.
    /// </summary>
    public partial class GameUI
    {
        // Günlük ödül
        GameObject dailyRoot;
        RectTransform dailyCard;
        readonly Image[] dayBg = new Image[7];
        readonly GameObject[] dayHighlight = new GameObject[7];
        readonly GameObject[] dayCheck = new GameObject[7];
        readonly RectTransform[] dayTile = new RectTransform[7];
        Text dailyButtonText, dailyHint;
        Image dailyButtonFace;
        bool dailyShownThisSession;

        // Görevler
        GameObject missionsRoot;
        RectTransform missionsCard;
        Text missionsTimer;
        readonly MissionRow[] rows = new MissionRow[Progression.MissionCount];

        // Menü rozetleri
        GameObject dailyDot, missionsBadge;
        Text missionsBadgeText;

        class MissionRow
        {
            public Image icon;
            public Text title, count, reward;
            public RectTransform fill;
            public GameObject claimButton, doneTag;
        }

        // ================= Simgeler =================

        static void GiftIcon(Transform parent, float s)
        {
            var box = UIKit.Panel(parent, "GiftBox", Palette.Hex("#FF5A5F"), s * 0.08f);
            UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -s * 0.12f), new Vector2(s * 0.72f, s * 0.5f));
            var lid = UIKit.Panel(parent, "GiftLid", Palette.Hex("#E63946"), s * 0.06f);
            UIKit.Place(lid.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, s * 0.16f), new Vector2(s * 0.84f, s * 0.2f));
            var ribbon = UIKit.Panel(parent, "Ribbon", Palette.UiGold, s * 0.03f);
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -s * 0.04f), new Vector2(s * 0.16f, s * 0.72f));
            UIKit.Dot(parent, "BowL", Palette.UiGold, s * 0.26f, new Vector2(-s * 0.13f, s * 0.33f));
            UIKit.Dot(parent, "BowR", Palette.UiGold, s * 0.26f, new Vector2(s * 0.13f, s * 0.33f));
        }

        static void ClipboardIcon(Transform parent, float s)
        {
            var board = UIKit.Panel(parent, "Board", Palette.UiGoldDark, s * 0.08f);
            UIKit.Place(board.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s * 0.7f, s * 0.86f));
            var paper = UIKit.Panel(parent, "Paper", Color.white, s * 0.04f);
            UIKit.Place(paper.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -s * 0.04f), new Vector2(s * 0.54f, s * 0.66f));
            for (int i = 0; i < 3; i++)
            {
                var line = UIKit.Panel(parent, "Line", Palette.Hex("#CBD5E1"), 3);
                UIKit.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, s * (0.1f - i * 0.15f)), new Vector2(s * 0.38f, s * 0.06f));
            }
            var clip = UIKit.Panel(parent, "Clip", Palette.UiDark, s * 0.05f);
            UIKit.Place(clip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, s * 0.4f), new Vector2(s * 0.32f, s * 0.14f));
        }

        static GameObject RedBadge(Transform parent, Vector2 pos, float size, out Text label)
        {
            var dot = UIKit.Dot(parent, "Badge", Palette.Hex("#FF3B30"), size, pos);
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(1, 1);
            label = UIKit.Label(dot.transform, "", (int)(size * 0.55f), Color.white);
            return dot.gameObject;
        }

        /// <summary>Menü için ek parçalar: hediye butonu ve rozetler. BuildMainMenu'den çağrılır.</summary>
        void BuildMenuExtras(Transform area, RectTransform missionsFace)
        {
            var gift = UIKit.CandyButton(area, "Gift", Color.white, Palette.UiWhiteEdge, 38, new Vector2(124, 124), ShowDaily, out var face);
            UIKit.Place((RectTransform)gift.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -40), new Vector2(124, 124));
            var icon = Box(face, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90));
            GiftIcon(icon, 90);
            dailyDot = RedBadge(face, new Vector2(-8, -8), 40, out var t);
            t.text = "!";
            missionsBadge = RedBadge(missionsFace, new Vector2(-14, -10), 52, out missionsBadgeText);
        }

        void RefreshMenuBadges()
        {
            if (dailyDot != null) dailyDot.SetActive(Progression.DailyAvailable);
            int n = Progression.ClaimableCount;
            if (missionsBadge != null) missionsBadge.SetActive(n > 0);
            if (missionsBadgeText != null) missionsBadgeText.text = n.ToString();
        }

        /// <summary>Ana menü açıldığında: rozetleri güncelle, bugünkü ödül alınmadıysa takvimi göster.</summary>
        void OnMainMenuShown()
        {
            RefreshMenuBadges();
            RefreshDailyPuzzleCard();
            if (Application.isPlaying && !dailyShownThisSession && Progression.DailyAvailable)
            {
                dailyShownThisSession = true;
                StartCoroutine(Tween.Delay(1.9f, ShowDaily, true)); // açılış logosundan sonra
            }
        }

        IEnumerator CountUp(Text label, int from, int to)
        {
            Sfx.Play(Sfx.Sound.Coin, 0.7f);
            yield return Tween.Run(0.6f, k => label.text = Mathf.RoundToInt(Mathf.Lerp(from, to, k)).ToString(), Ease.OutQuad, true);
            SetCoins(to);
        }

        // ================= GÜNLÜK ÖDÜL =================

        void BuildDaily()
        {
            dailyRoot = Overlay("Daily", out dailyCard, new Vector2(960, 1180));
            var ribbon = UIKit.Panel(dailyCard, "Ribbon", Palette.UiBlue, 70);
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 150));
            UIKit.Label(ribbon.transform, "GÜNLÜK ÖDÜL", 70, Color.white, TextAnchor.MiddleCenter, true);
            dailyHint = TextAt(dailyCard, "Her gün gel, ödüller büyüsün!", 36, Palette.UiGrayEdge, new Vector2(0, 430), new Vector2(880, 60));

            for (int i = 0; i < 7; i++)
            {
                bool big = i == 6;
                Vector2 pos, size;
                if (i < 4) { pos = new Vector2(-318 + i * 212, 215); size = new Vector2(196, 250); }
                else if (!big) { pos = new Vector2(-318 + (i - 4) * 212, -60); size = new Vector2(196, 250); }
                else { pos = new Vector2(212, -60); size = new Vector2(408, 250); }

                var hl = UIKit.Panel(dailyCard, "Highlight", Palette.UiGold, 40);
                UIKit.Place(hl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size + new Vector2(16, 16));
                dayHighlight[i] = hl.gameObject;

                var tile = UIKit.Panel(dailyCard, "Day", Palette.Hex("#F1F5F9"), 34);
                UIKit.Place(tile.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
                dayBg[i] = tile;
                dayTile[i] = tile.rectTransform;

                TextAt(tile.transform, $"{i + 1}. GÜN", 30, Palette.UiGrayEdge, new Vector2(0, 92), new Vector2(size.x, 44));
                var iconBox = Box(tile.transform, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(110, 110));
                if (big) ChestIcon(iconBox, 150).anchoredPosition = Vector2.zero;
                else UIKit.Coin(iconBox, 92, Vector2.zero, new Vector2(0.5f, 0.5f));
                TextAt(tile.transform, $"+{Progression.DailyRewards[i]}", big ? 48 : 40, Palette.UiGoldDark, new Vector2(0, -86), new Vector2(size.x, 56));

                var check = UIKit.Rect("Check", tile.transform);
                UIKit.Stretch(check);
                var veil = check.gameObject.AddComponent<Image>();
                veil.sprite = UIKit.Rounded;
                veil.type = Image.Type.Sliced;
                veil.pixelsPerUnitMultiplier = 60f / 34f;
                veil.color = new Color(1, 1, 1, 0.72f);
                UIKit.Dot(check, "CheckBg", Palette.UiGreen, 96, new Vector2(0, 6));
                var cb = Box(check, "CheckIcon", new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(80, 80));
                CheckIcon(cb, 70, Color.white);
                dayCheck[i] = check.gameObject;
            }

            var btn = UIKit.CandyButton(dailyCard, "Claim", Palette.UiGreen, Palette.UiGreenEdge, 60, new Vector2(600, 170), ClaimDaily, out var face);
            UIKit.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -400), new Vector2(600, 170));
            dailyButtonFace = face.GetComponent<Image>();
            dailyButtonText = UIKit.Label(face, "TOPLA", 66, Color.white, TextAnchor.MiddleCenter, true);
        }

        public void ShowDaily()
        {
            int today = Progression.DailyDay;
            bool available = Progression.DailyAvailable;
            for (int i = 0; i < 7; i++)
            {
                bool claimed = i < today || (i == today && !available);
                bool isToday = i == today && available;
                dayCheck[i].SetActive(claimed);
                dayHighlight[i].SetActive(isToday);
                dayBg[i].color = isToday ? Palette.Hex("#FFF6DB") : Palette.Hex("#F1F5F9");
            }
            dailyHint.text = available ? "Her gün gel, ödüller büyüsün!" : "Bugünkü ödülü aldın. Yarın yine gel!";
            dailyButtonText.text = available ? "TOPLA" : "KAPAT";
            dailyButtonFace.color = available ? Palette.UiGreen : Palette.UiGray;
            dailyRoot.SetActive(true);
            if (!Application.isPlaying) return;
            dailyCard.localScale = Vector3.one * 0.7f;
            StartCoroutine(Tween.Run(0.4f, k => dailyCard.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, k), Ease.OutBack, true));
            if (available) StartCoroutine(PulseToday(today));
        }

        IEnumerator PulseToday(int day)
        {
            var tile = dayTile[day];
            while (dailyRoot.activeSelf && dayHighlight[day].activeSelf)
            {
                float s = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.04f;
                tile.localScale = Vector3.one * s;
                dayHighlight[day].transform.localScale = Vector3.one * s;
                yield return null;
            }
            tile.localScale = Vector3.one;
        }

        void ClaimDaily()
        {
            if (!Progression.DailyAvailable)
            {
                dailyRoot.SetActive(false);
                return;
            }
            int day = Progression.DailyDay;
            int before = SaveData.Coins;
            int reward = Progression.ClaimDaily();
            dayHighlight[day].SetActive(false);
            dayCheck[day].SetActive(true);
            dayTile[day].localScale = Vector3.one;
            StartCoroutine(Tween.PunchScale(dayTile[day], new Vector3(0.2f, 0.2f, 0), 0.35f, 1));
            Sfx.Play(Sfx.Sound.Win, 0.8f);
            StartCoroutine(CountUp(menuCoins, before, SaveData.Coins));
            dailyButtonText.text = "KAPAT";
            dailyButtonFace.color = Palette.UiGray;
            dailyHint.text = $"+{reward} coin kazandın! Yarın yine gel.";
            RefreshMenuBadges();
        }

        // ================= GÜNLÜK GÖREVLER =================

        void BuildMissions()
        {
            missionsRoot = Overlay("Missions", out missionsCard, new Vector2(960, 1150));
            var ribbon = UIKit.Panel(missionsCard, "Ribbon", Palette.UiGoldDark, 70);
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 150));
            UIKit.Label(ribbon.transform, "GÖREVLER", 70, Color.white, TextAnchor.MiddleCenter, true);
            missionsTimer = TextAt(missionsCard, "", 34, Palette.UiGrayEdge, new Vector2(0, 420), new Vector2(880, 60));

            for (int i = 0; i < rows.Length; i++)
            {
                var r = new MissionRow();
                float y = 240 - i * 250;
                var bg = UIKit.Panel(missionsCard, "Row", Palette.Hex("#F1F5F9"), 40);
                UIKit.Place(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(880, 220));
                r.icon = UIKit.Dot(bg.transform, "Icon", Palette.UiBlue, 120, new Vector2(-350, 0));
                TextAt(r.icon.transform, (i + 1).ToString(), 60, Color.white, Vector2.zero, new Vector2(120, 120), TextAnchor.MiddleCenter, true);
                r.title = TextAt(bg.transform, "", 34, Palette.UiDark, new Vector2(-30, 50), new Vector2(500, 60), TextAnchor.MiddleLeft);
                r.fill = ProgressBar(bg.transform, new Vector2(-70, -20), new Vector2(420, 38), Palette.UiBlue);
                r.count = TextAt(bg.transform, "", 30, Palette.UiGrayEdge, new Vector2(-70, -66), new Vector2(420, 44));

                var rewardPill = UIKit.Panel(bg.transform, "Reward", Color.white, 30);
                UIKit.Place(rewardPill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(305, 50), new Vector2(190, 64));
                UIKit.Coin(rewardPill.transform, 46, new Vector2(34, 0), new Vector2(0, 0.5f));
                r.reward = TextAt(rewardPill.transform, "", 34, Palette.UiGoldDark, new Vector2(24, 0), new Vector2(130, 60));

                int slot = i;
                var claim = UIKit.CandyButton(bg.transform, "Claim", Palette.UiGreen, Palette.UiGreenEdge, 34, new Vector2(190, 80), () => ClaimMission(slot), out var cf);
                UIKit.Place((RectTransform)claim.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(305, -45), new Vector2(190, 80));
                UIKit.Label(cf, "AL", 44, Color.white, TextAnchor.MiddleCenter, true);
                r.claimButton = claim.gameObject;

                var done = UIKit.Panel(bg.transform, "Done", Palette.Hex("#DCFCE7"), 30);
                UIKit.Place(done.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(305, -45), new Vector2(190, 70));
                UIKit.Label(done.transform, "ALINDI", 32, Palette.UiGreenEdge);
                r.doneTag = done.gameObject;
                rows[i] = r;
            }

            var close = UIKit.CandyButton(missionsCard, "Close", Palette.UiBlue, Palette.UiBlueEdge, 60, new Vector2(560, 160), () =>
            {
                missionsRoot.SetActive(false);
                RefreshMenuBadges();
            }, out var face);
            UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -470), new Vector2(560, 160));
            UIKit.Label(face, "KAPAT", 60, Color.white, TextAnchor.MiddleCenter, true);
        }

        void RefreshMissions()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                var def = Progression.Get(i);
                var r = rows[i];
                int p = Progression.Progress(i);
                bool done = Progression.Done(i), claimed = Progression.Claimed(i);
                r.icon.color = def.Color;
                r.title.text = def.Title;
                r.count.text = done ? "Tamamlandı!" : $"{p} / {def.target}";
                r.fill.GetComponent<Image>().color = done ? Palette.UiGreen : def.Color;
                SetFill(r.fill, (float)p / def.target);
                r.reward.text = def.reward.ToString();
                r.claimButton.SetActive(done && !claimed);
                r.doneTag.SetActive(claimed);
            }
            var left = Progression.UntilReset;
            missionsTimer.text = $"Yenilenmesine: {left.Hours:00}:{left.Minutes:00}:{left.Seconds:00}";
        }

        public void ShowMissions()
        {
            RefreshMissions();
            missionsRoot.SetActive(true);
            if (!Application.isPlaying) return;
            missionsCard.localScale = Vector3.one * 0.7f;
            StartCoroutine(Tween.Run(0.4f, k => missionsCard.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, k), Ease.OutBack, true));
            StartCoroutine(MissionTimer());
        }

        IEnumerator MissionTimer()
        {
            while (missionsRoot.activeSelf)
            {
                var left = Progression.UntilReset;
                missionsTimer.text = $"Yenilenmesine: {left.Hours:00}:{left.Minutes:00}:{left.Seconds:00}";
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        void ClaimMission(int slot)
        {
            int before = SaveData.Coins;
            int reward = Progression.ClaimMission(slot);
            if (reward <= 0) return;
            Sfx.Play(Sfx.Sound.Win, 0.7f);
            StartCoroutine(CountUp(menuCoins, before, SaveData.Coins));
            RefreshMissions();
            RefreshMenuBadges();
        }

        /// <summary>Oyun sırasında görev tamamlanınca kısa bildirim.</summary>
        public void ShowMissionDone(MissionDef def)
        {
            Sfx.Play(Sfx.Sound.Coin, 0.6f, 1.3f);
            ShowToast($"GÖREV TAMAMLANDI!\n{def.Title}", 2.2f);
        }
    }
}
