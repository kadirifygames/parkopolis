using UnityEngine;

namespace Kadirify.Parkopolis
{
    /// <summary>
    /// Oyuncunun ilerlemesini cihazda saklar (PlayerPrefs).
    /// </summary>
    public static class SaveData
    {
        public static int Level
        {
            get => Mathf.Max(1, PlayerPrefs.GetInt("level", 1));
            set { PlayerPrefs.SetInt("level", value); PlayerPrefs.Save(); }
        }

        public static int Coins
        {
            get => PlayerPrefs.GetInt("coins", 150);
            set { PlayerPrefs.SetInt("coins", Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        /// <summary>Sandık ilerlemesi: her ChestEvery levelde bir sandık açılır.</summary>
        public const int ChestEvery = 3;

        public static int ChestProgress
        {
            get => PlayerPrefs.GetInt("chest", 0);
            set { PlayerPrefs.SetInt("chest", value); PlayerPrefs.Save(); }
        }

        public static bool Sound
        {
            get => PlayerPrefs.GetInt("sound", 1) == 1;
            set { PlayerPrefs.SetInt("sound", value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}
