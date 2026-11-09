using UnityEngine;

namespace CarSales
{
    public enum SiteFunction { Dealership = 0, ServiceCentre = 1, Workshop = 2, CarWash = 3, FuelStation = 4, AuctionCenter = 5, UsedCarMarket = 6, House = 7, Investment = 8, PartsShop = 9, Street = 10, Other = 11 }

    /// <summary>
    /// A place in the town (spec §7.3): what it's for, its entrance, and - for dealerships you add with the World
    /// Builder - its price and running cost (registered as a property you can buy).
    /// </summary>
    public class Site : MonoBehaviour
    {
        public string siteId;
        public string displayName = "Site";
        public SiteFunction function;
        public Transform entrance;
        public int price;          // 0 = yours from the start
        public int dailyCost;
        public bool userBuilt;
        public string buildingAssetId = "";

        public Vector3 EntrancePosition => entrance ? entrance.position : transform.position;

        void Start()
        {
            if (function != SiteFunction.Dealership || price <= 0 || string.IsNullOrEmpty(siteId)) return;
            var props = CarSalesServices.Properties;
            if (props) props.RegisterProperty(new PropertyInfo
            {
                id = siteId, displayName = displayName, type = PropertyType.Dealership, price = price, dailyCost = dailyCost,
                description = $"Dealership you built with the World Builder. Rent ${dailyCost:N0}/day.",
            });
        }
    }
}
