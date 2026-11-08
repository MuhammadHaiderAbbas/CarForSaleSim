using TMPro;
using UnityEngine;

namespace CarSales
{
    public class DailyReportUI : UIPanel
    {
        TextMeshProUGUI title, sold, bought, revenue, expenses, rent, interest, profit, balance, rep, worth, warning;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "End of Day", new Vector2(720f, 760f), null);
            UIFactory.Vertical(body, 0, 4f);
            title = UIFactory.Text(body, "", 38, UIFactory.TextMain, TextAlignmentOptions.Center, true);
            UIFactory.LE(title, h: 56);
            sold = UIFactory.SpecRow(body, "Cars sold");
            bought = UIFactory.SpecRow(body, "Cars bought");
            revenue = UIFactory.SpecRow(body, "Revenue");
            expenses = UIFactory.SpecRow(body, "Expenses");
            rent = UIFactory.SpecRow(body, "  incl. rent");
            interest = UIFactory.SpecRow(body, "  incl. loan interest");
            profit = UIFactory.SpecRow(body, "Profit on sold cars");
            UIFactory.Divider(body);
            balance = UIFactory.SpecRow(body, "Cash balance", 40);
            rep = UIFactory.SpecRow(body, "Reputation");
            worth = UIFactory.SpecRow(body, "Net worth");
            warning = UIFactory.Text(body, "", 18, UIFactory.Bad, TextAlignmentOptions.Center, true);
            UIFactory.Spacer(body);
            UIFactory.Btn(body, "Start Next Day", UIFactory.Accent, () => CarSalesServices.DayCycle.StartNextDay(), 60f, 22f);
        }

        public void Show(DailyReport r, int rentPaid, int interestPaid)
        {
            OpenSelf();
            var eco = CarSalesServices.Economy;
            var p = CarSalesServices.Progress;
            title.text = $"DAY {r.day} COMPLETE";
            sold.text = r.carsSold.ToString();
            bought.text = r.carsBought.ToString();
            revenue.text = $"<color={UIFactory.Hex(UIFactory.Good)}>{UIFactory.Money(r.revenue)}</color>";
            expenses.text = $"<color={UIFactory.Hex(UIFactory.Bad)}>{UIFactory.Money(r.expenses)}</color>";
            rent.text = UIFactory.Money(rentPaid);
            interest.text = UIFactory.Money(interestPaid);
            profit.text = UIFactory.Money(r.profit);
            balance.text = $"<size=130%><color={UIFactory.Hex(eco.Money < 0 ? UIFactory.Bad : UIFactory.Gold)}>{UIFactory.Money(eco.Money)}</color></size>";
            rep.text = $"{eco.Reputation:0}/100";
            worth.text = p ? UIFactory.Money(p.NetWorth) : "-";
            warning.text = p && p.DaysInDebt > 0
                ? $"IN DEBT: day {p.DaysInDebt}/{p.BankruptcyDays}. Get above $0 or you go bankrupt!\n(Sell cars, Quick Sell, or take a loan.)"
                : "";
        }
    }
}
