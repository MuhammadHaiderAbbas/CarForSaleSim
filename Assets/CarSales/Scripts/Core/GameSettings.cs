using UnityEngine;

namespace CarSales
{
    public enum DrivingModel { Realistic = 0, Arcade = 1 }

    /// <summary>Player preferences (stored in PlayerPrefs, shared by all save games).</summary>
    public static class GameSettings
    {
        const string SensitivityKey = "CarSales.MouseSensitivity";
        const string VolumeKey = "CarSales.Volume";
        public const string NewGameFlagKey = "CarSales.StartNewGame";
        const string ActiveSlotKey = "CarSales.ActiveSlot";
        const string DrivingKey = "CarSales.DrivingModel", AbsKey = "CarSales.ABS", TcsKey = "CarSales.TCS", EscKey = "CarSales.ESC";
        const string ManualKey = "CarSales.ManualGearbox";

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

        /// <summary>Realistic wheel physics, or the simple v4 arcade handling.</summary>
        public static DrivingModel Driving
        {
            get => (DrivingModel)Mathf.Clamp(PlayerPrefs.GetInt(DrivingKey, 0), 0, 1);
            set { PlayerPrefs.SetInt(DrivingKey, (int)value); PlayerPrefs.Save(); }
        }

        public static bool Abs { get => Flag(AbsKey); set => SetFlag(AbsKey, value); }
        public static bool TractionControl { get => Flag(TcsKey); set => SetFlag(TcsKey, value); }
        public static bool StabilityControl { get => Flag(EscKey); set => SetFlag(EscKey, value); }

        /// <summary>Manual gearbox (Left Shift = up, Left Ctrl = down, R from 1st at a standstill). Default off.</summary>
        public static bool ManualGearbox
        {
            get => PlayerPrefs.GetInt(ManualKey, 0) == 1;
            set => SetFlag(ManualKey, value);
        }

        static bool Flag(string key) => PlayerPrefs.GetInt(key, 1) == 1;
        static void SetFlag(string key, bool on) { PlayerPrefs.SetInt(key, on ? 1 : 0); PlayerPrefs.Save(); }

        public static void Apply() => AudioListener.volume = Volume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => loaded = false;
    }
}
