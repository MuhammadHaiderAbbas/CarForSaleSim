using System;
using System.Linq;
using System.Text.RegularExpressions;
using CarSales.Studio;
using NUnit.Framework;

namespace CarSales.Tests
{
    /// <summary>Phase 14a: the default content pack and the 48-car catalogue.</summary>
    public class DefaultContentTests
    {
        [Test]
        public void Catalogue_HasEightCarsInEveryClass()
        {
            foreach (var cat in CarCatalogue.Categories)
                Assert.AreEqual(8, CarCatalogue.InCategory(cat).Count(), cat);
            Assert.AreEqual(48, CarCatalogue.All.Count);
            Assert.AreEqual(CarCatalogue.All.Count, CarCatalogue.All.Select(c => c.Id).Distinct().Count(), "unique ids");
        }

        [Test]
        public void Catalogue_KeepsTheOriginalCarIds_SoSavesStillLoad()
        {
            foreach (var id in new[] { "hatch_velo", "hatch_metro", "sedan_avenue", "sedan_avenuegt", "suv_ridge", "sports_stallion", "pickup_hauler", "lux_regent" })
                Assert.IsNotNull(CarCatalogue.Find(id), id);
            var velo = CarCatalogue.Find("hatch_velo");
            Assert.AreEqual("Velo", velo.Name);
            Assert.AreEqual(12000, velo.Price);
        }

        [Test]
        public void Catalogue_HasProperDescriptionsAndSaneNumbers()
        {
            foreach (var c in CarCatalogue.All)
            {
                Assert.GreaterOrEqual(c.Description.Length, 30, c.Id + " description");
                Assert.IsTrue(c.Description.EndsWith("."), c.Id + " description is a sentence");
                Assert.That(c.Year, Is.InRange(1960, 2026), c.Id);
                Assert.That(c.Price, Is.InRange(1000, 250000), c.Id);
                Assert.That(c.Popularity, Is.InRange(0.5f, 1.6f), c.Id);
                CollectionAssert.Contains(new[] { "Petrol", "Diesel", "Hybrid", "Electric" }, c.Fuel, c.Id);
                CollectionAssert.Contains(new[] { "Manual", "Automatic" }, c.Gearbox, c.Id);
            }
        }

        [Test]
        public void Catalogue_NamesAreFictional()
        {
            // Real makes are only in the credits; nothing shown in the game may name one.
            foreach (var c in CarCatalogue.All)
                Assert.IsEmpty(TrademarkCheck.Brands(c.Brand + " " + c.Name), $"{c.Id}: {c.Brand} {c.Name}");
        }

        [Test]
        public void EveryCatalogueCar_HasExactlyOneModel()
        {
            foreach (var c in CarCatalogue.All)
            {
                var models = DefaultContent.OfKind(ContentKind.Car).Where(i => i.CarId == c.Id).ToList();
                Assert.AreEqual(1, models.Count, c.Id);
                Assert.AreEqual(c.Category.ToLowerInvariant(), models[0].Style, c.Id + " class");
            }
            foreach (var i in DefaultContent.OfKind(ContentKind.Car))
                Assert.IsNotNull(CarCatalogue.Find(i.CarId), i.Title);
        }

        [Test]
        public void YourSixCars_AreInThePack()
        {
            foreach (var uid in new[] { "f42d9d13b0404b5b9a2302a4c590a94d", "1b79ffa597de49d188537b3f40aa8b50", "4952cf97bbc94b07be8aa09f277c0bcb",
                                        "138a753d04dd4d8f8982d8f5583d212d", "c35a14d811b042d792a6da69381f7f80", "8e87379f40fd40dcac0a751e22c1a188" })
                Assert.IsTrue(DefaultContent.All.Any(i => i.Uid == uid && i.Kind == ContentKind.Car), uid);
            Assert.IsTrue(DefaultContent.All.Any(i => i.Uid == "4958b4ac23eb445aaa6115278676d5df" && i.Style == "villa"), "your villa link");
        }

        [Test]
        public void EveryItem_IsAllowedInAPaidGame_AndUnique()
        {
            var uid = new Regex("^[0-9a-f]{32}$");
            foreach (var i in DefaultContent.All)
            {
                Assert.IsTrue(DefaultContent.LicenceAllowed(i.Licence), $"{i.Title}: {i.Licence}");
                Assert.IsTrue(uid.IsMatch(i.Uid), i.Uid);
                Assert.IsNotEmpty(i.Author);
                Assert.IsNotEmpty(i.Title);
                StringAssert.Contains(i.Uid, i.Credit);
                StringAssert.Contains(i.AuthorName, i.Credit);
            }
            Assert.AreEqual(DefaultContent.All.Count, DefaultContent.All.Select(i => i.Uid).Distinct().Count(), "a model is listed twice");
            Assert.AreEqual(DefaultContent.All.Count, DefaultContent.All.Select(i => i.AssetId).Distinct().Count(), "asset ids collide");
        }

        [Test]
        public void AssetIds_SurviveTheAssetStudiosNaming()
        {
            foreach (var i in DefaultContent.All)
            {
                Assert.AreEqual(i.AssetId, StudioNames.SafeName(i.AssetId), i.AssetId);
                StringAssert.StartsWith("Dc", i.AssetId);
            }
            Assert.AreEqual("DcCarHatchVelo", DefaultContent.ForCar("hatch_velo").AssetId);
        }

        [Test]
        public void Buildings_CoverEveryPartOfTown()
        {
            foreach (var (style, min) in new[] { ("villa", 5), ("house", 6), ("apartment", 6), ("office", 6), ("shop", 1), ("industrial", 2) })
                Assert.GreaterOrEqual(DefaultContent.OfStyle(ContentKind.Building, style).Count(), min, style);
            foreach (var style in new[] { "crane", "excavator", "mixer", "scaffold", "cabin", "bricks", "fence" })
                Assert.IsTrue(DefaultContent.OfStyle(ContentKind.Construction, style).Any(), style);
            foreach (var style in new[] { "tree", "streetlamp", "bench", "fuelpump" })
                Assert.IsTrue(DefaultContent.OfStyle(ContentKind.Prop, style).Any(), style);
        }

        [Test]
        public void Categories_FitTheImportRules()
        {
            Assert.AreEqual(AssetCategory.Vehicle, DefaultContent.ForCar("lux_regent").Category);
            Assert.IsTrue(DefaultContent.OfStyle(ContentKind.Prop, "tree").All(i => i.Category == AssetCategory.Environment), "trees are taller than a prop");
            Assert.IsTrue(DefaultContent.OfKind(ContentKind.Construction).All(i => i.Category == AssetCategory.Environment), "cranes are taller than a prop");
            Assert.IsTrue(DefaultContent.OfKind(ContentKind.Building).All(i => i.Category == AssetCategory.Building));
            Assert.AreEqual(LicenceType.CCBY, DefaultContent.All.First(i => i.Licence == "CC Attribution").LicenceType);
        }
    }
}

namespace CarSales.Tests
{
    using CarSales.Sim;

    /// <summary>Phase 14c: construction runs in visible stages over several days.</summary>
    public class ConstructionTests
    {
        [Test]
        public void Level2_GoesThroughEveryStageInThreeDays()
        {
            Assert.AreEqual(3, ConstructionSim.TotalNights(2));
            Assert.AreEqual(ConstructionStage.Clearing, ConstructionSim.StageOn(10, 10, 2), "the day you book: the old building comes down");
            Assert.AreEqual(ConstructionStage.Site, ConstructionSim.StageOn(10, 11, 2));
            Assert.AreEqual(ConstructionStage.Frame, ConstructionSim.StageOn(10, 12, 2));
            Assert.AreEqual(ConstructionStage.Finished, ConstructionSim.StageOn(10, 13, 2));
            Assert.AreEqual(ConstructionStage.None, ConstructionSim.StageOn(10, 9, 2));
        }

        [Test]
        public void Level3_SpendsLongerOnTheFrame()
        {
            Assert.AreEqual(4, ConstructionSim.TotalNights(3));
            Assert.AreEqual(ConstructionStage.Frame, ConstructionSim.StageOn(0, 2, 3));
            Assert.AreEqual(ConstructionStage.Frame, ConstructionSim.StageOn(0, 3, 3));
            Assert.AreEqual(ConstructionStage.Finished, ConstructionSim.StageOn(0, 4, 3));
        }

        [Test]
        public void Progress_AndDaysLeft()
        {
            Assert.AreEqual(0f, ConstructionSim.Progress(5, 5, 2), 1e-5);
            Assert.AreEqual(1f / 3f, ConstructionSim.Progress(5, 6, 2), 1e-5);
            Assert.AreEqual(1f, ConstructionSim.Progress(5, 20, 2), 1e-5);
            Assert.AreEqual(3, ConstructionSim.DaysLeft(5, 5, 2));
            Assert.AreEqual(0, ConstructionSim.DaysLeft(5, 9, 2));
        }

        [Test]
        public void OldSaves_WorkOutTheStartFromTheReadyDay()
        {
            Assert.AreEqual(7, ConstructionSim.StartDayFor(0, 10, 2));
            Assert.AreEqual(4, ConstructionSim.StartDayFor(4, 10, 2), "a saved start day wins");
        }

        [Test]
        public void EveryStage_HasANameAndDescription()
        {
            foreach (var s in new[] { ConstructionStage.Clearing, ConstructionStage.Site, ConstructionStage.Frame, ConstructionStage.Finished })
            {
                Assert.IsNotEmpty(ConstructionSim.Name(s));
                Assert.IsNotEmpty(ConstructionSim.Describe(s));
            }
        }
    }
}

namespace CarSales.Tests
{
    using CarSales.Sim;

    /// <summary>Phase 14e: rival dealers you can buy from, sell to and hire from.</summary>
    public class RivalTests
    {
        static RivalProfile Downtown => RivalSim.Find("downtown");
        static RivalProfile Luxury => RivalSim.Find("luxury");
        static RivalProfile Cheap => RivalSim.Find("cheapwheels");

        [Test]
        public void Rivals_AreTheMarketsCompetitors_AndHaveAPlaceInTown()
        {
            CollectionAssert.AreEquivalent(MarketModel.Competitors.Select(c => c.Name), RivalSim.Rivals.Select(r => r.Name), "same names: their lots are the competitor prices");
            Assert.AreEqual(RivalSim.Rivals.Length, RivalSim.Rivals.Select(r => r.Id).Distinct().Count());
            foreach (var r in RivalSim.Rivals)
            {
                Assert.IsNotEmpty(r.Tagline, r.Id);
                Assert.Greater(r.Fleet.Length, 0, r.Id + " hires cars out");
                if (!string.IsNullOrEmpty(r.PremisesId)) Assert.IsNotNull(TownPlan.Site(r.PremisesId), r.PremisesId);
            }
            Assert.IsNotNull(TownPlan.Site("CheapWheels"), "Cheap Wheels has its own yard");
            Assert.AreSame(Downtown, RivalSim.AtPremises("dealer_downtown"));
            Assert.AreSame(Downtown, RivalSim.Find("Downtown Motors"), "found by market name too");
            Assert.IsNull(RivalSim.AtPremises("autoworld"));
        }

        [Test]
        public void BuyingTheirPremises_ClosesThem_ButCheapWheelsStays()
        {
            Func<string, bool> ownsDowntown = id => id == "dealer_downtown";
            Assert.IsFalse(RivalSim.IsOpen(Downtown, ownsDowntown));
            Assert.IsTrue(RivalSim.IsOpen(Luxury, ownsDowntown));
            Assert.IsTrue(RivalSim.IsOpen(Cheap, id => true), "no premises to buy");
            Assert.IsTrue(RivalSim.IsOpen(Downtown, null));
        }

        [Test]
        public void Asking_AndBestPrice()
        {
            Assert.AreEqual(11000, RivalSim.AskingPrice(Downtown, 10000, 0), "10% over market");
            Assert.AreEqual(11400, RivalSim.AskingPrice(Downtown, 10000, 0.5), "+4% noise at most");
            Assert.AreEqual(10200, RivalSim.AskingPrice(Cheap, 10000, 0));
            int low = RivalSim.BestPrice(Downtown, 11000, 0f, 0), high = RivalSim.BestPrice(Downtown, 11000, 100f, 0);
            Assert.Less(high, low, "a good reputation gets more off");
            Assert.AreEqual(10450, high, "the full 5% off");
            Assert.Less(RivalSim.BestPrice(Downtown, 11000, 100f, 10), high, "and a car that's been on the lot a while");
            Assert.GreaterOrEqual(RivalSim.BestPrice(Downtown, 11000, 100f, 999), 11000 * (1 - 0.05f - 0.03f) - 50, "but never more than the cap");
            Assert.AreEqual(0, RivalSim.BestPrice(Downtown, 11000, 100f, 0) % 50);
        }

        [Test]
        public void TheLotTurnsOver_OlderCarsSellFirst()
        {
            Assert.IsTrue(RivalSim.SellsToday(0, 0.1));
            Assert.IsFalse(RivalSim.SellsToday(0, 0.2));
            Assert.IsTrue(RivalSim.SellsToday(5, 0.35));
            Assert.IsFalse(RivalSim.SellsToday(100, 0.61), "60% a day at most");
        }

        [Test]
        public void TradeOffers()
        {
            int sedan = (int)MarketCategory.Sedan, pickup = (int)MarketCategory.Pickup;
            Assert.AreEqual(7800, RivalSim.TradeOffer(Downtown, 10000, sedan, 70f, 8));
            Assert.AreEqual(7000, RivalSim.TradeOffer(Downtown, 10000, pickup, 70f, 8), "-10% for a type they don't specialise in");
            Assert.AreEqual(6650, RivalSim.TradeOffer(Downtown, 10000, sedan, 30f, 8), "-15% for a car that needs work");
            Assert.Less(RivalSim.TradeOffer(Downtown, 10000, sedan, 70f, 11), 7800, "less when the lot is full");
            Assert.AreEqual(Math.Round(7800 * 0.85 / 50) * 50, RivalSim.TradeOffer(Downtown, 10000, sedan, 70f, 40), 1, "at most -15% for a full lot");
            Assert.IsTrue(Cheap.Wants(pickup) && Cheap.Wants(sedan), "Cheap Wheels takes anything");
            Assert.AreEqual(100, RivalSim.TradeOffer(Cheap, 50, sedan, 10f, 0), "never less than £100");
            Assert.Less(RivalSim.TradeOffer(Cheap, 10000, sedan, 70f, 0), RivalSim.TradeOffer(Downtown, 10000, sedan, 70f, 0), "cash-for-cars pays least");
        }

        [Test]
        public void Refusals()
        {
            Assert.IsNull(RivalSim.Refusal(Downtown, 3000, false, false));
            StringAssert.Contains("over £8,000", RivalSim.Refusal(Luxury, 6000, false, false));
            Assert.IsNotNull(RivalSim.Refusal(Cheap, 9000, true, false), "your own car");
            StringAssert.Contains("hire car", RivalSim.Refusal(Cheap, 9000, true, true));
        }

        [Test]
        public void HirePrices()
        {
            Assert.AreEqual(80, RivalSim.DailyRate(Downtown, 10000), "0.8% of value a day");
            Assert.AreEqual(100, RivalSim.DailyRate(Luxury, 10000));
            Assert.AreEqual(25, RivalSim.DailyRate(Cheap, 1000), "at least £25");
            Assert.AreEqual(240, RivalSim.HireCost(80, 3));
            Assert.AreEqual(475, RivalSim.HireCost(80, 7), "a week is 15% cheaper (rounded to £5)");
            Assert.AreEqual(0, RivalSim.HireCost(80, 0));
            Assert.AreEqual(1000, RivalSim.Deposit(10000));
            Assert.AreEqual(200, RivalSim.Deposit(500));
            Assert.AreEqual(3000, RivalSim.Deposit(90000));
        }

        [Test]
        public void ReturningAHireCar()
        {
            var clean = RivalSim.Return(80, 1000, 3, 0, 100f, 100f, 0.9f, 90f, 89.8f, 10000, 450);
            Assert.AreEqual(0, clean.Total, "full, normal wear, inside 3 x 200 km");
            Assert.AreEqual(1000, clean.Refund);
            var bad = RivalSim.Return(80, 1000, 3, 2, 100f, 40f, 0.9f, 90f, 80f, 10000, 1400);
            Assert.AreEqual((int)Math.Ceiling(60 * 0.9 * 1.2), bad.Fuel);
            Assert.AreEqual((int)Math.Round(9.5 * 10000 * 0.006), bad.Damage);
            Assert.AreEqual((int)Math.Round((1400 - 1000) * 0.25), bad.Excess, "5 days' allowance (3 + 2 late)");
            Assert.AreEqual(240, bad.Late, "2 late days at 1.5x");
            Assert.AreEqual(bad.Fuel + bad.Damage + bad.Excess + bad.Late, bad.Total);
            Assert.AreEqual(Math.Max(0, 1000 - bad.Total), bad.Refund);
            Assert.AreEqual(Math.Max(0, bad.Total - 1000), bad.Extra);
            Assert.AreEqual(0, RivalSim.LateDays(10, 9));
            Assert.AreEqual(2, RivalSim.LateDays(10, 12));
            Assert.IsFalse(RivalSim.Recovered(10, 13));
            Assert.IsTrue(RivalSim.Recovered(10, 14), "after 3 late days they collect it");
        }

        [Test]
        public void LeasingYourCarsToTheirFleet()
        {
            int lux = (int)MarketCategory.Luxury, hatch = (int)MarketCategory.Hatchback;
            Assert.IsNull(RivalSim.LeaseRefusal(Luxury, lux, 30000, 80f, false, false));
            Assert.IsNotNull(RivalSim.LeaseRefusal(Luxury, hatch, 30000, 80f, false, false), "they don't hire out hatchbacks");
            Assert.IsNotNull(RivalSim.LeaseRefusal(Luxury, lux, 5000, 80f, false, false), "too cheap for them");
            Assert.IsNotNull(RivalSim.LeaseRefusal(Downtown, hatch, 9000, 50f, false, false), "needs 60% condition");
            Assert.IsNotNull(RivalSim.LeaseRefusal(Downtown, hatch, 9000, 80f, true, false), "not your own car");
            Assert.AreEqual(40, RivalSim.LeaseFee(Downtown, 10000), "half their daily rate");
            Assert.AreEqual(20, RivalSim.LeaseFee(Cheap, 1000), "at least £20");
            Assert.Less(RivalSim.LeaseFee(Downtown, 10000) * 7, RivalSim.HireCost(RivalSim.DailyRate(Downtown, 10000), 7), "they make money on it");
            Assert.AreEqual(80, RivalSim.LeaseKm(0));
            Assert.AreEqual(200, RivalSim.LeaseKm(1));
        }

        [Test]
        public void CarDescriptions_NameNoRealMakes()
        {
            // Descriptions are prose: "seven-seat" or "dodge the traffic" are ordinary words, only the brand spelling counts.
            var ordinary = new System.Collections.Generic.Dictionary<string, string> { { "SEAT", "SEAT" }, { "Rolls-Royce", "Rolls" }, { "Dodge", "Dodge" }, { "Lotus", "Lotus" } };
            foreach (var c in CarCatalogue.All)
            {
                var hits = TrademarkCheck.Brands(c.Description)
                    .Where(b => !ordinary.TryGetValue(b, out var w) || Regex.IsMatch(c.Description, "\\b" + w + "\\b")).ToList();
                Assert.IsEmpty(hits, $"{c.Id}: {c.Description}");
            }
        }
    }
}
