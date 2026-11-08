using TMPro;
using UnityEngine;

namespace CarSales
{
    /// <summary>"OPEN / CLOSED" sign the player can interact with (same as pressing O).</summary>
    public class ShopToggleSign : MonoBehaviour, IInteractable
    {
        [SerializeField] TMP_Text label;

        void Start()
        {
            CarSalesServices.DayCycle.OnShopStateChanged += UpdateLabel;
            UpdateLabel(CarSalesServices.DayCycle.IsOpen);
        }
        void OnDestroy() { if (CarSalesServices.DayCycle) CarSalesServices.DayCycle.OnShopStateChanged -= UpdateLabel; }

        void UpdateLabel(bool open)
        {
            if (!label) return;
            label.text = open ? "OPEN" : "CLOSED";
            label.color = open ? Color.green : Color.red;
        }

        public bool CanInteract() => !CarSalesServices.DayCycle.IsDayOver;
        public string GetInteractionText() =>
            CarSalesServices.DayCycle.IsOpen ? "[E] Close dealership (end the day)" : "[E] Open dealership";
        public void Interact(GameObject interactor) => CarSalesServices.DayCycle.ToggleShop();
    }
}
