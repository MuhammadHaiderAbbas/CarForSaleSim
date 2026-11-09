using System.Collections.Generic;
using System.IO;
using System.Linq;
using CarSales.Sim;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace CarSales.EditorTools
{
    /// <summary>
    /// World Builder (spec §7.3, Phase 8): add working dealership sites to the town (optionally inside an imported
    /// building), keep them in Data/WorldLayout.asset so Build Everything rebuilds them, and validate every site.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        public const string LayoutPath = "Assets/CarSales/Data/WorldLayout.asset";
        public const string DefaultLayoutPath = "Assets/CarSales/Data/Layout_DefaultTown.asset";

        public static WorldLayout LoadLayout(bool create)
        {
            var l = AssetDatabase.LoadAssetAtPath<WorldLayout>(LayoutPath);
            if (l || !create) return l;
            if (!AssetDatabase.IsValidFolder("Assets/CarSales/Data")) AssetDatabase.CreateFolder("Assets/CarSales", "Data");
            l = ScriptableObject.CreateInstance<WorldLayout>();
            AssetDatabase.CreateAsset(l, LayoutPath);
            return l;
        }

        static GameObject BuildingPrefab(string assetId)
        {
            if (string.IsNullOrEmpty(assetId)) return null;
            var reg = AssetRegistry.Load();
            var r = reg ? reg.Find(assetId) : null;
            return r != null ? r.prefab : null;
        }

        /// <summary>Builds one World Builder site into the town (called by Build Everything and by the window).</summary>
        static GameObject BuildUserSite(Transform env, Mats m, SiteRecord rec, CarDisplay displayPrefab, CustomerAI customerPrefab, List<NavMeshSurface> surfaces)
        {
            var refs = new TownRefs();
            bool premium = rec.template == SiteTemplate.PremiumDealership;
            var site = BuildDealershipSite(env, m, refs, displayPrefab, customerPrefab, rec.id, rec.name, rec.price > 0 ? rec.id : "", rec.position,
                                           false, premium ? 1.5f : 1f, premium, BuildingPrefab(rec.buildingAssetId), computer: true, yaw: rec.yaw);
            site.name = "Site_" + rec.id;
            var s = site.GetComponent<Site>();
            s.price = rec.price;
            s.dailyCost = rec.dailyCost;
            s.userBuilt = true;
            s.buildingAssetId = rec.buildingAssetId ?? "";
            surfaces?.AddRange(refs.surfaces);
            return site;
        }

        /// <summary>Build Everything: rebuild the sites you added, tag the generated ones, write Layout_DefaultTown.</summary>
        static void BuildLayoutSites(Transform env, Mats m, CarDisplay displayPrefab, CustomerAI customerPrefab, List<NavMeshSurface> surfaces)
        {
            TagDefaultSites(env);
            var layout = LoadLayout(false);
            if (layout)
                foreach (var rec in layout.sites) BuildUserSite(env, m, rec, displayPrefab, customerPrefab, surfaces);

            var net = env.gameObject.GetComponent<RoadNetwork>();
            if (!net) net = env.gameObject.AddComponent<RoadNetwork>();
            net.segments = layout && layout.roads.Count > 0 ? new List<RoadSegment>(layout.roads) : RoadGraph.DefaultTown();

            // The generated v4 town as a layout (for reference and the validator).
            var def = AssetDatabase.LoadAssetAtPath<WorldLayout>(DefaultLayoutPath);
            if (!def) { def = ScriptableObject.CreateInstance<WorldLayout>(); AssetDatabase.CreateAsset(def, DefaultLayoutPath); }
            def.sites = env.GetComponentsInChildren<Site>().Where(x => !x.userBuilt).Select(x => new SiteRecord
            {
                id = x.siteId, name = x.displayName, function = x.function, position = x.transform.position, yaw = x.transform.eulerAngles.y,
            }).ToList();
            def.roads = RoadGraph.DefaultTown();
            EditorUtility.SetDirty(def);
        }

        /// <summary>Gives the generated service sites a Site component (from their map icon).</summary>
        static void TagDefaultSites(Transform env)
        {
            foreach (var place in env.GetComponentsInChildren<MapPlace>())
            {
                if (place.GetComponent<Site>()) continue;
                var s = place.gameObject.AddComponent<Site>();
                s.siteId = place.gameObject.name;
                s.displayName = place.placeName;
                s.entrance = place.arrivalPoint;
                s.function = place.glyph == "S" ? SiteFunction.ServiceCentre : place.glyph == "W" ? SiteFunction.Workshop : place.glyph == "C" ? SiteFunction.CarWash :
                             place.glyph == "F" ? SiteFunction.FuelStation : place.glyph == "A" ? SiteFunction.AuctionCenter : place.glyph == "U" ? SiteFunction.UsedCarMarket :
                             place.glyph == "H" ? SiteFunction.House : place.glyph == "$" ? SiteFunction.Investment : place.glyph == "P" ? SiteFunction.PartsShop :
                             place.glyph == "M" ? SiteFunction.Street : SiteFunction.Other;
            }
        }

        /// <summary>Adds a site to the open town scene and the layout, bakes its NavMesh and saves. Returns a message.</summary>
        public static string AddSiteToTown(SiteRecord rec)
        {
            if (!File.Exists(GameScenePath)) return "Run Build Everything first.";
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return "Cancelled.";
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var env = GameObject.Find("Town");
            if (!env) return "The town scene has no 'Town' object. Run Build Everything.";
            var displayPrefab = AssetDatabase.LoadAssetAtPath<CarDisplay>($"{Gen}/Prefabs/CarDisplay.prefab");
            var customerPrefab = AssetDatabase.LoadAssetAtPath<CustomerAI>($"{Gen}/Prefabs/Customer.prefab");
            if (!displayPrefab || !customerPrefab) return "Generated prefabs are missing. Run Build Everything.";
            var m = LoadMats();
            var surfaces = new List<NavMeshSurface>();
            var old = GameObject.Find("Site_" + rec.id);
            if (old) Object.DestroyImmediate(old);
            BuildUserSite(env.transform, m, rec, displayPrefab, customerPrefab, surfaces);
            foreach (var s in surfaces)
            {
                s.BuildNavMesh();
                if (s.navMeshData) { AssetDatabase.CreateAsset(s.navMeshData, $"{Gen}/Scenes/NavMesh_{rec.id}.asset"); EditorUtility.SetDirty(s); }
            }
            var layout = LoadLayout(true);
            layout.sites.RemoveAll(x => x.id == rec.id);
            layout.sites.Add(rec);
            EditorUtility.SetDirty(layout);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var issues = ValidateSite(GameObject.Find("Site_" + rec.id)?.GetComponent<Site>());
            return issues.Count == 0 ? $"{rec.name} built and saved. Validation: all good." : $"{rec.name} built. Validation: {string.Join(" ", issues)}";
        }

        public static string RemoveSite(string id)
        {
            var layout = LoadLayout(true);
            layout.sites.RemoveAll(x => x.id == id);
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
            if (File.Exists(GameScenePath) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
                var go = GameObject.Find("Site_" + id);
                if (go) Object.DestroyImmediate(go);
                AssetDatabase.DeleteAsset($"{Gen}/Scenes/NavMesh_{id}.asset");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            return "Site removed.";
        }

        // ------------------------------------------------------------------ VALIDATION
        /// <summary>
        /// Spec §7.3 checks: entrance on the NavMesh, a road within reach, a map place, and for dealerships enough spots,
        /// customer spawns, an exit and a NavMesh surface. Returns problems (empty = all good).
        /// </summary>
        public static List<string> ValidateSite(Site s)
        {
            var issues = new List<string>();
            if (!s) { issues.Add("Site not found."); return issues; }
            var p = s.EntrancePosition;
            if (!NavMesh.SamplePosition(p, out _, 4f, NavMesh.AllAreas)) issues.Add("Entrance is not on the NavMesh (bake it, or move the entrance).");
            var lanes = RoadGraph.Build(RoadGraph.DefaultTown());
            var net = Object.FindFirstObjectByType<RoadNetwork>();
            if (net && net.segments.Count > 0) lanes = RoadGraph.Build(net.segments, net.roadWidth);
            float road = lanes.Count == 0 ? 0f : lanes.Min(l => DistanceToLane(l, p));
            if (road > 45f) issues.Add($"No road within 45 m (nearest {road:0} m): customers and tow trucks need a road.");
            if (!s.GetComponentInChildren<MapPlace>()) issues.Add("No map place (it won't show on the map / GPS).");
            if (s.function == SiteFunction.Dealership)
            {
                var d = s.GetComponent<DealershipManager>();
                if (!d) issues.Add("Dealership without a DealershipManager.");
                else
                {
                    var so = new SerializedObject(d);
                    var spots = so.FindProperty("spots");
                    if (spots == null || spots.arraySize < 4) issues.Add("Fewer than 4 parking spots.");
                    var spawns = so.FindProperty("customerSpawnPoints");
                    if (spawns == null || spawns.arraySize == 0) issues.Add("No customer spawn points.");
                    if (!d.exitPoint) issues.Add("No customer exit point.");
                }
                if (!s.GetComponentInChildren<NavMeshSurface>()) issues.Add("No NavMesh surface for customers.");
                if (!s.GetComponentInChildren<DealerComputer>()) issues.Add("No computer (warning: you can still use the main one).");
            }
            return issues;
        }

        static float DistanceToLane(Lane l, Vector3 p)
        {
            var a = new Vector2(l.start.x, l.start.z); var b = new Vector2(l.end.x, l.end.z); var q = new Vector2(p.x, p.z);
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
            return Vector2.Distance(q, a + ab * t);
        }
    }

    /// <summary>Tools > Car Sales > World Builder.</summary>
    public class WorldBuilderWindow : EditorWindow
    {
        static readonly string[] Tabs = { "Sites", "Validate" };
        int tab;
        Vector2 scroll;
        string status = "";
        string newName = "Riverside Motors";
        int buildingIndex;
        SiteTemplate template = SiteTemplate.Dealership;
        int price = 120000, dailyCost = 600;
        Vector3 position = new Vector3(-200f, 0f, 30f);
        float yaw;
        readonly List<(string site, List<string> issues)> results = new List<(string, List<string>)>();

        [MenuItem("Tools/Car Sales/World Builder", priority = 30)]
        public static void Open() => GetWindow<WorldBuilderWindow>("World Builder").minSize = new Vector2(560f, 480f);

        void OnGUI()
        {
            tab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(26));
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, UnityEditor.MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (tab == 0) DrawSites(); else DrawValidate();
            EditorGUILayout.EndScrollView();
        }

        void DrawSites()
        {
            EditorGUILayout.HelpBox("Add a working dealership to the town: lot with 14 parking spots, showroom (or one of your imported buildings), " +
                                    "computer, customers, sign and map icon. It's kept in Data/WorldLayout.asset, so Build Everything rebuilds it.", UnityEditor.MessageType.None);
            var layout = CarSalesSetupWizard.LoadLayout(false);
            EditorGUILayout.LabelField("Your sites", EditorStyles.boldLabel);
            if (layout == null || layout.sites.Count == 0) EditorGUILayout.LabelField("  None yet.");
            else
                foreach (var r in layout.sites.ToList())
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{r.name}  ({r.template}, {(r.price > 0 ? $"${r.price:N0}" : "owned")}, at {r.position.x:0}, {r.position.z:0})");
                        if (GUILayout.Button("Rebuild", GUILayout.Width(70))) status = CarSalesSetupWizard.AddSiteToTown(r);
                        if (GUILayout.Button("Remove", GUILayout.Width(70)) && EditorUtility.DisplayDialog("World Builder", $"Remove {r.name} from the town?", "Remove", "Cancel"))
                            status = CarSalesSetupWizard.RemoveSite(r.id);
                    }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("New dealership", EditorStyles.boldLabel);
            newName = EditorGUILayout.TextField("Name", newName);
            template = (SiteTemplate)EditorGUILayout.EnumPopup("Type", template);
            var reg = AssetRegistry.Load();
            var buildings = reg ? reg.records.Where(r => r != null && r.prefab && r.category == CarSales.Studio.AssetCategory.Building).ToList() : new List<AssetRecord>();
            var names = new[] { "Glass showroom (standard)" }.Concat(buildings.Select(b => b.displayName)).ToArray();
            buildingIndex = EditorGUILayout.Popup("Building", Mathf.Clamp(buildingIndex, 0, names.Length - 1), names);
            price = EditorGUILayout.IntField(new GUIContent("Price", "0 = yours from the start; otherwise buy it in Real Estate."), price);
            dailyCost = EditorGUILayout.IntField("Rent per day", dailyCost);
            position = EditorGUILayout.Vector3Field("Position (lot centre)", position);
            yaw = EditorGUILayout.Slider("Turn (0 = facing south)", yaw, 0f, 359f);
            if (GUILayout.Button("Use the Scene view's centre")) { var sv = SceneView.lastActiveSceneView; if (sv) position = new Vector3(Mathf.Round(sv.pivot.x), 0f, Mathf.Round(sv.pivot.z)); }
            EditorGUILayout.HelpBox("The lot faces the road in front of it (local -Z): put it about 25 m from a road. The validator checks it.", UnityEditor.MessageType.None);
            if (GUILayout.Button("Build it in the town", GUILayout.Height(32)))
            {
                string id = "site_" + new string(newName.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
                var rec = new SiteRecord
                {
                    id = id, name = newName, template = template, buildingAssetId = buildingIndex > 0 ? buildings[buildingIndex - 1].id : "",
                    position = position, yaw = yaw, price = Mathf.Max(0, price), dailyCost = Mathf.Max(0, dailyCost), function = SiteFunction.Dealership,
                };
                status = CarSalesSetupWizard.AddSiteToTown(rec);
            }
        }

        void DrawValidate()
        {
            EditorGUILayout.HelpBox("Checks every site in the open scene: entrance on the NavMesh, a road within reach, a map icon; dealerships also " +
                                    "need 4+ parking spots, customer spawns, an exit and a NavMesh surface.", UnityEditor.MessageType.None);
            if (GUILayout.Button("Validate the open scene", GUILayout.Height(30)))
            {
                results.Clear();
                foreach (var s in Object.FindObjectsByType<Site>(FindObjectsSortMode.None).OrderBy(x => x.displayName))
                    results.Add((s.displayName, CarSalesSetupWizard.ValidateSite(s)));
                if (results.Count == 0) status = "No sites in this scene. Open the town scene (Tools > Car Sales > Build Everything first).";
            }
            foreach (var (site, issues) in results)
            {
                if (issues.Count == 0) EditorGUILayout.HelpBox($"{site}: OK", UnityEditor.MessageType.Info);
                else EditorGUILayout.HelpBox($"{site}:\n- " + string.Join("\n- ", issues), issues.Any(i => !i.Contains("warning")) ? UnityEditor.MessageType.Error : UnityEditor.MessageType.Warning);
            }
        }
    }
}
