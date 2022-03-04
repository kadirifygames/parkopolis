using System;
using System.Collections;
using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Yumuşatma eğrileri. Bir hareketin "nasıl" hızlanıp yavaşlayacağını belirler.
    /// </summary>
    public static class Ease
    {
        public static float Linear(float t) => t;
        public static float InQuad(float t) => t * t;
        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float OutElastic(float t)
        {
            const float c4 = 2f * Mathf.PI / 3f;
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
    }

    /// <summary>
    /// Basit animasyon aracı. StartCoroutine(Tween.Run(...)) ile kullanılır.
    /// Coroutine: birden fazla kareye yayılan fonksiyon. "yield return null" = bir sonraki kareyi bekle.
    /// </summary>
    public static class Tween
    {
        public static IEnumerator Run(float duration, Action<float> step, Func<float, float> ease = null, bool unscaled = false)
        {
            ease ??= Ease.Linear;
            float t = 0f;
            while (t < duration)
            {
                step(ease(Mathf.Clamp01(t / duration)));
                yield return null;
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }
            step(ease(1f));
        }

        public static IEnumerator Delay(float seconds, Action then, bool unscaled = false)
        {
            if (unscaled) yield return new WaitForSecondsRealtime(seconds);
            else yield return new WaitForSeconds(seconds);
            then?.Invoke();
        }

        /// <summary>Nesneyi bir yöne itip yay gibi titreterek geri getirir.</summary>
        public static IEnumerator Punch(Transform tr, Vector3 offset, float duration, int vibrato = 3)
        {
            Vector3 start = tr.localPosition;
            float t = 0f;
            while (t < duration)
            {
                float k = t / duration;
                float decay = 1f - k;
                tr.localPosition = start + offset * (Mathf.Sin(k * Mathf.PI * vibrato) * decay);
                yield return null;
                t += Time.deltaTime;
            }
            tr.localPosition = start;
        }

        /// <summary>Ölçeği bir an değiştirip esnek şekilde geri döndürür (squash and stretch).</summary>
        public static IEnumerator PunchScale(Transform tr, Vector3 amount, float duration, int vibrato = 2)
        {
            Vector3 start = Vector3.one;
            float t = 0f;
            while (t < duration)
            {
                float k = t / duration;
                float decay = 1f - k;
                tr.localScale = start + amount * (Mathf.Sin(k * Mathf.PI * vibrato) * decay);
                yield return null;
                t += Time.deltaTime;
            }
            tr.localScale = start;
        }
    }
}
