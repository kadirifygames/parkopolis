using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Ekrandaki tüm arayüz: üst bar (level, coin), alt güçlendirici butonları,
    /// bilgi balonu, kazanma ekranı, duraklatma menüsü ve Kadirify açılış ekranı.
    /// </summary>
    public partial class GameUI : MonoBehaviour
    {
        public Action OnHint, OnCrane, OnRestart, OnNext, OnRetry;
        public Action<bool> OnPause;

        Canvas canvas;
        RectTransform safe;
        Text levelText, coinText, regionText;
        int currentLevel = 1;
        CanvasGroup banner;
        Text bannerText, bannerCaption, failTitle, failMsg;
        CanvasGroup toast;
        Text toastText;
        GameObject winRoot, pauseRoot;
        RectTransform winCard, pauseCard;
        Text winSubtitle, winReward;
        Image craneFace;
        Text soundText;
        CanvasGroup intro;
        Coroutine toastRoutine;
        int shownCoins;
        Camera cam;
        RectTransform coinIcon, coinPill, rewardCoin;
        readonly RectTransform[] hearts = new RectTransform[4];
        int heartsShown = 4;
        GameObject failRoot;
        RectTransform failCard;

        public void Build(Camera cam, bool preview)
        {
            this.cam = cam;
            var go = gameObject;
            canvas = go.AddComponent<Canvas>();
            if (preview)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1.5f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0f;
            go.AddComponent<GraphicRaycaster>();

            safe = UIKit.Rect("SafeArea", transform);
            UIKit.Stretch(safe);
            if (!preview) safe.gameObject.AddComponent<SafeArea>();

            // Oyun içi göstergeler (HUD) ayrı bir kapta: menüdeyken topluca gizlenir
            hudRoot = UIKit.Rect("HUD", safe);
            UIKit.Stretch(hudRoot);
            var realSafe = safe;
            safe = hudRoot;
            BuildTopBar();
            BuildBottomBar();
            safe = realSafe;

            BuildMechanicsHud(); // GameUI.Mechanics.cs (HUD içinde)
            BuildToast();
            BuildBanner();
            BuildMainMenu();   // GameUI.Menus.cs
            BuildMap();        // GameUI.Menus.cs
            BuildGarage();     // GameUI.Garage.cs
            BuildWin();
            BuildWinChestBar(); // GameUI.Menus.cs
            BuildFail();
            BuildChest();      // GameUI.Menus.cs
            BuildPause();
            BuildSettings();   // GameUI.Menus.cs
            BuildDaily();      // GameUI.Daily.cs
            BuildMissions();   // GameUI.Daily.cs
            BuildDailyResult(); // GameUI.DailyPuzzle.cs
            BuildIntro();

            // Bilgi balonu her ekranın üstünde görünsün (açılış logosu hariç)
            toast.transform.SetParent(transform, true);
            toast.transform.SetSiblingIndex(intro.transform.GetSiblingIndex());
        }

        // ---------------- Üst bar ----------------

        void BuildTopBar()
        {
            var top = new Vector2(0.5f, 1f);

            // Duraklat butonu
            var pause = UIKit.CandyButton(safe, "Pause", Color.white, Palette.UiWhiteEdge, 38, new Vector2(124, 124),
                () => ShowPause(true), out var pauseFace);
            UIKit.Place((RectTransform)pause.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -40), new Vector2(124, 124));
            for (int i = -1; i <= 1; i += 2)
            {
                var bar = UIKit.Panel(pauseFace, "Bar", Palette.UiDark, 6);
                UIKit.Place(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(i * 15, 0), new Vector2(16, 50));
            }

            // Level etiketi: beyaz kart, üstte küçük "SEVİYE", altta büyük numara
            var lvl = UIKit.Panel(safe, "LevelPill", Color.white, 40);
            UIKit.Place(lvl.rectTransform, top, top, new Vector2(0, -38), new Vector2(250, 128));
            UIKit.ShadowFor(lvl.rectTransform);
            var lvlCaption = UIKit.Label(lvl.transform, "SEVİYE", 26, Palette.UiGray);
            lvlCaption.rectTransform.offsetMin = new Vector2(0, 62);
            levelText = UIKit.Label(lvl.transform, "1", 60, Palette.UiDark);
            levelText.rectTransform.offsetMax = new Vector2(0, -26);

            // Şehir etiketi: seviye kartının altında
            var chip = UIKit.Panel(safe, "RegionChip", new Color(Palette.UiDark.r, Palette.UiDark.g, Palette.UiDark.b, 0.78f), 30);
            UIKit.Place(chip.rectTransform, top, top, new Vector2(0, -182), new Vector2(330, 60));
            regionText = UIKit.Label(chip.transform, "İSTANBUL", 28, Color.white);

            // Hak kalpleri: önü kapalı arabaya her dokunuş bir kalp götürür
            var heartBar = UIKit.Panel(safe, "Hearts", Color.white, 30);
            UIKit.Place(heartBar.rectTransform, top, top, new Vector2(0, -256), new Vector2(250, 64));
            UIKit.ShadowFor(heartBar.rectTransform, 0.12f, 8f, 14f);
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = UIKit.Heart(heartBar.transform, 44, Palette.Hex("#FF4D6A"));
                hearts[i].anchoredPosition = new Vector2((i - 1.5f) * 54f, 0);
            }

            // Coin göstergesi: beyaz hap
            var coins = UIKit.Panel(safe, "CoinPill", Color.white, 46);
            UIKit.Place(coins.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -46), new Vector2(270, 100));
            UIKit.ShadowFor(coins.rectTransform);
            coinPill = coins.rectTransform;
            coinIcon = UIKit.Coin(coins.transform, 70, new Vector2(52, 0), new Vector2(0, 0.5f));
            coinText = UIKit.Label(coins.transform, "0", 46, Palette.UiDark, TextAnchor.MiddleRight);
            coinText.rectTransform.offsetMax = new Vector2(-30, 0);
        }

        // ---------------- Alt bar ----------------

        void BuildBottomBar()
        {
            BoosterButton("Hint", "?", "İPUCU", Palette.UiGoldDark, LevelController.HintCost, new Vector2(-290, 52), () => OnHint?.Invoke(), out _);
            BoosterButton("Crane", "▲", "VİNÇ", Palette.UiBlue, LevelController.CraneCost, new Vector2(0, 52), () => OnCrane?.Invoke(), out craneFace);
            BoosterButton("Restart", "↻", "YENİDEN", Palette.UiGreen, 0, new Vector2(290, 52), () => OnRestart?.Invoke(), out _);
        }

        void BoosterButton(string name, string icon, string label, Color iconColor, int cost, Vector2 pos, Action click, out Image face)
        {
            var b = UIKit.CandyButton(safe, name, Color.white, Palette.UiWhiteEdge, 48, new Vector2(240, 205), click, out var content);
            UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), pos, new Vector2(240, 205));
            face = content.GetComponent<Image>();

            var iconBg = UIKit.Dot(content, "IconBg", new Color(iconColor.r, iconColor.g, iconColor.b, 0.16f), 104, new Vector2(0, 26));
            var iconText = UIKit.Label(iconBg.transform, icon, 66, iconColor);
            iconText.rectTransform.offsetMin = new Vector2(0, 4);
            var l = UIKit.Label(content, label, 34, Palette.UiDark);
            l.rectTransform.offsetMin = new Vector2(0, 14);
            l.alignment = TextAnchor.LowerCenter;

            if (cost > 0)
            {
                var tag = UIKit.Panel(content, "Cost", Palette.UiDark, 28);
                UIKit.Place(tag.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-14, -6), new Vector2(118, 56));
                UIKit.Coin(tag.transform, 36, new Vector2(26, 0), new Vector2(0, 0.5f));
                var t = UIKit.Label(tag.transform, cost.ToString(), 32, Color.white, TextAnchor.MiddleRight);
                t.rectTransform.offsetMax = new Vector2(-16, 0);
            }
        }

        public void SetCraneMode(bool on)
        {
            if (craneFace != null) craneFace.color = on ? Palette.Hex("#DCEBFF") : Color.white;
        }

        // ---------------- Bilgi balonu ----------------

        void BuildToast()
        {
            var bg = UIKit.Panel(safe, "Toast", new Color(Palette.UiDark.r, Palette.UiDark.g, Palette.UiDark.b, 0.88f), 44);
            UIKit.Place(bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 300), new Vector2(880, 140));
            toastText = UIKit.Label(bg.transform, "", 40, Color.white);
            toast = bg.gameObject.AddComponent<CanvasGroup>();
            toast.alpha = 0;
            toast.blocksRaycasts = false;
        }

        /// <summary>duration = 0 ise kapatılana kadar kalır.</summary>
        public void ShowToast(string message, float duration)
        {
            toastText.text = message;
            toast.alpha = 1;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = null;
            if (Application.isPlaying)
            {
                toast.transform.localScale = Vector3.one * 0.9f;
                StartCoroutine(Tween.Run(0.25f, k => toast.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, k), Ease.OutBack, true));
                if (duration > 0) toastRoutine = StartCoroutine(Tween.Delay(duration, HideToast, true));
            }
        }

        public void HideToast()
        {
            if (toast != null) toast.alpha = 0;
        }

        // ---------------- Değerler ----------------

        public void SetLevel(int level)
        {
            currentLevel = level;
            levelText.text = level.ToString();
        }

        public void SetRegion(string name, int regionIndex, bool night)
        {
            int stage = (currentLevel - 1) % Theme.LevelsPerRegion + 1;
            regionText.text = $"{name}  {stage}/{Theme.LevelsPerRegion}";
        }

        // ---------------- Kalpler ----------------

        public void SetHearts(int left, bool animate)
        {
            for (int i = 0; i < hearts.Length; i++)
            {
                bool full = i < left;
                foreach (var img in hearts[i].GetComponentsInChildren<Image>())
                    img.color = img.name == "Shine" ? new Color(1, 1, 1, full ? 0.55f : 0f)
                        : full ? Palette.Hex("#FF4D6A") : Palette.Hex("#D5DAE2");
                // Az önce kaybedilen kalp: zıplayıp sönüyor
                if (animate && Application.isPlaying && !full && i < heartsShown)
                    StartCoroutine(Tween.PunchScale(hearts[i], new Vector3(0.6f, 0.6f, 0), 0.35f, 1));
            }
            heartsShown = left;
        }

        // ---------------- Başarısız ekranı ----------------

        void BuildFail()
        {
            failRoot = Overlay("Fail", out failCard, new Vector2(860, 760));
            var ribbon = UIKit.Panel(failCard, "Ribbon", Palette.Hex("#FF5A5F"), 70);
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 150));
            failTitle = UIKit.Label(ribbon.transform, "KİLİTLENDİ!", 72, Color.white, TextAnchor.MiddleCenter, true);
            var broken = UIKit.Heart(failCard, 170, Palette.Hex("#D5DAE2"));
            broken.anchoredPosition = new Vector2(0, 150);
            var crack = UIKit.Panel(broken, "Crack", Color.white, 3);
            UIKit.Place(crack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(12, 150));
            crack.rectTransform.localRotation = Quaternion.Euler(0, 0, 12f);
            failMsg = UIKit.Label(failCard, "Çok fazla hatalı hamle yaptın.\nOtoparkı baştan dene!", 42, Palette.UiGrayEdge);
            failMsg.rectTransform.anchoredPosition = new Vector2(0, -40);
            var retry = UIKit.CandyButton(failCard, "Retry", Palette.UiBlue, Palette.UiBlueEdge, 60, new Vector2(560, 170), () =>
            {
                failRoot.SetActive(false);
                OnRetry?.Invoke();
            }, out var face);
            UIKit.Place((RectTransform)retry.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -240), new Vector2(560, 170));
            UIKit.Label(face, "TEKRAR DENE", 60, Color.white, TextAnchor.MiddleCenter, true);
        }

        public void ShowFail(string title = "KİLİTLENDİ!", string message = "Çok fazla hatalı hamle yaptın.\nOtoparkı baştan dene!")
        {
            failTitle.text = title;
            failMsg.text = message;
            failRoot.SetActive(true);
            if (!Application.isPlaying) return;
            failCard.localScale = Vector3.one * 0.6f;
            StartCoroutine(Tween.Run(0.45f, k => failCard.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, k), Ease.OutBack, true));
        }

        public void HideFail()
        {
            if (failRoot != null) failRoot.SetActive(false);
        }

        // ---------------- Yeni şehir afişi ----------------

        void BuildBanner()
        {
            var card = UIKit.Panel(safe, "RegionBanner", Color.white, 56);
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -290), new Vector2(760, 230));
            bannerCaption = UIKit.Label(card.transform, "YENİ DURAK", 32, Palette.UiBlue);
            bannerCaption.rectTransform.offsetMin = new Vector2(0, 120);
            bannerText = UIKit.Label(card.transform, "", 76, Palette.UiDark);
            bannerText.rectTransform.offsetMax = new Vector2(0, -40);
            banner = card.gameObject.AddComponent<CanvasGroup>();
            banner.alpha = 0;
            banner.blocksRaycasts = false;
        }

        public void ShowRegionBanner(string name, string caption = "YENİ DURAK")
        {
            bannerText.text = name;
            bannerCaption.text = caption;
            bannerCaption.color = caption == "YENİ DURAK" ? Palette.UiBlue : Palette.Hex("#FF5A5F");
            banner.alpha = 1;
            if (!Application.isPlaying) return;
            StartCoroutine(BannerRoutine());
        }

        IEnumerator BannerRoutine()
        {
            var rt = (RectTransform)banner.transform;
            yield return new WaitForSecondsRealtime(0.5f);
            Sfx.Play(Sfx.Sound.Coin, 0.6f);
            yield return Tween.Run(0.45f, k =>
            {
                banner.alpha = k;
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, k);
            }, Ease.OutBack, true);
            yield return new WaitForSecondsRealtime(2f);
            yield return Tween.Run(0.35f, k => banner.alpha = 1f - k, Ease.InOutQuad, true);
        }

        public void SetCoins(int coins)
        {
            shownCoins = coins;
            coinText.text = coins.ToString();
        }

        // ---------------- Kazanma ekranı ----------------

        GameObject Overlay(string name, out RectTransform card, Vector2 size)
        {
            var dim = UIKit.Rect(name, transform);
            UIKit.Stretch(dim);
            var img = dim.gameObject.AddComponent<Image>();
            img.color = new Color(0.05f, 0.08f, 0.15f, 0.6f);
            var c = UIKit.Panel(dim, "Card", Color.white, 64);
            UIKit.Place(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), size);
            UIKit.ShadowFor(c.rectTransform, 0.35f, 24f, 40f);
            card = c.rectTransform;
            dim.gameObject.SetActive(false);
            return dim.gameObject;
        }

        void BuildWin()
        {
            winRoot = Overlay("Win", out winCard, new Vector2(880, 960));

            var ribbon = UIKit.Panel(winCard, "Ribbon", Palette.UiGreen, 70);
            UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(660, 150));
            UIKit.Label(ribbon.transform, "HARİKA!", 78, Color.white, TextAnchor.MiddleCenter, true);

            // Yıldızlar
            for (int i = -1; i <= 1; i++)
            {
                int size = i == 0 ? 190 : 150;
                var star = UIKit.Label(winCard, "★", size, Palette.UiGold, TextAnchor.MiddleCenter, true);
                star.rectTransform.anchoredPosition = new Vector2(i * 175, 205 + (i == 0 ? 25 : 0));
            }

            winSubtitle = UIKit.Label(winCard, "Seviye 1 tamamlandı", 46, Palette.UiGrayEdge);
            winSubtitle.rectTransform.anchoredPosition = new Vector2(0, 30);

            var rewardRow = UIKit.Panel(winCard, "RewardRow", Palette.Hex("#FFF6DB"), 50);
            UIKit.Place(rewardRow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(400, 120));
            rewardCoin = UIKit.Coin(rewardRow.transform, 86, new Vector2(70, 0), new Vector2(0, 0.5f));
            winReward = UIKit.Label(rewardRow.transform, "+20", 64, Palette.UiGoldDark, TextAnchor.MiddleCenter, false);
            winReward.rectTransform.offsetMin = new Vector2(80, 0);

            var next = UIKit.CandyButton(winCard, "Next", Palette.UiGreen, Palette.UiGreenEdge, 60, new Vector2(560, 170), () =>
            {
                HideWin();
                // Sandık dolduysa önce sandığı aç, sonra sonraki level
                if (chestReady) ShowChest(() => OnNext?.Invoke());
                else OnNext?.Invoke();
            }, out var nextFace);
            UIKit.Place((RectTransform)next.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(560, 170));
            UIKit.Label(nextFace, "DEVAM", 68, Color.white, TextAnchor.MiddleCenter, true);
        }

        public void ShowWin(int level, int reward, int totalCoins, string nextRegion = null)
        {
            winSubtitle.text = nextRegion != null ? $"Sıradaki durak: {nextRegion}!" : $"Seviye {level} tamamlandı";
            winReward.text = $"+{reward}";
            winRoot.SetActive(true);
            if (!Application.isPlaying) return;
            winCard.localScale = Vector3.one * 0.6f;
            StartCoroutine(Tween.Run(0.45f, k => winCard.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, k), Ease.OutBack, true));
            StartCoroutine(FlyCoins(shownCoins, totalCoins));
        }

        // ---------------- Uçan coinler ----------------

        /// <summary>Ödül coinleri kazanma kartından üstteki sayaca tek tek uçar.</summary>
        IEnumerator FlyCoins(int from, int to, RectTransform source = null)
        {
            var src = source != null ? source : rewardCoin;
            yield return new WaitForSecondsRealtime(0.55f);
            int count = Mathf.Clamp((to - from) / 3, 6, 14);
            int arrived = 0;
            for (int i = 0; i < count; i++)
            {
                var c = UIKit.Coin(transform, 72, Vector2.zero, new Vector2(0.5f, 0.5f));
                c.position = src.position;
                int index = i;
                StartCoroutine(FlyOne(c, src.position, coinIcon.position, () =>
                {
                    arrived++;
                    coinText.text = Mathf.RoundToInt(Mathf.Lerp(from, to, (float)arrived / count)).ToString();
                    Sfx.Play(Sfx.Sound.Coin, 0.35f, 1f + index * 0.04f);
                    StartCoroutine(Tween.PunchScale(coinPill, new Vector3(0.12f, 0.12f, 0), 0.2f, 1));
                }));
                yield return new WaitForSecondsRealtime(0.06f);
            }
            shownCoins = to;
        }

        IEnumerator FlyOne(RectTransform coin, Vector3 start, Vector3 end, Action onArrive)
        {
            // Kavisli yol: başlangıç ile bitiş arasında yana savrulan bir kontrol noktası (Bezier)
            Vector3 mid = (start + end) * 0.5f + new Vector3(UnityEngine.Random.Range(-260f, 260f), UnityEngine.Random.Range(-60f, 140f), 0) * (canvas.scaleFactor);
            yield return Tween.Run(0.6f, k =>
            {
                Vector3 a = Vector3.Lerp(start, mid, k), b = Vector3.Lerp(mid, end, k);
                coin.position = Vector3.Lerp(a, b, k);
                coin.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.75f, k);
            }, Ease.InOutQuad, true);
            onArrive?.Invoke();
            Destroy(coin.gameObject);
        }

        // ---------------- Uçuşan yazılar ----------------

        /// <summary>
        /// Dünyadaki bir noktanın üstünde beliren, yükselip kaybolan yazı ("+1", "x3 SÜPER!").
        /// bubble = true ise beyaz konuşma balonu içinde (sürücü tepkileri).
        /// </summary>
        public void Popup(Vector3 world, string text, Color color, int size, bool bubble = false)
        {
            if (!Application.isPlaying || cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return;
            var root = (RectTransform)transform;
            Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, uiCam, out var local);

            var rt = UIKit.Rect("Popup", transform);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = local;
            rt.sizeDelta = new Vector2(size * text.Length * 0.62f + 50f, size * 1.6f);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            if (bubble)
            {
                var bg = UIKit.Panel(rt, "Bubble", Color.white, size * 0.8f);
                UIKit.Stretch(bg.rectTransform);
                var tail = UIKit.Panel(rt, "Tail", Color.white, 4);
                UIKit.Place(tail.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(-14, 2), new Vector2(26, 26));
                tail.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            var label = UIKit.Label(rt, text, size, color);
            if (!bubble)
            {
                var o = label.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.1f, 0.12f, 0.2f, 0.85f);
                o.effectDistance = new Vector2(4, -4);
            }
            StartCoroutine(PopupRoutine(rt, group, bubble ? 60f : 150f, bubble ? 0.9f : 0.75f));
        }

        IEnumerator PopupRoutine(RectTransform rt, CanvasGroup group, float rise, float hold)
        {
            Vector2 start = rt.anchoredPosition;
            rt.localScale = Vector3.one * 0.3f;
            yield return Tween.Run(0.22f, k => rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1f, k), Ease.OutBack, true);
            yield return Tween.Run(hold, k =>
            {
                rt.anchoredPosition = start + Vector2.up * rise * k;
                group.alpha = k < 0.55f ? 1f : 1f - (k - 0.55f) / 0.45f;
            }, Ease.OutQuad, true);
            Destroy(rt.gameObject);
        }

        /// <summary>Coin sayacını kısa bir zıplamayla günceller (araba başına +1).</summary>
        public void BumpCoins(int coins)
        {
            SetCoins(coins);
            if (Application.isPlaying) StartCoroutine(Tween.PunchScale(coinPill, new Vector3(0.1f, 0.1f, 0), 0.2f, 1));
        }

        public void HideWin()
        {
            if (winRoot != null) winRoot.SetActive(false);
        }

        // ---------------- Duraklatma ----------------

        void BuildPause()
        {
            pauseRoot = Overlay("Pause", out pauseCard, new Vector2(800, 930));
            var title = UIKit.Label(pauseCard, "DURAKLATILDI", 60, Palette.UiDark);
            title.rectTransform.anchoredPosition = new Vector2(0, 365);

            void Btn(string text, Color face, Color edge, float y, Action a, out Text label)
            {
                var b = UIKit.CandyButton(pauseCard, text, face, edge, 50, new Vector2(600, 140), a, out var f);
                UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(600, 140));
                label = UIKit.Label(f, text, 50, Color.white, TextAnchor.MiddleCenter, true);
            }

            Btn("DEVAM ET", Palette.UiGreen, Palette.UiGreenEdge, 200, () => ShowPause(false), out _);
            Btn("YENİDEN BAŞLA", Palette.UiBlue, Palette.UiBlueEdge, 40, () => { ShowPause(false); OnRestart?.Invoke(); }, out _);
            Btn("ANA MENÜ", Palette.UiGoldDark, Palette.Hex("#D97706"), -120, () => { ShowPause(false); ShowMainMenu(currentLevel); }, out _);
            Btn("SES: AÇIK", Palette.UiGray, Palette.UiGrayEdge, -280, () =>
            {
                SaveData.Sound = !SaveData.Sound;
                RefreshSound();
            }, out soundText);
            RefreshSound();
        }

        void RefreshSound()
        {
            if (soundText != null) soundText.text = SaveData.Sound ? "SES: AÇIK" : "SES: KAPALI";
        }

        public void ShowPause(bool on)
        {
            pauseRoot.SetActive(on);
            Time.timeScale = on ? 0f : 1f;
            OnPause?.Invoke(on);
            if (on && Application.isPlaying)
            {
                pauseCard.localScale = Vector3.one * 0.7f;
                StartCoroutine(Tween.Run(0.35f, k => pauseCard.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, k), Ease.OutBack, true));
            }
        }

        // ---------------- Açılış ekranı ----------------

        void BuildIntro()
        {
            var root = UIKit.Rect("Intro", transform);
            UIKit.Stretch(root);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = Palette.Hex("#0F172A");
            intro = root.gameObject.AddComponent<CanvasGroup>();

            var logo = UIKit.Rect("Logo", root);
            UIKit.Place(logo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(900, 400));
            UIKit.Dot(logo, "Badge", Palette.UiGold, 190, new Vector2(0, 150));
            UIKit.Dot(logo, "BadgeInner", Palette.Hex("#0F172A"), 150, new Vector2(0, 150));
            var k = UIKit.Label(UIKit.Dot(logo, "BadgeK", Palette.UiGold, 120, new Vector2(0, 150)).transform, "K", 96, Palette.Hex("#0F172A"));
            k.rectTransform.offsetMin = new Vector2(4, 4);
            var name = UIKit.Label(logo, "KADIRIFY", 120, Color.white, TextAnchor.MiddleCenter, true);
            name.rectTransform.anchoredPosition = new Vector2(0, -40);
            var sub = UIKit.Label(logo, "G  A  M  E  S", 40, Palette.UiGold);
            sub.rectTransform.anchoredPosition = new Vector2(0, -135);
            root.gameObject.SetActive(false);
        }

        public void ShowIntro()
        {
            intro.gameObject.SetActive(true);
            intro.alpha = 1;
            StartCoroutine(IntroRoutine());
        }

        IEnumerator IntroRoutine()
        {
            var logo = intro.transform.Find("Logo");
            logo.localScale = Vector3.one * 0.85f;
            yield return Tween.Run(0.6f, k => logo.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, k), Ease.OutBack, true);
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Tween.Run(0.45f, k => intro.alpha = 1f - k, Ease.InOutQuad, true);
            intro.gameObject.SetActive(false);
        }
    }
}
