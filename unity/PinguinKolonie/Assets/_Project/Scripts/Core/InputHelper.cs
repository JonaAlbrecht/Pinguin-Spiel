using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace SFE.Game
{
    /// <summary>
    /// Maus, Touch und Tastatur über eine Schnittstelle. Frage C-01 sah nur Zeigen/Klicken vor; seit v0.2 läuft Pip
    /// im Hub auch mit WASD/Pfeiltasten (Wunsch aus dem ersten Spieltest). Klicken bleibt für Tablets.
    /// Funktioniert mit dem alten Input Manager und mit dem neuen Input System.
    /// </summary>
    public static class InputHelper
    {
        /// <summary>
        /// Laufrichtung aus WASD und Pfeiltasten: x = rechts, y = vorwärts (je −1…1, diagonal normiert).
        /// Direkt über die Tasten statt über die Achse „Horizontal“ – die ist geglättet und lässt Pip nachrutschen.
        /// </summary>
        public static Vector2 MoveInput
        {
            get
            {
                float x = 0f, y = 0f;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
                    if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
                    if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;
                }
#else
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
#endif
                var v = new Vector2(x, y);
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        /// <summary>E, Leertaste oder Enter: Figur ansprechen bzw. im Dialog weiterblättern.</summary>
        public static bool ConfirmPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var k = Keyboard.current;
                return k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame ||
                                     k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
#else
                return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) ||
                       Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
            }
        }

        /// <summary>
        /// Zoom in Rastschritten, positiv = näher heran: Mausrad, Touchpad (zwei Finger streichen oder spreizen –
        /// Windows meldet beides als Scrollen), Zwei-Finger-Pinch auf Touchscreens, Tasten + und −.
        /// </summary>
        public static float ZoomDelta
        {
            get
            {
                float steps = 0f;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                if (Mouse.current != null) steps += Mouse.current.scroll.ReadValue().y / 120f;
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.equalsKey.isPressed || k.numpadPlusKey.isPressed) steps += 4f * Time.unscaledDeltaTime;
                    if (k.minusKey.isPressed || k.numpadMinusKey.isPressed) steps -= 4f * Time.unscaledDeltaTime;
                }
                var ts = Touchscreen.current;
                if (ts != null)
                {
                    var t0 = ts.touches[0];
                    var t1 = ts.touches[1];
                    if (t0.press.isPressed && t1.press.isPressed)
                    {
                        Vector2 p0 = t0.position.ReadValue(), p1 = t1.position.ReadValue();
                        Vector2 d0 = t0.delta.ReadValue(), d1 = t1.delta.ReadValue();
                        steps += (Vector2.Distance(p0, p1) - Vector2.Distance(p0 - d0, p1 - d1)) / 80f;
                    }
                }
#else
                steps += Input.mouseScrollDelta.y;
                if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus)) steps += 4f * Time.unscaledDeltaTime;
                if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) steps -= 4f * Time.unscaledDeltaTime;
                if (Input.touchCount == 2)
                {
                    Touch a = Input.GetTouch(0), b = Input.GetTouch(1);
                    steps += (Vector2.Distance(a.position, b.position) -
                              Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition)) / 80f;
                }
#endif
                return steps;
            }
        }

        public static Vector2 PointerPosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        public static bool PointerDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(0);
#endif
            }
        }

        public static bool PointerHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Pointer.current != null && Pointer.current.press.isPressed;
#else
                return Input.GetMouseButton(0);
#endif
            }
        }

        public static bool PointerUp
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;
#else
                return Input.GetMouseButtonUp(0);
#endif
            }
        }

        /// <summary>True, wenn der Zeiger gerade über einem UI-Element liegt (dann keine Welt-Klicks).</summary>
        public static bool PointerOverUI
        {
            get
            {
                var es = EventSystem.current;
                if (es == null) return false;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return es.IsPointerOverGameObject();
#else
                if (Input.touchCount > 0) return es.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
                return es.IsPointerOverGameObject();
#endif
            }
        }

        /// <summary>Schnittpunkt des Zeigerstrahls mit der Bodenebene y = height.</summary>
        public static bool PointerOnGround(Camera cam, out Vector3 point, float height = 0f)
        {
            var ray = cam.ScreenPointToRay(PointerPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
            point = Vector3.zero;
            return false;
        }

        public static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            // Ohne Navigation drückt Leertaste/Enter nicht den zuletzt geklickten Knopf erneut – diese Tasten
            // blättern im Dialog weiter und sprechen Figuren an; sonst löste man nebenbei z. B. „Simulieren“ aus.
            go.AddComponent<EventSystem>().sendNavigationEvents = false;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
