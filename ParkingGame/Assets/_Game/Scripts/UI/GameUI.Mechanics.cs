using UnityEngine;
using UnityEngine.UI;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// GameUI'nin mekanik parçası: boss süre sayacı ve renkli kapı göstergesi (HUD içinde).
    /// </summary>
    public partial class GameUI
    {
        GameObject timerPill, gatePill, timerShadow, gateShadow; // gölgeler ayrı nesne: pille birlikte gizlenmeli
        Text timerText;
        Image timerBg;
        Image gateNow, gateNext;
        RectTransform gateFill;
        float lastTimerSecond = -1f;

        void BuildMechanicsHud()
        {
            var top = new Vector2(0.5f, 1f);

            // Boss sayacı
            var tp = UIKit.Panel(hudRoot, "Timer", Palette.Hex("#FF5A5F"), 36);
            UIKit.Place(tp.rectTransform, top, top, new Vector2(0, -330), new Vector2(260, 76));
            timerShadow = UIKit.ShadowFor(tp.rectTransform, 0.14f, 8f, 14f).gameObject;
            timerBg = tp;
            // Saat simgesi
            UIKit.Dot(tp.transform, "Clock", Color.white, 50, new Vector2(-82, 0));
            UIKit.Dot(tp.transform, "ClockFace", Palette.Hex("#FF5A5F"), 38, new Vector2(-82, 0));
            var hand = UIKit.Panel(tp.transform, "Hand", Color.white, 2);
            UIKit.Place(hand.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(-82, 0), new Vector2(5, 15));
            var hand2 = UIKit.Panel(tp.transform, "Hand2", Color.white, 2);
            UIKit.Place(hand2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(-82, 0), new Vector2(5, 11));
            hand2.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            timerText = UIKit.Label(tp.transform, "1:00", 46, Color.white);
            timerText.rectTransform.offsetMin = new Vector2(50, 0);
            timerPill = tp.gameObject;
            timerPill.SetActive(false);
            timerShadow.SetActive(false);

            // Renkli kapı göstergesi: şimdiki renk (büyük) → sıradaki renk (küçük) + süre çubuğu
            var gp = UIKit.Panel(hudRoot, "Gate", Color.white, 36);
            UIKit.Place(gp.rectTransform, top, top, new Vector2(0, -415), new Vector2(330, 76));
            gateShadow = UIKit.ShadowFor(gp.rectTransform, 0.14f, 8f, 14f).gameObject;
            var label = UIKit.Label(gp.transform, "KAPI", 28, Palette.UiGrayEdge, TextAnchor.MiddleLeft);
            label.rectTransform.offsetMin = new Vector2(24, 8);
            gateNow = UIKit.Dot(gp.transform, "Now", Color.red, 54, new Vector2(10, 6));
            var arrow = UIKit.Label(gp.transform, "›", 44, Palette.UiGray);
            arrow.rectTransform.offsetMin = new Vector2(160, 10);
            gateNext = UIKit.Dot(gp.transform, "Next", Color.blue, 34, new Vector2(120, 6));
            gateFill = ProgressBar(gp.transform, new Vector2(40, -26), new Vector2(240, 14), Palette.UiDark);
            gatePill = gp.gameObject;
            gatePill.SetActive(false);
            gateShadow.SetActive(false);
        }

        /// <summary>Boss süresi. show = false ise gizler.</summary>
        public void SetTimer(bool show, float seconds)
        {
            timerPill.SetActive(show);
            timerShadow.SetActive(show);
            if (!show) return;
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            timerText.text = $"{s / 60}:{s % 60:00}";
            bool urgent = seconds <= 15f;
            timerBg.color = urgent ? Palette.Hex("#E11D48") : Palette.Hex("#FF5A5F");
            // Son 15 saniyede her saniye kısa bir zıplama ve tık sesi
            if (urgent && Application.isPlaying && s != (int)lastTimerSecond)
            {
                StartCoroutine(Tween.PunchScale(timerPill.transform, new Vector3(0.12f, 0.12f, 0), 0.25f, 1));
                Sfx.Play(Sfx.Sound.Pop, 0.35f, 1.4f);
            }
            lastTimerSecond = s;
        }

        /// <summary>Kapının şu anki ve sıradaki rengi; progress = sıradaki renge geçişe kalan oran.</summary>
        public void SetGate(bool show, Color now, Color next, float progress)
        {
            gatePill.SetActive(show);
            gateShadow.SetActive(show);
            if (!show) return;
            gateNow.color = now;
            gateNext.color = next;
            SetFill(gateFill, 1f - progress);
        }
    }
}
