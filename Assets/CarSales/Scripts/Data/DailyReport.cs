using System;

namespace CarSales
{
    [Serializable]
    public class DailyReport
    {
        public int day;
        public int carsSold, carsBought;
        public int revenue, expenses, profit;
    }
}
