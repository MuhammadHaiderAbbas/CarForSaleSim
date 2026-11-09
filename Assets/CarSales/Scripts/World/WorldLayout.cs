using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarSales
{
    public enum SiteTemplate { Dealership = 0, PremiumDealership = 1 }

    [Serializable]
    public class SiteRecord
    {
        public string id, name;
        public SiteTemplate template;
        public string buildingAssetId = "";   // AssetRegistry id ("" = the standard glass showroom)
        public Vector3 position;
        public float yaw;
        public int price, dailyCost;
        public SiteFunction function;
    }

    /// <summary>
    /// The sites in a town (spec §7.3). Data/WorldLayout.asset keeps the sites you add in the World Builder so
    /// Build Everything rebuilds them; Data/Layout_DefaultTown.asset lists the generated v4 town.
    /// </summary>
    [CreateAssetMenu(menuName = "CarSales/World Layout", fileName = "WorldLayout")]
    public class WorldLayout : ScriptableObject
    {
        public List<SiteRecord> sites = new List<SiteRecord>();
        public List<CarSales.Sim.RoadSegment> roads = new List<CarSales.Sim.RoadSegment>();
    }
}
