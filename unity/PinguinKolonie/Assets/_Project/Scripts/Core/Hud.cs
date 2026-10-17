using System.Collections;
using System.Collections.Generic;
using SFE.SimCore.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SFE.Game
{
    /// <summary>
    /// Gemeinsame Bildschirm-Elemente: Sprechblasen, Hinweise („Toast“), Phasenleiste, Bildschirm-Blende.
    /// </summary>
    public sealed class Hud
    {
        private readonly GameData _data;
        private readonly MonoBehaviour _host;
        public readonly Canvas Canvas;
        public readonly RectTransform Root;

        private readonly Image _dialogBox;
        private readonly Text _speaker;
        private readonly Text _line;
        private readonly Text _continueHint;
        private readonly Image _continueChip;
        private bool _advance;

        private readonly Image _toastBox;
        private readonly Text _toastText;
        private float _toastUntil;

        private readonly Image _phaseBar;
        private readonly List<Text> _phaseLabels = new List<Text>();
        private readonly Image _fade;

        private readonly Button _notebookButton;
        private GameObject _notebook;
        private GameObject _popup;
        private readonly List<CanvasGroup> _locked = new List<CanvasGroup>();
        private readonly List<(GameObject go, System.Action onHidden)> _techOnly = new List<(GameObject, System.Action)>();
        private GameState _state;

        public bool DialogOpen => _dialogBox.gameObject.activeSelf;
        public bool NotebookOpen => _notebook != null;
        public bool PopupOpen => _popup != null;
        /// <summary>Solange ein Dialog, das Notizbuch oder ein Pop-up offen ist, ruht das Spiel (Eingabe, Wind, Zeitbalken).</summary>
        public bool Blocking => DialogOpen || NotebookOpen || PopupOpen;
        /// <summary>Wie sehr ZWILLI gerade stottert (0…1); setzt das Minigame je Schritt.</summary>
        public float ZwilliGlitch = 0.6f;

        public Hud(GameData data, MonoBehaviour host)
        {
            _data = data;
            _host = host;
            Canvas = UIKit.CreateCanvas("HUD", 10);
            Root = (RectTransform)Canvas.transform;

            // Phasenleiste oben
            _phaseBar = UIKit.Panel(Root, new Color(0.08f, 0.12f, 0.2f, 0.85f), "PhaseBar");
            UIKit.Place(_phaseBar.rectTransform, 0.05f, 1, 0.95f, 1, 0, -70, 0, 10);
            var phases = data.Mg1Content.Arr("phases");
            for (int i = 0; i < phases.Count; i++)
            {
                var p = (Dictionary<string, object>)phases[i];
                var t = UIKit.Label(_phaseBar.transform, (i + 1) + "  " + p.Str("title"), 24, new Color(1, 1, 1, 0.45f));
                UIKit.Place(t.rectTransform, i / (float)phases.Count, 0, (i + 1) / (float)phases.Count, 1, 6, 0, 6, 0);
                _phaseLabels.Add(t);
            }
            _phaseBar.gameObject.SetActive(false);

            // Sprechblase unten
            _dialogBox = UIKit.Panel(Root, Palette.PanelLight, "Dialog");
            UIKit.Place(_dialogBox.rectTransform, 0.15f, 0, 0.85f, 0, 0, 30, 0, -250);
            var btn = _dialogBox.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => _advance = true);
            _speaker = UIKit.Label(_dialogBox.transform, "", 30, Palette.Text, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(_speaker.rectTransform, 0, 0, 1, 1, 32, 0, 32, 18);
            _line = UIKit.Label(_dialogBox.transform, "", 34, Palette.Text, TextAnchor.UpperLeft);
            UIKit.Place(_line.rectTransform, 0, 0, 1, 1, 32, 40, 250, 64);
            // „Weiter »“ als sichtbarer Knopf: Solange jemand spricht, geht es nur hier weiter (Spieltest 17.10.:
            // der blasse Hinweis unten rechts ging neben „Nächster Zeitschritt“ unter). Der Klick zählt für die
            // ganze Sprechblase, der Knopf fängt ihn nicht selbst ab.
            _continueChip = UIKit.Panel(_dialogBox.transform, Palette.Button, "Continue");
            _continueChip.raycastTarget = false;
            UIKit.PlaceFixed(_continueChip.rectTransform, new Vector2(1, 0), new Vector2(-130, 42), new Vector2(200, 56));
            _continueHint = UIKit.Label(_continueChip.transform, data.Ui("continueHint"), 26, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Fill(_continueHint.rectTransform);
            _dialogBox.gameObject.SetActive(false);

            // Hinweis-Leiste
            _toastBox = UIKit.Panel(Root, new Color(0.1f, 0.14f, 0.22f, 0.9f), "Toast");
            UIKit.PlaceFixed(_toastBox.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(1100, 70));
            _toastText = UIKit.Label(_toastBox.transform, "", 28, Palette.TextLight);
            UIKit.Fill(_toastText.rectTransform);
            _toastBox.raycastTarget = false;
            _toastBox.gameObject.SetActive(false);

            // Notizbuch und Ton oben rechts (docs/00 Berührungspunkt 4: Notizbuch jederzeit erreichbar)
            _notebookButton = UIKit.Button(Root, data.Ui("notebookButton"), ToggleNotebook, new Color(0.45f, 0.32f, 0.2f), 24);
            // Unter der Phasenleiste (y −10…−70), sonst verdeckt der Knopf „6 So macht's Siemens“.
            UIKit.PlaceFixed((RectTransform)_notebookButton.transform, new Vector2(1, 1), new Vector2(-110, -108), new Vector2(190, 52));
            _notebookButton.gameObject.SetActive(false);
            // Einstellungen (Ton, Lautstärke, Tempo, Bild …) statt eines einzelnen Ton-Knopfs – jederzeit erreichbar
            var settings = UIKit.Button(Root, data.Ui("settingsButton"), OpenSettings, new Color(0.25f, 0.3f, 0.4f), 22);
            UIKit.PlaceFixed((RectTransform)settings.transform, new Vector2(1, 1), new Vector2(-110, -166), new Vector2(190, 44));
            Settings.Changed += ApplyTechVisibility;

            // Blende
            _fade = UIKit.Panel(Root, new Color(0.05f, 0.08f, 0.14f, 0f), "Fade");
            UIKit.Fill(_fade.rectTransform);
            _fade.raycastTarget = false;
        }

        public void Tick()
        {
            Sfx.Tick();
            if (_toastBox.gameObject.activeSelf && Time.time > _toastUntil) _toastBox.gameObject.SetActive(false);
            if (DialogOpen) _continueChip.color = Color.Lerp(Palette.Button, Color.white, 0.18f + 0.18f * Mathf.Sin(Time.time * 4f));
            // Gesperrte Bedienelemente: ausgegraut und nicht klickbar, solange ein Dialog oder Fenster offen ist
            bool blocked = Blocking;
            _locked.RemoveAll(g => g == null);
            foreach (var g in _locked)
            {
                g.interactable = !blocked;
                g.alpha = blocked ? 0.45f : 1f;
            }
        }

        /// <summary>Spielt einen Dialogknoten aus dialogue_de.json ab; jede Zeile wartet auf Klick.</summary>
        public IEnumerator Say(string nodeId)
        {
            if (!_data.Dialogue.TryGetValue(nodeId, out var lines))
            {
                Debug.LogWarning("Dialogknoten fehlt: " + nodeId);
                yield break;
            }
            // Immer nur ein Dialog: Startet ein zweiter (z. B. Figur im Hub angeklickt, während der Ablauf spricht),
            // wartet er. Zwei gleichzeitige Dialoge teilten sich _advance – einer blieb dann hängen (QA-Befund).
            while (DialogOpen) yield return null;
            if (NotebookOpen) ToggleNotebook();
            ClosePopup();
            _dialogBox.gameObject.SetActive(true);
            _dialogBox.transform.SetAsLastSibling();
            foreach (var line in lines)
            {
                if (_data.Speakers.TryGetValue(line.Speaker, out var sp))
                {
                    _speaker.text = sp.Name;
                    _speaker.color = sp.Color;
                }
                else _speaker.text = line.Speaker;
                _line.text = line.Text;
                if (line.Speaker == "zwilli") Sfx.Speak(ZwilliGlitch);
                else Sfx.Play(Sfx.Sound.Click, 0.4f);
                _advance = false;
                yield return null; // die Taste, die den Dialog gestartet hat, blättert nicht gleich weiter
                while (!_advance)
                {
                    if (InputHelper.ConfirmPressed && !PopupOpen) _advance = true;
                    yield return null;
                }
            }
            _dialogBox.gameObject.SetActive(false);
            _fade.transform.SetAsLastSibling();
            _closedFrame = Time.frameCount;
        }

        private int _closedFrame = -10;

        /// <summary>
        /// Gerade eben (diesen oder letzten Frame) ging ein Dialog zu. Dann startet E/Leertaste kein neues Gespräch –
        /// sonst öffnete der Tastendruck, der die letzte Zeile wegklickt, sofort das nächste.
        /// </summary>
        public bool JustClosed => Time.frameCount - _closedFrame <= 1;

        /// <summary>Ab jetzt ist das Notizbuch oben rechts erreichbar.</summary>
        public void EnableNotebook(GameState state)
        {
            _state = state;
            _notebookButton.gameObject.SetActive(true);
        }

        private void ToggleNotebook()
        {
            if (_notebook != null) { Object.Destroy(_notebook); _notebook = null; return; }
            if (_state == null) return;
            Sfx.Play(Sfx.Sound.Click);
            _notebook = new GameObject("Notebook");
            var rt = UIKit.Rect(_notebook, Root);
            UIKit.Fill(rt);
            var shade = UIKit.Panel(rt, new Color(0, 0, 0, 0.45f), "Shade");
            UIKit.Fill(shade.rectTransform);
            var page = UIKit.Panel(rt, new Color(0.99f, 0.97f, 0.9f), "Page");
            UIKit.PlaceFixed(page.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1100, 800));
            var title = UIKit.Label(page.transform, _data.Ui("notebookTitle"), 40, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, 0, 1, 1, 1, 20, -80, 20, 15);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(_data.Ui("notebookTerms"));
            if (_state.Glossary.Count == 0) sb.Append("–  ").AppendLine(_data.Ui("notebookEmpty"));
            foreach (var g in _state.Glossary) sb.Append("–  ").AppendLine(g);
            sb.AppendLine().AppendLine(_data.Ui("notebookMap", _state.MapCards, GameState.MapCardsTotal));
            if (_state.Takeaway != null)
                sb.AppendLine().AppendLine(_data.Ui("notebookSentence")).Append('„').Append(_state.Takeaway).AppendLine("“");
            sb.AppendLine().Append(_data.Ui("notebookPuddles", _state.Puddles));
            var body = UIKit.Label(page.transform, sb.ToString(), 26, Palette.Text, TextAnchor.UpperLeft);
            UIKit.Place(body.rectTransform, 0, 0, 1, 1, 60, 120, 60, 100);

            var close = UIKit.Button(page.transform, _data.Ui("close"), ToggleNotebook, Palette.Button, 28);
            UIKit.PlaceFixed((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(280, 80));
            _notebook.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Eigenes Fenster über allem (z. B. „Blick unter die Haube“): abgedunkelter Hintergrund, Titel oben,
        /// „Schließen“ unten. Gibt die Seite zurück; Inhalt mit Anker oben links (0, 1) und negativem y daraufsetzen.
        /// </summary>
        public RectTransform OpenPopup(string title, Vector2 size)
        {
            ClosePopup();
            if (NotebookOpen) ToggleNotebook();
            Sfx.Play(Sfx.Sound.Click);
            _popup = new GameObject("Popup");
            var rt = UIKit.Rect(_popup, Root);
            UIKit.Fill(rt);
            var shade = UIKit.Panel(rt, new Color(0, 0, 0, 0.55f), "Shade");
            UIKit.Fill(shade.rectTransform);
            shade.gameObject.AddComponent<Button>().onClick.AddListener(ClosePopup); // Klick daneben schließt
            var page = UIKit.Panel(rt, Palette.PanelLight, "Page");
            UIKit.PlaceFixed(page.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var head = UIKit.Label(page.transform, title, 36, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(head.rectTransform, 0, 1, 1, 1, 60, -80, 60, 20);
            var close = UIKit.Button(page.transform, _data.Ui("close"), ClosePopup, Palette.Button, 26);
            UIKit.PlaceFixed((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(260, 64));
            _popup.transform.SetAsLastSibling();
            _fade.transform.SetAsLastSibling();
            return page.rectTransform;
        }

        public void ClosePopup()
        {
            if (_popup == null) return;
            Object.Destroy(_popup);
            _popup = null;
        }

        /// <summary>
        /// Das Element (Knopf oder Leiste) ist ausgegraut und nicht klickbar, solange <see cref="Blocking"/> gilt –
        /// so sieht man, dass es gerade nur in der Sprechblase weitergeht. Eigene interactable-Werte bleiben unberührt.
        /// </summary>
        public void LockWhileBlocking(Component element)
        {
            var g = element.GetComponent<CanvasGroup>();
            if (g == null) g = element.gameObject.AddComponent<CanvasGroup>();
            _locked.Add(g);
        }

        /// <summary>
        /// Gehört zum Modus „Blick unter die Haube“ und verschwindet, wenn er in den Einstellungen ausgeblendet wird.
        /// <paramref name="onHidden"/> räumt dazu Offenes weg (z. B. das Technik-Feld der Siedlung).
        /// </summary>
        public void TechOnly(GameObject go, System.Action onHidden = null)
        {
            _techOnly.Add((go, onHidden));
            go.SetActive(Settings.TechButtons);
        }

        private void ApplyTechVisibility()
        {
            _techOnly.RemoveAll(t => t.go == null);
            foreach (var (go, onHidden) in _techOnly)
            {
                go.SetActive(Settings.TechButtons);
                if (!Settings.TechButtons) onHidden?.Invoke();
            }
        }

        private void OpenSettings()
        {
            var rows = new List<(string label, string[] options, System.Func<int> get, System.Action<int> set)>
            {
                ("settingSound", new[] { "settingOn", "settingOff" }, () => Settings.Muted ? 1 : 0, i => Settings.Muted = i == 1),
                ("settingVolume", new[] { "settingQuiet", "settingMedium", "settingLoud" }, () => Settings.Volume, i => Settings.Volume = i),
                ("settingPace", new[] { "settingPaceCalm", "settingPaceNormal" }, () => Settings.Pace, i => Settings.Pace = i),
                ("settingQuality", new[] { "settingQualityHigh", "settingQualityLow" }, () => Settings.HighQuality ? 0 : 1, i => Settings.HighQuality = i == 0),
                ("settingTech", new[] { "settingOn", "settingOff" }, () => Settings.TechButtons ? 0 : 1, i => Settings.TechButtons = i == 0),
            };
            if (Settings.FullscreenAvailable)
                rows.Add(("settingFullscreen", new[] { "settingOn", "settingOff" }, () => Settings.Fullscreen ? 0 : 1, i => Settings.Fullscreen = i == 0));

            const float rowH = 78;
            var page = OpenPopup(_data.Ui("settingsTitle"), new Vector2(1000, 260 + rows.Count * rowH));
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                float y = -135 - r * rowH;
                var label = UIKit.Label(page, _data.Ui(row.label), 28, Palette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.PlaceFixed(label.rectTransform, new Vector2(0, 1), new Vector2(60 + 210, y), new Vector2(420, 60));
                var buttons = new Button[row.options.Length];
                void Mark()
                {
                    int sel = row.get();
                    for (int i = 0; i < buttons.Length; i++)
                        buttons[i].GetComponent<Image>().color = i == sel ? Palette.Button : Palette.ButtonAlt;
                }
                for (int i = 0; i < row.options.Length; i++)
                {
                    int choice = i;
                    buttons[i] = UIKit.Button(page, _data.Ui(row.options[i]), () => { row.set(choice); Sfx.Play(Sfx.Sound.Click); Mark(); },
                        Palette.ButtonAlt, 24);
                    UIKit.PlaceFixed((RectTransform)buttons[i].transform, new Vector2(0, 1), new Vector2(570 + i * 145, y), new Vector2(135, 56));
                }
                Mark();
            }
            var note = UIKit.Label(page, _data.Ui("settingsNote"), 22, new Color(0.35f, 0.4f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Italic);
            UIKit.Place(note.rectTransform, 0, 0, 1, 0, 60, 95, 60, -135);
        }

        public void Toast(string text, float seconds = 3.5f)
        {
            _toastText.text = text;
            _toastBox.gameObject.SetActive(true);
            _toastBox.transform.SetAsLastSibling();
            _toastUntil = Time.time + seconds;
        }

        public void ShowPhases(int activeIndex)
        {
            _phaseBar.gameObject.SetActive(activeIndex >= 0);
            for (int i = 0; i < _phaseLabels.Count; i++)
            {
                _phaseLabels[i].color = i == activeIndex ? Palette.Selected
                    : i < activeIndex ? new Color(0.6f, 0.9f, 0.7f, 0.9f) : new Color(1, 1, 1, 0.45f);
                _phaseLabels[i].fontStyle = i == activeIndex ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        public IEnumerator FadeOut(float seconds = 0.35f) => FadeTo(1f, seconds);
        public IEnumerator FadeIn(float seconds = 0.35f) => FadeTo(0f, seconds);

        private IEnumerator FadeTo(float target, float seconds)
        {
            _fade.transform.SetAsLastSibling();
            float start = _fade.color.a;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                SetFade(Mathf.Lerp(start, target, t / seconds));
                yield return null;
            }
            SetFade(target);
        }

        private void SetFade(float a)
        {
            var c = _fade.color;
            c.a = a;
            _fade.color = c;
            _fade.raycastTarget = a > 0.5f;
        }

        public Coroutine Run(IEnumerator routine) => _host.StartCoroutine(routine);
    }
}
