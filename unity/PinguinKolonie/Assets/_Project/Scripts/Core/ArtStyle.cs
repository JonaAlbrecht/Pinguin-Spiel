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
        /// <summary>Wie Licht auf die Formen fällt: matte Facetten unter weicher Wintersonne (Diorama-Look).</summary>
        public static class Shading
        {
            /// <summary>
            /// Facetten statt weicher Rundungen: Jede Fläche hat einen Farbton. So sehen Kugeln, Würfel und Figuren
            /// aus einem Guss aus, und eine niedrige Segmentzahl wirkt gewollt statt grob.
            /// </summary>
            public static readonly bool Flat = true;

            /// <summary>0 = stumpf wie Schnee, 1 = Spiegel. Unity setzt 0,5 – das glänzte wie Plastik.</summary>
            public static readonly float Smoothness = 0.12f;

            public static readonly Color SunColor = new Color(1f, 0.97f, 0.92f);
            public static readonly float SunIntensity = 1.1f;
            public static readonly Vector3 SunAngles = new Vector3(55f, -35f, 0f);
            /// <summary>Kühles Himmelslicht von überall: hebt die Schattenseite an, damit Schwarz nicht absäuft.</summary>
            public static readonly Color Ambient = new Color(0.62f, 0.70f, 0.82f);
            /// <summary>Weichere, freundlichere Schatten; 1 wäre tiefschwarz.</summary>
            public static readonly float ShadowStrength = 0.65f;

            public static void ApplyTo(Material m)
            {
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", Smoothness);   // eingebaute Pipeline
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", Smoothness);   // URP
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            }

            public static void ApplyTo(Light sun)
            {
                sun.color = SunColor;
                sun.intensity = SunIntensity;
                sun.transform.rotation = Quaternion.Euler(SunAngles);
                sun.shadowStrength = ShadowStrength;
                // Ohne Flat wirkt ambientLight nicht: Eine leere Szene steht auf Skybox-Umgebungslicht (Review zu PR #2).
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Ambient;
            }
        }

        /// <summary>
        /// Wie fein Rundungen aufgelöst werden. Regel: Die Facetten sollen auf dem Bildschirm ungefähr gleich groß
        /// wirken. Darum haben große Figurenkörper mehr Segmente, Kleinteile (Augen, Schnabel, Zehen) weniger.
        /// 0 bei <see cref="Segments"/> schaltet zurück auf Unitys runde Standardformen.
        /// </summary>
        public static class Detail
        {
            /// <summary>Segmente rundum für Figurenkörper und alles, was eng an ihnen anliegt (Kleidung).</summary>
            public static readonly int FigureSegments = 16;
            /// <summary>Ringe von den Füßen bis zum Scheitel einer Figur.</summary>
            public static readonly int FigureRings = 14;
            /// <summary>Segmente rundum für Bauten und Gegenstände (Iglu, Laterne, Pfütze, Ring).</summary>
            public static readonly int Segments = 12;
            /// <summary>Segmente rundum für Kleinteile kleiner als etwa eine Handbreit.</summary>
            public static readonly int SmallSegments = 8;
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
