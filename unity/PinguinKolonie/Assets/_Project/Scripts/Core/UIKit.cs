using System;
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
            colors.highlightedColor = Palette.ButtonHoverTint;
            colors.pressedColor = Palette.ButtonPressTint;
            colors.disabledColor = Palette.ButtonDisabledTint;
            b.colors = colors;
            var t = Label(img.transform, label, fontSize, Palette.TextLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(t.rectTransform, 0, 0, 1, 1, 12, 6, 12, 6);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        public static void SetButtonLabel(Button b, string label) => b.GetComponentInChildren<Text>().text = label;

        public static void Fill(RectTransform rt) => Place(rt, 0, 0, 1, 1);
    }
}
