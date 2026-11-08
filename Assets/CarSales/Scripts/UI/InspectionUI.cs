using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>Per-part inspection report (engine, brakes, tires, body, interior, electronics).</summary>
    public class InspectionUI : UIPanel
    {
        CarInstance car;
        CarDisplay owned;   // null = marketplace listing
        RawImage photo;
        TextMeshProUGUI nameText, overallText, verdictText, claimText;
        Image verdictPanel;
        readonly List<Image> icons = new List<Image>();
        readonly List<TextMeshProUGUI> values = new List<TextMeshProUGUI>();
        readonly List<UIFactory.Bar> bars = new List<UIFactory.Bar>();

        public override void Build()
        {
            var body = UIFactory.Window(Root, "Vehicle Inspection", new Vector2(1100f, 680f), Back);
            UIFactory.Horizontal(body, 0, 28f, TextAnchor.UpperLeft).childForceExpandHeight = true;

            var left = UIFactory.VCol(body, 12f, "Left", 450f);
            photo = UIFactory.CarPicture(left, 280f);
            nameText = UIFactory.Text(left, "", 26, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            overallText = UIFactory.Text(left, "", 20, UIFactory.TextDim, TextAlignmentOptions.Left);
            claimText = UIFactory.Text(left, "", 17, UIFactory.Warn, TextAlignmentOptions.Left);
            UIFactory.Spacer(left);
            UIFactory.Btn(left, "Back", UIFactory.Neutral, Back, 48f);

            var right = UIFactory.VCol(body, 10f, "Right");
            UIFactory.LE(UIFactory.Text(right, "Inspection Report", 24, UIFactory.TextMain, TextAlignmentOptions.Left, true), h: 34);
            foreach (var part in CarParts.All)
            {
                var row = UIFactory.Panel(right, part.ToString(), UIFactory.CardBg, 1.8f);
                UIFactory.LE(row, h: 56);
                var h = UIFactory.Horizontal(row, 10, 14f);
                h.padding = new RectOffset(14, 16, 8, 8);
                h.childForceExpandHeight = false;
                icons.Add(UIFactory.Icon(row.transform, UIFactory.Good, "", 32f, true));
                var name = UIFactory.Text(row.transform, CarParts.DisplayName(part), 20, UIFactory.TextMain, TextAlignmentOptions.Left, true);
                UIFactory.LE(name, w: 150, flexW: 0);
                var barHolder = UIFactory.VCol(row.transform, 0f, "BarHolder");
                barHolder.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
                bars.Add(UIFactory.ProgressBar(barHolder, UIFactory.Good, 12f));
                var val = UIFactory.Text(row.transform, "", 18, UIFactory.TextMain, TextAlignmentOptions.Right, true);
                UIFactory.LE(val, w: 150, flexW: 0);
                values.Add(val);
            }
            UIFactory.Spacer(right);
            verdictPanel = UIFactory.Panel(right, "Verdict", UIFactory.Good, 1.6f);
            UIFactory.LE(verdictPanel, h: 60);
            verdictText = UIFactory.Text(verdictPanel.transform, "", 22, Color.white, TextAlignmentOptions.Center, true);
            UIFactory.Stretch(verdictText.rectTransform, 6f);
        }

        /// <summary>owner = the parked car when inspecting your own car, null for a marketplace listing.</summary>
        public void Show(CarInstance target, CarDisplay owner)
        {
            car = target;
            owned = owner;
            OpenSelf();
            CarParts.EnsureParts(car);
            var def = CarSalesServices.Database.Get(car.carId);
            UIFactory.SetCarPicture(photo, car);
            nameText.text = $"{def.year} {def.brand} {def.displayName}";
            overallText.text = $"Overall: <color={UIFactory.Hex(CarParts.LabelColor(car.condition))}><b>{car.condition:0}% {CarParts.Label(car.condition)}</b></color>\n" +
                               $"Full repair: {UIFactory.Money(CarValueCalculator.RepairCost(car))}";
            float lie = car.reportedCondition - car.condition;
            claimText.text = owner == null && lie >= 5f
                ? $"Seller claimed {car.reportedCondition:0}% - that's {lie:0}% better than reality!"
                : owner == null ? "The seller's description was honest." : "";

            for (int i = 0; i < CarParts.All.Length; i++)
            {
                float v = CarParts.Get(car, CarParts.All[i]);
                Color c = CarParts.LabelColor(v);
                icons[i].color = c;
                bars[i].Set(v / 100f);
                bars[i].SetColor(c);
                values[i].text = $"<color={UIFactory.Hex(c)}>{CarParts.Label(v)}</color>  {v:0}%";
            }
            verdictPanel.color = CarParts.LabelColor(car.condition);
            verdictText.text = car.condition >= 85 ? "Vehicle is in Excellent Condition!"
                             : car.condition >= 65 ? "Vehicle is in Good Condition!"
                             : car.condition >= 40 ? "Fair condition - some repairs needed"
                             : "Poor condition - major repairs needed";
        }

        void Back()
        {
            if (owned) CarSalesServices.UI.carMenu.Show(owned);
            else CarSalesServices.UI.vehicleDetails.Reopen();
        }
    }
}
