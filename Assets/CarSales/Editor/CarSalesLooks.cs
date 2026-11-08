using System.Linq;
using UnityEditor;
using UnityEngine;
#if CARSALES_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace CarSales.EditorTools
{
    /// <summary>
    /// Lighting pass (spec Phase 4): the newest sky imported with the Asset Studio (Poly Haven HDRI) becomes the
    /// scene sky, and URP post-processing (ACES tonemapping, bloom, colour grading, vignette, SMAA) is added to
    /// the game, menu and test-track cameras.
    /// </summary>
    public static partial class CarSalesSetupWizard
    {
        static void SetUpLooks(Camera cam)
        {
            ApplyImportedSky();
#if CARSALES_URP
            if (!cam) return;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var go = new GameObject("PostProcessing");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = PostProfile();
#endif
        }

        static void ApplyImportedSky()
        {
            var reg = AssetRegistry.Load();
            if (!reg) return;
            var sky = reg.records.Where(r => r != null && r.category == CarSales.Studio.AssetCategory.Sky && r.mainAsset is Material)
                                 .OrderByDescending(r => r.importedAtUtc).FirstOrDefault();
            if (sky == null) return;
            RenderSettings.skybox = (Material)sky.mainAsset;
            DynamicGI.UpdateEnvironment();
        }

#if CARSALES_URP
        static VolumeProfile PostProfile()
        {
            string path = $"{Gen}/Settings/PostProcessing.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing) return existing;
            EnsureFolder(Gen + "/Settings");
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);

            var tone = p.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var bloom = p.Add<Bloom>(true);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.65f);
            var color = p.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);   // ACES darkens slightly
            color.contrast.Override(8f);
            color.saturation.Override(6f);
            var vignette = p.Add<Vignette>(true);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.45f);
            foreach (var c in p.components) AssetDatabase.AddObjectToAsset(c, p);
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            return p;
        }
#endif
    }
}
