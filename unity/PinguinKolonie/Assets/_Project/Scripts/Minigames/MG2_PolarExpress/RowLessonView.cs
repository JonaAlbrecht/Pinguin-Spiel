using System.Collections;
using SFE.SimCore.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace SFE.Game
{
    /// <summary>
    /// MG2 Schritt 2, zweiter Teil: die Rechentafel „Fünf Kästchen Luft“ (Muster wie MG1 <see cref="PairLessonView"/>).
    /// Oben ZWILLIS Bildschirm mit fünf Kästchen in einer Reihe, links und rechts eine Wand; je Kästchen Pfeile für die
    /// Luft, die nach rechts will, nach links will oder bleibt. Jeder Schritt: erst <b>Ausgleichen</b> (die Pfeile in
    /// jedem Kästchen gleichen sich an), dann <b>Weiterschieben</b> (die Pfeile wandern zum Nachbarn). Unten die Tafel
    /// „Luft vorher + kommt an − zieht weiter = Luft neu“. Alle Zahlen sind die des Windkanal-Modells
    /// (<see cref="RowLesson"/>, Golden <c>flow_lesson</c>), angezeigt mal 10 mit einer Stelle (<see cref="RowLesson.RoundRow"/>).
    /// Formeln nur im Modus „Blick unter die Haube“.
    /// </summary>
    public static class RowLessonView
    {
        private const float Pitch = 250f;      // Abstand der Kästchen
        private const float Half = 105f;       // halbe Kästchenbreite
        private const float PxPerAir = 17f;    // Pfeillänge je angezeigter Luft-Einheit (Luft mal 10)
        private const double Show = 10.0;      // Anzeige: Luft mal 10
        private const double Unit = 0.1;

        private sealed class CellView
        {
            public RectTransform Right, Left, Stay, RightHead, LeftHead;
            public Text Value;
        }

        public static IEnumerator Play(GameRoot root, PolarExpressLevel level)
        {
            var data = root.Data;
            var hud = root.Hud;
            var lesson = new RowLesson(level.Lesson);
            int n = lesson.Cells;

            var layer = new GameObject("RowLesson");
            var rt = UIKit.Rect(layer, hud.Root);
            UIKit.Fill(rt);
            rt.SetSiblingIndex(0);
            var panel = UIKit.Panel(rt, Palette.PanelLight, "Board");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1600, 640));
            var title = UIKit.Label(panel.transform, data.Ui("mg2LessonTitle"), 30, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.PlaceFixed(title.rectTransform, new Vector2(0, 1), new Vector2(480, -32), new Vector2(900, 44));
            var stepLabel = UIKit.Label(panel.transform, "", 24, Palette.TextMuted, TextAnchor.MiddleRight);
            UIKit.PlaceFixed(stepLabel.rectTransform, new Vector2(1, 1), new Vector2(-200, -32), new Vector2(360, 44));

            // ZWILLIS Bildschirm mit den fünf Kästchen
            var screen = UIKit.Panel(panel.transform, Palette.ScreenFrame, "Screen");
            UIKit.PlaceFixed(screen.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -175), new Vector2(1440, 230));
            var phaseLabel = UIKit.Label(screen.transform, "", 22, Palette.Screen, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(phaseLabel.rectTransform, 0, 0, 1, 1, 24, 10, 24, 8);
            var cells = new CellView[n];
            float x0 = -(n - 1) * Pitch / 2f;
            for (int s = 0; s < 2; s++)
            {
                var wall = UIKit.Panel(screen.transform, Palette.Mg2Wall, "Wall");
                UIKit.PlaceFixed(wall.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(s == 0 ? x0 - Half - 18 : -x0 + Half + 18, -10), new Vector2(18, 170));
            }
            for (int i = 0; i < n; i++)
            {
                float cx = x0 + i * Pitch;
                var box = UIKit.Panel(screen.transform, Palette.Mg2CellBox, "Cell");
                UIKit.PlaceFixed(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(cx, -10), new Vector2(Half * 2, 170));
                var v = new CellView();
                v.Value = UIKit.Label(box.transform, "", 30, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.PlaceFixed(v.Value.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(200, 40));
                v.Right = Bar(box.transform, Palette.Mg2ArrowRight, 0f);
                v.Left = Bar(box.transform, Palette.Mg2ArrowLeft, 1f);
                v.RightHead = Head(box.transform, Palette.Mg2ArrowRight);
                v.LeftHead = Head(box.transform, Palette.Mg2ArrowLeft);
                v.Stay = UIKit.Panel(box.transform, Palette.Mg2ArrowStay, "Stay").rectTransform;
                v.Stay.anchorMin = v.Stay.anchorMax = new Vector2(0.5f, 0.5f);
                cells[i] = v;
            }

            // Tafel: Kästchen | vorher | + | kommt an | − | zieht weiter | ergibt | neu
            float[] colX = { 110, 310, 425, 540, 655, 770, 900, 1030 };
            string[] heads = { data.Ui("mg2LessonCell"), data.Ui("mg2LessonBefore"), data.Ui("pairPlus"), data.Ui("mg2LessonComeIn"),
                data.Ui("pairMinus"), data.Ui("mg2LessonMoveOn"), data.Ui("pairResult"), data.Ui("mg2LessonAfter") };
            Color[] tint = { Color.clear, Palette.TableHead, Color.clear, Palette.Mg2ArrowRight, Color.clear, Palette.Mg2ArrowLeft, Color.clear, Palette.TableHead };
            float tableX = 140f;
            for (int c = 0; c < colX.Length; c++)
            {
                bool op = c > 0 && tint[c] == Color.clear;
                if (tint[c] != Color.clear)
                {
                    var hb = UIKit.Panel(panel.transform, tint[c], "Head");
                    UIKit.PlaceFixed(hb.rectTransform, new Vector2(0, 1), new Vector2(tableX + colX[c], -320), new Vector2(170, 46));
                }
                var t = UIKit.Label(panel.transform, heads[c], op ? (c == 6 ? 22 : 30) : 21, Palette.Text, TextAnchor.MiddleCenter, op ? FontStyle.Bold : FontStyle.Normal);
                UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(tableX + colX[c], -320), new Vector2(op ? 90 : 170, 46));
            }
            var table = new Text[n, 8];
            for (int r = 0; r < n; r++)
            {
                float y = -370 - r * 40;
                table[r, 0] = UIKit.Label(panel.transform, data.Ui("mg2LessonCellN", r + 1), 22, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.PlaceFixed(table[r, 0].rectTransform, new Vector2(0, 1), new Vector2(tableX + colX[0], y), new Vector2(170, 38));
                foreach (int c in new[] { 1, 3, 5, 7 })
                {
                    table[r, c] = UIKit.Label(panel.transform, data.Ui("pairUnknown"), 24, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIKit.PlaceFixed(table[r, c].rectTransform, new Vector2(0, 1), new Vector2(tableX + colX[c], y), new Vector2(170, 38));
                }
            }
            var sum = UIKit.Label(panel.transform, "", 22, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.PlaceFixed(sum.rectTransform, new Vector2(0, 1), new Vector2(1420, -450), new Vector2(300, 160));
            var note = UIKit.Label(panel.transform, "", 22, Palette.TextWarm, TextAnchor.MiddleLeft, FontStyle.Italic);
            UIKit.PlaceFixed(note.rectTransform, new Vector2(0, 1), new Vector2(780, -600), new Vector2(880, 40));

            var techBtn = UIKit.Button(panel.transform, data.Ui("techButton"), () => ShowFormula(root, level), Palette.ButtonQuiet, 20);
            UIKit.PlaceFixed((RectTransform)techBtn.transform, new Vector2(0, 0), new Vector2(170, 40), new Vector2(300, 48));
            hud.TechOnly(techBtn.gameObject);
            hud.LockWhileBlocking(techBtn);

            bool go = false;
            var next = UIKit.Button(panel.transform, data.Ui("mg2LessonNext"), () => go = true, Palette.Button, 26);
            UIKit.PlaceFixed((RectTransform)next.transform, new Vector2(1, 0), new Vector2(-190, 40), new Vector2(340, 60));
            hud.LockWhileBlocking(next);

            double total = 0;
            foreach (double a in level.Lesson.Air) total += a;
            var arrows = CurrentArrows(lesson);
            ShowArrows(cells, arrows.r, arrows.l, arrows.s, 0f);
            ShowValues(data, cells, lesson.Air);
            for (int r = 0; r < n; r++) table[r, 1].text = data.Ui("mg2LessonValue", lesson.Air[r] * Show);
            stepLabel.text = data.Ui("mg2LessonStep", 0, level.Lesson.Steps);
            sum.text = data.Ui("mg2LessonSum", total * Show);
            yield return hud.Say("mg2_lesson_intro");

            for (int step = 1; step <= level.Lesson.Steps; step++)
            {
                go = false;
                while (!go) yield return null;
                Sfx.Play(Sfx.Sound.Click);
                var s = lesson.Step();
                for (int r = 0; r < n; r++)
                {
                    table[r, 1].text = data.Ui("mg2LessonValue", s.Before[r] * Show);
                    table[r, 3].text = table[r, 5].text = table[r, 7].text = data.Ui("pairUnknown");
                }

                // 1. Ausgleichen: in jedem Kästchen gleichen sich die Pfeile an – die Luft im Kästchen bleibt gleich
                phaseLabel.text = data.Ui("mg2LessonEvenOut");
                for (float t = 0; t <= 1f; t += Time.deltaTime / 0.7f)
                {
                    ShowArrows(cells, Mix(s.RightBefore, s.RightEven, t), Mix(s.LeftBefore, s.LeftEven, t), Mix(s.StayBefore, s.StayEven, t), 0f);
                    yield return null;
                }
                ShowArrows(cells, s.RightEven, s.LeftEven, s.StayEven, 0f);
                yield return new WaitForSeconds(0.25f);

                // 2. Weiterschieben: Pfeile wandern zum Nachbarn; an der Wand prallen sie zurück
                phaseLabel.text = data.Ui("mg2LessonPush");
                Sfx.Play(Sfx.Sound.Plop, 0.4f);
                for (float t = 0; t <= 1f; t += Time.deltaTime / 0.7f)
                {
                    ShowArrows(cells, s.RightEven, s.LeftEven, s.StayEven, t);
                    yield return null;
                }
                arrows = CurrentArrows(lesson);
                ShowArrows(cells, arrows.r, arrows.l, arrows.s, 0f);
                ShowValues(data, cells, s.After);
                phaseLabel.text = "";

                // Zeilen der Tafel füllen sich von links nach rechts
                var shown = new double[n, 4];
                for (int r = 0; r < n; r++)
                {
                    RowLesson.RoundRow(s.Before[r] * Show, s.ComeIn[r] * Show, s.MoveOn[r] * Show, s.After[r] * Show, Unit,
                        out shown[r, 0], out shown[r, 1], out shown[r, 2], out shown[r, 3]);
                    table[r, 1].text = data.Ui("mg2LessonValue", shown[r, 0]);
                }
                foreach (int c in new[] { 3, 5, 7 })
                {
                    yield return new WaitForSeconds(0.3f);
                    for (int r = 0; r < n; r++)
                        table[r, c].text = data.Ui("mg2LessonValue", c == 3 ? shown[r, 1] : c == 5 ? shown[r, 2] : shown[r, 3]);
                    Sfx.Play(c == 7 ? Sfx.Sound.Ding : Sfx.Sound.Plop, 0.4f);
                }
                stepLabel.text = data.Ui("mg2LessonStep", step, level.Lesson.Steps);
                double come = 0, move = 0, after = 0;
                for (int r = 0; r < n; r++) { come += s.ComeIn[r]; move += s.MoveOn[r]; after += s.After[r]; }
                sum.text = data.Ui("mg2LessonSum", after * Show) + "\n" + data.Ui("mg2LessonSumParts", come * Show, move * Show);
                if (step == 1)
                {
                    note.text = data.Ui("mg2LessonConservation", come * Show, move * Show);
                    yield return hud.Say("mg2_lesson_conservation");
                }
                else note.text = data.Ui("mg2LessonTravel");
            }

            // Den Rest rechnet ZWILLI schnell weiter, bis sich die Luft fast gleich verteilt hat
            UIKit.SetButtonLabel(next, data.Ui("mg2LessonRun"));
            go = false;
            while (!go) yield return null;
            next.gameObject.SetActive(false);
            for (int k = 0; k < level.Lesson.RunSteps; k++)
            {
                lesson.Step();
                arrows = CurrentArrows(lesson);
                ShowArrows(cells, arrows.r, arrows.l, arrows.s, 0f);
                ShowValues(data, cells, lesson.Air);
                stepLabel.text = data.Ui("mg2LessonStep", lesson.StepCount, level.Lesson.Steps + level.Lesson.RunSteps);
                yield return new WaitForSeconds(0.08f);
            }
            Sfx.Play(Sfx.Sound.Ding, 0.6f);
            note.text = data.Ui("mg2LessonEven");
            yield return hud.Say("mg2_lesson_done");
            Object.Destroy(layer);
        }

        private static (double[] r, double[] l, double[] s) CurrentArrows(RowLesson lesson)
        {
            int n = lesson.Cells;
            var r = new double[n];
            var l = new double[n];
            var s = new double[n];
            for (int x = 0; x < n; x++) RowLesson.Aggregates(lesson.Lbm.F, n, x, out r[x], out l[x], out s[x]);
            return (r, l, s);
        }

        private static double[] Mix(double[] a, double[] b, float t)
        {
            var m = new double[a.Length];
            for (int i = 0; i < a.Length; i++) m[i] = a[i] + (b[i] - a[i]) * Mathf.Clamp01(t);
            return m;
        }

        private static RectTransform Bar(Transform parent, Color color, float pivotX)
        {
            var bar = UIKit.Panel(parent, color, "Bar").rectTransform;
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
            bar.pivot = new Vector2(pivotX, 0.5f);
            return bar;
        }

        private static RectTransform Head(Transform parent, Color color)
        {
            var head = UIKit.Panel(parent, color, "Head").rectTransform;
            head.anchorMin = head.anchorMax = new Vector2(0.5f, 0.5f);
            head.sizeDelta = new Vector2(22, 22);
            head.localRotation = Quaternion.Euler(0, 0, 45);
            return head;
        }

        /// <summary>
        /// Pfeile je Kästchen. <paramref name="shift"/> 0…1 schiebt die Pfeile zum Nachbarn (Weiterschieben);
        /// an der Wand werden sie kürzer – dort prallen sie zurück und erscheinen danach in Gegenrichtung.
        /// </summary>
        private static void ShowArrows(CellView[] cells, double[] right, double[] left, double[] stay, float shift)
        {
            int n = cells.Length;
            float move = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shift)) * Pitch;
            for (int i = 0; i < n; i++)
            {
                var v = cells[i];
                float lr = (float)(right[i] * Show) * PxPerAir, ll = (float)(left[i] * Show) * PxPerAir;
                if (i == n - 1) lr *= 1f - Mathf.Clamp01(shift);   // am rechten Rand: prallt zurück
                if (i == 0) ll *= 1f - Mathf.Clamp01(shift);
                float dxR = i == n - 1 ? 0f : move, dxL = i == 0 ? 0f : -move;
                v.Right.sizeDelta = new Vector2(Mathf.Max(2f, lr), 18);
                v.Right.anchoredPosition = new Vector2(4 + dxR, -30);
                v.RightHead.anchoredPosition = new Vector2(4 + dxR + lr, -30);
                v.Left.sizeDelta = new Vector2(Mathf.Max(2f, ll), 18);
                v.Left.anchoredPosition = new Vector2(-4 + dxL, -62);
                v.LeftHead.anchoredPosition = new Vector2(-4 + dxL - ll, -62);
                float side = Mathf.Sqrt((float)(stay[i] * Show)) * 13f;
                v.Stay.sizeDelta = new Vector2(side, side);
                v.Stay.anchoredPosition = new Vector2(0, 18);
            }
        }

        private static void ShowValues(GameData data, CellView[] cells, double[] air)
        {
            for (int i = 0; i < cells.Length; i++) cells[i].Value.text = data.Ui("mg2LessonValue", air[i] * Show);
        }

        /// <summary>„Blick unter die Haube“: BGK-Stoß, Gleichgewicht, Strömen, Erhaltung – wie LaTeX gesetzt.</summary>
        public static void ShowFormula(GameRoot root, PolarExpressLevel level)
        {
            var data = root.Data;
            var page = root.Hud.OpenPopup(data.Tech("mg2TechLessonTitle"), new Vector2(1400, 960));
            var ink = Palette.Text;
            void Heading(string text, float y)
            {
                var t = UIKit.Label(page, text, 26, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(60 + 640, y), new Vector2(1280, 40));
            }
            void Formula(string name, float x, float y, float scale = 0.55f)
            {
                var f = UIKit.Formula(page, name, ink, scale).rectTransform;
                f.anchorMin = f.anchorMax = new Vector2(0, 1);
                f.anchoredPosition = new Vector2(x, y);
            }
            void Line(string text, float y, int size = 20)
            {
                var t = UIKit.Label(page, text, size, ink, TextAnchor.MiddleLeft, FontStyle.Italic);
                UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(60 + 640, y), new Vector2(1280, 56));
            }
            Heading(data.Tech("mg2TechCollide"), -125);
            Formula("mg2_bgk", 90, -195);
            Formula("mg2_feq", 90, -275);
            Line(data.Tech("mg2TechCollideNote", level.Lesson.Tau), -335);
            Heading(data.Tech("mg2TechStream"), -395);
            Formula("mg2_stream", 90, -465);
            Line(data.Tech("mg2TechStreamNote"), -525);
            Heading(data.Tech("mg2TechMoments"), -585);
            Formula("mg2_moments", 90, -655);
            Line(data.Tech("mg2TechConservation"), -720);
            Line(data.Tech("mg2TechTable"), -790);
            Line(data.Tech("mg2TechLessonValues"), -850);
        }
    }
}
