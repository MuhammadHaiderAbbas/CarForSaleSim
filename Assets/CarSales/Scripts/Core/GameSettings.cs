using UnityEngine;

namespace CarSales
{
    /// <summary>Player preferences (stored in PlayerPrefs, shared by all save games).</summary>
    public static class GameSettings
    {
        const string SensitivityKey = "CarSales.MouseSensitivity";
        const string VolumeKey = "CarSales.Volume";
        public const string NewGameFlagKey = "CarSales.StartNewGame";

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

        public static void Apply() => AudioListener.volume = Volume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => loaded = false;
    }
}
