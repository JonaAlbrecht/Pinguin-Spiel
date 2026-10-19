using System.Collections;
using SFE.SimCore.Data;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Schritt 6 von MG2 „So macht's Siemens“: links das Strömungsbild von Pips bestem Zug, rechts ein erzeugtes
    /// Symbolbild eines Hochgeschwindigkeitszugs im Luftstrom (bis freigegebenes Material da ist). Karte:
    /// <c>Data/Transfer/transfer_MG2_velaro.json</c> – ohne Freigabe als ENTWURF. Danach Dialog <c>mg2_transfer_after</c>.
    /// </summary>
    public static class PolarExpressTransfer
    {
        public const string CardPath = "Data/Transfer/transfer_MG2_velaro";

        public static IEnumerator Play(GameRoot root)
        {
            var card = MiniJson.ParseObject(GameData.Text(CardPath));
            var progress = Mg2Progress.Of(root.State);
            yield return TransferScreen.Play(root, card, progress.Snapshot, "mg2TransferImageTrain", TrainPlaceholder(),
                "mg2TransferImageReal", "mg2_transfer_after");
            // Notizbuch: Windkanal (Begriff zum Transfer)
            foreach (var g in root.Data.MinigameContent("mg2").Arr("glossary"))
            {
                var o = (System.Collections.Generic.Dictionary<string, object>)g;
                if (o.Str("phase") != "transfer") continue;
                string entry = $"{o.Str("term")}: {o.Str("text")}";
                if (!root.State.Glossary.Contains(entry)) root.State.Glossary.Add(entry);
            }
        }

        /// <summary>Satz zum Mitnehmen der Karte (für Notizbuch und Abschluss, <see cref="GameState.Takeaway"/>).</summary>
        public static string Takeaway() => GameData.Loc(MiniJson.ParseObject(GameData.Text(CardPath))["takeaway"]);

        private const int Res = 288;

        /// <summary>Symbolbild: stromlinienförmiger Zug, Luftlinien schmiegen sich an, dahinter kaum Wirbel.</summary>
        private static Texture2D TrainPlaceholder()
        {
            var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            const float ground = 0.30f, height = 0.17f, x0 = 0.12f, x1 = 0.92f, nose = 0.22f;
            for (int py = 0; py < Res; py++)
            {
                for (int px = 0; px < Res; px++)
                {
                    float x = px / (float)Res, y = py / (float)Res;
                    float top = TrainTop(x, ground, height, x0, x1, nose);
                    Color c;
                    if (y < ground) c = Palette.Mg2Rail;
                    else if (top > 0 && y < top) c = Palette.Mg2PixelTrain;
                    else
                    {
                        // Luftlinien: Abstand zur Dachlinie, nach oben auseinanderlaufend
                        float lift = top > 0 ? (top - ground) * Mathf.Exp(-(y - top) * 6f) : 0f;
                        float s = (y - ground - lift) * 22f;
                        float line = Mathf.Abs(s - Mathf.Round(s));
                        float speed = Mathf.Clamp01(0.55f + (top > 0 ? 0.45f * Mathf.Exp(-(y - top) * 8f) : 0f));
                        c = Color.Lerp(Palette.Mg2AirSlow, Palette.Mg2AirFastest, speed * 0.7f);
                        if (line < 0.08f) c = Color.Lerp(c, Palette.Mg2PixelLine, 0.8f);
                    }
                    if (px < 4 || py < 4 || px >= Res - 4 || py >= Res - 4) c = Palette.PixelFrame;
                    tex.SetPixel(px, py, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static float TrainTop(float x, float ground, float height, float x0, float x1, float nose)
        {
            if (x < x0 || x > x1) return -1f;
            float front = Mathf.Clamp01((x - x0) / nose), back = Mathf.Clamp01((x1 - x) / (nose * 0.6f));
            float f = Mathf.Min(Mathf.Sqrt(front), Mathf.Sqrt(back));
            return ground + height * f;
        }
    }
}
