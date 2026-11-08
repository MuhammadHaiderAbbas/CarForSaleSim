using System;

namespace CarSales
{
    [Serializable]
    public class CustomerProfile
    {
        public string customerName;
        public int budget;
        public CarCategory preferredCategory;
        public int patience;        // counter-offers tolerated (1-4)
        public float greed;         // 0 = easy buyer, 1 = hard haggler
        public float minCondition;  // won't want cars below this condition
    }
}
