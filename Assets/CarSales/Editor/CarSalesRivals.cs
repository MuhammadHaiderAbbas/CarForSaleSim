using System.Linq;
using UnityEngine;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Phase 14e: the rival dealers in town. Downtown Motors and Luxury Lane trade from the two dealerships you can buy
    /// (their stock stands on the lot, their hire fleet in the corner bays, a hire desk in the showroom); Cheap Wheels
    /// has its own car sales and hire yard on the loop road (lot, hire bay, sales cabin with two serving windows,
    /// palisade fence, bunting, floodlights). All of it is driven by RivalManager at runtime.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        static readonly Color RivalOrange = new Color(1f, 0.6f, 0.15f);

        /// <summary>A rival yard: tow destination, "in the yard" check and a handover bay in the aisle.</summary>
        static ServiceZone RivalYard(Transform g, string rivalId, string name, Vector3 center, Vector3 size, params Vector3[] bays)
        {
            var zone = g.gameObject.AddComponent<ServiceZone>();
            zone.type = ServiceType.RivalYard;
            zone.displayName = name;
            zone.propertyId = rivalId;
            zone.center = center;
            zone.size = size;
            zone.bays = bays.Select((p, i) =>
            {
                var b = Child("HandoverBay_" + i, g, p);
                b.localRotation = Quaternion.Euler(0f, 90f, 0f);
                return b;
            }).ToArray();
            return zone;
        }

        /// <summary>An invisible box on the Interactable layer over a counter or desk.</summary>
        static RivalDesk RivalCounter(Transform g, string name, Vector3 pos, Vector3 size, RivalDealership site, int tab)
        {
            var go = new GameObject(name);
            go.transform.SetParent(g, false);
            go.transform.localPosition = pos;
            go.layer = InteractLayer;
            go.AddComponent<BoxCollider>().size = size;
            var desk = go.AddComponent<RivalDesk>();
            desk.site = site;
            desk.tab = tab;
            return desk;
        }

        /// <summary>Downtown Motors / Luxury Lane: while you don't own the premises the rival trades from them.</summary>
        static void AddRivalToDealership(GameObject site, string rivalId, Mats m, bool premium)
        {
            var g = site.transform;
            var dealer = site.GetComponent<DealershipManager>();
            var profile = CarSales.Sim.RivalSim.Find(rivalId);
            var rival = site.AddComponent<RivalDealership>();
            rival.rivalId = rivalId;
            rival.lotSpots = dealer.spots.Where(s => s.requiredBuildingLevel == 0).Select(s => s.transform).ToArray();
            rival.fleetSpots = dealer.spots.Where(s => s.requiredBuildingLevel == 2).Select(s => s.transform).ToArray();
            rival.yard = RivalYard(g, rivalId, profile.Name, new Vector3(0f, 1f, -9f), new Vector3(36f, 4f, 24f),
                new Vector3(-8f, 0.05f, -9f), new Vector3(0f, 0.05f, -9f), new Vector3(8f, 0.05f, -9f));
            rival.handover = rival.yard.bays[1];

            var open = Group(g, "RivalOpen");
            // The hire desk where the sales computer would be (the showroom plan keeps that spot clear).
            Solid(open, "HireDesk", new Vector3(0f, 0.5f, 22f), new Vector3(1.8f, 1f, 0.75f), m.desk);
            Deco(open, "HireDeskTop", PrimitiveType.Cube, new Vector3(0f, 1.02f, 22f), new Vector3(1.9f, 0.04f, 0.85f), premium ? m.marble ?? m.worktop : m.worktop);
            Deco(open, "HireScreen", PrimitiveType.Cube, new Vector3(0.45f, 1.28f, 22.15f), new Vector3(0.5f, 0.32f, 0.03f), m.screen);
            Deco(open, "HireKeys", PrimitiveType.Cube, new Vector3(-0.5f, 1.06f, 21.85f), new Vector3(0.3f, 0.03f, 0.2f), m.chrome ?? m.metal);
            Deco(open, "HireSignBoard", PrimitiveType.Cube, new Vector3(0f, 2.9f, 23.82f), new Vector3(3.4f, 0.8f, 0.04f), premium ? m.signBoard : m.accentPanel);
            SignText(open, new Vector3(0f, 2.9f, 23.78f), "CAR HIRE", 5f, premium ? new Color(1f, 0.85f, 0.4f) : Color.white, new Vector2(3.2f, 0.7f));
            RivalCounter(open, "HireCounter", new Vector3(0f, 1.1f, 21.95f), new Vector3(1.9f, 0.3f, 0.85f), rival, 2);
            // The sales desk is the showroom's reception (InteriorPlan: reception at -8.2, 22.4).
            RivalCounter(open, "SalesCounter", new Vector3(-8.2f, 1.1f, 22.2f), new Vector3(3.4f, 0.3f, 0.7f), rival, 0);
            // Hire bay boards on the lot corners and an "open" board by the road.
            foreach (float x in new[] { -17.2f, 17.2f })
            {
                Solid(open, "HirePost", new Vector3(x, 1.1f, -9f), new Vector3(0.1f, 2.2f, 0.1f), m.metal);
                Deco(open, "HireBoard", PrimitiveType.Cube, new Vector3(x, 2.05f, -9f), new Vector3(0.05f, 0.7f, 1.6f), m.signWhite);
                SignText(open, new Vector3(x + (x < 0 ? 0.04f : -0.04f), 2.05f, -9f), "HIRE CARS", 3f, new Color(0.1f, 0.2f, 0.45f), new Vector2(1.5f, 0.6f), x < 0 ? 90f : -90f);
            }
            Solid(open, "OpenBoardLeg", new Vector3(-13f, 0.6f, -20.2f), new Vector3(0.08f, 1.2f, 0.08f), m.metal);
            Deco(open, "OpenBoard", PrimitiveType.Cube, new Vector3(-13f, 1.2f, -20.25f), new Vector3(1.6f, 1.0f, 0.04f), m.signWhite);
            SignText(open, new Vector3(-13f, 1.2f, -20.29f), $"{profile.Name.ToUpper()}\nCARS FOR SALE\nWE BUY ANY CAR\nCAR HIRE", 3f, new Color(0.1f, 0.2f, 0.45f), new Vector2(1.5f, 0.9f));
            var staff = Child("RivalStaff", g, new Vector3(-10.6f, 0f, 22.2f));
            staff.localRotation = Quaternion.Euler(0f, 180f, 0f);
            rival.staffPoint = staff;
            rival.openOnly = new[] { open.gameObject };
        }

        /// <summary>Cheap Wheels: a budget car sales and hire yard (site origin at the entrance, the lot behind it).</summary>
        static void BuildCheapWheels(Transform g, Mats m)
        {
            var site = g.gameObject;
            var rival = site.AddComponent<RivalDealership>();
            rival.rivalId = "cheapwheels";

            // Ground, kerbs and markings.
            Solid(g, "Yard_Asphalt", new Vector3(0f, 0.01f, 14f), new Vector3(34f, 0.02f, 34f), m.asphalt);
            Solid(g, "Entrance_Apron", new Vector3(0f, 0.012f, -1.8f), new Vector3(9f, 0.02f, 2.6f), m.concrete);
            foreach (float x in new[] { -17.05f, 17.05f })
                Deco(g, "Kerb", PrimitiveType.Cube, new Vector3(x, 0.06f, 14f), new Vector3(0.25f, 0.12f, 34f), m.sidewalk);

            // Sales lot: two rows of five, nose to the aisle.
            var spots = Child("LotSpots", g, Vector3.zero);
            var lot = new System.Collections.Generic.List<Transform>();
            foreach (var (z, yaw) in new[] { (6f, 0f), (14.5f, 180f) })
                for (int i = 0; i < 5; i++)
                {
                    float x = -12f + i * 6f;
                    var s = Child($"Spot_{lot.Count}", spots, new Vector3(x, 0.02f, z));
                    s.localRotation = Quaternion.Euler(0f, yaw, 0f);
                    lot.Add(s);
                    foreach (float dx in new[] { -1.4f, 1.4f })
                        Deco(g, "BayLine", PrimitiveType.Cube, new Vector3(x + dx, 0.022f, z), new Vector3(0.12f, 0.01f, 4.8f), m.line);
                }
            rival.lotSpots = lot.ToArray();
            for (int i = -2; i <= 2; i++) Deco(g, "AisleArrow", PrimitiveType.Cube, new Vector3(i * 6f, 0.022f, 10.25f), new Vector3(1.4f, 0.01f, 0.2f), m.yellowLine);

            // Hire bay at the back left, under a canopy.
            var fleet = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < 4; i++)
            {
                float x = -14.2f + i * 3.4f;
                var s = Child($"HireSpot_{i}", spots, new Vector3(x, 0.02f, 24.5f));
                s.localRotation = Quaternion.Euler(0f, 0f, 0f);
                fleet.Add(s);
                Deco(g, "HireLine", PrimitiveType.Cube, new Vector3(x - 1.7f, 0.022f, 24.5f), new Vector3(0.12f, 0.01f, 4.8f), m.yellowLine);
            }
            rival.fleetSpots = fleet.ToArray();
            Solid(g, "HireCanopy", new Vector3(-9.1f, 3.3f, 24.5f), new Vector3(14.2f, 0.18f, 6.4f), m.cladding ?? m.roof);
            Deco(g, "HireCanopyFascia", PrimitiveType.Cube, new Vector3(-9.1f, 3.05f, 21.3f), new Vector3(14.2f, 0.5f, 0.06f), m.flagRed);
            SignText(g, new Vector3(-9.1f, 3.05f, 21.26f), "CAR & VAN HIRE  -  FROM £25 A DAY", 4f, Color.white, new Vector2(13.5f, 0.45f));
            foreach (float x in new[] { -16f, -2.2f })
                foreach (float z in new[] { 21.6f, 27.4f })
                    Solid(g, "CanopyPost", new Vector3(x, 1.6f, z), new Vector3(0.14f, 3.2f, 0.14f), m.metal);
            PointLight(g, new Vector3(-9.1f, 3f, 24.5f), 12f, 1.1f, new Color(1f, 0.95f, 0.85f));

            // Sales cabin (back right): a portacabin with two serving windows facing the lot.
            var cabin = Group(g, "SalesCabin");
            cabin.localPosition = new Vector3(9.5f, 0f, 25.5f);
            float cw = 10f, cd = 5f, ch = 2.9f;
            Solid(cabin, "Base", new Vector3(0f, 0.2f, 0f), new Vector3(cw, 0.4f, cd), m.concrete);
            Solid(cabin, "Floor", new Vector3(0f, 0.42f, 0f), new Vector3(cw - 0.2f, 0.04f, cd - 0.2f), m.carpet ?? m.floor);
            Solid(cabin, "Back", new Vector3(0f, 0.4f + ch / 2f, cd / 2f), new Vector3(cw, ch, 0.12f), m.cladding ?? m.wall);
            Solid(cabin, "SideL", new Vector3(-cw / 2f, 0.4f + ch / 2f, 0f), new Vector3(0.12f, ch, cd), m.cladding ?? m.wall);
            Solid(cabin, "SideR", new Vector3(cw / 2f, 0.4f + ch / 2f, 0f), new Vector3(0.12f, ch, cd), m.cladding ?? m.wall);
            Solid(cabin, "Roof", new Vector3(0f, 0.4f + ch + 0.08f, 0f), new Vector3(cw + 0.3f, 0.16f, cd + 0.3f), m.roof);
            // Front: wall under and over the windows, three piers between them.
            Solid(cabin, "FrontLow", new Vector3(0f, 0.95f, -cd / 2f), new Vector3(cw, 1.1f, 0.12f), m.cladding ?? m.wall);
            Solid(cabin, "FrontHigh", new Vector3(0f, 2.85f, -cd / 2f), new Vector3(cw, 0.9f, 0.12f), m.cladding ?? m.wall);
            foreach (float x in new[] { -4.4f, 0f, 4.4f })
                Solid(cabin, "FrontPier", new Vector3(x, 1.95f, -cd / 2f), new Vector3(1.2f, 0.9f, 0.12f), m.cladding ?? m.wall);
            foreach (var (x, label, tab) in new[] { (2.2f, "SALES  -  WE BUY ANY CAR", 0), (-2.2f, "HIRE DESK", 2) })
            {
                Solid(cabin, "Counter", new Vector3(x, 1.53f, -cd / 2f - 0.15f), new Vector3(3.2f, 0.06f, 0.5f), m.worktop);
                Deco(cabin, "Shutter", PrimitiveType.Cube, new Vector3(x, 2.33f, -cd / 2f - 0.02f), new Vector3(3.2f, 0.12f, 0.12f), m.metal);
                Deco(cabin, "WindowSign", PrimitiveType.Cube, new Vector3(x, 2.9f, -cd / 2f - 0.08f), new Vector3(3f, 0.55f, 0.03f), tab == 0 ? m.flagRed : m.flagBlue);
                SignText(cabin, new Vector3(x, 2.9f, -cd / 2f - 0.11f), label, 3f, Color.white, new Vector2(2.9f, 0.45f));
                // Inside: a desk with a screen and paperwork behind each window.
                Solid(cabin, "InnerDesk", new Vector3(x, 0.8f, -cd / 2f + 0.55f), new Vector3(3f, 0.75f, 0.7f), m.desk);
                Deco(cabin, "DeskScreen", PrimitiveType.Cube, new Vector3(x + 0.6f, 1.38f, -cd / 2f + 0.7f), new Vector3(0.5f, 0.32f, 0.03f), m.screen);
                Deco(cabin, "Paperwork", PrimitiveType.Cube, new Vector3(x - 0.7f, 1.2f, -cd / 2f + 0.5f), new Vector3(0.32f, 0.03f, 0.24f), m.signWhite);
                RivalCounter(cabin, tab == 0 ? "SalesWindow" : "HireWindow", new Vector3(x, 1.75f, -cd / 2f - 0.15f), new Vector3(3.2f, 0.45f, 0.5f), rival, tab);
            }
            Solid(cabin, "KeyCabinet", new Vector3(4.6f, 1.6f, cd / 2f - 0.15f), new Vector3(0.6f, 0.8f, 0.12f), m.metal);
            Solid(cabin, "FilingCabinet", new Vector3(-4.5f, 1.05f, cd / 2f - 0.35f), new Vector3(0.5f, 1.3f, 0.6f), m.metal);
            Deco(cabin, "CeilingLight", PrimitiveType.Cube, new Vector3(0f, 3.25f, 0f), new Vector3(3f, 0.04f, 0.4f), m.ceilingLight);
            PointLight(cabin, new Vector3(0f, 3f, 0f), 7f, 1f, new Color(1f, 0.95f, 0.85f));
            var staff = Child("RivalStaff", cabin, new Vector3(2.2f, 0.42f, 0.6f));
            staff.localRotation = Quaternion.Euler(0f, 180f, 0f);
            rival.staffPoint = staff;
            Solid(cabin, "Steps", new Vector3(0f, 0.12f, -cd / 2f - 0.6f), new Vector3(7f, 0.24f, 0.8f), m.concrete);

            // Palisade fence round the yard, open at the entrance; bunting and the big sign at the gate.
            int f = 0;
            for (float x = -15.95f; x <= 16f; x += 2.2f)
                if (Mathf.Abs(x) > 5.2f) FencePanel(g, new Vector3(x, 0f, -2.6f), 0f, f++, m);
            for (float z = -1.5f; z <= 30.5f; z += 2.2f)
            {
                FencePanel(g, new Vector3(-17.4f, 0f, z), 90f, f++, m);
                FencePanel(g, new Vector3(17.4f, 0f, z), 90f, f++, m);
            }
            for (float x = -15.95f; x <= 16f; x += 2.2f) FencePanel(g, new Vector3(x, 0f, 31.4f), 0f, f++, m);

            foreach (float x in new[] { -5.2f, 5.2f })
                Solid(g, "GatePost", new Vector3(x, 2.6f, -2.6f), new Vector3(0.3f, 5.2f, 0.3f), m.metal);
            Deco(g, "GateSign", PrimitiveType.Cube, new Vector3(0f, 4.6f, -2.6f), new Vector3(10.8f, 1.4f, 0.2f), m.flagRed);
            SignText(g, new Vector3(0f, 4.85f, -2.72f), "CHEAP WHEELS", 11f, Color.white, new Vector2(10f, 0.8f));
            SignText(g, new Vector3(0f, 4.2f, -2.72f), "CARS  -  VANS  -  CASH FOR YOUR CAR  -  HIRE", 4f, new Color(1f, 0.92f, 0.4f), new Vector2(10f, 0.45f));
            var bunting = new[] { m.flagRed, m.flagWhite, m.flagBlue, RivalBuntingYellow(m) };
            int b = 0;
            foreach (var (from, to) in new[] { (new Vector3(-16.8f, 3.4f, -2.4f), new Vector3(-5.4f, 4.2f, -2.4f)), (new Vector3(5.4f, 4.2f, -2.4f), new Vector3(16.8f, 3.4f, -2.4f)),
                                               (new Vector3(-16.8f, 3.4f, 4f), new Vector3(16.8f, 3.4f, 18f)) })
                for (float t = 0.04f; t < 1f; t += 0.06f)
                {
                    var p = Vector3.Lerp(from, to, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.5f;
                    var flag = Deco(g, "Bunting", PrimitiveType.Cube, p, new Vector3(0.28f, 0.3f, 0.01f), bunting[b++ % bunting.Length]);
                    flag.transform.localRotation = Quaternion.LookRotation(to - from) * Quaternion.Euler(0f, 90f, 45f);
                }
            foreach (var p in new[] { new Vector3(-16.6f, 0f, 0.4f), new Vector3(16.6f, 0f, 0.4f), new Vector3(-16.6f, 0f, 30.6f), new Vector3(16.6f, 0f, 30.6f) })
            {
                Solid(g, "FloodPole", p + new Vector3(0f, 3.5f, 0f), new Vector3(0.16f, 7f, 0.16f), m.metal);
                Deco(g, "FloodHead", PrimitiveType.Cube, p + new Vector3(0f, 7f, 0f), new Vector3(0.7f, 0.25f, 0.4f), m.lampGlow);
                PointLight(g, p + new Vector3(0f, 6.6f, 0f), 22f, 1.3f, new Color(1f, 0.93f, 0.8f));
            }

            // Odds and ends: A-boards, a valeting corner, tyres, drums, cones, a bin.
            foreach (var (pos, text) in new[] { (new Vector3(-3.6f, 0f, 0.8f), "WE BUY\nANY CAR\nCASH TODAY"), (new Vector3(3.6f, 0f, 0.8f), "CARS FROM\n£1,000\nPX WELCOME") })
            {
                Deco(g, "ABoard", PrimitiveType.Cube, pos + new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 1.1f, 0.08f), m.signWhite);
                SignText(g, pos + new Vector3(0f, 0.6f, -0.05f), text, 2.4f, new Color(0.75f, 0.1f, 0.1f), new Vector2(0.65f, 0.9f));
            }
            var valet = Group(g, "ValetCorner");
            valet.localPosition = new Vector3(14.6f, 0f, 20.5f);
            Solid(valet, "PressureWasher", new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.5f), m.toolRed);
            Deco(valet, "HoseReel", PrimitiveType.Cylinder, new Vector3(0f, 1.05f, 0f), new Vector3(0.45f, 0.08f, 0.45f), m.blackGlass);
            Solid(valet, "ShampooDrum", new Vector3(0.9f, 0.45f, 0.2f), new Vector3(0.55f, 0.9f, 0.55f), m.barrel);
            Deco(valet, "Bucket", PrimitiveType.Cylinder, new Vector3(-0.8f, 0.17f, 0.1f), new Vector3(0.32f, 0.17f, 0.32f), m.flagBlue);
            Deco(valet, "Drain", PrimitiveType.Cube, new Vector3(-1.6f, 0.012f, -2f), new Vector3(0.5f, 0.01f, 2.5f), m.metal);
            for (int i = 0; i < 4; i++)
                Deco(g, "TyreStack", PrimitiveType.Cylinder, new Vector3(15.6f, 0.12f + i * 0.24f, 12f), new Vector3(0.7f, 0.12f, 0.7f), m.tire ?? m.blackGlass);
            foreach (var p in new[] { new Vector3(-15.8f, 0f, 9.5f), new Vector3(-15.8f, 0f, 18.5f) })
                Deco(g, "Cone", PrimitiveType.Cylinder, p + new Vector3(0f, 0.35f, 0f), new Vector3(0.3f, 0.35f, 0.3f), m.cone);
            Solid(g, "Bin", new Vector3(4.2f, 0.5f, 28.8f), new Vector3(0.7f, 1f, 0.7f), m.bin);
            foreach (var p in new[] { new Vector3(-15f, 0f, 30f), new Vector3(-5f, 0f, 30.3f) }) Tree(g, p, m, 0.8f);

            rival.yard = RivalYard(g, "cheapwheels", "Cheap Wheels", new Vector3(0f, 1f, 14f), new Vector3(34f, 4f, 34f),
                new Vector3(-8f, 0.05f, 10.25f), new Vector3(0f, 0.05f, 10.25f), new Vector3(8f, 0.05f, 10.25f));
            rival.handover = rival.yard.bays[1];
            AddMapPlace(site, "Cheap Wheels", "C", RivalOrange, new Vector3(0f, 0.1f, -7f));
            var info = site.AddComponent<Site>();
            info.siteId = "CheapWheels";
            info.displayName = "Cheap Wheels";
            info.function = SiteFunction.Other;
            info.entrance = Child("Entrance", g, new Vector3(0f, 0f, -4f));
        }

        static Material RivalBuntingYellow(Mats m) => m.liftYellow ? m.liftYellow : m.flagWhite;
    }
}
