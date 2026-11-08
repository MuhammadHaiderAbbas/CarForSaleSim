using CarSales.Foundation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>Marketplace listing details: photo, price, specs, inspection and buying.</summary>
    public class VehicleDetailsUI : UIPanel
    {
        CarInstance car;
        CarDisplay physical;   // set when viewing a car at the Used Car Market
        RawImage photo;
        TextMeshProUGUI buyLabel, backLabel;
        TextMeshProUGUI nameText, priceText, description, year, mileage, condition, clean, fuel, gearbox, value, inspectLabel;
        Image conditionChip;
        TextMeshProUGUI conditionChipText;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "Vehicle Details", new Vector2(1240f, 700f), Back);
            UIFactory.Horizontal(body, 0, 28f, TextAnchor.UpperLeft).childForceExpandHeight = true;

            var left = UIFactory.VCol(body, 14f, "Left", 640f);
            photo = UIFactory.CarPicture(left, 390f);
            var chips = UIFactory.HRow(left, 30, 8, "Chips", false);
            conditionChip = UIFactory.Chip(chips, "", UIFactory.Neutral, out conditionChipText, 15f);
            UIFactory.Spacer(chips, 0f, 1f);
            description = UIFactory.Text(left, "", 17, UIFactory.TextDim, TextAlignmentOptions.TopLeft);
            UIFactory.LE(description, flexH: 1);

            var right = UIFactory.VCol(body, 6f, "Right");
            nameText = UIFactory.Text(right, "", 34, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(nameText, h: 46);
            priceText = UIFactory.Text(right, "", 44, UIFactory.Gold, TextAlignmentOptions.Left, true);
            UIFactory.LE(priceText, h: 56);
            UIFactory.Divider(right);
            year = UIFactory.SpecRow(right, "Year");
            mileage = UIFactory.SpecRow(right, "Mileage");
            condition = UIFactory.SpecRow(right, "Condition");
            clean = UIFactory.SpecRow(right, "Cleanliness");
            fuel = UIFactory.SpecRow(right, "Fuel Type");
            gearbox = UIFactory.SpecRow(right, "Transmission");
            value = UIFactory.SpecRow(right, "Est. Market Value");
            UIFactory.Spacer(right);
            var row = UIFactory.HRow(right, 56, 12);
            UIFactory.Btn(row, "Inspect", UIFactory.Warn, Inspect, out inspectLabel, 54f, 18f);
            UIFactory.Btn(row, "Buy Now", UIFactory.Good, Buy, out buyLabel, 54f, 19f);
            UIFactory.Btn(right, "Back to Marketplace", UIFactory.Neutral, Back, out backLabel, 46f, 17f);
        }

        /// <summary>A car standing at the Used Car Market (free inspection, drive it away).</summary>
        public void ShowPhysical(CarDisplay display)
        {
            physical = display;
            ShowInternal(display.Data);
        }

        public void Show(CarInstance listing)
        {
            physical = null;
            ShowInternal(listing);
        }

        /// <summary>Returns to the same car (online or physical) after the inspection screen.</summary>
        public void Reopen() { if (physical) ShowPhysical(physical); else Show(car); }

        void Back()
        {
            if (physical) CloseAll();
            else CarSalesServices.UI.marketplace.Show();
        }

        void Buy()
        {
            if (!physical) { CarSalesServices.UI.marketplace.Buy(car); return; }
            int price = car.purchasePrice;
            if (CarSalesServices.Marketplace.TryBuyFromLot(physical, out string msg))
                CarSalesServices.UI.purchaseConfirm.Show(car, price, -1, msg);
            else CarSalesServices.UI.Error(msg);
        }

        void ShowInternal(CarInstance listing)
        {
            car = listing;
            if (physical) car.inspected = true;   // you can look the car over yourself on the lot
            OpenSelf();
            var def = CarSalesServices.Database.Get(car.carId);
            UIFactory.SetCarPicture(photo, car);
            bool known = CarValueCalculator.IsConditionKnown(car);
            float cond = known ? car.condition : car.reportedCondition;

            nameText.text = $"{def.year} {def.brand} {def.displayName}";
            priceText.text = UIFactory.Money(car.purchasePrice);
            year.text = def.year.ToString();
            mileage.text = $"{car.mileage:N0} km";
            condition.text = $"<color={UIFactory.Hex(CarParts.LabelColor(cond))}>{(known ? "" : "~")}{cond:0}% {CarParts.Label(cond)}</color>";
            clean.text = $"{car.cleanliness:0}%";
            fuel.text = string.IsNullOrEmpty(def.fuelType) ? "Petrol" : def.fuelType;
            gearbox.text = string.IsNullOrEmpty(def.transmission) ? "Automatic" : def.transmission;
            int estimate = CarValueCalculator.MarketValue(car, useReported: !known);
            value.text = UIFactory.Money(estimate) + (known ? "" : " (est.)");

            conditionChip.color = known ? UIFactory.Good : UIFactory.Warn;
            conditionChipText.text = known ? "Condition verified" : "Seller's claim - not inspected";

            string flavour = cond >= 80 ? "Well maintained and ready to sell." : cond >= 60 ? "Decent car, might need a little work."
                           : cond >= 40 ? "Needs some repairs before resale." : "Project car - expect major repairs.";
            description.text = (string.IsNullOrEmpty(def.description) ? "" : def.description + "\n\n") +
                               $"{def.category} for sale with {car.mileage:N0} km on the clock. {flavour}\n\n" +
                               $"<color={UIFactory.Hex(UIFactory.TextMain)}>Price vs. estimated value: {(float)car.purchasePrice / Mathf.Max(1, estimate) * 100f:0}%</color>";

            int fee = CarValueCalculator.InspectionCost();
            inspectLabel.text = car.inspected ? "Inspection Report" : fee > 0 ? $"Inspect ({UIFactory.Money(fee)})" : "Inspect (free)";
            int delivery = CarSalesServices.Marketplace.DeliveryFee;
            buyLabel.text = physical ? $"Buy Now - drive it away" : $"Buy Now\n+{UIFactory.Money(delivery)} delivery";
            backLabel.text = physical ? "Close" : "Back to Marketplace";
        }

        void Inspect()
        {
            if (!car.inspected)
            {
                int fee = CarValueCalculator.InspectionCost();
                if (fee > 0 && !CarSalesServices.Economy.TrySpend(fee, TxCategory.Inspection, $"Inspection - {car.Name}", car.instanceId)) { CarSalesServices.UI.Error("Not enough money for an inspection."); return; }
                if (fee > 0) CarSalesServices.DayCycle.RegisterExpense(fee);
                car.inspected = true;
            }
            CarSalesServices.UI.inspection.Show(car, null);
        }
    }
}
