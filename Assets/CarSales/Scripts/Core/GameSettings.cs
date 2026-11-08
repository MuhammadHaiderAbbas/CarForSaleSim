using UnityEngine;

namespace CarSales
{
    /// <summary>Player preferences (stored in PlayerPrefs, shared by all save games).</summary>
    public static class GameSettings
    {
        const string SensitivityKey = "CarSales.MouseSensitivity";
        const string VolumeKey = "CarSales.Volume";
        public const string NewGameFlagKey = "CarSales.StartNewGame";
        const string ActiveSlotKey = "CarSales.ActiveSlot";

        static bool loaded;
        static float sensitivity, volume;

        static void Load()
        {
            if (loaded) return;
            sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 0.12f);
            volume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            loaded = true;
        }

        public static float MouseSensitivity
        {
            get { Load(); return sensitivity; }
            set { Load(); sensitivity = value; PlayerPrefs.SetFloat(SensitivityKey, value); PlayerPrefs.Save(); }
        }

        public static float Volume
        {
            get { Load(); return volume; }
            set { Load(); volume = value; PlayerPrefs.SetFloat(VolumeKey, value); PlayerPrefs.Save(); AudioListener.volume = value; }
        }

        /// <summary>The save slot the game reads and writes (1-3). Chosen in the main menu.</summary>
        public static int ActiveSlot
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(ActiveSlotKey, 1), 1, CarSales.Foundation.SaveStore.SlotCount);
            set { PlayerPrefs.SetInt(ActiveSlotKey, Mathf.Clamp(value, 1, CarSales.Foundation.SaveStore.SlotCount)); PlayerPrefs.Save(); }
        }

        public static void Apply() => AudioListener.volume = Volume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => loaded = false;
    }
}
