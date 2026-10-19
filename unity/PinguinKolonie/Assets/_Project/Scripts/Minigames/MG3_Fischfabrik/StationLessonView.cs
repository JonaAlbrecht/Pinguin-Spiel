using System.Collections;
using System.Collections.Generic;
using SFE.SimCore.Process;
using UnityEngine;
using UnityEngine.UI;

namespace SFE.Game
{
    /// <summary>
    /// MG3 Schritt 2, nach dem Supermarkt-Bild: die Rechentafel wie in MG1 (<see cref="PairLessonView"/>). Eine
    /// Filetier-Station mit Kühlregal; Ereignis für Ereignis füllt sich eine Zeile
    /// „Kisten vorher + kommen an − werden fertig = Kisten neu“. Die Zahlen kommen aus derselben Simulation wie die ganze
    /// Fabrik (<see cref="StationLesson"/>, Golden <c>lesson</c>). Formeln nur im Modus „Blick unter die Haube“.
    /// </summary>
    public static class StationLessonView
    {
        private const int VisibleRows = 4;

        public static IEnumerator Play(GameRoot root, FishFactoryBoard board, ProcessLevel level)
        {
            if (level.Lesson == null) yield break;
            var data = root.Data;
            var hud = root.Hud;
            var lesson = new StationLesson(level.Lesson);

            // Auf dem Bildschirm nur eine Station: Kühlregal links, Filetieren rechts, mitten in der Halle
            var layout = FactoryLayout.FromRows(level.InitialTiles);
            int cy = level.Height / 2, cx = level.Width / 2;
            if (layout[cx, cy] != FactoryLayout.Floor) cx--;
            layout[cx - 1, cy] = FactoryLayout.Regal;
            layout[cx, cy] = 'F';
            board.Clear();
            board.Show(layout);
            int fCell = cy * level.Width + cx;
            var mid = (board.CellCenter(fCell) + board.CellCenter(fCell - 1)) / 2;
            root.Rig.LookAt(mid + new Vector3(0, 0, 1.4f), 50f, 7.5f, 0f);

            // Rechentafel oben
            var layer = new GameObject("MG3_Lesson");
            var rt = UIKit.Rect(layer, hud.Root);
            UIKit.Fill(rt);
            rt.SetSiblingIndex(0);
            var panel = UIKit.Panel(rt, Palette.PanelLight, "Board");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -345), new Vector2(1500, 470));
            var title = UIKit.Label(panel.transform, data.Ui("mg3LessonTitle"), 30, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.PlaceFixed(title.rectTransform, new Vector2(0, 1), new Vector2(440, -35), new Vector2(820, 44));
            var stepLabel = UIKit.Label(panel.transform, "", 24, Palette.TextMuted, TextAnchor.MiddleRight);
            UIKit.PlaceFixed(stepLabel.rectTransform, new Vector2(1, 1), new Vector2(-180, -35), new Vector2(320, 44));

            // Spalten: Uhr | Was passiert | vorher | + | kommen an | - | werden fertig | ergibt | neu
            float[] colX = { 110, 290, 500, 610, 720, 830, 940, 1065, 1190 };
            float[] colW = { 120, 230, 170, 50, 170, 50, 170, 100, 170 };
            string[] heads =
            {
                data.Ui("mg3LessonTime"), data.Ui("mg3LessonWhat"), data.Ui("mg3LessonBefore"), data.Ui("pairPlus"), data.Ui("mg3LessonIn"),
                data.Ui("pairMinus"), data.Ui("mg3LessonOut"), data.Ui("pairResult"), data.Ui("mg3LessonAfter"),
            };
            Color[] tint =
            {
                Color.clear, Color.clear, Palette.TableHead, Color.clear, Palette.Mg3LampBusy, Color.clear, Palette.Mg3Arrow, Color.clear,
                Palette.TableHead,
            };
            for (int c = 0; c < colX.Length; c++)
            {
                bool box = tint[c] != Color.clear;
                if (box)
                {
                    var head = UIKit.Panel(panel.transform, Palette.Tint(tint[c], 0.45f), "Head");
                    UIKit.PlaceFixed(head.rectTransform, new Vector2(0, 1), new Vector2(colX[c], -100), new Vector2(colW[c], 56));
                }
                bool op = c == 3 || c == 5;
                var t = UIKit.Label(panel.transform, heads[c], op ? 30 : 21, Palette.Text, TextAnchor.MiddleCenter, op || !box ? FontStyle.Bold : FontStyle.Normal);
                UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(colX[c], -100), new Vector2(colW[c], 56));
            }
            var cells = new Text[VisibleRows, colX.Length];
            for (int r = 0; r < VisibleRows; r++)
            {
                float y = -162 - r * 50;
                for (int c = 0; c < colX.Length; c++)
                {
                    bool op = c == 3 || c == 5 || c == 7;
                    cells[r, c] = UIKit.Label(panel.transform, "", c == 1 ? 22 : 26, op ? Palette.TextMuted : Palette.Text, TextAnchor.MiddleCenter,
                        c == 8 ? FontStyle.Bold : FontStyle.Normal);
                    UIKit.PlaceFixed(cells[r, c].rectTransform, new Vector2(0, 1), new Vector2(colX[c], y), new Vector2(colW[c], 46));
                }
            }
            var note = UIKit.Label(panel.transform, "", 22, Palette.TextWarm, TextAnchor.MiddleLeft, FontStyle.Italic);
            UIKit.PlaceFixed(note.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -375), new Vector2(1420, 44));
            var stack = UIKit.Label(panel.transform, "", 22, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.PlaceFixed(stack.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(560, 50));

            var techBtn = UIKit.Button(panel.transform, data.Ui("techButton"), () => ShowFormula(root, level), Palette.ButtonQuiet, 20);
            UIKit.PlaceFixed((RectTransform)techBtn.transform, new Vector2(0, 0), new Vector2(180, 40), new Vector2(300, 52));
            hud.TechOnly(techBtn.gameObject);
            hud.LockWhileBlocking(techBtn);

            bool go = false;
            var next = UIKit.Button(panel.transform, data.Ui("mg3LessonNext"), () => go = true, Palette.Button, 26);
            UIKit.PlaceFixed((RectTransform)next.transform, new Vector2(1, 0), new Vector2(-190, 40), new Vector2(340, 60));
            hud.LockWhileBlocking(next);

            var history = new List<LessonRow>();
            void ShowRows(int filledColumnsOfLast)
            {
                int first = Mathf.Max(0, history.Count - VisibleRows);
                for (int r = 0; r < VisibleRows; r++)
                {
                    int i = first + r;
                    for (int c = 0; c < colX.Length; c++)
                    {
                        if (i >= history.Count) { cells[r, c].text = ""; continue; }
                        bool last = i == history.Count - 1;
                        cells[r, c].text = last && c > filledColumnsOfLast ? (c == 3 || c == 5 || c == 7 ? "" : data.Ui("pairUnknown"))
                            : Cell(data, level, history[i], c);
                    }
                }
            }
            void ShowBoard()
            {
                stepLabel.text = data.Ui("mg3LessonStep", lesson.Step, lesson.Events);
                stack.text = data.Ui("mg3LessonStack", lesson.InStation, lesson.Waiting);
                board.ShowStation('F', lesson.Waiting, lesson.Working);
            }
            ShowBoard();
            yield return hud.Say("mg3_lesson_intro");

            bool conservationShown = false, queueShown = false;
            while (lesson.Step < lesson.Events)
            {
                go = false;
                while (!go) yield return null;
                Sfx.Play(Sfx.Sound.Click);
                var row = lesson.Next();
                history.Add(row);
                // Zeile füllt sich von links nach rechts: erst was passiert, dann die Teile, dann das Ergebnis
                for (int c = 1; c < colX.Length; c++)
                {
                    ShowRows(c);
                    if (c == 2 || c == 4 || c == 6 || c == 8)
                    {
                        Sfx.Play(c == 8 ? Sfx.Sound.Ding : Sfx.Sound.Plop, 0.4f);
                        yield return new WaitForSeconds(0.3f);
                    }
                }
                ShowBoard();
                if (!queueShown && row.After >= 3)
                {
                    queueShown = true;
                    yield return hud.Say("mg3_lesson_queue");
                }
                if (!conservationShown && row.Finished > 0)
                {
                    conservationShown = true;
                    note.text = data.Ui("mg3LessonConservation");
                    yield return hud.Say("mg3_lesson_conservation");
                }
            }
            next.gameObject.SetActive(false);
            yield return hud.Say("mg3_lesson_done");

            Object.Destroy(layer);
            board.Clear();
        }

        private static string Cell(GameData data, ProcessLevel level, LessonRow r, int column)
        {
            switch (column)
            {
                case 0: return FishFactoryHall.Clock(data, level, r.Time);
                case 1: return r.IsArrival ? data.Ui("mg3LessonArrive") : data.Ui("mg3LessonFinished");
                case 2: return data.Ui("mg3LessonCount", r.Before);
                case 3: return data.Ui("pairPlus");
                case 4: return data.Ui("mg3LessonCount", r.Arrived);
                case 5: return data.Ui("pairMinus");
                case 6: return data.Ui("mg3LessonCount", r.Finished);
                case 7: return data.Ui("pairResult");
                default: return data.Ui("mg3LessonCount", r.After);
            }
        }

        /// <summary>
        /// „Blick unter die Haube“ zur Rechentafel: Bilanz je Ereignis, nächstes Ereignis aus der Liste, wie der Zufall
        /// gezogen wird, Littles Gesetz und die Werte des Beispiels. Formeln wie LaTeX gesetzt (<see cref="UIKit.Formula"/>).
        /// </summary>
        private static void ShowFormula(GameRoot root, ProcessLevel level)
        {
            var data = root.Data;
            var page = root.Hud.OpenPopup(data.Tech("techTitle"), new Vector2(1400, 900));
            var ink = Palette.Text;
            void Heading(string text, float y)
            {
                var t = UIKit.Label(page, text, 26, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(700, y), new Vector2(1280, 40));
            }
            void Formula(string name, float y, float scale = 0.55f)
            {
                var f = UIKit.Formula(page, name, ink, scale).rectTransform;
                f.anchorMin = f.anchorMax = new Vector2(0, 1);
                f.anchoredPosition = new Vector2(90, y);
            }
            Heading(data.Tech("mg3LessonModel"), -125);
            Formula("mg3_balance", -185);
            Heading(data.Tech("mg3LessonNextEvent"), -260);
            Formula("mg3_next_event", -322);
            Heading(data.Tech("mg3LessonDraw"), -400);
            Formula("mg3_exp", -460, 0.5f);
            Formula("mg3_tri", -540, 0.5f);
            Heading(data.Tech("mg3LessonLittle"), -625);
            Formula("mg3_little", -685);
            var l = level.Lesson;
            var p = UIKit.Label(page, data.Tech("mg3LessonParams", l.ArrivalMean, l.Low, l.Mode, l.High, l.Seed), 22, ink, TextAnchor.MiddleLeft);
            UIKit.PlaceFixed(p.rectTransform, new Vector2(0, 1), new Vector2(700, -770), new Vector2(1280, 44));
        }
    }
}
