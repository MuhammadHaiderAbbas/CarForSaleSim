using UnityEngine;

namespace CarSales
{
    /// <summary>Registers the car database. Lives on the CarSalesSystems prefab.</summary>
    public class CarSalesBootstrap : MonoBehaviour
    {
        [SerializeField] CarDatabase database;

        void Awake()
        {
            if (!database) database = CarDatabase.LoadDefault();
            CarSalesServices.Database = database;
            if (!database) Debug.LogError("[CarSales] No CarDatabase assigned on CarSalesBootstrap.");
            // Phase 5 service: added here so older generated scenes work without re-running Build Everything.
            if (!GetComponent<WorkshopManager>()) gameObject.AddComponent<WorkshopManager>();
        }
    }
}
