using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Bildqualität, beim Start per Code gesetzt (Spieltest 17.10.2026: „niedrige Render-Qualität, hohe Pixelierung“).
    /// Die Qualitätsstufen in ProjectSettings legt Unity selbst an: im Editor „Ultra“ mit nur 2× Kantenglättung,
    /// im WebGL-Build „High“ ganz ohne – und nur 2 bis 4 Lichter pro Pixel bei bis zu 13 Laternen.
    /// Hier stehen die Werte, die dieses Spiel braucht, unabhängig von der gewählten Stufe.
    /// Gilt für die eingebaute Render-Pipeline; unter URP regelt das Pipeline-Asset dasselbe (dort wirkungslos, aber harmlos).
    /// </summary>
    public static class RenderQuality
    {
        private static Camera _cam;
        private static Light _sun;

        public static void Apply(Camera cam, Light sun)
        {
            _cam = cam;
            _sun = sun;
            Reapply();
        }

        /// <summary>
        /// Stufe aus <see cref="Settings.HighQuality"/>. „Sparsam“ ist für schwache Laptops: weniger Kantenglättung,
        /// Laternenlicht nur für die nächsten Lampen, einfache Schatten.
        /// </summary>
        public static void Reapply()
        {
            var cam = _cam;
            var sun = _sun;
            if (cam == null) return;
            bool web = Application.platform == RuntimePlatform.WebGLPlayer;
            bool high = Settings.HighQuality;
            // MSAA glättet die Kanten der Grundformen. WebGL 2 unterstützt meist höchstens 4×.
            QualitySettings.antiAliasing = !high ? 2 : web ? 4 : 8;
            // Jede Laterne bekommt echtes Licht pro Pixel statt grober Eckpunkt-Beleuchtung (Hub 3 + Siedlung bis 10).
            QualitySettings.pixelLightCount = high ? 16 : 4;
            QualitySettings.anisotropicFiltering = high ? AnisotropicFiltering.ForceEnable : AnisotropicFiltering.Enable;
            QualitySettings.shadows = high ? ShadowQuality.All : ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = !high ? ShadowResolution.Medium : web ? ShadowResolution.High : ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            // Die Minigame-Kamera steht bis zu ~70 m vor dem Feld; kürzer abgeschnitten fehlen dort Schatten,
            // länger verschmiert die Schattenkarte über leere Fläche.
            QualitySettings.shadowDistance = 90f;
            QualitySettings.shadowCascades = high ? 4 : 2;
            QualitySettings.globalTextureMipmapLimit = 0;

            cam.allowMSAA = true;
            if (sun != null)
            {
                sun.shadowResolution = high ? UnityEngine.Rendering.LightShadowResolution.VeryHigh : UnityEngine.Rendering.LightShadowResolution.Medium;
                sun.shadowBias = 0.03f;
                sun.shadowNormalBias = 0.3f;     // keine Streifen („Shadow Acne“) auf den runden Formen
            }
        }
    }
}
