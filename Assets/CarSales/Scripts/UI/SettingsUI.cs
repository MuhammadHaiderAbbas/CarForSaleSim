using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>Works in-game (through UIManager) and in the main menu (no UIManager).</summary>
    public class SettingsUI : UIPanel
    {
        Slider sensitivity, volume;
        TextMeshProUGUI sensitivityLabel, volumeLabel;
        public GameObject showOnBackInMenu;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "Settings", new Vector2(680f, 460f), Back);
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
            UIFactory.Spacer(body);
            UIFactory.Btn(body, "Back", UIFactory.Neutral, Back, 52f);
        }

        void OnEnable()
        {
            if (!sensitivity) return;
            sensitivity.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            volume.SetValueWithoutNotify(GameSettings.Volume);
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
        }
    }
}
