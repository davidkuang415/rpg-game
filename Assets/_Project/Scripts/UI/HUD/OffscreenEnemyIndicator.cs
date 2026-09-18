using System.Collections.Generic;
using UnityEngine;
using RPG.Core.Combat;

namespace RPG.UI.HUD
{
    /// <summary>
    /// An arrow pinned to the screen edge, pointing at each living enemy the camera cannot see.
    ///
    /// Enemies are found the same way the player's own aim-assist finds them (CombatQueries,
    /// a physics overlap on the Enemy layer) rather than through a registry, so nothing else in
    /// the game has to remember to register or unregister an enemy with this system.
    ///
    /// The search is on an interval, not every frame, for the same reason PlayerTargeting's is:
    /// a room full of enemies is a room full of overlap queries otherwise, and an indicator that
    /// updates ten times a second reads as continuous.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class OffscreenEnemyIndicator : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask enemyLayers;

        [Tooltip("How far from the camera to look for enemies. Comfortably larger than any " +
                 "stage, so nothing at the far end of a big room is missed.")]
        [SerializeField, Min(1f)] private float searchRadius = 60f;

        [SerializeField, Min(0.02f)] private float refreshInterval = 0.15f;

        [Header("Arrow")]
        [Tooltip("Inactive child used as the stamp for every arrow. Must point RIGHT (0,0,0) " +
                 "in its resting orientation - rotation is computed from that.")]
        [SerializeField] private RectTransform arrowTemplate;

        [Tooltip("Canvas units kept clear between the arrow and the screen edge.")]
        [SerializeField] private float edgePadding = 90f;

        [Tooltip("Viewport fraction inset that counts as \"still on screen\". A little larger " +
                 "than 0 so an enemy exactly at the edge doesn't flicker the arrow on and off.")]
        [SerializeField, Range(0f, 0.2f)] private float onscreenMargin = 0.03f;

        private RectTransform _root;
        private readonly Dictionary<Transform, RectTransform> _arrows = new Dictionary<Transform, RectTransform>();
        private readonly List<Transform> _toRemove = new List<Transform>();
        private float _nextRefreshTime;

        private void Awake()
        {
            _root = (RectTransform)transform;
            if (targetCamera == null) targetCamera = Camera.main;
            if (arrowTemplate != null) arrowTemplate.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Time.time < _nextRefreshTime) return;
            _nextRefreshTime = Time.time + refreshInterval;

            Refresh();
        }

        private void Refresh()
        {
            if (targetCamera == null || arrowTemplate == null) return;

            List<Collider2D> candidates = CombatQueries.OverlapCircle(
                targetCamera.transform.position, searchRadius, enemyLayers);

            _toRemove.Clear();
            foreach (Transform tracked in _arrows.Keys) _toRemove.Add(tracked);

            for (int i = 0; i < candidates.Count; i++)
            {
                Collider2D candidate = candidates[i];
                if (candidate == null) continue;

                var damageable = candidate.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                Transform enemy = damageable.Transform;
                _toRemove.Remove(enemy);

                if (IsOnScreen(enemy.position))
                {
                    RemoveArrow(enemy);
                    continue;
                }

                if (!_arrows.TryGetValue(enemy, out RectTransform arrow))
                {
                    arrow = Instantiate(arrowTemplate, _root);
                    arrow.gameObject.SetActive(true);
                    _arrows[enemy] = arrow;
                }

                PlaceArrow(arrow, enemy.position);
            }

            // Whatever is still marked for removal died, was pooled away, or left the search
            // radius since the last refresh.
            for (int i = 0; i < _toRemove.Count; i++) RemoveArrow(_toRemove[i]);
        }

        private bool IsOnScreen(Vector3 worldPosition)
        {
            Vector3 viewport = targetCamera.WorldToViewportPoint(worldPosition);
            return viewport.z > 0f &&
                   viewport.x > onscreenMargin && viewport.x < 1f - onscreenMargin &&
                   viewport.y > onscreenMargin && viewport.y < 1f - onscreenMargin;
        }

        /// <summary>
        /// Direction is measured from the camera in WORLD space, not projected through the
        /// viewport - a top-down orthographic camera never rolls, so world +X/+Y already line up
        /// with screen right/up, and this sidesteps the singularities WorldToScreenPoint has
        /// when something is behind the camera.
        /// </summary>
        private void PlaceArrow(RectTransform arrow, Vector3 worldPosition)
        {
            Vector3 toTarget = worldPosition - targetCamera.transform.position;
            Vector2 direction = new Vector2(toTarget.x, toTarget.y);
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.up;
            direction.Normalize();

            Rect bounds = _root.rect;
            float halfWidth = Mathf.Max(1f, bounds.width * 0.5f - edgePadding);
            float halfHeight = Mathf.Max(1f, bounds.height * 0.5f - edgePadding);

            // Scales the direction out until it just touches the padded screen rectangle -
            // the same trick a sundial's edge would use, whichever axis is more extreme wins.
            float scale = Mathf.Min(
                direction.x != 0f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue,
                direction.y != 0f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue);

            arrow.anchoredPosition = direction * scale;
            arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void RemoveArrow(Transform enemy)
        {
            if (!_arrows.TryGetValue(enemy, out RectTransform arrow)) return;
            _arrows.Remove(enemy);
            if (arrow != null) Destroy(arrow.gameObject);
        }

        private void OnDisable()
        {
            foreach (RectTransform arrow in _arrows.Values)
            {
                if (arrow != null) Destroy(arrow.gameObject);
            }
            _arrows.Clear();
        }
    }
}
