using System;
using UnityEngine;

namespace CarSales
{
    /// <summary>One specific car (in the marketplace or owned). Saved to JSON.</summary>
    [Serializable]
    public class CarInstance
    {
        public string instanceId;
        public string carId;
        public float condition;      // 0-100 (TRUE condition)
        public float reportedCondition; // what the seller claims in the marketplace (can be a lie)
        public float cleanliness;    // 0-100
        public int mileage;
        public Color paintColor = Color.white;
        public int purchasePrice;
        public int repairSpent;
        public int listedPrice;
        public CarState state;
        public int parkingSpotIndex = -1;
        public float[] parts;           // per-part condition (see CarParts)
        public bool inspected;          // a paid/free inspection revealed the true condition

        // v4: physical location, fuel, personal car
        public CarLocation location = CarLocation.Spot;
        public string dealershipId;     // dealership whose spot it occupies (location == Spot)
        public Vector3 worldPosition;   // location == World
        public float worldRotationY;
        public float fuel = 100f;       // 0-100 %
        public bool personal;           // the player's own car - never sold to customers

        /// <summary>Display name of the car model (not saved).</summary>
        public string Name => CarSalesServices.Database && CarSalesServices.Database.Get(carId) ? CarSalesServices.Database.Get(carId).displayName : carId;
    }
}
