using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Çıkıştaki bariyer kolu. Yakınından araba geçerken kalkar, sonra iner.
    /// </summary>
    public class Barrier : MonoBehaviour
    {
        float lastPing = -10f;
        float angle;

        /// <summary>Araba yaklaşınca çağrılır: "açık kal".</summary>
        public void Ping() => lastPing = Time.time;

        void Update()
        {
            bool open = Time.time - lastPing < 0.5f;
            float target = open ? -80f : 0f;
            // Açılırken hızlı, kapanırken yavaş
            angle = Mathf.MoveTowards(angle, target, (open ? 600f : 160f) * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
