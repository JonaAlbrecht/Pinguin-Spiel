using System.Collections.Generic;
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
    /// Pro Bild (<see cref="Tick"/>): Dunst nach Kameraabstand, Schatten der nächsten Laternen, Wellen (ADR-28).
    /// </summary>
    public static class RenderLook
    {
        private static Material _sky;
        private static Volume _volume;
        private static Bloom _bloom;
        private static Cubemap _reflection;
        private static readonly List<Light> Lanterns = new List<Light>();
        private static readonly List<(Material Mat, ArtStyle.Surfaces.Spec Spec)> Flowing = new List<(Material, ArtStyle.Surfaces.Spec)>();

        /// <summary>Für Spiel und Stilprobe: Kamera, Himmel, Dunst, Nachbearbeitung, Spiegelungen.</summary>
        public static void Apply(Camera cam, Light sun)
        {
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
            RenderSettings.fogColor = Palette.Haze;
            if (_volume == null)
            {
                _volume = CreateVolume();
                // Im Spiel läuft Tick jedes Bild; die Stilprobe (Editor ohne Play-Modus) ruft Tick selbst auf.
                if (Application.isPlaying) _volume.gameObject.AddComponent<LookDriver>().Cam = cam;
            }
            SetupCamera(cam);
            Tick(cam);
        }

        /// <summary>
        /// Kamera und Qualitätsstufe. Die Nachbearbeitung bleibt in beiden Stufen an: Ohne sie fehlt das Tonemapping,
        /// und Sonnenschnee (über 1,0 im HDR-Puffer) frisst zu reinem Weiß aus (Grafik-Audit B-03). „Sparsam“ spart
        /// stattdessen Bloom, Umgebungsverdeckung, Laternen-Schatten und nimmt FXAA statt SMAA.
        /// </summary>
        public static void SetupCamera(Camera cam)
        {
            bool high = Settings.HighQuality;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            // SMAA zusätzlich zu MSAA: glättet auch Kanten in Texturen und Glanzlichtern („Pixeligkeit“, Spieltest 17.10.)
            data.antialiasing = high ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.FastApproximateAntialiasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.renderShadows = true;
            if (_bloom != null) _bloom.active = high;
            SetAmbientOcclusion(high);
        }

        /// <summary>Pro Bild: Dunst nach Kameraabstand, Schatten der Laternen nahe am Blickpunkt, Wellen auf dem Wasser.</summary>
        public static void Tick(Camera cam)
        {
            if (cam == null) return;
            var focus = Focus(cam, out float distance);
            RenderSettings.fogDensity = ArtStyle.Atmosphere.FogAtFocus / Mathf.Max(distance, ArtStyle.Atmosphere.FogMinDistance);
            UpdateLanternShadows(focus, Settings.HighQuality ? ArtStyle.Shading.LanternShadows : 0);
            float t = Time.time;
            for (int i = Flowing.Count - 1; i >= 0; i--)
            {
                var (m, s) = Flowing[i];
                if (m == null) { Flowing.RemoveAt(i); continue; }
                // Versatz in Kacheln: Geschwindigkeit (m/s) × Kacheln je Meter. Die feine Lage läuft quer und schneller.
                float tiles = s.Flow * s.TilesPerMeter * t;
                m.mainTextureOffset = new Vector2(0.8f, 0.6f) * tiles;
                m.SetTextureOffset("_DetailAlbedoMap", new Vector2(-0.5f, 0.87f) * (tiles * s.DetailTiling * 1.3f));
            }
        }

        /// <summary>Material mit bewegter Oberfläche (Wasser) – Prims meldet es beim Anlegen.</summary>
        public static void AddFlowing(Material m, ArtStyle.Surfaces.Spec spec) => Flowing.Add((m, spec));

        /// <summary>Laternenlicht – Prims meldet es beim Bau; Schatten verteilt <see cref="Tick"/>.</summary>
        public static void AddLantern(Light l) => Lanterns.Add(l);

        /// <summary>Bodenpunkt in Blickrichtung (y = 0) und Abstand der Kamera dorthin.</summary>
        private static Vector3 Focus(Camera cam, out float distance)
        {
            var t = cam.transform;
            float down = -t.forward.y;
            distance = down > 0.05f ? t.position.y / down : 24f;
            distance = Mathf.Max(distance, 0f);
            return t.position + t.forward * distance;
        }

        private static readonly List<Light> Candidates = new List<Light>();

        private static void UpdateLanternShadows(Vector3 focus, int count)
        {
            Lanterns.RemoveAll(l => l == null);
            Candidates.Clear();
            foreach (var l in Lanterns)
            {
                if (l.isActiveAndEnabled) Candidates.Add(l);
                else l.shadows = LightShadows.None;
            }
            Candidates.Sort((x, y) => (x.transform.position - focus).sqrMagnitude.CompareTo((y.transform.position - focus).sqrMagnitude));
            for (int i = 0; i < Candidates.Count; i++)
            {
                var want = i < count ? LightShadows.Soft : LightShadows.None;
                if (Candidates[i].shadows != want) Candidates[i].shadows = want;
            }
        }

        private static ScriptableRendererFeature _ao;
        private static bool _aoRestoreHooked;

        /// <summary>
        /// SSAO ist ein Renderer-Feature im Asset. Abgeschaltet wird über SetActive; im Editor würde das das Asset auf
        /// der Platte ändern, darum beim Beenden (auch Ende des Play-Modus) zurück auf an.
        /// </summary>
        private static void SetAmbientOcclusion(bool on)
        {
            if (_ao == null)
            {
                foreach (var f in Resources.FindObjectsOfTypeAll<ScriptableRendererFeature>())
                    if (f.GetType().Name == "ScreenSpaceAmbientOcclusion") { _ao = f; break; }
                if (_ao == null) return;
            }
            if (!_aoRestoreHooked)
            {
                _aoRestoreHooked = true;
                Application.quitting += () => { if (_ao != null) _ao.SetActive(true); };
            }
            if (_ao.isActive != on) _ao.SetActive(on);
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
            _bloom = p.Add<Bloom>(true);
            _bloom.threshold.Override(ArtStyle.Atmosphere.BloomThreshold);
            _bloom.intensity.Override(ArtStyle.Atmosphere.BloomIntensity);
            _bloom.scatter.Override(ArtStyle.Atmosphere.BloomScatter);
            _bloom.highQualityFiltering.Override(true);
            _bloom.active = Settings.HighQuality;
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
            if (!ok)
            {
                Debug.LogWarning("Himmels-Spiegelung konnte nicht gerendert werden – Wasser und Eis spiegeln dann den Standardhimmel.");
                Prims.Kill(cube);
                return null;
            }
            return cube;
        }

        /// <summary>Ruft <see cref="Tick"/> nach der Kamerabewegung des Bildes auf.</summary>
        private sealed class LookDriver : MonoBehaviour
        {
            public Camera Cam;

            private void LateUpdate() => Tick(Cam != null ? Cam : Camera.main);
        }
    }
}
