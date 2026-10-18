using System.Collections;
using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Einstiegspunkt des Vertical Slice. Baut die ganze Welt per Code (keine Prefabs, keine Szenen-YAML)
    /// und führt durch den Ablauf: Titel → Hub → MG1 „Die erste Siedlung“ (Schritte 1–5) → „So macht's Siemens“
    /// → Hub mit Siedlung → Abschluss.
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

            while (true)
            {
                yield return Hud.FadeOut();
                _hub.Exit();
                yield return SettlementMinigame.Create(this, MinigameOrigin).Play();
                State.Mg1Done = true;

                Hud.ShowPhases(5);
                yield return TransferScreen.Play(this);
                Hud.ShowPhases(-1);
                State.MapCards = Mathf.Max(State.MapCards, 1);
                State.Takeaway = GameData.Loc(Data.TransferCard["takeaway"]);

                _hub.Enter();
                _hub.ShowSettlement(State.Mg1BestLayout, State.Mg1BestResult, State.Mg1BestFinalRows);
                _hub.SetIdleNode("grete", "hub_after_grete");
                _hub.SetIdleNode("kalle", "hub_after_kalle");
                yield return Hud.FadeIn();
                yield return Hud.Say("hub_after_grete");

                bool replay = false;
                yield return EndScreen(r => replay = r);
                if (!replay) break;
                yield return _hub.WaitForTalk("frieda");
                yield return Hud.Say("hub_after_frieda");
            }
            Hud.Toast(Data.Ui("hubThanks"), 6f);
        }

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

        private IEnumerator EndScreen(System.Action<bool> result)
        {
            var layer = new GameObject("End");
            var rt = UIKit.Rect(layer, Hud.Root);
            UIKit.Fill(rt);
            rt.SetSiblingIndex(0);
            var panel = UIKit.Panel(rt, Palette.PanelLight, "Panel");
            UIKit.PlaceFixed(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1200, 760));
            var title = UIKit.Label(panel.transform, Data.Ui("endTitle", State.MapCards, GameState.MapCardsTotal), 40, Palette.Text,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 20, -90, 20, 20);

            var card = UIKit.Panel(panel.transform, Palette.Card, "Card");
            UIKit.Place(card.rectTransform, 0, 1, 1, 1, 40, -330, 40, 110);
            var c = Data.TransferCard;
            var cardText = UIKit.Label(card.transform,
                Data.Ui("endCard", Data.Ui("transferBusiness"), c["siemensBusiness"], GameData.Loc(c["takeaway"]), Mathf.Max(0, State.Mg1BestStars)),
                28, Palette.Text, TextAnchor.MiddleLeft);
            UIKit.Place(cardText.rectTransform, 0, 0, 1, 1, 30, 10, 30, 10);

            var gloss = UIKit.Label(panel.transform, Data.Ui("endGlossary") + "\n" + string.Join("\n", State.Glossary), 24, Palette.Text,
                TextAnchor.UpperLeft);
            UIKit.Place(gloss.rectTransform, 0, 0, 1, 1, 40, 150, 40, 360);

            bool? answer = null;
            var again = UIKit.Button(panel.transform, Data.Ui("endReplay"), () => answer = true, Palette.ButtonAlt, 28);
            UIKit.PlaceFixed((RectTransform)again.transform, new Vector2(0.5f, 0), new Vector2(-220, 75), new Vector2(380, 90));
            var done = UIKit.Button(panel.transform, Data.Ui("endDone"), () => answer = false, Palette.Button, 28);
            UIKit.PlaceFixed((RectTransform)done.transform, new Vector2(0.5f, 0), new Vector2(220, 75), new Vector2(380, 90));
            while (answer == null) yield return null;
            Destroy(layer);
            if (answer == true) Hud.Toast(Data.Ui("hubReplayHint"), 4f);
            result(answer == true);
        }
    }
}
