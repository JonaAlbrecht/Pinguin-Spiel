using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace SFE.Game
{
    /// <summary>
    /// Maus und Touch über eine Schnittstelle (Frage C-01: Steuerung nur Zeigen/Klicken/Ziehen).
    /// Funktioniert mit dem alten Input Manager und mit dem neuen Input System.
    /// </summary>
    public static class InputHelper
    {
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
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
