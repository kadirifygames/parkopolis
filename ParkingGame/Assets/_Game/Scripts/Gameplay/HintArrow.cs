using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>İpucu için arabanın üstünde zıplayan parlak ok.</summary>
    public class HintArrow : MonoBehaviour
    {
        Vector3 basePos;
        Vector3 dir;

        public static HintArrow Create(Transform parent, Vector3 carPos, Vector3 moveDir)
        {
            var go = new GameObject("HintArrow");
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HintArrow>();
            h.dir = moveDir.normalized;
            h.basePos = carPos + Vector3.up * 1.25f;
            go.transform.position = h.basePos;
            go.transform.rotation = Quaternion.LookRotation(h.dir);

            var mat = Mats.Emissive(Palette.UiGold, 0.9f);
            var edge = Mats.Lit(Palette.UiGoldDark, 0.5f);
            Shape.Box("Shaft", go.transform, new Vector3(0.2f, 0.12f, 0.55f), 0.05f, mat, new Vector3(0, 0, -0.2f), null, false);
            Shape.Part("Head", go.transform, MeshFactory.Cone(0.32f, 0.45f, 3), mat, new Vector3(0, 0, 0.05f),
                Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, 30, 0), false);
            Shape.Part("Shadow", go.transform, MeshFactory.Quad(0.7f, 1.1f), Mats.Blob(), new Vector3(0, -1.18f, 0), null, false);
            edge.name = "HintEdge";
            return h;
        }

        void Update()
        {
            float k = Mathf.Sin(Time.time * 6f);
            transform.position = basePos + dir * (k * 0.18f) + Vector3.up * (Mathf.Abs(k) * 0.08f);
        }
    }
}
