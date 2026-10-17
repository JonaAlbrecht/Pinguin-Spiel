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
        /// <summary>Ab diesem Abstand (Meter, flach) lässt sich eine Figur mit E ansprechen.</summary>
        private const float TalkRange = 1.8f;
        /// <summary>So nah kommt Pip einer Figur höchstens – kein Durchlaufen.</summary>
        private const float NpcRadius = 0.75f;
        private TextMesh _talkPrompt;

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
            Prims.Make(PrimitiveType.Cylinder, sign, new Vector3(0, 0.8f, 0), new Vector3(0.12f, 0.8f, 0.12f), Palette.Wood, "Post");
            Prims.Make(PrimitiveType.Cube, sign, new Vector3(0, 1.75f, 0), new Vector3(2.8f, 1.1f, 0.08f), Palette.SignBoard, "Board");
            var ui = _root.Data;
            var l1 = Prims.Label(sign, ui.Ui("signPlaza"), new Vector3(0, 2.0f, -0.06f), 0.05f, Palette.Text);
            l1.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var l2 = Prims.Label(sign, ui.Ui("signBusiness"), new Vector3(0, 1.6f, -0.06f), 0.028f, Palette.SignAccent);
            l2.transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Friedas Labor
            // Begehbar: Wände mit Tür zum Platz (Westen), Dach und Südwand blenden aus, sobald Pip drin ist.
            // v0.1 war ein geschlossener Block – Pip lief hinein und verschwand (Spieltest 17.10.2026).
            _lab = BuildLab(t, new Vector3(7f, 0, -1f), ui.Ui("signLab"));

            // Kalles Baustelle
            var site = new GameObject("KalleSite").transform;
            site.SetParent(t, false);
            site.localPosition = new Vector3(-7f, 0, -2f);
            for (int i = 0; i < 4; i++)
                Prims.Make(PrimitiveType.Cube, site, new Vector3(i % 2 * 0.9f, 0.35f + i / 2 * 0.7f, 0), Vector3.one * 0.7f, Palette.Crate, "Crate");
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
            Prims.Make(PrimitiveType.Cylinder, future, new Vector3(0, 0.7f, 0), new Vector3(0.1f, 0.7f, 0.1f), Palette.Wood, "Post");
            Prims.Make(PrimitiveType.Cube, future, new Vector3(0, 1.5f, 0), new Vector3(2.4f, 0.8f, 0.06f), Palette.SignBoard, "Board");
            var fl = Prims.Label(future, ui.Ui("signFuture"), new Vector3(0, 1.5f, -0.05f), 0.026f, Palette.Text);
            fl.transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Figuren
            AddNpc("grete", Prims.Look.Grete, new Vector3(-1.8f, 0, 5f), 160f);
            AddNpc("frieda", Prims.Look.Frieda, new Vector3(5.2f, 0, 0.8f), 230f);
            AddNpc("kalle", Prims.Look.Kalle, new Vector3(-5.2f, 0, -0.5f), 120f);
            // Neu in v0.2: Theo misst nach (Lernziel 5), Klara rechnet den Nutzen vor (Lernziel 4), Flocke fragt „warum?“
            AddNpc("theo", Prims.Look.Theo, new Vector3(8.6f, 0, 2.2f), 250f);
            AddNpc("klara", Prims.Look.Klara, new Vector3(-3.4f, 0, -4.6f), 30f);
            AddNpc("flocke", Prims.Look.Flocke, new Vector3(1.6f, 0, 6.4f), 190f);
            foreach (var p in new[] { new Vector3(-0.6f, 0, 4.4f), new Vector3(0.1f, 0, 3.6f), new Vector3(0.8f, 0, 4.6f) })
            {
                var extra = Prims.Penguin(t, "Colony", Prims.Look.Plain, 1.3f);
                extra.transform.localPosition = p;
                extra.transform.localRotation = Quaternion.Euler(0, rnd.Next(0, 360), 0);
                extra.AddComponent<Wanderer>().Radius = 1.1f; // klein genug, um nicht durch Grete und Flocke zu laufen
            }

            _pip = Prims.Penguin(t, "Pip", Prims.Look.Pip, 1.25f);
            _pip.transform.localPosition = new Vector3(0, 0, -2f);
            _target = _pip.transform.position;

            _clickMarker = Prims.Make(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.5f, 0.01f, 0.5f), Palette.Selected, "ClickMarker").transform;
            _clickMarker.gameObject.SetActive(false);

            _talkPrompt = Prims.Label(t, ui.Ui("hubTalkPrompt"), Vector3.zero, 0.03f, Palette.Text);
            _talkPrompt.fontStyle = FontStyle.Bold;
            _talkPrompt.gameObject.SetActive(false);
        }

        /// <summary>Ein begehbares Gebäude: Grundriss für Wände und Tür, dazu was beim Betreten ausgeblendet wird.</summary>
        private sealed class Building
        {
            public Vector3 Center;
            public Vector2 Half;          // halbe Außenmaße (x, z)
            public float DoorZ, DoorHalf; // Tür in der Westwand
            public Renderer[] HideInside; // Dach, Südwand (zur Kamera hin)
            public bool Inside;
        }

        private const float WallThickness = 0.15f;
        private const float PipRadius = 0.3f;
        private Building _lab;

        private Building BuildLab(Transform parent, Vector3 pos, string sign)
        {
            var lab = new GameObject("Lab").transform;
            lab.SetParent(parent, false);
            lab.localPosition = pos;
            var wall = Palette.LabWall;
            var b = new Building { Center = pos, Half = new Vector2(1.6f, 1.3f), DoorZ = 0f, DoorHalf = 0.55f };
            float hx = b.Half.x, hz = b.Half.y, h = 1.8f, th = WallThickness;

            Prims.Make(PrimitiveType.Cube, lab, new Vector3(0, 0.01f, 0), new Vector3(hx * 2, 0.02f, hz * 2), Palette.LabFloor, "Floor");
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(0, h / 2, hz - th / 2), new Vector3(hx * 2, h, th), wall, "WallN");
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(hx - th / 2, h / 2, 0), new Vector3(th, h, hz * 2), wall, "WallE");
            var south = Prims.Make(PrimitiveType.Cube, lab, new Vector3(0, h / 2, -hz + th / 2), new Vector3(hx * 2, h, th), wall, "WallS");
            // Westwand in zwei Stücken – dazwischen die Tür zum Huddle-Platz
            float doorLow = b.DoorZ - b.DoorHalf, doorHigh = b.DoorZ + b.DoorHalf;
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(-hx + th / 2, h / 2, (doorHigh + hz) / 2), new Vector3(th, h, hz - doorHigh), wall, "WallW1");
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(-hx + th / 2, h / 2, (doorLow - hz) / 2), new Vector3(th, h, doorLow + hz), wall, "WallW2");
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(-hx + th / 2, h - 0.15f, b.DoorZ), new Vector3(th, 0.3f, b.DoorHalf * 2), Palette.LabTrim, "DoorFrame");
            var roof = Prims.Make(PrimitiveType.Cube, lab, new Vector3(0, h + 0.12f, 0), new Vector3(hx * 2 + 0.3f, 0.25f, hz * 2 + 0.3f), Palette.LabTrim, "Roof");

            // Einrichtung: Arbeitstisch mit Bildschirm (Wärmebild) an der Nordwand
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(0.3f, 0.4f, hz - 0.5f), new Vector3(1.6f, 0.08f, 0.6f), Palette.Desk, "Desk");
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(0.3f, 0.2f, hz - 0.5f), new Vector3(1.4f, 0.4f, 0.5f), Palette.DeskBase, "DeskBase");
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(0.3f, 0.85f, hz - th - 0.05f), new Vector3(1.0f, 0.6f, 0.04f), Palette.Screen, "Screen");
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(0.05f, 0.9f, hz - th - 0.08f), new Vector3(0.25f, 0.2f, 0.02f), HeatColors.Comfy, "HeatSpot");
            Prims.Make(PrimitiveType.Cube, lab, new Vector3(0.55f, 0.78f, hz - th - 0.08f), new Vector3(0.3f, 0.2f, 0.02f), HeatColors.Cold, "ColdSpot");

            var label = Prims.Label(lab, sign, new Vector3(0, h + 0.7f, 0), 0.04f, Palette.Text);
            b.HideInside = new[] { roof.GetComponent<Renderer>(), south.GetComponent<Renderer>(), label.GetComponent<Renderer>() };
            return b;
        }

        /// <summary>
        /// Wände sind fest, die Tür ist offen. Ein Punkt ist gesperrt, wenn er im Wandband liegt (Wanddicke plus
        /// Pips Radius) und nicht in der Türöffnung. Achsenweise geprüft, damit Pip an Wänden entlanggleitet.
        /// </summary>
        private bool Blocked(Vector3 p)
        {
            var b = _lab;
            if (b == null) return false;
            float lx = p.x - b.Center.x, lz = p.z - b.Center.z;
            float ox = b.Half.x + PipRadius, oz = b.Half.y + PipRadius;
            float ix = b.Half.x - WallThickness - PipRadius, iz = b.Half.y - WallThickness - PipRadius;
            bool inOuter = Mathf.Abs(lx) < ox && Mathf.Abs(lz) < oz;
            bool inInner = Mathf.Abs(lx) < ix && Mathf.Abs(lz) < iz;
            if (!inOuter || inInner) return false;
            bool inDoor = lx < -ix && Mathf.Abs(lz - b.DoorZ) < b.DoorHalf - PipRadius;
            return !inDoor;
        }

        /// <summary>Schritt von <paramref name="from"/> nach <paramref name="to"/>, an Wänden entlang statt hindurch.</summary>
        private Vector3 MoveWithWalls(Vector3 from, Vector3 to)
        {
            if (!Blocked(to)) return to;
            var xOnly = new Vector3(to.x, from.y, from.z);
            if (!Blocked(xOnly)) return xOnly;
            var zOnly = new Vector3(from.x, from.y, to.z);
            if (!Blocked(zOnly)) return zOnly;
            return from;
        }

        private void UpdateBuildings()
        {
            var b = _lab;
            if (b == null) return;
            float lx = Pip.position.x - b.Center.x, lz = Pip.position.z - b.Center.z;
            bool inside = Mathf.Abs(lx) < b.Half.x && Mathf.Abs(lz) < b.Half.y;
            if (inside == b.Inside) return;
            b.Inside = inside;
            foreach (var r in b.HideInside) r.enabled = !inside;
        }

        private void AddNpc(string id, Prims.Look look, Vector3 pos, float yaw)
        {
            var go = Prims.Penguin(transform, id, look, 1.4f);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var marker = Prims.Label(go.transform, "!", new Vector3(0, 1.35f, 0), 0.06f, Palette.Marker);
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
            bool free = !_root.Hud.Blocking;
            var move = free ? InputHelper.MoveInput : Vector2.zero;
            if (move.sqrMagnitude > 0.0001f) WalkByKeys(move);
            else
            {
                HandleClick(free);
                WalkToTarget();
            }
            UpdateTalkPrompt(free);
            UpdateBuildings();
        }

        /// <summary>
        /// WASD/Pfeiltasten, bezogen auf die Kamera: „W“ läuft immer bildeinwärts, egal wie die Kamera steht.
        /// Tastatur schlägt ein angeklicktes Ziel – wer lenkt, will nicht zum alten Klickpunkt zurück.
        /// </summary>
        private void WalkByKeys(Vector2 move)
        {
            var cam = _root.Rig.Cam.transform;
            var fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            var dir = right * move.x + fwd * move.y;
            var next = MoveWithWalls(Pip.position, KeepOnFloe(KeepClearOfNpcs(Pip.position + dir * (WalkSpeed * Time.deltaTime))));
            Pip.position = next;
            Pip.rotation = Quaternion.Slerp(Pip.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * Time.deltaTime);
            Pip.Find("Body").localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 14f) * 8f);   // Watscheln
            _target = next;
            _pendingNpc = null;
            _clickMarker.gameObject.SetActive(false);
        }

        /// <summary>Pip läuft nicht durch Figuren hindurch, sondern wird an ihrem Rand entlanggeschoben.</summary>
        private Vector3 KeepClearOfNpcs(Vector3 p)
        {
            foreach (var n in _npcs.Values)
            {
                var c = n.Go.transform.position;
                var d = new Vector3(p.x - c.x, 0, p.z - c.z);
                if (d.magnitude < NpcRadius && d.sqrMagnitude > 1e-6f) p = new Vector3(c.x, p.y, c.z) + d.normalized * NpcRadius;
            }
            return p;
        }

        private static Vector3 KeepOnFloe(Vector3 p)
        {
            var flat = new Vector2(p.x, p.z);
            if (flat.magnitude > FloeRadius - 1f) flat = flat.normalized * (FloeRadius - 1f);
            return new Vector3(flat.x, p.y, flat.y);
        }

        /// <summary>Steht Pip nah genug bei einer Figur, zeigt sie „E: ansprechen“ – und E/Leertaste/Enter spricht sie an.</summary>
        private void UpdateTalkPrompt(bool free)
        {
            Npc nearest = null;
            float best = TalkRange;
            if (free)
            {
                foreach (var n in _npcs.Values)
                {
                    var c = n.Go.transform.position;
                    float d = Vector2.Distance(new Vector2(c.x, c.z), new Vector2(Pip.position.x, Pip.position.z));
                    if (d < best) { best = d; nearest = n; }
                }
            }
            _talkPrompt.gameObject.SetActive(nearest != null);
            if (nearest == null) return;
            _talkPrompt.transform.position = nearest.Go.transform.position + Vector3.up * 2.35f;
            if (InputHelper.ConfirmPressed && !_root.Hud.JustClosed)
            {
                _pendingNpc = null;
                _target = Pip.position;
                Talk(nearest);
            }
        }

        private void Talk(Npc npc)
        {
            var look = Pip.position - npc.Go.transform.position;
            look.y = 0;
            if (look.sqrMagnitude > 0.01f) npc.Go.transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            if (npc.OnInteract != null) npc.OnInteract();
            else if (npc.IdleNode != null) StartCoroutine(_root.Hud.Say(npc.IdleNode));
            else _root.Hud.Toast(_root.Data.Ui("hubNoTask", _root.Data.Speakers[npc.Id].Name), 2.5f);
        }

        private void HandleClick(bool free)
        {
            if (free && InputHelper.PointerDown && !InputHelper.PointerOverUI &&
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
        }

        private void WalkToTarget()
        {
            var pos = Pip.position;
            var to = _target - pos;
            to.y = 0;
            if (to.magnitude > 0.05f)
            {
                var step = to.normalized * Mathf.Min(WalkSpeed * Time.deltaTime, to.magnitude);
                var next = MoveWithWalls(pos, pos + step);
                // Steht eine Wand im Weg, bleibt Pip dort stehen statt ewig dagegen zu laufen.
                if ((next - pos).sqrMagnitude < 1e-8f) _target = pos;
                Pip.position = next;
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
                    Talk(npc);
                }
            }
        }

        /// <summary>
        /// Stellt die geplante Siedlung (beste Runde) im Kleinformat auf die Scholle (Frage C-04).
        /// Mit <paramref name="result"/> leuchten die Türen der gemütlichen Iglus, frierende sind bläulich –
        /// die Belohnung zeigt also, wie gut die Siedlung war, nicht nur, dass es sie gibt.
        /// </summary>
        public void ShowSettlement(string[] rows, SettlementResult result = null, string[] finalRows = null)
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
                        case Cell.Igloo:
                            go = Prims.Igloo(_settlement.transform, cell * 1.6f);
                            var states = result?.IglooStates;
                            int idx = y * w + x;
                            // IglooStates gilt nur für Felder, die im End-Raster noch Iglu sind; geschmolzene
                            // stehen dort als Wasser und tragen den Standardwert – sie bleiben hier normal weiß.
                            if (states != null && finalRows != null && idx < states.Length && finalRows[y][x] == Cell.Igloo)
                            {
                                if (states[idx] == IglooState.Comfortable)
                                    Prims.SetColor(go.transform.Find("Door").gameObject, Palette.LanternGlow);
                                else if (states[idx] == IglooState.Cold)
                                    Prims.SetColor(go.transform.Find("Dome").gameObject, Palette.IglooCold);
                            }
                            break;
                        case Cell.Lantern: go = Prims.Lantern(_settlement.transform, cell * 1.6f, false); break;
                        case Cell.Wall: go = Prims.SnowWall(_settlement.transform, cell * 1.4f); break;
                    }
                    if (go != null) go.transform.localPosition = origin + new Vector3(x * cell, 0, -y * cell);
                }
            }
        }
    }
}
