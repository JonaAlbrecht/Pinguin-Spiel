using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFE.Game.EditorTools
{
    /// <summary>
    /// Stilprobe: stellt alle Figuren, die Bausteine der Siedlung und die Oberflächenfarben nach den aktuellen
    /// Stilregeln (ArtStyle, Palette) auf und speichert fünf Bilder nach <c>unity/PinguinKolonie/Stilprobe/</c>.
    /// Damit sieht man eine Änderung an Farben, Schattierung, Detailgrad oder Proportionen, ohne das Spiel
    /// durchzuklicken (docs/07_Stilregeln.md).
    /// <c>Unity -batchmode -quit -projectPath unity/PinguinKolonie -executeMethod SFE.Game.EditorTools.StylePreview.Render</c>
    /// </summary>
    public static class StylePreview
    {
        private const int Width = 1600, Height = 900;

        [MenuItem("Pinguin-Spiel/Stilprobe rendern", priority = 20)]
        public static void Render()
        {
            // Eine eigene, zusätzliche Szene: die offene Szene des Nutzers bleibt unberührt. Neben einer unbenannten
            // Szene (Batchmode, frisch geöffneter Editor) erlaubt Unity keine zusätzliche – dann ersetzt die Probe sie.
            var previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            bool additive = !string.IsNullOrEmpty(previous.path);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Stilprobe"));
            Directory.CreateDirectory(dir);
            try
            {
                var world = new GameObject("Stilprobe").transform;
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                ArtStyle.Shading.ApplyTo(sun);

                var cam = new GameObject("Camera").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Palette.Sky;
                cam.fieldOfView = 30f;
                cam.allowMSAA = true;

                var lineup = Group(world, "Figuren");
                Ground(lineup);
                var looks = (Prims.Look[])Enum.GetValues(typeof(Prims.Look));
                float step = 0.95f, x0 = -(looks.Length) * step / 2f;
                for (int i = 0; i < looks.Length; i++)
                    Face(Prims.Penguin(lineup, looks[i].ToString(), looks[i], 1.3f), new Vector3(x0 + i * step, 0, 0));
                Face(Hologram.Create(lineup, 1.3f), new Vector3(x0 + looks.Length * step, 0, 0));
                Shoot(cam, lineup, new Vector3(0, 0.6f, 0), 9.5f, 22f, 0f, Path.Combine(dir, "figuren.png"));

                var turn = Group(world, "Pinguin");
                Ground(turn);
                float[] angles = { 0f, 35f, 90f, 180f };
                for (int i = 0; i < angles.Length; i++)
                {
                    var p = Prims.Penguin(turn, "Plain" + i, Prims.Look.Plain, 1f).transform;
                    p.localPosition = new Vector3((i - 1.5f) * 0.65f, 0, 0);
                    p.localRotation = Quaternion.Euler(0, 180f + angles[i], 0);
                }
                Shoot(cam, turn, new Vector3(0, 0.4f, 0), 3.6f, 10f, 0f, Path.Combine(dir, "pinguin.png"));

                // Siedlung auf einer Scholle im Meer wie im Hub (HubWorld): zeigt auch, wie große Rundungen facettiert werden.
                var village = Group(world, "Siedlung");
                Prims.Make(PrimitiveType.Cylinder, village, new Vector3(0, -0.6f, 0), new Vector3(120, 0.1f, 120), Palette.Sea, "Sea");
                Prims.Make(PrimitiveType.Cylinder, village, new Vector3(0, -0.25f, 0), new Vector3(9f, 0.25f, 9f), Palette.Ice, "Floe");
                Prims.Make(PrimitiveType.Cylinder, village, new Vector3(0, 0.005f, -0.6f), new Vector3(3.2f, 0.01f, 3.2f), Palette.IceShadow, "Plaza");
                for (int i = 0; i < 5; i++)
                {
                    float a = 2.2f + i * 0.45f, s = 0.4f + 0.12f * (i % 3);
                    Prims.Make(PrimitiveType.Sphere, village, new Vector3(Mathf.Cos(a) * 3.8f, 0, Mathf.Sin(a) * 3.8f),
                        new Vector3(s * 1.4f, s * 0.7f, s), Palette.Snow, "SnowPile");
                }
                Prims.Igloo(village, 1.6f).transform.localPosition = new Vector3(-1.6f, 0, 0);
                Prims.Lantern(village, 1.2f, false).transform.localPosition = new Vector3(-0.2f, 0, 0.4f);
                Prims.SnowWall(village, 1f).transform.localPosition = new Vector3(0.9f, 0, -0.2f);
                Prims.Puddle(village, 1f).transform.localPosition = new Vector3(2.0f, 0, 0.5f);
                Face(Prims.Penguin(village, "Pip", Prims.Look.Pip, 1.25f), new Vector3(-0.6f, 0, -1.0f));
                Face(Prims.Penguin(village, "Grete", Prims.Look.Grete, 1.3f), new Vector3(0.3f, 0, -1.1f));
                Face(Prims.Penguin(village, "Flocke", Prims.Look.Flocke, 0.8f), new Vector3(0.75f, 0, -1.3f));
                Shoot(cam, village, new Vector3(0, 0.4f, -0.3f), 8f, 28f, 20f, Path.Combine(dir, "siedlung.png"));
                // So groß wie im Hub (CameraRig.Follow: 50 Grad, 24 m): Was hier nicht mehr zu sehen ist, lohnt kein Detail.
                Shoot(cam, village, new Vector3(0, 0.4f, -0.3f), 24f, 50f, 0f, Path.Combine(dir, "spielansicht.png"));

                ShootUi(cam, world, Path.Combine(dir, "oberflaeche.png"));
                Debug.Log("Stilprobe gespeichert: " + dir);
            }
            finally
            {
                if (additive)
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                }
            }
        }

        /// <summary>Figuren schauen zur Kamera; die Spielkamera blickt von −Z nach +Z (CameraRig, Gierwinkel 0).</summary>
        private static void Face(GameObject go, Vector3 pos)
        {
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        }

        private static Transform Group(Transform world, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(world, false);
            return t;
        }

        private static void Ground(Transform group) =>
            Prims.Make(PrimitiveType.Cube, group, new Vector3(0, -0.05f, 0), new Vector3(40f, 0.1f, 40f), Palette.Ice, "Ground");

        /// <summary>Zeigt nur <paramref name="show"/> und rendert es aus Abstand, Höhenwinkel und Seitenwinkel.</summary>
        private static void Shoot(Camera cam, Transform show, Vector3 target, float distance, float pitch, float yaw, string file)
        {
            foreach (Transform g in show.parent) g.gameObject.SetActive(g == show);
            var rot = Quaternion.Euler(pitch, yaw, 0);
            cam.transform.position = target - rot * Vector3.forward * distance;
            cam.transform.rotation = rot;
            Capture(cam, file);
        }

        /// <summary>
        /// Oberflächenfarben auf einen Blick: Titel und Knöpfe, helles Dialogfenster mit allen Sprechernamen,
        /// dunkles Fenster mit Ampeln, die Wärmeskala und die Siemens-Primärfarben. Beschriftet mit den Namen
        /// der Einträge in Palette.cs, damit man weiß, wo man dreht.
        /// </summary>
        private static void ShootUi(Camera cam, Transform world, string file)
        {
            foreach (Transform g in world) g.gameObject.SetActive(false);
            var canvas = UIKit.CreateCanvas("Oberflaeche", 0);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            var root = canvas.transform;
            UIKit.Fill(UIKit.Panel(root, Palette.Page, nameof(Palette.Page)).rectTransform);

            var title = UIKit.Panel(root, Palette.TitleBackground, nameof(Palette.TitleBackground));
            UIKit.Place(title.rectTransform, 0, 0.6f, 1, 1);
            UIKit.Place(UIKit.Label(title.transform, nameof(Palette.TitleBackground), 56, Palette.TextLight, TextAnchor.UpperLeft, FontStyle.Bold).rectTransform,
                0, 0, 1, 1, 48, 0, 48, 30);
            UIKit.Place(UIKit.Label(title.transform, nameof(Palette.TitleSub), 32, Palette.TitleSub, TextAnchor.UpperLeft).rectTransform,
                0, 0, 1, 1, 48, 0, 48, 105);
            var buttons = new[]
            {
                (nameof(Palette.Button), Palette.Button), (nameof(Palette.ButtonAlt), Palette.ButtonAlt),
                (nameof(Palette.ButtonQuiet), Palette.ButtonQuiet), (nameof(Palette.ButtonSide), Palette.ButtonSide),
                (nameof(Palette.ButtonOff), Palette.ButtonOff),
            };
            for (int i = 0; i < buttons.Length; i++)
                UIKit.PlaceFixed(UIKit.Button(title.transform, buttons[i].Item1, null, buttons[i].Item2, 26).GetComponent<RectTransform>(),
                    new Vector2(0, 0), new Vector2(170 + i * 330, 130), new Vector2(300, 70));
            var primaries = new[]
            {
                Palette.Siemens.Petrol, Palette.Siemens.LightPetrol, Palette.Siemens.BoldGreen, Palette.Siemens.SoftGreen,
                Palette.Siemens.BoldBlue, Palette.Siemens.SoftBlue, Palette.Siemens.DeepBlue, Palette.Siemens.LightSand,
            };
            for (int i = 0; i < primaries.Length; i++)
                UIKit.PlaceFixed(UIKit.Panel(title.transform, primaries[i], "Primary").rectTransform,
                    new Vector2(1, 1), new Vector2(-560 + i * 70, -80), new Vector2(62, 62));

            var dialog = UIKit.Panel(root, Palette.PanelLight, nameof(Palette.PanelLight));
            UIKit.Place(dialog.rectTransform, 0.03f, 0.06f, 0.47f, 0.55f);
            var data = GameData.Load();
            int row = 0;
            foreach (var sp in data.Speakers.Values)
            {
                var t = UIKit.Label(dialog.transform, sp.Name, 30, sp.Color, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Place(t.rectTransform, 0, 0, 0.5f, 1, 30, 0, 10, 24 + (row % 4) * 52);
                if (row >= 4) UIKit.Place(t.rectTransform, 0.5f, 0, 1, 1, 10, 0, 30, 24 + (row % 4) * 52);
                row++;
            }
            UIKit.Place(UIKit.Label(dialog.transform, nameof(Palette.Text), 30, Palette.Text, TextAnchor.LowerLeft).rectTransform, 0, 0, 0.5f, 1, 30, 20, 0, 0);
            UIKit.Place(UIKit.Label(dialog.transform, nameof(Palette.TextMuted), 26, Palette.TextMuted, TextAnchor.LowerRight).rectTransform, 0.5f, 0, 1, 1, 0, 20, 30, 0);

            var panel = UIKit.Panel(root, Palette.Panel, nameof(Palette.Panel));
            UIKit.Place(panel.rectTransform, 0.53f, 0.30f, 0.97f, 0.55f);
            var lamps = new[] { (Palette.Good, "OK"), (Palette.Warn, "!"), (Palette.Bad, "X") };
            for (int i = 0; i < lamps.Length; i++)
            {
                var lamp = UIKit.Panel(panel.transform, lamps[i].Item1, "Lamp");
                UIKit.PlaceFixed(lamp.rectTransform, new Vector2(0, 0.5f), new Vector2(70 + i * 110, 0), new Vector2(80, 80));
                UIKit.Fill(UIKit.Label(lamp.transform, lamps[i].Item2, 30, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform);
            }
            var sel = UIKit.Panel(panel.transform, Palette.Selected, nameof(Palette.Selected));
            UIKit.PlaceFixed(sel.rectTransform, new Vector2(0, 0.5f), new Vector2(400, 0), new Vector2(80, 80));
            UIKit.Place(UIKit.Label(panel.transform, nameof(Palette.TextTechLight), 26, Palette.TextTechLight, TextAnchor.MiddleRight).rectTransform,
                0.5f, 0, 1, 1, 0, 0, 24, 0);

            var heat = UIKit.Panel(root, Palette.PanelLight, "Heat");
            UIKit.Place(heat.rectTransform, 0.53f, 0.06f, 0.97f, 0.26f);
            const int slices = 48;
            for (int i = 0; i < slices; i++)
            {
                double t = -30 + 45.0 * i / (slices - 1);
                var s = UIKit.Panel(heat.transform, HeatColors.For(t, -30, -8, 0), "Slice");
                UIKit.Place(s.rectTransform, (float)i / slices, 0, (float)(i + 1) / slices, 1, 0, 16, 0, 16);
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Page;
            Capture(cam, file);
            Prims.Kill(canvas.gameObject);
        }

        private static void Capture(Camera cam, string file)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = old;
            cam.targetTexture = null;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
