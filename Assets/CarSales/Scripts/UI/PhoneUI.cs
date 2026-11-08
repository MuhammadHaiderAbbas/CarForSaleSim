using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>Phone (P): call a tow truck for any of your cars, or a taxi for yourself.</summary>
    public class PhoneUI : UIPanel
    {
        enum Mode { Tow, Taxi }
        Mode mode;
        CarDisplay selected;
        RectTransform left, right;
        TextMeshProUGUI hint;
        Button towTab, taxiTab;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "Phone", new Vector2(1180f, 820f), CloseAll);
            UIFactory.Vertical(body, 0, 12f);
            var tabs = UIFactory.HRow(body, 50, 0, "Tabs", true);
            towTab = UIFactory.Btn(tabs, "Tow Truck", UIFactory.Accent, () => { mode = Mode.Tow; Refresh(); }, 50f, 19f);
            taxiTab = UIFactory.Btn(tabs, "Taxi", UIFactory.Neutral, () => { mode = Mode.Taxi; Refresh(); }, 50f, 19f);
            hint = UIFactory.Text(body, "", 16, UIFactory.TextDim, TextAlignmentOptions.Left);
            UIFactory.LE(hint, h: 26);
            var row = UIFactory.Rect("Columns", body);
            UIFactory.LE(row, flexH: 1);
            UIFactory.Horizontal(row, 0, 18f, TextAnchor.UpperLeft, true);
            var l = UIFactory.VCol(row, 8f, "Left");
            left = UIFactory.ScrollList(l, 8f);
            var r = UIFactory.VCol(row, 8f, "Right");
            right = UIFactory.ScrollList(r, 8f);
        }

        public void Show() { mode = Mode.Tow; OpenSelf(); Refresh(); }

        public void ShowTow(CarDisplay car)
        {
            mode = Mode.Tow;
            selected = car;
            OpenSelf();
            Refresh();
        }

        void Refresh()
        {
            UIFactory.SetButtonColor(towTab, mode == Mode.Tow ? UIFactory.Accent : UIFactory.Neutral);
            UIFactory.SetButtonColor(taxiTab, mode == Mode.Taxi ? UIFactory.Accent : UIFactory.Neutral);
            UIFactory.ClearChildren(left);
            UIFactory.ClearChildren(right);
            if (mode == Mode.Tow) BuildTow(); else BuildTaxi();
        }

        void BuildTow()
        {
            hint.text = "1) Pick a car   2) Pick where the tow truck should take it. Price depends on distance.";
            foreach (var d in CarSalesServices.Vehicles.All.OrderByDescending(x => x.Data.personal))
            {
                var car = d;
                bool sel = selected == car;
                var item = UIFactory.Panel(left, "Car", sel ? UIFactory.AccentDark : UIFactory.CardBg, 1.8f);
                UIFactory.LE(item, h: 76);
                var btn = item.gameObject.AddComponent<Button>();
                btn.targetGraphic = item;
                btn.onClick.AddListener(() => { selected = car; Refresh(); });
                var h = UIFactory.Horizontal(item, 8, 12f);
                h.childForceExpandHeight = false;
                var pic = UIFactory.CarPicture(item.transform, 60f, 96f);
                UIFactory.SetCarPicture(pic, car.Data);
                var col = UIFactory.VCol(item.transform, 0f);
                UIFactory.Text(col, (car.Data.personal ? "[Your car] " : "") + $"{car.Definition.brand} {car.Definition.displayName}", 17, UIFactory.TextMain, TextAlignmentOptions.Left, true);
                UIFactory.Text(col, $"{CarSalesServices.Vehicles.LocationText(car.Data)}   Fuel {car.Data.fuel:0}%", 14, UIFactory.TextDim, TextAlignmentOptions.Left);
            }
            if (!selected)
            {
                UIFactory.LE(UIFactory.Text(right, "Select a car on the left.", 18, UIFactory.TextDim, TextAlignmentOptions.Center), h: 60);
                return;
            }
            UIFactory.LE(UIFactory.Text(right, $"Tow the {selected.Definition.displayName} to:", 19, UIFactory.TextMain, TextAlignmentOptions.Left, true), h: 30);
            foreach (var dest in CarSalesServices.Tow.Destinations())
            {
                var dst = dest;
                int cost = CarSalesServices.Tow.TowCost(selected.transform.position, dst.Position);
                var b = UIFactory.Btn(right, $"{dst.name}   -   {UIFactory.Money(cost)}", UIFactory.CardLight, () => Tow(dst), 52f, 17f);
                b.interactable = CarSalesServices.Economy.CanAfford(cost);
            }
        }

        void Tow(TowService.Destination dest)
        {
            if (CarSalesServices.Tow.Tow(selected, dest, out string msg)) CarSalesServices.UI.Success("Tow truck", msg);
            else CarSalesServices.UI.Error(msg);
            Refresh();
        }

        void BuildTaxi()
        {
            hint.text = "A taxi drives you to any place in town. It costs money and some time.";
            var player = FindFirstObjectByType<PlayerInteractor>();
            Vector3 me = player ? player.transform.position : Vector3.zero;
            foreach (var place in MapPlace.All.Where(p => p && p.IsVisible).OrderBy(p => Vector3.Distance(me, p.transform.position)))
            {
                var p = place;
                int fare = CarSalesServices.Tow.TaxiCost(me, p.ArrivalPosition);
                var target = Vector3.Distance(me, p.transform.position) < 40f ? left : (left.childCount <= right.childCount ? left : right);
                var b = UIFactory.Btn(target, $"{p.placeName}   -   {UIFactory.Money(fare)}", UIFactory.CardLight, () =>
                {
                    if (CarSalesServices.Tow.Taxi(p, out string msg)) { CloseAll(); CarSalesServices.UI.Success("Taxi", msg); }
                    else CarSalesServices.UI.Error(msg);
                }, 52f, 17f);
            }
        }
    }
}
