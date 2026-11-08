using UnityEngine;

namespace CarSales
{
    /// <summary>Registers the car database. Lives on the CarSalesSystems prefab.</summary>
    public class CarSalesBootstrap : MonoBehaviour
    {
        [SerializeField] CarDatabase database;

        void Awake()
        {
            CarSalesServices.Database = database;
            if (!database) Debug.LogError("[CarSales] No CarDatabase assigned on CarSalesBootstrap.");
        }
    }
}
