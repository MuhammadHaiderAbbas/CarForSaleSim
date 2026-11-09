using System;
using System.Linq;
using CarSales.Sim;
using NUnit.Framework;

namespace CarSales.Tests
{
    public class WorldTests
    {
        [Test]
        public void WeatherRows_SumToOne_AndRainHappensMoreInWinter()
        {
            foreach (Weather from in Enum.GetValues(typeof(Weather)))
                Assert.AreEqual(1f, Enum.GetValues(typeof(Weather)).Cast<Weather>().Sum(to => WorldSim.TransitionChance(from, to)), 1e-4, from.ToString());
            int winter = 0, summer = 0;
            var w1 = Weather.Clear; var w2 = Weather.Clear;
            var r1 = new Random(1); var r2 = new Random(1);
            for (int i = 0; i < 5000; i++)
            {
                w1 = WorldSim.Next(w1, 1, i % 24, r1); if (WorldSim.IsWet(w1)) winter++;
                w2 = WorldSim.Next(w2, 3, i % 24, r2); if (WorldSim.IsWet(w2)) summer++;
            }
            Assert.Greater(winter, summer);
            Assert.Greater(summer, 0);
        }

        [Test]
        public void Rain_CutsGripAndFootfall()
        {
            Assert.AreEqual(1f, WorldSim.Grip(Weather.Clear));
            Assert.Less(WorldSim.Grip(Weather.HeavyRain), WorldSim.Grip(Weather.Rain));
            Assert.AreEqual(0.7f, WorldSim.Footfall(Weather.Rain), 1e-5);
            Assert.Greater(WorldSim.FogDensity(Weather.Fog), WorldSim.FogDensity(Weather.Rain));
        }

        [Test]
        public void Calendar_BankHolidaysAndSundays()
        {
            Assert.IsTrue(WorldSim.IsBankHoliday(1));
            Assert.IsTrue(WorldSim.IsBankHoliday(365));   // day 1 of year 2
            Assert.IsFalse(WorldSim.IsBankHoliday(2));
            Assert.AreEqual(1.4f, WorldSim.DayFootfall(1), 1e-5);
            Assert.AreEqual(0.8f, WorldSim.DayFootfall(7), 1e-5);   // day 7 is a Sunday
            var (open, close) = WorldSim.Hours(7, 9f, 19f);
            Assert.AreEqual((10f, 17f), (open, close));
            Assert.AreEqual((9f, 19f), WorldSim.Hours(6, 9f, 19f));
        }

        [Test]
        public void Fuel_StaysInBounds_AndShiftsDemand()
        {
            float f = 1f;
            var r = new Random(3);
            for (int i = 0; i < 2000; i++) { f = WorldSim.StepFuel(f, r); Assert.That(f, Is.InRange(0.75f, 1.4f)); }
            Assert.Greater(WorldSim.EconomyDemand(1.3f, true), 1f);
            Assert.Less(WorldSim.EconomyDemand(1.3f, false), 1f);
            Assert.AreEqual(100, WorldSim.PoliceFineFor(false));
            Assert.AreEqual(0, WorldSim.PoliceFineFor(true));
        }

        [Test]
        public void Traffic_KeepsGaps_AndStopsAtRed()
        {
            Assert.AreEqual(13f, TrafficRules.TargetSpeed(13f, -1f, 0f, -1f, false));
            Assert.AreEqual(0f, TrafficRules.TargetSpeed(13f, 3f, 10f, -1f, false), "too close: stop");
            Assert.Less(TrafficRules.TargetSpeed(13f, 12f, 5f, -1f, false), 13f);
            Assert.AreEqual(0f, TrafficRules.TargetSpeed(13f, -1f, 0f, 1.5f, true));
            Assert.Less(TrafficRules.TargetSpeed(13f, -1f, 0f, 10f, true), 13f);
            Assert.AreEqual(5f, TrafficRules.Approach(4f, 13f, 0.4f), 1e-5);
        }

        [Test]
        public void Lights_AlternatePhases()
        {
            Assert.AreEqual(LightState.Green, TrafficRules.Light(1f, true));
            Assert.AreEqual(LightState.Red, TrafficRules.Light(1f, false));
            Assert.AreEqual(LightState.Amber, TrafficRules.Light(13f, true));
            Assert.AreEqual(LightState.Red, TrafficRules.Light(16f, true));
            Assert.AreEqual(LightState.Green, TrafficRules.Light(16f, false));
            Assert.Greater(TrafficRules.Density(8f), TrafficRules.Density(3f));
        }
    }
}

namespace CarSales.Tests
{
    using System.Linq;
    using CarSales.Sim;
    using NUnit.Framework;

    public class RoadGraphTests
    {
        [Test]
        public void TwoLanesPerSegment_DriveOnTheLeft()
        {
            var lanes = RoadGraph.Build(new[] { new RoadSegment(new V2(0f, 0f), new V2(100f, 0f)) });
            Assert.AreEqual(2, lanes.Count);
            var east = lanes.First(l => l.Dir.x > 0f);
            Assert.Greater(east.start.z, 0f, "heading +x, the left is +z");
            var west = lanes.First(l => l.Dir.x < 0f);
            Assert.Less(west.start.z, 0f);
            Assert.IsEmpty(east.next, "dead end, no U-turn");
        }

        [Test]
        public void DefaultTown_IsConnected_WithJunctions()
        {
            var lanes = RoadGraph.Build(RoadGraph.DefaultTown());
            Assert.AreEqual(16, lanes.Count);
            Assert.IsTrue(lanes.Any(l => l.endsAtJunction));
            foreach (var l in lanes.Where(x => x.endsAtJunction)) Assert.IsNotEmpty(l.next, $"lane {l.id} at a junction leads somewhere");
            // From the middle eastbound lane on the south road you can go on or turn north.
            var mid = lanes.First(l => l.Dir.x > 0f && System.Math.Abs(l.start.x + 70f) < 5f && l.start.z < 0f);
            Assert.GreaterOrEqual(mid.next.Count, 2);
            Assert.AreEqual(120f, (mid.PointAt(120f) - mid.start).x, 1e-3);
            Assert.AreEqual(140f, (mid.PointAt(500f) - mid.start).x, 1e-3, "clamped to the lane end");
        }
    }
}
