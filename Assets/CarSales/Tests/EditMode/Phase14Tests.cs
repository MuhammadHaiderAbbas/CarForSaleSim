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
