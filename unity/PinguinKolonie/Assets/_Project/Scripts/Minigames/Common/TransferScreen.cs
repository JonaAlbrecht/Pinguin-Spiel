using System.Collections;
using System.Collections.Generic;
using SFE.SimCore.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SFE.Game
{
    /// <summary>
    /// Schritt 6 „So macht's Siemens“ (docs/04 Abschnitt 5.7).
    /// Ohne vollständige Freigabe (approval) wird die Karte deutlich als ENTWURF gezeigt (Frage G-05:
    /// im Prototyp erlaubt). Bis ein freigegebenes Bild da ist (G-02), zeigt die rechte Seite ein
    /// erzeugtes Symbolbild eines Schaltschranks.
    /// </summary>
    public static class TransferScreen
    {
        public static IEnumerator Play(GameRoot root)
        {
            var card = root.Data.TransferCard;
            var layer = new GameObject("Transfer");
            var rt = UIKit.Rect(layer, root.Hud.Root);
            UIKit.Fill(rt);
            rt.SetSiblingIndex(0);
            var bg = UIKit.Panel(rt, new Color(0.93f, 0.95f, 0.98f), "Background");
            UIKit.Fill(bg.rectTransform);

            var head = UIKit.Label(rt, "So macht's Siemens", 30, new Color(0, 0.5f, 0.5f), TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(head.rectTransform, 0, 1, 1, 1, 80, -175, 80, 95);
            var headline = UIKit.Label(rt, GameData.Loc(card["headline"]), 44, Palette.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(headline.rectTransform, 0, 1, 1, 1, 80, -240, 80, 135);
            var business = UIKit.Label(rt, card.Str("siemensBusiness"), 24, new Color(0.35f, 0.4f, 0.5f), TextAnchor.UpperLeft);
            UIKit.Place(business.rectTransform, 0, 1, 1, 1, 80, -280, 80, 245);

            if (!IsApproved(card))
            {
                var draft = UIKit.Panel(rt, Palette.Draft, "Draft");
                UIKit.PlaceFixed(draft.rectTransform, new Vector2(1, 1), new Vector2(-230, -140), new Vector2(380, 70));
                draft.rectTransform.localRotation = Quaternion.Euler(0, 0, 6);
                var dt = UIKit.Label(draft.transform, "ENTWURF – nicht freigegeben", 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Fill(dt.rectTransform);
            }

            // Bilder nebeneinander
            ImageBox(rt, new Vector2(0.27f, 0.5f), root.State.Mg1Snapshot, "Deine Siedlung im Computer");
            ImageBox(rt, new Vector2(0.73f, 0.5f), SwitchgearPlaceholder(), "Schaltschrank im Computer (Symbolbild)");

            // Übersetzungstabelle zwischen den Bildern
            var table = card.Arr("translation");
            var tablePanel = UIKit.Panel(rt, Color.white, "Translation");
            UIKit.PlaceFixed(tablePanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(560, 420));
            var th1 = UIKit.Label(tablePanel.transform, "In der Siedlung", 24, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.PlaceFixed(th1.rectTransform, new Vector2(0, 1), new Vector2(145, -30), new Vector2(260, 40));
            var th2 = UIKit.Label(tablePanel.transform, "Im Schaltschrank", 24, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.PlaceFixed(th2.rectTransform, new Vector2(0, 1), new Vector2(425, -30), new Vector2(260, 40));
            for (int i = 0; i < table.Count; i++)
            {
                var row = (Dictionary<string, object>)table[i];
                var a = UIKit.Label(tablePanel.transform, GameData.Loc(row["game"]), 21, Palette.Text, TextAnchor.UpperLeft);
                UIKit.PlaceFixed(a.rectTransform, new Vector2(0, 1), new Vector2(145, -100 - i * 82), new Vector2(260, 80));
                var b = UIKit.Label(tablePanel.transform, GameData.Loc(row["real"]), 21, Palette.Text, TextAnchor.UpperLeft);
                UIKit.PlaceFixed(b.rectTransform, new Vector2(0, 1), new Vector2(425, -100 - i * 82), new Vector2(260, 80));
            }

            // Text, Nutzen, Ehrlichkeit, Satz zum Mitnehmen
            var body = UIKit.Label(rt, GameData.Loc(card["body"]), 26, Palette.Text, TextAnchor.UpperLeft);
            UIKit.Place(body.rectTransform, 0, 0, 0.5f, 0, 80, 150, 20, -330);
            var benefits = new System.Text.StringBuilder("Kundennutzen:\n");
            foreach (var bn in card.Arr("customerBenefits")) benefits.Append("–  ").Append(GameData.Loc(bn)).Append('\n');
            var ben = UIKit.Label(rt, benefits.ToString(), 26, Palette.Text, TextAnchor.UpperLeft);
            UIKit.Place(ben.rectTransform, 0.5f, 0, 1, 0, 20, 150, 80, -330);
            var honesty = UIKit.Label(rt, GameData.Loc(card["honesty"]) + "   Software: " + card.Str("product"), 20,
                new Color(0.4f, 0.45f, 0.55f), TextAnchor.UpperLeft, FontStyle.Italic);
            UIKit.Place(honesty.rectTransform, 0.5f, 0, 1, 0, 20, 140, 80, -190);

            var take = UIKit.Panel(rt, new Color(0f, 0.6f, 0.6f, 0.12f), "Takeaway");
            UIKit.Place(take.rectTransform, 0, 0, 1, 0, 60, 30, 420, -125);
            var takeText = UIKit.Label(take.transform, "Das kannst du jetzt erzählen:  „" + GameData.Loc(card["takeaway"]) + "“", 26,
                new Color(0, 0.35f, 0.38f), TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(takeText.rectTransform, 0, 0, 1, 1, 20, 6, 20, 6);

            bool done = false;
            var next = UIKit.Button(rt, "Weiter", () => done = true, Palette.Button, 32);
            UIKit.PlaceFixed((RectTransform)next.transform, new Vector2(1, 0), new Vector2(-200, 75), new Vector2(300, 90));

            yield return root.Hud.FadeIn();
            while (!done) yield return null;
            yield return root.Hud.Say("transfer_after");
            yield return root.Hud.FadeOut();
            Object.Destroy(layer);
        }

        private static bool IsApproved(Dictionary<string, object> card)
        {
            if (!card.Has("approval")) return false;
            var a = card.Obj("approval");
            return a.Has("department") && a.Has("communications") && a.Has("date");
        }

        private static void ImageBox(RectTransform parent, Vector2 anchor, Texture tex, string caption)
        {
            var frame = UIKit.Panel(parent, Color.white, "Frame");
            UIKit.PlaceFixed(frame.rectTransform, anchor, new Vector2(anchor.x < 0.5f ? -60 : 60, 0), new Vector2(430, 470));
            var img = new GameObject("Image").AddComponent<RawImage>();
            UIKit.Rect(img.gameObject, frame.transform);
            UIKit.Place(img.rectTransform, 0, 0, 1, 1, 15, 55, 15, 15);
            img.texture = tex;
            img.color = tex != null ? Color.white : new Color(0.85f, 0.88f, 0.93f);
            var cap = UIKit.Label(frame.transform, caption, 22, Palette.Text, TextAnchor.MiddleCenter);
            UIKit.Place(cap.rectTransform, 0, 0, 1, 0, 10, 8, 10, -48);
        }

        /// <summary>Platzhalter: Schaltschrank mit warmen Bauteilen in der Farbskala des Spiels.</summary>
        private static Texture2D SwitchgearPlaceholder()
        {
            const int w = 96, h = 96;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var hot = new[] { new Vector3(30, 70, 9), new Vector3(66, 72, 7), new Vector3(48, 44, 11), new Vector3(28, 20, 6), new Vector3(70, 22, 8) };
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double t = -30;
                    foreach (var s in hot)
                    {
                        float d2 = (x - s.x) * (x - s.x) + (y - s.y) * (y - s.y);
                        t += 3.2 * s.z * Mathf.Exp(-d2 / (2f * s.z * s.z));
                    }
                    var c = HeatColors.For(t, -30, -8, 0);
                    bool frameEdge = x < 4 || y < 4 || x >= w - 4 || y >= h - 4;
                    bool shelf = y % 32 < 2;
                    bool component = false;
                    foreach (var s in hot) if (Mathf.Abs(x - s.x) < s.z * 0.6f && Mathf.Abs(y - s.y) < s.z * 0.9f) component = true;
                    if (frameEdge || shelf) c = new Color(0.3f, 0.33f, 0.4f);
                    else if (component) c = Color.Lerp(c, new Color(0.2f, 0.22f, 0.28f), 0.45f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
