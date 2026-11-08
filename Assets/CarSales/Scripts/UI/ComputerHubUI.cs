using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>The dealer computer: window with a sidebar and pages (Home, Marketplace, Inventory, Messages...).</summary>
    public class ComputerHubUI : UIPanel
    {
        public HomePageUI Home { get; private set; }
        public MarketplaceUI Marketplace { get; private set; }
        public InventoryUI Inventory { get; private set; }
        public MessagesUI Messages { get; private set; }
        public UpgradesUI Upgrades { get; private set; }
        public BankUI Bank { get; private set; }
        public StatsUI Stats { get; private set; }
        public RealEstateUI RealEstate { get; private set; }

        TextMeshProUGUI moneyText, titleText, messagesNavLabel;
        RectTransform content;
        ComputerPage current;
        readonly List<KeyValuePair<ComputerPage, Image>> nav = new List<KeyValuePair<ComputerPage, Image>>();

        public override void Build()
        {
            var body = UIFactory.Window(Root, "AUTO WORLD  |  Dealer System", new Vector2(1560f, 900f), CloseAll, out titleText, out var header);
            moneyText = UIFactory.Text(header, "$0", 26, UIFactory.Good, TextAlignmentOptions.Right, true, "Money");
            UIFactory.Place(moneyText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70f, 0f), new Vector2(320f, 40f));

            UIFactory.Horizontal(body, 0, 20f, TextAnchor.UpperLeft).childForceExpandHeight = true;

            // Sidebar
            var side = UIFactory.Panel(body, "Sidebar", UIFactory.CardBg, 1.4f);
            UIFactory.LE(side, w: 250, flexW: 0);
            var sv = UIFactory.Vertical(side, 12, 6f);
            sv.padding = new RectOffset(12, 12, 16, 16);
            var brand = UIFactory.Text(side.transform, "AUTO WORLD", 26, UIFactory.TextMain, TextAlignmentOptions.Center, true);
            UIFactory.LE(brand, h: 34);
            var brandSub = UIFactory.Text(side.transform, "USED CARS", 14, UIFactory.Accent, TextAlignmentOptions.Center, true);
            UIFactory.LE(brandSub, h: 22);
            UIFactory.Divider(side.transform);

            content = UIFactory.Rect("Content", body);
            UIFactory.LE(content, flexW: 1, flexH: 1);

            Home = AddPage<HomePageUI>("Home");
            Marketplace = AddPage<MarketplaceUI>("Marketplace");
            Inventory = AddPage<InventoryUI>("My Inventory");
            Messages = AddPage<MessagesUI>("Messages");
            Upgrades = AddPage<UpgradesUI>("Upgrades");
            Bank = AddPage<BankUI>("Bank & Loans");
            RealEstate = AddPage<RealEstateUI>("Real Estate");
            Stats = AddPage<StatsUI>("Statistics");

            foreach (var page in new ComputerPage[] { Home, Marketplace, Inventory, Messages, Upgrades, Bank, RealEstate, Stats })
            {
                var p = page;
                var btn = UIFactory.Btn(side.transform, "   " + p.pageTitle, UIFactory.Clear, () => ShowPage(p), out var label, 48f, 19f);
                label.alignment = TextAlignmentOptions.Left;
                if (p == Messages) messagesNavLabel = label;
                nav.Add(new KeyValuePair<ComputerPage, Image>(p, (Image)btn.targetGraphic));
            }
            UIFactory.Spacer(side.transform);
            UIFactory.Btn(side.transform, "Settings", UIFactory.Neutral, () => CarSalesServices.UI.settings.Show(), 42f, 17f);
            UIFactory.Btn(side.transform, "Log Off", UIFactory.AccentDark, CloseAll, 42f, 17f);
        }

        T AddPage<T>(string title) where T : ComputerPage
        {
            var rt = UIFactory.Rect(title, content);
            UIFactory.Stretch(rt);
            var page = rt.gameObject.AddComponent<T>();
            page.pageTitle = title;
            page.Build();
            rt.gameObject.SetActive(false);
            return page;
        }

        public void Show() => ShowPage(Home);

        public void ShowPage(ComputerPage page)
        {
            if (!gameObject.activeSelf || CarSalesServices.UI.OpenPanel != gameObject) OpenSelf();
            current = page;
            foreach (var pair in nav)
            {
                bool active = pair.Key == page;
                pair.Key.gameObject.SetActive(active);
                pair.Value.color = active ? UIFactory.Accent : UIFactory.Clear;
            }
            titleText.text = "AUTO WORLD  |  " + page.pageTitle;
            page.Refresh();
        }

        public void RefreshCurrent() { if (current) current.Refresh(); }

        void Update()
        {
            moneyText.text = UIFactory.Money(CarSalesServices.Economy.Money);
            if (messagesNavLabel && CarSalesServices.Messages)
            {
                int unread = CarSalesServices.Messages.UnreadCount;
                messagesNavLabel.text = unread > 0 ? $"   Messages  <color={UIFactory.Hex(UIFactory.Gold)}>({unread})</color>" : "   Messages";
            }
        }
    }
}
