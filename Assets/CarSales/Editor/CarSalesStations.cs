using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Phase 14d: the town's service points fitted out down to the small things - fuel pumps with hoses and price
    /// screens, a walk-in forecourt shop (shelves, fridges, counter, coffee, ATM), the car wash (vacuums, jet-wash bay,
    /// programme board, entry lights), the auction hall (lighting truss, clerks' desks, side screens, auction office,
    /// refreshments, queue barriers) and the tyre shop (tyre wall, balancer, waiting area). Pumps and lifts are
    /// Equipment slots the default content pack fills with real models.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        // ------------------------------------------------------------------ FUEL STATION
        static void FuelPump(Transform g, Vector3 pos, float yaw, int variant, Mats m)
        {
            var t = Slot(g, "FuelPump", pos, PlaceholderKind.Equipment, new Vector3(0.9f, 2.1f, 0.7f), 0f, "fuelpump", variant);
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Part(t, "Plinth", new Vector3(0f, 0.08f, 0f), new Vector3(0.9f, 0.16f, 0.7f), m.concrete);
            Part(t, "Body", new Vector3(0f, 0.95f, 0f), new Vector3(0.72f, 1.6f, 0.5f), m.flagWhite, solid: true);
            Part(t, "Head", new Vector3(0f, 1.9f, 0f), new Vector3(0.8f, 0.3f, 0.6f), m.flagRed);
            Part(t, "LedStrip", new Vector3(0f, 1.9f, 0.302f), new Vector3(0.74f, 0.05f, 0.005f), m.ledStrip, shadows: false);
            foreach (var side in new[] { -1f, 1f })
            {
                float z = 0.252f * side;
                var rot = new Vector3(0f, side > 0 ? 0f : 180f, 0f);
                Part(t, "Screen", new Vector3(0f, 1.45f, z), new Vector3(0.42f, 0.28f, 0.005f), m.screen, shadows: false, euler: rot);
                Part(t, "PriceLcd", new Vector3(0f, 1.15f, z), new Vector3(0.5f, 0.14f, 0.005f), m.signBoard, shadows: false, euler: rot);
                Part(t, "Keypad", new Vector3(0.22f, 1.0f, z), new Vector3(0.14f, 0.16f, 0.01f), m.blackGlass, shadows: false, euler: rot);
                for (int i = 0; i < 3; i++)
                {
                    float x = -0.24f + i * 0.17f;
                    Part(t, "Holster", new Vector3(x, 0.75f, z * 1.05f), new Vector3(0.11f, 0.2f, 0.06f), i == 0 ? m.flagBlue : i == 1 ? m.leaves : m.signBoard, euler: rot);
                    Part(t, "Hose", new Vector3(x, 0.48f, z * 1.3f), new Vector3(0.025f, 0.32f, 0.025f), m.blackGlass, type: PrimitiveType.Cylinder, shadows: false);
                }
            }
        }

        static void FurnishFuelStation(Transform g, Mats m)
        {
            // Canopy underside: a grid of LED panels, fascia lights, protective bollards round the pillars.
            for (int i = -2; i <= 2; i++)
                for (int j = 0; j < 3; j++)
                    Part(g, "CanopyPanel", new Vector3(i * 3.6f, 4.74f, 2.5f + j * 3.5f), new Vector3(1.4f, 0.03f, 0.6f), m.ceilingLight, shadows: false);
            foreach (var p in new[] { new Vector3(-8.5f, 0f, 2f), new Vector3(8.5f, 0f, 2f), new Vector3(-8.5f, 0f, 10f), new Vector3(8.5f, 0f, 10f) })
                foreach (var off in new[] { new Vector3(-0.6f, 0f, -0.6f), new Vector3(0.6f, 0f, -0.6f) })
                {
                    Part(g, "Bollard", p + off + new Vector3(0f, 0.5f, 0f), new Vector3(0.18f, 0.5f, 0.18f), m.liftYellow, solid: true, type: PrimitiveType.Cylinder);
                    Part(g, "BollardBand", p + off + new Vector3(0f, 0.75f, 0f), new Vector3(0.19f, 0.06f, 0.19f), m.blackGlass, type: PrimitiveType.Cylinder, shadows: false);
                }
            // Between the pumps: bins, squeegee buckets, paper towels.
            foreach (float x in new[] { -3.3f, 3.3f })
            {
                Part(g, "Bin", new Vector3(x, 0.75f, 6f), new Vector3(0.45f, 0.45f, 0.45f), m.bin, solid: true, type: PrimitiveType.Cylinder);
                Part(g, "Bucket", new Vector3(x + 0.35f, 0.5f, 6f), new Vector3(0.3f, 0.2f, 0.3f), m.flagBlue, type: PrimitiveType.Cylinder);
                Part(g, "TowelDispenser", new Vector3(x - 0.35f, 1.1f, 6f), new Vector3(0.28f, 0.36f, 0.2f), m.flagWhite);
            }
            // Forecourt extras: air & water, vacuum, gas-bottle cage, ice freezer, two EV chargers, bay lines.
            Part(g, "AirWater", new Vector3(-10.2f, 0.75f, 12.5f), new Vector3(0.6f, 1.5f, 0.45f), m.flagBlue, solid: true);
            Part(g, "AirWaterScreen", new Vector3(-10.2f, 1.2f, 12.27f), new Vector3(0.3f, 0.2f, 0.005f), m.screen, shadows: false);
            Part(g, "AirHose", new Vector3(-9.85f, 0.6f, 12.3f), new Vector3(0.03f, 0.5f, 0.03f), m.blackGlass, type: PrimitiveType.Cylinder);
            Part(g, "Vacuum", new Vector3(10.2f, 0.85f, 12.5f), new Vector3(0.7f, 1.7f, 0.7f), m.liftYellow, solid: true);
            Part(g, "VacuumHose", new Vector3(10.2f, 1.9f, 12.5f), new Vector3(0.6f, 0.08f, 0.6f), m.blackGlass, type: PrimitiveType.Cylinder);
            Part(g, "GasCage", new Vector3(-10.4f, 0.9f, 15.2f), new Vector3(1.4f, 1.8f, 0.8f), m.metal, solid: true);
            for (int i = 0; i < 4; i++) Part(g, "GasBottle", new Vector3(-10.85f + i * 0.3f, 0.45f, 15.2f), new Vector3(0.24f, 0.4f, 0.24f), m.flagRed, type: PrimitiveType.Cylinder);
            Part(g, "IceFreezer", new Vector3(-7.3f, 0.55f, 13.6f), new Vector3(1.2f, 1.1f, 0.7f), m.flagWhite, solid: true);
            Part(g, "IceSign", new Vector3(-7.3f, 0.75f, 13.24f), new Vector3(0.8f, 0.3f, 0.005f), m.flagBlue, shadows: false);
            foreach (float z in new[] { 1f, 3.5f })
            {
                Part(g, "EvCharger", new Vector3(10.6f, 0.8f, z), new Vector3(0.45f, 1.6f, 0.35f), m.flagWhite, solid: true);
                Part(g, "EvScreen", new Vector3(10.37f, 1.2f, z), new Vector3(0.005f, 0.25f, 0.2f), m.screen, shadows: false);
                Part(g, "EvLed", new Vector3(10.6f, 1.62f, z), new Vector3(0.45f, 0.04f, 0.35f), m.ledStrip, shadows: false);
                Part(g, "EvBay", new Vector3(8.9f, 0.026f, z), new Vector3(3f, 0.005f, 2.2f), m.leaves, shadows: false);
            }
            for (int i = 0; i < 3; i++)
                Part(g, "BayLine", new Vector3(-6.2f + i * 6.2f, 0.026f, 1f), new Vector3(0.12f, 0.005f, 2f), m.line, shadows: false);
        }

        /// <summary>The forecourt shop as a room you can walk into (12 x 6 m): shelves, fridges, counter with the till.</summary>
        static void FuelShop(Transform g, Mats m, out Vector3 counterPos)
        {
            const float x0 = -6f, x1 = 6f, z0 = 14f, z1 = 20f, h = 4f;
            Solid(g, "ShopFloor", new Vector3(0f, 0.02f, 17f), new Vector3(12f, 0.04f, 6f), m.floor);
            Solid(g, "ShopBack", new Vector3(0f, h / 2f, z1), new Vector3(12.3f, h, 0.3f), m.wall);
            Solid(g, "ShopWest", new Vector3(x0, h / 2f, 17f), new Vector3(0.3f, h, 6f), m.wall);
            Solid(g, "ShopEast", new Vector3(x1, h / 2f, 17f), new Vector3(0.3f, h, 6f), m.wall);
            Solid(g, "ShopRoof", new Vector3(0f, h + 0.15f, 17f), new Vector3(12.6f, 0.3f, 6.6f), m.roof);
            // Glass front with a door gap in the middle.
            WallWithDoor(g, "ShopFrontFrame", x0, x1, z0, 0.4f, 0.3f, 0f, 1.8f, 0.4f, m.frame);
            Glass(g, "ShopGlassW", new Vector3(-3.45f, 1.9f, z0), new Vector3(5.1f, 3f, 0.06f), m.windowGlass);
            Glass(g, "ShopGlassE", new Vector3(3.45f, 1.9f, z0), new Vector3(5.1f, 3f, 0.06f), m.windowGlass);
            Solid(g, "ShopHeader", new Vector3(0f, 3.7f, z0), new Vector3(12f, 0.6f, 0.3f), m.frame);
            Part(g, "DoorMat", new Vector3(0f, 0.045f, z0 + 0.8f), new Vector3(1.6f, 0.01f, 1f), m.carpet, shadows: false);
            // Two aisles of gondola shelving with stock.
            foreach (float x in new[] { -3.2f, -0.6f })
            {
                var shelf = Slot(g, "Gondola", new Vector3(x, 0f, 16.8f), PlaceholderKind.Storage, new Vector3(0.9f, 1.6f, 3f), 0f, "shelf", Mathf.RoundToInt(x));
                Part(shelf, "Base", new Vector3(0f, 0.08f, 0f), new Vector3(0.9f, 0.16f, 3f), m.metal, solid: true);
                Part(shelf, "Spine", new Vector3(0f, 0.8f, 0f), new Vector3(0.06f, 1.6f, 3f), m.metal, solid: true);
                for (int s = 0; s < 4; s++)
                {
                    float y = 0.3f + s * 0.38f;
                    Part(shelf, "Shelf", new Vector3(0f, y, 0f), new Vector3(0.88f, 0.02f, 2.95f), m.metal);
                    for (int k = 0; k < 6; k++)
                        foreach (var side in new[] { -0.25f, 0.25f })
                            Part(shelf, "Stock", new Vector3(side, y + 0.12f, -1.25f + k * 0.5f), new Vector3(0.3f, 0.22f, 0.4f),
                                 m.art[(k + s + (side > 0 ? 1 : 0)) % 3], shadows: false);
                }
            }
            // Fridges along the back wall: lit glass doors.
            for (int i = 0; i < 4; i++)
            {
                float x = -5.2f + i * 0.95f;
                var fridge = Slot(g, "DrinksFridge", new Vector3(x, 0f, z1 - 0.45f), PlaceholderKind.Appliance, new Vector3(0.9f, 2.1f, 0.7f), 180f, "fridge", i);
                Part(fridge, "Body", new Vector3(0f, 1.05f, 0f), new Vector3(0.9f, 2.1f, 0.7f), m.blackGlass, solid: true);
                Part(fridge, "Glow", new Vector3(0f, 1.05f, -0.34f), new Vector3(0.78f, 1.8f, 0.005f), m.ceilingLight, shadows: false);
                for (int s = 0; s < 4; s++) Part(fridge, "Bottles", new Vector3(0f, 0.45f + s * 0.42f, -0.2f), new Vector3(0.75f, 0.26f, 0.3f), m.art[s % 3], shadows: false);
                Part(fridge, "Door", new Vector3(0f, 1.05f, -0.355f), new Vector3(0.86f, 1.9f, 0.01f), m.windowGlass, shadows: false);
            }
            // Counter (where you pay for fuel), till, cigarette cabinet, coffee machine, ATM, newspapers.
            counterPos = new Vector3(3.2f, 0.53f, 18.2f);   // the counter itself is the fuel kiosk (built by BuildFuelStation)
            Part(g, "CounterTop", new Vector3(3.2f, 1.08f, 18.15f), new Vector3(3.3f, 0.04f, 0.85f), m.worktop);
            Part(g, "CounterSide", new Vector3(4.65f, 0.53f, 17.3f), new Vector3(0.6f, 1.06f, 1.5f), m.desk, solid: true);
            Part(g, "Till", new Vector3(2.6f, 1.25f, 18.25f), new Vector3(0.4f, 0.3f, 0.3f), m.blackGlass);
            Part(g, "TillScreen", new Vector3(2.6f, 1.32f, 18.09f), new Vector3(0.32f, 0.2f, 0.005f), m.screen, shadows: false);
            Part(g, "CardReader", new Vector3(3.2f, 1.14f, 17.85f), new Vector3(0.1f, 0.08f, 0.16f), m.blackGlass);
            Part(g, "Cabinet", new Vector3(3.2f, 1.8f, z1 - 0.25f), new Vector3(3f, 1.8f, 0.4f), m.signBoard);
            for (int r = 0; r < 4; r++) Part(g, "CabinetStock", new Vector3(3.2f, 1.2f + r * 0.4f, z1 - 0.46f), new Vector3(2.8f, 0.25f, 0.02f), m.art[r % 3], shadows: false);
            CoffeeStation(g, new Vector3(5.4f, 0f, 15.6f), -90f, m);
            Part(g, "ATM", new Vector3(-5.5f, 0.85f, 14.6f), new Vector3(0.7f, 1.7f, 0.6f), m.metal, solid: true);
            Part(g, "AtmScreen", new Vector3(-5.5f, 1.25f, 14.92f), new Vector3(0.35f, 0.25f, 0.005f), m.screen, shadows: false, euler: new Vector3(0f, 180f, 0f));
            Part(g, "NewsStand", new Vector3(1.6f, 0.5f, 15f), new Vector3(1f, 1f, 0.5f), m.metal);
            for (int i = 0; i < 3; i++) Part(g, "Papers", new Vector3(1.3f + i * 0.3f, 1.02f, 15f), new Vector3(0.28f, 0.04f, 0.4f), m.flagWhite, shadows: false);
            for (int i = 0; i < 3; i++) Part(g, "ShopLight", new Vector3(-3f + i * 3f, h - 0.03f, 17f), new Vector3(0.6f, 0.03f, 3.6f), m.ceilingLight, shadows: false);
            PointLight(g, new Vector3(0f, h - 0.4f, 17f), 9f, 1.4f, new Color(1f, 0.97f, 0.92f));
            AddReflectionProbe(g, "ShopReflections", new Vector3(0f, h / 2f, 17f), new Vector3(12f, h, 6f), 0.8f);
        }

        // ------------------------------------------------------------------ CAR WASH
        static void FurnishCarWash(Transform g, Mats m)
        {
            // Entry traffic light, programme board, wall hoses, drains, chemical drums.
            Part(g, "EntryLightPost", new Vector3(-4.6f, 1.6f, 0.3f), new Vector3(0.12f, 3.2f, 0.12f), m.metal);
            Part(g, "EntryLightBox", new Vector3(-4.6f, 3.3f, 0.2f), new Vector3(0.35f, 0.8f, 0.25f), m.blackGlass);
            Part(g, "EntryRed", new Vector3(-4.6f, 3.48f, 0.07f), new Vector3(0.18f, 0.18f, 0.02f), m.taillight, type: PrimitiveType.Cylinder, euler: new Vector3(90f, 0f, 0f), shadows: false);
            Part(g, "EntryGreen", new Vector3(-4.6f, 3.12f, 0.07f), new Vector3(0.18f, 0.18f, 0.02f), m.ledStrip, type: PrimitiveType.Cylinder, euler: new Vector3(90f, 0f, 0f), shadows: false);
            Part(g, "ProgrammeBoard", new Vector3(7.5f, 1.6f, -1.2f), new Vector3(1.8f, 1.6f, 0.08f), m.accentPanel, solid: true);
            var board = Text3D("Programmes", g, new Vector3(7.5f, 1.6f, -1.25f), "WASH PROGRAMMES\nBASIC    £6\nPOLISH   £9\nDELUXE  £14", 1.6f, Color.white);
            board.alignment = TextAlignmentOptions.Center;
            board.rectTransform.sizeDelta = new Vector2(1.6f, 1.4f);
            board.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            for (float z = 2f; z <= 12f; z += 2.5f)
                Part(g, "Drain", new Vector3(0f, 0.026f, z), new Vector3(3f, 0.005f, 0.25f), m.metal, shadows: false);
            for (int i = 0; i < 3; i++)
                Part(g, "ChemicalDrum", new Vector3(-4.4f, 0.45f, 12.6f - i * 0.7f), new Vector3(0.55f, 0.45f, 0.55f), i == 1 ? m.flagBlue : m.barrel, solid: true, type: PrimitiveType.Cylinder);
            // Two vacuum stations and a self-service jet-wash bay beside the tunnel.
            foreach (float z in new[] { 12.5f, 15.5f })
            {
                Part(g, "VacPost", new Vector3(8.6f, 1.3f, z), new Vector3(0.5f, 2.6f, 0.5f), m.liftYellow, solid: true);
                Part(g, "VacArm", new Vector3(7.6f, 2.5f, z), new Vector3(2f, 0.12f, 0.12f), m.metal);
                Part(g, "VacHose", new Vector3(6.7f, 1.6f, z), new Vector3(0.06f, 0.9f, 0.06f), m.blackGlass, type: PrimitiveType.Cylinder);
                Part(g, "VacBin", new Vector3(8.6f, 0.45f, z + 0.7f), new Vector3(0.45f, 0.45f, 0.45f), m.bin, solid: true, type: PrimitiveType.Cylinder);
                Part(g, "VacBay", new Vector3(5f, 0.026f, z), new Vector3(4.6f, 0.005f, 0.1f), m.line, shadows: false);
            }
            Solid(g, "JetBayWall", new Vector3(-8.8f, 1.5f, 13.5f), new Vector3(0.2f, 3f, 6f), m.wall);
            Part(g, "JetBayRoof", new Vector3(-7.1f, 3.1f, 13.5f), new Vector3(3.6f, 0.15f, 6.2f), m.roof);
            Part(g, "JetUnit", new Vector3(-8.5f, 1.1f, 13.5f), new Vector3(0.4f, 1.2f, 0.8f), m.metal, solid: true);
            Part(g, "JetCoinBox", new Vector3(-8.27f, 1.4f, 13.5f), new Vector3(0.05f, 0.35f, 0.25f), m.screen, shadows: false);
            Part(g, "JetLance", new Vector3(-8.2f, 1.2f, 14.1f), new Vector3(0.04f, 0.6f, 0.04f), m.blackGlass, type: PrimitiveType.Cylinder, euler: new Vector3(0f, 0f, 15f));
            Part(g, "JetBrush", new Vector3(-8.2f, 1.2f, 12.9f), new Vector3(0.12f, 0.12f, 0.5f), m.brushBlue);
            PointLight(g, new Vector3(-7f, 2.8f, 13.5f), 7f, 1.2f, new Color(0.85f, 0.92f, 1f));
        }

        // ------------------------------------------------------------------ AUCTION HALL
        static void FurnishAuctionHall(Transform g, Mats m)
        {
            // Bench cushions, legs and an aisle runner (the "Bench" objects stay: the crowd sits on them).
            for (int row = 0; row < 4; row++)
                foreach (float x in new[] { -6f, 6f })
                {
                    float z = 3.5f + row * 1.7f;
                    Part(g, "BenchCushion", new Vector3(x, 0.53f, z), new Vector3(7.9f, 0.06f, 0.48f), m.fabricAccent, shadows: false);
                    foreach (float lx in new[] { -3.8f, 0f, 3.8f })
                        Part(g, "BenchLeg", new Vector3(x + lx, 0.2f, z), new Vector3(0.06f, 0.4f, 0.4f), m.metal);
                    Part(g, "SeatNumbers", new Vector3(x, 0.82f, z - 0.27f), new Vector3(7.5f, 0.06f, 0.005f), m.goldTrim, shadows: false);
                }
            Part(g, "AisleRunner", new Vector3(0f, 0.045f, 6f), new Vector3(1.6f, 0.01f, 7.5f), m.flagRed, shadows: false);
            // Lighting truss over the stage with real spotlights on the lot.
            Part(g, "Truss", new Vector3(0f, 6.4f, 12.6f), new Vector3(11f, 0.35f, 0.35f), m.metal);
            foreach (float x in new[] { -5.5f, 5.5f }) Part(g, "TrussPost", new Vector3(x, 3.2f, 15f), new Vector3(0.3f, 6.4f, 0.3f), m.metal);
            for (int i = 0; i < 4; i++)
            {
                float x = -3.6f + i * 2.4f;
                Part(g, "StageLamp", new Vector3(x, 6.1f, 12.6f), new Vector3(0.3f, 0.35f, 0.3f), m.blackGlass, type: PrimitiveType.Cylinder);
                var go = new GameObject("StageSpot");
                go.transform.SetParent(g, false);
                go.transform.localPosition = new Vector3(x, 5.9f, 12.4f);
                go.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
                var l = go.AddComponent<Light>();
                l.type = LightType.Spot; l.spotAngle = 40f; l.range = 9f; l.intensity = 2.2f; l.color = new Color(1f, 0.96f, 0.9f); l.shadows = LightShadows.None;
            }
            // Clerks' desks beside the rostrum, side screens with the lot details, the house logo.
            foreach (float x in new[] { 3.6f, 9.8f })
            {
                Desk(g, new Vector3(x, 0f, 15.8f), 180f, 1.6f, m);
                OfficeChair(g, new Vector3(x, 0f, 16.7f), 180f, m.blackGlass, m);
            }
            foreach (float x in new[] { -12.8f, 12.8f })
            {
                Part(g, "SideScreen", new Vector3(x, 4f, 9f), new Vector3(0.1f, 2f, 3.6f), m.blackGlass);
                Part(g, "SideScreenGlow", new Vector3(x - Mathf.Sign(x) * 0.06f, 4f, 9f), new Vector3(0.01f, 1.8f, 3.4f), m.screen, shadows: false);
            }
            Part(g, "LogoPanel", new Vector3(6.8f, 3f, 17.8f), new Vector3(4f, 1.6f, 0.1f), m.signBoard);
            var logo = Text3D("HouseLogo", g, new Vector3(6.8f, 3f, 17.72f), "LOT  ·  BID  ·  SOLD", 4f, new Color(1f, 0.85f, 0.4f));
            logo.alignment = TextAlignmentOptions.Center;
            logo.rectTransform.sizeDelta = new Vector2(3.8f, 1.4f);
            logo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            // Auction office in the back corner behind glass: desks, filing, a plant.
            Glass(g, "OfficeGlassFront", new Vector3(-9.5f, 1.4f, 13.2f), new Vector3(6.8f, 2.8f, 0.06f), m.windowGlass);
            Glass(g, "OfficeGlassSide", new Vector3(-6.1f, 1.4f, 15.6f), new Vector3(0.06f, 2.8f, 4.8f), m.windowGlass);
            Part(g, "OfficeCarpet", new Vector3(-9.5f, 0.045f, 15.6f), new Vector3(6.6f, 0.01f, 4.6f), m.carpet, shadows: false);
            Desk(g, new Vector3(-10.8f, 0f, 16.9f), 180f, 1.6f, m);
            OfficeChair(g, new Vector3(-10.8f, 0f, 16f), 0f, m.blackGlass, m);
            Desk(g, new Vector3(-8f, 0f, 16.9f), 180f, 1.4f, m);
            OfficeChair(g, new Vector3(-8f, 0f, 16f), 0f, m.blackGlass, m);
            Bookcase(g, new Vector3(-12.6f, 0f, 15f), 90f, 1.8f, 2f, m, 21);
            PottedPlant(g, new Vector3(-6.8f, 0f, 13.8f), 0.9f, m);
            // Refreshments and queue barriers.
            CoffeeStation(g, new Vector3(12.4f, 0f, 3.2f), -90f, m);
            for (int i = 0; i < 4; i++)
            {
                Part(g, "Stanchion", new Vector3(-4.4f + i * 1.2f, 0.5f, -2.4f), new Vector3(0.08f, 0.5f, 0.08f), m.chrome, type: PrimitiveType.Cylinder);
                Part(g, "StanchionBase", new Vector3(-4.4f + i * 1.2f, 0.02f, -2.4f), new Vector3(0.35f, 0.02f, 0.35f), m.chrome, type: PrimitiveType.Cylinder);
                if (i < 3) Part(g, "Rope", new Vector3(-3.8f + i * 1.2f, 0.85f, -2.4f), new Vector3(1.1f, 0.04f, 0.04f), m.flagRed, shadows: false);
            }
            BrochureStand(g, new Vector3(1.4f, 0f, -1f), 180f, m);
        }

        // ------------------------------------------------------------------ TYRE SHOP
        static void FurnishTyreShopExtras(Transform g, Mats m)
        {
            // Tyre wall behind the counter, a wheel balancer, a waiting area.
            Part(g, "TyreWallBoard", new Vector3(0f, 2.2f, 8.82f), new Vector3(5f, 2.6f, 0.04f), m.signBoard);
            for (int r = 0; r < 3; r++)
                for (int i = 0; i < 6; i++)
                    Part(g, "WallTyre", new Vector3(-2f + i * 0.8f, 1.3f + r * 0.85f, 8.6f), new Vector3(0.7f, 0.12f, 0.7f), m.tire, type: PrimitiveType.Cylinder, euler: new Vector3(90f, 0f, 0f));
            var bal = Slot(g, "Balancer", new Vector3(-4.3f, 0f, 1.4f), PlaceholderKind.Equipment, new Vector3(0.9f, 1.4f, 0.8f), 0f, "balancer", 1);
            Part(bal, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(0.8f, 1f, 0.7f), m.flagBlue, solid: true);
            Part(bal, "Shaft", new Vector3(0.45f, 0.9f, 0f), new Vector3(0.05f, 0.3f, 0.05f), m.chrome, type: PrimitiveType.Cylinder, euler: new Vector3(0f, 0f, 90f));
            Part(bal, "Hood", new Vector3(0.35f, 1.15f, 0f), new Vector3(0.6f, 0.2f, 0.6f), m.blackGlass);
            for (int i = 0; i < 3; i++) DiningChair(g, new Vector3(5.2f, 0f, 1.8f + i * 0.7f), -90f, m.chrome, m.sofa);
            CoffeeTable(g, new Vector3(4.2f, 0f, 2.5f), 90f, 1f, 0.5f, m);
            WallArt(g, new Vector3(-5.83f, 2.2f, 6f), 90f, 1f, 0.7f, m.art[2], m);
        }
    }
}
