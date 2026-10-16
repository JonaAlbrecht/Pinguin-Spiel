using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SFE.Game.EditorTools
{
    /// <summary>
    /// Legt die Prototyp-Szene per Code an (docs/04 Abschnitt 7: Szenen aus Code statt von Hand) und baut
    /// WebGL/Windows im Batchmode:
    /// <c>Unity -batchmode -quit -projectPath unity/PinguinKolonie -executeMethod SFE.Game.EditorTools.PrototypeSetup.BuildWebGL</c>
    /// </summary>
    public static class PrototypeSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string UnlitPath = "Assets/_Project/Resources/Materials/Unlit.mat";

        [MenuItem("Pinguin-Spiel/Prototyp-Szene erstellen", priority = 1)]
        public static void CreateScene()
        {
            EnsureUnlitMaterial();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("GameRoot").AddComponent<GameRoot>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("Prototyp-Szene angelegt: " + ScenePath);
        }

        [MenuItem("Pinguin-Spiel/Prototyp starten", priority = 2)]
        public static void Play()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            else EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void BuildWebGL() => Build(BuildTarget.WebGL, "Builds/WebGL");

        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/VomHuddleZurSiedlung.exe");

        private static void Build(BuildTarget target, string path)
        {
            if (!File.Exists(ScenePath)) CreateScene();
            EnsureUnlitMaterial();
            PlayerSettings.productName = "Vom Huddle zur Siedlung";
            PlayerSettings.companyName = "Siemens ST@ND";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            });
            Debug.Log($"Build {target}: {report.summary.result} ({report.summary.totalSize / (1024 * 1024)} MB) → {path}");
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        /// <summary>Material-Asset mit „Sprites/Default“, damit der unbeleuchtete Shader im Build enthalten ist.</summary>
        private static void EnsureUnlitMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(UnlitPath) != null) return;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) { Debug.LogWarning("Kein Unlit-Shader gefunden."); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(UnlitPath));
            AssetDatabase.CreateAsset(new Material(shader), UnlitPath);
            AssetDatabase.SaveAssets();
        }
    }
}
