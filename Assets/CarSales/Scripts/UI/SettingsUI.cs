using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>Works in-game (through UIManager) and in the main menu (no UIManager).</summary>
    public class SettingsUI : UIPanel
    {
        Slider sensitivity, volume;
        TextMeshProUGUI sensitivityLabel, volumeLabel, drivingLabel, gearboxLabel, absLabel, tcsLabel, escLabel, themeLabel, scaleLabel;
        Slider uiScale;
        public GameObject showOnBackInMenu;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "Settings", new Vector2(720f, 860f), Back);
            UIFactory.Vertical(body, 0, 12f);
            sensitivityLabel = UIFactory.Text(body, "", 21, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            sensitivity = UIFactory.MakeSlider(body, UIFactory.Accent, 32f);
            sensitivity.minValue = 0.02f; sensitivity.maxValue = 0.5f;
            sensitivity.onValueChanged.AddListener(v => { GameSettings.MouseSensitivity = v; UpdateLabels(); });
            UIFactory.Spacer(body, 0f).GetComponent<LayoutElement>().minHeight = 10f;
            volumeLabel = UIFactory.Text(body, "", 21, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            volume = UIFactory.MakeSlider(body, UIFactory.Good, 32f);
            volume.minValue = 0f; volume.maxValue = 1f;
            volume.onValueChanged.AddListener(v => { GameSettings.Volume = v; UpdateLabels(); });
            UIFactory.Spacer(body, 0f).GetComponent<LayoutElement>().minHeight = 10f;

            UIFactory.Text(body, "DISPLAY", 16, UIFactory.TextDim, TextAlignmentOptions.Left, true);
            scaleLabel = UIFactory.Text(body, "", 19, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            uiScale = UIFactory.MakeSlider(body, UIFactory.Accent, 28f);
            uiScale.minValue = 0.8f; uiScale.maxValue = 1.3f;
            uiScale.onValueChanged.AddListener(v => { GameSettings.UiScale = Mathf.Round(v * 20f) / 20f; UpdateLabels(); });
            UIFactory.Btn(body, "", UIFactory.AccentDark, () =>
            {
                GameSettings.Theme = (UIThemeChoice)(((int)GameSettings.Theme + 1) % 4);
                UpdateLabels();
            }, out themeLabel, 48f, 18f);

            UIFactory.Text(body, "DRIVING", 16, UIFactory.TextDim, TextAlignmentOptions.Left, true);
            UIFactory.Btn(body, "", UIFactory.AccentDark, () =>
            {
                GameSettings.Driving = GameSettings.Driving == DrivingModel.Realistic ? DrivingModel.Arcade : DrivingModel.Realistic;
                UpdateLabels();
            }, out drivingLabel, 52f, 19f);
            UIFactory.Btn(body, "", UIFactory.AccentDark, () => { GameSettings.ManualGearbox = !GameSettings.ManualGearbox; UpdateLabels(); },
                          out gearboxLabel, 52f, 19f);
            var assists = UIFactory.HRow(body, 52f, 10f);
            UIFactory.Btn(assists, "", UIFactory.Neutral, () => { GameSettings.Abs = !GameSettings.Abs; UpdateLabels(); }, out absLabel, 52f, 18f);
            UIFactory.Btn(assists, "", UIFactory.Neutral, () => { GameSettings.TractionControl = !GameSettings.TractionControl; UpdateLabels(); }, out tcsLabel, 52f, 18f);
            UIFactory.Btn(assists, "", UIFactory.Neutral, () => { GameSettings.StabilityControl = !GameSettings.StabilityControl; UpdateLabels(); }, out escLabel, 52f, 18f);
            var note = UIFactory.Text(body, "Applies the next time you get into a car. F3 while driving shows telemetry.", 15, UIFactory.TextDim);
            UIFactory.LE(note, h: 24);
            UIFactory.Spacer(body);
            UIFactory.Btn(body, "Back", UIFactory.Neutral, Back, 52f);
        }

        void OnEnable()
        {
            if (!sensitivity) return;
            sensitivity.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            volume.SetValueWithoutNotify(GameSettings.Volume);
            if (uiScale) uiScale.SetValueWithoutNotify(GameSettings.UiScale);
            UpdateLabels();
        }

        public void Show()
        {
            if (CarSalesServices.UI) OpenSelf();
            else gameObject.SetActive(true);
        }

        void Back()
        {
            if (CarSalesServices.UI && CarSalesServices.UI.pause) { CarSalesServices.UI.pause.Show(); return; }
            gameObject.SetActive(false);
            if (showOnBackInMenu) showOnBackInMenu.SetActive(true);
        }

        void UpdateLabels()
        {
            sensitivityLabel.text = $"Mouse sensitivity: {GameSettings.MouseSensitivity:0.00}";
            volumeLabel.text = $"Volume: {GameSettings.Volume * 100f:0}%";
            if (!drivingLabel) return;
            scaleLabel.text = $"Interface size: {GameSettings.UiScale * 100f:0}%";
            themeLabel.text = $"Theme: {GameSettings.Theme.ToString().ToUpper()}  (applies after loading / main menu)" +
                              (GameSettings.Theme == UIThemeChoice.Custom ? "  - Resources/CarSalesUITheme" : "");
            drivingLabel.text = GameSettings.Driving == DrivingModel.Realistic ? "Physics: REALISTIC (wheels, gearbox, weight)" : "Physics: ARCADE (simple)";
            gearboxLabel.text = GameSettings.ManualGearbox ? "Gearbox: MANUAL (Left Shift up, Left Ctrl down)" : "Gearbox: AUTOMATIC";
            absLabel.text = "ABS: " + (GameSettings.Abs ? "ON" : "OFF");
            tcsLabel.text = "Traction control: " + (GameSettings.TractionControl ? "ON" : "OFF");
            escLabel.text = "Stability: " + (GameSettings.StabilityControl ? "ON" : "OFF");
        }
    }
}
