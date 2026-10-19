using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SFE.Game.EditorTools
{
    /// <summary>
    /// Richtet die Universal Render Pipeline ein (ADR-27, „ultra realistisch“): Pipeline-Asset mit HDR, 4× MSAA,
    /// weichen 4096er-Schatten und Forward+ (alle Laternen mit Licht pro Pixel), Renderer mit Umgebungsverdeckung
    /// (SSAO) und Nachbearbeitung, linearer Farbraum, Himmelsmaterial in Resources (sonst fehlt der Shader im Build).
    /// Wiederholbar – vorhandene Assets werden nur angepasst. Batchmode:
    /// <c>Unity -batchmode -quit -projectPath unity/PinguinKolonie -executeMethod SFE.Game.EditorTools.RenderPipelineSetup.Apply</c>
    /// </summary>
    public static class RenderPipelineSetup
    {
        private const string Dir = "Assets/_Project/Settings/URP";
        private const string RendererPath = Dir + "/PinguinRenderer.asset";
        private const string AssetPath = Dir + "/PinguinURP.asset";
        private const string SkyPath = "Assets/_Project/Resources/Materials/Sky.mat";

        [MenuItem("Pinguin-Spiel/Render-Pipeline einrichten (URP)", priority = 30)]
        public static void Apply()
        {
            Directory.CreateDirectory(Dir);
            AssetDatabase.Refresh();

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }
            renderer.renderingMode = RenderingMode.ForwardPlus;
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            if (renderer.postProcessData == null) Debug.LogWarning("PostProcessData nicht gefunden – Nachbearbeitung bleibt aus.");
            AddAmbientOcclusion(renderer);
            EditorUtility.SetDirty(renderer);

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
            if (urp == null)
            {
                urp = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(urp, AssetPath);
            }
            urp.msaaSampleCount = 4;
            urp.supportsHDR = true;
            urp.renderScale = 1f;
            urp.shadowDistance = 90f;
            urp.shadowCascadeCount = 4;
            var so = new SerializedObject(urp);
            SetInt(so, "m_MainLightShadowmapResolution", 4096);
            SetInt(so, "m_AdditionalLightsRenderingMode", 1);          // pro Pixel
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 8);
            SetBool(so, "m_AdditionalLightShadowsSupported", true);
            SetInt(so, "m_AdditionalLightsShadowmapResolution", 2048);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetBool(so, "m_MainLightShadowsSupported", true);
            SetBool(so, "m_SupportsTerrainHoles", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);

            GraphicsSettings.defaultRenderPipeline = urp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(current, false);
            PlayerSettings.colorSpace = ColorSpace.Linear;

            if (AssetDatabase.LoadAssetAtPath<Material>(SkyPath) == null)
            {
                var shader = Shader.Find("Skybox/Procedural");
                if (shader != null) AssetDatabase.CreateAsset(new Material(shader), SkyPath);
                else Debug.LogWarning("Shader Skybox/Procedural fehlt – Himmel bleibt einfarbig.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Render-Pipeline eingerichtet: " + AssetPath);
        }

        /// <summary>Einrichten und gleich die Stilprobe rendern (ein Batchmode-Lauf für beides).</summary>
        public static void ApplyAndPreview()
        {
            Apply();
            StylePreview.Render();
        }

        /// <summary>
        /// SSAO ist in URP ein „Renderer Feature“, dessen Klasse nicht öffentlich ist – darum per Typname.
        /// Die Feature-Liste braucht dazu die Datei-IDs (m_RendererFeatureMap), sonst verwirft URP den Eintrag.
        /// </summary>
        private static void AddAmbientOcclusion(UniversalRendererData renderer)
        {
            var type = typeof(UniversalRendererData).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
            if (type == null) { Debug.LogWarning("SSAO-Klasse nicht gefunden – ohne Umgebungsverdeckung."); return; }
            foreach (var f in renderer.rendererFeatures) if (f != null && f.GetType() == type) return;
            var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
            feature.name = "AmbientOcclusion";
            AssetDatabase.AddObjectToAsset(feature, renderer);
            renderer.rendererFeatures.Add(feature);
            var so = new SerializedObject(renderer);
            var map = so.FindProperty("m_RendererFeatureMap");
            if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long id))
            {
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var fs = new SerializedObject(feature);
            var intensity = fs.FindProperty("m_Settings.Intensity");
            if (intensity != null) intensity.floatValue = ArtStyle.Atmosphere.AmbientOcclusion;
            var radius = fs.FindProperty("m_Settings.Radius");
            if (radius != null) radius.floatValue = 0.4f;
            fs.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(SerializedObject so, string name, int value)
        {
            var p = so.FindProperty(name);
            if (p == null) { Debug.LogWarning("URP-Einstellung nicht gefunden: " + name); return; }
            p.intValue = value;
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            var p = so.FindProperty(name);
            if (p == null) { Debug.LogWarning("URP-Einstellung nicht gefunden: " + name); return; }
            p.boolValue = value;
        }
    }
}
