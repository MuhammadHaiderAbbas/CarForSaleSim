using System.Collections.Generic;
using System.Linq;
using CarSales.Sim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
#if CARSALES_SPLINES
using Unity.Mathematics;
using UnityEngine.Splines;
#endif

namespace CarSales.EditorTools
{
    /// <summary>
    /// Phase 14f: real road meshes (RoadMesher) and Unity Splines. Every road of the town plan becomes a spline
    /// (a TownRoad object with a SplineContainer, when com.unity.splines is installed) and the meshes are built from
    /// those lines: carriageway, kerbs, raised pavements with dropped kerbs at entrances, junctions with rounded
    /// corners, roundabouts, verges and every white line. Signs and beacons go where the lines say: give way, roundabout
    /// ahead, zebra crossings. Edit a spline, then Tools > Car Sales > Roads > Rebuild Road Meshes.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        const string RoadMeshPath = Gen + "/Meshes/RoadMeshes.asset";
        const string SplinesPackage = "com.unity.splines";

        // ------------------------------------------------------------------ MENU
        [MenuItem("Tools/Car Sales/Roads/Rebuild Road Meshes (from splines)", priority = 60)]
        public static void RebuildRoadMeshesMenu()
        {
            var root = GameObject.Find("Environment/Roads") ?? GameObject.Find("Roads");
            if (!root) { EditorUtility.DisplayDialog("Roads", "No Roads object in the open scene. Run Build Everything first.", "OK"); return; }
            var m = LoadMats();
            var inputs = RoadInputsFromScene(root.transform);
            var set = RoadMesher.Build(inputs, TownPlan.Roundabouts, TownPlan.LightNodes, RoadMesher.TownEntrances(), RoadMesher.TownCrossings);
            BuildRoadMeshes(root.transform, set, m);
            EditorSceneManager.MarkSceneDirty(root.scene);
#if CARSALES_SPLINES
            string how = "from the splines";
#else
            string how = "from the town plan (install Unity Splines to edit the roads as splines)";
#endif
            Debug.Log($"[CarSales] Rebuilt {set.Chunks.Count} road meshes {how}: {set.Runs.Count} stretches, {set.Features.Count} signs and crossings.");
        }

        [MenuItem("Tools/Car Sales/Roads/Install Unity Splines", priority = 61)]
        public static void InstallSplines()
        {
            UnityEditor.PackageManager.Client.Add(SplinesPackage);
            Debug.Log("[CarSales] Installing Unity Splines - once it has compiled, run Build Everything (or Rebuild Road Meshes) and each road is a spline you can edit.");
        }

        // ------------------------------------------------------------------ BUILD EVERYTHING
        /// <summary>The town's roads: a spline per road, then the meshes made from them.</summary>
        static void BuildTownRoads(Transform root, Mats m)
        {
            var splines = Group(root, "RoadSplines");
            foreach (var r in TownPlan.Roads)
            {
                var go = new GameObject("Road_" + r.Name);
                go.transform.SetParent(splines, false);
                var road = go.AddComponent<TownRoad>();
                road.roadName = r.Name;
                road.width = r.Width;
                road.pavements = r.Pavements;
                road.speedLimit = r.SpeedLimit;
#if CARSALES_SPLINES
                var container = go.AddComponent<SplineContainer>();
                var spline = container.Spline;
                spline.Clear();
                bool closed = r.Points.Length > 2 && V2.Distance(r.Points[0], r.Points[r.Points.Length - 1]) < 1f;
                int count = closed ? r.Points.Length - 1 : r.Points.Length;
                for (int i = 0; i < count; i++)
                    spline.Add(new BezierKnot((float3)go.transform.InverseTransformPoint(W(r.Points[i], RoadMesher.RoadY))), TangentMode.Linear);
                spline.Closed = closed;
#endif
            }
            BuildRoadMeshes(root, RoadMesher.BuildTown(), m);
        }

        /// <summary>The roads as the splines in the scene describe them (the town plan when Splines isn't installed).</summary>
        static List<RoadInput> RoadInputsFromScene(Transform root)
        {
            var list = new List<RoadInput>();
#if CARSALES_SPLINES
            foreach (var road in root.GetComponentsInChildren<TownRoad>())
            {
                var container = road.GetComponent<SplineContainer>();
                if (!container || container.Spline == null || container.Spline.Count < 2) continue;
                var spline = container.Spline;
                var pts = new List<V2>();
                V2 World(float3 local) { var w = container.transform.TransformPoint((Vector3)local); return new V2(w.x, w.z); }
                int curves = spline.Closed ? spline.Count : spline.Count - 1;
                pts.Add(World(spline[0].Position));
                for (int i = 0; i < curves; i++)
                {
                    int j = (i + 1) % spline.Count;
                    bool straight = spline.GetTangentMode(i) == TangentMode.Linear && spline.GetTangentMode(j) == TangentMode.Linear;
                    if (!straight)
                    {
                        // A curved stretch: follow it every 4 m (the knots themselves stay exact, so junctions still meet).
                        var curve = spline.GetCurve(i);
                        int n = Mathf.Max(2, Mathf.CeilToInt(spline.GetCurveLength(i) / 4f));
                        for (int k = 1; k < n; k++) pts.Add(World(CurveUtility.EvaluatePosition(curve, k / (float)n)));
                    }
                    pts.Add(World(spline[j].Position));
                }
                list.Add(new RoadInput { Name = road.roadName, Points = pts.ToArray(), Width = road.width, Pavements = road.pavements });
            }
#endif
            return list.Count > 0 ? list : RoadMesher.TownRoads();
        }

        // ------------------------------------------------------------------ MESHES
        static Material RoadMaterial(RoadSurface s, Mats m)
        {
            switch (s)
            {
                case RoadSurface.Asphalt: return m.asphalt;
                case RoadSurface.Kerb: return m.concrete ? m.concrete : m.sidewalk;
                case RoadSurface.Pavement: return m.sidewalk;
                case RoadSurface.Marking: return m.line;
                case RoadSurface.Verge: return m.fieldSoil ? m.fieldSoil : m.ground;
                default: return m.ground;
            }
        }

        static void BuildRoadMeshes(Transform root, RoadMeshSet set, Mats m)
        {
            var old = root.Find("RoadMeshes");
            if (old) Object.DestroyImmediate(old.gameObject);
            var old2 = root.Find("RoadSigns");
            if (old2) Object.DestroyImmediate(old2.gameObject);
            var meshesRoot = Group(root, "RoadMeshes");

            EnsureFolder(Gen + "/Meshes");
            AssetDatabase.DeleteAsset(RoadMeshPath);
            Object container = null;
            foreach (var ch in set.Chunks)
            {
                var used = Enumerable.Range(0, RoadChunk.SurfaceCount).Where(i => ch.Triangles[i].Count > 0).ToList();
                if (used.Count == 0) continue;
                var mesh = new Mesh { name = "Road " + ch.Name };
                if (ch.Vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(ch.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToList());
                mesh.SetUVs(0, ch.Vertices.Select(v => new Vector2(v.U, v.V)).ToList());
                mesh.subMeshCount = used.Count;
                for (int k = 0; k < used.Count; k++) mesh.SetTriangles(ch.Triangles[used[k]], k);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                if (container == null) { AssetDatabase.CreateAsset(mesh, RoadMeshPath); container = mesh; }
                else AssetDatabase.AddObjectToAsset(mesh, container);

                var go = new GameObject(ch.Name);
                go.transform.SetParent(meshesRoot, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = used.Select(i => RoadMaterial((RoadSurface)i, m)).ToArray();
                mr.shadowCastingMode = ShadowCastingMode.Off;   // flat: kerbs are too low to cast useful shadows
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                go.isStatic = true;
            }
            AssetDatabase.SaveAssets();
            BuildRoadSigns(Group(root, "RoadSigns"), set, m);
        }

        // ------------------------------------------------------------------ SIGNS AND BEACONS
        static void BuildRoadSigns(Transform root, RoadMeshSet set, Mats m)
        {
            foreach (var f in set.Features)
            {
                var g = Group(root, f.Kind.ToString());
                g.position = W(f.Kerb);
                // Signs face the traffic coming towards them.
                g.rotation = Quaternion.LookRotation(new Vector3(-f.Facing.x, 0f, -f.Facing.z));
                switch (f.Kind)
                {
                    case RoadFeatureKind.GiveWay:
                        SignPost(g, m, 2.3f);
                        var rim = Prim(PrimitiveType.Cylinder, "GiveWayRim", g, new Vector3(0f, 2.15f, 0.06f), new Vector3(0.8f, 0.015f, 0.8f), m.flagRed);
                        rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        var plate = Prim(PrimitiveType.Cylinder, "GiveWayPlate", g, new Vector3(0f, 2.15f, 0.08f), new Vector3(0.64f, 0.015f, 0.64f), m.signWhite);
                        plate.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        SignText(g, new Vector3(0f, 2.15f, 0.1f), "GIVE\nWAY", 2.4f, new Color(0.75f, 0.08f, 0.08f), new Vector2(0.55f, 0.4f), 180f);
                        break;
                    case RoadFeatureKind.RoundaboutEntry:
                        SignPost(g, m, 2.3f);
                        var disc = Prim(PrimitiveType.Cylinder, "RoundaboutPlate", g, new Vector3(0f, 2.15f, 0.06f), new Vector3(0.7f, 0.015f, 0.7f), m.flagBlue);
                        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        SignText(g, new Vector3(0f, 2.15f, 0.08f), "ROUND-\nABOUT", 2.2f, Color.white, new Vector2(0.55f, 0.4f), 180f);
                        break;
                    case RoadFeatureKind.Zebra:
                        // Belisha beacons on both kerbs.
                        var across = new Vector3(f.Kerb.x - f.Position.x, 0f, f.Kerb.z - f.Position.z);
                        foreach (float side in new[] { 1f, -1f })
                        {
                            var b = Group(root, "BelishaBeacon");
                            b.position = W(f.Position) + across * side;
                            for (int i = 0; i < 6; i++)
                                Prim(PrimitiveType.Cylinder, "PoleBand", b, new Vector3(0f, 0.25f + i * 0.45f, 0f), new Vector3(0.11f, 0.225f, 0.11f), i % 2 == 0 ? m.blackGlass : m.signWhite);
                            Prim(PrimitiveType.Sphere, "Globe", b, new Vector3(0f, 2.95f, 0f), new Vector3(0.38f, 0.38f, 0.38f), m.lampGlow);
                            PointLight(b, new Vector3(0f, 2.95f, 0f), 6f, 0.8f, new Color(1f, 0.65f, 0.15f));
                        }
                        Object.DestroyImmediate(g.gameObject);
                        break;
                    default:
                        Object.DestroyImmediate(g.gameObject);   // stop lines: the traffic lights stand there at runtime
                        break;
                }
            }
        }

        static void SignPost(Transform g, Mats m, float height)
        {
            var post = Prim(PrimitiveType.Cylinder, "Post", g, new Vector3(0f, height / 2f, 0f), new Vector3(0.08f, height / 2f, 0.08f), m.metal, keepCollider: true);
            post.isStatic = true;
        }
    }
}
