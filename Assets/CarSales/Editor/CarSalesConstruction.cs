using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarSales.EditorTools
{
    /// <summary>
    /// Phase 14c: the construction site every rebuildable building gets, built once and switched on by
    /// BuildingLevelVisual stage by stage: Clearing (levelled ground, excavator, dump truck, rubble), Site (foundations,
    /// cabins, tower crane, mixer truck, loader, materials) and Frame (slab, columns, floors, scaffolding - the draft of
    /// the new building). Every machine and material pile is a PlaceholderSlot (kind Construction) so the default
    /// content pack's real machines take their place.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        static void BuildConstructionSite(Transform g, BuildingLevelVisual v)
        {
            var m = mats;
            if (m == null) return;
            var root = Child("Construction", g, Vector3.zero);
            Vector3 c = v.siteCentre, s = v.siteSize;
            float hx = s.x / 2f, hz = s.z / 2f;
            float front = c.z - hz - 1.5f, back = c.z + hz + 1f, left = c.x - hx - 1f, right = c.x + hx + 1f;
            float height = Mathf.Clamp(s.y * 0.8f, 6f, 60f);

            // --- Always: hoarding round the plot (gate at the front), floodlights, the site board.
            var common = Child("Common", root, Vector3.zero);
            int f = 0;
            for (float x = left + 1.1f; x <= right - 1.0f; x += 2.2f)
                if (Mathf.Abs(x - c.x) > 3.2f) FencePanel(common, new Vector3(x, 0f, front), 0f, f++, m);
            for (float z = front + 1.1f; z <= back - 1.0f; z += 2.2f)
            {
                FencePanel(common, new Vector3(left, 0f, z), 90f, f++, m);
                FencePanel(common, new Vector3(right, 0f, z), 90f, f++, m);
            }
            for (float x = left + 1.1f; x <= right - 1.0f; x += 2.2f) FencePanel(common, new Vector3(x, 0f, back), 0f, f++, m);
            foreach (var (x, z) in new[] { (left + 1f, front + 1f), (right - 1f, back - 1f) })
            {
                Part(common, "FloodPole", new Vector3(x, 4f, z), new Vector3(0.15f, 8f, 0.15f), m.metal);
                Part(common, "Floodlight", new Vector3(x, 8.1f, z), new Vector3(0.9f, 0.4f, 0.3f), m.lampGlow, shadows: false);
            }
            Part(common, "BoardPost", new Vector3(c.x + 5.2f, 1.2f, front - 0.25f), new Vector3(0.12f, 2.4f, 0.12f), m.metal);
            Part(common, "Board", new Vector3(c.x + 5.2f, 2.1f, front - 0.3f), new Vector3(3.4f, 1.5f, 0.06f), m.liftYellow);
            var label = Text3D("SiteLabel", common, new Vector3(c.x + 5.2f, 2.1f, front - 0.34f), "UNDER CONSTRUCTION", 2.8f, Color.black);
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(3.2f, 1.4f);
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            v.constructionLabel = label;

            // --- Stage 1: the old building is gone - levelled earth, rubble, an excavator and a dump truck.
            var clearing = Child("Stage_Clearing", root, Vector3.zero);
            Part(clearing, "LevelledGround", new Vector3(c.x, 0.03f, c.z), new Vector3(s.x, 0.06f, s.z), m.fieldSoil, solid: true);
            for (int i = 0; i < 3; i++)
                Part(clearing, "Rubble", new Vector3(c.x - hx * 0.5f + i * hx * 0.45f, 0.2f, c.z + hz * 0.45f - i * 1.5f), new Vector3(3.2f - i * 0.6f, 1.2f, 2.6f), i == 1 ? m.brick : m.concrete,
                     solid: true, type: PrimitiveType.Sphere);
            Machine(clearing, "excavator", new Vector3(c.x - hx * 0.25f, 0f, c.z - hz * 0.1f), 35f, 3.2f, m, 1);
            Machine(clearing, "dumptruck", new Vector3(c.x + hx * 0.35f, 0f, c.z - hz * 0.45f), -90f, 3.4f, m, 2);
            for (int i = 0; i < 4; i++) Part(clearing, "Cone", new Vector3(c.x - 2.4f + i * 1.6f, 0.35f, front + 1.6f), new Vector3(0.35f, 0.35f, 0.35f), m.cone, type: PrimitiveType.Cylinder);

            // --- Stage 2: foundations, cabins, tower crane, mixer truck, loader and materials.
            var site = Child("Stage_Site", root, Vector3.zero);
            Part(site, "Ground", new Vector3(c.x, 0.03f, c.z), new Vector3(s.x, 0.06f, s.z), m.fieldSoil, solid: true);
            float fx = hx * 0.85f, fz = hz * 0.8f;
            foreach (var (pos, size) in new[]
            {
                (new Vector3(c.x, 0.12f, c.z - fz), new Vector3(fx * 2f, 0.24f, 0.7f)), (new Vector3(c.x, 0.12f, c.z + fz), new Vector3(fx * 2f, 0.24f, 0.7f)),
                (new Vector3(c.x - fx, 0.12f, c.z), new Vector3(0.7f, 0.24f, fz * 2f)), (new Vector3(c.x + fx, 0.12f, c.z), new Vector3(0.7f, 0.24f, fz * 2f)),
                (new Vector3(c.x, 0.12f, c.z), new Vector3(fx * 2f, 0.24f, 0.6f)), (new Vector3(c.x, 0.12f, c.z), new Vector3(0.6f, 0.24f, fz * 2f)),
            })
                Part(site, "Footing", pos, size, m.concrete, solid: true);
            for (float x = c.x - fx + 2f; x < c.x + fx; x += 2f) Part(site, "Rebar", new Vector3(x, 0.4f, c.z - fz), new Vector3(0.03f, 0.6f, 0.03f), m.metal, shadows: false);
            Machine(site, "cabin", new Vector3(left + 4f, 0f, back - 2.2f), 0f, 2.6f, m, 1);
            Machine(site, "cabin", new Vector3(left + 4f, 0f, back - 5f), 0f, 2.6f, m, 2);
            Machine(site, "crane", new Vector3(right - 3f, 0f, back - 3f), -90f, Mathf.Max(24f, height * 2f), m, 1);
            Machine(site, "mixer", new Vector3(c.x + fx * 0.4f, 0f, c.z - fz - 3f), 90f, 3.6f, m, 1);
            Machine(site, "loader", new Vector3(c.x - fx * 0.5f, 0f, c.z + fz * 0.3f), -30f, 3.3f, m, 1);
            for (int i = 0; i < 3; i++) Machine(site, "bricks", new Vector3(right - 4f - i * 1.6f, 0f, front + 2.5f), 0f, 1.1f, m, i + 1);
            for (int i = 0; i < 2; i++) Machine(site, "pallet", new Vector3(left + 3f + i * 1.6f, 0f, front + 2.5f), 0f, 0.9f, m, i + 1);
            for (int i = 0; i < 4; i++) Part(site, "SteelBeam", new Vector3(c.x - fx * 0.6f, 0.12f + i * 0.24f, c.z + fz + 2f), new Vector3(8f, 0.22f, 0.25f), m.racking);
            Part(site, "SandPile", new Vector3(c.x + fx * 0.7f, 0.3f, c.z + fz * 0.6f), new Vector3(3.5f, 1.5f, 3f), m.fieldWheat, type: PrimitiveType.Sphere);
            Part(site, "Portaloo", new Vector3(left + 7.5f, 1.15f, back - 1.6f), new Vector3(1.1f, 2.3f, 1.1f), m.brushBlue, solid: true);

            // --- Stage 3: the draft of the new building - slab, columns, floors and scaffolding.
            var frame = Child("Stage_Frame", root, Vector3.zero);
            Part(frame, "Slab", new Vector3(c.x, 0.15f, c.z), new Vector3(fx * 2f + 1f, 0.3f, fz * 2f + 1f), m.concrete, solid: true);
            int nx = Mathf.Max(2, Mathf.RoundToInt(fx * 2f / 5f)), nz = Mathf.Max(2, Mathf.RoundToInt(fz * 2f / 5f));
            for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++)
                    Part(frame, "Column", new Vector3(c.x - fx + i * (fx * 2f / nx), 0.3f + height / 2f, c.z - fz + j * (fz * 2f / nz)), new Vector3(0.4f, height, 0.4f), m.concrete, solid: true);
            for (float y = 3.5f; y < height; y += 3.5f)
                Part(frame, "Floor", new Vector3(c.x, y, c.z), new Vector3(fx * 2f + 0.4f, 0.25f, fz * 2f + 0.4f), m.concrete);
            Part(frame, "Roof", new Vector3(c.x, height + 0.3f, c.z), new Vector3(fx * 2f + 0.4f, 0.25f, fz * 2f + 0.4f), m.concrete);
            Part(frame, "BackWall", new Vector3(c.x, 1.6f, c.z + fz), new Vector3(fx * 2f, 2.6f, 0.3f), m.brick, solid: true);
            Part(frame, "SideWall", new Vector3(c.x - fx, 1.2f, c.z), new Vector3(0.3f, 1.8f, fz * 2f), m.brick, solid: true);
            int k = 0;
            for (float x = c.x - fx; x <= c.x + fx + 0.01f; x += 4f)
                Machine(frame, "scaffold", new Vector3(x, 0f, c.z - fz - 1.2f), 0f, height + 1.2f, m, ++k);
            Machine(frame, "crane", new Vector3(right - 3f, 0f, back - 3f), -90f, Mathf.Max(24f, height * 2f), m, 2);
            Machine(frame, "mixer", new Vector3(c.x - fx * 0.3f, 0f, c.z - fz - 4f), 90f, 3.6f, m, 2);
            Machine(frame, "bulldozer", new Vector3(left + 5f, 0f, c.z + fz * 0.2f), 70f, 3.2f, m, 1);
            for (int i = 0; i < 2; i++) Machine(frame, "bricks", new Vector3(right - 4f - i * 1.6f, 0f, front + 2.5f), 0f, 1.1f, m, i + 5);
            Machine(frame, "cabin", new Vector3(left + 4f, 0f, back - 2.2f), 0f, 2.6f, m, 3);

            v.constructionRoot = root;
            v.constructionStages = new[] { clearing, site, frame };
            root.gameObject.SetActive(false);
        }

        static void FencePanel(Transform g, Vector3 pos, float yaw, int variant, Mats m)
        {
            var t = Slot(g, "Fence", pos, PlaceholderKind.Construction, new Vector3(2.1f, 1.9f, 0.4f), 0f, "fence", variant);
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Part(t, "Panel", new Vector3(0f, 1.05f, 0f), new Vector3(2.05f, 1.7f, 0.04f), variant % 2 == 0 ? m.racking : m.flagWhite, solid: true);
            foreach (var x in new[] { -0.95f, 0.95f })
                Part(t, "Foot", new Vector3(x, 0.08f, 0f), new Vector3(0.25f, 0.16f, 0.5f), m.concrete);
        }

        /// <summary>A construction machine or material pile: a primitive stand-in inside a slot the real model replaces.</summary>
        static void Machine(Transform g, string style, Vector3 pos, float yaw, float height, Mats m, int variant)
        {
            Vector3 foot;
            switch (style)
            {
                case "crane": foot = new Vector3(4f, height, 4f); break;
                case "cabin": foot = new Vector3(6f, height, 2.5f); break;
                case "scaffold": foot = new Vector3(4f, height, 1.2f); break;
                case "bricks": case "pallet": foot = new Vector3(1.2f, height, 1f); break;
                default: foot = new Vector3(3f, height, 7f); break;
            }
            var t = Slot(g, "Machine_" + style, pos, PlaceholderKind.Construction, foot, 0f, style, variant);
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var yellow = m.liftYellow;
            switch (style)
            {
                case "crane":
                    Part(t, "Base", new Vector3(0f, 0.5f, 0f), new Vector3(4f, 1f, 4f), m.concrete, solid: true);
                    Part(t, "Mast", new Vector3(0f, height / 2f, 0f), new Vector3(1.6f, height, 1.6f), yellow, solid: true);
                    Part(t, "Jib", new Vector3(0f, height + 0.6f, -height * 0.25f), new Vector3(1.2f, 1.2f, height * 0.9f), yellow);
                    Part(t, "CounterJib", new Vector3(0f, height + 0.6f, height * 0.22f), new Vector3(1.2f, 1f, height * 0.25f), yellow);
                    Part(t, "Counterweight", new Vector3(0f, height - 0.4f, height * 0.32f), new Vector3(2f, 2f, 2f), m.concrete);
                    Part(t, "Cab", new Vector3(1.3f, height - 1f, 0f), new Vector3(1.2f, 1.6f, 1.6f), m.windowGlass);
                    Part(t, "Cable", new Vector3(0f, height * 0.6f, -height * 0.55f), new Vector3(0.05f, height * 0.8f, 0.05f), m.metal, shadows: false);
                    break;
                case "excavator":
                    foreach (var x in new[] { -1.1f, 1.1f }) Part(t, "Track", new Vector3(x, 0.45f, 0f), new Vector3(0.7f, 0.9f, 4f), m.blackGlass, solid: true);
                    Part(t, "Body", new Vector3(0f, 1.5f, 0.3f), new Vector3(2.4f, 1.1f, 2.8f), yellow, solid: true);
                    Part(t, "Cab", new Vector3(-0.6f, 2.5f, -0.3f), new Vector3(1f, 1.3f, 1.2f), m.windowGlass);
                    Part(t, "Boom", new Vector3(0.4f, 2.6f, -2f), new Vector3(0.45f, 0.5f, 3.2f), yellow, euler: new Vector3(-30f, 0f, 0f));
                    Part(t, "Stick", new Vector3(0.4f, 2.4f, -3.9f), new Vector3(0.35f, 2.2f, 0.35f), yellow, euler: new Vector3(20f, 0f, 0f));
                    Part(t, "Bucket", new Vector3(0.4f, 1.2f, -4.2f), new Vector3(1.1f, 0.8f, 0.8f), m.metal);
                    break;
                case "cabin":
                    Part(t, "Container", new Vector3(0f, height / 2f, 0f), new Vector3(6f, height, 2.4f), m.flagWhite, solid: true);
                    Part(t, "Door", new Vector3(1.8f, 1f, -1.21f), new Vector3(0.9f, 2f, 0.04f), m.metal, shadows: false);
                    foreach (var x in new[] { -1.6f, 0f }) Part(t, "Window", new Vector3(x, 1.5f, -1.21f), new Vector3(1f, 0.7f, 0.04f), m.windowGlass, shadows: false);
                    break;
                case "scaffold":
                    foreach (var x in new[] { -2f, 2f })
                        foreach (var z in new[] { -0.55f, 0.55f })
                            Part(t, "Pole", new Vector3(x, height / 2f, z), new Vector3(0.06f, height, 0.06f), m.metal);
                    for (float y = 2f; y < height; y += 2f)
                    {
                        Part(t, "Board", new Vector3(0f, y, 0f), new Vector3(4.1f, 0.05f, 1.2f), m.timber);
                        Part(t, "Rail", new Vector3(0f, y + 1f, -0.55f), new Vector3(4.1f, 0.05f, 0.05f), m.metal, shadows: false);
                    }
                    break;
                case "bricks":
                    Part(t, "Pallet", new Vector3(0f, 0.07f, 0f), new Vector3(1.2f, 0.14f, 1f), m.timber, solid: true);
                    Part(t, "Bricks", new Vector3(0f, 0.6f, 0f), new Vector3(1.1f, 0.9f, 0.95f), m.brick, solid: true);
                    break;
                case "pallet":
                    Part(t, "Pallet", new Vector3(0f, 0.07f, 0f), new Vector3(1.2f, 0.14f, 1f), m.timber, solid: true);
                    Part(t, "Bags", new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.6f, 0.9f), m.flagWhite, solid: true);
                    break;
                default:   // trucks, loader, bulldozer: a chassis, a cab and the business end
                    Part(t, "Chassis", new Vector3(0f, 0.9f, 0f), new Vector3(2.4f, 0.8f, 6.4f), yellow, solid: true);
                    Part(t, "Cab", new Vector3(0f, 2.1f, style == "bulldozer" ? 0.6f : -2.1f), new Vector3(2.2f, 1.6f, 1.8f), m.windowGlass);
                    foreach (var x in new[] { -1.15f, 1.15f })
                        foreach (var z in new[] { -2.2f, 2.2f })
                            Part(t, "Wheel", new Vector3(x, 0.55f, z), new Vector3(1.1f, 0.25f, 1.1f), m.tire, type: PrimitiveType.Cylinder, euler: new Vector3(0f, 0f, 90f));
                    if (style == "mixer") Part(t, "Drum", new Vector3(0f, 2.3f, 1f), new Vector3(2f, 1.8f, 2f), m.flagWhite, type: PrimitiveType.Sphere, euler: new Vector3(80f, 0f, 0f));
                    else if (style == "dumptruck") Part(t, "Body", new Vector3(0f, 2f, 1.1f), new Vector3(2.4f, 1.4f, 3.8f), m.metal);
                    else Part(t, "Blade", new Vector3(0f, 0.8f, -3.5f), new Vector3(2.8f, 1.2f, 0.3f), m.metal);
                    break;
            }
        }
    }
}
