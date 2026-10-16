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
        private bool _advance;

        private readonly Image _toastBox;
        private readonly Text _toastText;
        private float _toastUntil;

        private readonly Image _phaseBar;
        private readonly List<Text> _phaseLabels = new List<Text>();
        private readonly Image _fade;

        private readonly Button _notebookButton;
        private GameObject _notebook;
        private GameState _state;

        public bool DialogOpen => _dialogBox.gameObject.activeSelf;
        public bool NotebookOpen => _notebook != null;
        /// <summary>Solange ein Dialog oder das Notizbuch offen ist, ruht das Spiel (Eingabe, Wind, Zeitbalken).</summary>
        public bool Blocking => DialogOpen || NotebookOpen;
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
            UIKit.Place(_line.rectTransform, 0, 0, 1, 1, 32, 40, 32, 64);
            _continueHint = UIKit.Label(_dialogBox.transform, data.Ui("continueHint"), 24, new Color(0.3f, 0.35f, 0.45f), TextAnchor.LowerRight);
            UIKit.Place(_continueHint.rectTransform, 0, 0, 1, 1, 32, 14, 32, 0);
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
            Button sound = null;
            sound = UIKit.Button(Root, data.Ui(Sfx.Muted ? "soundOff" : "soundOn"), () =>
            {
                Sfx.SetMuted(!Sfx.Muted);
                UIKit.SetButtonLabel(sound, _data.Ui(Sfx.Muted ? "soundOff" : "soundOn"));
            }, new Color(0.25f, 0.3f, 0.4f), 22);
            UIKit.PlaceFixed((RectTransform)sound.transform, new Vector2(1, 1), new Vector2(-110, -166), new Vector2(190, 44));

            // Blende
            _fade = UIKit.Panel(Root, new Color(0.05f, 0.08f, 0.14f, 0f), "Fade");
            UIKit.Fill(_fade.rectTransform);
            _fade.raycastTarget = false;
        }

        public void Tick()
        {
            Sfx.Tick();
            if (_toastBox.gameObject.activeSelf && Time.time > _toastUntil) _toastBox.gameObject.SetActive(false);
            if (DialogOpen) _continueHint.color = new Color(0.3f, 0.35f, 0.45f, 0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
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
                yield return null;
                while (!_advance) yield return null;
            }
            _dialogBox.gameObject.SetActive(false);
            _fade.transform.SetAsLastSibling();
        }

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
