using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CarSales
{
    /// <summary>The dealership "brain": parking spots, spawned cars, sales. Root of Dealership.prefab.</summary>
    public class DealershipManager : MonoBehaviour
    {
        [SerializeField] CarDisplay carDisplayPrefab;
        public List<ParkingSpot> spots = new List<ParkingSpot>();
        public Transform[] customerSpawnPoints;
        public Transform exitPoint;

        readonly Dictionary<string, CarDisplay> spawned = new Dictionary<string, CarDisplay>();

        void Awake()
        {
            CarSalesServices.Dealership = this;
            spots.RemoveAll(s => s == null);
            if (spots.Count == 0) spots = GetComponentsInChildren<ParkingSpot>().ToList();
            spots.Sort((a, b) => a.index.CompareTo(b.index));
        }

        void Start()
        {
            if (CarSalesServices.Upgrades) CarSalesServices.Upgrades.OnUpgradesChanged += RefreshSpotVisuals;
            RefreshSpotVisuals();
        }

        void OnDestroy()
        {
            if (CarSalesServices.Upgrades) CarSalesServices.Upgrades.OnUpgradesChanged -= RefreshSpotVisuals;
        }

        /// <summary>Shows/hides parking lines of spots that need a Lot Expansion upgrade.</summary>
        public void RefreshSpotVisuals()
        {
            foreach (var s in spots)
                foreach (Transform child in s.transform) child.gameObject.SetActive(s.IsUnlocked);
        }

        public int UnlockedSpotCount => spots.Count(s => s.IsUnlocked);

        public ParkingSpot GetFreeSpot() => spots.Find(s => s.IsFree && s.IsUnlocked);

        public CarDisplay SpawnCar(CarInstance car, ParkingSpot spot)
        {
            var display = Instantiate(carDisplayPrefab, spot.transform.position, spot.transform.rotation);
            display.Initialize(car);
            spot.Occupant = display;
            car.parkingSpotIndex = spot.index;
            spawned[car.instanceId] = display;
            return display;
        }

        public void DespawnCar(string instanceId)
        {
            if (!spawned.TryGetValue(instanceId, out var display)) return;
            var spot = spots.Find(s => s.Occupant == display);
            if (spot) spot.Occupant = null;
            if (display) Destroy(display.gameObject);
            spawned.Remove(instanceId);
        }

        public void ClearLot()
        {
            foreach (var d in spawned.Values) if (d) Destroy(d.gameObject);
            spawned.Clear();
            foreach (var s in spots) s.Occupant = null;
        }

        /// <summary>Re-creates all car GameObjects from the inventory data (after load / new game).</summary>
        public void RebuildLot()
        {
            ClearLot();
            foreach (var car in CarSalesServices.Inventory.OwnedCars)
            {
                var spot = spots.Find(s => s.index == car.parkingSpotIndex && s.IsFree) ?? GetFreeSpot();
                if (spot) SpawnCar(car, spot);
                else Debug.LogWarning($"[CarSales] No parking spot for car {car.carId}");
            }
        }

        public List<CarDisplay> GetListedCars() =>
            spawned.Values.Where(c => c && c.gameObject.activeInHierarchy && c.Data.state == CarState.Listed).ToList();

        public void CompleteSale(CarDisplay car, int price)
        {
            var d = car.Data;
            int value = CarValueCalculator.MarketValue(d);
            int profit = price - d.purchasePrice - d.repairSpent;

            CarSalesServices.Economy.AddMoney(price);
            CarSalesServices.Economy.ChangeReputation(price <= value ? 2f : 0.5f);
            d.state = CarState.Sold;
            CarSalesServices.Inventory.Remove(d.instanceId);
            DespawnCar(d.instanceId);
            CarSalesServices.DayCycle.RegisterSale(price, profit);
            if (CarSalesServices.Progress) CarSalesServices.Progress.RegisterSale(price, profit);
            CarSalesServices.PlaySfx(Sfx.Success);
            CarSalesServices.UI.Success("Car sold!", $"{car.Definition.displayName} sold for ${price:N0}. Profit: {UIFactory.Money(profit)}");
        }

        /// <summary>Instant wholesale sale (75% of market value). Doesn't change reputation.</summary>
        public void QuickSell(CarDisplay car)
        {
            if (!car) return;
            var d = car.Data;
            int price = CarValueCalculator.QuickSellPrice(d);
            int profit = price - d.purchasePrice - d.repairSpent;
            if (CarSalesServices.TestDrive && CarSalesServices.TestDrive.IsDriving) CarSalesServices.TestDrive.EndDrive();
            CarSalesServices.Economy.AddMoney(price);
            d.state = CarState.Sold;
            CarSalesServices.Inventory.Remove(d.instanceId);
            DespawnCar(d.instanceId);
            CarSalesServices.DayCycle.RegisterSale(price, profit);
            if (CarSalesServices.Progress) CarSalesServices.Progress.RegisterSale(price, profit);
            CarSalesServices.PlaySfx(Sfx.Coin);
            CarSalesServices.UI.Success("Wholesale complete", $"{car.Definition.displayName} sold to the wholesaler for ${price:N0}.");
        }

        public CarDisplay GetDisplay(string instanceId) =>
            spawned.TryGetValue(instanceId, out var d) ? d : null;
    }
}
