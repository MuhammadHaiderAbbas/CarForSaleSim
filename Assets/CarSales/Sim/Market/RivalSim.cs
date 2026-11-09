using System;

namespace CarSales.Sim
{
    /// <summary>A rival dealer in town: what it sells, what it pays for your cars and what its hire desk charges.</summary>
    public sealed class RivalProfile
    {
        public string Id;
        public string Name;          // the same name as its MarketModel competitor (their online prices)
        public string Tagline;
        public string PremisesId;    // the property it trades from; empty = its own yard (you can't buy it out)
        public float Margin;         // asking price over market value
        public float Quality;        // typical condition of its stock
        public int Stock;            // cars it likes to have on the lot
        public float BuyRate;        // share of market value it pays for a car it wants
        public MarketCategory[] Prefers;    // body types it wants to buy (empty = anything)
        public int MinValue;         // won't buy cars worth less than this (0 = any)
        public float MaxDiscount;    // the most it takes off when you ask for its best price
        public float HireFactor;     // its hire prices against the standard rate
        public MarketCategory[] Fleet;      // its hire fleet, one car per entry

        public bool Wants(int category)
        {
            if (Prefers == null || Prefers.Length == 0) return true;
            foreach (var c in Prefers) if ((int)c == category) return true;
            return false;
        }

        public bool HiresOut(int category)
        {
            if (Fleet == null) return false;
            foreach (var c in Fleet) if ((int)c == category) return true;
            return false;
        }
    }

    /// <summary>What you owe or get back when you hand a hire car back.</summary>
    public struct HireBill
    {
        public int Fuel, Damage, Excess, Late, Total, Refund, Extra;
    }

    /// <summary>
    /// Phase 14e: the rival dealerships are real businesses you can visit. Each sells cars from its lot (ask for its best
    /// price), buys your cars (part-exchange / cash for cars), hires cars out by the day and takes cars from you into
    /// its hire fleet for a daily fee. Downtown Motors and Luxury Lane trade from the dealerships you can buy: buy their
    /// premises and they stop trading. Cheap Wheels has its own yard and is always there. Engine-free and tested.
    /// </summary>
    public static class RivalSim
    {
        public const int CollectionFee = 150;          // they fetch a car you sell them from wherever it is
        public const int FreeKmPerDay = 200;
        public const float ExcessPerKm = 0.25f;
        public const float LateFactor = 1.5f;          // each late day costs 1.5 days
        public const int GraceDays = 3;                // after this many late days they recover the car
        public const int RecoveryFee = 250;
        public const float RefuelMarkup = 1.2f;        // they refuel a car returned short, at 20% over the pump price
        public const float DamagePerPoint = 0.006f;    // each condition point lost costs 0.6% of the car's value
        public const float DamageTolerance = 0.5f;     // normal wear on a short hire
        public const float MinLeaseCondition = 60f;
        public static readonly int[] HireDays = { 1, 3, 7 };
        public static readonly int[] LeaseDays = { 3, 7, 14 };

        public static readonly RivalProfile[] Rivals =
        {
            new RivalProfile
            {
                Id = "downtown", Name = "Downtown Motors", PremisesId = "dealer_downtown",
                Tagline = "Family cars, part-exchange and car hire",
                Margin = 0.10f, Quality = 70f, Stock = 8, BuyRate = 0.78f,
                Prefers = new[] { MarketCategory.Hatchback, MarketCategory.Sedan, MarketCategory.SUV },
                MaxDiscount = 0.05f, HireFactor = 1f,
                Fleet = new[] { MarketCategory.Hatchback, MarketCategory.Sedan, MarketCategory.SUV, MarketCategory.Hatchback },
            },
            new RivalProfile
            {
                Id = "luxury", Name = "Luxury Lane", PremisesId = "dealer_luxury",
                Tagline = "Prestige and performance cars, executive hire",
                Margin = 0.18f, Quality = 82f, Stock = 6, BuyRate = 0.82f,
                Prefers = new[] { MarketCategory.Luxury, MarketCategory.Sports },
                MinValue = 8000, MaxDiscount = 0.03f, HireFactor = 1.25f,
                Fleet = new[] { MarketCategory.Luxury, MarketCategory.Sports, MarketCategory.Luxury },
            },
            new RivalProfile
            {
                Id = "cheapwheels", Name = "Cheap Wheels", PremisesId = "",
                Tagline = "Budget cars, cash for any car, cheap van and car hire",
                Margin = 0.02f, Quality = 52f, Stock = 10, BuyRate = 0.68f,
                Prefers = new MarketCategory[0],
                MaxDiscount = 0.08f, HireFactor = 0.8f,
                Fleet = new[] { MarketCategory.Hatchback, MarketCategory.Pickup, MarketCategory.Sedan, MarketCategory.Pickup },
            },
        };

        public static RivalProfile Find(string idOrName)
        {
            if (string.IsNullOrEmpty(idOrName)) return null;
            foreach (var r in Rivals) if (r.Id == idOrName || r.Name == idOrName) return r;
            return null;
        }

        /// <summary>The rival trading from a property, if any.</summary>
        public static RivalProfile AtPremises(string propertyId)
        {
            if (string.IsNullOrEmpty(propertyId)) return null;
            foreach (var r in Rivals) if (r.PremisesId == propertyId) return r;
            return null;
        }

        /// <summary>A rival is open unless you bought the premises it trades from.</summary>
        public static bool IsOpen(RivalProfile r, Func<string, bool> playerOwns) =>
            r != null && (string.IsNullOrEmpty(r.PremisesId) || playerOwns == null || !playerOwns(r.PremisesId));

        static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        static int RoundTo(float v, int step) => (int)(Math.Round(v / step) * step);

        // ------------------------------------------------------------------ SELLING TO YOU
        /// <summary>The price on the windscreen. <paramref name="noise"/> is -0.5..0.5 (±4%).</summary>
        public static int AskingPrice(RivalProfile r, int marketValue, double noise) =>
            Math.Max(500, RoundTo(marketValue * (1f + r.Margin + (float)noise * 0.08f), 100));

        /// <summary>Their best price when you ask: more off for a good reputation and for a car that's been on the lot a while.</summary>
        public static int BestPrice(RivalProfile r, int asking, float reputation, int daysOnLot)
        {
            float rep = Clamp(reputation / 100f, 0f, 1f);
            float off = r.MaxDiscount * (0.4f + 0.6f * rep) + Math.Min(0.03f, Math.Max(0, daysOnLot) * 0.005f);
            return Math.Max(0, RoundTo(asking * (1f - off), 50));
        }

        /// <summary>Each morning some of the lot is sold: the longer a car has been there, the likelier (they cut the price).</summary>
        public static bool SellsToday(int daysOnLot, double roll) => roll < Math.Min(0.6, 0.15 + 0.05 * Math.Max(0, daysOnLot));

        // ------------------------------------------------------------------ BUYING FROM YOU
        /// <summary>Why they won't take a car (null = they'll make an offer).</summary>
        public static string Refusal(RivalProfile r, int marketValue, bool personal, bool hired)
        {
            if (hired) return "That's a hire car - hand it back instead.";
            if (personal) return "That's your own car.";
            if (r.MinValue > 0 && marketValue < r.MinValue) return $"{r.Name} only buy cars worth over £{r.MinValue:N0}.";
            return null;
        }

        /// <summary>
        /// Their cash offer: market value x their buy rate, less for a body type they don't specialise in (-10%), a car that
        /// needs work (under 40%: -15%) and when their lot is already full (-3% per car over, at most -15%).
        /// </summary>
        public static int TradeOffer(RivalProfile r, int marketValue, int category, float condition, int stockCount)
        {
            float rate = r.BuyRate * (r.Wants(category) ? 1f : 0.9f);
            if (condition < 40f) rate *= 0.85f;
            int over = stockCount - r.Stock;
            if (over > 0) rate *= Math.Max(0.85f, 1f - 0.03f * over);
            return Math.Max(100, RoundTo(marketValue * rate, 50));
        }

        // ------------------------------------------------------------------ HIRING A CAR FROM THEM
        /// <summary>About 0.8% of the car's value a day (at least £25), adjusted for the firm.</summary>
        public static int DailyRate(RivalProfile r, int marketValue) => Math.Max(25, RoundTo(marketValue * 0.008f * r.HireFactor, 5));

        /// <summary>A week or more is 15% cheaper per day.</summary>
        public static int HireCost(int dailyRate, int days) => days <= 0 ? 0 : days >= 7 ? RoundTo(dailyRate * days * 0.85f, 5) : dailyRate * days;

        /// <summary>Refundable deposit: 10% of the car's value, £200 to £3,000.</summary>
        public static int Deposit(int marketValue) => (int)Clamp(RoundTo(marketValue * 0.1f, 50), 200, 3000);

        /// <summary>The bill on return: refuelling, damage beyond normal wear, kilometres over the allowance and late days.</summary>
        public static HireBill Return(int dailyRate, int deposit, int days, int lateDays, float fuelOut, float fuelIn, float fuelPricePerPercent,
            float conditionOut, float conditionIn, int marketValue, int kmDriven)
        {
            var b = new HireBill();
            b.Fuel = (int)Math.Ceiling(Math.Max(0f, fuelOut - fuelIn) * fuelPricePerPercent * RefuelMarkup);
            b.Damage = (int)Math.Round(Math.Max(0f, conditionOut - conditionIn - DamageTolerance) * marketValue * DamagePerPoint);
            b.Excess = (int)Math.Round(Math.Max(0, kmDriven - FreeKmPerDay * (Math.Max(1, days) + Math.Max(0, lateDays))) * ExcessPerKm);
            b.Late = (int)Math.Round(Math.Max(0, lateDays) * dailyRate * LateFactor);
            b.Total = b.Fuel + b.Damage + b.Excess + b.Late;
            b.Refund = Math.Max(0, deposit - b.Total);
            b.Extra = Math.Max(0, b.Total - deposit);
            return b;
        }

        public static int LateDays(int dueDay, int today) => Math.Max(0, today - dueDay);
        public static bool Recovered(int dueDay, int today) => LateDays(dueDay, today) > GraceDays;

        // ------------------------------------------------------------------ LEASING YOUR CAR TO THEIR FLEET
        /// <summary>Why a car can't join their hire fleet (null = it can).</summary>
        public static string LeaseRefusal(RivalProfile r, int category, int marketValue, float condition, bool personal, bool hired)
        {
            if (hired || personal) return "You can only lease out stock cars.";
            if (!r.HiresOut(category)) return $"{r.Name} don't hire out that kind of car.";
            if (r.MinValue > 0 && marketValue < r.MinValue) return $"{r.Name} only hire out cars worth over £{r.MinValue:N0}.";
            if (condition < MinLeaseCondition) return $"It needs to be in better condition (at least {MinLeaseCondition:0}%).";
            return null;
        }

        /// <summary>What they pay you a day for the car: half their own daily rate, at least £20.</summary>
        public static int LeaseFee(RivalProfile r, int marketValue) => Math.Max(20, RoundTo(DailyRate(r, marketValue) * 0.5f, 5));

        /// <summary>Kilometres their customers drive your car in a day (80-200).</summary>
        public static int LeaseKm(double roll) => (int)Math.Round(80 + Clamp((float)roll, 0f, 1f) * 120);
    }
}
