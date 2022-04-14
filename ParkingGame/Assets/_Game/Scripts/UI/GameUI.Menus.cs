using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// GameUI'nin ikinci yarısı (partial class): Ana Menü, Türkiye Haritası, Sandık ve Ayarlar ekranları.
    /// </summary>
    public partial class GameUI
    {
        /// <summary>Menü açılınca true, oyuna dönünce false (oyun girişini kilitlemek için).</summary>
        public Action<bool> OnMenu;

        RectTransform hudRoot;
        bool isPreview;

        // Ana menü
        GameObject menuRoot;
        Text menuPlaySub, menuCoins, menuChestText;
        RectTransform menuChestFill;

        // Harita
        GameObject mapRoot;
        ScrollRect mapScroll;
        RectTransform mapContent;
        Text mapTour;

        // Sandık
        GameObject chestRoot;
        RectTransform chestBox, chestLid, chestRays;
        CanvasGroup chestRewardGroup, chestButtonGroup;
        Text chestRewardText;
        Action chestDone;
        bool chestReady;
        RectTransform winChestFill;
        Text winChestText;

        // Ayarlar
        GameObject settingsRoot;
        Text settingsSound;

        static readonly Color ChestBrown = Palette.Hex("#B8682E");
        static readonly Color ChestDark = Palette.Hex("#8A4A1E");

        // ================= Yardımcılar =================

        static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = UIKit.Rect(name, parent);
            UIKit.Place(rt, anchor, new Vector2(0.5f, 0.5f), pos, size);
            return rt;
        }

        static Text TextAt(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 box, TextAnchor align = TextAnchor.MiddleCenter, bool shadow = false)
        {
            var holder = Box(parent, "T", new Vector2(0.5f, 0.5f), pos, box);
            var t = UIKit.Label(holder, text, size, color, align, shadow);
            return t;
        }

        static GameObject FullScreen(Transform parent, string name, Color bg)
        {
            var rt = UIKit.Rect(name, parent);
            UIKit.Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = bg;
            return rt.gameObject;
        }

        /// <summary>Sandık simgesi: gövde, kapak, altın şerit, kilit.</summary>
        static RectTransform ChestIcon(Transform parent, float s)
        {
            var rt = UIKit.Rect("ChestIcon", parent);
            rt.sizeDelta = new Vector2(s, s);
            var body = UIKit.Panel(rt, "Body", ChestBrown, s * 0.1f);
            UIKit.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -s * 0.14f), new Vector2(s * 0.9f, s * 0.52f));
            var lid = UIKit.Panel(rt, "Lid", ChestDark, s * 0.14f);
            UIKit.Place(lid.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, s * 0.18f), new Vector2(s * 0.96f, s * 0.3f));
            var band = UIKit.Panel(rt, "Band", Palette.UiGold, s * 0.03f);
            UIKit.Place(band.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(s * 0.16f, s * 0.78f));
            UIKit.Dot(rt, "Lock", Palette.UiGoldDark, s * 0.2f, new Vector2(0, s * 0.03f));
            return rt;
        }

        /// <summary>Harita iğnesi simgesi.</summary>
        static void PinIcon(Transform parent, float s, Color c)
        {
            var tip = UIKit.Panel(parent, "PinTip", c, s * 0.05f);
            UIKit.Place(tip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -s * 0.08f), new Vector2(s * 0.42f, s * 0.42f));
            tip.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            UIKit.Dot(parent, "PinHead", c, s * 0.6f, new Vector2(0, s * 0.12f));
            UIKit.Dot(parent, "PinHole", Color.white, s * 0.24f, new Vector2(0, s * 0.12f));
        }

        /// <summary>Küçük araba simgesi.</summary>
        static void CarIcon(Transform parent, float s, Color c)
        {
            var body = UIKit.Panel(parent, "CarBody", c, s * 0.12f);
            UIKit.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -s * 0.02f), new Vector2(s * 0.9f, s * 0.34f));
            var top = UIKit.Panel(parent, "CarTop", c, s * 0.12f);
            UIKit.Place(top.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-s * 0.04f, s * 0.18f), new Vector2(s * 0.5f, s * 0.3f));
            var glass = UIKit.Panel(parent, "CarGlass", Color.white, s * 0.06f);
            UIKit.Place(glass.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-s * 0.04f, s * 0.19f), new Vector2(s * 0.36f, s * 0.15f));
            UIKit.Dot(parent, "W1", Palette.UiDark, s * 0.22f, new Vector2(-s * 0.25f, -s * 0.2f));
            UIKit.Dot(parent, "W2", Palette.UiDark, s * 0.22f, new Vector2(s * 0.25f, -s * 0.2f));
        }

        /// <summary>Dişli (ayarlar) simgesi.</summary>
        static void GearIcon(Transform parent, float s, Color c)
        {
            for (int i = 0; i < 4; i++)
            {
                var tooth = UIKit.Panel(parent, "Tooth", c, s * 0.04f);
                UIKit.Place(tooth.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s * 0.2f, s * 0.86f));
                tooth.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 45f);
            }
            UIKit.Dot(parent, "Ring", c, s * 0.66f, Vector2.zero);
            UIKit.Dot(parent, "Hole", Color.white, s * 0.28f, Vector2.zero);
        }

        /// <summary>Kilit simgesi.</summary>
        static void LockIcon(Transform parent, float s, Color c)
        {
            UIKit.Dot(parent, "Shackle", c, s * 0.5f, new Vector2(0, s * 0.14f));
            UIKit.Dot(parent, "ShackleHole", Palette.Hex("#C5CCD6"), s * 0.28f, new Vector2(0, s * 0.14f));
            var body = UIKit.Panel(parent, "LockBody", c, s * 0.08f);
            UIKit.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -s * 0.1f), new Vector2(s * 0.62f, s * 0.44f));
        }

        /// <summary>Onay işareti (✓).</summary>
        static void CheckIcon(Transform parent, float s, Color c)
        {
            var a = UIKit.Panel(parent, "CheckA", c, s * 0.06f);
            UIKit.Place(a.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-s * 0.14f, -s * 0.06f), new Vector2(s * 0.16f, s * 0.42f));
            a.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var b = UIKit.Panel(parent, "CheckB", c, s * 0.06f);
            UIKit.Place(b.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(s * 0.1f, s * 0.04f), new Vector2(s * 0.16f, s * 0.7f));
            b.rectTransform.localRotation = Quaternion.Euler(0, 0, -38);
        }

        /// <summary>İlerleme çubuğu: gri zemin + renkli dolgu. Dolgu oranı anchorMax.x ile ayarlanır.</summary>
        static RectTransform ProgressBar(Transform parent, Vector2 pos, Vector2 size, Color fill)
        {
            var bg = UIKit.Panel(parent, "BarBg", Palette.Hex("#E2E8F0"), size.y / 2f);
            UIKit.Place(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var f = UIKit.Panel(bg.transform, "Fill", fill, size.y / 2f);
            var rt = f.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(4, 4);
            rt.offsetMax = new Vector2(-4, -4);
            return rt;
        }

        static void SetFill(RectTransform fill, float k)
        {
            if (fill == null) return;
            var max = fill.anchorMax;
            max.x = Mathf.Clamp(k, 0.08f, 1f);
            fill.anchorMax = max;
        }

        // ================= ANA MENÜ =================

        void BuildMainMenu()
        {
            isPreview = canvas.renderMode != RenderMode.ScreenSpaceOverlay;
            menuRoot = FullScreen(transform, "MainMenu", new Color(0.05f, 0.08f, 0.16f, 0.18f));
            var area = UIKit.Rect("Safe", menuRoot.transform);
            UIKit.Stretch(area);
            if (!isPreview) area.gameObject.AddComponent<SafeArea>();

            // Logo: hafif eğik "PARKO / POLIS"
            var logo = Box(area, "Logo", new Vector2(0.5f, 1f), new Vector2(0, -330), new Vector2(900, 420));
            logo.localRotation = Quaternion.Euler(0, 0, 4f);
            var plate = UIKit.Panel(logo, "Plate", Color.white, 70);
            UIKit.Place(plate.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(760, 330));
            UIKit.ShadowFor(plate.rectTransform, 0.25f, 18f, 30f);
            var park = TextAt(logo, "PARKO", 170, Palette.UiBlue, new Vector2(0, 85), new Vector2(760, 170));
            park.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -8);
            var rush = TextAt(logo, "POLIS", 170, Palette.Hex("#FF5A5F"), new Vector2(0, -60), new Vector2(760, 170));
            rush.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -8);
            var by = UIKit.Panel(area, "By", Palette.UiDark, 30);
            UIKit.Place(by.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, -585), new Vector2(330, 62));
            UIKit.Label(by.transform, "by KADIRIFY", 30, Palette.UiGold);

            // Coin
            var coins = UIKit.Panel(area, "Coins", Color.white, 46);
            UIKit.Place(coins.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -46), new Vector2(270, 100));
            UIKit.ShadowFor(coins.rectTransform);
            UIKit.Coin(coins.transform, 70, new Vector2(52, 0), new Vector2(0, 0.5f));
            menuCoins = UIKit.Label(coins.transform, "0", 46, Palette.UiDark, TextAnchor.MiddleRight);
            menuCoins.rectTransform.offsetMax = new Vector2(-30, 0);

            // Sandık kartı
            var chest = UIKit.Panel(area, "ChestCard", Color.white, 44);
            UIKit.Place(chest.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 640), new Vector2(780, 150));
            UIKit.ShadowFor(chest.rectTransform);
            var ci = ChestIcon(chest.transform, 110);
            ci.anchoredPosition = new Vector2(-300, 0);
            menuChestText = TextAt(chest.transform, "", 34, Palette.UiDark, new Vector2(60, 26), new Vector2(560, 50), TextAnchor.MiddleLeft);
            menuChestFill = ProgressBar(chest.transform, new Vector2(60, -26), new Vector2(560, 36), Palette.UiGold);

            // OYNA
            var play = UIKit.CandyButton(area, "Play", Palette.UiGreen, Palette.UiGreenEdge, 70, new Vector2(660, 220), () =>
            {
                HideMenus();
            }, out var playFace);
            UIKit.Place((RectTransform)play.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 410), new Vector2(660, 220));
            TextAt(playFace, "OYNA", 96, Color.white, new Vector2(0, 22), new Vector2(600, 110), TextAnchor.MiddleCenter, true);
            menuPlaySub = TextAt(playFace, "", 34, new Color(1, 1, 1, 0.92f), new Vector2(0, -58), new Vector2(600, 50));

            // Alt butonlar
            RectTransform SmallButton(string label, float x, Action click, Action<Transform> icon, bool soon)
            {
                var b = UIKit.CandyButton(area, label, Color.white, Palette.UiWhiteEdge, 48, new Vector2(215, 200), click, out var face);
                UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(x, 160), new Vector2(215, 200));
                var iconRt = Box(face, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(100, 100));
                icon(iconRt);
                TextAt(face, label, 34, Palette.UiDark, new Vector2(0, -62), new Vector2(220, 50));
                if (soon)
                {
                    var tag = UIKit.Panel(face, "Soon", Palette.Hex("#FF5A5F"), 24);
                    UIKit.Place(tag.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-20, -4), new Vector2(150, 48));
                    UIKit.Label(tag.transform, "YAKINDA", 24, Color.white);
                }
                return face;
            }
            SmallButton("HARİTA", -375, () => ShowMap(currentLevel), t => PinIcon(t, 100, Palette.Hex("#FF5A5F")), false);
            var missionsFace = SmallButton("GÖREVLER", -125, ShowMissions, t => ClipboardIcon(t, 100), false);
            SmallButton("GARAJ", 125, ShowGarage, t => CarIcon(t, 100, Palette.UiBlue), false);
            SmallButton("AYARLAR", 375, () => ShowSettings(true), t => GearIcon(t, 92, Palette.UiGray), false);
            BuildMenuExtras(area, missionsFace); // GameUI.Daily.cs: hediye butonu ve rozetler
            BuildDailyPuzzleCard(area);          // GameUI.DailyPuzzle.cs

            menuRoot.SetActive(false);
        }

        public void ShowMainMenu(int level)
        {
            currentLevel = level;
            var theme = Theme.For(level);
            menuPlaySub.text = $"Seviye {level}  •  {theme.Name}";
            menuCoins.text = SaveData.Coins.ToString();
            int left = SaveData.ChestEvery - SaveData.ChestProgress;
            menuChestText.text = left <= 1 ? "Sonraki levelde sandık açılıyor!" : $"Sandığa {left} level kaldı";
            SetFill(menuChestFill, (float)SaveData.ChestProgress / SaveData.ChestEvery);
            if (mapRoot != null) mapRoot.SetActive(false);
            if (garageRoot != null) garageRoot.SetActive(false);
            menuRoot.SetActive(true);
            hudRoot.gameObject.SetActive(false);
            OnMenu?.Invoke(true);
            OnMainMenuShown();
        }

        /// <summary>Menüleri kapatıp oyuna döner.</summary>
        public void HideMenus()
        {
            menuRoot.SetActive(false);
            mapRoot.SetActive(false);
            hudRoot.gameObject.SetActive(true);
            OnMenu?.Invoke(false);
        }

        // ================= TÜRKİYE HARİTASI =================

        void BuildMap()
        {
            mapRoot = FullScreen(transform, "Map", Palette.Hex("#D6ECFF"));
            var area = UIKit.Rect("Safe", mapRoot.transform);
            UIKit.Stretch(area);
            if (!isPreview) area.gameObject.AddComponent<SafeArea>();

            // Kaydırılabilir alan (ScrollRect): viewport = görünen pencere, content = kayan uzun içerik
            var viewport = UIKit.Rect("Viewport", area);
            UIKit.Stretch(viewport);
            viewport.offsetMin = new Vector2(0, 260);
            viewport.offsetMax = new Vector2(0, -230);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(1, 1, 1, 0); // sürüklemeyi yakalamak için görünmez zemin
            mapContent = UIKit.Rect("Content", viewport);
            mapContent.anchorMin = new Vector2(0, 0);
            mapContent.anchorMax = new Vector2(1, 0);
            mapContent.pivot = new Vector2(0.5f, 0);
            mapScroll = area.gameObject.AddComponent<ScrollRect>();
            mapScroll.viewport = viewport;
            mapScroll.content = mapContent;
            mapScroll.horizontal = false;
            mapScroll.vertical = true;
            mapScroll.movementType = ScrollRect.MovementType.Elastic;
            mapScroll.scrollSensitivity = 30f;

            // Başlık
            var header = UIKit.Panel(area, "Header", Color.white, 50);
            UIKit.Place(header.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1010, 180));
            UIKit.ShadowFor(header.rectTransform);
            var back = UIKit.CandyButton(header.transform, "Back", Palette.UiBlue, Palette.UiBlueEdge, 36, new Vector2(120, 120), () => ShowMainMenu(currentLevel), out var backFace);
            UIKit.Place((RectTransform)back.transform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90, 4), new Vector2(120, 120));
            var arrow = UIKit.Panel(backFace, "ArrowA", Color.white, 6);
            UIKit.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-4, 12), new Vector2(16, 48));
            arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
            var arrow2 = UIKit.Panel(backFace, "ArrowB", Color.white, 6);
            UIKit.Place(arrow2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-4, -12), new Vector2(16, 48));
            arrow2.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            TextAt(header.transform, "TÜRKİYE TURU", 64, Palette.UiDark, new Vector2(50, 18), new Vector2(700, 80));
            mapTour = TextAt(header.transform, "", 30, Palette.UiGray, new Vector2(50, -46), new Vector2(700, 50));

            // OYNA
            var play = UIKit.CandyButton(area, "MapPlay", Palette.UiGreen, Palette.UiGreenEdge, 60, new Vector2(600, 170), HideMenus, out var face);
            UIKit.Place((RectTransform)play.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(600, 170));
            TextAt(face, "OYNA", 76, Color.white, Vector2.zero, new Vector2(560, 120), TextAnchor.MiddleCenter, true);

            mapRoot.SetActive(false);
        }

        public void ShowMap(int level)
        {
            currentLevel = level;
            menuRoot.SetActive(false);
            mapRoot.SetActive(true);
            hudRoot.gameObject.SetActive(false);
            OnMenu?.Invoke(true);

            var old = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in mapContent) old.Add(child.gameObject);
            foreach (var go in old)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }

            int cities = Theme.All.Length;
            int perTour = Theme.LevelsPerRegion * cities;
            int tour = (level - 1) / perTour;
            int current = Theme.RegionIndex(level);
            int stage = (level - 1) % Theme.LevelsPerRegion;
            mapTour.text = tour == 0 ? $"Seviye {level}" : $"{tour + 1}. TUR  •  Seviye {level}";

            const float spacing = 340f, bottomPad = 180f;
            float height = bottomPad * 2f + spacing * (cities - 1) + 120f;
            mapContent.sizeDelta = new Vector2(0, height);

            Vector2 NodePos(int i) => new Vector2(i % 2 == 0 ? -190f : 190f, bottomPad + i * spacing);

            // Şehirler arası noktalı yol
            for (int i = 0; i < cities - 1; i++)
            {
                Vector2 a = NodePos(i), b = NodePos(i + 1);
                for (int k = 1; k < 9; k++)
                {
                    var p = Vector2.Lerp(a, b, k / 9f);
                    var dot = UIKit.Dot(mapContent, "PathDot", i < current ? Palette.UiGold : Color.white, 22, Vector2.zero);
                    dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.5f, 0);
                    dot.rectTransform.anchoredPosition = p;
                }
            }

            for (int i = 0; i < cities; i++)
            {
                var t = Theme.All[i];
                bool done = i < current, here = i == current, locked = i > current;
                Vector2 pos = NodePos(i);

                var node = UIKit.Rect("City_" + t.Name, mapContent);
                node.anchorMin = node.anchorMax = new Vector2(0.5f, 0);
                node.anchoredPosition = pos;
                node.sizeDelta = new Vector2(220, 220);

                UIKit.Dot(node, "Glow", new Color(1, 1, 1, 0.6f), here ? 250 : 0, Vector2.zero);
                UIKit.Dot(node, "Ring", Color.white, 210, Vector2.zero);
                UIKit.Dot(node, "Fill", locked ? Palette.Hex("#C5CCD6") : t.Accent, 180, Vector2.zero);
                var iconBox = Box(node, "Icon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
                if (locked) LockIcon(iconBox, 90, Palette.Hex("#8A94A6"));
                else if (done) CheckIcon(iconBox, 110, Color.white);
                else TextAt(iconBox, (i + 1).ToString(), 90, Color.white, Vector2.zero, new Vector2(120, 120), TextAnchor.MiddleCenter, true);

                // Yanındaki bilgi kartı
                float side = pos.x < 0 ? 1f : -1f;
                var card = UIKit.Panel(mapContent, "Card", Color.white, 36);
                card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(0.5f, 0);
                card.rectTransform.anchoredPosition = pos + new Vector2(side * 300f, 0);
                card.rectTransform.sizeDelta = new Vector2(340, 160);
                UIKit.ShadowFor(card.rectTransform, 0.12f, 10f, 16f);
                TextAt(card.transform, t.Name, t.Name.Length > 10 ? 32 : 40, locked ? Palette.UiGray : Palette.UiDark, new Vector2(0, 42), new Vector2(320, 50));
                TextAt(card.transform, t.Tagline ?? "", 24, Palette.UiGray, new Vector2(0, 2), new Vector2(320, 40));
                for (int s = 0; s < Theme.LevelsPerRegion; s++)
                {
                    bool filled = done || (here && s < stage);
                    bool now = here && s == stage;
                    bool bossStage = s == Theme.LevelsPerRegion - 1; // her şehrin son durağı boss
                    UIKit.Dot(card.transform, "Stage", filled ? t.Accent : now ? Palette.UiGold : bossStage ? Palette.Hex("#FFD0D0") : Palette.Hex("#E2E8F0"),
                        now || bossStage ? 32 : 24, new Vector2((s - 2) * 46f, -46f));
                }

                if (here)
                {
                    var tag = UIKit.Panel(node, "Here", Palette.UiGold, 26);
                    UIKit.Place(tag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 26), new Vector2(270, 56));
                    UIKit.Label(tag.transform, "SEN BURADASIN", 26, Palette.UiDark);
                    if (Application.isPlaying) StartCoroutine(Bob(tag.rectTransform));
                }
                if (done)
                {
                    var stamp = UIKit.Panel(card.transform, "Stamp", Palette.UiGreen, 18);
                    UIKit.Place(stamp.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-30, -6), new Vector2(150, 40));
                    stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, -8);
                    UIKit.Label(stamp.transform, "GEZİLDİ", 22, Color.white);
                }
            }

            // Bulunduğun şehri ekranın ortasına getir
            Canvas.ForceUpdateCanvases();
            float viewH = ((RectTransform)mapScroll.viewport).rect.height;
            float target = NodePos(current).y - viewH * 0.45f;
            mapScroll.verticalNormalizedPosition = Mathf.Clamp01(target / Mathf.Max(1f, height - viewH));
        }

        IEnumerator Bob(RectTransform rt)
        {
            Vector2 start = rt.anchoredPosition;
            while (rt != null)
            {
                rt.anchoredPosition = start + Vector2.up * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 12f;
                yield return null;
            }
        }

        // ================= SANDIK =================

        /// <summary>Kazanma kartındaki sandık ilerleme çubuğu.</summary>
        void BuildWinChestBar()
        {
            var icon = ChestIcon(winCard, 80);
            icon.anchoredPosition = new Vector2(-270, -200);
            winChestFill = ProgressBar(winCard, new Vector2(10, -200), new Vector2(380, 40), Palette.UiGold);
            winChestText = TextAt(winCard, "0/3", 34, Palette.UiGrayEdge, new Vector2(265, -200), new Vector2(120, 50));
        }

        /// <summary>Kazanma ekranında sandık çubuğunu gösterir; ready = bu levelle sandık doldu.</summary>
        public void SetChestProgress(int progress, bool ready)
        {
            chestReady = ready;
            winChestText.text = ready ? "AÇ!" : $"{progress}/{SaveData.ChestEvery}";
            float to = (float)progress / SaveData.ChestEvery;
            float from = Mathf.Max(0f, (float)(progress - 1) / SaveData.ChestEvery);
            SetFill(winChestFill, from);
            if (Application.isPlaying)
                StartCoroutine(Tween.Delay(0.7f, () => StartCoroutine(Tween.Run(0.5f, k => SetFill(winChestFill, Mathf.Lerp(from, to, k)), Ease.OutCubic, true)), true));
            else SetFill(winChestFill, to);
        }

        void BuildChest()
        {
            chestRoot = FullScreen(transform, "Chest", new Color(0.05f, 0.08f, 0.15f, 0.82f));

            // Arkada dönen ışık hüzmeleri
            chestRays = Box(chestRoot.transform, "Rays", new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(10, 10));
            for (int i = 0; i < 6; i++)
            {
                var ray = UIKit.Panel(chestRays, "Ray", new Color(1f, 0.86f, 0.4f, 0.22f), 30);
                UIKit.Place(ray.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 1300));
                ray.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 30f);
            }

            TextAt(chestRoot.transform, "SANDIK!", 110, Palette.UiGold, new Vector2(0, 620), new Vector2(900, 150), TextAnchor.MiddleCenter, true);

            chestBox = Box(chestRoot.transform, "Box", new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(420, 420));
            var body = UIKit.Panel(chestBox, "Body", ChestBrown, 34);
            UIKit.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(400, 240));
            var plank = UIKit.Panel(chestBox, "Plank", ChestDark, 10);
            UIKit.Place(plank.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(400, 18));
            var bandL = UIKit.Panel(chestBox, "BandL", Palette.UiGold, 8);
            UIKit.Place(bandL.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-130, -60), new Vector2(44, 240));
            var bandR = UIKit.Panel(chestBox, "BandR", Palette.UiGold, 8);
            UIKit.Place(bandR.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(130, -60), new Vector2(44, 240));

            // Kapak ayrı bir parça: açılırken yukarı fırlar
            chestLid = Box(chestBox, "Lid", new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(430, 140));
            var lidBody = UIKit.Panel(chestLid, "LidBody", Palette.Hex("#C97A3A"), 50);
            UIKit.Stretch(lidBody.rectTransform);
            var lidL = UIKit.Panel(chestLid, "LidBandL", Palette.UiGold, 8);
            UIKit.Place(lidL.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-130, 0), new Vector2(44, 140));
            var lidR = UIKit.Panel(chestLid, "LidBandR", Palette.UiGold, 8);
            UIKit.Place(lidR.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(130, 0), new Vector2(44, 140));
            UIKit.Dot(chestLid, "Lock", Palette.UiGoldDark, 70, new Vector2(0, -50));

            // Ödül
            var reward = Box(chestRoot.transform, "Reward", new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(500, 140));
            chestRewardGroup = reward.gameObject.AddComponent<CanvasGroup>();
            UIKit.Coin(reward, 110, new Vector2(-130, 0), new Vector2(0.5f, 0.5f));
            chestRewardText = TextAt(reward, "+100", 110, Palette.UiGold, new Vector2(70, 0), new Vector2(320, 140), TextAnchor.MiddleCenter, true);

            var collect = UIKit.CandyButton(chestRoot.transform, "Collect", Palette.UiGreen, Palette.UiGreenEdge, 60, new Vector2(560, 170), () =>
            {
                chestRoot.SetActive(false);
                var done = chestDone;
                chestDone = null;
                done?.Invoke();
            }, out var face);
            UIKit.Place((RectTransform)collect.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -520), new Vector2(560, 170));
            TextAt(face, "TOPLA", 70, Color.white, Vector2.zero, new Vector2(520, 120), TextAnchor.MiddleCenter, true);
            chestButtonGroup = collect.gameObject.AddComponent<CanvasGroup>();

            chestRoot.SetActive(false);
        }

        /// <summary>Sandığı açar: sallanır, kapak fırlar, ödül çıkar, coinler sayaca uçar.</summary>
        public void ShowChest(Action done)
        {
            chestDone = done;
            Progression.Track(MissionType.Chest);
            int reward = UnityEngine.Random.Range(6, 17) * 10;
            chestRoot.SetActive(true);
            StartCoroutine(ChestRoutine(reward));
        }

        IEnumerator ChestRoutine(int reward)
        {
            int before = SaveData.Coins;
            SaveData.Coins += reward;
            chestRewardText.text = $"+{reward}";
            chestRewardGroup.alpha = 0;
            chestButtonGroup.alpha = 0;
            chestButtonGroup.blocksRaycasts = false;
            chestRays.gameObject.SetActive(false);
            chestLid.anchoredPosition = new Vector2(0, 100);
            chestLid.localRotation = Quaternion.identity;
            chestBox.localScale = Vector3.zero;

            yield return Tween.Run(0.4f, k => chestBox.localScale = Vector3.one * k, Ease.OutBack, true);
            // Giderek şiddetlenen sallanma
            for (int i = 0; i < 3; i++)
            {
                float power = 6f + i * 5f;
                Sfx.Play(Sfx.Sound.Pop, 0.6f, 0.8f + i * 0.15f);
                yield return Tween.Run(0.28f, k => chestBox.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 4f) * power * (1f - k)), null, true);
                yield return new WaitForSecondsRealtime(0.12f);
            }
            chestBox.localRotation = Quaternion.identity;

            // Patlama: kapak uçar, ışıklar döner
            Sfx.Play(Sfx.Sound.Win, 0.9f);
            chestRays.gameObject.SetActive(true);
            StartCoroutine(SpinRays());
            StartCoroutine(Tween.Run(0.5f, k =>
            {
                chestLid.anchoredPosition = new Vector2(80f * k, 100f + 260f * k);
                chestLid.localRotation = Quaternion.Euler(0, 0, -35f * k);
            }, Ease.OutCubic, true));
            yield return Tween.PunchScale(chestBox, new Vector3(0.15f, -0.12f, 0), 0.3f, 1);
            yield return Tween.Run(0.3f, k =>
            {
                chestRewardGroup.alpha = k;
                chestRewardGroup.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.5f, 1f, k);
            }, Ease.OutBack, true);

            StartCoroutine(FlyCoins(before, SaveData.Coins, (RectTransform)chestRewardGroup.transform));
            yield return new WaitForSecondsRealtime(0.4f);
            chestButtonGroup.blocksRaycasts = true;
            yield return Tween.Run(0.25f, k => chestButtonGroup.alpha = k, null, true);
        }

        IEnumerator SpinRays()
        {
            while (chestRoot.activeSelf)
            {
                chestRays.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 25f);
                yield return null;
            }
        }

        /// <summary>Editör önizlemesi için: sandığın açılmış hâli.</summary>
        public void PreviewChest()
        {
            chestRoot.SetActive(true);
            chestRewardText.text = "+120";
            chestLid.anchoredPosition = new Vector2(80, 360);
            chestLid.localRotation = Quaternion.Euler(0, 0, -35);
        }

        // ================= AYARLAR =================

        void BuildSettings()
        {
            settingsRoot = Overlay("Settings", out var card, new Vector2(780, 700));
            TextAt(card, "AYARLAR", 64, Palette.UiDark, new Vector2(0, 260), new Vector2(700, 90));
            var sound = UIKit.CandyButton(card, "Sound", Palette.UiBlue, Palette.UiBlueEdge, 50, new Vector2(580, 140), () =>
            {
                SaveData.Sound = !SaveData.Sound;
                RefreshSettings();
                RefreshSound();
            }, out var sf);
            UIKit.Place((RectTransform)sound.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(580, 140));
            settingsSound = UIKit.Label(sf, "", 50, Color.white, TextAnchor.MiddleCenter, true);
            var close = UIKit.CandyButton(card, "Close", Palette.UiGreen, Palette.UiGreenEdge, 50, new Vector2(580, 140), () => ShowSettings(false), out var cf);
            UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(580, 140));
            UIKit.Label(cf, "KAPAT", 50, Color.white, TextAnchor.MiddleCenter, true);
            TextAt(card, "Kadirify Games  •  v0.1.0", 28, Palette.UiGray, new Vector2(0, -250), new Vector2(700, 50));
            RefreshSettings();
        }

        void RefreshSettings()
        {
            if (settingsSound != null) settingsSound.text = SaveData.Sound ? "SES: AÇIK" : "SES: KAPALI";
        }

        public void ShowSettings(bool on)
        {
            RefreshSettings();
            settingsRoot.SetActive(on);
        }
    }
}
