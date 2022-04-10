using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Arayüz parçalarını (panel, yazı, buton) kodla üretmek için küçük araç kutusu.
    /// Yuvarlak köşeli şekiller de burada üretilir.
    /// </summary>
    public static class UIKit
    {
        const int SpriteSize = 128;
        const int SpriteRadius = 60;
        static Sprite rounded, circle;
        static Font font;

        static Sprite softShadow;
        const int ShadowBlur = 28;

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                // Assets/_Game/Resources/Fonts/GameFont.ttf varsa onu kullan, yoksa Unity'nin varsayılanı
                font = Resources.Load<Font>("Fonts/GameFont");
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>Kenarları yumuşakça dağılan gölge (kartların altına).</summary>
        public static Sprite SoftShadow => softShadow != null ? softShadow : (softShadow = MakeSoftShadow());

        static Sprite MakeSoftShadow()
        {
            int size = 160, radius = 40, blur = ShadowBlur;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            int inset = blur;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float lo = inset + radius, hi = size - inset - radius;
                float qx = Mathf.Max(lo - fx, fx - hi, 0f);
                float qy = Mathf.Max(lo - fy, fy - hi, 0f);
                float d = Mathf.Sqrt(qx * qx + qy * qy) - radius;
                float a = 1f - Mathf.SmoothStep(-blur * 0.5f, blur, d);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            float border = inset + radius;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        /// <summary>Hedef öğenin arkasına yumuşak gölge koyar (Place'ten sonra çağır).</summary>
        public static Image ShadowFor(RectTransform target, float alpha = 0.16f, float yOffset = 14f, float spread = 22f)
        {
            var rt = Rect(target.name + "Shadow", target.parent);
            rt.SetSiblingIndex(target.GetSiblingIndex());
            rt.anchorMin = target.anchorMin;
            rt.anchorMax = target.anchorMax;
            rt.pivot = target.pivot;
            rt.anchoredPosition = target.anchoredPosition + new Vector2(0, -yOffset);
            rt.sizeDelta = target.sizeDelta + new Vector2(spread * 2f, spread * 2f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = SoftShadow;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.08f, 0.12f, 0.22f, alpha);
            img.raycastTarget = false;
            return img;
        }
        public static Sprite Rounded => rounded != null ? rounded : (rounded = MakeSprite(SpriteSize, SpriteRadius, true));
        public static Sprite Circle => circle != null ? circle : (circle = MakeSprite(SpriteSize, SpriteSize / 2, false));

        static Sprite MakeSprite(int size, int radius, bool sliced)
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
                float fx = x + 0.5f, fy = y + 0.5f;
                float qx = Mathf.Max(radius - fx, fx - (size - radius), 0f);
                float qy = Mathf.Max(radius - fy, fy - (size - radius), 0f);
                float d = Mathf.Sqrt(qx * qx + qy * qy) - radius;
                byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            var border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Yuvarlak köşeli renkli panel.</summary>
        public static Image Panel(Transform parent, string name, Color color, float corner, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = SpriteRadius / Mathf.Max(1f, corner);
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Dot(Transform parent, string name, Color color, float size, Vector2 pos)
        {
            var rt = Rect(name, parent);
            Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Circle;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool shadow = false)
        {
            var rt = Rect("Text", parent);
            Stretch(rt);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            // Fredoka zaten kalın üretildi; yapay "Bold" eklemeye gerek yok
            t.fontStyle = Font.name == "GameFont" ? FontStyle.Normal : FontStyle.Bold;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0, 0, 0.3f);
                s.effectDistance = new Vector2(0, -5);
            }
            return t;
        }

        /// <summary>
        /// "Şeker" görünümlü 3D buton: altta koyu bir kenar, üstte renkli yüz.
        /// Basınca yüz aşağı iner, tıklama hissi verir.
        /// </summary>
        public static Button CandyButton(Transform parent, string name, Color face, Color edge, float corner, Vector2 size, Action onClick, out RectTransform content)
        {
            var root = Rect(name, parent);
            root.sizeDelta = size;
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0);
            var btn = root.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;

            var shadowRt = Rect("Shadow", root);
            Stretch(shadowRt);
            shadowRt.offsetMin = new Vector2(-22, -40);
            shadowRt.offsetMax = new Vector2(22, 4);
            var shadowImg = shadowRt.gameObject.AddComponent<Image>();
            shadowImg.sprite = SoftShadow;
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = new Color(0.08f, 0.12f, 0.22f, 0.2f);
            shadowImg.raycastTarget = false;

            var edgeImg = Panel(root, "Edge", edge, corner);
            Stretch(edgeImg.rectTransform);
            edgeImg.rectTransform.offsetMin = new Vector2(0, -12);
            edgeImg.rectTransform.offsetMax = new Vector2(0, -4);

            var faceImg = Panel(root, "Face", face, corner);
            Stretch(faceImg.rectTransform);

            var gloss = Panel(faceImg.transform, "Gloss", new Color(1, 1, 1, 0.22f), corner * 0.7f);
            gloss.rectTransform.anchorMin = new Vector2(0, 0.55f);
            gloss.rectTransform.anchorMax = new Vector2(1, 1);
            gloss.rectTransform.offsetMin = new Vector2(14, 0);
            gloss.rectTransform.offsetMax = new Vector2(-14, -10);

            content = faceImg.rectTransform;
            var fx = root.gameObject.AddComponent<PressFx>();
            fx.face = faceImg.rectTransform;
            btn.onClick.AddListener(() =>
            {
                Sfx.Play(Sfx.Sound.Pop, 0.7f);
                onClick?.Invoke();
            });
            return btn;
        }

        /// <summary>Kalp ikonu: iki daire + 45° döndürülmüş kare.</summary>
        public static RectTransform Heart(Transform parent, float size, Color color)
        {
            var rt = Rect("Heart", parent);
            rt.sizeDelta = new Vector2(size, size);
            Dot(rt, "L", color, size * 0.58f, new Vector2(-size * 0.2f, size * 0.12f));
            Dot(rt, "R", color, size * 0.58f, new Vector2(size * 0.2f, size * 0.12f));
            var tip = Panel(rt, "Tip", color, size * 0.08f);
            Place(tip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -size * 0.08f), new Vector2(size * 0.56f, size * 0.56f));
            tip.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            Dot(rt, "Shine", new Color(1, 1, 1, 0.55f), size * 0.16f, new Vector2(-size * 0.22f, size * 0.2f));
            return rt;
        }

        /// <summary>Altın coin ikonu.</summary>
        public static RectTransform Coin(Transform parent, float size, Vector2 pos, Vector2 anchor)
        {
            var rt = Rect("Coin", parent);
            Place(rt, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            Dot(rt, "Outer", Palette.UiGoldDark, size, Vector2.zero);
            Dot(rt, "Inner", Palette.UiGold, size * 0.76f, Vector2.zero);
            Dot(rt, "Mark", Palette.UiGoldDark, size * 0.36f, Vector2.zero);
            Dot(rt, "Glint", new Color(1, 1, 1, 0.85f), size * 0.18f, new Vector2(-size * 0.2f, size * 0.2f));
            return rt;
        }
    }

    /// <summary>Butona basınca hafifçe içeri gömülme efekti.</summary>
    public class PressFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform face;

        public void OnPointerDown(PointerEventData e) => Set(true);
        public void OnPointerUp(PointerEventData e) => Set(false);
        public void OnPointerExit(PointerEventData e) => Set(false);

        void Set(bool down)
        {
            if (face == null) return;
            face.offsetMin = new Vector2(0, down ? -8 : 0);
            face.offsetMax = new Vector2(0, down ? -8 : 0);
        }
    }

    /// <summary>Çentikli telefonlarda arayüzün kamera deliğinin altında kalmaması için güvenli alan.</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect last;

        void Update()
        {
            var sa = Screen.safeArea;
            if (sa == last || Screen.width == 0) return;
            last = sa;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
