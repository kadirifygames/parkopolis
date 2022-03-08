using UnityEngine;
using UnityEngine.Rendering;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Sahneye parça eklemenin kısa yolu: bir mesh + bir material = görünen bir nesne.
    /// Unity'de görünen her nesnenin MeshFilter (şekil) ve MeshRenderer (çizim) bileşeni vardır.
    /// </summary>
    public static class Shape
    {
        public static GameObject Part(string name, Transform parent, Mesh mesh, Material mat,
            Vector3 localPos, Quaternion? localRot = null, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            if (localRot.HasValue) go.transform.localRotation = localRot.Value;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 size, float radius, Material mat,
            Vector3 localPos, Quaternion? localRot = null, bool castShadows = true)
            => Part(name, parent, MeshFactory.RoundedBox(size, radius), mat, localPos, localRot, castShadows);

        /// <summary>Zemine boyanmış çizgi/şerit gibi ince, gölge düşürmeyen parça.</summary>
        public static GameObject Paint(string name, Transform parent, Vector3 size, Material mat, Vector3 localPos)
            => Part(name, parent, MeshFactory.RoundedBox(size, 0.005f, 1), mat, localPos, null, false);
    }
}
