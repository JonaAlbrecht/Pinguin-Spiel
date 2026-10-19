using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SFE.Game
{
    /// <summary>
    /// Realistischer Look (ADR-27, Wunsch von Jona 19.10.2026: „ultra realistisch“): Himmel, Licht aus dem Himmel,
    /// Spiegelungen, Dunst und Nachbearbeitung wie im Film (ACES-Tonemapping, Bloom, Farbkorrektur, Vignette).
    /// Umgebungsverdeckung (SSAO) und Kantenglättung stehen im Pipeline-Asset (Editor-Menü „Pinguin-Spiel →
    /// Render-Pipeline einrichten“). Alles per Code wie der Rest der Welt; Werte aus <see cref="ArtStyle.Atmosphere"/>.
    /// </summary>
    public static class RenderLook
    {
        private static Material _sky;
        private static Volume _volume;
        private static Cubemap _reflection;

        /// <summary>Für Spiel und Stilprobe: Kamera, Himmel, Dunst, Nachbearbeitung, Spiegelungen.</summary>
        public static void Apply(Camera cam, Light sun)
        {
            SetupCamera(cam);
            var sky = Sky;
            if (sky != null)
            {
                RenderSettings.skybox = sky;
                RenderSettings.sun = sun;
                cam.clearFlags = CameraClearFlags.Skybox;
                // Spiegelungen: Himmel sofort in eine Cubemap rendern. Eine Reflexionssonde rendert erst im nächsten
                // Bild, die Welt entsteht aber per Code ohne vorberechnete Umgebung (Stilprobe: zufällige Tönung).
                if (_reflection == null) _reflection = RenderSkyCubemap(cam);
                if (_reflection != null)
                {
                    RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                    RenderSettings.customReflectionTexture = _reflection;
                    RenderSettings.reflectionIntensity = 1f;
                }
            }
            // Himmelslicht dreifarbig (Himmel, Horizont, Boden) – gleich ab dem ersten Bild, ohne Hintergrundrechnung.
            // Der Boden ist hell: Schnee wirft viel Licht zurück, die Unterseiten bleiben nicht schwarz.
            float a = ArtStyle.Shading.AmbientIntensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.AmbientSky * a;
            RenderSettings.ambientEquatorColor = Palette.AmbientHorizon * a;
            RenderSettings.ambientGroundColor = Palette.AmbientGround * a;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = ArtStyle.Atmosphere.FogDensity;
            RenderSettings.fogColor = Palette.Haze;
            if (_volume == null) _volume = CreateVolume();
        }

        public static void SetupCamera(Camera cam)
        {
            cam.allowHDR = true;
            cam.allowMSAA = true;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = Settings.HighQuality;
            // SMAA zusätzlich zu MSAA: glättet auch Kanten in Texturen und Glanzlichtern („Pixeligkeit“, Spieltest 17.10.)
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
        }

        /// <summary>Prozeduraler Himmel: Material aus Resources (im Build enthalten), sonst – im Editor – direkt aus dem Shader.</summary>
        private static Material Sky
        {
            get
            {
                if (_sky != null) return _sky;
                var asset = Resources.Load<Material>("Materials/Sky");
                if (asset != null) _sky = new Material(asset);
                else
                {
                    var shader = Shader.Find("Skybox/Procedural");
                    if (shader == null) return null;
                    _sky = new Material(shader);
                }
                _sky.SetFloat("_SunSize", ArtStyle.Atmosphere.SunSize);
                _sky.SetFloat("_AtmosphereThickness", ArtStyle.Atmosphere.AtmosphereThickness);
                _sky.SetFloat("_Exposure", ArtStyle.Atmosphere.SkyExposure);
                _sky.SetColor("_SkyTint", Palette.SkyTint);
                _sky.SetColor("_GroundColor", Palette.SkyGround);
                return _sky;
            }
        }

        private static Volume CreateVolume()
        {
            var go = new GameObject("PostFx");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            p.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var bloom = p.Add<Bloom>(true);
            bloom.threshold.Override(ArtStyle.Atmosphere.BloomThreshold);
            bloom.intensity.Override(ArtStyle.Atmosphere.BloomIntensity);
            bloom.scatter.Override(ArtStyle.Atmosphere.BloomScatter);
            bloom.highQualityFiltering.Override(true);
            var color = p.Add<ColorAdjustments>(true);
            color.postExposure.Override(ArtStyle.Atmosphere.PostExposure);
            color.contrast.Override(ArtStyle.Atmosphere.Contrast);
            color.saturation.Override(ArtStyle.Atmosphere.Saturation);
            var vignette = p.Add<Vignette>(true);
            vignette.intensity.Override(ArtStyle.Atmosphere.Vignette);
            vignette.smoothness.Override(0.45f);
            volume.sharedProfile = p;
            return volume;
        }

        /// <summary>Spiegelungen des Himmels für Wasser, Eis und Augen: eine Kamera, die nur den Himmel sieht, einmal rundum.</summary>
        private static Cubemap RenderSkyCubemap(Camera like)
        {
            var go = new GameObject("SkyCapture");
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(like);
            cam.cullingMask = 0;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var cube = new Cubemap(128, TextureFormat.RGBAHalf, true) { name = "SkyReflection" };
            bool ok = cam.RenderToCubemap(cube);
            Prims.Kill(go);
            if (!ok) { Prims.Kill(cube); return null; }
            return cube;
        }
    }
}
