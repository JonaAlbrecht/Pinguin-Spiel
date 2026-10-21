using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Die Stilregeln des Spiels an einer Stelle (docs/07_Stilregeln.md). Alles, was gebaut wird, liest seine
    /// Schattierung, seinen Detailgrad und seine Maße von hier – wer hier dreht, ändert jedes Asset zugleich.
    /// Farben stehen weiter in <see cref="Palette"/>. Die Werte sind <c>static readonly</c> statt <c>const</c>,
    /// damit Abfragen wie <c>if (Shading.Flat)</c> keine Warnung zu unerreichbarem Code auslösen.
    /// </summary>
    public static class ArtStyle
    {
        /// <summary>
        /// Wie Licht auf die Formen fällt. Seit 19.10.2026 realistisch (ADR-27, Wunsch von Jona: „ultra realistisch“):
        /// weiche Rundungen, physikalische Oberflächen, tief stehende Polarsonne, Himmelslicht aus dem Himmel selbst.
        /// Vorher: matte Facetten unter weicher Wintersonne (Diorama-Look, ADR-16).
        /// </summary>
        public static class Shading
        {
            /// <summary>
            /// Facetten (true) oder weiche Rundungen (false). Realistisch heißt weich – die Segmentzahlen in
            /// <see cref="Detail"/> sind dafür hoch genug, dass man keine Kanten sieht.
            /// </summary>
            public static readonly bool Flat = false;

            /// <summary>Standard-Glanz für alles ohne eigene Oberfläche (Federn, Holz, Stoff): leicht seidig.</summary>
            public static readonly float Smoothness = 0.32f;

            // Licht in physikalischen Verhältnissen (linearer Farbraum, HDR, ACES-Tonemapping in RenderLook):
            // Die Sonne ist deutlich heller als das Himmelslicht, die Schatten bleiben trotzdem bläulich aufgehellt.
            public static readonly Color SunColor = new Color(1f, 0.93f, 0.84f);
            public static readonly float SunIntensity = 2.1f;
            /// <summary>Polarsonne: tief und seitlich – lange Schatten, Streiflicht auf Schnee.</summary>
            public static readonly Vector3 SunAngles = new Vector3(32f, -40f, 0f);
            /// <summary>Kühles Himmelslicht, nur als Rückfall ohne Himmel (RenderLook nimmt sonst den Himmel selbst).</summary>
            public static readonly Color Ambient = new Color(0.46f, 0.50f, 0.57f);
            public static readonly float AmbientIntensity = 1.05f;
            /// <summary>Echte Schatten sind satt; aufgehellt werden sie nur vom Himmelslicht und der Umgebungsverdeckung.</summary>
            public static readonly float ShadowStrength = 1f;
            /// <summary>
            /// So viele Laternen nahe am Blickpunkt werfen Schatten (Stufe „Hoch“; „Sparsam“: keine). Jede Punktlicht-
            /// Schattenquelle kostet sechs Schattenbilder pro Bild, und der 2048er-Schattenatlas fasst nur wenige in
            /// guter Auflösung (Grafik-Audit B-02). Weiter entfernte Laternen leuchten weiter, nur ohne Schatten.
            /// </summary>
            public static readonly int LanternShadows = 2;
            /// <summary>
            /// Laternenlicht bei Größe 1 (wächst mit der Fläche, also Größe²) und Reichweite in Metern (wächst mit der Größe).
            /// Am Tag nur ein warmer Schein: Bei 2,2 war der Schnee unter einer Laterne heller als in der Sonne, Iglus daneben
            /// fraßen gelb-weiß aus, und in Kalles Siedlung (zehn Laternen dicht beieinander) verschwand die ganze Fläche (ADR-29).
            /// </summary>
            public static readonly float LanternIntensity = 0.6f, LanternRange = 2.2f;

            public static void ApplyTo(Material m) => Surfaces.ApplyTo(m, Surface.Default);

            public static void ApplyTo(Light sun)
            {
                sun.color = SunColor;
                sun.intensity = SunIntensity;
                sun.transform.rotation = Quaternion.Euler(SunAngles);
                sun.shadowStrength = ShadowStrength;
                // Rückfall ohne Himmel (Stilprobe alter Art, Tests): flaches Himmelslicht. RenderLook schaltet auf den Himmel um.
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Ambient;
            }
        }

        /// <summary>Oberflächenarten. Welche ein Teil hat, entscheidet der Bau (Prims) – die Werte stehen in <see cref="Surfaces"/>.</summary>
        public enum Surface { Default, Snow, Ice, Water, Glossy, Metal, Glow, Feather }

        /// <summary>
        /// Physikalische Oberflächen (URP Lit): Glanz, Metall, Struktur (prozedurale Normal-Map) und Leuchten.
        /// Werte nach realen Materialien: Neuschnee ist stumpf mit feiner Körnung, Eis glatt mit Schlieren,
        /// Wasser fast ein Spiegel mit Wellen, Laternenglas leuchtet über die Bloom-Schwelle hinaus.
        /// </summary>
        public static class Surfaces
        {
            public sealed class Spec
            {
                public float Smoothness;
                public float Metallic;
                /// <summary>Welche prozedurale Struktur (null = glatt) und wie stark.</summary>
                public SurfaceTextures.Kind? Normal;
                public float NormalStrength = 1f;
                /// <summary>Wiederholungen der Struktur je Meter (Struktur bleibt gleich groß, egal wie groß das Teil ist).</summary>
                public float TilesPerMeter = 1f;
                /// <summary>Leuchtkraft relativ zur Grundfarbe (HDR; blüht im Bloom erst über <see cref="Atmosphere.BloomThreshold"/>).</summary>
                public float Emission;
                /// <summary>
                /// Bewegte Oberfläche (Wasser): Die Struktur wandert mit dieser Geschwindigkeit in m/s, eine zweite, feinere
                /// Lage (<see cref="DetailTiling"/>-mal so dicht) läuft quer dazu – so entsteht kein starres Muster.
                /// </summary>
                public float Flow;
                public float DetailTiling;
            }

            public static readonly Spec Default = new Spec { Smoothness = Shading.Smoothness };
            public static readonly Spec Snow = new Spec { Smoothness = 0.18f, Normal = SurfaceTextures.Kind.Snow, NormalStrength = 0.55f, TilesPerMeter = 0.9f };
            public static readonly Spec Ice = new Spec { Smoothness = 0.62f, Normal = SurfaceTextures.Kind.Snow, NormalStrength = 0.35f, TilesPerMeter = 0.35f };
            public static readonly Spec Water = new Spec
            {
                Smoothness = 0.94f, Normal = SurfaceTextures.Kind.Waves, NormalStrength = 0.3f, TilesPerMeter = 0.12f,
                Flow = 0.35f, DetailTiling = 2.3f,
            };
            public static readonly Spec Glossy = new Spec { Smoothness = 0.86f };
            public static readonly Spec Metal = new Spec { Smoothness = 0.58f, Metallic = 0.85f };
            /// <summary>Laternenglas: deutlich heller als Schnee in der Sonne (≈ 1,5), damit nur die Lampe blüht, nicht der Schnee.</summary>
            public static readonly Spec Glow = new Spec { Smoothness = 0.7f, Emission = 10f };
            /// <summary>Gefieder: feine Struktur, leichter Glanz wie bei echtem Pinguin-Gefieder (dicht, ölig).</summary>
            public static readonly Spec Feather = new Spec { Smoothness = 0.42f, Normal = SurfaceTextures.Kind.Snow, NormalStrength = 0.11f, TilesPerMeter = 6f };

            public static Spec For(Surface s)
            {
                switch (s)
                {
                    case Surface.Snow: return Snow;
                    case Surface.Ice: return Ice;
                    case Surface.Water: return Water;
                    case Surface.Glossy: return Glossy;
                    case Surface.Metal: return Metal;
                    case Surface.Glow: return Glow;
                    case Surface.Feather: return Feather;
                    default: return Default;
                }
            }

            /// <summary>
            /// Oberfläche nach Farbe, wo die Farbe eindeutig ist (Eis, Meer, Wasser, Laternenlicht). Prims rundet Farben
            /// auf 1/32 (ADR-06) – darum werden die Palettenfarben hier genauso gerundet. Ohne das traf der Vergleich
            /// nie: Siemens-Werte wie #FFE270 liegen nicht auf dem 1/32-Raster, Meer, Eis und Lampen blieben „Default“.
            /// </summary>
            public static Surface ByColor(Color c)
            {
                c = Prims.Quantize(c);
                if (c == Prims.Quantize(Palette.Ice) || c == Prims.Quantize(Palette.IceShadow)) return Surface.Ice;
                if (c == Prims.Quantize(Palette.Sea) || c == Prims.Quantize(Palette.Water)) return Surface.Water;
                if (c == Prims.Quantize(Palette.LanternGlow)) return Surface.Glow;
                return Surface.Default;
            }

            /// <param name="tiling">Wiederholungen der Struktur über das ganze Teil (<see cref="Prims.TilingFor"/>).</param>
            public static void ApplyTo(Material m, Surface surface, int tiling = 1)
            {
                var s = For(surface);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", s.Smoothness);   // eingebaute Pipeline
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", s.Smoothness);   // URP
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", s.Metallic);
                if (tiling > 1) m.mainTextureScale = new Vector2(tiling, tiling);
                if (s.Normal.HasValue && m.HasProperty("_BumpMap"))
                {
                    m.SetTexture("_BumpMap", SurfaceTextures.Get(s.Normal.Value));
                    m.SetFloat("_BumpScale", s.NormalStrength);
                    m.EnableKeyword("_NORMALMAP");
                }
                // Zweite Wellenlage über die Detail-Normal-Map von URP Lit. Ohne Detail-Farbbild nimmt Lit ein neutrales
                // Grau (×2 = 1), die Farbe bleibt also unverändert. Die Detail-Kachelung rechnet Lit auf die schon
                // gekachelten Koordinaten – darum nur der Faktor, nicht noch einmal mal tiling.
                if (s.Normal.HasValue && s.DetailTiling > 0f && m.HasProperty("_DetailNormalMap"))
                {
                    m.SetTexture("_DetailNormalMap", SurfaceTextures.Get(s.Normal.Value));
                    m.SetFloat("_DetailNormalMapScale", s.NormalStrength);
                    m.SetTextureScale("_DetailAlbedoMap", Vector2.one * s.DetailTiling);
                    m.EnableKeyword("_DETAIL_MULX2");
                }
                if (s.Emission > 0f && m.HasProperty("_EmissionColor"))
                {
                    m.SetColor("_EmissionColor", m.color * s.Emission);
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }
        }

        /// <summary>
        /// Himmel, Dunst und Nachbearbeitung (RenderLook). Werte für einen klaren Polartag: tiefblauer Zenit,
        /// heller Horizont, leichter Dunst über dem Meer, ACES-Tonemapping wie im Film.
        /// </summary>
        public static class Atmosphere
        {
            public static readonly float SunSize = 0.035f;
            public static readonly float AtmosphereThickness = 0.7f;
            public static readonly float SkyExposure = 1.1f;
            /// <summary>
            /// Dunst (exponentiell²) als Produkt aus Dichte und Kameraabstand zum Blickpunkt: Am Blickpunkt liegen so
            /// immer ≈ 2 % Dunst, beim Sechsfachen des Abstands etwa die Hälfte – egal ob die Kamera 24 m (Hub) oder
            /// 70 m (Spielbrett) entfernt steht. Eine feste Dichte legte über das Spielbrett ≈ 19 % Schleier (Audit B-10).
            /// Im Hub ergibt das 0,15 / 24 m ≈ 0,0063, also den früheren festen Wert.
            /// </summary>
            public static readonly float FogAtFocus = 0.15f;
            /// <summary>Kürzester Abstand für die Dunst-Rechnung (Nahaufnahmen sollen nicht im Nebel versinken).</summary>
            public static readonly float FogMinDistance = 8f;

            /// <summary>Belichtung: Sonnenschnee (≈ 1,5 vor dem Tonemapping) soll Zeichnung behalten statt weiß auszufressen.</summary>
            public static readonly float PostExposure = -0.35f;
            public static readonly float Contrast = 14f;
            public static readonly float Saturation = 6f;
            /// <summary>
            /// Über Sonnenschnee (≈ 1,5), unter Laternenglas (≈ 9): Es blühen nur Lichtquellen. Bei 1,05 blühte jede
            /// besonnte Schneefläche und legte einen milchigen Schleier über das Bild (Grafik-Audit B-01).
            /// </summary>
            public static readonly float BloomThreshold = 2.6f;
            public static readonly float BloomIntensity = 0.4f;
            public static readonly float BloomScatter = 0.68f;
            public static readonly float Vignette = 0.2f;
            /// <summary>Umgebungsverdeckung (SSAO): dunkelt Fugen, Kontaktflächen und Ecken ab.</summary>
            public static readonly float AmbientOcclusion = 1.1f;
        }

        /// <summary>
        /// Wie fein Rundungen aufgelöst werden. Regel: Die Facetten sollen auf dem Bildschirm ungefähr gleich groß
        /// wirken. Bauten und Gegenstände bekommen ihre Segmentzahl darum aus ihrer Größe (<see cref="SegmentsFor"/>):
        /// Eine Eisscholle von 20 m wird kein sichtbares Zwölfeck, eine Laternenkugel kein Kristall.
        /// Figuren haben feste Stufen, weil ihre Teile zueinander passen müssen.
        /// 0 bei <see cref="FacetLength"/> schaltet Bauten zurück auf Unitys runde Standardformen.
        /// </summary>
        public static class Detail
        {
            /// <summary>Segmente rundum für Figurenkörper und alles, was eng an ihnen anliegt (Kleidung).</summary>
            public static readonly int FigureSegments = 48;
            /// <summary>Ringe von den Füßen bis zum Scheitel einer Figur.</summary>
            public static readonly int FigureRings = 40;
            /// <summary>Segmente rundum für mittlere Teile der Figuren (Flossen, Brillenfassung).</summary>
            public static readonly int Segments = 32;
            /// <summary>Segmente rundum für Kleinteile kleiner als etwa eine Handbreit.</summary>
            public static readonly int SmallSegments = 20;

            /// <summary>Angestrebte Kantenlänge einer Facette am Umfang von Bauten, in Metern.</summary>
            public static readonly float FacetLength = 0.08f;
            /// <summary>Grenzen für Bauten: weich gerundet (realistisch, ADR-27) – unter 20 sähe man Kanten, über 128 keinen Unterschied.</summary>
            public static readonly int MinSegments = 20, MaxSegments = 128;

            /// <summary>Segmente rundum für eine runde Form mit diesem Durchmesser (Vielfaches von 4, damit sie symmetrisch bleibt).</summary>
            public static int SegmentsFor(float diameter)
            {
                if (FacetLength <= 0f) return 0;
                int n = Mathf.RoundToInt(Mathf.PI * diameter / FacetLength / 4f) * 4;
                return Mathf.Clamp(n, MinSegments, MaxSegments);
            }
        }

        /// <summary>
        /// Maße. Alle Figurenmaße sind Anteile der Körperhöhe, damit eine Änderung der Größe alles mitzieht.
        /// Der Pinguin ist das Maß der Welt: Türen, Laternen und Iglus richten sich nach ihm.
        /// </summary>
        public static class Proportion
        {
            /// <summary>
            /// Scheitelhöhe eines Pinguins der Größe 1 in Metern. docs/04 nennt 0,6 m; die bisherigen Szenen sind
            /// auf 0,76 m eingerichtet (Kamera, Abstände im Huddle) – darum gilt bis zu einem Spieltest dieser Wert.
            /// </summary>
            public static readonly float PenguinHeight = 0.76f;

            /// <summary>Ausgewachsener Pinguin nach Art des Kaiserpinguins: kein abgesetzter Hals, weißer Bauch bis zur Kehle.</summary>
            public static readonly PenguinShape Adult = new PenguinShape
            {
                // Höhe, Radius, Versatz nach vorn – alles in Anteilen der Körperhöhe. Unten breit, oben schmal,
                // der Kopf sitzt ohne Hals auf dem Körper: Diese Kegelform liest man sofort als Pinguin.
                Profile = new[]
                {
                    new Vector3(0.03f, 0.000f, 0.000f), new Vector3(0.06f, 0.150f, 0.010f), new Vector3(0.14f, 0.235f, 0.020f),
                    new Vector3(0.28f, 0.265f, 0.025f), new Vector3(0.42f, 0.255f, 0.020f), new Vector3(0.56f, 0.215f, 0.010f),
                    new Vector3(0.68f, 0.165f, 0.005f), new Vector3(0.76f, 0.142f, 0.010f), new Vector3(0.84f, 0.152f, 0.020f),
                    new Vector3(0.92f, 0.132f, 0.015f), new Vector3(0.97f, 0.085f, 0.010f), new Vector3(1.00f, 0.000f, 0.005f),
                },
            };

            /// <summary>Küken: rund, großer Kopf, kurze Flossen, graue Daunen mit schwarzer Kappe und weißer Maske.</summary>
            public static readonly PenguinShape Chick = new PenguinShape
            {
                Profile = new[]
                {
                    new Vector3(0.03f, 0.000f, 0.000f), new Vector3(0.07f, 0.170f, 0.000f), new Vector3(0.18f, 0.270f, 0.010f),
                    new Vector3(0.32f, 0.290f, 0.010f), new Vector3(0.46f, 0.265f, 0.010f), new Vector3(0.58f, 0.210f, 0.010f),
                    new Vector3(0.64f, 0.192f, 0.010f), new Vector3(0.74f, 0.215f, 0.020f), new Vector3(0.86f, 0.205f, 0.020f),
                    new Vector3(0.95f, 0.140f, 0.010f), new Vector3(1.00f, 0.000f, 0.005f),
                },
                BellyTop = 0.60f, BellyWidthLow = 70f, BellyWidthHigh = 50f,
                HeadFrom = 0.62f,
                FaceFrom = 0.68f, FaceTo = 0.93f, FaceWidth = 58f,
                EyeHeight = 0.80f, EyeAngle = 30f, EyeSize = 0.075f, Sclera = false,
                BeakHeight = 0.77f, BeakLength = 0.08f,
                FlipperTop = 0.58f, FlipperLength = 0.30f, FlipperWidth = 0.13f,
                FootLength = 0.15f,
                EarPatches = false,
            };
        }
    }

    /// <summary>
    /// Bauplan eines Pinguinkörpers. Alle Werte sind Anteile der Körperhöhe, Winkel in Grad (0 = vorn).
    /// Die Vorgaben gelten für den ausgewachsenen Pinguin; Varianten überschreiben nur, was abweicht.
    /// </summary>
    public sealed class PenguinShape
    {
        /// <summary>Umriss von unten nach oben: (Höhe, Radius, Versatz nach vorn). Erster und letzter Radius 0.</summary>
        public Vector3[] Profile;
        /// <summary>Tiefe zu Breite des Querschnitts: Pinguine sind von vorn breiter als von der Seite.</summary>
        public float Depth = 0.88f;

        /// <summary>Bis zu dieser Höhe reicht der helle Bauch; seine halbe Breite verjüngt sich von unten nach oben.</summary>
        public float BellyTop = 0.80f, BellyWidthLow = 74f, BellyWidthHigh = 46f;
        /// <summary>Ab dieser Höhe zählt die Fläche als Kopf (eigene Farbe möglich, Küken: schwarze Kappe).</summary>
        public float HeadFrom = 0.80f;
        /// <summary>Helle Gesichtsmaske; Breite 0 = keine.</summary>
        public float FaceFrom, FaceTo, FaceWidth;

        public float EyeHeight = 0.875f, EyeAngle = 32f, EyeSize = 0.070f;
        /// <summary>Augenweiß um die Pupille. Ohne wirkt das Auge kleiner und tierhafter.</summary>
        public bool Sclera = true;
        public float BeakHeight = 0.845f, BeakLength = 0.13f, BeakWidth = 0.055f, BeakTilt = 12f;
        public float FlipperTop = 0.66f, FlipperLength = 0.42f, FlipperWidth = 0.12f, FlipperThickness = 0.035f, FlipperSpread = 12f;
        public float FootLength = 0.17f, FootSpread = 18f;
        /// <summary>Gelb-orange Flecken hinter den Augen wie beim Kaiserpinguin.</summary>
        public bool EarPatches = true;

        /// <summary>Weicher Umriss aus <see cref="Profile"/>, einmal berechnet (PenguinFigure).</summary>
        internal Outline CachedOutline;
    }
}
