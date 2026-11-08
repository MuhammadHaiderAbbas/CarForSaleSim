using CarSales.Foundation;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>
    /// Live car auction against AI bidders. Condition is only the seller's claim - auctions are cheap but risky.
    /// The bid timer resets whenever someone bids; when it runs out the highest bidder wins.
    /// </summary>
    public class AuctionUI : UIPanel
    {
        class Bidder { public string name; public int max; }
        static readonly string[] Names = { "Dealer Dan", "Mrs. Patel", "Big Mike", "Collector Joe", "Kenji", "Rosa", "TradeCars Ltd", "Vlad" };

        RawImage photo;
        TextMeshProUGUI lotText, carText, specText, bidText, leaderText, logText, timerText;
        UIFactory.Bar timerBar;
        Button bidSmall, bidMed, bidLarge, passButton;
        readonly List<Bidder> bidders = new List<Bidder>();

        CarInstance lot;
        int lotNumber, totalLots, currentBid;
        string leader = "";
        float timer, aiTimer;
        bool running;
        const float LotTime = 12f, ResetTime = 6f;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "Car Auction", new Vector2(1280f, 780f), CloseAll);
            UIFactory.Horizontal(body, 0, 26f, TextAnchor.UpperLeft).childForceExpandHeight = true;

            var left = UIFactory.VCol(body, 8f, "Left", 580f);
            lotText = UIFactory.Text(left, "", 18, UIFactory.Accent, TextAlignmentOptions.Left, true);
            photo = UIFactory.CarPicture(left, 330f);
            carText = UIFactory.Text(left, "", 26, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            specText = UIFactory.Text(left, "", 17, UIFactory.TextDim, TextAlignmentOptions.Left);
            UIFactory.Spacer(left);

            var right = UIFactory.VCol(body, 8f, "Right");
            UIFactory.Text(right, "CURRENT BID", 15, UIFactory.TextDim, TextAlignmentOptions.Left, true);
            bidText = UIFactory.Text(right, "$0", 56, UIFactory.Gold, TextAlignmentOptions.Left, true);
            UIFactory.LE(bidText, h: 70);
            leaderText = UIFactory.Text(right, "", 20, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            timerText = UIFactory.Text(right, "", 16, UIFactory.TextDim, TextAlignmentOptions.Left);
            timerBar = UIFactory.ProgressBar(right, UIFactory.Good, 14f);
            var logBg = UIFactory.Panel(right, "Log", UIFactory.Overlay, 1.6f);
            UIFactory.LE(logBg, flexH: 1);
            UIFactory.Vertical(logBg, 10, 0f);
            var logContent = UIFactory.ScrollList(logBg.transform, 2f, "LogScroll");
            logText = UIFactory.Text(logContent, "", 16, UIFactory.TextMain, TextAlignmentOptions.TopLeft);

            var row = UIFactory.HRow(right, 58, 10);
            bidSmall = UIFactory.Btn(row, "Bid +$250", UIFactory.Good, () => PlayerBid(250), 56f, 18f);
            bidMed = UIFactory.Btn(row, "Bid +$1,000", UIFactory.Good, () => PlayerBid(1000), 56f, 18f);
            bidLarge = UIFactory.Btn(row, "Bid +$5,000", UIFactory.Good, () => PlayerBid(5000), 56f, 18f);
            var row2 = UIFactory.HRow(right, 48, 10);
            passButton = UIFactory.Btn(row2, "Skip this lot", UIFactory.Neutral, Pass, 46f, 17f);
            UIFactory.Btn(row2, "Leave auction", UIFactory.Bad, CloseAll, 46f, 17f);
        }

        public void Show()
        {
            var a = CarSalesServices.Auction;
            if (!a) { CarSalesServices.UI.Error("There's no auction house here."); return; }
            if (!a.IsOpen) { CarSalesServices.UI.Error($"Auctions run {a.OpenHour:00}:00 - {a.CloseHour:00}:00. Come back later!"); return; }
            if (a.Lots.Count == 0) { CarSalesServices.UI.Notify("No more lots today. New cars arrive tomorrow."); return; }
            OpenSelf();
            totalLots = a.Lots.Count;
            lotNumber = 0;
            StartLot();
        }

        void StartLot()
        {
            var a = CarSalesServices.Auction;
            if (a.Lots.Count == 0 || !a.IsOpen) { Finish(); return; }
            lot = a.Lots[0];
            lotNumber++;
            var def = CarSalesServices.Database.Get(lot.carId);
            currentBid = a.StartingBid(lot);
            leader = "";
            timer = LotTime;
            aiTimer = Random.Range(1f, 2.5f);
            running = true;
            logText.text = "";

            bidders.Clear();
            int trueValue = CarValueCalculator.MarketValue(lot);
            int count = Random.Range(2, 5);
            for (int i = 0; i < count; i++)
                bidders.Add(new Bidder { name = Names[Random.Range(0, Names.Length)], max = Mathf.RoundToInt(trueValue * Random.Range(0.5f, 1.02f)) });

            UIFactory.SetCarPicture(photo, lot);
            lotText.text = $"LOT {lotNumber} OF {totalLots}";
            carText.text = $"{def.year} {def.brand} {def.displayName}";
            int est = CarValueCalculator.MarketValue(lot, useReported: true);
            specText.text = $"{lot.mileage:N0} km   |   {def.fuelType}   |   {def.transmission}\n" +
                            $"Condition: <color={UIFactory.Hex(UIFactory.Warn)}>~{lot.reportedCondition:0}% (seller's claim, no inspection!)</color>\n" +
                            $"Cleanliness {lot.cleanliness:0}%   |   Fuel {lot.fuel:0}%\n" +
                            $"Estimated value if the claim is true: <b>{UIFactory.Money(est)}</b>\n" +
                            $"Starting bid: {UIFactory.Money(currentBid)}";
            Log($"<color={UIFactory.Hex(UIFactory.Accent)}>Lot {lotNumber} opens at {UIFactory.Money(currentBid)}.</color>");
            UpdateLabels();
        }

        int Increment => currentBid < 10000 ? 250 : currentBid < 30000 ? 500 : 1000;

        void PlayerBid(int raise)
        {
            if (!running) return;
            int bid = leader == "" ? currentBid : currentBid + raise;
            if (leader == "You") { CarSalesServices.UI.Notify("You're already the highest bidder."); return; }
            if (!CarSalesServices.Economy.CanAfford(bid)) { CarSalesServices.UI.Error("You can't afford that bid."); return; }
            currentBid = bid;
            leader = "You";
            timer = Mathf.Max(timer, ResetTime);
            Log($"<b>You</b> bid {UIFactory.Money(bid)}");
            CarSalesServices.PlaySfx(Sfx.Click);
            UpdateLabels();
        }

        void Update()
        {
            if (!running) return;
            timer -= Time.unscaledDeltaTime;
            aiTimer -= Time.unscaledDeltaTime;
            if (aiTimer <= 0f)
            {
                aiTimer = Random.Range(0.8f, 2f);
                int next = leader == "" ? currentBid : currentBid + Increment;
                var interested = bidders.FindAll(b => b.name != leader && b.max >= next);
                if (interested.Count > 0 && Random.value < 0.75f)
                {
                    var b = interested[Random.Range(0, interested.Count)];
                    currentBid = next;
                    leader = b.name;
                    timer = Mathf.Max(timer, ResetTime);
                    Log($"{b.name} bids {UIFactory.Money(next)}");
                }
            }
            UpdateLabels();
            if (timer <= 0f) EndLot();
        }

        void UpdateLabels()
        {
            bidText.text = UIFactory.Money(currentBid);
            leaderText.text = leader == "" ? "No bids yet" : leader == "You" ? $"<color={UIFactory.Hex(UIFactory.Good)}>You are the highest bidder!</color>" : $"Highest bidder: {leader}";
            timerText.text = timer > 3f ? $"{timer:0}s left" : timer > 0f ? $"<color={UIFactory.Hex(UIFactory.Bad)}>Going... going... {timer:0.0}s</color>" : "";
            timerBar.Set(Mathf.Clamp01(timer / LotTime));
            timerBar.SetColor(timer > 4f ? UIFactory.Good : UIFactory.Bad);
            bool canBid = running && leader != "You";
            bidSmall.interactable = bidMed.interactable = bidLarge.interactable = canBid;
            passButton.interactable = running;
        }

        void Pass()
        {
            if (!running) return;
            if (leader == "You") { CarSalesServices.UI.Notify("You're leading - wait for the hammer or keep bidding."); return; }
            timer = 0f;
            EndLot();
        }

        void EndLot()
        {
            if (!running) return;
            running = false;
            var a = CarSalesServices.Auction;
            a.Lots.Remove(lot);
            if (leader == "You")
            {
                if (CarSalesServices.Economy.TrySpend(currentBid, TxCategory.Auction, $"Auction win - {lot.Name}", lot.instanceId))
                {
                    lot.purchasePrice = currentBid;
                    lot.state = CarValueCalculator.DefaultState(lot);
                    var bay = a.FreeParkingBay();
                    CarSalesServices.Vehicles.AddOwnedCar(lot, bay.position, bay.eulerAngles.y);
                    CarSalesServices.DayCycle.RegisterPurchase(currentBid);
                    if (CarSalesServices.Progress) CarSalesServices.Progress.RegisterPurchase();
                    float lie = lot.reportedCondition - lot.condition;
                    lot.inspected = true;
                    CarSalesServices.PlaySfx(Sfx.Success);
                    string reveal = lie >= 5f ? $" Bad news: real condition is {lot.condition:0}% (claimed {lot.reportedCondition:0}%)." : " The condition was as described!";
                    CarSalesServices.UI.Success("SOLD to you!", $"{UIFactory.Money(currentBid)}. The car is in the auction car park - drive it (F) or tow it (P).{reveal}");
                    Log($"<color={UIFactory.Hex(UIFactory.Good)}><b>SOLD to you for {UIFactory.Money(currentBid)}!</b></color>");
                }
                else
                {
                    CarSalesServices.Economy.ChangeReputation(-2f);
                    CarSalesServices.UI.Error("You couldn't pay for the car! The lot is cancelled (-2 reputation).");
                }
            }
            else if (leader != "") Log($"Sold to {leader} for {UIFactory.Money(currentBid)}.");
            else Log("No bids - lot withdrawn.");
            UpdateLabels();
            if (gameObject.activeInHierarchy) StartCoroutine(NextLotSoon());
        }

        IEnumerator NextLotSoon()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            if (gameObject.activeInHierarchy) StartLot();
        }

        void Finish()
        {
            running = false;
            Log("<b>That's all for today.</b> New lots tomorrow.");
            UpdateLabels();
        }

        void Log(string line) => logText.text = line + "\n" + logText.text;

        void OnDisable()
        {
            // Leaving mid-lot: if you were winning, the car is yours.
            if (running && leader == "You") EndLot();
            running = false;
        }
    }
}
