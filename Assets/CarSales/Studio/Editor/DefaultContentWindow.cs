using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace CarSales.Studio.EditorTools
{
    /// <summary>
    /// Phase 14b: Tools > Car Sales > Default Content. One button downloads and sets up the whole default pack (realistic
    /// buildings, 48 cars, trees, street lamps, benches, workshop equipment and construction machines) from Sketchfab.
    /// </summary>
    public class DefaultContentWindow : EditorWindow
    {
        Vector2 scroll;
        string filter = "";

        static readonly (string title, ContentKind kind, string[] styles)[] Groups =
        {
            ("Cars (8 per class)", ContentKind.Car, null),
            ("Buildings", ContentKind.Building, null),
            ("Trees, street lamps and benches", ContentKind.Prop, new[] { "tree", "streetlamp", "bench" }),
            ("Equipment (fuel pumps, lifts, racks)", ContentKind.Prop, new[] { "fuelpump", "carlift", "toolcart", "tyrerack", "shelf" }),
            ("Construction sites", ContentKind.Construction, null),
        };

        [MenuItem("Tools/Car Sales/Default Content (download models)", priority = 27)]
        public static void Open() => GetWindow<DefaultContentWindow>("Default Content").minSize = new Vector2(620f, 520f);

        void OnEnable() => DefaultContentInstaller.Changed += Repaint;
        void OnDisable() => DefaultContentInstaller.Changed -= Repaint;

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                $"{DefaultContent.All.Count} realistic models from Sketchfab, all checked: free to download and licensed CC Attribution or CC0, " +
                "so they may be used in a paid game (the credits are added for you). Cars appear under fictional names. " +
                "The whole pack is a few GB: you can stop and carry on later, finished models are kept.", UnityEditor.MessageType.Info);

            // Token and importer.
            using (new EditorGUILayout.HorizontalScope())
            {
                var token = EditorGUILayout.PasswordField("Sketchfab API token", DefaultContentInstaller.Token);
                if (token != DefaultContentInstaller.Token) DefaultContentInstaller.Token = token;
                if (GUILayout.Button("Get token", GUILayout.Width(80))) Application.OpenURL("https://sketchfab.com/settings/password");
            }
            if (!DefaultContentInstaller.GltfImporterInstalled)
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Sketchfab models are glTF: install the glTFast importer first.", EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button("Install glTFast", GUILayout.Width(120))) Client.Add("com.unity.cloud.gltfast");
                }

            var (done, total) = DefaultContentInstaller.Count(DefaultContent.All);
            EditorGUILayout.Space(4);
            var bar = GUILayoutUtility.GetRect(100f, 18f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(bar, total == 0 ? 0f : done / (float)total, $"{done} of {total} models set up");
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(DefaultContentInstaller.Running || string.IsNullOrEmpty(DefaultContentInstaller.Token)))
                    if (GUILayout.Button(done == 0 ? "Download & set up everything" : "Download & set up the rest", GUILayout.Height(30)))
                        DefaultContentInstaller.Enqueue(DefaultContent.All);
                using (new EditorGUI.DisabledScope(!DefaultContentInstaller.Running))
                    if (GUILayout.Button("Stop after this one", GUILayout.Height(30), GUILayout.Width(140))) DefaultContentInstaller.Stop();
                if (GUILayout.Button("Build Everything", GUILayout.Height(30), GUILayout.Width(130)))
                    EditorApplication.ExecuteMenuItem("Tools/Car Sales/2. Build Everything");
            }
            EditorGUILayout.LabelField("When it finishes, run Build Everything: buildings, trees, lamps, equipment and machines go into the town, cars into the showrooms.",
                EditorStyles.wordWrappedMiniLabel);

            filter = EditorGUILayout.TextField("Filter", filter);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var (title, kind, styles) in Groups)
            {
                var items = DefaultContent.OfKind(kind).Where(i => styles == null || styles.Contains(i.Style))
                    .Where(i => string.IsNullOrEmpty(filter) || i.Title.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0 || i.Style.Contains(filter.ToLowerInvariant()))
                    .ToList();
                if (items.Count == 0) continue;
                var (gd, gt) = DefaultContentInstaller.Count(items);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{title}  ({gd}/{gt})", EditorStyles.boldLabel);
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(DefaultContentInstaller.Token)))
                        if (gd < gt && GUILayout.Button("Download group", GUILayout.Width(120))) DefaultContentInstaller.Enqueue(items);
                }
                foreach (var i in items) Row(i);
                EditorGUILayout.Space(6);
            }
            EditorGUILayout.EndScrollView();
        }

        static void Row(ContentItem i)
        {
            var s = DefaultContentInstaller.StatusOf(i);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                string what = i.Kind == ContentKind.Car
                    ? $"{CarCatalogue.Find(i.CarId)?.Brand} {CarCatalogue.Find(i.CarId)?.Name}  ({i.Style})"
                    : i.Style;
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(what, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"\"{i.Title}\" by {i.AuthorName} - {i.Licence} - {i.Faces:N0} polygons", EditorStyles.miniLabel);
                    if (s != null && s.State != DefaultContentInstaller.State.Done || (s != null && s.Message != "Set up"))
                        EditorGUILayout.LabelField(s.Message, s.State == DefaultContentInstaller.State.Failed || s.State == DefaultContentInstaller.State.NeedsYou
                            ? new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = new Color(0.95f, 0.55f, 0.3f) } }
                            : EditorStyles.wordWrappedMiniLabel);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("View", GUILayout.Width(50))) Application.OpenURL(i.Url);
                bool installed = DefaultContentInstaller.IsInstalled(i);
                using (new EditorGUI.DisabledScope(installed || string.IsNullOrEmpty(DefaultContentInstaller.Token)))
                    if (GUILayout.Button(s?.State == DefaultContentInstaller.State.Failed ? "Retry" : installed ? "Done" : "Get", GUILayout.Width(60)))
                        DefaultContentInstaller.Enqueue(new[] { i });
            }
        }
    }
}
