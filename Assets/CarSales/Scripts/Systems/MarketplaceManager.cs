using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarSales
{
    /// <summary>Daily list of used cars the player can buy from the computer.</summary>
    public class MarketplaceManager : MonoBehaviour
    {
        [SerializeField] int listingsPerDay = 8;
        [SerializeField] Vector2 priceVsMarket = new Vector2(0.70f, 0.95f); // you buy below market value

        public List<CarInstance> Listings { get; private set; } = new List<CarInstance>();
        public event Action OnListingsChanged;

        void Awake() => CarSalesServices.Marketplace = this;
        void Start() { if (Listings.Count == 0) RefreshListings(); }

        public void RefreshListings()
        {
            Listings.Clear();
            var db = CarSalesServices.Database;
            if (db == null || db.cars.Count == 0) return;
            for (int i = 0; i < listingsPerDay; i++)
            {
                var car = CarValueCalculator.GenerateRandom(db.GetRandom());
                car.purchasePrice = Mathf.RoundToInt(CarValueCalculator.MarketValue(car, useReported: true) *
                                                     UnityEngine.Random.Range(priceVsMarket.x, priceVsMarket.y));
                Listings.Add(car);
            }
            OnListingsChanged?.Invoke();
        }

        public void SetListings(List<CarInstance> listings)
        {
            if (listings == null || listings.Count == 0) { RefreshListings(); return; }
            foreach (var l in listings) { if (l.reportedCondition <= 0f) l.reportedCondition = l.condition; CarParts.EnsureParts(l); } // older saves
            Listings = listings;
            OnListingsChanged?.Invoke();
        }

        public bool TryBuy(CarInstance listing, out string message)
        {
            var spot = CarSalesServices.Dealership.GetFreeSpot();
            if (spot == null) { message = "No free parking spot! Sell a car first."; return false; }
            if (!CarSalesServices.Economy.TrySpend(listing.purchasePrice)) { message = "Not enough money!"; return false; }

            listing.state = CarValueCalculator.DefaultState(listing);
            Listings.Remove(listing);
            CarSalesServices.Inventory.Add(listing);
            CarSalesServices.Dealership.SpawnCar(listing, spot);
            CarSalesServices.DayCycle.RegisterPurchase(listing.purchasePrice);
            if (CarSalesServices.Progress) CarSalesServices.Progress.RegisterPurchase();
            CarSalesServices.PlaySfx(Sfx.Purchase);
            OnListingsChanged?.Invoke();
            message = "Car purchased and delivered to your lot!";
            float lie = listing.reportedCondition - listing.condition;
            listing.inspected = true;   // you own it now - you know everything about it
            if (lie >= 5f)
                message += $" Real condition is {listing.condition:0}% (seller claimed {listing.reportedCondition:0}%).";
            return true;
        }
    }
}
