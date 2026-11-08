using TMPro;
using UnityEngine;

namespace CarSales
{
    /// <summary>One car card in the marketplace grid.</summary>
    public class MarketListingItemUI : MonoBehaviour
    {
        public static MarketListingItemUI Create(Transform parent, CarInstance car, MarketplaceUI owner)
        {
            var def = CarSalesServices.Database.Get(car.carId);
            var card = UIFactory.Panel(parent, "Card_" + car.carId, UIFactory.CardBg, 1.3f);
            var v = UIFactory.Vertical(card, 10, 5f);
            v.padding = new RectOffset(10, 10, 10, 12);

            var pic = UIFactory.CarPicture(card.transform, 150f);
            UIFactory.SetCarPicture(pic, car);

            var name = UIFactory.Text(card.transform, $"{def.brand} {def.displayName}", 20, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(name, h: 26);
            var price = UIFactory.Text(card.transform, UIFactory.Money(car.purchasePrice), 24, UIFactory.Gold, TextAlignmentOptions.Left, true);
            UIFactory.LE(price, h: 30);
            bool known = CarValueCalculator.IsConditionKnown(car);
            float cond = known ? car.condition : car.reportedCondition;
            var details = UIFactory.Text(card.transform,
                $"{def.year}  |  {car.mileage:N0} km\n<color={UIFactory.Hex(CarParts.LabelColor(cond))}>{(known ? "" : "~")}{CarParts.Label(cond)} {cond:0}%</color>" +
                (known ? "  <color=#6f6>(verified)</color>" : "  <color=#aab>(seller's claim)</color>"),
                15, UIFactory.TextDim, TextAlignmentOptions.Left);
            UIFactory.LE(details, h: 40);
            UIFactory.Btn(card.transform, "View Details", UIFactory.Accent, () => owner.OpenDetails(car), 38f, 17f);
            return card.gameObject.AddComponent<MarketListingItemUI>();
        }
    }
}
