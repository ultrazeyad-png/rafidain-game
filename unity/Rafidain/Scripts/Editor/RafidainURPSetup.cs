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
                HighQualityShadows(urp, isHigh);
                if (isHigh) EnsureSSAO(urp);
                EditorUtility.SetDirty(urp);
            }
            var def = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (def != null)
            {
                def.shadowDistance = Mathf.Max(def.shadowDistance, 160f);
                EditorUtility.SetDirty(def);
            }
        }

        // Sharper, softer shadows on the PC/high profile (serialized fields, safe across URP versions)
        static void HighQualityShadows(UniversalRenderPipelineAsset urp, bool isHigh)
        {
            var so = new SerializedObject(urp);
            void SetInt(string name, int v) { var p = so.FindProperty(name); if (p != null) p.intValue = v; }
            void SetBool(string name, bool v) { var p = so.FindProperty(name); if (p != null) p.boolValue = v; }
            SetInt("m_MainLightShadowmapResolution", isHigh ? 4096 : 1024);
            SetInt("m_AdditionalLightsShadowmapResolution", isHigh ? 2048 : 512);
            SetBool("m_SoftShadowsSupported", true);
            SetInt("m_SoftShadowQuality", isHigh ? 3 : 1);
            SetBool("m_AdditionalLightShadowsSupported", true);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Screen-space ambient occlusion: contact shadows in corners, under rocks and eaves
        static void EnsureSSAO(UniversalRenderPipelineAsset urp)
        {
            var listField = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = listField != null ? listField.GetValue(urp) as ScriptableRendererData[] : null;
            if (list == null) return;

            System.Type ssaoType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                ssaoType = asm.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
                if (ssaoType != null) break;
            }
            if (ssaoType == null) return;

            foreach (var data in list)
            {
                if (data == null) continue;
                bool has = false;
                foreach (var f in data.rendererFeatures) if (f != null && f.GetType() == ssaoType) has = true;
                if (has) continue;
                var feature = ScriptableObject.CreateInstance(ssaoType) as ScriptableRendererFeature;
                if (feature == null) continue;
                feature.name = "SSAO";
                AssetDatabase.AddObjectToAsset(feature, data);
                data.rendererFeatures.Add(feature);
                var so = new SerializedObject(data);
                var map = so.FindProperty("m_RendererFeatureMap");
                if (map != null)
                {
                    map.arraySize++;
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorUtility.SetDirty(data);
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
