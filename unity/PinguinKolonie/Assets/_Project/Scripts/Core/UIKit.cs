using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.UI;

namespace SFE.Game
{
    /// <summary>
    /// Erzeugt die uGUI-Oberfläche per Code (kein Prefab, kein TextMeshPro-Import nötig).
    /// Referenzauflösung 1920 × 1080; skaliert auf Laptop und Tablet.
    /// </summary>
    public static class UIKit
    {
        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(GameObject go, Transform parent)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Anker in Bildschirmanteilen (0…1) plus Pixel-Abstände.</summary>
        public static RectTransform Place(RectTransform rt, float minX, float minY, float maxX, float maxY,
            float padL = 0, float padB = 0, float padR = 0, float padT = 0)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = new Vector2(padL, padB);
            rt.offsetMax = new Vector2(-padR, -padT);
            return rt;
        }

        /// <summary>Feste Größe um einen Ankerpunkt.</summary>
        public static RectTransform PlaceFixed(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(Transform parent, Color color, string name = "Panel")
        {
            var go = new GameObject(name);
            Rect(go, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text");
            Rect(go, parent);
            var t = go.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick, Color? color = null, int fontSize = 30)
        {
            var img = Panel(parent, color ?? Palette.Button, "Button");
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            b.colors = colors;
            var t = Label(img.transform, label, fontSize, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(t.rectTransform, 0, 0, 1, 1, 12, 6, 12, 6);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        public static void SetButtonLabel(Button b, string label) => b.GetComponentInChildren<Text>().text = label;

        private static readonly Dictionary<string, Texture2D> _formulas = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Eine wie LaTeX gesetzte Formel (nur „Blick unter die Haube“). Quelle: <c>underTheHood.formulas</c> in ui_de.json,
        /// gesetzt von <c>tools/render_formulas.py</c>. <paramref name="scale"/> 0,5 ≈ Schriftgröße 33; Pivot links mittig.
        /// </summary>
        public static RawImage Formula(Transform parent, string name, Color color, float scale = 0.5f)
        {
            var go = new GameObject("Formula_" + name);
            Rect(go, parent);
            var img = go.AddComponent<RawImage>();
            img.texture = FormulaTexture(name);
            img.color = color;
            img.raycastTarget = false;
            img.rectTransform.pivot = new Vector2(0, 0.5f);
            img.rectTransform.sizeDelta = new Vector2(img.texture.width, img.texture.height) * scale;
            return img;
        }

        /// <summary>Breite · Höhe (je uint32) · Deckkraft von oben nach unten, roh deflate – siehe render_formulas.py.</summary>
        private static Texture2D FormulaTexture(string name)
        {
            if (_formulas.TryGetValue(name, out var cached)) return cached;
            var asset = Resources.Load<TextAsset>("Formulas/" + name);
            if (asset == null) throw new InvalidOperationException($"Formel '{name}' fehlt – python tools/render_formulas.py");
            var raw = asset.bytes;
            int w = BitConverter.ToInt32(raw, 0), h = BitConverter.ToInt32(raw, 4);
            var alpha = new byte[w * h];
            using (var z = new DeflateStream(new MemoryStream(raw, 8, raw.Length - 8), CompressionMode.Decompress))
            {
                int read = 0;
                while (read < alpha.Length)
                {
                    int n = z.Read(alpha, read, alpha.Length - read);
                    if (n == 0) throw new InvalidOperationException($"Formel '{name}' ist beschädigt");
                    read += n;
                }
            }
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) // Unity zählt Zeilen von unten
                for (int x = 0; x < w; x++)
                    px[(h - 1 - y) * w + x] = new Color32(255, 255, 255, alpha[y * w + x]);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Formula_" + name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _formulas[name] = tex;
            return tex;
        }

        public static void Fill(RectTransform rt) => Place(rt, 0, 0, 1, 1);
    }
}
