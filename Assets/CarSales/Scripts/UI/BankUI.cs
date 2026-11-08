using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    public class BankUI : ComputerPage
    {
        [SerializeField] int smallAmount = 5000, largeAmount = 20000;
        TextMeshProUGUI loanText, limitText, availableText, interestText;
        Button borrowSmall, borrowLarge, repaySmall, repayAll;

        public override void Build()
        {
            UIFactory.Vertical(Area, 0, 16f);
            var cards = UIFactory.HRow(Area, 130, 14);
            loanText = Card(cards, "Current loan");
            availableText = Card(cards, "Available to borrow");
            interestText = Card(cards, "Interest tonight");

            var info = UIFactory.Panel(Area, "Info", UIFactory.CardBg, 1.4f);
            UIFactory.Vertical(info, 20, 6f);
            limitText = UIFactory.Text(info.transform, "", 19, UIFactory.TextMain, TextAlignmentOptions.Left);

            var row1 = UIFactory.HRow(Area, 58, 14);
            borrowSmall = UIFactory.Btn(row1, $"Borrow {UIFactory.Money(smallAmount)}", UIFactory.Good, () => Borrow(smallAmount), 56f);
            borrowLarge = UIFactory.Btn(row1, $"Borrow {UIFactory.Money(largeAmount)}", UIFactory.Good, () => Borrow(largeAmount), 56f);
            var row2 = UIFactory.HRow(Area, 58, 14);
            repaySmall = UIFactory.Btn(row2, $"Repay {UIFactory.Money(smallAmount)}", UIFactory.Accent, () => Repay(smallAmount), 56f);
            repayAll = UIFactory.Btn(row2, "Repay All", UIFactory.Accent, () => Repay(int.MaxValue), 56f);
            UIFactory.Spacer(Area);
        }

        static TextMeshProUGUI Card(Transform parent, string label)
        {
            var card = UIFactory.Panel(parent, label, UIFactory.CardBg, 1.4f);
            var v = UIFactory.Vertical(card, 18, 6f);
            v.padding = new RectOffset(20, 18, 18, 16);
            UIFactory.Text(card.transform, label.ToUpper(), 14, UIFactory.TextDim, TextAlignmentOptions.Left, true);
            return UIFactory.Text(card.transform, "-", 34, UIFactory.TextMain, TextAlignmentOptions.Left, true);
        }

        void Borrow(int amount)
        {
            if (CarSalesServices.Bank.Borrow(amount, out string msg)) CarSalesServices.UI.Success("Loan approved", msg);
            else CarSalesServices.UI.Error(msg);
            Refresh();
        }

        void Repay(int amount)
        {
            if (CarSalesServices.Bank.Repay(amount, out string msg)) CarSalesServices.UI.Success("Payment received", msg);
            else CarSalesServices.UI.Error(msg);
            Refresh();
        }

        public override void Refresh()
        {
            var bank = CarSalesServices.Bank;
            loanText.text = UIFactory.Money(bank.Loan);
            loanText.color = bank.Loan > 0 ? UIFactory.Warn : UIFactory.Good;
            availableText.text = UIFactory.Money(bank.Available);
            interestText.text = UIFactory.Money(bank.TomorrowInterest);
            interestText.color = bank.TomorrowInterest > 0 ? UIFactory.Bad : UIFactory.TextMain;
            limitText.text =
                $"Loan limit: <b>{UIFactory.Money(bank.LoanLimit)}</b>  (grows with your reputation)\n" +
                $"Interest: <b>{bank.DailyInterestRate * 100f:0.#}% per day</b>, charged at closing time.\n" +
                $"<color={UIFactory.Hex(UIFactory.Warn)}>Ending 3 days in a row with negative cash means bankruptcy.</color>";
            borrowSmall.interactable = borrowLarge.interactable = bank.Available > 0;
            repaySmall.interactable = repayAll.interactable = bank.Loan > 0 && CarSalesServices.Economy.Money > 0;
        }
    }
}
