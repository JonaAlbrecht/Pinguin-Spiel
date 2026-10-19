using System.Collections;
using SFE.SimCore.Data;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// MG3 Schritt 7 „So macht's Siemens“: links die beste Fabrik des Spielers auf ZWILLIS Bildschirm, rechts ein
    /// erzeugtes Symbolbild einer Fertigungslinie im Computer (bis freigegebenes Material da ist, docs/00 Abschnitt 3).
    /// Karte <c>Data/Transfer/transfer_MG3_factory.json</c> – ohne Freigabe als ENTWURF.
    /// </summary>
    public static class FishFactoryTransfer
    {
        public const string CardPath = "Data/Transfer/transfer_MG3_factory";

        public static IEnumerator Play(GameRoot root)
        {
            var card = MiniJson.ParseObject(GameData.Text(CardPath));
            root.Hud.ShowPhases(6);
            yield return TransferScreen.Play(root, card, FishFactoryState.Snapshot, "mg3TransferImageFactory", LinePlaceholder(),
                "mg3TransferImageLine", "mg3_transfer_after");
            root.Hud.ShowPhases(-1);
            root.State.Takeaway = GameData.Loc(card["takeaway"]);
        }

        /// <summary>Kantenlänge des Symbolbilds (wie die Schaltschrank-Grafik in <see cref="TransferScreen"/>).</summary>
        private const int Res = 288;

        /// <summary>
        /// Platzhalter: Fertigungslinie von oben auf einem Bildschirm – Band, vier Stationen, vor der zweiten ein Stapel
        /// wartender Teile (der Engpass) und das Raster des Digitalen Zwillings.
        /// </summary>
        public static Texture2D LinePlaceholder()
        {
            var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[Res * Res];
            for (int y = 0; y < Res; y++)
            {
                for (int x = 0; x < Res; x++)
                {
                    float u = x / (float)Res, v = y / (float)Res;
                    Color c = (int)(u * 12) % 2 == (int)(v * 12) % 2 ? Palette.ScreenIce : Palette.ScreenIceAlt;
                    if (x % 24 == 0 || y % 24 == 0) c = Palette.GridLine;
                    if (v > 0.44f && v < 0.56f) c = Palette.Mg3Belt;                                  // Band
                    for (int s = 0; s < 4; s++)
                    {
                        float cx = 0.16f + s * 0.23f;
                        if (Mathf.Abs(u - cx) < 0.07f && Mathf.Abs(v - 0.5f) < 0.11f)
                            c = s == 1 ? Palette.Mg3Jam : Palette.Mg3Machine;                         // Engpass rot
                    }
                    // wartende Teile vor dem Engpass
                    if (u > 0.27f && u < 0.32f && v > 0.6f && v < 0.84f && (int)(v * 40) % 3 != 0) c = Palette.Mg3Crate;
                    // fertige Teile am Ende
                    if (u > 0.9f && v > 0.2f && v < 0.36f && (int)(v * 40) % 3 != 0) c = Palette.Mg3Sushi;
                    bool frame = x < 8 || y < 8 || x >= Res - 8 || y >= Res - 8;
                    px[y * Res + x] = frame ? Palette.ScreenFrame : c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
