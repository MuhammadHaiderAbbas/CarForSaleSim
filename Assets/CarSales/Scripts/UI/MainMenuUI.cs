using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>Main menu scene. Builds its own UI (put it on any GameObject; a Canvas is added if missing).</summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] string gameScene = "DealershipTest";
        GameObject mainPanel;
        SettingsUI settings;
        Button continueButton;
        TextMeshProUGUI infoText;

        void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameSettings.Apply();
            EnsureCanvas();
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            Build();
        }

        void EnsureCanvas()
        {
            if (!GetComponent<Canvas>()) gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = GetComponent<CanvasScaler>();
            if (!scaler) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            if (!GetComponent<GraphicRaycaster>()) gameObject.AddComponent<GraphicRaycaster>();
        }

        void Build()
        {
            var panel = UIFactory.Panel(transform, "MainPanel", new Color(0.03f, 0.06f, 0.13f, 0.92f), 1f);
            mainPanel = panel.gameObject;
            UIFactory.Place(panel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, 0f), new Vector2(600f, 840f));
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = UIFactory.Line;
            var v = UIFactory.Vertical(panel, 44, 16f);

            var logo = UIFactory.Text(panel.transform, "AUTO WORLD", 66, UIFactory.TextMain, TextAlignmentOptions.Center, true);
            UIFactory.LE(logo, h: 80);
            var sub = UIFactory.Text(panel.transform, "USED CARS", 24, UIFactory.Accent, TextAlignmentOptions.Center, true);
            UIFactory.LE(sub, h: 30);
            var tag = UIFactory.Text(panel.transform, "Car For Sale Simulator  -  Buy. Fix. Sell. Build an empire.", 18, UIFactory.TextDim, TextAlignmentOptions.Center);
            UIFactory.LE(tag, h: 50);
            UIFactory.Divider(panel.transform);
            infoText = UIFactory.Text(panel.transform, "", 17, UIFactory.TextDim, TextAlignmentOptions.Center);
            UIFactory.LE(infoText, h: 30);

            continueButton = UIFactory.Btn(panel.transform, "Continue", UIFactory.Good, () => LoadGame(false), 70f, 24f);
            UIFactory.Btn(panel.transform, "New Game", UIFactory.Accent, () => LoadGame(true), 70f, 24f);
            UIFactory.Btn(panel.transform, "Settings", UIFactory.Neutral, () => { mainPanel.SetActive(false); settings.Show(); }, 70f, 24f);
            UIFactory.Btn(panel.transform, "Quit", UIFactory.Bad, PauseMenuUI.QuitGame, 70f, 24f);
            UIFactory.Spacer(panel.transform);
            var version = UIFactory.Text(panel.transform, "v3.0", 14, new Color(1f, 1f, 1f, 0.4f), TextAlignmentOptions.Center);
            UIFactory.LE(version, h: 20);

            var settingsRoot = UIFactory.Rect("Settings", transform);
            UIFactory.Stretch(settingsRoot);
            settings = settingsRoot.gameObject.AddComponent<SettingsUI>();
            settings.Build();
            settings.showOnBackInMenu = mainPanel;
            settingsRoot.gameObject.SetActive(false);
        }

        void Start()
        {
            bool hasSave = SaveManager.SaveExists();
            continueButton.interactable = hasSave;
            infoText.text = hasSave ? "A saved game was found." : "No saved game yet - start a New Game!";
        }

        void LoadGame(bool newGame)
        {
            if (!Application.CanStreamedLevelBeLoaded(gameScene))
            {
                infoText.text = $"Scene '{gameScene}' is not in the build scene list.";
                return;
            }
            if (newGame) PlayerPrefs.SetInt(GameSettings.NewGameFlagKey, 1);
            SceneManager.LoadScene(gameScene);
        }
    }
}
