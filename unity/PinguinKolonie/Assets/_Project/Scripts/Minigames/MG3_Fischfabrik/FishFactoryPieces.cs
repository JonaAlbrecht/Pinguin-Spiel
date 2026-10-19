using SFE.SimCore.Process;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Bausteine der Fischfabrik aus Grundformen (<see cref="Prims.Make"/>), Maße in Feldern (1 = ein Rasterfeld).
    /// Jede Station hat ein Lämpchen „Lamp“: grün = arbeitet, rot = Störung, grau = wartet auf Fisch.
    /// </summary>
    public static class FishFactoryPieces
    {
        public static GameObject Create(char symbol, Transform parent)
        {
            switch (symbol)
            {
                case 'S': return Sorter(parent);
                case 'F': return Filleter(parent);
                case 'P': return Packer(parent);
                case FactoryLayout.Regal: return Regal(parent);
                case FactoryLayout.Belt: return Belt(parent);
                case FactoryLayout.Robot: return RoboSeal(parent);
                case FactoryLayout.Pillar: return Pillar(parent);
                case FactoryLayout.Dock: return Pier(parent);
                case FactoryLayout.Train: return Platform(parent);
                default: return null;
            }
        }

        private static GameObject Root(string name, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root;
        }

        private static void Lamp(Transform root, float height)
        {
            Prims.Make(PrimitiveType.Sphere, root, new Vector3(0.28f, height, -0.28f), Vector3.one * 0.16f, Palette.Mg3LampIdle, "Lamp");
        }

        /// <summary>Sortiertisch: Holztisch mit Fischen und zwei Kisten daneben.</summary>
        public static GameObject Sorter(Transform parent)
        {
            var r = Root("Sorter", parent).transform;
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.22f, 0), new Vector3(0.82f, 0.44f, 0.6f), Palette.Mg3SortTable, "Table");
            for (int i = 0; i < 3; i++)
                Prims.Make(PrimitiveType.Capsule, r, new Vector3(-0.22f + i * 0.22f, 0.5f, 0), new Vector3(0.08f, 0.14f, 0.08f), Palette.Mg3Fish, "Fish")
                    .transform.localRotation = Quaternion.Euler(90, 20 * i, 0);
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.62f, 0.25f), new Vector3(0.82f, 0.06f, 0.06f), Palette.Mg3MachineDark, "Rail");
            Lamp(r, 0.75f);
            return r.gameObject;
        }

        /// <summary>Filetiermaschine: Gehäuse mit rundem Messer obendrauf.</summary>
        public static GameObject Filleter(Transform parent)
        {
            var r = Root("Filleter", parent).transform;
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.3f, 0), new Vector3(0.78f, 0.6f, 0.7f), Palette.Mg3Machine, "Body");
            var blade = Prims.Make(PrimitiveType.Cylinder, r, new Vector3(0, 0.66f, 0), new Vector3(0.5f, 0.03f, 0.5f), Palette.Mg3Blade, "Blade");
            blade.transform.localRotation = Quaternion.Euler(0, 0, 90);
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.62f, -0.3f), new Vector3(0.7f, 0.08f, 0.12f), Palette.Mg3MachineDark, "Front");
            Lamp(r, 0.85f);
            return r.gameObject;
        }

        /// <summary>Verpackungsmaschine: Kasten mit Petrol-Haube und fertiger Sushi-Kiste.</summary>
        public static GameObject Packer(Transform parent)
        {
            var r = Root("Packer", parent).transform;
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.28f, 0), new Vector3(0.8f, 0.56f, 0.72f), Palette.Mg3Machine, "Body");
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.62f, 0.05f), new Vector3(0.62f, 0.14f, 0.5f), Palette.Mg3PackTop, "Hood");
            var box = SushiBox(r);
            box.transform.localPosition = new Vector3(-0.2f, 0.69f, -0.25f);
            Lamp(r, 0.85f);
            return r.gameObject;
        }

        /// <summary>Kühlregal: hellblauer Schrank mit Böden – hier stapeln sich die Kisten vor dem Filetieren.</summary>
        public static GameObject Regal(Transform parent)
        {
            var r = Root("Regal", parent).transform;
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.03f, 0), new Vector3(0.86f, 0.06f, 0.86f), Palette.Mg3RegalFrame, "Base");
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.55f, 0.38f), new Vector3(0.86f, 1.1f, 0.08f), Palette.Mg3Regal, "Back");
            for (int i = 0; i < 4; i++)
            {
                float x = i < 2 ? -0.4f : 0.4f, z = i % 2 == 0 ? -0.38f : 0.38f;
                Prims.Make(PrimitiveType.Cube, r, new Vector3(x, 0.55f, z), new Vector3(0.06f, 1.1f, 0.06f), Palette.Mg3RegalFrame, "Post");
            }
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 1.12f, 0), new Vector3(0.86f, 0.05f, 0.86f), Palette.Mg3Regal, "Top");
            return r.gameObject;
        }

        /// <summary>Förderband: dunkles Band mit Rollen.</summary>
        public static GameObject Belt(Transform parent)
        {
            var r = Root("Belt", parent).transform;
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.18f, 0), new Vector3(0.94f, 0.08f, 0.6f), Palette.Mg3Belt, "Band");
            for (int i = 0; i < 3; i++)
                Prims.Make(PrimitiveType.Cube, r, new Vector3(-0.3f + i * 0.3f, 0.07f, 0), new Vector3(0.08f, 0.14f, 0.64f), Palette.Mg3BeltRoller, "Leg");
            return r.gameObject;
        }

        /// <summary>Robo-Robbe: liegender grauer Robbenkörper auf Rädern, Kopf mit leuchtenden Augen.</summary>
        public static GameObject RoboSeal(Transform parent)
        {
            var r = Root("RoboSeal", parent).transform;
            var body = Prims.Make(PrimitiveType.Capsule, r, new Vector3(0, 0.24f, -0.02f), new Vector3(0.34f, 0.3f, 0.34f), Palette.Mg3Robot, "Body");
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Prims.Make(PrimitiveType.Sphere, r, new Vector3(0, 0.16f, 0), new Vector3(0.28f, 0.14f, 0.5f), Palette.Mg3RobotBelly, "Belly");
            Prims.Make(PrimitiveType.Sphere, r, new Vector3(0, 0.36f, 0.26f), Vector3.one * 0.24f, Palette.Mg3Robot, "Head");
            Prims.Make(PrimitiveType.Sphere, r, new Vector3(-0.06f, 0.4f, 0.37f), Vector3.one * 0.06f, Palette.Mg3RobotEye, "EyeL");
            Prims.Make(PrimitiveType.Sphere, r, new Vector3(0.06f, 0.4f, 0.37f), Vector3.one * 0.06f, Palette.Mg3RobotEye, "EyeR");
            for (int i = 0; i < 4; i++)
            {
                var w = Prims.Make(PrimitiveType.Cylinder, r, new Vector3(i < 2 ? -0.16f : 0.16f, 0.06f, i % 2 == 0 ? -0.16f : 0.14f),
                    new Vector3(0.12f, 0.03f, 0.12f), Palette.Mg3RobotWheel, "Wheel");
                w.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            // Ladefläche auf dem Rücken
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.42f, -0.08f), new Vector3(0.26f, 0.03f, 0.26f), Palette.Mg3RobotWheel, "Tray");
            return r.gameObject;
        }

        public static GameObject Pillar(Transform parent)
        {
            var r = Root("Pillar", parent).transform;
            Prims.Make(PrimitiveType.Cylinder, r, new Vector3(0, 0.7f, 0), new Vector3(0.36f, 0.7f, 0.36f), Palette.Mg3Pillar, "Column");
            return r.gameObject;
        }

        /// <summary>Anlegestelle: Holzsteg mit Pollern.</summary>
        public static GameObject Pier(Transform parent)
        {
            var r = Root("Pier", parent).transform;
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.03f, 0), new Vector3(0.98f, 0.06f, 0.98f), Palette.Mg3Pier, "Planks");
            Prims.Make(PrimitiveType.Cylinder, r, new Vector3(-0.38f, 0.14f, 0.3f), new Vector3(0.12f, 0.12f, 0.12f), Palette.Mg3MachineDark, "Bollard");
            return r.gameObject;
        }

        /// <summary>Verladung: Bahnsteigkante mit Gleis.</summary>
        public static GameObject Platform(Transform parent)
        {
            var r = Root("Platform", parent).transform;
            Prims.Make(PrimitiveType.Cube, r, new Vector3(-0.2f, 0.05f, 0), new Vector3(0.58f, 0.1f, 0.98f), Palette.Mg3Machine, "Edge");
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0.22f, 0.02f, -0.18f), new Vector3(0.06f, 0.04f, 0.98f), Palette.Mg3Rail, "RailA");
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0.22f, 0.02f, 0.18f), new Vector3(0.06f, 0.04f, 0.98f), Palette.Mg3Rail, "RailB");
            return r.gameObject;
        }

        /// <summary>Fischkiste (für Stapel), 0,26 hoch.</summary>
        public static GameObject Crate(Transform parent)
        {
            var c = Prims.Make(PrimitiveType.Cube, parent, Vector3.zero, new Vector3(0.36f, 0.24f, 0.3f), Palette.Mg3Crate, "Crate");
            Prims.Make(PrimitiveType.Cube, c.transform, Vector3.zero, new Vector3(1.02f, 0.3f, 1.02f), Palette.Mg3CrateBand, "Band");
            return c;
        }

        public static GameObject SushiBox(Transform parent)
        {
            var c = Prims.Make(PrimitiveType.Cube, parent, Vector3.zero, new Vector3(0.3f, 0.14f, 0.24f), Palette.Mg3Sushi, "Sushi");
            Prims.Make(PrimitiveType.Cube, c.transform, new Vector3(0, 0.55f, 0), new Vector3(0.9f, 0.15f, 0.9f), Palette.Mg3SushiLid, "Lid");
            return c;
        }

        /// <summary>Kutter: Rumpf, Steuerhaus, Mast.</summary>
        public static GameObject Boat(Transform parent)
        {
            var r = Root("Kutter", parent).transform;
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.12f, 0), new Vector3(0.9f, 0.3f, 1.9f), Palette.Mg3BoatHull, "Hull");
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.12f, 1.0f), new Vector3(0.64f, 0.3f, 0.5f), Palette.Mg3BoatHull, "Bow")
                .transform.localRotation = Quaternion.Euler(0, 45, 0);
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.48f, -0.35f), new Vector3(0.56f, 0.42f, 0.6f), Palette.Mg3BoatCabin, "Cabin");
            Prims.Make(PrimitiveType.Cylinder, r, new Vector3(0, 0.9f, 0.3f), new Vector3(0.05f, 0.6f, 0.05f), Palette.Mg3MachineDark, "Mast");
            for (int i = 0; i < 2; i++)
                Crate(r).transform.localPosition = new Vector3(-0.15f + i * 0.3f, 0.39f, 0.45f);
            return r.gameObject;
        }

        /// <summary>Polar-Express: Lok und ein Wagen, längs der Gleise (z-Richtung).</summary>
        public static GameObject TrainSet(Transform parent)
        {
            var r = Root("PolarExpress", parent).transform;
            for (int i = 0; i < 2; i++)
            {
                float z = i * 1.5f;
                Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.36f, z), new Vector3(0.62f, 0.56f, 1.36f), Palette.Mg3Train, i == 0 ? "Engine" : "Wagon");
                Prims.Make(PrimitiveType.Cube, r, new Vector3(-0.32f, 0.46f, z), new Vector3(0.02f, 0.18f, 1.0f), Palette.Mg3TrainWindow, "Window");
            }
            Prims.Make(PrimitiveType.Capsule, r, new Vector3(0, 0.36f, -0.72f), new Vector3(0.6f, 0.3f, 0.6f), Palette.Mg3Train, "Nose")
                .transform.localRotation = Quaternion.Euler(90, 0, 0);
            return r.gameObject;
        }

        /// <summary>Möwe: heller Körper, graue Flügel, oranger Schnabel.</summary>
        public static GameObject Gull(Transform parent)
        {
            var r = Root("Gull", parent).transform;
            Prims.Make(PrimitiveType.Sphere, r, Vector3.zero, new Vector3(0.18f, 0.16f, 0.32f), Palette.Mg3Gull, "Body");
            Prims.Make(PrimitiveType.Sphere, r, new Vector3(0, 0.08f, 0.16f), Vector3.one * 0.13f, Palette.Mg3Gull, "Head");
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0.07f, 0.25f), new Vector3(0.04f, 0.03f, 0.08f), Palette.Beak, "Beak");
            Prims.Make(PrimitiveType.Cube, r, new Vector3(-0.22f, 0.04f, 0), new Vector3(0.32f, 0.02f, 0.14f), Palette.Mg3GullWing, "WingL");
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0.22f, 0.04f, 0), new Vector3(0.32f, 0.02f, 0.14f), Palette.Mg3GullWing, "WingR");
            return r.gameObject;
        }

        /// <summary>Flacher Pfeil am Boden von <paramref name="from"/> nach <paramref name="to"/> (Weltkoordinaten).</summary>
        public static GameObject Arrow(Transform parent, Vector3 from, Vector3 to, Color color)
        {
            var r = Root("Arrow", parent).transform;
            var dir = to - from;
            dir.y = 0;
            float len = Mathf.Max(0.2f, dir.magnitude - 0.5f);
            r.position = (from + to) / 2 + Vector3.up * 0.9f;
            r.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0, -0.2f), new Vector3(0.16f, 0.08f, Mathf.Max(0.1f, len - 0.4f)), color, "Shaft");
            var head = Prims.Make(PrimitiveType.Cube, r, new Vector3(0, 0, len / 2 - 0.3f), new Vector3(0.4f, 0.08f, 0.4f), color, "Head");
            head.transform.localRotation = Quaternion.Euler(0, 45, 0);
            return r.gameObject;
        }
    }
}
