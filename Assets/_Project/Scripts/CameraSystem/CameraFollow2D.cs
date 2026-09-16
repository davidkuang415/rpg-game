using UnityEngine;

namespace RPG.CameraSystem
{
    /// <summary>
    /// Smooth top-down follow camera.
    ///
    /// Visibility is expressed as "how many world units tall is the visible area"
    /// (visibleWorldHeight) rather than a raw orthographic size, so the design rule
    /// "the arena is ~50 units but the player sees ~10 around themselves" is a single
    /// configurable number instead of a hardcoded camera value.
    ///
    /// Optional arena bounds stop the camera from showing empty space outside the walls.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Tooltip("If no target is assigned, find the object tagged 'Player' once at Start. " +
                 "Never searched again during gameplay.")]
        [SerializeField] private bool findPlayerByTagOnStart = true;

        [SerializeField] private Vector2 followOffset = Vector2.zero;

        [Header("Visibility")]
        [Tooltip("How tall the visible area is in world units. 10 = the player sees about " +
                 "5 units above and 5 below. Horizontal extent follows the device aspect ratio.")]
        [SerializeField, Min(1f)] private float visibleWorldHeight = 12f;

        [Tooltip("Let this component drive the camera's orthographic size from Visible World Height.")]
        [SerializeField] private bool controlOrthographicSize = true;

        [Header("Smoothing")]
        [Tooltip("Roughly how long the camera takes to catch up. 0 = rigid lock.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        [Tooltip("Snap instantly instead of easing when the target is farther away than this " +
                 "(teleports, respawns, room transitions). 0 disables snapping.")]
        [SerializeField, Min(0f)] private float snapDistance = 15f;

        [Header("Arena Bounds (optional)")]
        [Tooltip("Clamp the camera so it never shows past the arena edges.")]
        [SerializeField] private bool useBounds;
        [SerializeField] private Vector2 boundsCenter = Vector2.zero;
        [SerializeField] private Vector2 boundsSize = new Vector2(50f, 50f);

        private Camera _camera;
        private Vector3 _velocity;
        private float _fixedZ;

        public Transform Target => target;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _fixedZ = transform.position.z;   // Keep the 2D camera in front of the scene.

            if (!_camera.orthographic)
            {
                Debug.LogWarning($"{nameof(CameraFollow2D)}: camera is not orthographic. " +
                                 "A 2D top-down game should use an orthographic camera.", this);
            }
        }

        private void Start()
        {
            if (target == null && findPlayerByTagOnStart)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            ApplyOrthographicSize();

            if (target != null)
            {
                transform.position = ClampToBounds(DesiredPosition());
            }
        }

        // LateUpdate: the player has already moved this frame, so the camera never lags a frame behind.
        private void LateUpdate()
        {
            if (target == null) return;

            ApplyOrthographicSize();

            Vector3 desired = ClampToBounds(DesiredPosition());
            Vector3 current = transform.position;

            bool shouldSnap = smoothTime <= 0f ||
                              (snapDistance > 0f &&
                               ((Vector2)(desired - current)).sqrMagnitude > snapDistance * snapDistance);

            transform.position = shouldSnap
                ? desired
                : Vector3.SmoothDamp(current, desired, ref _velocity, smoothTime);
        }

        /// <summary>Retargets the camera, e.g. after respawning the player.</summary>
        public void SetTarget(Transform newTarget, bool snap = true)
        {
            target = newTarget;
            if (snap && target != null)
            {
                _velocity = Vector3.zero;
                transform.position = ClampToBounds(DesiredPosition());
            }
        }

        /// <summary>
        /// Sets the arena clamp. Called when a stage loads, so each stage carries its own
        /// bounds instead of the camera hardcoding one arena size.
        /// </summary>
        public void SetBounds(Vector2 center, Vector2 size)
        {
            boundsCenter = center;
            boundsSize = size;
            useBounds = size.x > 0f && size.y > 0f;
        }

        /// <summary>Runtime hook for future effects (zoom out for bosses, zoom in for cutscenes).</summary>
        public void SetVisibleWorldHeight(float height)
        {
            visibleWorldHeight = Mathf.Max(1f, height);
            ApplyOrthographicSize();
        }

        private Vector3 DesiredPosition()
        {
            Vector3 p = target.position;
            return new Vector3(p.x + followOffset.x, p.y + followOffset.y, _fixedZ);
        }

        private void ApplyOrthographicSize()
        {
            if (!controlOrthographicSize || !_camera.orthographic) return;
            // Orthographic size is HALF the visible height.
            _camera.orthographicSize = visibleWorldHeight * 0.5f;
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            if (!useBounds || !_camera.orthographic) return position;

            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            float minX = boundsCenter.x - boundsSize.x * 0.5f + halfWidth;
            float maxX = boundsCenter.x + boundsSize.x * 0.5f - halfWidth;
            float minY = boundsCenter.y - boundsSize.y * 0.5f + halfHeight;
            float maxY = boundsCenter.y + boundsSize.y * 0.5f - halfHeight;

            // If the arena is narrower than the view, centre on it instead of fighting the clamp.
            position.x = minX > maxX ? boundsCenter.x : Mathf.Clamp(position.x, minX, maxX);
            position.y = minY > maxY ? boundsCenter.y : Mathf.Clamp(position.y, minY, maxY);
            return position;
        }

        private void OnDrawGizmosSelected()
        {
            if (!useBounds) return;
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }
    }
}
