using UnityEngine;

namespace CarSales
{
    /// <summary>A full-screen panel created and owned by UIManager. Build() creates its UI from code.</summary>
    public abstract class UIPanel : MonoBehaviour
    {
        protected RectTransform Root => (RectTransform)transform;
        public abstract void Build();
        protected void OpenSelf() => CarSalesServices.UI.Open(gameObject);
        protected static void CloseAll() => CarSalesServices.UI.Close();
    }

    /// <summary>A page inside the dealer computer window.</summary>
    public abstract class ComputerPage : MonoBehaviour
    {
        public string pageTitle;
        protected RectTransform Area => (RectTransform)transform;
        public abstract void Build();
        public abstract void Refresh();
        public void Show() => CarSalesServices.UI.computerHub.ShowPage(this);
    }
}
