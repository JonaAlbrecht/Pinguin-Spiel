using UnityEngine;

namespace SFE.Game
{
    /// <summary>
    /// Feste Vogelperspektive (Vorbild Animal Crossing): Pitch ~50°, kleines Sichtfeld für den Diorama-Look.
    /// Für Minigames fährt die Kamera steiler über das Spielfeld (Frage P-04: ohne Cinemachine im Prototyp).
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Cam { get; private set; }

        private Vector3 _focus;
        private float _pitch = 50f;
        private float _distance = 24f;
        private float _yaw;
        private Transform _follow;
        private float _smooth = 4f;

        /// <summary>Spieler darf zoomen (Simulationsbrett). Aus = feste Ansicht wie bisher (Hub, Szenen).</summary>
        public bool ZoomEnabled;
        /// <summary>1 = Standardansicht, kleiner = näher dran (Abstand mal Faktor).</summary>
        private float _zoom = 1f;
        /// <summary>Verschiebung des Blickpunkts beim Zoomen auf den Zeiger hin bzw. beim Schwenken.</summary>
        private Vector3 _zoomOffset;
        private const float MinZoom = 0.25f;
        private const float ZoomPerStep = 0.85f;

        public static CameraRig Create()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Sky;
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 300f;
            go.AddComponent<AudioListener>();
            var rig = go.AddComponent<CameraRig>();
            rig.Cam = cam;
            return rig;
        }

        public void Follow(Transform target, float pitch = 50f, float distance = 24f, float yaw = 0f)
        {
            ResetZoom();
            _follow = target;
            _pitch = pitch;
            _distance = distance;
            _yaw = yaw;
        }

        public void LookAt(Vector3 focus, float pitch, float distance, float yaw = 0f, bool snap = false)
        {
            // Dieselbe Ansicht noch einmal (z. B. nach jeder Runde) behält den Zoom; eine neue Ansicht beginnt von vorn.
            if (_follow != null || (focus - _focus).sqrMagnitude > 1f || Mathf.Abs(distance - _distance) > 0.5f) ResetZoom();
            _follow = null;
            _focus = focus;
            _pitch = pitch;
            _distance = distance;
            _yaw = yaw;
            if (snap) Apply(1f);
        }

        /// <summary>Abstand, damit eine quadratische Fläche der Kantenlänge <paramref name="size"/> sicher ins Bild passt.</summary>
        public float DistanceToFit(float size, float margin = 1.15f)
        {
            float vFov = Cam.fieldOfView * Mathf.Deg2Rad;
            float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov / 2f) * Cam.aspect);
            float fov = Mathf.Min(vFov, hFov);
            return size * margin / (2f * Mathf.Tan(fov / 2f));
        }

        public void ResetZoom()
        {
            _zoom = 1f;
            _zoomOffset = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (ZoomEnabled) HandleZoom();
            Apply(1f - Mathf.Exp(-_smooth * Time.deltaTime));
        }

        /// <summary>
        /// Zoom auf den Punkt unter dem Zeiger hin (Wunsch von Jona, 18.10.2026: „es ist alles sehr klein“).
        /// Die Kamera wird um diesen Bodenpunkt gestreckt – er bleibt dabei an derselben Stelle im Bild.
        /// Herauszoomen führt den Blickpunkt anteilig zurück, ganz draußen ist wieder die Standardansicht.
        /// Nah dran schwenken die Pfeiltasten bzw. WASD.
        /// </summary>
        private void HandleZoom()
        {
            float steps = InputHelper.ZoomDelta;
            if (Mathf.Abs(steps) > 1e-4f && !InputHelper.PointerOverUI)
            {
                float old = _zoom;
                _zoom = Mathf.Clamp(_zoom * Mathf.Pow(ZoomPerStep, steps), MinZoom, 1f);
                if (_zoom < old && InputHelper.PointerOnGround(Cam, out var p, _focus.y))
                {
                    var focusNow = _focus + _zoomOffset;
                    _zoomOffset = p + (focusNow - p) * (_zoom / old) - _focus;
                }
                else if (_zoom > old)
                    _zoomOffset *= old < 1f ? (1f - _zoom) / (1f - old) : 0f;
            }
            if (_zoom < 0.999f && _follow == null)
            {
                var move = InputHelper.MoveInput;
                if (move.sqrMagnitude > 0f)
                {
                    var yawRot = Quaternion.Euler(0f, _yaw, 0f);
                    _zoomOffset += yawRot * new Vector3(move.x, 0f, move.y) * (_distance * 0.6f * _zoom * Time.unscaledDeltaTime);
                }
                // Nicht beliebig weit wegschwenken: höchstens so weit, wie die Standardansicht breit ist
                _zoomOffset = Vector3.ClampMagnitude(_zoomOffset, _distance * 0.45f);
            }
            else if (_zoom >= 0.999f) _zoomOffset = Vector3.zero;
        }

        private void Apply(float t)
        {
            if (_follow != null) _focus = _follow.position;
            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            var targetPos = _focus + _zoomOffset - rot * Vector3.forward * (_distance * _zoom);
            transform.position = Vector3.Lerp(transform.position, targetPos, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, t);
        }
    }
}
