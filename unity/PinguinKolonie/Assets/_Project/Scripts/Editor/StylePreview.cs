using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFE.Game.EditorTools
{
    /// <summary>
    /// Stilprobe: stellt alle Figuren und die Bausteine der Siedlung nach den aktuellen Stilregeln (ArtStyle) auf und
    /// speichert vier Bilder nach <c>unity/PinguinKolonie/Stilprobe/</c>. Damit sieht man eine Änderung an
    /// Schattierung, Detailgrad oder Proportionen, ohne das Spiel durchzuklicken (docs/07_Stilregeln.md).
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
                Prims.Make(PrimitiveType.Cube, world, new Vector3(0, -0.05f, 0), new Vector3(40f, 0.1f, 40f), Palette.Ice, "Ground");

                var cam = new GameObject("Camera").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Palette.Sky;
                cam.fieldOfView = 30f;
                cam.allowMSAA = true;

                var lineup = Group(world, "Figuren");
                var looks = (Prims.Look[])Enum.GetValues(typeof(Prims.Look));
                float step = 0.95f, x0 = -(looks.Length) * step / 2f;
                for (int i = 0; i < looks.Length; i++)
                    Face(Prims.Penguin(lineup, looks[i].ToString(), looks[i], 1.3f), new Vector3(x0 + i * step, 0, 0));
                Face(Hologram.Create(lineup, 1.3f), new Vector3(x0 + looks.Length * step, 0, 0));
                Shoot(cam, lineup, new Vector3(0, 0.6f, 0), 9.5f, 22f, 0f, Path.Combine(dir, "figuren.png"));

                var turn = Group(world, "Pinguin");
                float[] angles = { 0f, 35f, 90f, 180f };
                for (int i = 0; i < angles.Length; i++)
                {
                    var p = Prims.Penguin(turn, "Plain" + i, Prims.Look.Plain, 1f).transform;
                    p.localPosition = new Vector3((i - 1.5f) * 0.65f, 0, 0);
                    p.localRotation = Quaternion.Euler(0, 180f + angles[i], 0);
                }
                Shoot(cam, turn, new Vector3(0, 0.4f, 0), 3.6f, 10f, 0f, Path.Combine(dir, "pinguin.png"));

                var village = Group(world, "Siedlung");
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

        /// <summary>Zeigt nur <paramref name="show"/> und rendert es aus Abstand, Höhenwinkel und Seitenwinkel.</summary>
        private static void Shoot(Camera cam, Transform show, Vector3 target, float distance, float pitch, float yaw, string file)
        {
            foreach (Transform g in show.parent)
                if (g.name != "Ground") g.gameObject.SetActive(g == show);
            var rot = Quaternion.Euler(pitch, yaw, 0);
            cam.transform.position = target - rot * Vector3.forward * distance;
            cam.transform.rotation = rot;

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
            cam.targetTexture = rt;
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
