using System;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Was Spielende im Menü „Einstellungen“ selbst einstellen können. Gilt nur für die laufende Sitzung und wird
    /// nicht gespeichert (Titelbildschirm: „Es werden keine Daten gespeichert“).
    /// </summary>
    public static class Settings
    {
        /// <summary>Lautstärkestufen für leise / mittel / laut.</summary>
        public static readonly float[] VolumeLevels = { 0.35f, 0.7f, 1f };
        /// <summary>Tempo im Huddle: gemütlich (Wärme und Wind laufen langsamer) / normal.</summary>
        public static readonly float[] PaceLevels = { 0.6f, 1f };

        private static int _volume = VolumeLevels.Length - 1;
        private static int _pace = PaceLevels.Length - 1;
        private static bool _highQuality = true;
        private static bool _techButtons = true;

        /// <summary>Nach jeder Änderung – wer davon abhängt (Knöpfe „Blick unter die Haube“), richtet sich danach.</summary>
        public static event Action Changed;

        public static int Volume
        {
            get => _volume;
            set { _volume = Mathf.Clamp(value, 0, VolumeLevels.Length - 1); AudioListener.volume = VolumeLevels[_volume]; Changed?.Invoke(); }
        }

        public static int Pace
        {
            get => _pace;
            set { _pace = Mathf.Clamp(value, 0, PaceLevels.Length - 1); Changed?.Invoke(); }
        }

        /// <summary>Faktor auf die Spielzeit im Huddle (Wärme, Windwechsel).</summary>
        public static float PaceFactor => PaceLevels[_pace];

        public static bool HighQuality
        {
            get => _highQuality;
            set { _highQuality = value; RenderQuality.Reapply(); Changed?.Invoke(); }
        }

        public static bool TechButtons
        {
            get => _techButtons;
            set { _techButtons = value; Changed?.Invoke(); }
        }

        public static bool Muted
        {
            get => Sfx.Muted;
            set { Sfx.SetMuted(value); Changed?.Invoke(); }
        }

        /// <summary>Im Editor wirkungslos, darum dort nicht angeboten.</summary>
        public static bool FullscreenAvailable => !Application.isEditor;

        public static bool Fullscreen
        {
            get => Screen.fullScreen;
            set { Screen.fullScreen = value; Changed?.Invoke(); }
        }
    }
}
