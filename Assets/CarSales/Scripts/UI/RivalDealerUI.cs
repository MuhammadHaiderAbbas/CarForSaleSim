using System.Linq;
using CarSales.Sim;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>
    /// Phase 14e: a rival dealer's screen (its sales desk, hire desk, or E on one of its cars). Four tabs: buy a car off
    /// the lot (ask for their best price first), sell them one of yours, hire one of their cars (and hand it back), lease
    /// your stock cars to their hire fleet. Created on demand over the UI canvas, like the fuel screen.
    /// </summary>
    public class RivalDealerUI : MonoBehaviour
    {
        static RivalDealerUI open;
        string rivalId, focusId;
        int tab;
        RectTransform list;
        TextMeshProUGUI info, footer;
        UIFactory.TabBar tabs;

        static RivalManager Rivals => CarSalesServices.Rivals;
        RivalProfile Profile => RivalSim.Find(rivalId);
        RivalDealership Site => RivalDealership.For(rivalId);

        public static void Show(string rivalId, int tab) => Open(rivalId, tab, null);

        public static void ShowCar(string rivalId, string instanceId, CarDisplay display)
        {
            var rc = Rivals ? Rivals.Find(instanceId) : null;
            Open(rivalId, rc != null && rc.fleet ? 2 : 0, instanceId);
        }

        static void Open(string rivalId, int tab, string focus)
        {
            if (!Rivals || RivalSim.Find(rivalId) == null) return;
            var canvas = CarSalesServices.UI ? CarSalesServices.UI.transform : null;
            if (!canvas) return;
            if (!RivalManager.IsOpen(rivalId)) { CarSalesServices.UI.Error($"{RivalSim.Find(rivalId).Name} has stopped trading."); return; }
            if (open) Destroy(open.gameObject);
            var root = UIFactory.Rect("RivalDealer", canvas);
            UIFactory.Stretch(root);
            open = root.gameObject.AddComponent<RivalDealerUI>();
            open.rivalId = rivalId;
            open.tab = Mathf.Clamp(tab, 0, 3);
            open.focusId = focus;
            open.Build(root);
            CarSalesServices.UI.Open(root.gameObject);
        }

        void Build(RectTransform root)
        {
            var p = Profile;
            var body = UIFactory.Window(root, $"{p.Name} - car sales & hire", new Vector2(1080f, 740f), Close);
            UIFactory.Vertical(body, 0, 10f);
            info = UIFactory.Text(body, "", 16, UIFactory.TextDim, TextAlignmentOptions.Left);
            info.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.LE(info, h: 44);
            tabs = UIFactory.Tabs(body, new[] { "Buy a car", "Sell us your car", "Hire a car", "Lease us your car" }, i => { tab = i; Refresh(); });
            list = UIFactory.ScrollList(body, 8f);
            footer = UIFactory.Text(body, "", 14, UIFactory.TextFaint, TextAlignmentOptions.Left);
            footer.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.LE(footer, h: 38);
            if (Rivals) Rivals.OnChanged += Refresh;
            tabs.Select(tab);
        }

        void OnDestroy() { if (Rivals) Rivals.OnChanged -= Refresh; }

        void Refresh()
        {
            if (!this || !list) return;
            if (!Rivals || !RivalManager.IsOpen(rivalId)) { Close(); return; }
            UIFactory.ClearChildren(list);
            var s = Rivals.State;
            footer.text = $"You have bought {s.bought} car(s) from rival dealers and sold them {s.sold}. Fleet leasing has earned you {UIFactory.Money(s.leaseIncome)}. " +
                          "Their prices also show as competitor prices on your market page.";
            switch (tab)
            {
                case 0: RenderBuy(); break;
                case 1: RenderSell(); break;
                case 2: RenderHire(); break;
                default: RenderLease(); break;
            }
        }

        // ------------------------------------------------------------------ ROWS
        RectTransform Row(CarInstance car, string text, bool highlight)
        {
            var row = UIFactory.Panel(list, "Row", highlight ? UIFactory.CardLight : UIFactory.CardBg, 1.8f);
            UIFactory.LE(row, h: 100);
            var h = UIFactory.Horizontal(row, 10, 12f);
            h.padding = new RectOffset(10, 12, 8, 8);
            h.childForceExpandHeight = false;
            if (car != null)
            {
                var pic = UIFactory.CarPicture(row.transform, 84f, 146f);
                UIFactory.SetCarPicture(pic, car);
            }
            var t = UIFactory.Text(row.transform, text, 15, UIFactory.TextMain, TextAlignmentOptions.Left);
            t.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.LE(t, flexW: 1);
            return row.rectTransform;
        }

        static Button Button(RectTransform row, string label, Color color, UnityAction act, bool enabled = true, float width = 140f)
        {
            var b = UIFactory.Btn(row, label, color, act, 44f, 14f);
            UIFactory.LE(b, h: 44, w: width, flexW: 0);
            b.interactable = enabled;
            return b;
        }

        static string Title(CarInstance car)
        {
            var d = CarSalesServices.Database ? CarSalesServices.Database.Get(car.carId) : null;
            return d ? $"<b>{d.year} {d.brand} {d.displayName}</b>" : $"<b>{car.Name}</b>";
        }

        static string Specs(CarInstance car)
        {
            var d = CarSalesServices.Database ? CarSalesServices.Database.Get(car.carId) : null;
            return $"{CarParts.Label(car.condition)} {car.condition:0}%  -  {car.mileage:N0} km" + (d ? $"  -  {d.fuelType}, {d.transmission}" : "");
        }

        void Done(bool ok, string message)
        {
            if (ok) CarSalesServices.UI.Success(Profile.Name, message);
            else CarSalesServices.UI.Error(message);
            Refresh();
        }

        // ------------------------------------------------------------------ BUY
        void RenderBuy()
        {
            var p = Profile;
            var stock = Rivals.Stock(rivalId).OrderBy(c => c.car.instanceId == focusId ? 0 : 1).ThenBy(c => Rivals.PriceOf(c)).ToList();
            info.text = $"{p.Tagline}. {stock.Count} cars on the lot today - descriptions are honest and the condition is checked. " +
                        "Ask for their best price before you buy; the longer a car has been on the lot, the more they'll take off.";
            foreach (var c in stock)
            {
                int value = CarValueCalculator.MarketValue(c.car);
                int price = Rivals.PriceOf(c);
                string priceText = c.best > 0
                    ? $"<s>{UIFactory.Money(c.asking)}</s>  <color={UIFactory.Hex(UIFactory.Good)}><b>{UIFactory.Money(c.best)}</b> best price</color>"
                    : $"<b>{UIFactory.Money(c.asking)}</b>";
                var row = Row(c.car, $"{Title(c.car)}\n<size=88%>{Specs(c.car)}</size>\n<size=88%>{priceText}  -  market value about {UIFactory.Money(value)}  -  on the lot {Mathf.Max(0, CarRecords.Today - c.listedDay)} day(s)</size>",
                    c.car.instanceId == focusId);
                var car = c;
                Button(row, c.best > 0 ? "Price asked" : "Best price?", UIFactory.CardLight, () =>
                {
                    int best = Rivals.AskBestPrice(car);
                    CarSalesServices.UI.Notify($"\"For you, {UIFactory.Money(best)}. That's as low as I can go.\"");
                    Refresh();
                }, c.best <= 0, 130f);
                Button(row, $"Buy {UIFactory.Money(price)}", UIFactory.Good, () => Confirm($"Buy from {p.Name}?",
                    $"{car.car.Name} for {UIFactory.Money(Rivals.PriceOf(car))}. It's left on their lot for you to drive away or tow.", () =>
                    {
                        var at = Site ? Site.HandoverFor(car.car.instanceId) : ((Vector3, float)?)null;
                        bool ok = at.HasValue ? Rivals.Buy(car, at.Value.Item1, at.Value.Item2, out var msg) : Rivals.Buy(car, null, 0f, out msg);
                        Done(ok, msg);
                    }), CarSalesServices.Economy.CanAfford(price), 160f);
            }
            if (stock.Count == 0) UIFactory.EmptyState(list, "The lot is empty today", "They restock every morning.");
        }

        // ------------------------------------------------------------------ SELL
        void RenderSell()
        {
            var p = Profile;
            var site = Site;
            info.text = $"{p.Name} pay cash on the spot. Bring the car into their yard, or they'll collect it from wherever it is for {UIFactory.Money(RivalSim.CollectionFee)}. " +
                        (p.Prefers != null && p.Prefers.Length > 0 ? $"They pay most for: {string.Join(", ", p.Prefers)}." : "They'll take anything.") +
                        (p.MinValue > 0 ? $" Only cars worth over {UIFactory.Money(p.MinValue)}." : "");
            var cars = CarSalesServices.Vehicles ? CarSalesServices.Vehicles.All.Where(d => d.Data != null && !d.Data.personal).ToList() : new System.Collections.Generic.List<CarDisplay>();
            foreach (var d in cars.OrderBy(d => site && site.InYard(d) ? 0 : 1))
            {
                int offer = Rivals.Offer(rivalId, d.Data, out var refusal);
                bool here = site && site.InYard(d);
                int pay = Mathf.Max(0, offer - (here ? 0 : RivalSim.CollectionFee));
                int value = CarValueCalculator.MarketValue(d.Data);
                int invested = d.Data.purchasePrice + d.Data.repairSpent;
                string where = CarSalesServices.Vehicles.LocationText(d.Data);
                string line = refusal != null
                    ? $"<color={UIFactory.Hex(UIFactory.Warn)}>{refusal}</color>"
                    : $"They offer <b>{UIFactory.Money(pay)}</b>{(here ? "" : $" (collected from {where})")}  -  market value {UIFactory.Money(value)}  -  you've put in {UIFactory.Money(invested)}" +
                      $"  -  <color={UIFactory.Hex(pay - invested >= 0 ? UIFactory.Good : UIFactory.Bad)}>{UIFactory.Money(pay - invested)}</color>";
                var row = Row(d.Data, $"{Title(d.Data)}\n<size=88%>{Specs(d.Data)}  -  {where}</size>\n<size=88%>{line}</size>", here);
                var car = d;
                Button(row, offer > 0 ? $"Sell {UIFactory.Money(pay)}" : "Not wanted", UIFactory.Good, () => Confirm($"Sell to {p.Name}?",
                    $"{car.Definition?.displayName} for {UIFactory.Money(pay)}. This can't be undone.", () =>
                    {
                        bool ok = Rivals.SellTo(rivalId, car, !(Site && Site.InYard(car)), out var msg);
                        Done(ok, msg);
                    }), offer > 0, 160f);
            }
            if (cars.Count == 0) UIFactory.EmptyState(list, "You have no stock cars to sell", "Your personal car and hire cars can't be sold.");
        }

        // ------------------------------------------------------------------ HIRE
        void RenderHire()
        {
            var p = Profile;
            var site = Site;
            var hire = Rivals.ActiveHire;
            info.text = $"Hire by the day: {RivalSim.FreeKmPerDay} km a day included, a week or more is 15% cheaper. Pay up front plus a refundable deposit. " +
                        $"Bring it back full and undamaged; late days cost {RivalSim.LateFactor:0.#}x, and after {RivalSim.GraceDays} late days they collect it.";
            if (hire != null)
            {
                var car = CarSalesServices.Inventory.Get(hire.carInstanceId);
                var display = CarSalesServices.Vehicles.GetDisplay(hire.carInstanceId);
                var from = RivalSim.Find(hire.rival);
                var bill = Rivals.BillFor(hire, car);
                bool mine = hire.rival == rivalId;
                bool here = mine && display && site && site.InYard(display);
                string status = !mine ? $"Hired from {from?.Name} - hand it back there."
                    : here ? "It's in the yard: hand it back now."
                    : $"Bring it into {p.Name}'s yard to hand it back (it's at {CarSalesServices.Vehicles.LocationText(car)}).";
                var row = Row(car, $"<b>Your hire car:</b> {(car != null ? Title(car) : "")}  -  due back day {hire.dueDay} ({(CarRecords.Today > hire.dueDay ? $"<color={UIFactory.Hex(UIFactory.Bad)}>{CarRecords.Today - hire.dueDay} day(s) late</color>" : $"{hire.dueDay - CarRecords.Today} day(s) left")})\n" +
                    $"<size=88%>If returned now: fuel {UIFactory.Money(bill.Fuel)}, damage {UIFactory.Money(bill.Damage)}, extra km {UIFactory.Money(bill.Excess)}, late {UIFactory.Money(bill.Late)}  -  deposit {UIFactory.Money(hire.deposit)}, back to you {UIFactory.Money(bill.Refund)}</size>\n<size=88%>{status}</size>", true);
                Button(row, "Hand it back", UIFactory.Accent, () => { bool ok = Rivals.ReturnHire(hire, display, out var msg); Done(ok, msg); }, here, 150f);
            }
            var fleet = Rivals.Fleet(rivalId).OrderBy(c => c.car.instanceId == focusId ? 0 : 1).ToList();
            foreach (var c in fleet)
            {
                int rate = Rivals.DailyRate(c), deposit = Rivals.Deposit(c);
                var row = Row(c.car, $"{Title(c.car)}\n<size=88%>{Specs(c.car)}  -  full tank</size>\n<size=88%><b>{UIFactory.Money(rate)}</b> a day  -  deposit {UIFactory.Money(deposit)} (refundable)</size>", c.car.instanceId == focusId);
                var car = c;
                foreach (int days in RivalSim.HireDays)
                {
                    int cost = RivalSim.HireCost(rate, days), n = days;
                    Button(row, $"{days} day{(days == 1 ? "" : "s")} {UIFactory.Money(cost)}", UIFactory.Good, () =>
                    {
                        var at = Site ? Site.HandoverFor(car.car.instanceId) : ((Vector3, float)?)null;
                        var pos = at.HasValue ? at.Value.Item1 : (Vector3?)null;
                        bool ok = Rivals.Hire(car, n, pos, at.HasValue ? at.Value.Item2 : 0f, out var msg);
                        Done(ok, msg);
                    }, hire == null && CarSalesServices.Economy.CanAfford(cost + deposit), 136f);
                }
            }
            if (fleet.Count == 0 && hire == null) UIFactory.EmptyState(list, "All their hire cars are out", "Try again tomorrow.");
        }

        // ------------------------------------------------------------------ LEASE
        void RenderLease()
        {
            var p = Profile;
            info.text = $"{p.Name} will take stock cars into their hire fleet and pay you every morning. The car leaves your lot and comes back to a free spot " +
                        $"with more kilometres and some wear. They hire out: {string.Join(", ", p.Fleet.Distinct())}; condition at least {RivalSim.MinLeaseCondition:0}%.";
            foreach (var l in Rivals.Leases(rivalId).ToList())
            {
                var lease = l;
                var row = Row(l.car, $"<b>In their fleet:</b> {Title(l.car)}\n<size=88%>{UIFactory.Money(l.dailyFee)} a day  -  back on day {l.endDay}  -  earned {UIFactory.Money(l.earned)} so far, {l.km:N0} km</size>", true);
                Button(row, "Bring it back", UIFactory.CardLight, () => { bool ok = Rivals.RecallLease(lease, out var msg); Done(ok, msg); }, true, 150f);
            }
            var cars = CarSalesServices.Vehicles ? CarSalesServices.Vehicles.All.Where(d => d.Data != null && !d.Data.personal).ToList() : new System.Collections.Generic.List<CarDisplay>();
            foreach (var d in cars)
            {
                var refusal = Rivals.LeaseRefusal(rivalId, d.Data);
                int fee = Rivals.LeaseFee(rivalId, d.Data);
                var row = Row(d.Data, $"{Title(d.Data)}\n<size=88%>{Specs(d.Data)}  -  {CarSalesServices.Vehicles.LocationText(d.Data)}</size>\n<size=88%>" +
                    (refusal != null ? $"<color={UIFactory.Hex(UIFactory.Warn)}>{refusal}</color>" : $"They pay <b>{UIFactory.Money(fee)}</b> a day") + "</size>", false);
                var car = d;
                foreach (int days in RivalSim.LeaseDays)
                {
                    int n = days;
                    Button(row, $"{days} days +{UIFactory.Money(fee * days)}", UIFactory.Good, () =>
                    {
                        bool ok = Rivals.Lease(rivalId, car, n, out var msg);
                        Done(ok, msg);
                    }, refusal == null, 140f);
                }
            }
            if (cars.Count == 0 && !Rivals.Leases(rivalId).Any()) UIFactory.EmptyState(list, "No stock cars to lease", "Buy some cars first.");
        }

        // ------------------------------------------------------------------ CLOSE
        static void Confirm(string title, string body, UnityAction ok) => UIFactory.Confirm(title, body, "Yes", UIFactory.Good, ok);

        void Close()
        {
            if (CarSalesServices.UI && CarSalesServices.UI.IsMenuOpen) CarSalesServices.UI.Close();
            else if (this) Destroy(gameObject);
        }

        // Closed by Esc / UIManager (the panel is just hidden): remove it.
        void OnDisable() { if (open == this) open = null; if (this && gameObject) Destroy(gameObject); }
    }
}
