using UnityEngine;
using UnityEngine.Rendering;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Builds the low-poly dealership world: lot, road, glass-front showroom with interior,
    /// service garage, car wash, street props and a city backdrop. All decorative (except colliders).
    /// Coordinates: +Z = north (showroom), the road runs along the south.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        static void BuildWorld(Transform env, Mats m, CarDatabase db)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(env, false);
            ground.transform.localScale = new Vector3(12f, 1f, 12f);   // 120 x 120 m
            ground.GetComponent<Renderer>().sharedMaterial = m.ground;
            ground.isStatic = true;

            BuildLotAndRoad(Group(env, "LotAndRoad"), m);
            BuildShowroom(Group(env, "Showroom"), m);
            BuildGarage(Group(env, "ServiceGarage"), m, db);
            BuildCarWash(Group(env, "CarWash"), m);
            BuildStreetProps(Group(env, "Props"), m);
            BuildBackdrop(Group(env, "CityBackdrop"), m);
        }

        static Transform Group(Transform parent, string name)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            return g;
        }

        static GameObject Solid(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = Prim(PrimitiveType.Cube, name, parent, pos, size, mat, keepCollider: true);
            go.isStatic = true;
            return go;
        }

        static GameObject Deco(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 size, Material mat)
        {
            var go = Prim(type, name, parent, pos, size, mat);
            go.isStatic = true;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static void PointLight(Transform parent, Vector3 pos, float range, float intensity, Color color)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = color;
            l.shadows = LightShadows.None;
        }

        static void SignText(Transform parent, Vector3 pos, string text, float size, Color color, Vector2 box, float yRotation = 0f)
        {
            var t = Text3D(text, parent, pos, text, size, color);
            t.rectTransform.sizeDelta = box;
            t.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        }

        // ------------------------------------------------------------------ LOT & ROAD
        static void BuildLotAndRoad(Transform g, Mats m)
        {
            Solid(g, "Lot_Asphalt", new Vector3(0f, 0.01f, -9f), new Vector3(36f, 0.02f, 24f), m.asphalt);
            // curbs around the lot (leave the south side open as the entrance)
            Deco(g, "Curb_W", PrimitiveType.Cube, new Vector3(-18.1f, 0.06f, -9f), new Vector3(0.25f, 0.12f, 24f), m.sidewalk);
            Deco(g, "Curb_E", PrimitiveType.Cube, new Vector3(18.1f, 0.06f, -9f), new Vector3(0.25f, 0.12f, 24f), m.sidewalk);
            // driving aisle arrows
            for (int i = -2; i <= 2; i++)
                Deco(g, "AisleMark", PrimitiveType.Cube, new Vector3(i * 6f, 0.025f, -9f), new Vector3(1.6f, 0.01f, 0.25f), m.yellowLine);

            Solid(g, "Road", new Vector3(0f, 0.008f, -27f), new Vector3(120f, 0.016f, 9f), m.asphalt);
            for (float x = -57f; x <= 57f; x += 6f)
                Deco(g, "LaneDash", PrimitiveType.Cube, new Vector3(x, 0.02f, -27f), new Vector3(3f, 0.01f, 0.18f), m.line);
            Deco(g, "EdgeLine_N", PrimitiveType.Cube, new Vector3(0f, 0.02f, -23f), new Vector3(120f, 0.01f, 0.12f), m.line);
            Deco(g, "EdgeLine_S", PrimitiveType.Cube, new Vector3(0f, 0.02f, -31f), new Vector3(120f, 0.01f, 0.12f), m.line);
            Solid(g, "Sidewalk_S", new Vector3(0f, 0.06f, -32.4f), new Vector3(120f, 0.12f, 2.6f), m.sidewalk);
            Solid(g, "Sidewalk_N_W", new Vector3(-39.5f, 0.06f, -21.9f), new Vector3(41f, 0.12f, 1.8f), m.sidewalk);
            Solid(g, "Sidewalk_N_E", new Vector3(39.5f, 0.06f, -21.9f), new Vector3(41f, 0.12f, 1.8f), m.sidewalk);
            // pedestrian path from the lot to the showroom door
            Solid(g, "Path_ToShowroom", new Vector3(0f, 0.015f, 6.5f), new Vector3(6f, 0.03f, 7f), m.sidewalk);
        }

        // ------------------------------------------------------------------ SHOWROOM
        static void BuildShowroom(Transform g, Mats m)
        {
            Solid(g, "Floor", new Vector3(0f, 0.02f, 17f), new Vector3(24f, 0.04f, 14f), m.floor);
            Solid(g, "Wall_North", new Vector3(0f, 2.5f, 24f), new Vector3(24.3f, 5f, 0.3f), m.wall);
            Solid(g, "Wall_West", new Vector3(-12f, 2.5f, 17f), new Vector3(0.3f, 5f, 14f), m.wall);
            Solid(g, "Wall_East", new Vector3(12f, 2.5f, 17f), new Vector3(0.3f, 5f, 14f), m.wall);
            Solid(g, "Roof", new Vector3(0f, 5.15f, 17f), new Vector3(24.8f, 0.3f, 14.8f), m.roof);

            // Glass front with frame posts and an open doorway (x -3..3)
            foreach (float x in new[] { -12f, -7.5f, -3f, 3f, 7.5f, 12f })
                Solid(g, "FramePost", new Vector3(x, 2.5f, 10f), new Vector3(0.22f, 5f, 0.32f), m.frame);
            foreach (float x in new[] { -9.75f, -5.25f, 5.25f, 9.75f })
            {
                var pane = Solid(g, "GlassPane", new Vector3(x, 2.5f, 10f), new Vector3(4.3f, 5f, 0.06f), m.windowGlass);
                pane.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            Solid(g, "FrameBottom", new Vector3(0f, 0.08f, 10f), new Vector3(24.2f, 0.16f, 0.32f), m.frame);
            Solid(g, "DoorHeader", new Vector3(0f, 4.4f, 10f), new Vector3(6f, 1.2f, 0.32f), m.frame);

            // Facade sign band
            Deco(g, "SignBand", PrimitiveType.Cube, new Vector3(0f, 5.95f, 9.95f), new Vector3(24.8f, 1.5f, 0.5f), m.accentPanel);
            Deco(g, "SignStripe", PrimitiveType.Cube, new Vector3(0f, 5.22f, 9.68f), new Vector3(24.8f, 0.06f, 0.05f), m.ringGlow);
            SignText(g, new Vector3(0f, 6.15f, 9.66f), "AUTO WORLD", 16f, Color.white, new Vector2(20f, 1.4f));
            SignText(g, new Vector3(0f, 5.5f, 9.66f), "USED CARS", 5.5f, new Color(0.45f, 0.7f, 1f), new Vector2(10f, 0.6f));

            // Interior: ceiling light panels + lights
            for (int ix = -1; ix <= 1; ix++)
                for (int iz = 0; iz < 3; iz++)
                    Deco(g, "CeilingLight", PrimitiveType.Cube, new Vector3(ix * 7.5f, 4.97f, 12.5f + iz * 4.5f), new Vector3(3f, 0.04f, 0.8f), m.ceilingLight);
            PointLight(g, new Vector3(-6f, 4.4f, 13.5f), 13f, 1.6f, new Color(1f, 0.96f, 0.9f));
            PointLight(g, new Vector3(6f, 4.4f, 13.5f), 13f, 1.6f, new Color(1f, 0.96f, 0.9f));
            PointLight(g, new Vector3(-6f, 4.4f, 20.5f), 13f, 1.6f, new Color(1f, 0.96f, 0.9f));
            PointLight(g, new Vector3(6f, 4.4f, 20.5f), 13f, 1.6f, new Color(1f, 0.96f, 0.9f));

            // Display platforms for the two showroom spots
            foreach (float x in new[] { -6f, 6f })
            {
                Solid(g, "DisplayPlatform", new Vector3(x, 0.07f, 17f), new Vector3(6f, 0.05f, 6f), m.platform).GetComponent<BoxCollider>();
                Deco(g, "PlatformGlow", PrimitiveType.Cylinder, new Vector3(x, 0.045f, 17f), new Vector3(6.3f, 0.02f, 6.3f), m.ringGlow);
            }

            // Reception desk
            Solid(g, "ReceptionDesk", new Vector3(-8.2f, 0.55f, 22.4f), new Vector3(3.4f, 1.1f, 0.8f), m.wall);
            Deco(g, "ReceptionTop", PrimitiveType.Cube, new Vector3(-8.2f, 1.13f, 22.4f), new Vector3(3.6f, 0.06f, 1.0f), m.desk);
            Deco(g, "ReceptionStripe", PrimitiveType.Cube, new Vector3(-8.2f, 0.6f, 21.99f), new Vector3(3.4f, 0.08f, 0.02f), m.ringGlow);
            SignText(g, new Vector3(-8.2f, 3.2f, 23.8f), "AUTO WORLD", 5f, new Color(0.15f, 0.25f, 0.45f), new Vector2(8f, 1f), 180f);

            // Customer lounge
            Solid(g, "Sofa", new Vector3(8.2f, 0.25f, 23.1f), new Vector3(3f, 0.5f, 0.9f), m.sofa);
            Deco(g, "SofaBack", PrimitiveType.Cube, new Vector3(8.2f, 0.75f, 23.5f), new Vector3(3f, 0.6f, 0.2f), m.sofa);
            Solid(g, "CoffeeTable", new Vector3(8.2f, 0.22f, 21.7f), new Vector3(1.6f, 0.44f, 0.8f), m.desk);
            Solid(g, "SideSofa", new Vector3(10.9f, 0.25f, 21.2f), new Vector3(0.9f, 0.5f, 2.4f), m.sofa);

            foreach (var p in new[] { new Vector3(-11f, 0f, 23f), new Vector3(11f, 0f, 11f), new Vector3(-11f, 0f, 11f) })
                Plant(g, p, m, 1f);
        }

        static void Plant(Transform g, Vector3 pos, Mats m, float scale)
        {
            Solid(g, "PlantPot", pos + new Vector3(0f, 0.3f * scale, 0f), new Vector3(0.6f, 0.6f, 0.6f) * scale, m.pot);
            Deco(g, "PlantLeaves", PrimitiveType.Sphere, pos + new Vector3(0f, 1.0f * scale, 0f), new Vector3(0.9f, 1.1f, 0.9f) * scale, m.leaves);
            Deco(g, "PlantLeaves2", PrimitiveType.Sphere, pos + new Vector3(0.15f, 1.5f * scale, 0.1f), new Vector3(0.6f, 0.7f, 0.6f) * scale, m.leaves);
        }

        // ------------------------------------------------------------------ SERVICE GARAGE
        static void BuildGarage(Transform g, Mats m, CarDatabase db)
        {
            Solid(g, "Floor", new Vector3(20f, 0.02f, 17f), new Vector3(12f, 0.04f, 14f), m.concrete);
            Solid(g, "Wall_North", new Vector3(20f, 2.5f, 24f), new Vector3(12.3f, 5f, 0.3f), m.wall);
            Solid(g, "Wall_East", new Vector3(26f, 2.5f, 17f), new Vector3(0.3f, 5f, 14f), m.wall);
            Solid(g, "Wall_West", new Vector3(14f, 2.5f, 17f), new Vector3(0.3f, 5f, 14f), m.wall);
            Solid(g, "Pillar_W", new Vector3(14.6f, 2.5f, 10f), new Vector3(1.2f, 5f, 0.3f), m.wall);
            Solid(g, "Pillar_E", new Vector3(25.4f, 2.5f, 10f), new Vector3(1.2f, 5f, 0.3f), m.wall);
            Solid(g, "Header", new Vector3(20f, 4.4f, 10f), new Vector3(12.3f, 1.2f, 0.3f), m.wall);
            Solid(g, "Roof", new Vector3(20f, 5.15f, 17f), new Vector3(12.8f, 0.3f, 14.8f), m.roof);
            Deco(g, "SignBand", PrimitiveType.Cube, new Vector3(20f, 5.95f, 9.95f), new Vector3(12.8f, 1.5f, 0.5f), m.accentPanel);
            SignText(g, new Vector3(20f, 5.95f, 9.66f), "SERVICE & REPAIR", 9f, Color.white, new Vector2(12f, 1.2f));
            Deco(g, "DoorStripe", PrimitiveType.Cube, new Vector3(20f, 0.03f, 10.6f), new Vector3(9.5f, 0.01f, 0.4f), m.yellowLine);

            // Two-post lift with a car on it
            foreach (float x in new[] { 17.6f, 22.4f })
            {
                Solid(g, "LiftPost", new Vector3(x, 1.6f, 17f), new Vector3(0.35f, 3.2f, 0.35f), m.liftYellow);
                Deco(g, "LiftArm", PrimitiveType.Cube, new Vector3(x + (x < 20f ? 0.6f : -0.6f), 1.55f, 17f), new Vector3(1.2f, 0.12f, 0.25f), m.liftYellow);
            }
            if (db.cars.Count > 2 && db.cars[2].displayPrefab)
            {
                var car = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(db.cars[2].displayPrefab, g);
                car.transform.localPosition = new Vector3(20f, 1.6f, 17f);
                car.name = "CarOnLift";
            }
            Solid(g, "ToolChest", new Vector3(25.2f, 0.55f, 21.5f), new Vector3(0.7f, 1.1f, 1.6f), m.toolRed);
            Solid(g, "Workbench", new Vector3(20f, 0.45f, 23.4f), new Vector3(4f, 0.9f, 0.8f), m.metal);
            for (int i = 0; i < 4; i++)
            {
                var tire = Deco(g, "Tire", PrimitiveType.Cylinder, new Vector3(15f, 0.13f + i * 0.26f, 22.8f), new Vector3(0.75f, 0.12f, 0.75f), m.tire);
                tire.AddComponent<BoxCollider>();
            }
            foreach (float z in new[] { 12f, 13.1f })
            {
                var barrel = Deco(g, "OilBarrel", PrimitiveType.Cylinder, new Vector3(25.2f, 0.5f, z), new Vector3(0.6f, 0.5f, 0.6f), m.barrel);
                barrel.AddComponent<CapsuleCollider>();
            }
            PointLight(g, new Vector3(20f, 4.4f, 17f), 14f, 1.8f, new Color(0.95f, 0.97f, 1f));
            for (int i = 0; i < 3; i++)
                Deco(g, "TrafficCone", PrimitiveType.Cylinder, new Vector3(16f + i * 1.1f, 0.3f, 8.6f), new Vector3(0.35f, 0.3f, 0.35f), m.cone);
        }

        // ------------------------------------------------------------------ CAR WASH
        static void BuildCarWash(Transform g, Mats m)
        {
            Solid(g, "Floor", new Vector3(-20f, 0.02f, 17f), new Vector3(12f, 0.04f, 12f), m.concrete);
            Solid(g, "Wall_West", new Vector3(-26f, 2.5f, 17f), new Vector3(0.3f, 5f, 12f), m.wall);
            Solid(g, "Wall_East", new Vector3(-14f, 2.5f, 17f), new Vector3(0.3f, 5f, 12f), m.wall);
            Solid(g, "Roof", new Vector3(-20f, 5.15f, 17f), new Vector3(12.8f, 0.3f, 12.8f), m.roof);
            Deco(g, "SignBand", PrimitiveType.Cube, new Vector3(-20f, 5.95f, 10.95f), new Vector3(12.8f, 1.5f, 0.5f), m.accentPanel);
            SignText(g, new Vector3(-20f, 5.95f, 10.66f), "CAR WASH", 11f, new Color(0.5f, 0.85f, 1f), new Vector2(12f, 1.2f));

            foreach (var p in new[] { new Vector3(-23f, 0f, 15f), new Vector3(-17f, 0f, 15f), new Vector3(-23f, 0f, 19.5f), new Vector3(-17f, 0f, 19.5f) })
            {
                var brush = Deco(g, "Brush", PrimitiveType.Cylinder, p + new Vector3(0f, 1.9f, 0f), new Vector3(1.3f, 1.8f, 1.3f), m.brushBlue);
                brush.AddComponent<CapsuleCollider>();
                Deco(g, "BrushAxle", PrimitiveType.Cylinder, p + new Vector3(0f, 4.2f, 0f), new Vector3(0.15f, 0.6f, 0.15f), m.metal);
            }
            var top = Deco(g, "TopBrush", PrimitiveType.Cylinder, new Vector3(-20f, 4f, 17.2f), new Vector3(0.9f, 3.5f, 0.9f), m.brushBlue);
            top.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Deco(g, "Gantry", PrimitiveType.Cube, new Vector3(-20f, 4.7f, 17.2f), new Vector3(11.5f, 0.3f, 0.4f), m.metal);
            PointLight(g, new Vector3(-20f, 4.3f, 17f), 12f, 1.5f, new Color(0.7f, 0.85f, 1f));
            for (int i = 0; i < 5; i++)
                Deco(g, "Puddle", PrimitiveType.Cylinder, new Vector3(-21.5f + i * 0.8f, 0.045f, 13f + (i % 2) * 0.7f), new Vector3(1.2f, 0.005f, 0.8f), m.windowGlass);
        }

        // ------------------------------------------------------------------ STREET PROPS
        static void BuildStreetProps(Transform g, Mats m)
        {
            // Trees (away from walking paths)
            foreach (var p in new[]
            {
                new Vector3(-50f, 0f, -35f), new Vector3(-35f, 0f, -35f), new Vector3(-20f, 0f, -35f), new Vector3(20f, 0f, -35f),
                new Vector3(35f, 0f, -35f), new Vector3(50f, 0f, -35f), new Vector3(-32f, 0f, -14f), new Vector3(32f, 0f, -14f),
                new Vector3(-32f, 0f, 2f), new Vector3(32f, 0f, 2f), new Vector3(-32f, 0f, 16f), new Vector3(32f, 0f, 16f),
                new Vector3(-36f, 0f, 30f), new Vector3(-22f, 0f, 31f), new Vector3(-6f, 0f, 30f), new Vector3(8f, 0f, 31f),
                new Vector3(22f, 0f, 30f), new Vector3(36f, 0f, 31f),
            })
                Tree(g, p, m, 0.85f + Mathf.Abs(Mathf.Sin(p.x * 0.37f + p.z)) * 0.4f);

            // Lamp posts at the lot corners
            foreach (var p in new[] { new Vector3(-19f, 0f, -20.5f), new Vector3(19f, 0f, -20.5f), new Vector3(-19f, 0f, 2.5f), new Vector3(19f, 0f, 2.5f) })
            {
                Solid(g, "LampPole", p + new Vector3(0f, 3f, 0f), new Vector3(0.18f, 6f, 0.18f), m.metal);
                Deco(g, "LampArm", PrimitiveType.Cube, p + new Vector3(0f, 6f, Mathf.Sign(-p.z - 9f) * 0.5f), new Vector3(0.12f, 0.12f, 1.2f), m.metal);
                Deco(g, "LampHead", PrimitiveType.Cube, p + new Vector3(0f, 5.9f, Mathf.Sign(-p.z - 9f) * 1.0f), new Vector3(0.45f, 0.12f, 0.6f), m.lampGlow);
                PointLight(g, p + new Vector3(0f, 5.5f, Mathf.Sign(-p.z - 9f) * 1.0f), 14f, 1.2f, new Color(1f, 0.9f, 0.7f));
            }

            // Promo banners along the lot
            var bannerMats = new[] { m.flagRed, m.flagBlue, m.flagWhite };
            int b = 0;
            foreach (float x in new[] { -16.5f, -13.5f, 13.5f, 16.5f })
            {
                var basePos = new Vector3(x, 0f, -20.2f);
                Solid(g, "BannerPole", basePos + new Vector3(0f, 2.5f, 0f), new Vector3(0.1f, 5f, 0.1f), m.metal);
                Deco(g, "Banner", PrimitiveType.Cube, basePos + new Vector3(0.38f, 3.3f, 0f), new Vector3(0.7f, 2.8f, 0.04f), bannerMats[b % 3]);
                SignText(g, basePos + new Vector3(0.38f, 3.3f, -0.03f), "SALE", 3f, b % 3 == 2 ? new Color(0.8f, 0.1f, 0.1f) : Color.white, new Vector2(0.7f, 0.6f));
                b++;
            }
            foreach (float x in new[] { -13f, 13f })
            {
                var basePos = new Vector3(x, 0f, 8.8f);
                Solid(g, "FlagPole", basePos + new Vector3(0f, 3.5f, 0f), new Vector3(0.12f, 7f, 0.12f), m.metal);
                Deco(g, "Flag", PrimitiveType.Cube, basePos + new Vector3(0.6f, 6.2f, 0f), new Vector3(1.1f, 1.2f, 0.03f), x < 0 ? m.flagBlue : m.flagRed);
            }

            // Benches, bins, planters in front of the showroom
            foreach (float x in new[] { -8f, 8f })
            {
                Solid(g, "BenchSeat", new Vector3(x, 0.45f, 7.6f), new Vector3(1.8f, 0.08f, 0.5f), m.desk);
                Deco(g, "BenchBack", PrimitiveType.Cube, new Vector3(x, 0.78f, 7.85f), new Vector3(1.8f, 0.45f, 0.06f), m.desk);
                Deco(g, "BenchLeg", PrimitiveType.Cube, new Vector3(x - 0.75f, 0.22f, 7.6f), new Vector3(0.08f, 0.44f, 0.45f), m.metal);
                Deco(g, "BenchLeg", PrimitiveType.Cube, new Vector3(x + 0.75f, 0.22f, 7.6f), new Vector3(0.08f, 0.44f, 0.45f), m.metal);
                var bin = Deco(g, "Bin", PrimitiveType.Cylinder, new Vector3(x + Mathf.Sign(x) * 1.6f, 0.45f, 7.6f), new Vector3(0.5f, 0.45f, 0.5f), m.bin);
                bin.AddComponent<CapsuleCollider>();
            }
            foreach (float x in new[] { -4.5f, 4.5f })
            {
                Solid(g, "Planter", new Vector3(x, 0.3f, 8.4f), new Vector3(1.3f, 0.6f, 1.3f), m.concrete);
                Deco(g, "Bush", PrimitiveType.Sphere, new Vector3(x, 0.85f, 8.4f), new Vector3(1.2f, 0.8f, 1.2f), m.leaves);
            }
        }

        static void Tree(Transform g, Vector3 pos, Mats m, float s)
        {
            Solid(g, "TreeTrunk", pos + new Vector3(0f, 1.5f * s, 0f), new Vector3(0.35f, 3f, 0.35f) * s, m.trunk);
            Deco(g, "TreeCanopy", PrimitiveType.Sphere, pos + new Vector3(0f, 3.9f * s, 0f), new Vector3(3.2f, 2.8f, 3.2f) * s, m.leaves);
            Deco(g, "TreeCanopy2", PrimitiveType.Sphere, pos + new Vector3(0.5f, 5f * s, 0.3f), new Vector3(2.2f, 2f, 2.2f) * s, m.leaves);
        }

        // ------------------------------------------------------------------ CITY BACKDROP
        static void BuildBackdrop(Transform g, Mats m)
        {
            float[] xs = { -48f, -31f, -14f, 3f, 20f, 37f, 52f };
            float[] heights = { 22f, 34f, 18f, 28f, 40f, 24f, 30f };
            for (int i = 0; i < xs.Length; i++)
            {
                Building(g, new Vector3(xs[i], 0f, 48f), new Vector3(14f, heights[i], 10f), m.buildings[i % m.buildings.Length], m, true);
                if (i % 2 == 0)
                    Building(g, new Vector3(xs[i] + 4f, 0f, -48f), new Vector3(13f, heights[(i + 3) % heights.Length] * 0.8f, 10f),
                             m.buildings[(i + 1) % m.buildings.Length], m, false);
            }
        }

        static void Building(Transform g, Vector3 pos, Vector3 size, Material mat, Mats m, bool facesSouth)
        {
            Solid(g, "Building", pos + new Vector3(0f, size.y / 2f, 0f), size, mat);
            float faceZ = facesSouth ? pos.z - size.z / 2f - 0.06f : pos.z + size.z / 2f + 0.06f;
            int floors = Mathf.FloorToInt(size.y / 3.2f);
            for (int f = 1; f < floors; f++)
                Deco(g, "Windows", PrimitiveType.Cube, new Vector3(pos.x, f * 3.2f + 0.6f, faceZ), new Vector3(size.x * 0.86f, 1.3f, 0.06f), m.windowsGlow);
            Deco(g, "RoofTop", PrimitiveType.Cube, pos + new Vector3(0f, size.y + 0.4f, 0f), new Vector3(size.x * 0.4f, 0.8f, size.z * 0.4f), m.roof);
        }
    }
}
