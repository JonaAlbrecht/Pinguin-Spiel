using System.Collections;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Einstiegspunkt des Vertical Slice. Baut die ganze Welt per Code (keine Prefabs, keine Szenen-YAML)
    /// und führt durch den Ablauf: Titel → Hub mit Kalles Versuch draußen → MG1 „Die erste Siedlung“ → „So macht's Siemens“
    /// → Hub → Tilda am Gleis: MG2 „Polar-Express“ → Transfer → Hub → Olaf an der Küste: MG3 „Fischfabrik“ → Transfer
    /// → Abschluss mit Siemens-Landkarte; danach lässt sich jedes Minigame wiederholen.
    /// In eine leere Szene legen oder über das Menü „Pinguin-Spiel → Prototyp-Szene erstellen“ anlegen.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        public GameData Data { get; private set; }
        public GameState State { get; } = new GameState();
        public Hud Hud { get; private set; }
        public CameraRig Rig { get; private set; }

        private HubWorld _hub;
        private static readonly Vector3 MinigameOrigin = new Vector3(200f, 0f, 0f);
        /// <summary>MG3 baut seine Halle 60 m südlich des Labors – eigener Ursprung, damit nichts mit MG1/MG2 überlappt.</summary>
        private static readonly Vector3 FactoryOrigin = new Vector3(400f, 0f, 0f);

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Data = GameData.Load();
            Rig = CameraRig.Create();
            RenderQuality.Apply(Rig.Cam, CreateLight());
            Sfx.Init(Rig.gameObject);
            if (UnityEngine.EventSystems.EventSystem.current == null) InputHelper.CreateEventSystem();
            Hud = new Hud(Data, this);
            _hub = HubWorld.Create(this);
        }

        private void Start() => StartCoroutine(Flow());

        private void Update() => Hud.Tick();

        private static Light CreateLight()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            ArtStyle.Shading.ApplyTo(sun);
            return sun;
        }

        private IEnumerator Flow()
        {
            yield return TitleScreen();

            // Hub: Oma Grete erinnert an den Huddle, Frieda ruft ins Labor
            _hub.Enter();
            yield return Hud.FadeIn();
            yield return Hud.Say("hub_intro");
            Hud.Toast(Data.Ui("hubControls"), 7f);
            Hud.EnableNotebook(State);
            _hub.SetIdleNode("theo", "hub_theo");
            _hub.SetIdleNode("klara", "hub_klara");
            _hub.SetIdleNode("flocke", "hub_flocke");
            yield return _hub.WaitForTalk("frieda");

            // Kalle kommt Frieda zuvor – draußen, in echt (kein Raster, kein ZWILLI). Danach geht es ins Labor.
            yield return _hub.PlayKalleAttempt(Data.Settlement, Data.Materials, State);

            yield return PlaySettlement();
            yield return Hud.Say("hub_after_grete");

            // Tilda wartet am Gleis: Der Polar-Express soll die Siedlung mit der Küste verbinden.
            _hub.ShowNpc("tilda", true);
            yield return Hud.Say("hub_to_tilda");
            yield return _hub.WaitForTalk("tilda");
            yield return PlayPolarExpress();

            // Olaf an der Küste: Der Zug bringt Fischer, jetzt staut sich der Fisch.
            _hub.ShowNpc("olaf", true);
            yield return Hud.Say("hub_to_olaf");
            yield return _hub.WaitForTalk("olaf");
            yield return PlayFishFactory();
            yield return Hud.Say("hub_all_done");

            while (true)
            {
                int replay = 0;
                yield return EndScreen(r => replay = r);
                if (replay == 0) break;
                if (replay == 1)
                {
                    yield return _hub.WaitForTalk("frieda");
                    yield return Hud.Say("hub_after_frieda");
                    yield return PlaySettlement();
                }
                else if (replay == 2)
                {
                    yield return _hub.WaitForTalk("tilda");
                    yield return PlayPolarExpress();
                }
                else
                {
                    yield return _hub.WaitForTalk("olaf");
                    yield return PlayFishFactory();
                }
            }
            Hud.Toast(Data.Ui("hubThanks"), 6f);
        }

        private IEnumerator PlaySettlement()
        {
            yield return Hud.FadeOut();
            _hub.Exit();
            yield return SettlementMinigame.Create(this, MinigameOrigin).Play();
            State.Mg1Done = true;

            Hud.ShowPhases(5);
            yield return TransferScreen.Play(this);
            Hud.ShowPhases(-1);
            UpdateMapCards();
            State.Takeaway = GameData.Loc(Data.TransferCard["takeaway"]);

            _hub.Enter();
            _hub.ShowSettlement(State.Mg1BestLayout, State.Mg1BestResult, State.Mg1BestFinalRows);
            _hub.SetIdleNode("grete", "hub_after_grete");
            _hub.SetIdleNode("kalle", "hub_after_kalle");
            yield return Hud.FadeIn();
        }

        private IEnumerator PlayPolarExpress()
        {
            yield return Hud.FadeOut();
            _hub.Exit();
            yield return PolarExpressMinigame.Create(this, MinigameOrigin).Play(); // mit Kalles Schlitten draußen
            Hud.ShowPhases(5);
            yield return PolarExpressTransfer.Play(this);
            Hud.ShowPhases(-1);
            UpdateMapCards();
            State.Takeaway = PolarExpressTransfer.Takeaway();

            _hub.Enter();
            _hub.SetIdleNode("tilda", "hub_tilda_after");
            yield return Hud.FadeIn();
        }

        private IEnumerator PlayFishFactory()
        {
            yield return Hud.FadeOut();
            _hub.Exit();
            yield return FishFactoryMinigame.Create(this, FactoryOrigin).Play(); // mit Kalles Versuch in der Halle
            yield return FishFactoryTransfer.Play(this);                         // setzt Phasenleiste und Takeaway selbst
            UpdateMapCards();

            _hub.Enter();
            _hub.SetIdleNode("olaf", "mg3_hub_olaf");
            yield return Hud.FadeIn();
        }

        /// <summary>Je fertigem Minigame eine Karte der Siemens-Landkarte (Schaltschrank, Mobility, Fabrik).</summary>
        private void UpdateMapCards() =>
            State.MapCards = (State.Mg1Done ? 1 : 0) + (Mg2Progress.Of(State).Done ? 1 : 0) + (FishFactoryState.Done ? 1 : 0);

        private IEnumerator TitleScreen()
        {
            var layer = new GameObject("Title");
            var rt = UIKit.Rect(layer, Hud.Root);
            UIKit.Fill(rt);
            var bg = UIKit.Panel(rt, Palette.TitleBackground, "Background");
            UIKit.Fill(bg.rectTransform);
            var title = UIKit.Label(rt, Data.Ui("titleMain"), 84, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.PlaceFixed(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1600, 120));
            var sub = UIKit.Label(rt, Data.Ui("titleSub"), 34, Palette.TitleSub, TextAnchor.MiddleCenter);
            UIKit.PlaceFixed(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1600, 60));
            var note = UIKit.Label(rt, Data.Ui("titleNote"), 26, Palette.TitleNote, TextAnchor.MiddleCenter);
            UIKit.PlaceFixed(note.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(1500, 100));
            bool go = false;
            var start = UIKit.Button(rt, Data.Ui("startGame"), () => go = true, Palette.Button, 40);
            UIKit.PlaceFixed((RectTransform)start.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(420, 110));
            while (!go) yield return null;
            Sfx.Play(Sfx.Sound.Click);
            yield return Hud.FadeOut(0.3f);
            Destroy(layer);
        }

        /// <summary>Abschluss: Landkarte, Sterne je Minigame, neue Wörter. <paramref name="result"/>: 0 = fertig, 1–3 = Minigame wiederholen.</summary>
        private IEnumerator EndScreen(System.Action<int> result)
        {
            var layer = new GameObject("End");
            var rt = UIKit.Rect(layer, Hud.Root);
            UIKit.Fill(rt);
            rt.SetSiblingIndex(0);
            var panel = UIKit.Panel(rt, Palette.PanelLight, "Panel");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1200, 800));
            var title = UIKit.Label(panel.transform, Data.Ui("endTitle", State.MapCards, GameState.MapCardsTotal), 40, Palette.Text,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 20, -90, 20, 20);

            var card = UIKit.Panel(panel.transform, Palette.Card, "Card");
            UIKit.Place(card.rectTransform, 0, 1, 1, 1, 40, -350, 40, 110);
            var cardText = UIKit.Label(card.transform,
                Data.Ui("endSummary", Mathf.Max(0, State.Mg1BestStars), Mathf.Max(0, Mg2Progress.Of(State).BestStars),
                    Mathf.Max(0, FishFactoryState.BestStars), State.Takeaway ?? ""),
                28, Palette.Text, TextAnchor.MiddleLeft);
            UIKit.Place(cardText.rectTransform, 0, 0, 1, 1, 30, 10, 30, 10);

            var gloss = UIKit.Label(panel.transform, Data.Ui("endGlossary") + "\n" + string.Join("\n", State.Glossary), 22, Palette.Text,
                TextAnchor.UpperLeft);
            UIKit.Place(gloss.rectTransform, 0, 0, 1, 1, 40, 250, 40, 370);

            int? answer = null;
            string[] replayKeys = { "endReplayMg1", "endReplayMg2", "endReplayMg3" };
            for (int i = 0; i < 3; i++)
            {
                int choice = i + 1;
                var again = UIKit.Button(panel.transform, Data.Ui(replayKeys[i]), () => answer = choice, Palette.ButtonAlt, 24);
                UIKit.PlaceFixed((RectTransform)again.transform, new Vector2(0.5f, 0), new Vector2(-370 + i * 370, 175), new Vector2(340, 80));
            }
            var done = UIKit.Button(panel.transform, Data.Ui("endDone"), () => answer = 0, Palette.Button, 28);
            UIKit.PlaceFixed((RectTransform)done.transform, new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(420, 90));
            while (answer == null) yield return null;
            Destroy(layer);
            if (answer > 0) Hud.Toast(Data.Ui("hubReplayHint" + answer), 4f);
            result(answer.Value);
        }
    }
}
