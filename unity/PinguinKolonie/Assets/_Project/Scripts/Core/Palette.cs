using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Alle Farben des Spiels. Oben das Siemens-Farbschema (<see cref="Siemens"/>), darunter die Spielfarben, die nur
    /// darauf verweisen. Regeln (docs/07_Stilregeln.md):
    /// - Wo es geht Primärfarben (Petrol, Light Petrol, Bold/Soft Green, Bold/Soft Blue, Deep Blue, Light Sand),
    ///   sonst Sekundär- und Zusatzfarben. Keine Farbe von außerhalb des Schemas.
    /// - Helle Flächen (Himmel, Eis) dürfen eine Siemens-Farbe mit Light Sand aufhellen (<see cref="Tint"/>),
    ///   Abdunkeln nur mit Deep Blue (<see cref="Shade"/>).
    /// - Neue Farben hier eintragen, nie <c>new Color(…)</c> im übrigen Code – tools/style_lint.py prüft das.
    /// </summary>
    public static partial class Palette
    {
        /// <summary>Siemens-Farbschema „sie-colors-overview“ V1.4.4 (Stand 2025-02-17), Bildschirmwerte.</summary>
        public static class Siemens
        {
            // Primärfarben
            public static readonly Color Petrol = Hex(0x009999);
            public static readonly Color LightPetrol = Hex(0x00C1B6);
            public static readonly Color BoldGreen = Hex(0x00FFB9);
            public static readonly Color SoftGreen = Hex(0x00D7A0);
            public static readonly Color BoldBlue = Hex(0x00E6DC);
            public static readonly Color SoftBlue = Hex(0x00BEDC);
            public static readonly Color DeepBlue = Hex(0x000028);
            public static readonly Color LightSand = Hex(0xF3F3F0);

            // Sekundärfarben
            public static readonly Color DarkSand = Hex(0xAAAA96);
            public static readonly Color SoftSand = Hex(0xC5C5B8);
            public static readonly Color BrightSand = Hex(0xDFDFD9);
            public static readonly Color DarkYellow = Hex(0xF7C600);
            public static readonly Color Yellow = Hex(0xFFD732);
            public static readonly Color SoftYellow = Hex(0xFFE270);
            public static readonly Color DarkGreen = Hex(0x00646E);
            public static readonly Color Green = Hex(0x00AF8E);
            public static readonly Color DarkBlue = Hex(0x00557C);
            public static readonly Color Blue = Hex(0x0087BE);
            public static readonly Color DarkPurple = Hex(0x553BA3);
            public static readonly Color Purple = Hex(0x805CFF);
            public static readonly Color SoftPurple = Hex(0xB4A8FF);
            public static readonly Color DeepBlue80 = Hex(0x333353);
            public static readonly Color DeepBlue60 = Hex(0x66667E);
            public static readonly Color DeepBlue40 = Hex(0x9999A9);
            public static readonly Color DeepBlue20 = Hex(0xCCCCD4);
            public static readonly Color DeepBlue10 = Hex(0xE5E5E9);
            public static readonly Color Red = Hex(0xEF0137);
            public static readonly Color DarkOrange = Hex(0xEC6602);
            public static readonly Color Orange = Hex(0xFF9000);

            // Zusatzfarben (Abstufungen von Deep Blue)
            public static readonly Color DeepBlue95 = Hex(0x0D0D33);
            public static readonly Color DeepBlue90 = Hex(0x19193D);
            public static readonly Color DeepBlue85 = Hex(0x262648);
            public static readonly Color DeepBlue70 = Hex(0x4C4C68);
            public static readonly Color DeepBlue55 = Hex(0x737389);
            public static readonly Color DeepBlue50 = Hex(0x808099);
            public static readonly Color DeepBlue30 = Hex(0xB3B3BE);
            public static readonly Color DeepBlue8 = Hex(0xEBEBEE);
        }

        public static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

        public static Color Alpha(Color c, float a) { c.a = a; return c; }

        /// <summary>Aufhellung für große Flächen: <paramref name="amount"/> Anteil der Farbe, der Rest Light Sand.</summary>
        public static Color Tint(Color c, float amount) => Color.Lerp(Siemens.LightSand, c, amount);

        /// <summary>Abdunkeln mit Deep Blue statt mit Schwarz: <paramref name="amount"/> Anteil Deep Blue.</summary>
        public static Color Shade(Color c, float amount) => Color.Lerp(c, Siemens.DeepBlue, amount);

        // ---------- Welt ----------
        public static readonly Color Sky = Tint(Siemens.Blue, 0.32f);
        /// <summary>Realistischer Himmel (ADR-27): Tönung des Himmels, Boden unter dem Horizont, Dunst am Horizont.</summary>
        public static readonly Color SkyTint = Siemens.DeepBlue30;
        public static readonly Color SkyGround = Shade(Siemens.DarkBlue, 0.35f);
        public static readonly Color Haze = Tint(Siemens.SoftBlue, 0.30f);
        /// <summary>Himmelslicht (dreifarbig): blau von oben, hell am Horizont, vom Schnee hell zurückgeworfen.</summary>
        public static readonly Color AmbientSky = Tint(Siemens.Blue, 0.55f);
        public static readonly Color AmbientHorizon = Tint(Siemens.SoftBlue, 0.35f);
        public static readonly Color AmbientGround = Tint(Siemens.SoftBlue, 0.18f);
        public static readonly Color Sea = Siemens.DarkBlue;
        public static readonly Color Ice = Tint(Siemens.SoftBlue, 0.20f);
        public static readonly Color IceShadow = Tint(Siemens.SoftBlue, 0.34f);
        public static readonly Color Snow = Siemens.LightSand;
        public static readonly Color GridLine = Siemens.DeepBlue20;
        /// <summary>Gitterlinien über dem Wärmebild: dunkel genug, um auf Gelb und Blau zu stehen.</summary>
        public static readonly Color GridLineOnHeat = Siemens.DeepBlue60;
        /// <summary>Iglu und Schnee heller als das Eis darunter, sonst verschwinden sie auf der Scholle.</summary>
        public static readonly Color Igloo = Siemens.LightSand;
        /// <summary>Fugen zwischen den Schneeblöcken: bläulich-grau wie Schnee im Schatten, damit der helle Iglu Form bekommt.</summary>
        public static readonly Color IglooJoint = Siemens.DeepBlue40;
        public static readonly Color IglooCold = Tint(Siemens.SoftBlue, 0.40f);
        public static readonly Color IglooCozy = Tint(Siemens.SoftYellow, 0.35f);
        public static readonly Color IglooMelting = Tint(Siemens.DarkOrange, 0.30f);
        public static readonly Color Door = Siemens.DeepBlue80;
        public static readonly Color Wall = Siemens.DeepBlue10;
        public static readonly Color Water = Siemens.SoftBlue;
        public static readonly Color Thread = Siemens.Orange;
        public static readonly Color Wind = Siemens.Blue;
        public static readonly Color WindLabel = Siemens.DarkBlue;
        public static readonly Color LanternGlow = Siemens.SoftYellow;
        public static readonly Color LanternPole = Siemens.DeepBlue80;
        public static readonly Color Wood = Siemens.DarkSand;
        public static readonly Color Crate = Siemens.SoftSand;
        public static readonly Color SignBoard = Siemens.LightSand;
        public static readonly Color SignAccent = Siemens.Petrol;
        public static readonly Color Marker = Siemens.DarkYellow;
        public static readonly Color LabWall = Siemens.DeepBlue10;
        public static readonly Color LabFloor = Siemens.DeepBlue30;
        /// <summary>Dach und Türrahmen des Labors: das Siemens-Haus in Petrol.</summary>
        public static readonly Color LabTrim = Siemens.Petrol;
        public static readonly Color Desk = Siemens.DarkSand;
        public static readonly Color DeskBase = Siemens.DeepBlue55;
        public static readonly Color Screen = Siemens.BoldBlue;
        /// <summary>ZWILLIS Bildschirm im Labor: dunkler Rahmen, leuchtende Kante, kühl getöntes „Rechen-Eis“ –
        /// damit niemand das Raster mit der echten Scholle verwechselt (Kalles Versuch läuft draußen in Weiß).</summary>
        public static readonly Color ScreenFrame = Siemens.DeepBlue85;
        public static readonly Color ScreenIce = Tint(Siemens.BoldBlue, 0.14f);
        public static readonly Color ScreenIceAlt = Tint(Siemens.BoldBlue, 0.24f);

        // ---------- Figuren ----------
        public static readonly Color PenguinBlack = Siemens.DeepBlue85;
        public static readonly Color PenguinWhite = Siemens.LightSand;
        /// <summary>Ergrautes Gefieder der Ältesten (Oma Grete).</summary>
        public static readonly Color PenguinElder = Siemens.DeepBlue60;
        /// <summary>Gelbe Flecken am Hals wie beim Kaiserpinguin.</summary>
        public static readonly Color PenguinAccent = Siemens.Yellow;
        public static readonly Color ChickDown = Siemens.DeepBlue40;
        public static readonly Color ChickBelly = Siemens.DeepBlue20;
        public static readonly Color Beak = Siemens.Orange;
        public static readonly Color Ring = Alpha(Siemens.LightSand, 0.5f);

        // Erkennungsfarben der Figuren – eine Signalfarbe je Figur, gut unterscheidbar
        public static readonly Color PipScarf = Siemens.Red;
        public static readonly Color PipTablet = Siemens.BoldBlue;
        public static readonly Color FriedaTuft = Siemens.Yellow;
        /// <summary>Laborkittel in Light Petrol: ein weißer Kittel verschwände vor dem weißen Bauch.</summary>
        public static readonly Color FriedaCoat = Siemens.LightPetrol;
        public static readonly Color Glasses = Siemens.DeepBlue;
        public static readonly Color KalleHelmet = Siemens.DarkYellow;
        public static readonly Color KalleBraces = Siemens.Red;
        public static readonly Color GreteShawl = Siemens.DarkPurple;
        public static readonly Color TheoCap = Siemens.Red;
        public static readonly Color TheoCapPeak = Shade(Siemens.Red, 0.25f);
        public static readonly Color Thermometer = Siemens.LightSand;
        public static readonly Color ThermometerRed = Siemens.Red;
        public static readonly Color KlaraVisor = Siemens.SoftGreen;
        public static readonly Color Ledger = Siemens.DarkSand;
        /// <summary>Tilda (MG2 Polar-Express): Lokführerinnen-Mütze in Siemens-Blau, Trillerpfeife.</summary>
        public static readonly Color TildaCap = Siemens.Blue;
        public static readonly Color TildaCapPeak = Shade(Siemens.Blue, 0.3f);
        public static readonly Color TildaWhistle = Siemens.DarkYellow;
        /// <summary>Olaf (MG3 Fischfabrik): Südwester in Orange, Schürze in Petrol.</summary>
        public static readonly Color OlafHat = Siemens.Orange;
        public static readonly Color OlafApron = Siemens.Petrol;
        /// <summary>ZWILLI, der digitale Zwilling: die Siemens-Verlaufsfarben „Bold Dynamic Petrol“.</summary>
        public static readonly Color Hologram = Siemens.BoldBlue;
        public static readonly Color HologramBelly = Siemens.BoldGreen;
        public static readonly Color HologramProjector = Siemens.DarkGreen;

        // ---------- Oberfläche ----------
        public static readonly Color Panel = Alpha(Siemens.DeepBlue, 0.92f);
        public static readonly Color PanelSoft = Alpha(Siemens.DeepBlue, 0.85f);
        public static readonly Color PanelLight = Alpha(Siemens.LightSand, 0.97f);
        /// <summary>Hintergrund ganzer Seiten (Transfer), auf dem helle Felder stehen.</summary>
        public static readonly Color Page = Siemens.DeepBlue8;
        public static readonly Color Card = Alpha(Siemens.Petrol, 0.12f);
        public static readonly Color TableHead = Siemens.DeepBlue10;
        public static readonly Color Placeholder = Siemens.DeepBlue20;
        /// <summary>Abdunkelung hinter Fenstern.</summary>
        public static readonly Color Dim = Alpha(Siemens.DeepBlue, 0.5f);
        public static readonly Color Fade = Alpha(Siemens.DeepBlue, 0f);
        public static readonly Color TitleBackground = Siemens.DeepBlue;
        public static readonly Color TitleSub = Siemens.BoldBlue;
        public static readonly Color TitleNote = Siemens.DeepBlue20;

        public static readonly Color Button = Siemens.Petrol;
        public static readonly Color ButtonAlt = Siemens.DeepBlue60;
        /// <summary>Zurückhaltende Knöpfe (Rückgängig, Hilfe, Waagschalen).</summary>
        public static readonly Color ButtonQuiet = Siemens.DeepBlue70;
        /// <summary>Notizbuch und Beispiel-Siedlung: eigene Farbe, damit sie sich von den Spielknöpfen abheben.</summary>
        public static readonly Color ButtonSide = Siemens.DarkGreen;
        public static readonly Color ButtonOff = Siemens.DeepBlue40;
        // Tönungen, mit denen Unity Knöpfe beim Zeigen/Drücken/Sperren multipliziert (keine eigenen Farben)
        public static readonly Color ButtonHoverTint = Color.white;
        public static readonly Color ButtonPressTint = Siemens.DeepBlue20;
        public static readonly Color ButtonDisabledTint = Alpha(Siemens.DeepBlue40, 0.5f);

        public static readonly Color Text = Siemens.DeepBlue;
        public static readonly Color TextLight = Siemens.LightSand;
        public static readonly Color TextMuted = Siemens.DeepBlue60;
        public static readonly Color TextAccent = Siemens.Petrol;
        public static readonly Color TextAccentDark = Siemens.DarkGreen;
        /// <summary>Kleine Erklärtexte („Blick unter die Haube“, Rechenteile).</summary>
        public static readonly Color TextTech = Siemens.DarkBlue;
        public static readonly Color TextTechLight = Siemens.BoldBlue;
        public static readonly Color TextWarm = Siemens.DarkOrange;
        public static readonly Color PhaseDone = Alpha(Siemens.SoftGreen, 0.9f);
        public static readonly Color PhaseTodo = Alpha(Siemens.LightSand, 0.45f);
        public static readonly Color StarOff = Siemens.DeepBlue20;
        public static readonly Color ScaleBeam = Siemens.DeepBlue20;

        public static readonly Color Selected = Siemens.Yellow;
        public static readonly Color Good = Siemens.Green;
        public static readonly Color Warn = Siemens.DarkYellow;
        public static readonly Color Bad = Siemens.Red;
        public static readonly Color Draft = Alpha(Siemens.Red, 0.9f);

        // Pixelbilder der Transferkarten
        public static readonly Color PixelFrame = Siemens.DeepBlue70;
        public static readonly Color PixelComponent = Siemens.DeepBlue80;
    }
}
