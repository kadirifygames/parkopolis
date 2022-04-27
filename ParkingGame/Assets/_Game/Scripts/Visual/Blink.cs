using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>İki ışığı sırayla yakıp söndürür (polis, ambulans, itfaiye tepe lambası).</summary>
    public class Blink : MonoBehaviour
    {
        GameObject a, b;
        float offset;

        public void Setup(GameObject first, GameObject second)
        {
            a = first;
            b = second;
            offset = Random.value;
        }

        void Update()
        {
            bool on = Mathf.Repeat(Time.time * 3.5f + offset, 2f) < 1f;
            if (a != null) a.SetActive(on);
            if (b != null) b.SetActive(!on);
        }
    }
}
