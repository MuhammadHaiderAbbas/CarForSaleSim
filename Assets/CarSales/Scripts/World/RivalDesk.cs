using UnityEngine;

namespace CarSales
{
    /// <summary>Phase 14e: a rival's sales or hire desk. E opens its screen on the matching tab.</summary>
    public class RivalDesk : MonoBehaviour, IInteractable
    {
        public RivalDealership site;
        [Tooltip("0 buy, 1 sell to them, 2 hire a car, 3 lease them your car")]
        public int tab;

        public bool CanInteract() => site && site.IsOpen;

        public string GetInteractionText()
        {
            var p = site ? site.Profile : null;
            if (p == null) return "";
            return tab >= 2 ? $"[E] {p.Name} car hire: hire a car, return a hire car, lease them your cars"
                            : $"[E] {p.Name} sales desk: buy a car, sell yours for cash";
        }

        public void Interact(GameObject interactor)
        {
            if (site) RivalDealerUI.Show(site.rivalId, tab);
        }
    }
}
