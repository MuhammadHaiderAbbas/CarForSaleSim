using System.Linq;
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
        TextMeshProUGUI headerText, carText, offerText, moodText, dealText;
        Button discloseButton, tradeButton, financeButton;
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

            // Phase 7: disclosure, trade-in, finance
            var extras = UIFactory.HRow(body, 44, 10, "Extras");
            discloseButton = UIFactory.Btn(extras, "Disclose faults", UIFactory.Warn, Disclose, 42f, 14f);
            tradeButton = UIFactory.Btn(extras, "Trade-in", UIFactory.CardLight, ToggleTradeIn, 42f, 14f);
            financeButton = UIFactory.Btn(extras, "Finance", UIFactory.CardLight, ToggleFinance, 42f, 14f);
            dealText = UIFactory.Text(body, "", 15, UIFactory.TextDim, TextAlignmentOptions.Center);
            UIFactory.LE(dealText, h: 22);

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
            UIFactory.Btn(row, "Accept Their Offer", UIFactory.Good, () => { if (session == null) return; session.AcceptCustomerOffer(); TryClose(); }, 56f);
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
            headerText.text = $"{customer.Profile.customerName}  wants your {def.brand} {def.displayName}\n<size=60%><color={UIFactory.Hex(UIFactory.TextDim)}>{session.Persona.Name}</color></size>";
            carText.text = $"Listed at {UIFactory.Money(session.Car.Data.listedPrice)}   |   Market value {UIFactory.Money(session.MarketValue)}   |   " +
                           $"Your cost {UIFactory.Money(session.Car.Data.purchasePrice + session.Car.Data.repairSpent)}";
            if (session.Spotted.Count > 0)
                Bubble($"I had a good look. I noticed: {string.Join(", ", session.Spotted).ToLowerInvariant()}. That needs sorting.", false);
            if (session.TradeIn != null)
                Bubble($"I'd like to part-exchange my {session.TradeIn.Name} ({session.TradeIn.mileage:N0} km). I think it's worth {UIFactory.Money(session.TradeInCredit)}.", false);
            if (customer.Profile.wantsFinance) Bubble("I'd need finance on this.", false);
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
                case NegotiationResult.Accepted: TryClose(); break;
                case NegotiationResult.WalkedAway:
                    CarSalesServices.Economy.ChangeReputation(-2f);
                    CarSalesServices.UI.Error($"{session.Customer.Profile.customerName} walked away. (-2 reputation)");
                    Finish(false);
                    break;
                default: UpdateState(); break;
            }
        }

        void Disclose()
        {
            if (session == null || session.Disclosed) return;
            var known = CarRecords.KnownFaults(session.Car.Data).Select(f => f.Def.Name).ToList();
            if (known.Count == 0) { CarSalesServices.UI.Notify("You don't know of any faults on this car."); return; }
            session.Disclose();
            Bubble($"To be straight with you: {string.Join(", ", known).ToLowerInvariant()}.", true);
            Bubble("Thanks for being honest. I'll take that into account.", false);
            UpdateState();
        }

        void ToggleTradeIn()
        {
            if (session?.TradeIn == null) return;
            session.SetTradeIn(!session.IncludeTradeIn);
            Bubble(session.IncludeTradeIn ? $"I'll take your {session.TradeIn.Name} at {UIFactory.Money(session.TradeInCredit)}." : "Let's leave the trade-in out.", true);
            UpdateState();
        }

        void ToggleFinance()
        {
            if (session == null) return;
            session.SetFinance(!session.UseFinance);
            Bubble(session.UseFinance ? "We can arrange finance for you." : "Let's keep this cash.", true);
            UpdateState();
        }

        /// <summary>A price is agreed: check the finance, then finish the sale.</summary>
        void TryClose()
        {
            int price = session.FinalPrice;
            if (!session.CheckFinance(price, out string msg))
            {
                Bubble(msg + " I can't afford it at that price without it.", false);
                UpdateState();
                return;
            }
            if (session.UseFinance) Bubble(msg, false);
            Finish(true);
        }

        void UpdateState()
        {
            offerText.text = $"Their offer: {UIFactory.Money(session.CustomerOffer)}";
            float p = session.PatienceRatio;
            patienceBar.Set(p);
            patienceBar.SetColor(p > 0.6f ? UIFactory.Good : p > 0.3f ? UIFactory.Warn : UIFactory.Bad);
            moodText.text = "Patience: " + (p > 0.6f ? "Relaxed" : p > 0.3f ? "Getting impatient" : "About to leave!");
            int known = CarRecords.KnownFaults(session.Car.Data).Count();
            discloseButton.GetComponentInChildren<TextMeshProUGUI>().text = session.Disclosed ? "Faults disclosed" : $"Disclose {known} known fault{(known == 1 ? "" : "s")}";
            discloseButton.interactable = !session.Disclosed && known > 0;
            tradeButton.gameObject.SetActive(session.TradeIn != null);
            if (session.TradeIn != null)
            {
                int ours = Mathf.RoundToInt(CarValueCalculator.MarketValue(session.TradeIn) * 0.75f);
                tradeButton.GetComponentInChildren<TextMeshProUGUI>().text = (session.IncludeTradeIn ? "Trade-in ON: " : "Trade-in: ") + $"{session.TradeIn.Name} ({UIFactory.Money(session.TradeInCredit)}, trade value ~{UIFactory.Money(ours)})";
                UIFactory.SetButtonColor(tradeButton, session.IncludeTradeIn ? UIFactory.Accent : UIFactory.CardLight);
            }
            financeButton.GetComponentInChildren<TextMeshProUGUI>().text = session.UseFinance ? $"Finance ON (+{Sim.Buyers.FinanceCommissionRate * 100f:0}% commission)" : "Offer finance";
            UIFactory.SetButtonColor(financeButton, session.UseFinance ? UIFactory.Accent : UIFactory.CardLight);
            int credit = session.IncludeTradeIn ? session.TradeInCredit : 0;
            dealText.text = credit > 0 ? $"At their offer you'd receive {UIFactory.Money(session.CustomerOffer - credit)} cash + their car" :
                            session.HiddenKnownFaults > 0 ? $"<color={UIFactory.Hex(UIFactory.Warn)}>You know of {session.HiddenKnownFaults} fault(s) you haven't mentioned - they may complain later.</color>" : "";
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
            if (sold && s.Customer)
                CustomerDeal.CompleteSale(s.Customer, s.FinalPrice, s.MarketValue, s.DisclosedFaults, s.HiddenKnownFaults, s.Rounds, null,
                                          s.IncludeTradeIn ? s.TradeIn : null, s.IncludeTradeIn ? s.TradeInCredit : 0, s.UseFinance && s.FinanceApproved);
            else if (s.Customer) s.Customer.EndNegotiation(false);
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
