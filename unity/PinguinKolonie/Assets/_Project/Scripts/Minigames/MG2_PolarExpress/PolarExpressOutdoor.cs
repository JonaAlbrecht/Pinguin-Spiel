using System.Collections;
using System.Collections.Generic;
using SFE.SimCore.Flow;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Draußen in der Tundra, in echt (kein Raster, kein ZWILLI, Kamera auf Augenhöhe) – wie Kalles Siedlung in MG1
    /// (<see cref="HubWorld.PlayKalleAttempt"/>), aber als eigene kleine Szene an eigenem Ort, damit der Hub unberührt
    /// bleibt. Kalles kastenförmiger Schlitten mit Motor und flacher Front kämpft gegen den Gegenwind und bleibt fast
    /// stehen; am Ende ein Foto für Schritt 4. Nach dem Minigame fährt hier Pips Zug (<see cref="PlayPlayerTrain"/>).
    /// Der Zug fährt nach links (−x), der Wind kommt von links – wie im Windkanal auf ZWILLIS Bildschirm.
    /// </summary>
    public sealed class PolarExpressOutdoor : MonoBehaviour
    {
        /// <summary>Fein-Einheit des Kanals in Metern draußen: Zughöhe 12 → knapp 1 m, Pinguine ~1 m.</summary>
        private const float Meter = 0.08f;

        private GameRoot _root;
        private readonly List<Transform> _flakes = new List<Transform>();
        private float _wind = 6f;
        private System.Random _rnd;

        public static PolarExpressOutdoor Create(GameRoot root, Vector3 origin)
        {
            var go = new GameObject("MG2_Tundra");
            go.transform.position = origin;
            var o = go.AddComponent<PolarExpressOutdoor>();
            o._root = root;
            o._rnd = new System.Random(5);
            o.Build();
            return o;
        }

        private void Build()
        {
            var t = transform;
            Prims.Make(PrimitiveType.Cylinder, t, new Vector3(0, -0.05f, 0), new Vector3(120, 0.1f, 120), Palette.Mg2Tundra, "Tundra");
            // Gleis: zwei Schienen auf Schwellen
            foreach (float z in new[] { -0.4f, 0.4f })
                Prims.Make(PrimitiveType.Cube, t, new Vector3(0, 0.06f, z), new Vector3(90, 0.07f, 0.07f), Palette.Mg2Rail, "Rail");
            for (float x = -45; x <= 45; x += 0.9f)
                Prims.Make(PrimitiveType.Cube, t, new Vector3(x, 0.02f, 0), new Vector3(0.22f, 0.04f, 1.2f), Palette.Mg2Sleeper, "Sleeper");
            for (int i = 0; i < 26; i++)
            {
                float x = -30 + (float)_rnd.NextDouble() * 60, z = 3 + (float)_rnd.NextDouble() * 14;
                if (i % 2 == 0) z = -z - 2f;
                float s = 0.6f + (float)_rnd.NextDouble() * 1.4f;
                Prims.Make(PrimitiveType.Sphere, t, new Vector3(x, 0, z), new Vector3(s * 1.5f, s * 0.6f, s), Palette.Snow, "SnowPile");
            }
            // Schneeflocken im Gegenwind
            for (int i = 0; i < 90; i++)
            {
                var f = Prims.Make(PrimitiveType.Cube, t, RandomFlake(true), Vector3.one * 0.06f, Palette.Mg2Flake, "Flake");
                _flakes.Add(f.transform);
            }
        }

        private Vector3 RandomFlake(bool anywhere)
        {
            float x = anywhere ? -14 + (float)_rnd.NextDouble() * 28 : -14f;
            return new Vector3(x, 0.2f + (float)_rnd.NextDouble() * 3f, -3f + (float)_rnd.NextDouble() * 6f);
        }

        private void Update()
        {
            // Wind von links nach rechts – kräftig, mit leichtem Wirbeln
            float dt = Time.deltaTime;
            foreach (var f in _flakes)
            {
                var p = f.localPosition;
                p.x += _wind * dt;
                p.y += Mathf.Sin(Time.time * 3f + p.z * 2f) * 0.4f * dt;
                if (p.x > 14f) p = RandomFlake(false);
                f.localPosition = p;
            }
        }

        private GameObject Stand(string name, Prims.Look look, Vector3 pos, float yaw, string speakerId)
        {
            var p = Prims.Penguin(transform, name, look, 1.3f);
            p.transform.localPosition = pos;
            p.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            if (speakerId != null && _root.Data.Speakers.TryGetValue(speakerId, out var sp))
            {
                var label = Prims.Label(p.transform, sp.Name, new Vector3(0, 1.15f, 0), 0.022f, sp.Color);
                label.fontStyle = FontStyle.Bold;
            }
            return p;
        }

        // ---------- Kalles Versuch ----------

        /// <summary>Kalles Schlitten in echt: flache Front, großer Motor – im Gegenwind fast Stillstand. Danach ein Foto.</summary>
        public IEnumerator PlayKalleAttempt(Mg2Progress progress)
        {
            var sled = BuildSled();
            sled.localPosition = new Vector3(3f, 0, 0);
            Stand("Tilda", Prims.Look.Tilda, new Vector3(-1.5f, 0, -2.2f), 160f, "tilda");
            var kalle = Stand("Kalle", Prims.Look.Kalle, new Vector3(4.5f, 0, -2.4f), 200f, null);
            Stand("Pip", Prims.Look.Pip, new Vector3(0.5f, 0, -2.8f), 180f, null);
            _root.Rig.LookAt(transform.position + new Vector3(1.2f, 0.8f, 0), 12f, 12f, 0f, snap: true);
            yield return _root.Hud.FadeIn();
            yield return _root.Hud.Say("mg2_kalle_intro");

            // Kalle wirft den Motor an: Der Schlitten ruckt los – und der Wind drückt ihn gegen die flache Front zurück
            Sfx.Play(Sfx.Sound.Bleep, 0.6f);
            var smoke = sled.Find("Chimney");
            float v = 0f;
            for (float t = 0; t < 6.5f; t += Time.deltaTime)
            {
                float push = t < 1.2f ? 1.6f : 0.4f;           // Anfahren, dann reicht die Kraft kaum gegen den Wind
                float drag = 0.9f * v * v + 0.25f * _wind * 0.1f; // flache Front: viel Widerstand
                v = Mathf.Max(0.05f, v + (push - drag) * Time.deltaTime);
                sled.localPosition += Vector3.left * v * Time.deltaTime;
                sled.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 30f) * 0.6f);      // rüttelt
                if (smoke != null) smoke.localScale = new Vector3(0.25f, 0.35f + 0.1f * Mathf.Sin(t * 12f), 0.25f);
                yield return null;
            }
            sled.localRotation = Quaternion.identity;
            kalle.transform.localRotation = Quaternion.Euler(0, 250, 0);
            yield return new WaitForSeconds(0.6f);
            if (progress.KallePhoto != null) Destroy(progress.KallePhoto);
            progress.KallePhoto = Photo();
            yield return _root.Hud.Say("mg2_kalle_after");
            yield return _root.Hud.FadeOut();
        }

        /// <summary>Kalles Schlitten: Kasten mit flacher Front, Motor obendrauf, Kufen.</summary>
        private Transform BuildSled()
        {
            var root = new GameObject("KalleSled").transform;
            root.SetParent(transform, false);
            Prims.Make(PrimitiveType.Cube, root, new Vector3(0, 0.12f, -0.35f), new Vector3(3.4f, 0.08f, 0.1f), Palette.Mg2Rail, "Runner");
            Prims.Make(PrimitiveType.Cube, root, new Vector3(0, 0.12f, 0.35f), new Vector3(3.4f, 0.08f, 0.1f), Palette.Mg2Rail, "Runner");
            Prims.Make(PrimitiveType.Cube, root, new Vector3(0, 0.95f, 0), new Vector3(3.2f, 1.6f, 1.3f), Palette.Mg2Sled, "Box");
            Prims.Make(PrimitiveType.Cube, root, new Vector3(-1.62f, 0.95f, 0), new Vector3(0.06f, 1.6f, 1.32f), Palette.Mg2SledFront, "FlatFront");
            Prims.Make(PrimitiveType.Cube, root, new Vector3(0.6f, 2.05f, 0), new Vector3(1.0f, 0.6f, 0.9f), Palette.Mg2Motor, "Motor");
            Prims.Make(PrimitiveType.Cylinder, root, new Vector3(0.9f, 2.6f, 0), new Vector3(0.25f, 0.35f, 0.25f), Palette.Mg2Smoke, "Chimney");
            for (int i = 0; i < 3; i++)
                Prims.Make(PrimitiveType.Cube, root, new Vector3(-0.8f + i * 0.8f, 1.2f, -0.66f), new Vector3(0.45f, 0.4f, 0.02f), Palette.Mg2Window, "Window");
            return root;
        }

        // ---------- Pips Zug fährt ----------

        /// <summary>
        /// Nach dem Minigame: Pips Zug (beste Runde) fährt durch den Gegenwind davon; Kalles Schlitten steht am Rand.
        /// Die Fahrgeschwindigkeit folgt dem Ergebnis (schneller Zug = zügige Fahrt), Form aus der Dachlinie der Simulation.
        /// </summary>
        public IEnumerator PlayPlayerTrain(TrainShape shape, PolarExpressLevel level, TrainResult result)
        {
            var sled = BuildSled();
            sled.localPosition = new Vector3(8f, 0, 3.2f);
            var train = BuildTrain(shape, level);
            train.localPosition = new Vector3(4f, 0, 0);
            Stand("Tilda", Prims.Look.Tilda, new Vector3(-2f, 0, -2.4f), 160f, "tilda");
            Stand("Kalle", Prims.Look.Kalle, new Vector3(6f, 0, 2.0f), 200f, null);
            Stand("Pip", Prims.Look.Pip, new Vector3(0f, 0, -2.8f), 180f, null);
            _root.Rig.LookAt(transform.position + new Vector3(1f, 0.8f, 0), 12f, 13f, 0f, snap: true);
            yield return _root.Hud.FadeIn();
            yield return _root.Hud.Say("mg2_train_departs");
            Sfx.Play(Sfx.Sound.Success, 0.5f);
            float top = 3f + 6f * Mathf.Clamp01((float)(result.Speed / 0.15));
            float v = 0f;
            for (float t = 0; t < 5f; t += Time.deltaTime)
            {
                v = Mathf.Min(top, v + 2.5f * Time.deltaTime);
                train.localPosition += Vector3.left * v * Time.deltaTime;
                yield return null;
            }
            yield return _root.Hud.Say("mg2_train_gone");
            yield return _root.Hud.FadeOut();
        }

        /// <summary>Pips Zug draußen: Scheiben entlang der Dachlinie (dieselbe Form wie im Windkanal), Petrol-Streifen.</summary>
        private Transform BuildTrain(TrainShape shape, PolarExpressLevel level)
        {
            var root = new GameObject("PolarExpress").transform;
            root.SetParent(transform, false);
            const float step = 2f;
            for (double x = level.Start; x < level.End; x += step)
            {
                double xc = x + step / 2;
                double h = TrainGeometry.Profile(shape, level, xc);
                if (h <= 0) continue;
                int t = TrainGeometry.TransitionAt(level, xc);
                bool open = t >= 0 && !shape.Covered[t];
                float lx = (float)(xc - level.Start) * Meter, hh = (float)h * Meter;
                Prims.Make(PrimitiveType.Cube, root, new Vector3(lx, 0.15f + hh / 2f, 0), new Vector3(step * Meter, hh, 1.1f),
                    open ? Palette.Mg2TransitionOpen : Palette.Mg2TrainOutside, "Slice");
                if (t < 0 && h >= level.Headroom * level.Height)
                {
                    Prims.Make(PrimitiveType.Cube, root, new Vector3(lx, 0.15f + hh * 0.35f, -0.56f), new Vector3(step * Meter, 0.08f, 0.02f), Palette.Mg2TrainStripe, "Stripe");
                    if (((int)(x - level.Start) / (int)step) % 3 == 1)
                        Prims.Make(PrimitiveType.Cube, root, new Vector3(lx, 0.15f + hh * 0.68f, -0.56f), new Vector3(0.12f, 0.2f, 0.02f), Palette.Mg2Window, "Window");
                }
            }
            return root;
        }

        /// <summary>Foto der aktuellen Ansicht ohne Oberfläche (wie <c>HubWorld.Photo</c>).</summary>
        private Texture2D Photo()
        {
            const int w = 640, h = 400;
            var cam = _root.Rig.Cam;
            var rt = RenderTexture.GetTemporary(w, h, 24);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevTarget;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear };
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }
    }
}
