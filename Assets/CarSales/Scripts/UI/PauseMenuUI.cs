using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarSales
{
    public class PauseMenuUI : UIPanel
    {
        [SerializeField] string mainMenuScene = "MainMenu";
        Button mainMenuButton;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "Paused", new Vector2(520f, 600f), CloseAll);
            UIFactory.Vertical(body, 0, 14f);
            UIFactory.Btn(body, "Resume", UIFactory.Good, CloseAll, 62f, 22f);
            UIFactory.Btn(body, "Save Game", UIFactory.Accent, () => { CarSalesServices.Save.SaveGame(); CarSalesServices.UI.Success("Saved", "Your progress has been saved."); }, 62f, 22f);
            UIFactory.Btn(body, "Settings", UIFactory.Neutral, () => CarSalesServices.UI.settings.Show(), 62f, 22f);
            mainMenuButton = UIFactory.Btn(body, "Save & Main Menu", UIFactory.AccentDark, GoToMainMenu, 62f, 22f);
            UIFactory.Btn(body, "Save & Quit", UIFactory.Bad, () => { CarSalesServices.Save.SaveGame(); QuitGame(); }, 62f, 22f);
        }

        public void Show()
        {
            OpenSelf();
            mainMenuButton.interactable = Application.CanStreamedLevelBeLoaded(mainMenuScene);
        }

        void GoToMainMenu()
        {
            if (!(CarSalesServices.Progress && CarSalesServices.Progress.IsGameOver)) CarSalesServices.Save.SaveGame();
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        }

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
