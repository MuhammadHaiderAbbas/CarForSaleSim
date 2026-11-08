using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarSales
{
    /// <summary>Lifetime stats, goals with rewards, win (net worth goal) and bankruptcy.</summary>
    public class ProgressManager : MonoBehaviour
    {
        [SerializeField] int winNetWorth = 1000000;
        [SerializeField] int bankruptcyDays = 3;   // days in a row ending with negative money

        public GameStats Stats { get; private set; } = new GameStats();
        public int ObjectiveIndex { get; private set; }
        public int DaysInDebt { get; private set; }
        public bool HasWon { get; private set; }
        public bool IsGameOver { get; private set; }
        public int WinNetWorth => winNetWorth;
        public int BankruptcyDays => bankruptcyDays;
        public event Action OnObjectiveChanged;

        class Objective
        {
            public string text; public Func<bool> done; public int reward;
            public Objective(string text, Func<bool> done, int reward) { this.text = text; this.done = done; this.reward = reward; }
        }

        List<Objective> objectives;
        float checkTimer;

        void Awake()
        {
            CarSalesServices.Progress = this;
            var eco = (Func<EconomyManager>)(() => CarSalesServices.Economy);
            objectives = new List<Objective>
            {
                new Objective("Repair or clean one of your cars (walk up to it, press E)", () => Stats.carsServiced >= 1, 0),
                new Objective("List a car for sale (car menu > Set Price)", () => Stats.carsListed >= 1, 0),
                new Objective("Open the shop (press O or use the sign)", () => Stats.shopOpened >= 1, 0),
                new Objective("Sell your first car", () => Stats.carsSold >= 1, 1000),
                new Objective("Buy a car from the computer marketplace", () => Stats.carsBought >= 1, 0),
                new Objective("Take a car for a test drive (car menu)", () => Stats.testDrives >= 1, 500),
                new Objective("Make $15,000 total profit", () => Stats.totalProfit >= 15000, 2000),
                new Objective("Buy your first upgrade (computer > Upgrades)", () => Stats.upgradesBought >= 1, 1000),
                new Objective("Sell 10 cars", () => Stats.carsSold >= 10, 5000),
                new Objective("Reach 75 reputation", () => eco().Reputation >= 75f, 5000),
                new Objective("Reach $250,000 net worth", () => NetWorth >= 250000, 10000),
                new Objective("Sell 50 cars", () => Stats.carsSold >= 50, 20000),
                new Objective($"Reach ${winNetWorth:N0} net worth - WIN THE GAME", () => NetWorth >= winNetWorth, 0),
            };
        }

        public int ObjectiveCount => objectives.Count;
        public string CurrentObjectiveText =>
            ObjectiveIndex < objectives.Count
                ? $"GOAL {ObjectiveIndex + 1}/{objectives.Count}: {objectives[ObjectiveIndex].text}" +
                  (objectives[ObjectiveIndex].reward > 0 ? $"  (reward ${objectives[ObjectiveIndex].reward:N0})" : "")
                : "All goals complete - keep growing your empire!";

        /// <summary>Money + value of owned cars - loan.</summary>
        public int NetWorth
        {
            get
            {
                long worth = CarSalesServices.Economy.Money;
                foreach (var car in CarSalesServices.Inventory.OwnedCars) worth += CarValueCalculator.MarketValue(car);
                if (CarSalesServices.Bank) worth -= CarSalesServices.Bank.Loan;
                return (int)Mathf.Clamp(worth, int.MinValue, int.MaxValue);
            }
        }

        void Update()
        {
            checkTimer -= Time.unscaledDeltaTime;
            if (checkTimer > 0f) return;
            checkTimer = 0.5f;
            CheckObjectives();
            CheckWin();
        }

        void CheckObjectives()
        {
            bool changed = false;
            while (ObjectiveIndex < objectives.Count && objectives[ObjectiveIndex].done())
            {
                var o = objectives[ObjectiveIndex];
                ObjectiveIndex++;
                changed = true;
                if (o.reward > 0) CarSalesServices.Economy.AddMoney(o.reward);
                CarSalesServices.UI.Success("Goal complete!", o.text + (o.reward > 0 ? $"  (+${o.reward:N0})" : ""));
                if (CarSalesServices.Messages)
                    CarSalesServices.Messages.Add(MessageType.Goal, "Goal complete", o.text + (o.reward > 0 ? $"\nReward: ${o.reward:N0}" : ""));
                CarSalesServices.PlaySfx(Sfx.Success);
            }
            if (changed) OnObjectiveChanged?.Invoke();
        }

        void CheckWin()
        {
            if (HasWon || IsGameOver || CarSalesServices.UI.IsMenuOpen) return;
            if (CarSalesServices.TestDrive && CarSalesServices.TestDrive.IsDriving) return;
            if (NetWorth < winNetWorth) return;
            HasWon = true;
            CarSalesServices.PlaySfx(Sfx.Success);
            CarSalesServices.UI.endScreen.ShowWin();
        }

        // -------- Hooks called by other systems --------
        public void RegisterSale(int price, int profit)
        {
            Stats.carsSold++;
            Stats.totalRevenue += price;
            Stats.totalProfit += profit;
            Stats.bestSale = Mathf.Max(Stats.bestSale, profit);
        }

        public void RegisterPurchase() => Stats.carsBought++;

        /// <summary>Called by DayCycleManager after rent/interest. Returns true if the player went bankrupt.</summary>
        public bool OnDayEnded()
        {
            Stats.daysPlayed++;
            DaysInDebt = CarSalesServices.Economy.Money < 0 ? DaysInDebt + 1 : 0;
            if (DaysInDebt >= bankruptcyDays)
            {
                IsGameOver = true;
                CarSalesServices.PlaySfx(Sfx.Error);
                CarSalesServices.UI.endScreen.ShowGameOver();
                return true;
            }
            return false;
        }

        public void ResetProgress() => SetState(new GameStats(), 0, 0, false);

        public void SetState(GameStats stats, int objectiveIndex, int daysInDebt, bool hasWon)
        {
            Stats = stats ?? new GameStats();
            ObjectiveIndex = Mathf.Clamp(objectiveIndex, 0, objectives.Count);
            DaysInDebt = daysInDebt;
            HasWon = hasWon;
            IsGameOver = false;
            OnObjectiveChanged?.Invoke();
        }
    }
}
