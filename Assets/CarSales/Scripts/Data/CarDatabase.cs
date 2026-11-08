using System.Collections.Generic;
using UnityEngine;

namespace CarSales
{
    [CreateAssetMenu(menuName = "CarSales/Car Database", fileName = "CarDatabase")]
    public class CarDatabase : ScriptableObject
    {
        public List<CarDefinition> cars = new List<CarDefinition>();

        public CarDefinition Get(string id) => cars.Find(c => c && c.carId == id);
        public CarDefinition GetRandom() => cars[Random.Range(0, cars.Count)];
    }
}
