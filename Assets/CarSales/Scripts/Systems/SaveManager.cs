using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CarSales
{
    /// <summary>Saves/loads the game as JSON in Application.persistentDataPath. Sleeping autosaves.</summary>
    public class SaveManager : MonoBehaviour
    {
        public const string DefaultFileName = "carsales_save.json";

        [SerializeField] string fileName = DefaultFileName;
        [SerializeField] bool loadOnStart = true;
        [SerializeField] int starterCars = 2;
        [SerializeField] string personalCarId = "hatch_velo";

        public string FilePath => Path.Combine(Application.persistentDataPath, fileName);
        public static bool SaveExists(string file = DefaultFileName) => File.Exists(Path.Combine(Application.persistentDataPath, file));

        void Awake() => CarSalesServices.Save = this;

        void Start()
        {
            bool forceNew = PlayerPrefs.GetInt(GameSettings.NewGameFlagKey, 0) == 1;
            PlayerPrefs.DeleteKey(GameSettings.NewGameFlagKey);
            if (forceNew) { DeleteSaveFile(); NewGame(); }
            else if (!(loadOnStart && LoadGame())) NewGame();
        }

        void StopEverything()
        {
            if (CarSalesServices.TestDrive && CarSalesServices.TestDrive.IsDriving) CarSalesServices.TestDrive.EndDrive();
            CustomerSpawner.StopAll(true);
        }

        public void NewGame()
        {
            StopEverything();
            CarSalesServices.Economy.ResetToStart();
            if (CarSalesServices.Properties) CarSalesServices.Properties.ResetToStart();
            if (CarSalesServices.Upgrades) CarSalesServices.Upgrades.SetLevels(null);
            if (CarSalesServices.Bank) CarSalesServices.Bank.SetLoan(0);
            if (CarSalesServices.Progress) CarSalesServices.Progress.ResetProgress();
            CarSalesServices.DayCycle.StartDay(1, 100f);

            var cars = new List<CarInstance>();
            var db = CarSalesServices.Database;
            for (int i = 0; i < starterCars && db && db.cars.Count > 0; i++)
            {
                var car = CarValueCalculator.GenerateRandom(db.GetRandom());
                car.reportedCondition = car.condition;
                car.inspected = true;
                car.fuel = Random.Range(30f, 80f);
                car.purchasePrice = Mathf.RoundToInt(CarValueCalculator.MarketValue(car) * 0.8f);
                car.state = CarValueCalculator.DefaultState(car);
                car.location = CarLocation.Spot;
                cars.Add(car);
            }

            // Your own (beat-up) car, parked at home
            var personalDef = db ? (db.Get(personalCarId) ?? db.cars.FirstOrDefault()) : null;
            if (personalDef)
            {
                var mine = CarValueCalculator.GenerateRandom(personalDef);
                CarParts.Randomize(mine, 58f);
                mine.reportedCondition = mine.condition;
                mine.inspected = true;
                mine.personal = true;
                mine.fuel = 70f;
                mine.cleanliness = 45f;
                mine.paintColor = new Color(0.25f, 0.45f, 0.75f);
                mine.purchasePrice = 0;
                mine.state = CarState.Ready;
                var homeZone = ServiceZone.Find(ServiceType.Home, PropertyManager.StarterHome);
                var bay = homeZone ? homeZone.FreeBay() : null;
                mine.location = CarLocation.World;
                mine.worldPosition = bay ? bay.position : Vector3.zero;
                mine.worldRotationY = bay ? bay.eulerAngles.y : 0f;
                cars.Add(mine);
            }

            CarSalesServices.Inventory.SetCars(cars);
            CarSalesServices.Marketplace.RefreshListings();
            if (CarSalesServices.Auction) CarSalesServices.Auction.GenerateLots();
            CarSalesServices.Vehicles.RebuildAll();
            if (CarSalesServices.Messages)
            {
                CarSalesServices.Messages.SetMessages(null);
                CarSalesServices.Messages.Add(MessageType.Info, "Welcome to AUTO WORLD!",
                    "Congratulations on your new dealership!\n\nYou live in a small trailer. Your own car is parked outside - press F next to it to drive. " +
                    "Your dealership AUTO WORLD opens at 08:00; list cars there and haggle with customers.\n\n" +
                    "Buy cars online (computer), at the Used Car Market or at the Auction. Repair them at the Mechanic Workshop, " +
                    "wash them at the Car Wash, fuel up at the gas station. Press M for the map, P for your phone (tow & taxi).\n\n" +
                    "When the day is done, drive home and sleep (from 18:00). Stay up past 02:00 and you'll pass out!");
                CarSalesServices.Messages.GenerateDaily(1);
            }
            DayCycleManager.TeleportPlayerHome();
            CarSalesServices.UI.Toast(ToastType.Success, "Welcome to AUTO WORLD!", "Your car is outside your trailer. Press F next to it to drive. M = map.");
        }

        public void SaveGame()
        {
            if (CarSalesServices.TestDrive && CarSalesServices.TestDrive.IsDriving)
            {
                CarSalesServices.TestDrive.EndDrive();   // the car is saved where you stopped
            }
            var progress = CarSalesServices.Progress;
            var props = CarSalesServices.Properties;
            var day = CarSalesServices.DayCycle;
            var data = new SaveData
            {
                money = CarSalesServices.Economy.Money,
                reputation = CarSalesServices.Economy.Reputation,
                day = day.CurrentDay,
                hour = day.Hour,
                energy = day.Energy,
                shopClosedByPlayer = day.ClosedByPlayer,
                ownedCars = CarSalesServices.Inventory.OwnedCars,
                marketListings = CarSalesServices.Marketplace.Listings,
                lotListings = CarSalesServices.Marketplace.LotListings,
                upgradeLevels = CarSalesServices.Upgrades ? CarSalesServices.Upgrades.GetLevels() : new int[0],
                loan = CarSalesServices.Bank ? CarSalesServices.Bank.Loan : 0,
                stats = progress ? progress.Stats : new GameStats(),
                objectiveIndex = progress ? progress.ObjectiveIndex : 0,
                daysInDebt = progress ? progress.DaysInDebt : 0,
                hasWon = progress && progress.HasWon,
                messages = CarSalesServices.Messages ? CarSalesServices.Messages.Messages : new List<GameMessage>(),
                ownedProperties = props ? props.GetOwned() : new List<string>(),
                activeHome = props ? props.ActiveHome : PropertyManager.StarterHome
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

            StopEverything();
            CarSalesServices.Economy.SetState(data.money, data.reputation);
            if (CarSalesServices.Properties) CarSalesServices.Properties.SetState(data.ownedProperties, data.activeHome);
            CarSalesServices.DayCycle.SetState(data.day, data.hour, data.energy, data.shopClosedByPlayer);
            if (CarSalesServices.Upgrades) CarSalesServices.Upgrades.SetLevels(data.upgradeLevels);
            if (CarSalesServices.Bank) CarSalesServices.Bank.SetLoan(data.loan);
            if (CarSalesServices.Progress) CarSalesServices.Progress.SetState(data.stats, data.objectiveIndex, data.daysInDebt, data.hasWon);
            if (data.ownedCars != null) foreach (var c in data.ownedCars) { CarParts.EnsureParts(c); c.inspected = true; }
            CarSalesServices.Inventory.SetCars(data.ownedCars);
            if (CarSalesServices.Messages) CarSalesServices.Messages.SetMessages(data.messages);
            CarSalesServices.Vehicles.RebuildAll();
            CarSalesServices.Marketplace.SetListings(data.marketListings, data.lotListings);
            if (CarSalesServices.Auction) CarSalesServices.Auction.GenerateLots();
            DayCycleManager.TeleportPlayerHome();
            CarSalesServices.UI.Notify($"Game loaded (Day {data.day}). You're at home.");
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
