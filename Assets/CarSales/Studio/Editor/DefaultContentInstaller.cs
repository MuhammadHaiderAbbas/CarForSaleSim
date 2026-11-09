using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CarSales.EditorTools;
using UnityEditor;
using UnityEngine;

namespace CarSales.Studio.EditorTools
{
    /// <summary>
    /// Phase 14b: downloads the default content pack from Sketchfab (with your own API token) and sets every model up
    /// with the normal Asset Studio steps - scale, materials, wheels and paint, licence record - then puts it where it
    /// belongs: cars become the catalogue's 48 cars, buildings, trees, lamps, benches, equipment and construction machines
    /// fill the town's slots by style. Models that need a human decision are left in Art/ and listed as "needs you".
    /// </summary>
    public static class DefaultContentInstaller
    {
        public enum State { Waiting, Downloading, SettingUp, Done, NeedsYou, Failed }

        public sealed class Status
        {
            public State State;
            public string Message = "";
            public float Progress;
        }

        const string TokenKey = "CarSales.SketchfabToken";   // shared with Asset Studio > Online
        static readonly Dictionary<string, Status> status = new Dictionary<string, Status>();
        static readonly Queue<ContentItem> queue = new Queue<ContentItem>();
        static ContentItem current;
        public static event Action Changed;

        public static bool Running => current != null;
        public static int Queued => queue.Count;
        public static string Token { get => EditorPrefs.GetString(TokenKey, ""); set => EditorPrefs.SetString(TokenKey, (value ?? "").Trim()); }

        /// <summary>True when the glTF importer (glTFast) is installed - Sketchfab downloads are glTF.</summary>
        public static bool GltfImporterInstalled =>
            AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name.StartsWith("glTFast", StringComparison.OrdinalIgnoreCase));

        public static AssetRecord Record(ContentItem item)
        {
            var reg = AssetRegistry.Load();
            var r = reg ? reg.Find(item.AssetId) : null;
            return r != null && r.prefab ? r : null;
        }

        public static bool IsInstalled(ContentItem item) => Record(item) != null;

        public static Status StatusOf(ContentItem item)
        {
            if (status.TryGetValue(item.AssetId, out var s)) return s;
            return IsInstalled(item) ? new Status { State = State.Done, Message = "Set up" } : null;
        }

        public static void Enqueue(IEnumerable<ContentItem> items)
        {
            foreach (var i in items)
            {
                if (IsInstalled(i) || queue.Contains(i) || i == current) continue;
                queue.Enqueue(i);
                status[i.AssetId] = new Status { State = State.Waiting, Message = "Waiting" };
            }
            Changed?.Invoke();
            if (current == null) Next();
        }

        public static void Stop()
        {
            foreach (var i in queue) status.Remove(i.AssetId);
            queue.Clear();
            Changed?.Invoke();
        }

        static void Next()
        {
            current = null;
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                if (IsInstalled(item)) { Set(item, State.Done, "Set up"); continue; }
                current = item;
                Download(item);
                return;
            }
            AssetDatabase.SaveAssets();
            Changed?.Invoke();
        }

        static void Set(ContentItem item, State state, string message, float progress = 0f)
        {
            status[item.AssetId] = new Status { State = state, Message = message, Progress = progress };
            Changed?.Invoke();
        }

        static void Fail(ContentItem item, string message)
        {
            Set(item, State.Failed, message);
            Debug.LogWarning($"[CarSales] Default content '{item.Title}': {message}");
            EditorApplication.delayCall += Next;
        }

        // ------------------------------------------------------------------ DOWNLOAD
        static void Download(ContentItem item)
        {
            if (string.IsNullOrEmpty(Token)) { Stop(); Fail(item, "No Sketchfab API token (sketchfab.com > Settings > Password & API)."); return; }
            if (!GltfImporterInstalled) { Stop(); Fail(item, "Install glTFast first (button at the top of the window)."); return; }
            var headers = new Dictionary<string, string> { { "Authorization", SketchfabApi.AuthHeader(Token) } };
            Set(item, State.Downloading, "Asking Sketchfab...");
            EditorHttp.GetText(SketchfabApi.DownloadUrl(item.Uid), headers, (json, error) =>
            {
                if (error != null) { Fail(item, "Sketchfab refused the download: " + error); return; }
                string url;
                bool zip;
                try { url = SketchfabApi.ParseDownload(json, out zip); }
                catch (FormatException e) { Fail(item, "Unexpected answer from Sketchfab: " + e.Message); return; }
                if (string.IsNullOrEmpty(url)) { Fail(item, "Sketchfab offered no glTF download."); return; }

                string folder = StudioPaths.AssetFolder(item.Category, item.AssetId) + "/Source";
                string target = zip ? Path.Combine(Application.temporaryCachePath, item.AssetId + ".zip") : $"{folder}/{item.AssetId}.glb";
                // The file URL is pre-signed: no Authorization header.
                EditorHttp.Download(url, target, null, err =>
                {
                    if (err != null) { Fail(item, "Download failed: " + err); return; }
                    string modelPath = target;
                    try
                    {
                        if (zip)
                        {
                            if (Directory.Exists(folder)) Directory.Delete(folder, true);
                            Directory.CreateDirectory(folder);
                            ZipFile.ExtractToDirectory(target, folder);
                            File.Delete(target);
                            modelPath = Directory.GetFiles(folder, "*.gltf", SearchOption.AllDirectories).Concat(Directory.GetFiles(folder, "*.glb", SearchOption.AllDirectories))
                                                 .FirstOrDefault()?.Replace('\\', '/');
                        }
                        if (modelPath == null) { Fail(item, "The download contained no .gltf file."); return; }
                        Set(item, State.SettingUp, "Setting up...");
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        SetUp(item, modelPath);
                    }
                    catch (Exception e) { Debug.LogException(e); Fail(item, e.Message); return; }
                    EditorApplication.delayCall += Next;
                }, p => { if (status.TryGetValue(item.AssetId, out var s)) { s.Progress = p; s.Message = $"Downloading {p:P0}"; Changed?.Invoke(); } });
            });
        }

        // ------------------------------------------------------------------ SET UP
        static void SetUp(ContentItem item, string modelPath)
        {
            var job = new ImportJob
            {
                modelPath = modelPath,
                category = item.Category,
                displayName = item.AssetId,   // the record id; the real title goes in the credits below
                licence = new LicenceInfo
                {
                    source = "Sketchfab", sourceUrl = item.Url, author = item.AuthorName, authorUrl = item.AuthorUrl,
                    licence = item.LicenceType, licenceUrl = LicencePolicy.DefaultUrl(item.LicenceType),
                    attributionText = item.Credit, dateAcquired = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                },
            };
            var car = item.Kind == ContentKind.Car ? CarCatalogue.Find(item.CarId) : null;
            if (car != null)
            {
                job.brand = car.Brand;
                job.carCategory = (CarCategory)Enum.Parse(typeof(CarCategory), car.Category);
                job.year = car.Year;
                job.basePrice = car.Price;
                job.fuelType = car.Fuel;
                job.transmission = car.Gearbox;
            }
            AssetPipeline.Configure(modelPath, item.Category);
            if (!job.Model) { Set(item, State.Failed, "Unity could not import the model (is glTFast installed?)."); return; }
            AssetPipeline.Analyse(job, suggestScale: true, detectParts: true);
            AssetPipeline.FixMaterials(job);
            AssetPipeline.Analyse(job, suggestScale: false);
            if (ImportValidation.HasErrors(job.issues))
            {
                string why = job.issues.First(i => i.Level == IssueLevel.Error).Message;
                Set(item, State.NeedsYou, $"{why} Finish it in Asset Studio > Import: {modelPath}");
                return;
            }

            var rec = AssetPipeline.Approve(job);
            rec.displayName = item.Title;
            rec.style = item.Style;
            rec.notes = "Default content pack. " + item.Credit;
            EditorUtility.SetDirty(StudioPaths.Registry());

            string note = "Set up";
            if (car != null) note = LinkCar(item, rec, job);
            else AddToTown(item, rec);
            Set(item, State.Done, note);
        }

        /// <summary>The catalogue car keeps its own name, price and description; only its look comes from the model.</summary>
        static string LinkCar(ContentItem item, AssetRecord rec, ImportJob job)
        {
            if (rec.carDefinition)
            {
                var path = AssetDatabase.GetAssetPath(rec.carDefinition);
                foreach (var g in AssetDatabase.FindAssets("t:CarDatabase"))
                {
                    var db = AssetDatabase.LoadAssetAtPath<CarDatabase>(AssetDatabase.GUIDToAssetPath(g));
                    if (db && db.cars.Remove(rec.carDefinition)) EditorUtility.SetDirty(db);
                }
                rec.carDefinition = null;
                if (!string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
            }
            // Show it straight away in an existing build; Build Everything links it from now on.
            var generated = AssetDatabase.LoadAssetAtPath<CarDefinition>($"Assets/CarSales/Generated/Data/Cars/Car_{item.CarId}.asset");
            if (generated) { generated.displayPrefab = rec.prefab; EditorUtility.SetDirty(generated); }
            bool wheels = job.WheelsAssigned, paint = job.paintMaterials.Count > 0;
            return wheels && paint ? "Set up" : $"Set up ({(wheels ? "" : "wheels not found")}{(!wheels && !paint ? ", " : "")}{(paint ? "" : "paint not found")} - check in Asset Studio)";
        }

        static PlaceholderKind KindFor(ContentItem item)
        {
            if (item.Kind == ContentKind.Building) return PlaceholderKind.Building;
            if (item.Kind == ContentKind.Construction) return PlaceholderKind.Construction;
            switch (item.Style)
            {
                case "tree": return PlaceholderKind.Tree;
                case "streetlamp": return PlaceholderKind.StreetLamp;
                case "bench": return PlaceholderKind.Bench;
                default: return PlaceholderKind.Equipment;
            }
        }

        static void AddToTown(ContentItem item, AssetRecord rec)
        {
            var settings = PlaceholderReplacer.LoadSettings(true);
            var entry = settings.For(KindFor(item));
            if (!entry.assetIds.Contains(rec.id)) entry.assetIds.Add(rec.id);
            EditorUtility.SetDirty(settings);
        }

        // ------------------------------------------------------------------ SUMMARY
        public static (int done, int total) Count(IEnumerable<ContentItem> items)
        {
            var list = items.ToList();
            return (list.Count(IsInstalled), list.Count);
        }
    }
}
