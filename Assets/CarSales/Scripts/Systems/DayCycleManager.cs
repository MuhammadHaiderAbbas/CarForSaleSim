using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CarSales
{
    /// <summary>Shop hours, clock, rent and the end-of-day report.</summary>
    public class DayCycleManager : MonoBehaviour
    {
        [SerializeField] float realSecondsPerDay = 480f;   // 8 real minutes per working day
        [SerializeField] float openHour = 8f, closeHour = 18f;
        [SerializeField] int baseRent = 500, rentPerCar = 50;
        [SerializeField] InputActionReference toggleShopAction;

        public int CurrentDay { get; private set; } = 1;
        public float Hour { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsDayOver { get; private set; }
        public DailyReport Today { get; private set; }

        public event Action<float> OnTimeChanged;
        public event Action<bool> OnShopStateChanged;
        InputAction toggleShop;

        void Awake()
        {
            CarSalesServices.DayCycle = this;
            Hour = openHour;
            Today = new DailyReport { day = CurrentDay };
        }

        void OnEnable()
        {
            toggleShop = CarSalesInput.Resolve(toggleShopAction, "ToggleShop");
            if (toggleShop != null) toggleShop.performed += OnToggleShopInput;
        }

        void OnDisable() { if (toggleShop != null) toggleShop.performed -= OnToggleShopInput; }

        void OnToggleShopInput(InputAction.CallbackContext _)
        {
            if (CarSalesServices.UI && CarSalesServices.UI.IsMenuOpen) return;
            if (CarSalesServices.TestDrive && CarSalesServices.TestDrive.IsDriving) return;
            ToggleShop();
        }

        public void ToggleShop()
        {
            if (IsOpen) EndDay(); else OpenShop();
        }

        public void OpenShop()
        {
            if (IsOpen || IsDayOver) return;
            if (CarSalesServices.Progress && CarSalesServices.Progress.IsGameOver) return;
            IsOpen = true;
            if (CarSalesServices.Progress) CarSalesServices.Progress.Stats.shopOpened++;
            CarSalesServices.PlaySfx(Sfx.DoorChime);
            CarSalesServices.Spawner.StartSpawning();
            OnShopStateChanged?.Invoke(true);
            CarSalesServices.UI.Notify("Dealership is OPEN! Customers are on their way.");
        }

        void Update()
        {
            if (!IsOpen) return;
            Hour += (closeHour - openHour) / realSecondsPerDay * Time.deltaTime;
            OnTimeChanged?.Invoke(Hour);
            if (Hour >= closeHour) EndDay();
        }

        public void EndDay()
        {
            if (IsDayOver) return;
            if (CarSalesServices.TestDrive && CarSalesServices.TestDrive.IsDriving) CarSalesServices.TestDrive.EndDrive();
            IsOpen = false;
            IsDayOver = true;
            CarSalesServices.Spawner.StopSpawning(true);
            OnShopStateChanged?.Invoke(false);

            int rent = baseRent + rentPerCar * CarSalesServices.Inventory.OwnedCars.Count;
            CarSalesServices.Economy.ForceSpend(rent);
            int interest = CarSalesServices.Bank ? CarSalesServices.Bank.ChargeInterest() : 0;
            Today.expenses += rent + interest;

            float repBonus = CarSalesServices.Upgrades ? CarSalesServices.Upgrades.DailyReputationBonus : 0f;
            if (repBonus > 0f) CarSalesServices.Economy.ChangeReputation(repBonus);

            bool bankrupt = CarSalesServices.Progress && CarSalesServices.Progress.OnDayEnded();
            if (bankrupt)
            {
                CarSalesServices.Save.DeleteSaveFile();   // "Continue" must not load a bankrupt game
                return;
            }
            CarSalesServices.UI.dailyReport.Show(Today, rent, interest);
            CarSalesServices.Save.SaveGame();
        }

        public void StartNextDay()
        {
            SetDay(CurrentDay + 1);
            CarSalesServices.Marketplace.RefreshListings();
            if (CarSalesServices.Messages) CarSalesServices.Messages.GenerateDaily(CurrentDay);
            CarSalesServices.UI.Close();
            CarSalesServices.Save.SaveGame();
            CarSalesServices.UI.Notify($"Day {CurrentDay}. New cars are on the marketplace. Press O to open.");
        }

        public void SetDay(int day)
        {
            CurrentDay = Mathf.Max(1, day);
            Hour = openHour;
            IsOpen = false;
            IsDayOver = false;
            Today = new DailyReport { day = CurrentDay };
            OnTimeChanged?.Invoke(Hour);
            OnShopStateChanged?.Invoke(false);
        }

        public void RegisterSale(int price, int profit) { Today.carsSold++; Today.revenue += price; Today.profit += profit; }
        public void RegisterPurchase(int price) { Today.carsBought++; Today.expenses += price; }
        public void RegisterExpense(int amount) => Today.expenses += amount;
    }
}
