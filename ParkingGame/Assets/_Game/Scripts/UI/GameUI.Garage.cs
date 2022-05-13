using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// GameUI'nin dördüncü parçası: Garaj ekranı (özel arabalar + boya paletleri).
    /// Kartlardaki 3D görüntüler, sahnenin görünmeyen bir köşesindeki "stüdyoda" çekilir.
    /// </summary>
    public partial class GameUI
    {
        /// <summary>Garajda bir şey alınınca/seçilince: level yeniden kurulur ki yeni görünüm hemen gelsin.</summary>
        public Action OnGarageChanged;

        GameObject garageRoot;
        RectTransform garageContent;
        ScrollRect garageScroll;
        Text garageCoins;
        Image tabCarsFace, tabPaintsFace;
        Text tabCarsText, tabPaintsText;
        bool garageShowPaints;
        bool garageDirty;
        PreviewStudio studio;
        readonly Dictionary<string, RenderTexture> previews = new Dictionary<string, RenderTexture>();

        static readonly Color StudioBg = Palette.Hex("#EEF3FA");

        // ================= Fotoğraf stüdyosu =================

        class PreviewStudio
        {
            readonly Transform stage;
            readonly Camera cam;

            public PreviewStudio()
            {
                stage = new GameObject("GarageStudio").transform;
                stage.position = new Vector3(0, -500f, 0); // oyuncunun hiç görmediği yer
                // Kaide
                Shape.Part("Pedestal", stage, MeshFactory.Cylinder(1.35f, 0.12f, 40), Mats.Lit(Palette.Hex("#DCE4F0"), 0.5f), new Vector3(0, -0.06f, 0));
                Shape.Part("PedestalRim", stage, MeshFactory.Cylinder(1.42f, 0.06f, 40), Mats.Lit(Palette.Hex("#C3CEDD"), 0.5f), new Vector3(0, -0.12f, 0));

                cam = new GameObject("GarageCam").AddComponent<Camera>();
                cam.transform.SetParent(stage, false);
                cam.transform.localPosition = new Vector3(2.5f, 2.0f, 3.1f);
                cam.transform.LookAt(stage.position + new Vector3(0, 0.3f, 0));
                cam.fieldOfView = 26f;
                cam.nearClipPlane = 0.5f;
                cam.farClipPlane = 20f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = StudioBg;
                cam.enabled = false; // sadece istediğimizde çeker
                var data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
            }

            public RenderTexture Shoot(Action<Transform> buildCar)
            {
                var holder = new GameObject("PreviewCar").transform;
                holder.SetParent(stage, false);
                holder.localRotation = Quaternion.Euler(0, 160f, 0); // ön taraf kameraya dönük
                buildCar(holder);

                var rt = new RenderTexture(440, 330, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                rt.Create();
                cam.targetTexture = rt;
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, request)) RenderPipeline.SubmitRenderRequest(cam, request);
                else cam.Render();
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(holder.gameObject);
                return rt;
            }
        }

        RenderTexture Preview(string key, Action<Transform> build)
        {
            if (previews.TryGetValue(key, out var rt) && rt != null) return rt;
            studio ??= new PreviewStudio();
            rt = studio.Shoot(build);
            previews[key] = rt;
            return rt;
        }

        // ================= Ekran =================

        void BuildGarage()
        {
            garageRoot = FullScreen(transform, "Garage", Palette.Hex("#E3EEFF"));
            var area = UIKit.Rect("Safe", garageRoot.transform);
            UIKit.Stretch(area);
            if (!isPreview) area.gameObject.AddComponent<SafeArea>();

            // Kaydırılabilir kart ızgarası
            var viewport = UIKit.Rect("Viewport", area);
            UIKit.Stretch(viewport);
            viewport.offsetMin = new Vector2(0, 30);
            viewport.offsetMax = new Vector2(0, -360);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0);
            garageContent = UIKit.Rect("Content", viewport);
            garageContent.anchorMin = new Vector2(0, 1);
            garageContent.anchorMax = new Vector2(1, 1);
            garageContent.pivot = new Vector2(0.5f, 1);
            garageScroll = area.gameObject.AddComponent<ScrollRect>();
            garageScroll.viewport = viewport;
            garageScroll.content = garageContent;
            garageScroll.horizontal = false;
            garageScroll.scrollSensitivity = 30f;

            // Başlık
            var header = UIKit.Panel(area, "Header", Color.white, 50);
            UIKit.Place(header.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1010, 180));
            UIKit.ShadowFor(header.rectTransform);
            var back = UIKit.CandyButton(header.transform, "Back", Palette.UiBlue, Palette.UiBlueEdge, 36, new Vector2(120, 120), CloseGarage, out var backFace);
            UIKit.Place((RectTransform)back.transform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90, 4), new Vector2(120, 120));
            foreach (float s in new[] { 1f, -1f })
            {
                var arrow = UIKit.Panel(backFace, "Arrow", Color.white, 6);
                UIKit.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-4, 12 * s), new Vector2(16, 48));
                arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -45 * s);
            }
            TextAt(header.transform, "GARAJ", 70, Palette.UiDark, new Vector2(-40, 4), new Vector2(400, 100));
            var coins = UIKit.Panel(header.transform, "Coins", Palette.Hex("#FFF6DB"), 40);
            UIKit.Place(coins.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 4), new Vector2(250, 90));
            UIKit.Coin(coins.transform, 62, new Vector2(46, 0), new Vector2(0, 0.5f));
            garageCoins = UIKit.Label(coins.transform, "0", 42, Palette.UiDark, TextAnchor.MiddleRight);
            garageCoins.rectTransform.offsetMax = new Vector2(-26, 0);

            // Sekmeler
            var t1 = UIKit.CandyButton(area, "TabCars", Palette.UiBlue, Palette.UiBlueEdge, 40, new Vector2(440, 100), () => { garageShowPaints = false; RefreshGarage(); }, out var f1);
            UIKit.Place((RectTransform)t1.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(-235, -285), new Vector2(440, 100));
            tabCarsFace = f1.GetComponent<Image>();
            tabCarsText = UIKit.Label(f1, "ARABALAR", 42, Color.white);
            var t2 = UIKit.CandyButton(area, "TabPaints", Color.white, Palette.UiWhiteEdge, 40, new Vector2(440, 100), () => { garageShowPaints = true; RefreshGarage(); }, out var f2);
            UIKit.Place((RectTransform)t2.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(235, -285), new Vector2(440, 100));
            tabPaintsFace = f2.GetComponent<Image>();
            tabPaintsText = UIKit.Label(f2, "BOYALAR", 42, Palette.UiDark);

            garageRoot.SetActive(false);
        }

        public void ShowGaragePaints()
        {
            ShowGarage();
            garageShowPaints = true;
            RefreshGarage();
        }

        public void ShowGarage()
        {
            garageShowPaints = false;
            garageRoot.SetActive(true);
            menuRoot.SetActive(false);
            garageDirty = false;
            RefreshGarage();
            garageScroll.verticalNormalizedPosition = 1f;
        }

        void CloseGarage()
        {
            garageRoot.SetActive(false);
            if (garageDirty) OnGarageChanged?.Invoke();
            ShowMainMenu(currentLevel);
        }

        void RefreshGarage()
        {
            garageCoins.text = SaveData.Coins.ToString();
            menuCoins.text = SaveData.Coins.ToString();
            tabCarsFace.color = garageShowPaints ? Color.white : Palette.UiBlue;
            tabCarsText.color = garageShowPaints ? Palette.UiDark : Color.white;
            tabPaintsFace.color = garageShowPaints ? Palette.UiBlue : Color.white;
            tabPaintsText.color = garageShowPaints ? Color.white : Palette.UiDark;

            var old = new List<GameObject>();
            foreach (Transform child in garageContent) old.Add(child.gameObject);
            foreach (var go in old)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }

            int count = garageShowPaints ? Garage.Paints.Length : Garage.Models.Length;
            int rows = (count + 1) / 2;
            garageContent.sizeDelta = new Vector2(0, rows * 610f + 40f);
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = new Vector2(i % 2 == 0 ? -250f : 250f, -320f - (i / 2) * 610f);
                if (garageShowPaints) PaintCard(Garage.Paints[i], pos);
                else ModelCard(Garage.Models[i], pos);
            }
        }

        RectTransform Card(Vector2 pos, out RectTransform previewArea)
        {
            var card = UIKit.Panel(garageContent, "Card", Color.white, 46);
            card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(0.5f, 1);
            card.rectTransform.anchoredPosition = pos;
            card.rectTransform.sizeDelta = new Vector2(480, 580);
            var pa = UIKit.Panel(card.transform, "PreviewBg", StudioBg, 34);
            UIKit.Place(pa.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(440, 330));
            previewArea = pa.rectTransform;
            return card.rectTransform;
        }

        static void ShowTexture(RectTransform area, Texture tex)
        {
            var raw = UIKit.Rect("Photo", area);
            UIKit.Stretch(raw);
            raw.offsetMin = new Vector2(6, 6);
            raw.offsetMax = new Vector2(-6, -6);
            var img = raw.gameObject.AddComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
        }

        static string SpeedText(int cls) => cls switch { 3 => "Hız: ●●●●", 0 => "Hız: ●●●○", 1 => "Hız: ●●○○", _ => "Hız: ●○○○" };

        void ModelCard(ModelItem item, Vector2 pos)
        {
            var card = Card(pos, out var area);
            var tex = Preview("model_" + item.model, t => CarBuilder.Build(t, 2, 1f, Color.white, item.speedClass, item.model));
            ShowTexture(area, tex);
            TextAt(card, item.name, 44, Palette.UiDark, new Vector2(0, -90), new Vector2(440, 60));
            TextAt(card, SpeedText(item.speedClass), 28, Palette.UiGray, new Vector2(0, -135), new Vector2(440, 44));

            if (Garage.Owns(item.model))
                Tag(card, "SAHİPSİN", Palette.Hex("#DCFCE7"), Palette.UiGreenEdge);
            else
                PriceButton(card, item.price, () =>
                {
                    if (Garage.Buy(item)) Bought();
                    else ShowToast("Yeterli coin yok!", 1.5f);
                });
        }

        void PaintCard(PaintItem paint, Vector2 pos)
        {
            var card = Card(pos, out var area);
            var tex = Preview("paint_" + paint.id, t =>
            {
                // Paletten iki araba: biri önde, biri arkada
                CarBuilder.Build(t, 2, 1f, paint.colors[0], 0, CarModel.None, paint.smoothness, paint.metallic);
                var second = new GameObject("Second").transform;
                second.SetParent(t, false);
                second.localPosition = new Vector3(-0.95f, 0, -0.4f);
                CarBuilder.Build(second, 2, 1f, paint.colors[5], 3, CarModel.None, paint.smoothness, paint.metallic);
            });
            ShowTexture(area, tex);
            TextAt(card, paint.name, 44, Palette.UiDark, new Vector2(0, -90), new Vector2(440, 60));
            for (int i = 0; i < paint.colors.Length; i++)
            {
                var dot = UIKit.Dot(card, "Swatch", paint.colors[i], 30, new Vector2((i - 4.5f) * 40f, -135f));
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            }

            bool owned = Garage.OwnsPaint(paint.id);
            bool selected = Garage.SelectedPaint.id == paint.id;
            if (selected) Tag(card, "SEÇİLİ", Palette.Hex("#DCFCE7"), Palette.UiGreenEdge);
            else if (owned)
            {
                var b = UIKit.CandyButton(card, "Select", Palette.UiBlue, Palette.UiBlueEdge, 36, new Vector2(300, 90), () =>
                {
                    Garage.SelectedPaintId = paint.id;
                    garageDirty = true;
                    Sfx.Play(Sfx.Sound.Pop);
                    RefreshGarage();
                }, out var f);
                UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -220), new Vector2(300, 90));
                UIKit.Label(f, "SEÇ", 44, Color.white, TextAnchor.MiddleCenter, true);
            }
            else PriceButton(card, paint.price, () =>
            {
                if (Garage.BuyPaint(paint)) Bought();
                else ShowToast("Yeterli coin yok!", 1.5f);
            });
        }

        void Tag(RectTransform card, string text, Color bg, Color fg)
        {
            var tag = UIKit.Panel(card, "Tag", bg, 30);
            UIKit.Place(tag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -220), new Vector2(300, 80));
            UIKit.Label(tag.transform, text, 38, fg);
        }

        void PriceButton(RectTransform card, int price, Action buy)
        {
            bool afford = SaveData.Coins >= price;
            var b = UIKit.CandyButton(card, "Buy", afford ? Palette.UiGold : Palette.Hex("#E2E8F0"),
                afford ? Palette.UiGoldDark : Palette.Hex("#CBD5E1"), 36, new Vector2(300, 90), buy, out var f);
            UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -220), new Vector2(300, 90));
            UIKit.Coin(f, 54, new Vector2(56, 0), new Vector2(0, 0.5f));
            var t = UIKit.Label(f, price.ToString(), 44, afford ? Palette.UiDark : Palette.UiGray, TextAnchor.MiddleCenter);
            t.rectTransform.offsetMin = new Vector2(60, 0);
        }

        void Bought()
        {
            garageDirty = true;
            Sfx.Play(Sfx.Sound.Win, 0.8f);
            SetCoins(SaveData.Coins);
            RefreshGarage();
            ShowToast("Garaja eklendi!\nArtık otoparklarda görünecek.", 2f);
        }
    }
}
