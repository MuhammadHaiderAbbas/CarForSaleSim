using TMPro;
using UnityEngine;

namespace CarSales
{
    /// <summary>A car standing on the lot. Root of CarDisplay.prefab.</summary>
    public class CarDisplay : MonoBehaviour, IInteractable
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] Transform browsePoint;   // where customers stand to look at the car
        [SerializeField] TMP_Text priceTag;       // world-space TextMeshPro above the roof

        public CarInstance Data { get; private set; }
        public CarDefinition Definition { get; private set; }
        public Transform BrowsePoint => browsePoint ? browsePoint : transform;
        CarVisual visual;

        public void Initialize(CarInstance data)
        {
            Data = data;
            Definition = CarSalesServices.Database.Get(data.carId);
            if (visual) Destroy(visual.gameObject);
            if (Definition && Definition.displayPrefab)
            {
                var go = Instantiate(Definition.displayPrefab, visualRoot ? visualRoot : transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                visual = go.GetComponent<CarVisual>();
            }
            name = Definition ? $"Car_{Definition.displayName}_{data.instanceId.Substring(0, 6)}" : "Car_Unknown";
            Refresh();
        }

        public void Refresh()
        {
            var marker = MinimapMarker.Attach(gameObject, MarkerColor(), 3.2f);
            if (visual) visual.Apply(Data.paintColor, 1f - Data.cleanliness / 100f);
            bool listed = Data.state == CarState.Listed;
            if (priceTag)
            {
                priceTag.gameObject.SetActive(listed);
                if (listed) priceTag.text = $"${Data.listedPrice:N0}";
            }
        }

        Color MarkerColor()
        {
            switch (Data.state)
            {
                case CarState.Listed: return UIFactory.Good;
                case CarState.NeedsRepair: return UIFactory.Warn;
                default: return new Color(0.55f, 0.65f, 0.8f);
            }
        }

        public bool CanInteract() => Data != null && Definition != null;

        public string GetInteractionText() =>
            $"[E] {Definition.year} {Definition.brand} {Definition.displayName}  |  Condition {Data.condition:0}%  |  " +
            $"Value ${CarValueCalculator.MarketValue(Data):N0}  |  {Data.state}";

        public void Interact(GameObject interactor) => CarSalesServices.UI.carMenu.Show(this);
    }
}
