using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Sahnedeki tek bir araba. Hareket, çarpma ve çıkış animasyonlarını yönetir.
    /// Hangi hareketin yapılacağına LevelController karar verir; Car sadece uygular.
    /// </summary>
    public class Car : MonoBehaviour
    {
        public int Id { get; private set; }
        public CarSpec Spec { get; private set; }
        public bool Busy { get; private set; }   // animasyon oynuyor
        public bool Gone { get; private set; }   // otoparktan ayrıldı

        public Vector3 Axis => Spec.horizontal ? Vector3.right : Vector3.forward;

        // Yol hareketi
        public float MaxSpeed { get; private set; }   // arabanın kendi azami hızı
        public float Speed { get; private set; }      // şu anki hızı
        public bool OnRoad { get; private set; }      // yolda ilerliyor mu
        public float Length => length;
        float crashCooldownUntil;
        Car follow;                                   // çarptıktan sonra arkasından gittiği araba
        public bool CanCrash => OnRoad && Time.time > crashCooldownUntil;

        /// <summary>Tipine göre hız: spor > sedan > hatchback > minibüs > otobüs/kamyon.</summary>
        static float BaseSpeed(CarSpec s)
        {
            if (s.length >= 3) return 7.5f;
            return (s.variant % 4) switch { 3 => 14.5f, 0 => 12f, 1 => 11f, _ => 9.5f };
        }

        /// <summary>Arabanın ön, orta ve arka noktaları (çarpışma kontrolü için).</summary>
        public void Points(Vector3[] into)
        {
            Vector3 f = transform.forward * Mathf.Max(0f, length / 2f - 0.38f);
            into[0] = transform.position + f;
            into[1] = transform.position;
            into[2] = transform.position - f;
        }

        /// <summary>
        /// Yolda başka bir arabayla çarpıştı. Arkadaki yavaşlar ve öndekini takip etmeye başlar,
        /// öndeki hafif sarsılır. İkisi de yola devam eder.
        /// </summary>
        public void Crash(Vector3 pushDir, Car other, bool isRear)
        {
            crashCooldownUntil = Time.time + 1.2f;
            if (isRear)
            {
                Speed = Mathf.Min(Speed, 1.5f);
                follow = other;
            }
            else
            {
                Speed *= 0.85f;
            }
            if (visual != null)
            {
                StartCoroutine(Tween.Punch(visual, transform.InverseTransformDirection(pushDir) * 0.12f, 0.4f, 4));
                StartCoroutine(Tween.PunchScale(visual, new Vector3(0.06f, -0.05f, -0.1f), 0.3f));
            }
        }

        Transform visual;
        readonly List<Transform> wheels = new List<Transform>();
        BoxCollider hitBox;
        float length;
        float wheelAngle;
        Vector3 lastPos;
        bool pulsing;
        float puffTimer;
        float shakeUntil;

        public void Init(int id, CarSpec spec, float cell, int level)
        {
            Id = id;
            Spec = spec;
            // Garaj: seçili boya paleti ve satın alınan özel arabalar (sadece görünüm, hız aynı kalır)
            var paint = Garage.SelectedPaint;
            var built = CarBuilder.Build(transform, spec.length, cell, Garage.CarColor(spec.colorIndex), spec.variant,
                Garage.ModelFor(spec, level), paint.smoothness, paint.metallic);
            visual = built.root;
            wheels.AddRange(built.wheels);
            length = built.length;

            // Dokunmayı algılamak için görünmez kutu (Collider)
            hitBox = gameObject.AddComponent<BoxCollider>();
            MaxSpeed = BaseSpeed(spec) + ((id * 37) % 7 - 3) * 0.15f; // aynı tipteki arabalar da biraz farklı
            hitBox.size = new Vector3(1.0f, 0.9f, spec.length * cell - 0.05f);
            hitBox.center = new Vector3(0, 0.45f, 0);
            lastPos = transform.position;
        }

        void LateUpdate()
        {
            // Tekerlekleri, arabanın ilerlediği mesafeye göre döndür
            Vector3 delta = transform.position - lastPos;
            lastPos = transform.position;
            float forward = Vector3.Dot(delta, transform.forward);
            if (Mathf.Abs(forward) > 0.0001f)
            {
                wheelAngle += forward / 0.16f * Mathf.Rad2Deg;
                foreach (var w in wheels) w.localRotation = Quaternion.Euler(wheelAngle, 0, 0);

                // Hızlı giderken arkadan egzoz dumanı
                puffTimer -= Time.deltaTime;
                if (puffTimer <= 0f && delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f) > 4f)
                {
                    puffTimer = 0.09f;
                    Vector3 back = transform.position - transform.forward * Mathf.Sign(forward) * (length / 2f + 0.1f);
                    Fx.Puff(back + Vector3.up * 0.2f, 2);
                }
            }

            // İpucu verilen araba hafifçe "nefes alır"
            if (pulsing && !Busy && visual != null)
            {
                float s = 1f + Mathf.Sin(Time.time * 7f) * 0.035f;
                visual.localScale = new Vector3(s, s, s);
            }

            // Rölanti: park hâlindeki araba motoru çalışıyormuş gibi çok hafif titrer
            if (!Busy && !Gone && visual != null && Time.time > shakeUntil)
            {
                float idle = Mathf.Sin(Time.time * 22f + Id * 1.7f) * 0.0035f;
                visual.localPosition = new Vector3(0, idle, 0);
            }
        }

        public void SetPulse(bool on)
        {
            pulsing = on;
            if (!on && visual != null && !Busy) visual.localScale = Vector3.one;
        }

        /// <summary>Engele kadar git, çarp, geri sek.</summary>
        public void Bump(int sign, float travel, Car hitCar)
        {
            if (Busy) return;
            StartCoroutine(BumpRoutine(sign, travel, hitCar));
        }

        IEnumerator BumpRoutine(int sign, float travel, Car hitCar)
        {
            Busy = true;
            SetPulse(false);
            Vector3 start = transform.position;
            Vector3 dir = Axis * sign;
            Vector3 hitPos = start + dir * travel;

            // İleri atıl
            float goTime = 0.07f + travel / 16f;
            yield return Tween.Run(goTime, k => transform.position = Vector3.LerpUnclamped(start, hitPos, k), Ease.InQuad);

            // Çarpma anı
            Vector3 contact = hitPos + dir * (length / 2f + 0.05f) + Vector3.up * 0.35f;
            Sfx.Play(Sfx.Sound.Bump, 0.9f);
            Fx.Impact(contact);
            float alongForward = Vector3.Dot(dir, transform.forward);
            StartCoroutine(Tween.PunchScale(visual, new Vector3(0.08f, -0.06f, -0.14f), 0.35f));
            StartCoroutine(Tween.Punch(visual, new Vector3(0, 0, -0.04f * alongForward), 0.3f));
            if (hitCar != null) hitCar.Shake(dir);
            Handheld();

            // Geri sek
            float backTime = 0.3f + travel / 22f;
            yield return Tween.Run(backTime, k => transform.position = Vector3.LerpUnclamped(hitPos, start, k), Ease.OutBack);
            transform.position = start;
            Busy = false;
        }

        /// <summary>Başka bir araba çarpınca sarsıl, bazen korna çal.</summary>
        public void Shake(Vector3 worldDir)
        {
            if (Gone || visual == null) return;
            shakeUntil = Time.time + 0.45f;
            Vector3 local = transform.InverseTransformDirection(worldDir) * 0.09f;
            StartCoroutine(Tween.Punch(visual, local, 0.4f, 4));
            if (Random.value < 0.5f) Sfx.Play(Sfx.Sound.Horn, 0.55f);
        }

        /// <summary>Otoparktan çık ve yolu takip ederek uzaklaş.</summary>
        public void Exit(Vector3 lanePoint, List<Vector3> path, Barrier barrier, float gateZ)
        {
            StartCoroutine(ExitRoutine(lanePoint, path, null, null, barrier, gateZ));
        }

        /// <summary>
        /// Renkli kapılı çıkış: karar noktasına kadar git; kapı rengi uyuyorsa çık, uymuyorsa bir tur daha at.
        /// </summary>
        public void ExitGated(Vector3 lanePoint, RoadPath.GatedRoute route, System.Func<Car, bool> canPass, Barrier barrier, float gateZ)
        {
            StartCoroutine(ExitRoutine(lanePoint, route.entry, route, canPass, barrier, gateZ));
        }

        float carry; // bir yol parçasından diğerine taşan mesafe

        IEnumerator ExitRoutine(Vector3 lanePoint, List<Vector3> path, RoadPath.GatedRoute route, System.Func<Car, bool> canPass, Barrier barrier, float gateZ)
        {
            Busy = true;
            Gone = true;
            SetPulse(false);
            hitBox.enabled = false;
            Sfx.Play(Sfx.Sound.Whoosh, 0.8f);
            StartCoroutine(Tween.PunchScale(visual, new Vector3(-0.05f, 0.06f, 0.1f), 0.3f));

            OnRoad = true;

            // 1) Otoparktan yola, düz çizgide
            Vector3 start = transform.position;
            Vector3 toLane = lanePoint - start;
            float d = toLane.magnitude;
            Vector3 dir = d > 0.001f ? toLane / d : transform.forward;
            Speed = 3f;
            float s = 0f;
            while (s < d)
            {
                Speed = Mathf.Min(Speed + 35f * Time.deltaTime, MaxSpeed);
                s += Speed * Time.deltaTime;
                transform.position = start + dir * Mathf.Min(s, d);
                yield return null;
            }
            carry = s - d;

            // 2) Yolu takip et
            if (route == null)
            {
                yield return Follow(path, barrier, gateZ, true);
            }
            else
            {
                yield return Follow(route.entry, null, gateZ, false);
                // Karar noktası: kapı izin verene kadar tur at
                while (!canPass(this))
                    yield return Follow(route.loop, null, gateZ, false);
                yield return Follow(route.exitTail, barrier, gateZ, true);
            }
            OnRoad = false;
            Destroy(gameObject);
        }

        /// <summary>Bir yol parçasını takip eder; virajlarda yumuşakça döner. final = son parça (sonunda küçülerek kaybolur).</summary>
        IEnumerator Follow(List<Vector3> path, Barrier barrier, float gateZ, bool final)
        {
            var cumulative = RoadPath.Cumulative(path);
            float total = cumulative[cumulative.Length - 1];
            float t = carry;
            while (t < total)
            {
                // Önünde takip ettiği araba yakınsa onun hızına uy
                float target = MaxSpeed;
                if (follow != null && follow.OnRoad &&
                    Vector3.Distance(follow.transform.position, transform.position) < (follow.Length + length) * 0.5f + 1.6f)
                    target = Mathf.Min(target, follow.Speed);
                Speed = Mathf.MoveTowards(Speed, target, 18f * Time.deltaTime);
                t += Speed * Time.deltaTime;
                Vector3 p = RoadPath.Sample(path, cumulative, Mathf.Min(t, total));
                Vector3 ahead = RoadPath.Sample(path, cumulative, t + 0.4f) - p;
                ahead.y = 0;
                transform.position = p;
                if (ahead.sqrMagnitude > 1e-5f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(ahead), 540f * Time.deltaTime);
                if (barrier != null && p.z > gateZ - 2.5f) barrier.Ping();

                if (final)
                {
                    float remain = total - t;
                    if (remain < 2.5f) transform.localScale = Vector3.one * Mathf.Clamp01(remain / 2.5f);
                }
                yield return null;
            }
            carry = t - total;
        }

        /// <summary>
        /// Buzlu zeminde: önü kapalı araba engele kadar kayar ve orada KALIR (yeni park yeri).
        /// </summary>
        public void Slide(CarSpec newSpec, Vector3 target, Car hitCar)
        {
            StartCoroutine(SlideRoutine(newSpec, target, hitCar));
        }

        IEnumerator SlideRoutine(CarSpec newSpec, Vector3 target, Car hitCar)
        {
            Busy = true;
            SetPulse(false);
            Spec = newSpec;
            Vector3 start = transform.position;
            Quaternion rot = transform.rotation;
            Sfx.Play(Sfx.Sound.Whoosh, 0.6f, 1.5f);
            float dist = Vector3.Distance(start, target);
            yield return Tween.Run(0.18f + dist / 10f, k =>
            {
                transform.position = Vector3.LerpUnclamped(start, target, k);
                // Kayarken hafifçe yalpalar
                transform.rotation = rot * Quaternion.Euler(0, Mathf.Sin(k * Mathf.PI * 3f) * 9f * (1f - k), 0);
            }, Ease.OutCubic);
            transform.SetPositionAndRotation(target, rot);
            if (hitCar != null)
            {
                hitCar.Shake(Axis * Mathf.Sign(Vector3.Dot(target - start, Axis)));
                Fx.Impact(target + transform.forward * (length / 2f) * Mathf.Sign(Vector3.Dot(target - start, transform.forward)) + Vector3.up * 0.3f);
            }
            StartCoroutine(Tween.PunchScale(visual, new Vector3(0.06f, -0.05f, -0.1f), 0.3f));
            Busy = false;
        }
        /// <summary>Vinç ile yukarı kaldırılıp götürülür.</summary>
        public void LiftAway()
        {
            StartCoroutine(LiftRoutine());
        }

        IEnumerator LiftRoutine()
        {
            Busy = true;
            Gone = true;
            SetPulse(false);
            hitBox.enabled = false;
            Sfx.Play(Sfx.Sound.Lift);
            Vector3 start = transform.position;
            Quaternion startRot = transform.rotation;
            yield return Tween.Run(0.25f, k => transform.position = start + Vector3.up * 0.4f * k, Ease.OutQuad);
            Vector3 mid = transform.position;
            yield return Tween.Run(0.7f, k =>
            {
                transform.position = mid + new Vector3(0, 7f * k, 2f * k);
                transform.rotation = startRot * Quaternion.Euler(0, 200f * k, 15f * k);
                transform.localScale = Vector3.one * (1f - 0.7f * k);
            }, Ease.InQuad);
            Destroy(gameObject);
        }

        static void Handheld()
        {
            // Kısa titreşim (haptic) için ileride native bir eklenti ekleyeceğiz.
            // Unity'nin hazır Handheld.Vibrate() fonksiyonu çok uzun titrettiği için kullanmıyoruz.
        }
    }
}
