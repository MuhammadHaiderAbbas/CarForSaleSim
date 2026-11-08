using System;
using System.Collections.Generic;
using CarSales.Foundation;
using TMPro;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Accounts app: profit &amp; loss for a period (today / 7 / 30 days / all kept), daily-profit chart,
    /// totals per category and a sortable transaction table, all read from the ledger.
    /// </summary>
    public class AccountsPageUI : ComputerPage
    {
        static readonly int[] PeriodDays = { 1, 7, 30, 0 };
        UIFactory.TabBar periods, views;
        UIFactory.StatTileRefs income, expenses, net, cash;
        UIFactory.MiniChart chart;
        UIFactory.Table categories, transactions;
        RectTransform categoryView, transactionView;

        public override void Build()
        {
            UIFactory.Vertical(Area, 0, 12f);
            periods = UIFactory.Tabs(Area, new[] { "Today", "7 days", "30 days", "All kept" }, _ => Refresh(), 40f, 130f, 16f);

            var tiles = UIFactory.HRow(Area, 104, 12, "Tiles");
            income = UIFactory.StatTile(tiles, "Income", 104f);
            expenses = UIFactory.StatTile(tiles, "Expenses", 104f);
            net = UIFactory.StatTile(tiles, "Profit", 104f);
            cash = UIFactory.StatTile(tiles, "Cash now", 104f);

            chart = UIFactory.Chart(Area, "Profit per day", 90f);

            views = UIFactory.Tabs(Area, new[] { "By category", "Transactions" }, _ => ShowView(), 40f, 180f, 16f);

            var body = UIFactory.Rect("Views", Area);
            UIFactory.LE(body, flexH: 1, flexW: 1);
            categoryView = UIFactory.Rect("Categories", body);
            UIFactory.Stretch(categoryView);
            UIFactory.Vertical(categoryView, 0, 4f);
            categories = UIFactory.MakeTable(categoryView, new List<UIFactory.TableColumn>
            {
                new UIFactory.TableColumn("Category"),
                new UIFactory.TableColumn("Money in", 200f, TextAlignmentOptions.Right),
                new UIFactory.TableColumn("Money out", 200f, TextAlignmentOptions.Right),
                new UIFactory.TableColumn("Net", 200f, TextAlignmentOptions.Right),
            }, 38f);
            categories.SetSort(3, false);

            transactionView = UIFactory.Rect("Transactions", body);
            UIFactory.Stretch(transactionView);
            UIFactory.Vertical(transactionView, 0, 4f);
            transactions = UIFactory.MakeTable(transactionView, new List<UIFactory.TableColumn>
            {
                new UIFactory.TableColumn("When", 150f),
                new UIFactory.TableColumn("Category", 220f),
                new UIFactory.TableColumn("Details"),
                new UIFactory.TableColumn("Amount", 170f, TextAlignmentOptions.Right),
            }, 36f);
            transactions.SetSort(0, true);
            transactions.SetEmptyText("No transactions in this period.");

            periods.Select(1, false);
            views.Select(0, false);
            ShowView();
        }

        void ShowView()
        {
            categoryView.gameObject.SetActive(views.Index == 0);
            transactionView.gameObject.SetActive(views.Index == 1);
        }

        public override void Refresh()
        {
            var eco = CarSalesServices.Economy;
            var day = CarSalesServices.DayCycle;
            if (!eco || !day) return;
            var ledger = eco.Ledger;
            int today = day.CurrentDay;
            int span = PeriodDays[Mathf.Clamp(periods.Index, 0, PeriodDays.Length - 1)];
            int from = span > 0 ? Mathf.Max(1, today - span + 1) : 1;
            var s = ledger.Summarize(from, today);

            income.value.text = UIFactory.Money(s.income);
            income.value.color = UIFactory.Good;
            expenses.value.text = UIFactory.Money(s.expenses);
            expenses.value.color = UIFactory.Bad;
            net.value.text = UIFactory.Money(s.Net);
            net.value.color = s.Net >= 0 ? UIFactory.Good : UIFactory.Bad;
            net.sub.text = s.Net >= 0 ? "profit" : "loss";
            cash.value.text = UIFactory.Money(eco.Money);
            int loan = CarSalesServices.Bank ? CarSalesServices.Bank.Loan : 0;
            cash.sub.text = loan > 0 ? $"loan {UIFactory.Money(loan)}" : "no loan";
            income.sub.text = span == 1 ? $"day {today}" : $"days {from}-{today}";
            expenses.sub.text = s.financingIn + s.financingOut > 0 ? $"loans: +{UIFactory.Money(s.financingIn)} / -{UIFactory.Money(s.financingOut)}" : "loans excluded";

            int chartFrom = Mathf.Max(1, today - 13);
            var daily = ledger.DailyNet(chartFrom, today);
            var values = new List<float>();
            foreach (var v in daily) values.Add(v);
            chart.Set(values);
            chart.caption.text = $"PROFIT PER DAY  (days {chartFrom}-{today})";

            // Category totals: split in / out for the period.
            var inBy = new int[TxCategories.Count];
            var outBy = new int[TxCategories.Count];
            var rows = new List<UIFactory.Table.Row>();
            foreach (var e in ledger.Entries)
            {
                if (e.day < from || e.day > today) continue;
                int c = (int)e.category;
                if (c < 0 || c >= inBy.Length) continue;
                if (e.amount > 0) inBy[c] += e.amount; else outBy[c] -= e.amount;

                rows.Add(new UIFactory.Table.Row
                {
                    cells = new[]
                    {
                        $"Day {e.day}  {Mathf.FloorToInt(e.hour):00}:{Mathf.FloorToInt((e.hour % 1f) * 60f):00}",
                        TxCategories.DisplayName(e.category),
                        string.IsNullOrEmpty(e.memo) ? "-" : e.memo,
                        Coloured(e.amount),
                    },
                    keys = new IComparable[] { e.id, TxCategories.DisplayName(e.category), e.memo ?? "", e.amount },
                });
            }
            transactions.SetRows(rows);

            var catRows = new List<UIFactory.Table.Row>();
            for (int c = 0; c < inBy.Length; c++)
            {
                if (inBy[c] == 0 && outBy[c] == 0) continue;
                var cat = (TxCategory)c;
                int n = inBy[c] - outBy[c];
                catRows.Add(new UIFactory.Table.Row
                {
                    cells = new[]
                    {
                        TxCategories.DisplayName(cat) + (TxCategories.IsFinancing(cat) ? $"  <color={UIFactory.Hex(UIFactory.TextDim)}>(financing)</color>" : ""),
                        inBy[c] > 0 ? UIFactory.Money(inBy[c]) : "-",
                        outBy[c] > 0 ? UIFactory.Money(outBy[c]) : "-",
                        Coloured(n),
                    },
                    keys = new IComparable[] { TxCategories.DisplayName(cat), inBy[c], outBy[c], n },
                });
            }
            categories.SetEmptyText("No money moved in this period.");
            categories.SetRows(catRows);
        }

        static string Coloured(int amount) =>
            $"<color={UIFactory.Hex(amount >= 0 ? UIFactory.Good : UIFactory.Bad)}>{(amount > 0 ? "+" : "")}{UIFactory.Money(amount)}</color>";
    }
}
