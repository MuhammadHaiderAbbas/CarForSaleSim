using System.Collections.Generic;
using UnityEngine;

namespace CarSales
{
    /// <summary>Daily car auction lots. The live bidding itself happens in AuctionUI.</summary>
    public class AuctionManager : MonoBehaviour
    {
        [SerializeField] int lotsPerDay = 5;
        [SerializeField] float openHour = 10f, closeHour = 17f;

        public List<CarInstance> Lots { get; private set; } = new List<CarInstance>();
        public float OpenHour => openHour;
        public float CloseHour => closeHour;
        public bool IsOpen => CarSalesServices.DayCycle && CarSalesServices.DayCycle.HourOfDay >= openHour && CarSalesServices.DayCycle.HourOfDay < closeHour;
        public Transform[] parkingBays;   // where won cars are left (set by the wizard)

        void Awake() => CarSalesServices.Auction = this;
        void Start() { if (Lots.Count == 0) GenerateLots(); }

        public void GenerateLots()
        {
            Lots.Clear();
            var db = CarSalesServices.Database;
            if (!db || db.cars.Count == 0) return;
            for (int i = 0; i < lotsPerDay; i++)
            {
                var car = CarValueCalculator.GenerateRandom(db.GetRandom());
                car.cleanliness = Random.Range(5f, 60f);
                car.fuel = Random.Range(5f, 40f);
                Lots.Add(car);
            }
        }

        public int StartingBid(CarInstance car) =>
            Mathf.RoundToInt(CarValueCalculator.MarketValue(car, useReported: true) * 0.35f / 100f) * 100;

        /// <summary>Where a won car is parked.</summary>
        public Transform FreeParkingBay()
        {
            var zone = ServiceZone.Find(ServiceType.Auction);
            var bay = zone ? zone.FreeBay() : null;
            if (bay) return bay;
            return parkingBays != null && parkingBays.Length > 0 ? parkingBays[0] : transform;
        }
    }
}
