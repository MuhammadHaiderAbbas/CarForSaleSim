using System;
using System.Collections.Generic;
using System.Linq;
using CarSales.Sim;
using TMPro;
using UnityEngine;

namespace CarSales
{
    /// <summary>Market app: price trend and body-type factors, competitor research vs. your listings, weekly trade batches.</summary>
    public class MarketPageUI : ComputerPage
    {
        UIFactory.TabBar views;
        RectTransform trendView, rivalsView, tradeView, factors, tradeList;
        UIFactory.MiniChart chart;
        UIFactory.Table rivals, mine;

        public override void Build()
        {
            UIFactory.Vertical(Area, 0, 10f);
            views = UIFactory.Tabs(Area, new[] { "Market prices", "Competitors", "Trade offers" }, _ => Refresh(), 42f, 220f, 16f);
            var host = UIFactory.Rect("Views", Area);
            UIFactory.LE(host, flexH: 1, flexW: 1);

            trendView = View(host);
            UIFactory.Vertical(trendView, 0, 10f);
            chart = UIFactory.Chart(trendView, "PRICE INDEX (last 60 days, 0 = base)", 140f);
            UIFactory.SectionTitle(trendView, "Body types today (season, demand, supply)");
            factors = UIFactory.ScrollList(trendView, 6f);

            rivalsView = View(host);
            UIFactory.Vertical(rivalsView, 0, 6f);
            UIFactory.SectionTitle(rivalsView, "Your cars for sale vs. the cheapest rival");
            mine = UIFactory.MakeTable(rivalsView, new List<UIFactory.TableColumn>
            {
                new UIFactory.TableColumn("Your car"), new UIFactory.TableColumn("Your price", 140f, TextAlignmentOptions.Right),
                new UIFactory.TableColumn("Market value", 140f, TextAlignmentOptions.Right), new UIFactory.TableColumn("Cheapest rival", 280f),
                new UIFactory.TableColumn("Difference", 140f, TextAlignmentOptions.Right),
            }, 36f, false);
            mine.SetEmptyText("You have no cars listed for sale.");
            UIFactory.SectionTitle(rivalsView, "Competitor stock");
            rivals = UIFactory.MakeTable(rivalsView, new List<UIFactory.TableColumn>
            {
                new UIFactory.TableColumn("Dealer", 200f), new UIFactory.TableColumn("Car"), new UIFactory.TableColumn("Condition", 120f),
                new UIFactory.TableColumn("Mileage", 130f, TextAlignmentOptions.Right), new UIFactory.TableColumn("Price", 140f, TextAlignmentOptions.Right),
            }, 34f);

            tradeView = View(host);
            UIFactory.Vertical(tradeView, 0, 8f);
            tradeList = UIFactory.ScrollList(tradeView, 8f);
            views.Select(0, false);
        }

        static RectTransform View(Transform host) { var v = UIFactory.Rect("View", host); UIFactory.Stretch(v); return v; }

        public override void Refresh()
        {
            var m = CarSalesServices.Market;
            trendView.gameObject.SetActive(views.Index == 0);
            rivalsView.gameObject.SetActive(views.Index == 1);
            tradeView.gameObject.SetActive(views.Index == 2);
            if (!m) return;
            var db = CarSalesServices.Database;
            if (views.Index == 0)
            {
                chart.Set(m.State.history.Select(v => (v - 1f) * 100f).ToList());
                UIFactory.ClearChildren(factors);
                foreach (MarketCategory c in Enum.GetValues(typeof(MarketCategory)))
                {
                    float f = m.CategoryMultiplier(c);
                    var v = UIFactory.SpecRow(factors, c.ToString(), 32f);
                    v.text = $"<color={UIFactory.Hex(f >= 1f ? UIFactory.Good : UIFactory.Bad)}>{(f - 1f) * 100f:+0.0;-0.0}%</color>";
                }
                var tip = UIFactory.Text(factors, "Sports cars sell for more in summer, SUVs and pickups in winter. Every sale lowers demand for a few days; " +
                                                  "few cars of a type on your lots push its price up.", 14, UIFactory.TextDim, TextAlignmentOptions.TopLeft);
                tip.textWrappingMode = TextWrappingModes.Normal;
                UIFactory.LE(tip, h: 44);
            }
            else if (views.Index == 1)
            {
                var rows = new List<UIFactory.Table.Row>();
                foreach (var car in CarSalesServices.Inventory.OwnedCars.Where(c => c.state == CarState.Listed))
                {
                    var def = db.Get(car.carId);
                    var r = m.CheapestRival(def);
                    int diff = r != null ? car.listedPrice - r.price : 0;
                    rows.Add(new UIFactory.Table.Row
                    {
                        cells = new[] { car.Name, UIFactory.Money(car.listedPrice), UIFactory.Money(CarValueCalculator.MarketValue(car)),
                                        r != null ? $"{r.dealer}: {db.Get(r.carId)?.displayName} {UIFactory.Money(r.price)}" : "-",
                                        r != null ? $"<color={UIFactory.Hex(diff <= 0 ? UIFactory.Good : UIFactory.Warn)}>{(diff > 0 ? "+" : "")}{UIFactory.Money(diff)}</color>" : "-" },
                        keys = new IComparable[] { car.Name, car.listedPrice, CarValueCalculator.MarketValue(car), r?.price ?? 0, diff },
                    });
                }
                mine.SetRows(rows);
                rivals.SetRows(m.State.competitorListings.Select(l => new UIFactory.Table.Row
                {
                    cells = new[] { l.dealer, db.Get(l.carId)?.displayName ?? l.carId, $"{l.condition:0}%", $"{l.mileage:N0} km", UIFactory.Money(l.price) },
                    keys = new IComparable[] { l.dealer, l.carId, l.condition, l.mileage, l.price },
                }).ToList());
            }
            else
            {
                UIFactory.ClearChildren(tradeList);
                foreach (var b in m.State.tradeBatches.Where(x => !x.taken))
                {
                    var row = UIFactory.Panel(tradeList, b.id, UIFactory.CardBg, 1.8f);
                    UIFactory.LE(row, h: 110);
                    var h = UIFactory.Horizontal(row, 10, 12f);
                    h.padding = new RectOffset(16, 12, 10, 10);
                    var cars = string.Join(", ", b.carIds.Select((id, i) => $"{db.Get(id)?.displayName} ({b.conditions[i]:0}%, {b.mileages[i] / 1000}k km)"));
                    var t = UIFactory.Text(row.transform, $"<b>{b.dealer}: {b.carIds.Count} cars for {UIFactory.Money(b.price)}</b>  -  until day {b.expiresDay}\n<size=85%>{cars}</size>\n" +
                                                          $"<size=80%><color={UIFactory.Hex(UIFactory.TextDim)}>Take the whole batch or leave it. Condition is as described (trade terms).</color></size>",
                                           16, UIFactory.TextMain, TextAlignmentOptions.Left);
                    t.textWrappingMode = TextWrappingModes.Normal;
                    UIFactory.LE(t, flexW: 1);
                    var bb = b;
                    var buy = UIFactory.Btn(row.transform, $"Buy batch\n{UIFactory.Money(b.price)}", UIFactory.Good, () => { m.BuyBatch(bb); Refresh(); }, 50f, 15f);
                    UIFactory.LE(buy, h: 54, w: 160, flexW: 0);
                }
                if (tradeList.childCount == 0) UIFactory.EmptyState(tradeList, "No trade offers right now", "Dealers send a new batch every Monday.");
            }
        }
    }
}
