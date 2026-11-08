using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>Marketplace page: search, category filter, sorting, car cards.</summary>
    public class MarketplaceUI : ComputerPage
    {
        enum Sort { Newest, PriceLow, PriceHigh, Condition }

        RectTransform grid;
        TMP_InputField search;
        TextMeshProUGUI info;
        CarCategory? category;
        Sort sort = Sort.Newest;
        readonly List<KeyValuePair<object, Button>> chips = new List<KeyValuePair<object, Button>>();

        public override void Build()
        {
            UIFactory.Vertical(Area, 0, 12f);

            var top = UIFactory.HRow(Area, 46, 10, "SearchRow", false);
            search = UIFactory.MakeInput(top, "Search cars (name, brand)...", 46f);
            search.onValueChanged.AddListener(_ => Refresh());
            SortChip(top, "Newest", Sort.Newest);
            SortChip(top, "Price: Low", Sort.PriceLow);
            SortChip(top, "Price: High", Sort.PriceHigh);
            SortChip(top, "Condition", Sort.Condition);

            var cats = UIFactory.HRow(Area, 38, 8, "Categories", false);
            CategoryChip(cats, "All Types", null);
            foreach (CarCategory c in System.Enum.GetValues(typeof(CarCategory))) CategoryChip(cats, c.ToString(), c);
            UIFactory.Spacer(cats, 0f, 1f);

            info = UIFactory.Text(Area, "", 16, UIFactory.TextDim, TextAlignmentOptions.Left);
            UIFactory.LE(info, h: 24);
            grid = UIFactory.ScrollGrid(Area, new Vector2(285f, 330f), 4, 16f);
        }

        void SortChip(Transform parent, string label, Sort s)
        {
            var b = UIFactory.Btn(parent, label, UIFactory.Neutral, () => { sort = s; Refresh(); }, 46f, 16f);
            UIFactory.LE(b, h: 46, w: 128, flexW: 0);
            chips.Add(new KeyValuePair<object, Button>(s, b));
        }

        void CategoryChip(Transform parent, string label, CarCategory? c)
        {
            var b = UIFactory.Btn(parent, label, UIFactory.Neutral, () => { category = c; Refresh(); }, 38f, 15f);
            UIFactory.LE(b, h: 38, w: 118, flexW: 0);
            chips.Add(new KeyValuePair<object, Button>(c.HasValue ? (object)c.Value : "all", b));
        }

        public override void Refresh()
        {
            foreach (var chip in chips)
            {
                bool on = chip.Key is Sort s ? s == sort
                        : chip.Key is CarCategory c ? category.HasValue && category.Value == c
                        : !category.HasValue;
                UIFactory.SetButtonColor(chip.Value, on ? UIFactory.Accent : UIFactory.Neutral);
            }

            var db = CarSalesServices.Database;
            IEnumerable<CarInstance> list = CarSalesServices.Marketplace.Listings;
            string q = search ? search.text.Trim().ToLowerInvariant() : "";
            if (q.Length > 0)
                list = list.Where(c => { var d = db.Get(c.carId); return d && (d.displayName + " " + d.brand + " " + d.category).ToLowerInvariant().Contains(q); });
            if (category.HasValue) list = list.Where(c => { var d = db.Get(c.carId); return d && d.category == category.Value; });
            switch (sort)
            {
                case Sort.PriceLow: list = list.OrderBy(c => c.purchasePrice); break;
                case Sort.PriceHigh: list = list.OrderByDescending(c => c.purchasePrice); break;
                case Sort.Condition: list = list.OrderByDescending(c => CarValueCalculator.IsConditionKnown(c) ? c.condition : c.reportedCondition); break;
            }
            var shown = list.ToList();

            UIFactory.ClearChildren(grid);
            foreach (var car in shown) MarketListingItemUI.Create(grid, car, this);

            bool mech = CarSalesServices.Upgrades && CarSalesServices.Upgrades.AccurateInspections;
            info.text = $"{shown.Count} of {CarSalesServices.Marketplace.Listings.Count} cars shown  -  new cars arrive every morning  -  " +
                        (mech ? "<color=#6f6>Mechanic: true condition shown</color>" : $"Inspections cost ${CarValueCalculator.InspectionFee}. Sellers may exaggerate!");
            if (shown.Count == 0)
            {
                var empty = UIFactory.Text(grid, "No cars match your search.", 20, UIFactory.TextDim, TextAlignmentOptions.Center);
                UIFactory.LE(empty, h: 60);
            }
        }

        public void OpenDetails(CarInstance car) => CarSalesServices.UI.vehicleDetails.Show(car);

        public void Buy(CarInstance car)
        {
            int price = car.purchasePrice;
            if (CarSalesServices.Marketplace.TryBuy(car, out string msg))
            {
                var spot = car.parkingSpotIndex;
                CarSalesServices.UI.purchaseConfirm.Show(car, price, spot, msg);
            }
            else CarSalesServices.UI.Error(msg);
        }
    }
}
