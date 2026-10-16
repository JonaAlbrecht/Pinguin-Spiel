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
            _follow = target;
            _pitch = pitch;
            _distance = distance;
            _yaw = yaw;
        }

        public void LookAt(Vector3 focus, float pitch, float distance, float yaw = 0f, bool snap = false)
        {
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

        private void LateUpdate() => Apply(1f - Mathf.Exp(-_smooth * Time.deltaTime));

        private void Apply(float t)
        {
            if (_follow != null) _focus = _follow.position;
            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            var targetPos = _focus - rot * Vector3.forward * _distance;
            transform.position = Vector3.Lerp(transform.position, targetPos, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, t);
        }
    }
}
