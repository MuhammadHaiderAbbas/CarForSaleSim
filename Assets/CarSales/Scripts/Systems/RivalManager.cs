using System;
using System.Collections.Generic;
using System.Linq;
using CarSales.Foundation;
using CarSales.Sim;
using UnityEngine;

namespace CarSales
{
    /// <summary>A car on a rival's lot, or in its hire fleet.</summary>
    [Serializable]
    public class RivalCar
    {
        public string rival;
        public bool fleet;              // a hire car, not for sale
        public CarInstance car;
        public int asking;
        public int best;                // > 0 once you asked for their best price
        public int listedDay;
    }

    /// <summary>A car you hired from a rival.</summary>
    [Serializable]
    public class HireContract
    {
        public string id;
        public string rival;
        public string carInstanceId;
        public int dailyRate, deposit, days, startDay, dueDay, value;
        public float fuelOut, conditionOut;
        public int mileageOut;
    }

    /// <summary>One of your cars out in a rival's hire fleet. The car is kept here (not in your inventory) until it comes back.</summary>
    [Serializable]
    public class FleetLease
    {
        public string id;
        public string rival;
        public CarInstance car;
        public int dailyFee, startDay, endDay, earned, km;
    }

    [Serializable]
    public class RivalsState
    {
        public int version = 1;
        public int lastDay;
        public List<RivalCar> cars = new List<RivalCar>();
        public List<HireContract> hires = new List<HireContract>();
        public List<FleetLease> leases = new List<FleetLease>();
        public int bought, sold, hireDays, leaseIncome;
    }

    /// <summary>
    /// Phase 14e: the rival dealerships (see <see cref="RivalSim"/>). Their lots restock every morning, their stock is
    /// also what the market shows as competitor prices, you can buy from them, sell to them, hire one of their cars and
    /// lease your cars to their hire fleets. Saved as module "rivals". Added by CarSalesBootstrap if missing.
    /// </summary>
    public class RivalManager : MonoBehaviour
    {
        RivalsState state = new RivalsState();
        public RivalsState State => state;
        public event Action OnChanged;
        readonly System.Random rng = new System.Random();

        void Awake() => CarSalesServices.Rivals = this;
        void OnEnable() => EventBus.Subscribe<DayStarted>(OnDayStarted);
        void OnDisable() => EventBus.Unsubscribe<DayStarted>(OnDayStarted);

        void Start()
        {
            if (CarSalesServices.Properties) CarSalesServices.Properties.OnPropertiesChanged += PremisesChanged;
            if (state.cars.Count == 0) Restock(CarRecords.Today, true);
        }

        void OnDestroy() { if (CarSalesServices.Properties) CarSalesServices.Properties.OnPropertiesChanged -= PremisesChanged; }

        bool suspended;

        /// <summary>Loading or starting a game: forget the old game's rivals so the property reset can't act on them.</summary>
        public void Suspend() { state = new RivalsState(); suspended = true; }

        public void SetState(RivalsState s)
        {
            suspended = false;
            state = s ?? new RivalsState();
            state.cars.RemoveAll(c => c == null || c.car == null);
            foreach (var c in state.cars) CarParts.EnsureParts(c.car);
            if (state.cars.Count == 0) Restock(CarRecords.Today, true);
            else Changed();
        }

        /// <summary>New game: day 1 (the day counter may still show the old game's day here).</summary>
        public void ResetState() { suspended = false; state = new RivalsState(); Restock(1, true); }

        void Changed()
        {
            SyncToMarket();
            OnChanged?.Invoke();
        }

        // ------------------------------------------------------------------ WHO IS TRADING
        static bool PlayerOwns(string id) => CarSalesServices.Properties && CarSalesServices.Properties.Owns(id);
        public static bool IsOpen(RivalProfile r) => RivalSim.IsOpen(r, PlayerOwns);
        public static bool IsOpen(string rivalId) => IsOpen(RivalSim.Find(rivalId));

        public IEnumerable<RivalCar> Stock(string rival) => state.cars.Where(c => c.rival == rival && !c.fleet);
        public IEnumerable<RivalCar> Fleet(string rival) => state.cars.Where(c => c.rival == rival && c.fleet);
        public RivalCar Find(string instanceId) => state.cars.Find(c => c.car.instanceId == instanceId);
        public HireContract ActiveHire => state.hires.FirstOrDefault();
        public HireContract HireFor(string carInstanceId) => state.hires.Find(h => h.carInstanceId == carInstanceId);
        public IEnumerable<FleetLease> Leases(string rival) => state.leases.Where(l => l.rival == rival);

        /// <summary>You bought a rival's premises: it stops trading, its hire car and your leased cars come back.</summary>
        void PremisesChanged()
        {
            if (suspended) return;
            bool any = false;
            foreach (var r in RivalSim.Rivals)
            {
                if (IsOpen(r)) continue;
                bool closed = state.cars.RemoveAll(c => c.rival == r.Id) > 0;
                foreach (var h in state.hires.Where(h => h.rival == r.Id).ToList()) { EndHire(h, null, closing: true); closed = true; }
                foreach (var l in state.leases.Where(l => l.rival == r.Id).ToList()) { ReturnLease(l, closing: true); closed = true; }
                any |= closed;
                if (closed && CarSalesServices.Messages)
                    CarSalesServices.Messages.Add(MessageType.Info, $"{r.Name} has stopped trading",
                        $"You bought {r.Name}'s premises, so they've closed down. Their stock has gone to auction, any car you'd hired from them has been collected and your cars in their hire fleet are back on your lots.");
            }
            if (any) Changed();
        }

        // ------------------------------------------------------------------ STOCK
        static CarDatabase Db => CarSalesServices.Database;

        CarDefinition PickModel(MarketCategory[] categories)
        {
            var db = Db;
            if (!db || db.cars.Count == 0) return null;
            if (categories != null && categories.Length > 0)
            {
                var cat = categories[rng.Next(categories.Length)];
                var pool = db.cars.Where(c => c && (int)c.category == (int)cat).ToList();
                if (pool.Count > 0) return pool[rng.Next(pool.Count)];
            }
            return db.GetRandom();
        }

        CarInstance MakeCar(CarDefinition def, float condition)
        {
            var car = CarValueCalculator.GenerateRandom(def);
            CarParts.Randomize(car, Mathf.Clamp(condition, 20f, 98f));
            car.reportedCondition = car.condition;   // a dealer's description is honest
            car.inspected = true;
            car.cleanliness = UnityEngine.Random.Range(75f, 100f);   // valeted for the forecourt
            car.fuel = UnityEngine.Random.Range(25f, 60f);
            car.state = CarState.Ready;
            return car;
        }

        void AddToLot(RivalProfile r, CarInstance car, int day)
        {
            int value = CarValueCalculator.MarketValue(car);
            state.cars.Add(new RivalCar { rival = r.Id, car = car, asking = RivalSim.AskingPrice(r, value, rng.NextDouble() - 0.5), listedDay = day });
        }

        /// <summary>Each morning: some cars are sold, the lots fill up again, the hire fleets are made up.</summary>
        void Restock(int day, bool fresh)
        {
            var db = Db;
            if (!db || db.cars.Count == 0) return;
            foreach (var r in RivalSim.Rivals)
            {
                if (!IsOpen(r)) { state.cars.RemoveAll(c => c.rival == r.Id); continue; }
                if (!fresh)
                    state.cars.RemoveAll(c => c.rival == r.Id && !c.fleet && RivalSim.SellsToday(day - c.listedDay, rng.NextDouble()));
                int have = Stock(r.Id).Count();
                for (int i = have; i < r.Stock; i++)
                {
                    // Mostly the body types they specialise in; a prestige dealer only ever stocks those.
                    bool special = r.Prefers != null && r.Prefers.Length > 0 && (r.MinValue > 0 || rng.NextDouble() < 0.7);
                    var def = PickModel(special ? r.Prefers : null);
                    if (!def) break;
                    AddToLot(r, MakeCar(def, r.Quality + UnityEngine.Random.Range(-15f, 15f)), fresh ? day - rng.Next(0, 6) : day);
                }
                // The hire fleet: one car for each entry of the profile, unless it's out on hire with you.
                int hiredOut = state.hires.Count(h => h.rival == r.Id);
                int fleet = Fleet(r.Id).Count();
                for (int i = fleet + hiredOut; i < r.Fleet.Length; i++)
                {
                    var def = PickModel(new[] { r.Fleet[i] });
                    if (!def) break;
                    var car = MakeCar(def, UnityEngine.Random.Range(82f, 96f));
                    car.fuel = 100f;
                    car.mileage = UnityEngine.Random.Range(6000, 45000);
                    state.cars.Add(new RivalCar { rival = r.Id, fleet = true, car = car, listedDay = day });
                }
            }
            state.lastDay = day;
            Changed();
        }

        /// <summary>The market's competitor prices are the rivals' real stock (their names match MarketModel.Competitors).</summary>
        public void SyncToMarket()
        {
            var market = CarSalesServices.Market;
            var db = Db;
            if (!market || !db) return;
            var listings = market.State.competitorListings;
            listings.RemoveAll(l => RivalSim.Find(l.dealer) != null);
            foreach (var c in state.cars.Where(c => !c.fleet))
            {
                var def = db.Get(c.car.carId);
                var r = RivalSim.Find(c.rival);
                if (!def || r == null) continue;
                listings.Add(new CompetitorListing { dealer = r.Name, carId = def.carId, category = (int)def.category, price = c.best > 0 ? c.best : c.asking, condition = c.car.condition, mileage = c.car.mileage });
            }
        }

        void OnDayStarted(DayStarted e)
        {
            if (state.lastDay >= e.Day) return;
            Restock(e.Day, false);
            HiresDue(e.Day);
            PayLeases(e.Day);
            Changed();
        }

        // ------------------------------------------------------------------ BUYING FROM A RIVAL
        public int PriceOf(RivalCar c) => c.best > 0 ? Mathf.Min(c.best, c.asking) : c.asking;

        /// <summary>"What's your best price?" - asked once per car.</summary>
        public int AskBestPrice(RivalCar c)
        {
            if (c.best > 0) return c.best;
            var r = RivalSim.Find(c.rival);
            float rep = CarSalesServices.Economy ? CarSalesServices.Economy.Reputation : 50f;
            c.best = RivalSim.BestPrice(r, c.asking, rep, CarRecords.Today - c.listedDay);
            Changed();
            return c.best;
        }

        /// <summary>Buy a car off a rival's lot. It stays where it stands (or at their handover bay): drive it away or tow it.</summary>
        public bool Buy(RivalCar c, Vector3? pos, float yaw, out string message)
        {
            var r = RivalSim.Find(c.rival);
            if (r == null || c.fleet || !state.cars.Contains(c)) { message = "That car isn't for sale."; return false; }
            int price = PriceOf(c);
            var car = c.car;
            if (!CarSalesServices.Economy.TrySpend(price, TxCategory.VehiclePurchase, $"{r.Name} - {car.Name}", car.instanceId))
            { message = $"You need {UIFactory.Money(price)}."; return false; }
            state.cars.Remove(c);
            car.purchasePrice = price;
            car.inspected = true;
            car.state = CarValueCalculator.DefaultState(car);
            car.personal = false;
            car.hireId = null;
            CarSalesServices.Vehicles.AddOwnedCar(car, pos, yaw);
            CarSalesServices.DayCycle.RegisterPurchase(price);
            if (CarSalesServices.Progress) CarSalesServices.Progress.RegisterPurchase();
            CarSalesServices.PlaySfx(Sfx.Purchase);
            state.bought++;
            Changed();
            message = pos.HasValue ? $"You bought the {car.Name} from {r.Name} for {UIFactory.Money(price)}. Drive it away (F) or tow it (P)."
                                   : $"You bought the {car.Name} from {r.Name} for {UIFactory.Money(price)}. They've delivered it to your lot.";
            return true;
        }

        // ------------------------------------------------------------------ SELLING TO A RIVAL
        /// <summary>Their offer for one of your cars (0 = they won't buy it; <paramref name="refusal"/> says why).</summary>
        public int Offer(string rivalId, CarInstance car, out string refusal)
        {
            var r = RivalSim.Find(rivalId);
            var def = car != null && Db ? Db.Get(car.carId) : null;
            if (r == null || !def) { refusal = "They won't buy that."; return 0; }
            int value = CarValueCalculator.MarketValue(car);
            refusal = RivalSim.Refusal(r, value, car.personal, car.IsHire);
            if (refusal != null) return 0;
            return RivalSim.TradeOffer(r, value, (int)def.category, car.condition, Stock(rivalId).Count());
        }

        /// <summary>Sell them a car. Away from their yard they collect it (fee taken off the price). It then goes on their lot.</summary>
        public bool SellTo(string rivalId, CarDisplay display, bool collect, out string message)
        {
            var r = RivalSim.Find(rivalId);
            if (r == null || !display) { message = "Nothing to sell."; return false; }
            int offer = Offer(rivalId, display.Data, out var refusal);
            if (offer <= 0) { message = refusal; return false; }
            int price = Mathf.Max(0, offer - (collect ? RivalSim.CollectionFee : 0));
            var copy = JsonUtility.FromJson<CarInstance>(JsonUtility.ToJson(display.Data));
            string name = display.Definition ? display.Definition.displayName : display.Data.Name;
            CarSalesServices.Vehicles.SellToTrade(display, price, r.Name);
            // The car joins their lot at their mark-up (you may see it again).
            copy.instanceId = Guid.NewGuid().ToString();
            copy.state = CarState.Ready;
            copy.location = CarLocation.World;
            copy.listing = new ListingInfo();
            copy.purchasePrice = 0; copy.repairSpent = 0; copy.listedPrice = 0;
            AddToLot(r, copy, CarRecords.Today);
            state.sold++;
            Changed();
            message = $"{r.Name} paid {UIFactory.Money(price)} for the {name}" + (collect ? $" (after the {UIFactory.Money(RivalSim.CollectionFee)} collection fee)." : ".");
            return true;
        }

        // ------------------------------------------------------------------ HIRING ONE OF THEIR CARS
        public int DailyRate(RivalCar c) => RivalSim.DailyRate(RivalSim.Find(c.rival), CarValueCalculator.MarketValue(c.car));
        public int Deposit(RivalCar c) => RivalSim.Deposit(CarValueCalculator.MarketValue(c.car));

        /// <summary>Hire a car for some days: pay the hire up front plus a refundable deposit. It waits at their handover bay.</summary>
        public bool Hire(RivalCar c, int days, Vector3? pos, float yaw, out string message)
        {
            var r = RivalSim.Find(c.rival);
            if (r == null || !c.fleet || !state.cars.Contains(c)) { message = "That car isn't available."; return false; }
            if (ActiveHire != null) { message = "You already have a hire car - hand it back first."; return false; }
            int rate = DailyRate(c), deposit = Deposit(c), cost = RivalSim.HireCost(rate, days);
            if (!CarSalesServices.Economy.CanAfford(cost + deposit)) { message = $"You need {UIFactory.Money(cost + deposit)} ({UIFactory.Money(cost)} hire + {UIFactory.Money(deposit)} deposit)."; return false; }
            var car = c.car;
            var h = new HireContract
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 8), rival = r.Id, carInstanceId = car.instanceId,
                dailyRate = rate, deposit = deposit, days = days, startDay = CarRecords.Today, dueDay = CarRecords.Today + days,
                value = CarValueCalculator.MarketValue(car), fuelOut = car.fuel, conditionOut = car.condition, mileageOut = car.mileage,
            };
            CarSalesServices.Economy.TrySpend(cost, TxCategory.CarHire, $"Hire {car.Name} from {r.Name} ({days} day{(days == 1 ? "" : "s")})", car.instanceId);
            CarSalesServices.Economy.TrySpend(deposit, TxCategory.Deposits, $"Hire deposit - {r.Name}", car.instanceId);
            state.cars.Remove(c);
            state.hires.Add(h);
            car.personal = true;          // never sold, never offered to customers
            car.hireId = h.id;
            car.purchasePrice = 0;
            car.state = CarState.Ready;
            CarSalesServices.Vehicles.AddOwnedCar(car, pos, yaw);
            state.hireDays += days;
            CarSalesServices.PlaySfx(Sfx.Coin);
            Changed();
            message = $"The {car.Name} is yours until the morning of day {h.dueDay}. Bring it back to {r.Name} with a full tank: {RivalSim.FreeKmPerDay} km a day included.";
            return true;
        }

        /// <summary>The bill if you handed the car back now.</summary>
        public HireBill BillFor(HireContract h, CarInstance car)
        {
            int today = CarRecords.Today;
            float fuelPrice = CarSalesServices.Vehicles ? CarSalesServices.Vehicles.FuelPricePerPercent : 0.9f;
            return RivalSim.Return(h.dailyRate, h.deposit, h.days, RivalSim.LateDays(h.dueDay, today), h.fuelOut, car != null ? car.fuel : 0f, fuelPrice,
                h.conditionOut, car != null ? car.condition : 0f, h.value, car != null ? Mathf.Max(0, car.mileage - h.mileageOut) : 0);
        }

        /// <summary>Hand the hire car back (at their yard). Deposit refunded less any charges.</summary>
        public bool ReturnHire(HireContract h, CarDisplay display, out string message)
        {
            if (h == null) { message = "No hire car."; return false; }
            var bill = EndHire(h, display, closing: false);
            message = bill.Total <= 0 ? $"All in order - your {UIFactory.Money(h.deposit)} deposit is back."
                : $"Charges {UIFactory.Money(bill.Total)} (fuel {UIFactory.Money(bill.Fuel)}, damage {UIFactory.Money(bill.Damage)}, extra km {UIFactory.Money(bill.Excess)}, late {UIFactory.Money(bill.Late)}). " +
                  (bill.Extra > 0 ? $"That's {UIFactory.Money(bill.Extra)} more than the deposit." : $"{UIFactory.Money(bill.Refund)} of your deposit is back.");
            return true;
        }

        HireBill EndHire(HireContract h, CarDisplay display, bool closing)
        {
            var r = RivalSim.Find(h.rival);
            var car = CarSalesServices.Inventory ? CarSalesServices.Inventory.Get(h.carInstanceId) : null;
            var bill = closing ? new HireBill { Refund = h.deposit } : BillFor(h, car);
            var eco = CarSalesServices.Economy;
            if (bill.Refund > 0) eco.AddMoney(bill.Refund, TxCategory.Deposits, $"Hire deposit back - {r?.Name}", h.carInstanceId);
            if (bill.Extra > 0) eco.ForceSpend(bill.Extra, TxCategory.CarHire, $"Hire charges - {r?.Name}", h.carInstanceId);
            var drive = CarSalesServices.TestDrive;
            if (!display && CarSalesServices.Vehicles) display = CarSalesServices.Vehicles.GetDisplay(h.carInstanceId);
            if (drive && drive.IsDriving && display && drive.CurrentCar == display) drive.EndDrive();
            if (CarSalesServices.Vehicles) CarSalesServices.Vehicles.Despawn(h.carInstanceId);
            if (CarSalesServices.Inventory) CarSalesServices.Inventory.Remove(h.carInstanceId);
            state.hires.Remove(h);
            if (car != null && r != null && IsOpen(r))
            {
                // Back into their fleet, valeted and refuelled.
                car.personal = false; car.hireId = null; car.fuel = 100f; car.cleanliness = 95f;
                state.cars.Add(new RivalCar { rival = r.Id, fleet = true, car = car, listedDay = CarRecords.Today });
            }
            CarSalesServices.PlaySfx(Sfx.Coin);
            Changed();
            return bill;
        }

        /// <summary>Reminders on the due day; after the grace days they come and take the car (and keep a recovery fee).</summary>
        void HiresDue(int day)
        {
            foreach (var h in state.hires.ToList())
            {
                var r = RivalSim.Find(h.rival);
                if (RivalSim.Recovered(h.dueDay, day))
                {
                    var car = CarSalesServices.Inventory ? CarSalesServices.Inventory.Get(h.carInstanceId) : null;
                    var bill = BillFor(h, car);
                    EndHire(h, null, closing: false);
                    CarSalesServices.Economy.ForceSpend(RivalSim.RecoveryFee, TxCategory.Fines, $"Hire car recovered by {r?.Name}");
                    if (CarSalesServices.Messages)
                        CarSalesServices.Messages.Add(MessageType.Info, $"{r?.Name} took their car back",
                            $"The hire car was {RivalSim.LateDays(h.dueDay, day)} days late, so they collected it. Charges {UIFactory.Money(bill.Total)} plus a {UIFactory.Money(RivalSim.RecoveryFee)} recovery fee.");
                }
                else if (day == h.dueDay && CarSalesServices.Messages)
                    CarSalesServices.Messages.Add(MessageType.Info, $"Hire car due back at {r?.Name}",
                        $"Your hire is up today. Each late day costs {UIFactory.Money(Mathf.RoundToInt(h.dailyRate * RivalSim.LateFactor))}; after {RivalSim.GraceDays} days they collect it.");
            }
        }

        // ------------------------------------------------------------------ LEASING YOUR CARS TO THEIR FLEET
        public int LeaseFee(string rivalId, CarInstance car) => RivalSim.LeaseFee(RivalSim.Find(rivalId), CarValueCalculator.MarketValue(car));

        public string LeaseRefusal(string rivalId, CarInstance car)
        {
            var r = RivalSim.Find(rivalId);
            var def = car != null && Db ? Db.Get(car.carId) : null;
            if (r == null || !def) return "They can't use that car.";
            return RivalSim.LeaseRefusal(r, (int)def.category, CarValueCalculator.MarketValue(car), car.condition, car.personal, car.IsHire);
        }

        /// <summary>Your car joins their hire fleet: it leaves your lot now and they pay you every morning until it comes back.</summary>
        public bool Lease(string rivalId, CarDisplay display, int days, out string message)
        {
            var r = RivalSim.Find(rivalId);
            var car = display ? display.Data : null;
            var refusal = LeaseRefusal(rivalId, car);
            if (refusal != null) { message = refusal; return false; }
            var drive = CarSalesServices.TestDrive;
            if (drive && drive.IsDriving && drive.CurrentCar == display) drive.EndDrive();
            if (car.state == CarState.Listed) car.state = CarValueCalculator.DefaultState(car);
            var l = new FleetLease { id = Guid.NewGuid().ToString("N").Substring(0, 8), rival = r.Id, car = car, dailyFee = LeaseFee(rivalId, car), startDay = CarRecords.Today, endDay = CarRecords.Today + days };
            CarSalesServices.Vehicles.Despawn(car.instanceId);
            CarSalesServices.Inventory.Remove(car.instanceId);
            state.leases.Add(l);
            Changed();
            message = $"The {car.Name} joins {r.Name}'s hire fleet for {days} days at {UIFactory.Money(l.dailyFee)} a day. It comes back to a free spot on your lots.";
            return true;
        }

        void PayLeases(int day)
        {
            foreach (var l in state.leases.ToList())
            {
                var r = RivalSim.Find(l.rival);
                int km = RivalSim.LeaseKm(rng.NextDouble());
                l.km += km;
                l.car.mileage += km;
                CarParts.Wear(l.car, km, 1.3f);   // hire customers aren't gentle
                l.car.cleanliness = Mathf.Max(20f, l.car.cleanliness - 12f);
                l.car.fuel = UnityEngine.Random.Range(20f, 60f);
                l.earned += l.dailyFee;
                state.leaseIncome += l.dailyFee;
                CarSalesServices.Economy.AddMoney(l.dailyFee, TxCategory.CarHire, $"Fleet lease - {l.car.Name} at {r?.Name}", l.car.instanceId);
                if (day >= l.endDay) ReturnLease(l, closing: false);
            }
        }

        /// <summary>The car comes back to a free spot (if there's none it stays one more day, still earning).</summary>
        bool ReturnLease(FleetLease l, bool closing)
        {
            var r = RivalSim.Find(l.rival);
            if (DealershipManager.FindFreeSpotAnywhere(out _) == null && !closing)
            {
                l.endDay++;
                if (CarSalesServices.Messages)
                    CarSalesServices.Messages.Add(MessageType.Info, "No space for a returning car", $"{r?.Name} couldn't drop off your {l.car.Name}: no free parking spot. They'll keep it (and keep paying) another day.");
                return false;
            }
            state.leases.Remove(l);
            l.car.state = CarValueCalculator.DefaultState(l.car);
            CarSalesServices.Vehicles.AddOwnedCar(l.car);
            if (CarSalesServices.Messages && !closing)
                CarSalesServices.Messages.Add(MessageType.Info, $"Your {l.car.Name} is back from {r?.Name}",
                    $"It earned {UIFactory.Money(l.earned)} in {Mathf.Max(1, l.endDay - l.startDay)} days and covered {l.km:N0} km. Check it over before you sell it.");
            Changed();
            return true;
        }

        /// <summary>Hand a leased car back early (no further fees).</summary>
        public bool RecallLease(FleetLease l, out string message)
        {
            if (DealershipManager.FindFreeSpotAnywhere(out _) == null) { message = "You need a free parking spot for it."; return false; }
            l.endDay = CarRecords.Today;
            ReturnLease(l, closing: false);
            message = $"The {l.car.Name} is back on your lot.";
            return true;
        }
    }
}
