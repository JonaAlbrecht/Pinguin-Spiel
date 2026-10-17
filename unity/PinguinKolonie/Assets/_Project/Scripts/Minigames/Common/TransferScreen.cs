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
        /// <summary>„So macht's Siemens“ nach MG1: Siedlung neben Schaltschrank.</summary>
        public static IEnumerator Play(GameRoot root) =>
            Play(root, root.Data.TransferCard, root.State.Mg1Snapshot, "transferImageSettlement",
                SwitchgearPlaceholder(), "transferImageSwitchgear", "transfer_after");

        /// <summary>„So macht's Siemens“ nach dem Huddle (Prolog): Huddle neben Batteriezellen im Akku.</summary>
        public static IEnumerator PlayBattery(GameRoot root) =>
            Play(root, root.Data.BatteryCard, root.State.PrologueSnapshot, "transferImageHuddle",
                BatteryPlaceholder(), "transferImageBattery", "transfer_battery_after");

        /// <param name="placeholder">Erzeugtes Symbolbild rechts; wird am Ende freigegeben.</param>
        private static IEnumerator Play(GameRoot root, Dictionary<string, object> card, Texture leftImage, string leftCaption,
            Texture2D placeholder, string rightCaption, string afterNode)
        {
            var layer = new GameObject("Transfer");
            var rt = UIKit.Rect(layer, root.Hud.Root);
            UIKit.Fill(rt);
            rt.SetSiblingIndex(0);
            var bg = UIKit.Panel(rt, Palette.Page, "Background");
            UIKit.Fill(bg.rectTransform);

            var ui = root.Data;
            var head = UIKit.Label(rt, ui.Ui("transferHead"), 30, Palette.TextAccent, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(head.rectTransform, 0, 1, 1, 1, 80, -130, 80, 88);
            var headline = UIKit.Label(rt, GameData.Loc(card["headline"]), 40, Palette.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(headline.rectTransform, 0, 1, 1, 1, 80, -235, 520, 130);
            var business = UIKit.Label(rt, card.Str("siemensBusiness"), 24, Palette.TextMuted, TextAnchor.UpperLeft);
            UIKit.Place(business.rectTransform, 0, 1, 1, 1, 80, -280, 520, 242);

            if (!IsApproved(card))
            {
                var draft = UIKit.Panel(rt, Palette.Draft, "Draft");
                UIKit.PlaceFixed(draft.rectTransform, new Vector2(1, 1), new Vector2(-230, -245), new Vector2(380, 70));
                draft.rectTransform.localRotation = Quaternion.Euler(0, 0, 6);
                var dt = UIKit.Label(draft.transform, ui.Ui("transferDraft"), 24, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Fill(dt.rectTransform);
            }

            // Bänder von oben (Referenz 1920 × 1080): Titel bis ~280 · Bilder und Tabelle 300–700 · Text 720–880 ·
            // „Ehrlich vereinfacht“ 882–945 · Erzählsatz ab 955. Vorher ragten die Bildrahmen in den Text (Spieltest 17.10.2026).
            ImageBox(rt, new Vector2(0.27f, 0.5f), leftImage, ui.Ui(leftCaption));
            ImageBox(rt, new Vector2(0.73f, 0.5f), placeholder, ui.Ui(rightCaption));

            // Übersetzungstabelle zwischen den Bildern
            var table = card.Arr("translation");
            var tablePanel = UIKit.Panel(rt, Palette.PanelLight, "Translation");
            UIKit.PlaceFixed(tablePanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(560, 400));
            var th1 = UIKit.Label(tablePanel.transform, TableHead(card, "tableGame", ui.Ui("transferTableGame")), 24, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.PlaceFixed(th1.rectTransform, new Vector2(0, 1), new Vector2(145, -30), new Vector2(260, 40));
            var th2 = UIKit.Label(tablePanel.transform, TableHead(card, "tableReal", ui.Ui("transferTableReal")), 24, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
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
            UIKit.Place(body.rectTransform, 0, 0, 0.5f, 0, 80, 200, 20, -360);
            var benefits = new System.Text.StringBuilder(ui.Ui("transferBenefits")).Append('\n');
            foreach (var bn in card.Arr("customerBenefits")) benefits.Append("–  ").Append(GameData.Loc(bn)).Append('\n');
            var ben = UIKit.Label(rt, benefits.ToString(), 26, Palette.Text, TextAnchor.UpperLeft);
            UIKit.Place(ben.rectTransform, 0.5f, 0, 1, 0, 20, 200, 80, -360);
            var honesty = UIKit.Label(rt, GameData.Loc(card["honesty"]) + "   " + ui.Ui("transferSoftware", card.Str("product")), 18,
                Palette.TextMuted, TextAnchor.UpperLeft, FontStyle.Italic);
            UIKit.Place(honesty.rectTransform, 0.5f, 0, 1, 0, 20, 135, 80, -198);

            var take = UIKit.Panel(rt, Palette.Card, "Takeaway");
            UIKit.Place(take.rectTransform, 0, 0, 1, 0, 60, 30, 420, -125);
            var takeText = UIKit.Label(take.transform, ui.Ui("transferTakeaway", GameData.Loc(card["takeaway"])), 26,
                Palette.TextAccentDark, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(takeText.rectTransform, 0, 0, 1, 1, 20, 6, 20, 6);

            bool done = false;
            var next = UIKit.Button(rt, ui.Ui("next"), () => done = true, Palette.Button, 32);
            UIKit.PlaceFixed((RectTransform)next.transform, new Vector2(1, 0), new Vector2(-200, 75), new Vector2(300, 90));

            yield return root.Hud.FadeIn();
            while (!done) yield return null;
            Sfx.Play(Sfx.Sound.Click);
            if (card.Has("retrieval")) yield return Recall(root, rt, card.Arr("retrieval"));
            yield return root.Hud.Say(afterNode);
            yield return root.Hud.FadeOut();
            Object.Destroy(layer);
            Object.Destroy(placeholder);
        }

        /// <summary>
        /// „Kurz nachgedacht“: Satz und Geschäftsfeld selbst wiederfinden statt nur lesen (Abnahme-Kriterien docs/00).
        /// Keine Punkte, kein Scheitern: Eine falsche Antwort wird erklärt, dann darf man neu wählen.
        /// </summary>
        private static IEnumerator Recall(GameRoot root, RectTransform parent, List<object> questions)
        {
            var ui = root.Data;
            foreach (var q in questions)
            {
                var question = (Dictionary<string, object>)q;
                var layer = UIKit.Panel(parent, Palette.Dim, "Recall");
                UIKit.Fill(layer.rectTransform);
                var page = UIKit.Panel(layer.transform, Palette.PanelLight, "Page");
                UIKit.PlaceFixed(page.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 700));
                var title = UIKit.Label(page.transform, ui.Ui("recallTitle"), 30, Palette.TextAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Place(title.rectTransform, 0, 1, 1, 1, 20, -70, 20, 15);
                var qText = UIKit.Label(page.transform, GameData.Loc(question["question"]), 36, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Place(qText.rectTransform, 0, 1, 1, 1, 40, -170, 40, 80);
                var feedback = UIKit.Label(page.transform, ui.Ui("recallHint"), 24, Palette.TextMuted, TextAnchor.MiddleCenter, FontStyle.Italic);
                UIKit.Place(feedback.rectTransform, 0, 0, 1, 0, 40, 30, 40, -110);

                bool right = false;
                var options = question.Arr("options");
                var buttons = new List<Button>();
                for (int i = 0; i < options.Count; i++)
                {
                    var o = (Dictionary<string, object>)options[i];
                    Button b = null;
                    b = UIKit.Button(page.transform, GameData.Loc(o["text"]), () =>
                    {
                        if (right) return; // nach der richtigen Antwort überschreibt kein Klick mehr das „Genau!“
                        feedback.text = GameData.Loc(o["feedback"]);
                        if ((bool)o["correct"])
                        {
                            foreach (var other in buttons) other.interactable = false;
                            b.GetComponent<Image>().color = Palette.Good;
                            Sfx.Play(Sfx.Sound.Success, 0.5f);
                            right = true;
                        }
                        else
                        {
                            b.GetComponent<Image>().color = Palette.ButtonOff;
                            b.interactable = false;
                            Sfx.Play(Sfx.Sound.Wrong, 0.5f);
                        }
                    }, Palette.ButtonAlt, 26);
                    UIKit.PlaceFixed((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0, -260 - i * 105), new Vector2(900, 90));
                    buttons.Add(b);
                }
                while (!right) yield return null;
                yield return new WaitForSeconds(1.6f);
                Object.Destroy(layer.gameObject);
            }
        }

        private static bool IsApproved(Dictionary<string, object> card)
        {
            if (!card.Has("approval")) return false;
            var a = card.Obj("approval");
            return a.Has("department") && a.Has("communications") && a.Has("date");
        }

        private static void ImageBox(RectTransform parent, Vector2 anchor, Texture tex, string caption)
        {
            var frame = UIKit.Panel(parent, Palette.PanelLight, "Frame");
            UIKit.PlaceFixed(frame.rectTransform, anchor, new Vector2(anchor.x < 0.5f ? -60 : 60, 40), new Vector2(400, 400));
            var img = new GameObject("Image").AddComponent<RawImage>();
            UIKit.Rect(img.gameObject, frame.transform);
            UIKit.Place(img.rectTransform, 0, 0, 1, 1, 15, 55, 15, 15);
            img.texture = tex;
            img.color = tex != null ? Color.white : Palette.Placeholder;
            var cap = UIKit.Label(frame.transform, caption, 22, Palette.Text, TextAnchor.MiddleCenter);
            UIKit.Place(cap.rectTransform, 0, 0, 1, 0, 10, 8, 10, -48);
        }

        /// <summary>Platzhalter: Schaltschrank mit warmen Bauteilen in der Farbskala des Spiels.</summary>
        /// <summary>Spaltenkopf der Übersetzungstabelle aus der Karte („Im Akku“), sonst der allgemeine Text.</summary>
        private static string TableHead(Dictionary<string, object> card, string key, string fallback) =>
            card.Has(key) ? GameData.Loc(card[key]) : fallback;

        /// <summary>
        /// Platzhalter: Akku von oben, 8 × 5 Zellen. Innen warm, am Rand kühler – dieselbe Farbskala wie die Pinguine.
        /// Symbolbild, keine Rechnung; das sagt auch die Bildunterschrift.
        /// </summary>
        private static Texture2D BatteryPlaceholder()
        {
            // Gezeichnet in einem 96er-Raster, abgetastet mit Res Pixeln – scharf auf großen Bildschirmen.
            const int w = 96, h = 96, cols = 8, rows = 5;
            var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var frame = Palette.PixelFrame;
            float cellW = (w - 8) / (float)cols, cellH = (h - 30) / (float)rows;
            for (int py = 0; py < Res; py++)
            {
                for (int px = 0; px < Res; px++)
                {
                    float x = px * (float)w / Res, y = py * (float)h / Res;
                    var c = frame;
                    int cx = (int)((x - 4) / cellW), cy = (int)((y - 15) / cellH);
                    bool inside = x >= 4 && x < w - 4 && y >= 15 && y < h - 15;
                    float lx = (x - 4) - cx * cellW, ly = (y - 15) - cy * cellH;
                    bool gap = lx < 1.5f || ly < 1.5f;
                    if (inside && !gap && cx < cols && cy < rows)
                    {
                        // Abstand zur Mitte des Akkus: innen wärmer
                        float dx = (cx + 0.5f) / cols - 0.5f, dy = (cy + 0.5f) / rows - 0.5f;
                        float t = 40f - 55f * Mathf.Sqrt(dx * dx + dy * dy) * 1.6f;
                        c = HeatColors.Penguin(t, 18);
                    }
                    tex.SetPixel(px, py, c);
                }
            }
            tex.Apply();
            return tex;
        }

        /// <summary>Kantenlänge der erzeugten Symbolbilder (v0.1: 96 – auf dem Bildschirm sichtbar verwaschen).</summary>
        private const int Res = 288;

        private static Texture2D SwitchgearPlaceholder()
        {
            const int w = 96, h = 96;
            var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var hot = new[] { new Vector3(30, 70, 9), new Vector3(66, 72, 7), new Vector3(48, 44, 11), new Vector3(28, 20, 6), new Vector3(70, 22, 8) };
            for (int py = 0; py < Res; py++)
            {
                for (int px = 0; px < Res; px++)
                {
                    float x = px * (float)w / Res, y = py * (float)h / Res;
                    double t = -30;
                    foreach (var s in hot)
                    {
                        float d2 = (x - s.x) * (x - s.x) + (y - s.y) * (y - s.y);
                        t += 3.2 * s.z * Mathf.Exp(-d2 / (2f * s.z * s.z));
                    }
                    var c = HeatColors.For(t, -30, -8, 0);
                    bool frameEdge = x < 4 || y < 4 || x >= w - 4 || y >= h - 4;
                    bool shelf = y % 32f < 2f;
                    bool component = false;
                    foreach (var s in hot) if (Mathf.Abs(x - s.x) < s.z * 0.6f && Mathf.Abs(y - s.y) < s.z * 0.9f) component = true;
                    if (frameEdge || shelf) c = Palette.PixelFrame;
                    else if (component) c = Color.Lerp(c, Palette.PixelComponent, 0.45f);
                    tex.SetPixel(px, py, c);
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
