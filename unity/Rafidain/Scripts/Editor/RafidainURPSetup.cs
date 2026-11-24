using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rafidain.EditorTools
{
    /// <summary>
    /// Turns a Built-In project into a URP project: creates two URP quality profiles
    /// (High for PC / strong phones, Mobile for normal phones) and assigns them to all quality levels.
    /// Runs automatically from "Build Zakho Test Arena" if URP is not set up yet.
    /// </summary>
    public static class RafidainURPSetup
    {
        const string Folder = "Assets/Rafidain/Settings";

        [MenuItem("Rafidain/Setup URP (Graphics)")]
        public static void SetupFromMenu()
        {
            EnsureURP(force: true);
            EditorUtility.DisplayDialog("Rafidain", "URP graphics are set up (High + Mobile profiles).", "OK");
        }

        public static void EnsureURP(bool force = false)
        {
            if (!force && GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset) return;

            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Rafidain", "Settings");

            var high = CreatePipeline("Rafidain_High", highQuality: true);
            var mobile = CreatePipeline("Rafidain_Mobile", highQuality: false);

            GraphicsSettings.defaultRenderPipeline = high;

            int current = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                string n = names[i].ToLowerInvariant();
                bool isHigh = n.Contains("high") || n.Contains("ultra") || n.Contains("pc");
                QualitySettings.renderPipeline = isHigh ? high : mobile;
            }
            QualitySettings.SetQualityLevel(current, false);

            // Linear colour space looks far more realistic than Gamma
            PlayerSettings.colorSpace = ColorSpace.Linear;

            AssetDatabase.SaveAssets();
            Debug.Log("[Rafidain] URP set up: High + Mobile profiles assigned.");
        }

        /// <summary>Longer shadows and better cascades for big open regions.</summary>
        public static void TuneForOpenWorld()
        {
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                var urp = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                if (urp == null) continue;
                string n = names[i].ToLowerInvariant();
                bool isHigh = n.Contains("high") || n.Contains("ultra") || n.Contains("pc");
                urp.shadowDistance = isHigh ? 160f : 60f;
                urp.shadowCascadeCount = isHigh ? 4 : 2;
                urp.supportsHDR = true;
                EditorUtility.SetDirty(urp);
            }
            var def = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (def != null)
            {
                def.shadowDistance = Mathf.Max(def.shadowDistance, 160f);
                EditorUtility.SetDirty(def);
            }
        }

        static UniversalRenderPipelineAsset CreatePipeline(string name, bool highQuality)
        {
            string assetPath = $"{Folder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (existing != null) return existing;

            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, $"{Folder}/{name}_Renderer.asset");

            var asset = UniversalRenderPipelineAsset.Create(renderer);
            asset.supportsHDR = true;
            asset.shadowDistance = highQuality ? 90f : 40f;
            asset.shadowCascadeCount = highQuality ? 4 : 1;
            asset.msaaSampleCount = highQuality ? 4 : 1;
            asset.renderScale = highQuality ? 1f : 0.85f;
            asset.supportsCameraDepthTexture = true;
            asset.supportsCameraOpaqueTexture = highQuality;

            AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
            EditorUtility.SetDirty(renderer);
            return asset;
        }
    }
}
