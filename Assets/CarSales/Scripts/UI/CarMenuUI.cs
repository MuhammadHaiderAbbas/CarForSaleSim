using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>"My Vehicle" screen for a car you own: photo, stats and all actions.</summary>
    public class CarMenuUI : UIPanel
    {
        CarDisplay car;
        RawImage photo;
        TextMeshProUGUI nameText, valueText, condLabel, cleanLabel, mileage, fuel, bought, spent, invested, profit, listed, quickSellLabel;
        Image stateChip;
        TextMeshProUGUI stateChipText;
        UIFactory.Bar condBar, cleanBar;
        Button testDrive;
        bool confirmQuickSell;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "My Vehicle", new Vector2(1260f, 720f), CloseAll);
            UIFactory.Horizontal(body, 0, 28f, TextAnchor.UpperLeft).childForceExpandHeight = true;

            var left = UIFactory.VCol(body, 10f, "Left", 600f);
            photo = UIFactory.CarPicture(left, 330f);
            var chips = UIFactory.HRow(left, 30, 8, "Chips", false);
            stateChip = UIFactory.Chip(chips, "", UIFactory.Neutral, out stateChipText, 15f);
            UIFactory.Spacer(chips, 0f, 1f);
            condLabel = UIFactory.Text(left, "", 18, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            condBar = UIFactory.ProgressBar(left, UIFactory.Good, 14f);
            cleanLabel = UIFactory.Text(left, "", 18, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            cleanBar = UIFactory.ProgressBar(left, UIFactory.Accent, 14f);
            UIFactory.Spacer(left);

            var right = UIFactory.VCol(body, 6f, "Right");
            nameText = UIFactory.Text(right, "", 32, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(nameText, h: 42);
            valueText = UIFactory.Text(right, "", 30, UIFactory.Gold, TextAlignmentOptions.Left, true);
            UIFactory.LE(valueText, h: 40);
            UIFactory.Divider(right);
            mileage = UIFactory.SpecRow(right, "Mileage", 28);
            fuel = UIFactory.SpecRow(right, "Fuel / Transmission", 28);
            bought = UIFactory.SpecRow(right, "Bought for", 28);
            spent = UIFactory.SpecRow(right, "Repairs & cleaning", 28);
            invested = UIFactory.SpecRow(right, "Total invested", 28);
            profit = UIFactory.SpecRow(right, "Potential profit", 28);
            listed = UIFactory.SpecRow(right, "Listed price", 28);
            UIFactory.Spacer(right);

            var gridRt = UIFactory.Rect("Actions", right);
            var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(186f, 52f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            UIFactory.LE(gridRt, h: 116);
            UIFactory.Btn(gridRt, "Inspect", UIFactory.Neutral, () => CarSalesServices.UI.inspection.Show(car.Data, car), 52f, 18f);
            UIFactory.Btn(gridRt, "Garage / Repair", UIFactory.Warn, () => CarSalesServices.UI.garage.Show(car), 52f, 18f);
            UIFactory.Btn(gridRt, "Car Wash", UIFactory.Accent, () => CarSalesServices.UI.carWash.Show(car), 52f, 18f);
            UIFactory.Btn(gridRt, "Set Price", UIFactory.Good, () => CarSalesServices.UI.priceUI.Show(car), 52f, 18f);
            testDrive = UIFactory.Btn(gridRt, "Test Drive", new Color(0.55f, 0.3f, 0.8f), () => CarSalesServices.TestDrive.StartDrive(car), 52f, 18f);
            UIFactory.Btn(gridRt, "Quick Sell", UIFactory.Bad, QuickSell, out quickSellLabel, 52f, 16f);
        }

        public void Show(CarDisplay target)
        {
            car = target;
            confirmQuickSell = false;
            OpenSelf();
            Refresh();
        }

        void Refresh()
        {
            var d = car.Data;
            var def = car.Definition;
            int value = CarValueCalculator.MarketValue(d);
            int inv = d.purchasePrice + d.repairSpent;
            UIFactory.SetCarPicture(photo, d);
            nameText.text = $"{def.year} {def.brand} {def.displayName}";
            valueText.text = $"<size=60%><color={UIFactory.Hex(UIFactory.TextDim)}>Market value </color></size>{UIFactory.Money(value)}";
            condLabel.text = $"Condition: <color={UIFactory.Hex(CarParts.LabelColor(d.condition))}>{CarParts.Label(d.condition)} ({d.condition:0}%)</color>";
            condBar.Set(d.condition / 100f);
            condBar.SetColor(CarParts.LabelColor(d.condition));
            cleanLabel.text = $"Cleanliness: {d.cleanliness:0}%";
            cleanBar.Set(d.cleanliness / 100f);
            mileage.text = $"{d.mileage:N0} km";
            fuel.text = $"{(string.IsNullOrEmpty(def.fuelType) ? "Petrol" : def.fuelType)} / {(string.IsNullOrEmpty(def.transmission) ? "Automatic" : def.transmission)}";
            bought.text = UIFactory.Money(d.purchasePrice);
            spent.text = UIFactory.Money(d.repairSpent);
            invested.text = UIFactory.Money(inv);
            profit.text = $"<color={UIFactory.Hex(value - inv >= 0 ? UIFactory.Good : UIFactory.Bad)}>{UIFactory.Money(value - inv)}</color>";
            listed.text = d.state == CarState.Listed ? UIFactory.Money(d.listedPrice) : "Not listed";

            stateChip.color = d.state == CarState.Listed ? UIFactory.Good : d.state == CarState.NeedsRepair ? UIFactory.Warn : UIFactory.Neutral;
            stateChipText.text = d.state == CarState.Listed ? "FOR SALE" : d.state == CarState.NeedsRepair ? "NEEDS REPAIR" : "READY";
            testDrive.interactable = CarSalesServices.TestDrive && !CarSalesServices.TestDrive.IsDriving;
            quickSellLabel.text = confirmQuickSell ? $"Confirm {UIFactory.Money(CarValueCalculator.QuickSellPrice(d))}" : "Quick Sell";
        }

        void QuickSell()
        {
            if (!confirmQuickSell)
            {
                confirmQuickSell = true;
                CarSalesServices.UI.Notify($"A wholesaler offers {UIFactory.Money(CarValueCalculator.QuickSellPrice(car.Data))} (75% of value). Click again to confirm.");
                Refresh();
                return;
            }
            CarSalesServices.UI.Close();
            CarSalesServices.Dealership.QuickSell(car);
        }
    }
}
