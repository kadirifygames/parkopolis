using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Editör kurulumunun oluşturduğu temel malzemeler (material).
    /// "Resources" klasöründe durduğu için oyun derlenirken shader'lar da pakete eklenir.
    /// </summary>
    public class GameAssets : ScriptableObject
    {
        public Material lit;          // ışık alan normal yüzey
        public Material emissive;     // kendi ışığını yayan yüzey (farlar, lambalar)
        public Material unlit;        // ışıktan etkilenmeyen yüzey
        public Material transparent;  // yarı saydam (araba altı gölgesi)
        public Material particles;    // efekt parçacıkları (duman, konfeti)

        static GameAssets instance;

        public static GameAssets I
        {
            get
            {
                if (instance == null) instance = Resources.Load<GameAssets>("GameAssets");
                return instance;
            }
        }
    }
}
