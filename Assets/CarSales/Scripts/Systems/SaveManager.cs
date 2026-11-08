using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CarSales
{
    /// <summary>Saves/loads the game as JSON in Application.persistentDataPath.</summary>
    public class SaveManager : MonoBehaviour
    {
        public const string DefaultFileName = "carsales_save.json";

        [SerializeField] string fileName = DefaultFileName;
        [SerializeField] bool loadOnStart = true;
        [SerializeField] int starterCars = 2;

        public string FilePath => Path.Combine(Application.persistentDataPath, fileName);
        public static bool SaveExists(string file = DefaultFileName) => File.Exists(Path.Combine(Application.persistentDataPath, file));

        void Awake() => CarSalesServices.Save = this;

        void Start()
        {
            // The main menu's "New Game" button sets this flag before loading the game scene.
            bool forceNew = PlayerPrefs.GetInt(GameSettings.NewGameFlagKey, 0) == 1;
            PlayerPrefs.DeleteKey(GameSettings.NewGameFlagKey);

            if (forceNew) { DeleteSaveFile(); NewGame(); }
            else if (!(loadOnStart && LoadGame())) NewGame();
        }

        public void NewGame()
        {
            if (CarSalesServices.TestDrive && CarSalesServices.TestDrive.IsDriving) CarSalesServices.TestDrive.EndDrive();
            CarSalesServices.Spawner.StopSpawning(true);
            CarSalesServices.Economy.ResetToStart();
            CarSalesServices.DayCycle.SetDay(1);
            if (CarSalesServices.Upgrades) CarSalesServices.Upgrades.SetLevels(null);
            if (CarSalesServices.Bank) CarSalesServices.Bank.SetLoan(0);
            if (CarSalesServices.Progress) CarSalesServices.Progress.ResetProgress();

            var cars = new List<CarInstance>();
            var db = CarSalesServices.Database;
            for (int i = 0; i < starterCars && db && db.cars.Count > 0; i++)
            {
                var car = CarValueCalculator.GenerateRandom(db.GetRandom());
                car.reportedCondition = car.condition;
                car.inspected = true;
                car.purchasePrice = Mathf.RoundToInt(CarValueCalculator.MarketValue(car) * 0.8f);
                car.state = CarValueCalculator.DefaultState(car);
                cars.Add(car);
            }
            CarSalesServices.Inventory.SetCars(cars);
            CarSalesServices.Marketplace.RefreshListings();
            CarSalesServices.Dealership.RebuildLot();
            if (CarSalesServices.Messages)
            {
                CarSalesServices.Messages.SetMessages(null);
                CarSalesServices.Messages.Add(MessageType.Info, "Welcome to AUTO WORLD!",
                    "Congratulations on your new dealership!\n\nBuy cars cheaply, fix them up, list them for sale and haggle with customers. " +
                    "Follow the GOAL on the top-left of your screen.\n\nPress TAB any time to open your inventory.");
                CarSalesServices.Messages.GenerateDaily(1);
            }
            CarSalesServices.UI.Toast(ToastType.Success, "Welcome to AUTO WORLD!", "Follow the GOAL at the top-left. Use the office computer to buy cars.");
        }

        public void SaveGame()
        {
            var progress = CarSalesServices.Progress;
            var data = new SaveData
            {
                money = CarSalesServices.Economy.Money,
                reputation = CarSalesServices.Economy.Reputation,
                day = CarSalesServices.DayCycle.CurrentDay,
                ownedCars = CarSalesServices.Inventory.OwnedCars,
                marketListings = CarSalesServices.Marketplace.Listings,
                upgradeLevels = CarSalesServices.Upgrades ? CarSalesServices.Upgrades.GetLevels() : new int[0],
                loan = CarSalesServices.Bank ? CarSalesServices.Bank.Loan : 0,
                stats = progress ? progress.Stats : new GameStats(),
                objectiveIndex = progress ? progress.ObjectiveIndex : 0,
                daysInDebt = progress ? progress.DaysInDebt : 0,
                hasWon = progress && progress.HasWon,
                messages = CarSalesServices.Messages ? CarSalesServices.Messages.Messages : new List<GameMessage>()
            };
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            Debug.Log($"[CarSales] Saved to {FilePath}");
        }

        public bool LoadGame()
        {
            if (!File.Exists(FilePath)) return false;
            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); }
            catch (System.Exception e) { Debug.LogWarning($"[CarSales] Save file unreadable: {e.Message}"); return false; }
            if (data == null) return false;

            if (CarSalesServices.TestDrive && CarSalesServices.TestDrive.IsDriving) CarSalesServices.TestDrive.EndDrive();
            CarSalesServices.Spawner.StopSpawning(true);
            CarSalesServices.Economy.SetState(data.money, data.reputation);
            CarSalesServices.DayCycle.SetDay(data.day);
            if (CarSalesServices.Upgrades) CarSalesServices.Upgrades.SetLevels(data.upgradeLevels);
            if (CarSalesServices.Bank) CarSalesServices.Bank.SetLoan(data.loan);
            if (CarSalesServices.Progress) CarSalesServices.Progress.SetState(data.stats, data.objectiveIndex, data.daysInDebt, data.hasWon);
            if (data.ownedCars != null) foreach (var c in data.ownedCars) { CarParts.EnsureParts(c); c.inspected = true; }
            CarSalesServices.Inventory.SetCars(data.ownedCars);
            if (CarSalesServices.Messages) CarSalesServices.Messages.SetMessages(data.messages);
            CarSalesServices.Marketplace.SetListings(data.marketListings);
            CarSalesServices.Dealership.RebuildLot();
            CarSalesServices.UI.Notify($"Game loaded (Day {data.day}).");
            return true;
        }

        public void DeleteSaveFile()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }

        public void DeleteSave()
        {
            DeleteSaveFile();
            CarSalesServices.UI.Notify("Save deleted. Restart Play Mode (or New Game) for a fresh start.");
        }
    }
}
