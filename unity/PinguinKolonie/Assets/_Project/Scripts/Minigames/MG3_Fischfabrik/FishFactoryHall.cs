using System.Collections;
using SFE.SimCore.Data;
using SFE.SimCore.Process;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Kalles Versuch – in echt, in Olafs Halle: kein Raster, kein ZWILLI, Kamera flach wie ein Besucher (wie MG1
    /// <see cref="HubWorld.PlayKalleAttempt"/>, aber als eigene kleine Szene, damit HubWorld unverändert bleibt).
    /// Kalle verdoppelt die Robo-Robben; ein ganzer Arbeitstag läuft, die Kisten stapeln sich, die Robben stehen sich im
    /// Weg. Was passiert, bestimmt dasselbe Modell wie im Labor – die „Wirklichkeit“ des Spiels (Golden <c>day_kalle</c>).
    /// Am Ende ein Foto für den Vergleich in Schritt 4 (<see cref="FishFactoryState.HallPhoto"/>).
    /// </summary>
    public static class FishFactoryHall
    {
        public static IEnumerator Play(GameRoot root, Vector3 origin)
        {
            var data = root.Data;
            var hud = root.Hud;
            var level = FishFactoryState.Level;
            var content = data.MinigameContent("mg3");
            var scene = new GameObject("MG3_Hall");
            scene.transform.position = origin;
            var board = FishFactoryBoard.Create(scene.transform, origin, level, true, data.Ui("mg3HallSign"), content.Obj("stations"));
            board.Show(FactoryLayout.FromRows(level.OlafTiles));

            // Figuren vor der Halle (Süden), Blick hinein
            var front = origin + new Vector3(0, 0, -1.3f);
            // Halb zur Kamera gedreht: man sieht Gesichter und trotzdem, dass alle in die Halle schauen
            Figure(root, scene.transform, "olaf", Prims.Look.Plain, front + new Vector3(2.2f, 0, 0), 200f);
            Figure(root, scene.transform, "kalle", Prims.Look.Kalle, front + new Vector3(4.0f, 0, -0.2f), 170f);
            Figure(root, scene.transform, "pip", Prims.Look.Pip, front + new Vector3(6.0f, 0, -0.1f), 190f);
            Figure(root, scene.transform, "frieda", Prims.Look.Frieda, front + new Vector3(7.6f, 0, -0.3f), 160f);

            hud.ShowPhases(-1);
            root.Rig.LookAt(board.Center + new Vector3(0, 0, -1.6f), 40f, root.Rig.DistanceToFit(level.Width + 4f), 0f, snap: true);
            yield return hud.FadeIn();
            yield return hud.Say("mg3_hall_intro");
            yield return hud.Say("mg3_kalle_intro");

            // Kalle stellt die zusätzlichen Robo-Robben dazu – eine nach der anderen
            var kalle = FactoryLayout.FromRows(level.KalleTiles);
            var now = FactoryLayout.FromRows(level.OlafTiles);
            for (int i = 0; i < kalle.CellCount; i++)
            {
                if (kalle[i] == now[i]) continue;
                now[i] = kalle[i];
                board.Show(now);
                Sfx.Play(Sfx.Sound.Plop, 0.6f);
                yield return new WaitForSeconds(0.35f);
            }

            // Ein Arbeitstag im Zeitraffer, oben läuft die Uhr mit
            var layer = new GameObject("MG3_HallUi");
            var rt = UIKit.Rect(layer, hud.Root);
            UIKit.Fill(rt);
            rt.SetSiblingIndex(0);
            var bar = UIKit.Panel(rt, Palette.Panel, "Status");
            UIKit.PlaceFixed(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(1300, 64));
            var status = UIKit.Label(bar.transform, "", 28, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(status.rectTransform);

            var config = kalle.ToConfig(level);
            var sim = new FactorySimulation(config, level.Seed, trace: true);
            yield return board.Play(sim, 24f, level.ShiftLength, s => status.text = Status(data, level, s));
            var day = sim.Result();
            Sfx.Play(Sfx.Sound.Wrong, 0.5f);

            var banner = UIKit.Panel(rt, Palette.PanelLight, "Result");
            UIKit.PlaceFixed(banner.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 330), new Vector2(1200, 90));
            var text = UIKit.Label(banner.transform, data.Ui("mg3HallResult", day.Delivered, level.Target, day.Spoiled), 30, Palette.Text,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(text.rectTransform);
            yield return new WaitForSeconds(1.2f);

            if (FishFactoryState.HallPhoto != null) Object.Destroy(FishFactoryState.HallPhoto);
            FishFactoryState.HallPhoto = FishFactoryBoard.Photo(root.Rig.Cam);
            yield return hud.Say("mg3_kalle_after");
            yield return hud.FadeOut();
            Object.Destroy(layer);
            Object.Destroy(scene);
        }

        /// <summary>Uhr und Zähler einer laufenden Schicht – dieselbe Zeile im Labor.</summary>
        public static string Status(GameData data, ProcessLevel level, FactorySimulation sim) =>
            data.Ui("mg3Status", Clock(data, level, sim.Time), sim.Delivered, sim.Spoiled, sim.Stolen);

        public static string Clock(GameData data, ProcessLevel level, double minutes)
        {
            int total = (int)System.Math.Floor(level.ShiftStart + minutes);
            return data.Ui("mg3Clock", total / 60, total % 60);
        }

        /// <summary>Ein Pinguin mit Namensschild in der Farbe seiner Sprechblase.</summary>
        public static GameObject Figure(GameRoot root, Transform parent, string id, Prims.Look look, Vector3 at, float yaw = 0f)
        {
            var go = Prims.Penguin(parent, id, look, 1.3f);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            if (root.Data.Speakers.TryGetValue(id, out var sp))
            {
                var name = Prims.Label(go.transform, sp.Name, new Vector3(0, 1.1f, 0), 0.022f, sp.Color);
                name.fontStyle = FontStyle.Bold;
            }
            return go;
        }
    }
}
