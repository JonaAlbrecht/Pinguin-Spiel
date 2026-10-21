using System.Collections;
using SFE.SimCore.Thermal;
using UnityEngine;
using UnityEngine.UI;

namespace SFE.Game
{
    /// <summary>
    /// Ende von MG1 Schritt 2: Die Waage bekommt Zahlen. Zwei Felder nebeneinander – Laterne und Iglu – tauschen Wärme,
    /// Zeitschritt für Zeitschritt, bis sich fast nichts mehr ändert (der Gleichgewichtspunkt, den der Computer danach
    /// für die ganze Siedlung sucht). Die Zahlen kommen aus <see cref="SettlementPairLesson"/> (Golden <c>pair_settlement</c>).
    /// Die Formel steht nur im Modus „Blick unter die Haube“ (docs/00: keine Formeln im Standardspiel).
    /// Bis 18.10.2026 stand dieselbe Tafel als Zwei-Pinguine-Beispiel im Prolog „Der letzte Huddle“.
    /// </summary>
    public static class PairLessonView
    {
        public static IEnumerator Play(GameRoot root, SettlementBoard board, SettlementLevel level)
        {
            var lesson = level.PairLesson;
            if (lesson == null) yield break;
            lesson.Reset();
            var data = root.Data;
            var hud = root.Hud;

            // Zwei Felder in der Mitte des Bretts: links die Laterne, rechts das Iglu
            int cx = level.Width / 2 - 1, cy = level.Height / 2;
            int[] cells = { cy * level.Width + cx, cy * level.Width + cx + 1 };
            var grid = new SettlementGrid(level.Width, level.Height);
            grid[cx, cy] = Cell.Lantern;
            grid[cx + 1, cy] = Cell.Igloo;
            board.Clear();
            board.Show(grid);
            var heat = new double[level.Width * level.Height];
            var mid = (board.CellCenter(cells[0]) + board.CellCenter(cells[1])) / 2;
            root.Rig.LookAt(mid + new Vector3(0, 0, 1.1f), 50f, 6.5f, 0f);

            string[] names = { data.Ui("pairLantern"), data.Ui("pairIgloo") };
            var heads = new TextMesh[2];
            for (int i = 0; i < 2; i++)
            {
                heads[i] = Prims.Label(board.transform, "", board.CellCenter(cells[i]) - board.transform.position + new Vector3(0, 1.5f, 0), 0.03f, Palette.Text);
                heads[i].fontStyle = FontStyle.Bold;
            }

            // Rechentafel oben: Bild-Gleichung + je eine Zeile für Laterne und Iglu
            var layer = new GameObject("PairLesson");
            var rt = UIKit.Rect(layer, hud.Root);
            UIKit.Fill(rt);
            rt.SetSiblingIndex(0);
            var panel = UIKit.Panel(rt, Palette.PanelLight, "Board");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -330), new Vector2(1500, 420));
            var title = UIKit.Label(panel.transform, data.Ui("pairTitle"), 30, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.PlaceFixed(title.rectTransform, new Vector2(0, 1), new Vector2(430, -35), new Vector2(800, 44));
            var stepLabel = UIKit.Label(panel.transform, "", 24, Palette.TextMuted, TextAnchor.MiddleRight);
            UIKit.PlaceFixed(stepLabel.rectTransform, new Vector2(1, 1), new Vector2(-180, -35), new Vector2(320, 44));

            // Spalten: Name | vorher | + | selbst | + | Nachbar | - | Wind | ergibt | neu
            // „ergibt“ braucht mehr Platz als + und - (brach sonst um: „ergib/t“, Spieltest 17.10.)
            float[] colX = { 90, 270, 375, 480, 585, 690, 795, 900, 1025, 1150 };
            string[] heads0 = { "", data.Ui("pairBefore"), data.Ui("pairPlus"), data.Ui("pairOwn"), data.Ui("pairPlus"), data.Ui("pairNeighbour"),
                data.Ui("pairMinus"), data.Ui("pairWind"), data.Ui("pairResult"), data.Ui("pairAfter") };
            Color[] tint = { Color.clear, Palette.TableHead, Color.clear, HeatColors.Comfy, Color.clear, Palette.Thread,
                Color.clear, HeatColors.Cold, Color.clear, Palette.TableHead };
            for (int c = 1; c < colX.Length; c++)
            {
                bool op = tint[c] == Color.clear;
                if (!op)
                {
                    var box = UIKit.Panel(panel.transform, tint[c], "Head");
                    UIKit.PlaceFixed(box.rectTransform, new Vector2(0, 1), new Vector2(colX[c] + 40, -100), new Vector2(170, 56));
                }
                var t = UIKit.Label(panel.transform, heads0[c], c == 8 ? 22 : op ? 30 : 21, Palette.Text, TextAnchor.MiddleCenter, op ? FontStyle.Bold : FontStyle.Normal);
                UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(colX[c] + 40, -100), new Vector2(op ? 80 : 165, 56));
            }
            var table = new Text[2, 10];
            for (int r = 0; r < 2; r++)
            {
                float y = -165 - r * 58;
                var n = UIKit.Label(panel.transform, names[r], 24, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.PlaceFixed(n.rectTransform, new Vector2(0, 1), new Vector2(colX[0] + 40, y), new Vector2(170, 50));
                foreach (int c in new[] { 1, 3, 5, 7, 9 })
                {
                    table[r, c] = UIKit.Label(panel.transform, data.Ui("pairUnknown"), 28, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIKit.PlaceFixed(table[r, c].rectTransform, new Vector2(0, 1), new Vector2(colX[c] + 40, y), new Vector2(170, 50));
                }
            }
            var note = UIKit.Label(panel.transform, "", 22, Palette.TextWarm, TextAnchor.MiddleLeft, FontStyle.Italic);
            UIKit.PlaceFixed(note.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -280), new Vector2(1420, 44));

            // Blick unter die Haube: die Formel hinter der Bild-Gleichung im eigenen Fenster (optional, docs/00)
            var techBtn = UIKit.Button(panel.transform, data.Ui("techButton"), () => ShowFormula(root, lesson), Palette.ButtonQuiet, 20);
            UIKit.PlaceFixed((RectTransform)techBtn.transform, new Vector2(0, 0), new Vector2(180, 38), new Vector2(300, 52));
            hud.TechOnly(techBtn.gameObject);
            hud.LockWhileBlocking(techBtn);

            bool go = false;
            var next = UIKit.Button(panel.transform, data.Ui("pairNext"), () => go = true, Palette.Button, 26);
            UIKit.PlaceFixed((RectTransform)next.transform, new Vector2(1, 0), new Vector2(-190, 38), new Vector2(340, 60));
            // Solange jemand spricht, ist der Knopf ausgegraut – weiter geht es dann nur in der Sprechblase.
            hud.LockWhileBlocking(next);

            void ShowState(int step)
            {
                stepLabel.text = data.Ui("pairStep", step, lesson.Steps);
                for (int i = 0; i < 2; i++)
                {
                    heads[i].text = data.Ui("pairHead", names[i], lesson.Temperatures[i]);
                    heat[cells[i]] = lesson.Temperatures[i];
                }
                board.SetHeat(heat);
            }

            for (int i = 0; i < heat.Length; i++) heat[i] = level.AirTemp;
            ShowState(0);
            for (int i = 0; i < 2; i++) table[i, 1].text = data.Ui("pairValue", lesson.Temperatures[i]);
            yield return hud.Say("pair_intro");

            for (int step = 1; step <= lesson.Steps; step++)
            {
                go = false;
                while (!go) yield return null;
                Sfx.Play(Sfx.Sound.Click);
                var before = (double[])lesson.Temperatures.Clone();
                lesson.Step();
                var terms = lesson.LastStepTerms();
                // Zeile füllt sich von links nach rechts – erst die Teile, dann das Ergebnis
                for (int i = 0; i < 2; i++)
                {
                    table[i, 1].text = data.Ui("pairValue", before[i]);
                    table[i, 3].text = table[i, 5].text = table[i, 7].text = table[i, 9].text = data.Ui("pairUnknown");
                }
                foreach (int c in new[] { 3, 5, 7, 9 })
                {
                    yield return new WaitForSeconds(0.35f);
                    for (int i = 0; i < 2; i++)
                        table[i, c].text = c == 3 ? data.Ui("pairSigned", terms.Own[i])
                            : c == 5 ? data.Ui("pairSigned", terms.FromNeighbour[i])
                            : c == 7 ? data.Ui("pairValue", terms.ToWind[i])
                            : data.Ui("pairValue", lesson.Temperatures[i]);
                    Sfx.Play(c == 9 ? Sfx.Sound.Ding : Sfx.Sound.Plop, 0.4f);
                }
                ShowState(step);
                int igloo = SettlementPairLesson.Igloo, lantern = SettlementPairLesson.Lantern;
                if (step == 1)
                {
                    note.text = data.Ui("pairConservation", names[igloo], terms.FromNeighbour[igloo], names[lantern], terms.FromNeighbour[lantern],
                        terms.FromNeighbour[igloo] + terms.FromNeighbour[lantern]);
                    yield return hud.Say("pair_conservation");
                }
                else note.text = data.Ui("pairChange", names[igloo], lesson.Temperatures[igloo] - before[igloo]);
            }
            next.gameObject.SetActive(false);
            yield return hud.Say("pair_done");

            Object.Destroy(layer);
            foreach (var h in heads) Object.Destroy(h.gameObject);
            board.SetHeat(null);
            board.Clear();
        }

        /// <summary>
        /// „Blick unter die Haube“ zur Rechenregel: Zellbilanz mit Speicherglied, Zeitschritt, welches Kästchen der
        /// Bild-Gleichung welcher Term ist, und die Werte des Beispiels. Formeln wie LaTeX gesetzt (<see cref="UIKit.Formula"/>).
        /// </summary>
        private static void ShowFormula(GameRoot root, SettlementPairLesson lesson)
        {
            var data = root.Data;
            // 1040 hoch: zwei Hinweiszeilen unter den Tabellen und darunter frei für den Schließen-Knopf
            // (bei 960 lag der Knopf über der letzten Zeile).
            var page = root.Hud.OpenPopup(data.Tech("techTitle"), new Vector2(1400, 1040));
            var ink = Palette.Text;

            void Heading(string text, float x, float y, float width)
            {
                var t = UIKit.Label(page, text, 26, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(x + width / 2, y), new Vector2(width, 40));
            }
            void Formula(string name, float x, float y, float scale = 0.5f)
            {
                var rt = UIKit.Formula(page, name, ink, scale).rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(x, y);
            }
            void Line(string text, float x, float y, float width, int size = 22, FontStyle style = FontStyle.Normal)
            {
                var t = UIKit.Label(page, text, size, ink, TextAnchor.MiddleLeft, style);
                UIKit.PlaceFixed(t.rectTransform, new Vector2(0, 1), new Vector2(x + width / 2, y), new Vector2(width, 56));
            }

            Heading(data.Tech("pairModel"), 60, -125, 1280);
            Formula("cell_ode", 90, -205, 0.6f);
            Heading(data.Tech("pairStep"), 60, -295, 1280);
            Formula("cell_step", 90, -370, 0.6f);
            Line(data.Tech("pairStepNote"), 60, -452, 1280, 20, FontStyle.Italic);

            // links: Kästchen der Bild-Gleichung → Term der Formel (gleiche Farben wie auf der Rechentafel)
            Heading(data.Tech("pairTerms"), 60, -515, 600);
            var terms = new (string ui, Color tint, string formula)[]
            {
                ("pairBefore", Palette.TableHead, "term_before"), ("pairOwn", HeatColors.Comfy, "term_own"),
                ("pairNeighbour", Palette.Thread, "term_neighbour"), ("pairWind", HeatColors.Cold, "term_wind"),
                ("pairAfter", Palette.TableHead, "term_after"),
            };
            for (int r = 0; r < terms.Length; r++)
            {
                float y = -570 - r * 62;
                var chip = UIKit.Panel(page, terms[r].tint, "Chip");
                UIKit.PlaceFixed(chip.rectTransform, new Vector2(0, 1), new Vector2(155, y), new Vector2(190, 50));
                var label = UIKit.Label(chip.transform, data.Ui(terms[r].ui), 20, ink);
                UIKit.Fill(label.rectTransform);
                Formula(terms[r].formula, 275, y, 0.42f);
            }

            // rechts: Werte des Zwei-Felder-Beispiels, direkt aus den Leveldaten
            Heading(data.Tech("pairParams"), 700, -515, 640);
            var values = new (string formula, string meaning, string value)[]
            {
                ("sym_dt", "pairParamDt", data.Tech("pairParamValue", lesson.Dt)),
                ("sym_c", "pairParamC", data.Tech("pairParamValue", lesson.Capacity)),
                ("sym_q", "pairParamQ", data.Tech("pairParamQValue", lesson.LanternHeat, lesson.IglooHeat)),
                ("sym_k", "pairParamK", data.Tech("pairParamValue", lesson.K)),
                ("sym_h", "pairParamH", data.Tech("pairParamValue", lesson.H)),
                ("sym_air", "pairParamAir", data.Tech("pairParamValue", lesson.AirTemp)),
            };
            for (int r = 0; r < values.Length; r++)
            {
                float y = -565 - r * 52;
                Formula(values[r].formula, 700, y, 0.42f);
                Line(data.Tech(values[r].meaning), 775, y, 250, 20);
                Line(values[r].value, 1030, y, 310, 20, FontStyle.Bold);
            }
            Line(data.Tech("pairSteadyNote"), 60, -868, 1280, 20, FontStyle.Italic);
            // Antwort auf Pips „jeder Schritt bringt weniger“ (pair_done) in der Sprache der Technik
            Line(data.Tech("pairFlowNote"), 60, -925, 1280, 20, FontStyle.Italic);
        }
    }
}
