using System;
using System.IO;
using System.Linq;
using CarSales.EditorTools;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CarSales.Studio.EditorTools
{
    /// <summary>
    /// Tools > Car Sales > Asset Studio. Brings external 3D models into the game:
    /// choose a model → say what it is and its licence → check scale/orientation → (cars) wheels, paint and car data
    /// → validation → Approve: a game-ready prefab, a licence record and (cars) a CarDefinition in the car list.
    /// </summary>
    public class AssetStudioWindow : EditorWindow
    {
        enum Tab { Import, Library, Licences }
        static readonly string[] TabNames = { "Import", "Library", "Licences" };
        static readonly int[] Yaws = { 0, 90, 180, 270 };
        static readonly string[] YawNames = { "0°", "90°", "180°", "270°" };
        static readonly string[] Corners = { "Front left", "Front right", "Rear left", "Rear right" };
        static readonly string[] Fuels = { "Petrol", "Diesel", "Hybrid", "Electric" };

        Tab tab;
        ImportJob job;
        AssetRecord lastApproved;
        Vector2 scroll;
        Editor preview;
        Object previewTarget;
        string status = "";

        [MenuItem("Tools/Car Sales/Asset Studio", priority = 29)]
        public static void Open() => GetWindow<AssetStudioWindow>("Asset Studio").minSize = new Vector2(760f, 560f);

        /// <summary>Opens the Studio with a model that's already in the project (used by the Drop-folder watcher).</summary>
        public static void OpenWith(string modelPath)
        {
            var w = GetWindow<AssetStudioWindow>("Asset Studio");
            w.tab = Tab.Import;
            w.StartJob(modelPath, AssetCategory.Vehicle);
        }

        void OnDisable() { if (preview) DestroyImmediate(preview); }

        void OnGUI()
        {
            tab = (Tab)GUILayout.Toolbar((int)tab, TabNames, GUILayout.Height(26));
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, UnityEditor.MessageType.None);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            switch (tab)
            {
                case Tab.Import: DrawImport(); break;
                case Tab.Library: DrawLibrary(); break;
                default: DrawLicences(); break;
            }
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ IMPORT
        void DrawImport()
        {
            Section("1. Choose a model");
            EditorGUILayout.HelpBox("Supported: FBX, OBJ, DAE, BLEND (needs Blender installed), GLB/glTF (needs the glTFast package). " +
                                    "Download from Sketchfab, KitBash3D, Fab, CGTrader... yourself, then import the file here, drop it on this window, " +
                                    "or put it in " + StudioPaths.DropFolder + ". Asset Store models are already in the project: pick them below.", UnityEditor.MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Import model file...", GUILayout.Height(28))) PickFile();
                if (GUILayout.Button("Install glTFast (for GLB/glTF)", GUILayout.Height(28), GUILayout.Width(220)))
                {
                    Client.Add("com.unity.cloud.gltfast");
                    status = "Installing glTFast... (Package Manager). Re-import your .glb/.gltf files afterwards.";
                }
            }
            DropArea();
            var picked = EditorGUILayout.ObjectField("Or a model in the project", null, typeof(GameObject), false) as GameObject;
            if (picked) StartJob(AssetDatabase.GetAssetPath(picked), job?.category ?? AssetCategory.Vehicle);
            DrawDropFolder();
            if (job == null) return;

            Section("2. What is it?");
            EditorGUI.BeginChangeCheck();
            job.category = (AssetCategory)EditorGUILayout.EnumPopup("Category", job.category);
            if (EditorGUI.EndChangeCheck()) { AssetPipeline.Configure(job.modelPath, job.category); AssetPipeline.Analyse(job, true); }
            job.displayName = EditorGUILayout.TextField("Name", job.displayName);
            EditorGUILayout.LabelField("Model file", job.modelPath);

            Section("3. Licence (required for every asset)");
            var l = job.licence;
            l.source = EditorGUILayout.TextField("Source (website/store)", l.source);
            l.sourceUrl = EditorGUILayout.TextField("Source URL", l.sourceUrl);
            l.author = EditorGUILayout.TextField("Author", l.author);
            l.licence = (LicenceType)EditorGUILayout.EnumPopup("Licence", l.licence);
            if (l.licence == LicenceType.RoyaltyFreePurchased || l.licence == LicenceType.AssetStoreStandard)
                l.purchaseRef = EditorGUILayout.TextField("Order / invoice no.", l.purchaseRef);
            l.attributionText = EditorGUILayout.TextField(new GUIContent("Attribution (optional)", "Exact credit text if the author asks for one."), l.attributionText);
            if (string.IsNullOrEmpty(l.dateAcquired)) l.dateAcquired = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var verdict = LicencePolicy.Check(l);
            EditorGUILayout.HelpBox($"{verdict.Status}: {verdict.Reason}", verdict.BlocksRelease ? UnityEditor.MessageType.Warning : UnityEditor.MessageType.Info);

            Section("4. Size and orientation");
            EditorGUILayout.LabelField("Measured", $"{job.stats.Triangles:N0} triangles, {job.stats.Renderers} meshes, {job.stats.Materials} materials" +
                                                   (job.stats.SkinnedMeshes > 0 ? $", {job.stats.SkinnedMeshes} skinned" : ""));
            if (!string.IsNullOrEmpty(job.suggestion)) EditorGUILayout.HelpBox("Auto: " + job.suggestion, UnityEditor.MessageType.None);
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                job.scale = Mathf.Max(0.0001f, EditorGUILayout.FloatField("Scale", job.scale));
                if (GUILayout.Button("Auto", GUILayout.Width(60))) { AssetPipeline.Analyse(job, true); GUI.changed = false; }
            }
            int yawIndex = Mathf.Max(0, Array.IndexOf(Yaws, job.yaw));
            job.yaw = Yaws[EditorGUILayout.Popup(new GUIContent("Turn (Y)", "Cars must face +Z (forward)."), yawIndex, YawNames)];
            job.zUpFix = EditorGUILayout.Toggle(new GUIContent("Model is Z-up", "Rotate -90° around X (Blender / 3ds Max exports)."), job.zUpFix);
            if (EditorGUI.EndChangeCheck()) AssetPipeline.Analyse(job, false);
            EditorGUILayout.LabelField("Final size", $"width {job.finalSize.x:0.00} m  ×  height {job.finalSize.y:0.00} m  ×  length {job.finalSize.z:0.00} m");

            if (job.category == AssetCategory.Vehicle) DrawVehicle();

            Section("Preview (source model)");
            DrawPreview(job.Model, 260f);

            Section("6. Checks");
            if (job.issues.Count == 0) EditorGUILayout.HelpBox("All checks passed.", UnityEditor.MessageType.Info);
            foreach (var i in job.issues)
                EditorGUILayout.HelpBox(i.Message, i.Level == IssueLevel.Error ? UnityEditor.MessageType.Error : i.Level == IssueLevel.Warning ? UnityEditor.MessageType.Warning : UnityEditor.MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fix materials for URP", GUILayout.Height(30)))
                {
                    int n = AssetPipeline.FixMaterials(job);
                    AssetPipeline.Analyse(job, false);
                    status = n > 0 ? $"Converted {n} material(s) to URP Lit." : "Materials are fine (or textures were filled in).";
                }
                using (new EditorGUI.DisabledScope(ImportValidation.HasErrors(job.issues)))
                    if (GUILayout.Button("Approve - create game prefab", GUILayout.Height(30))) Approve();
            }
            if (lastApproved != null && lastApproved.prefab)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Show prefab")) EditorGUIUtility.PingObject(lastApproved.prefab);
                    if (lastApproved.carDefinition && GUILayout.Button("Drive on test track")) VehicleStudioWindow.DriveOnTestTrack(lastApproved.carDefinition);
                }
                Section("Result");
                DrawPreview(lastApproved.prefab, 220f);
            }
        }

        void DrawVehicle()
        {
            Section("5. Car setup");
            var options = new[] { "(none)" }.Concat(job.rendererPaths.Concat(ParentPaths())).Distinct().ToArray();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Wheels (tyre + rim of each corner)", EditorStyles.boldLabel);
                if (GUILayout.Button("Detect", GUILayout.Width(70))) AssetPipeline.Analyse(job, false, detectParts: true);
            }
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < 4; i++)
            {
                int current = Mathf.Max(0, Array.IndexOf(options, job.wheelPaths[i]));
                int sel = EditorGUILayout.Popup(Corners[i], current, options);
                job.wheelPaths[i] = sel == 0 ? null : options[sel];
            }
            if (!job.WheelsAssigned) EditorGUILayout.HelpBox("Pick the 4 wheel meshes (or their parent objects). Without them the car drives on invisible wheels.", UnityEditor.MessageType.None);

            EditorGUILayout.LabelField("Paint materials (recoloured per car)", EditorStyles.boldLabel);
            foreach (var m in job.materialNames)
            {
                bool on = job.paintMaterials.Contains(m);
                bool now = EditorGUILayout.ToggleLeft(m, on);
                if (now && !on) job.paintMaterials.Add(m);
                if (!now && on) job.paintMaterials.Remove(m);
            }

            EditorGUILayout.LabelField("Car data", EditorStyles.boldLabel);
            job.brand = EditorGUILayout.TextField("Brand (fictional)", job.brand);
            job.carCategory = (CarCategory)EditorGUILayout.EnumPopup("Category", job.carCategory);
            job.year = EditorGUILayout.IntSlider("Year", job.year, 1960, 2030);
            job.basePrice = EditorGUILayout.IntField("Base price", job.basePrice);
            job.fuelType = Fuels[Mathf.Max(0, EditorGUILayout.Popup("Fuel", Mathf.Max(0, Array.IndexOf(Fuels, job.fuelType)), Fuels))];
            job.transmission = EditorGUILayout.TextField("Transmission", job.transmission);
            EditorGUILayout.HelpBox("Real brand names and logos are trademarks: use a fictional brand and remove badges from the model.", UnityEditor.MessageType.None);
            if (EditorGUI.EndChangeCheck()) AssetPipeline.Analyse(job, false);
        }

        System.Collections.Generic.IEnumerable<string> ParentPaths() =>
            job.rendererPaths.Where(p => p.Contains('/')).Select(p => p.Substring(0, p.LastIndexOf('/')));

        void DropArea()
        {
            var rect = GUILayoutUtility.GetRect(0f, 54f, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "Drop a model file (or a model from the Project window) here", EditorStyles.helpBox);
            var e = Event.current;
            if (!rect.Contains(e.mousePosition) || (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)) return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type != EventType.DragPerform) return;
            DragAndDrop.AcceptDrag();
            var go = DragAndDrop.objectReferences.OfType<GameObject>().FirstOrDefault();
            if (go && AssetDatabase.Contains(go)) { StartJob(AssetDatabase.GetAssetPath(go), job?.category ?? AssetCategory.Vehicle); return; }
            var file = DragAndDrop.paths.FirstOrDefault(StudioNames.IsModelFile);
            if (file != null) IngestExternal(Path.GetFullPath(file));
            e.Use();
        }

        void DrawDropFolder()
        {
            if (!AssetDatabase.IsValidFolder(StudioPaths.DropFolder))
            {
                if (GUILayout.Button("Create Drop folder (" + StudioPaths.DropFolder + ")")) StudioPaths.Ensure(StudioPaths.DropFolder);
                return;
            }
            var files = Directory.GetFiles(StudioPaths.DropFolder, "*.*", SearchOption.AllDirectories)
                .Where(StudioNames.IsModelFile).Select(f => f.Replace('\\', '/')).ToList();
            if (files.Count == 0) return;
            EditorGUILayout.LabelField("Waiting in the Drop folder", EditorStyles.boldLabel);
            foreach (var f in files)
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(Path.GetFileName(f));
                    if (GUILayout.Button("Set up", GUILayout.Width(80))) IngestExternal(Path.GetFullPath(f), deleteAfter: true);
                }
        }

        void PickFile()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Import 3D model", "", new[] { "3D models", "fbx,obj,glb,gltf,dae,blend,3ds", "All files", "*" });
            if (!string.IsNullOrEmpty(path)) IngestExternal(path);
        }

        void IngestExternal(string fullPath, bool deleteAfter = false)
        {
            try
            {
                var cat = job?.category ?? AssetCategory.Vehicle;
                string assetPath = AssetPipeline.Ingest(fullPath, cat);
                string full = fullPath.Replace('\\', '/');
                string dataPath = Application.dataPath.Replace('\\', '/');
                if (deleteAfter && full.StartsWith(dataPath))
                {
                    string projectPath = "Assets" + full.Substring(dataPath.Length);
                    if (projectPath.StartsWith(StudioPaths.DropFolder + "/")) AssetDatabase.DeleteAsset(projectPath);
                }
                StartJob(assetPath, cat);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                status = "Import failed: " + e.Message;
            }
        }

        void StartJob(string assetPath, AssetCategory category)
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(assetPath))
            {
                string ext = Path.GetExtension(assetPath).ToLowerInvariant();
                status = ext == ".glb" || ext == ".gltf"
                    ? "Unity can't read GLB/glTF yet: click 'Install glTFast', then select the file again."
                    : ext == ".blend" ? "Unity needs Blender installed to read .blend files (or export FBX from Blender)."
                    : "That file didn't import as a 3D model.";
                return;
            }
            job = new ImportJob { modelPath = assetPath, category = category, displayName = Path.GetFileNameWithoutExtension(assetPath) };
            var existing = StudioPaths.Registry().records.FirstOrDefault(r => r.sourcePath == assetPath && !string.IsNullOrEmpty(r.jobJson));
            if (existing != null)
            {
                JsonUtility.FromJsonOverwrite(existing.jobJson, job);
                AssetPipeline.Analyse(job, false);
                status = $"Re-opened '{existing.displayName}' (version {existing.version}).";
            }
            else
            {
                AssetPipeline.Analyse(job, true);
                status = "";
            }
            lastApproved = null;
        }

        void Approve()
        {
            try
            {
                lastApproved = AssetPipeline.Approve(job);
                status = $"Created {AssetDatabase.GetAssetPath(lastApproved.prefab)} (version {lastApproved.version})" +
                         (lastApproved.carDefinition ? $" and car '{lastApproved.carDefinition.displayName}' - it can now appear for sale, and you can drive it on the test track." : ".");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                status = "Approve failed: " + e.Message;
            }
        }

        void DrawPreview(Object target, float height)
        {
            if (!target) return;
            if (previewTarget != target)
            {
                if (preview) DestroyImmediate(preview);
                preview = Editor.CreateEditor(target);
                previewTarget = target;
            }
            var rect = GUILayoutUtility.GetRect(200f, height, GUILayout.ExpandWidth(true));
            if (preview && preview.HasPreviewGUI()) preview.OnInteractivePreviewGUI(rect, EditorStyles.helpBox);
        }

        // ------------------------------------------------------------------ LIBRARY
        void DrawLibrary()
        {
            var reg = StudioPaths.Registry();
            if (reg.records.Count == 0) { EditorGUILayout.HelpBox("Nothing imported yet.", UnityEditor.MessageType.Info); return; }
            foreach (var r in reg.records.ToList())
            {
                if (r == null) continue;
                var v = LicencePolicy.Check(r.licence);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"{r.displayName}  ({r.category}, v{r.version})", EditorStyles.boldLabel, GUILayout.Width(280));
                    EditorGUILayout.LabelField(LicencePolicy.DisplayName(r.licence.licence) + (v.BlocksRelease ? "  ⚠" : ""), GUILayout.Width(190));
                    if (GUILayout.Button("Show", GUILayout.Width(50)) && r.prefab) EditorGUIUtility.PingObject(r.prefab);
                    if (GUILayout.Button("Edit", GUILayout.Width(50))) { tab = Tab.Import; StartJob(r.sourcePath, r.category); }
                    using (new EditorGUI.DisabledScope(AssetPipeline.LatestHistory(r) == null))
                        if (GUILayout.Button("Roll back", GUILayout.Width(70)) && AssetPipeline.Rollback(r)) status = $"{r.displayName} is back to version {r.version}.";
                    if (GUILayout.Button("Remove", GUILayout.Width(65)) &&
                        EditorUtility.DisplayDialog("Asset Studio", $"Remove {r.displayName}'s prefab, car data and record?\n(The source files in Art/ stay.)", "Remove", "Cancel"))
                        AssetPipeline.Remove(r);
                }
            }
        }

        // ------------------------------------------------------------------ LICENCES
        void DrawLicences()
        {
            var reg = StudioPaths.Registry();
            var groups = reg.records.Where(r => r != null).GroupBy(r => LicencePolicy.Check(r.licence).Status);
            foreach (var g in groups) EditorGUILayout.LabelField(g.Key.ToString(), g.Count().ToString());
            var blockers = reg.ReleaseBlockers().ToList();
            EditorGUILayout.Space();
            if (blockers.Count == 0) EditorGUILayout.HelpBox("Every imported asset may be used in a release build.", UnityEditor.MessageType.Info);
            else
            {
                EditorGUILayout.HelpBox($"{blockers.Count} asset(s) block a release build (development builds still work):", UnityEditor.MessageType.Warning);
                foreach (var r in blockers) EditorGUILayout.LabelField(" - " + r.displayName, LicencePolicy.Check(r.licence).Reason);
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Write CREDITS.txt", GUILayout.Height(28)))
            {
                File.WriteAllText(StudioPaths.CreditsPath, reg.CreditsText());
                AssetDatabase.ImportAsset(StudioPaths.CreditsPath);
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(StudioPaths.CreditsPath));
                status = "Wrote " + StudioPaths.CreditsPath + " (also written next to every build automatically).";
            }
            EditorGUILayout.HelpBox("Not legal advice: always read the licence of each asset. If your GitHub repository is public, " +
                                    "don't commit paid assets (Asset Store, KitBash3D...): that would redistribute them.", UnityEditor.MessageType.None);
        }

        static void Section(string title)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }
}
