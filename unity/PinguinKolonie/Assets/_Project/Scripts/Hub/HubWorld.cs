using System;
using System.Collections;
using System.Collections.Generic;
using SFE.SimCore.Thermal;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Hub-Ausschnitt „Huddle-Platz“ (Fragen C-01…C-04): Pip läuft per Klick/Tipp dorthin, wo man hinzeigt.
    /// Ein Klick auf eine Figur lässt Pip hinlaufen und startet das Gespräch. Ein gelbes „!“ zeigt,
    /// wer als Nächstes etwas will. Nach MG1 steht die vom Spieler geplante Siedlung auf der Scholle.
    /// </summary>
    public sealed class HubWorld : MonoBehaviour
    {
        private sealed class Npc
        {
            public string Id;
            public GameObject Go;
            public GameObject Marker;
            public Action OnInteract;
            public string IdleNode;
        }

        private const float FloeRadius = 14f;
        private const float WalkSpeed = 4f;

        private GameRoot _root;
        private GameObject _pip;
        private Vector3 _target;
        private Npc _pendingNpc;
        private readonly Dictionary<string, Npc> _npcs = new Dictionary<string, Npc>();
        private GameObject _settlement;
        private bool _active;
        private Transform _clickMarker;

        public Transform Pip => _pip.transform;

        public static HubWorld Create(GameRoot root)
        {
            var go = new GameObject("Hub_HuddlePlatz");
            var hub = go.AddComponent<HubWorld>();
            hub._root = root;
            hub.Build();
            go.SetActive(false);
            return hub;
        }

        private void Build()
        {
            var t = transform;
            Prims.Make(PrimitiveType.Cylinder, t, new Vector3(0, -0.6f, 0), new Vector3(120, 0.1f, 120), Palette.Sea, "Sea");
            Prims.Make(PrimitiveType.Cylinder, t, new Vector3(0, -0.25f, 0), new Vector3(FloeRadius * 2, 0.25f, FloeRadius * 2), Palette.Ice, "Floe");
            Prims.Make(PrimitiveType.Cylinder, t, new Vector3(0, 0.005f, 4f), new Vector3(7f, 0.01f, 7f), Palette.IceShadow, "HuddlePlaza");

            // Schneehaufen und Eisbrocken
            var rnd = new System.Random(11);
            for (int i = 0; i < 18; i++)
            {
                float a = (float)(rnd.NextDouble() * Math.PI * 2), r = 9f + (float)rnd.NextDouble() * 4f;
                float s = 0.6f + (float)rnd.NextDouble() * 1.2f;
                Prims.Make(PrimitiveType.Sphere, t, new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), new Vector3(s * 1.4f, s * 0.7f, s), Palette.Snow, "SnowPile");
            }

            // Schild mit Siemens-Geschäft (docs/00 Abschnitt 4, Berührungspunkt 1)
            var sign = new GameObject("Sign").transform;
            sign.SetParent(t, false);
            sign.localPosition = new Vector3(3.6f, 0, 6.5f);
            sign.localRotation = Quaternion.Euler(0, 200, 0);
            Prims.Make(PrimitiveType.Cylinder, sign, new Vector3(0, 0.8f, 0), new Vector3(0.12f, 0.8f, 0.12f), new Color(0.45f, 0.32f, 0.2f), "Post");
            Prims.Make(PrimitiveType.Cube, sign, new Vector3(0, 1.75f, 0), new Vector3(2.8f, 1.1f, 0.08f), new Color(0.98f, 0.96f, 0.9f), "Board");
            var l1 = Prims.Label(sign, "Huddle-Platz", new Vector3(0, 2.0f, -0.06f), 0.05f, Palette.Text);
            l1.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var l2 = Prims.Label(sign, "Siemens Smart Infrastructure\nSchaltanlagen", new Vector3(0, 1.6f, -0.06f), 0.028f, new Color(0, 0.45f, 0.45f));
            l2.transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Friedas Labor
            var lab = new GameObject("Lab").transform;
            lab.SetParent(t, false);
            lab.localPosition = new Vector3(7f, 0, -1f);
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(0, 0.9f, 0), new Vector3(3f, 1.8f, 2.4f), new Color(0.78f, 0.84f, 0.92f), "Hut");
            var roof = Prims.Make(PrimitiveType.Cube, lab, new Vector3(0, 2.0f, 0), new Vector3(3.3f, 0.25f, 2.7f), new Color(0.3f, 0.5f, 0.65f), "Roof");
            roof.transform.localRotation = Quaternion.Euler(0, 0, 4);
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(-0.6f, 0.6f, 1.21f), new Vector3(0.7f, 1.2f, 0.05f), new Color(0.2f, 0.25f, 0.35f), "Door");
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(0.7f, 1.1f, 1.21f), new Vector3(0.8f, 0.6f, 0.05f), new Color(0.6f, 0.9f, 1f), "Screen");
            var labLabel = Prims.Label(lab, "Simulationslabor", new Vector3(0, 2.5f, 0.6f), 0.04f, Palette.Text);
            labLabel.transform.rotation = Quaternion.Euler(50, 0, 0);

            // Kalles Baustelle
            var site = new GameObject("KalleSite").transform;
            site.SetParent(t, false);
            site.localPosition = new Vector3(-7f, 0, -2f);
            for (int i = 0; i < 4; i++)
                Prims.Make(PrimitiveType.Cube, site, new Vector3(i % 2 * 0.9f, 0.35f + i / 2 * 0.7f, 0), Vector3.one * 0.7f, new Color(0.75f, 0.55f, 0.3f), "Crate");
            Prims.Lantern(site, 1f, false).transform.localPosition = new Vector3(1.8f, 0, 0.5f);
            Prims.Lantern(site, 1f, false).transform.localPosition = new Vector3(2.3f, 0, -0.2f);

            // Laternen am Platz (Friedas neues Stromnetz)
            foreach (var p in new[] { new Vector3(-3.5f, 0, 2f), new Vector3(3.5f, 0, 2.2f), new Vector3(0, 0, 8.2f) })
                Prims.Lantern(t, 1.2f, true).transform.localPosition = p;

            // Ausblick auf Release 1 (MG2, MG3)
            var future = new GameObject("FutureSign").transform;
            future.SetParent(t, false);
            future.localPosition = new Vector3(12f, 0, 3f);
            future.localRotation = Quaternion.Euler(0, 240, 0);
            Prims.Make(PrimitiveType.Cylinder, future, new Vector3(0, 0.7f, 0), new Vector3(0.1f, 0.7f, 0.1f), new Color(0.45f, 0.32f, 0.2f), "Post");
            Prims.Make(PrimitiveType.Cube, future, new Vector3(0, 1.5f, 0), new Vector3(2.4f, 0.8f, 0.06f), new Color(0.98f, 0.96f, 0.9f), "Board");
            var fl = Prims.Label(future, "Zum Gleis an die Küste\n(kommt mit Release 1)", new Vector3(0, 1.5f, -0.05f), 0.026f, Palette.Text);
            fl.transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Figuren
            AddNpc("grete", Prims.Look.Grete, new Vector3(-1.8f, 0, 5f), 160f);
            AddNpc("frieda", Prims.Look.Frieda, new Vector3(5.2f, 0, 0.8f), 230f);
            AddNpc("kalle", Prims.Look.Kalle, new Vector3(-5.2f, 0, -0.5f), 120f);
            foreach (var p in new[] { new Vector3(-0.6f, 0, 4.4f), new Vector3(0.1f, 0, 3.6f), new Vector3(0.8f, 0, 4.6f) })
            {
                var extra = Prims.Penguin(t, "Colony", Prims.Look.Plain, 1.3f);
                extra.transform.localPosition = p;
                extra.transform.localRotation = Quaternion.Euler(0, rnd.Next(0, 360), 0);
            }

            _pip = Prims.Penguin(t, "Pip", Prims.Look.Pip, 1.25f);
            _pip.transform.localPosition = new Vector3(0, 0, -2f);
            _target = _pip.transform.position;

            _clickMarker = Prims.Make(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.5f, 0.01f, 0.5f), Palette.Selected, "ClickMarker").transform;
            _clickMarker.gameObject.SetActive(false);
        }

        private void AddNpc(string id, Prims.Look look, Vector3 pos, float yaw)
        {
            var go = Prims.Penguin(transform, id, look, 1.4f);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var marker = Prims.Label(go.transform, "!", new Vector3(0, 1.35f, 0), 0.06f, new Color(1f, 0.75f, 0f));
            marker.fontStyle = FontStyle.Bold;
            marker.gameObject.AddComponent<Bobber>().Amplitude = 0.08f;
            marker.gameObject.SetActive(false);
            if (_root.Data.Speakers.TryGetValue(id, out var sp))
            {
                var name = Prims.Label(go.transform, sp.Name, new Vector3(0, 1.05f, 0), 0.022f, sp.Color);
                name.fontStyle = FontStyle.Bold;
            }
            _npcs[id] = new Npc { Id = id, Go = go, Marker = marker.gameObject };
        }

        public void Enter()
        {
            gameObject.SetActive(true);
            _active = true;
            _target = _pip.transform.position;
            _root.Rig.Follow(_pip.transform, 50f, 24f);
        }

        public void Exit()
        {
            _active = false;
            gameObject.SetActive(false);
        }

        /// <summary>Wartet, bis Pip mit der Figur spricht (Klick auf die Figur). Andere Figuren plaudern.</summary>
        public IEnumerator WaitForTalk(string npcId)
        {
            bool talked = false;
            foreach (var n in _npcs.Values) { n.Marker.SetActive(n.Id == npcId); n.OnInteract = null; }
            _npcs[npcId].OnInteract = () => talked = true;
            while (!talked) yield return null;
            _npcs[npcId].Marker.SetActive(false);
            _npcs[npcId].OnInteract = null;
        }

        public void SetIdleNode(string npcId, string node) => _npcs[npcId].IdleNode = node;

        private void Update()
        {
            if (!_active) return;
            if (!_root.Hud.DialogOpen && InputHelper.PointerDown && !InputHelper.PointerOverUI &&
                InputHelper.PointerOnGround(_root.Rig.Cam, out var p))
            {
                _pendingNpc = null;
                foreach (var n in _npcs.Values)
                {
                    var flat = n.Go.transform.position;
                    if (Vector2.Distance(new Vector2(flat.x, flat.z), new Vector2(p.x, p.z)) < 1.2f) _pendingNpc = n;
                }
                if (_pendingNpc != null)
                {
                    var npcPos = _pendingNpc.Go.transform.position;
                    _target = npcPos + (Pip.position - npcPos).normalized * 1.2f;
                }
                else _target = p;
                var flatT = new Vector2(_target.x, _target.z);
                if (flatT.magnitude > FloeRadius - 1f) flatT = flatT.normalized * (FloeRadius - 1f);
                _target = new Vector3(flatT.x, 0, flatT.y);
                _clickMarker.position = _target + Vector3.up * 0.01f;
                _clickMarker.gameObject.SetActive(_pendingNpc == null);
            }

            var pos = Pip.position;
            var to = _target - pos;
            to.y = 0;
            if (to.magnitude > 0.05f)
            {
                var step = to.normalized * Mathf.Min(WalkSpeed * Time.deltaTime, to.magnitude);
                Pip.position = pos + step;
                Pip.rotation = Quaternion.Slerp(Pip.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), 10f * Time.deltaTime);
                var body = Pip.Find("Body");
                body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 14f) * 8f);   // Watscheln
            }
            else
            {
                Pip.Find("Body").localRotation = Quaternion.identity;
                _clickMarker.gameObject.SetActive(false);
                if (_pendingNpc != null)
                {
                    var npc = _pendingNpc;
                    _pendingNpc = null;
                    var look = Pip.position - npc.Go.transform.position;
                    look.y = 0;
                    if (look.sqrMagnitude > 0.01f) npc.Go.transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
                    if (npc.OnInteract != null) npc.OnInteract();
                    else if (npc.IdleNode != null) StartCoroutine(_root.Hud.Say(npc.IdleNode));
                    else _root.Hud.Toast(_root.Data.Speakers[npc.Id].Name + ": „Ich hab gerade keine Aufgabe für dich.“", 2.5f);
                }
            }
        }

        /// <summary>Stellt die geplante Siedlung (beste Runde) im Kleinformat auf die Scholle (Frage C-04).</summary>
        public void ShowSettlement(string[] rows)
        {
            if (_settlement != null) Destroy(_settlement);
            if (rows == null) return;
            _settlement = new GameObject("Settlement");
            _settlement.transform.SetParent(transform, false);
            const float cell = 0.42f;
            int h = rows.Length, w = rows[0].Length;
            // Nur den bebauten Ausschnitt zeigen, mittig südlich vom Huddle-Platz
            int minX = w, maxX = -1, minY = h, maxY = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (rows[y][x] != Cell.Ice && rows[y][x] != Cell.Water)
                    { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
            if (maxX < 0) return;
            var center = new Vector3(0, 0, -7f);
            var origin = center - new Vector3((minX + maxX) / 2f * cell, 0, -(minY + maxY) / 2f * cell);
            Prims.Make(PrimitiveType.Cylinder, _settlement.transform, center + new Vector3(0, 0.004f, 0),
                new Vector3((maxX - minX + 3) * cell * 1.2f, 0.01f, (maxY - minY + 3) * cell * 1.2f), Palette.IceShadow, "Plot");
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    GameObject go = null;
                    switch (rows[y][x])
                    {
                        case Cell.Igloo: go = Prims.Igloo(_settlement.transform, cell * 1.6f); break;
                        case Cell.Lantern: go = Prims.Lantern(_settlement.transform, cell * 1.6f, false); break;
                        case Cell.Wall: go = Prims.SnowWall(_settlement.transform, cell * 1.4f); break;
                    }
                    if (go != null) go.transform.localPosition = origin + new Vector3(x * cell, 0, -y * cell);
                }
            }
        }
    }
}
