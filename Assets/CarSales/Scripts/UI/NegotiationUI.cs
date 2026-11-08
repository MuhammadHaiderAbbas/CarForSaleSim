using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>Haggling with a customer, shown as a chat with a patience meter.</summary>
    public class NegotiationUI : UIPanel
    {
        NegotiationSession session;
        bool finished;
        RawImage photo;
        TextMeshProUGUI headerText, carText, offerText, moodText;
        UIFactory.Bar patienceBar;
        RectTransform chat;
        ScrollRect chatScroll;
        TMP_InputField input;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "Negotiation", new Vector2(1100f, 800f), () => { if (session != null) Finish(false); });
            UIFactory.Vertical(body, 0, 12f);

            var top = UIFactory.HRow(body, 140, 20, "Top", false);
            photo = UIFactory.CarPicture(top, 140f, 220f);
            var info = UIFactory.VCol(top, 4f);
            headerText = UIFactory.Text(info, "", 28, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            carText = UIFactory.Text(info, "", 17, UIFactory.TextDim, TextAlignmentOptions.Left);
            moodText = UIFactory.Text(info, "", 16, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            patienceBar = UIFactory.ProgressBar(info, UIFactory.Good, 12f);

            var chatBg = UIFactory.Panel(body, "ChatBg", UIFactory.Overlay, 1.4f);
            UIFactory.LE(chatBg, flexH: 1);
            UIFactory.Vertical(chatBg, 10, 0f);
            chat = UIFactory.ScrollList(chatBg.transform, 10f, "Chat");
            chatScroll = chat.GetComponentInParent<ScrollRect>();

            offerText = UIFactory.Text(body, "", 28, UIFactory.Gold, TextAlignmentOptions.Center, true);
            UIFactory.LE(offerText, h: 40);

            var inputRow = UIFactory.HRow(body, 50, 10, "Input", false);
            var lbl = UIFactory.Text(inputRow, "Your price:", 20, UIFactory.TextMain, TextAlignmentOptions.Right, true);
            UIFactory.LE(lbl, w: 130, flexW: 0);
            Nudge(inputRow, "-1000", -1000);
            input = UIFactory.MakeInput(inputRow, "Enter price...", 48f);
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 9;
            Nudge(inputRow, "+1000", 1000);

            var row = UIFactory.HRow(body, 58, 12);
            UIFactory.Btn(row, "Accept Their Offer", UIFactory.Good, () => { if (session == null) return; session.AcceptCustomerOffer(); Finish(true); }, 56f);
            UIFactory.Btn(row, "Counter-Offer", UIFactory.Accent, Counter, 56f);
            UIFactory.Btn(row, "Walk Away", UIFactory.Bad, () => { if (session != null) Finish(false); }, 56f);
        }

        void Nudge(Transform parent, string label, int delta)
        {
            var b = UIFactory.Btn(parent, label, UIFactory.CardLight, () =>
            {
                int.TryParse(input.text, out int v);
                input.text = Mathf.Max(0, v + delta).ToString();
            }, 48f, 16f);
            UIFactory.LE(b, h: 48, w: 90, flexW: 0);
        }

        public void Show(CustomerAI customer)
        {
            OpenSelf();
            session = new NegotiationSession(customer, customer.TargetCar);
            finished = false;
            UIFactory.ClearChildren(chat);
            var def = session.Car.Definition;
            UIFactory.SetCarPicture(photo, session.Car.Data);
            headerText.text = $"{customer.Profile.customerName}  wants your {def.brand} {def.displayName}";
            carText.text = $"Listed at {UIFactory.Money(session.Car.Data.listedPrice)}   |   Market value {UIFactory.Money(session.MarketValue)}   |   " +
                           $"Your cost {UIFactory.Money(session.Car.Data.purchasePrice + session.Car.Data.repairSpent)}";
            Bubble($"Hi! I really like this {def.displayName}. Would you take {UIFactory.Money(session.CustomerOffer)}?", false);
            input.text = session.Car.Data.listedPrice.ToString();
            UpdateState();
        }

        void Counter()
        {
            if (session == null) return;
            if (!int.TryParse(input.text, out int offer) || offer <= 0) { CarSalesServices.UI.Error("Enter a valid price"); return; }
            Bubble($"How about {UIFactory.Money(offer)}?", true);
            var result = session.Propose(offer, out string reply);
            Bubble(reply, false);
            switch (result)
            {
                case NegotiationResult.Accepted: Finish(true); break;
                case NegotiationResult.WalkedAway:
                    CarSalesServices.Economy.ChangeReputation(-2f);
                    CarSalesServices.UI.Error($"{session.Customer.Profile.customerName} walked away. (-2 reputation)");
                    Finish(false);
                    break;
                default: UpdateState(); break;
            }
        }

        void UpdateState()
        {
            offerText.text = $"Their offer: {UIFactory.Money(session.CustomerOffer)}";
            float p = session.PatienceRatio;
            patienceBar.Set(p);
            patienceBar.SetColor(p > 0.6f ? UIFactory.Good : p > 0.3f ? UIFactory.Warn : UIFactory.Bad);
            moodText.text = "Patience: " + (p > 0.6f ? "Relaxed" : p > 0.3f ? "Getting impatient" : "About to leave!");
        }

        void Bubble(string text, bool fromPlayer)
        {
            var row = UIFactory.HRow(chat, -1, 0, "Line", false);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;
            if (fromPlayer) UIFactory.Spacer(row, 0f, 1f);
            var bubble = UIFactory.Panel(row, "Bubble", fromPlayer ? UIFactory.Accent : UIFactory.CardLight, 1.8f);
            var v = UIFactory.Vertical(bubble, 0, 2f);
            v.padding = new RectOffset(16, 16, 10, 10);
            var who = UIFactory.Text(bubble.transform, fromPlayer ? "You" : session.Customer.Profile.customerName, 14,
                                     fromPlayer ? UIFactory.ChatPlayer : UIFactory.Gold, TextAlignmentOptions.Left, true);
            var t = UIFactory.Text(bubble.transform, text, 19, UIFactory.TextMain, TextAlignmentOptions.Left);
            float width = Mathf.Min(560f, t.GetPreferredValues(text, 560f, 0f).x + 40f);
            UIFactory.LE(bubble, w: Mathf.Max(160f, width), flexW: 0);
            if (!fromPlayer) UIFactory.Spacer(row, 0f, 1f);
            Canvas.ForceUpdateCanvases();
            if (chatScroll) chatScroll.verticalNormalizedPosition = 0f;
        }

        void Finish(bool sold)
        {
            finished = true;
            var s = session;
            session = null;
            if (sold) CarSalesServices.Vehicles.CompleteSale(s.Car, s.FinalPrice);
            if (s.Customer) s.Customer.EndNegotiation(sold);
            CarSalesServices.UI.Close();
        }

        void OnDisable()
        {
            if (finished || session == null) return;
            finished = true;
            if (session.Customer) session.Customer.EndNegotiation(false);
            session = null;
        }
    }
}
