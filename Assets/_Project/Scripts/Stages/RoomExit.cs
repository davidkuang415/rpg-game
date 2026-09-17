using System;
using UnityEngine;

namespace RPG.Stages
{
    /// <summary>
    /// A door between two rooms.
    ///
    /// Closed while its room is being fought, unlocked when the room clears. Walking through
    /// an open door activates the room on the other side - which is how a stage streams its
    /// fights instead of spawning everything at once.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RoomExit : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("The room activated when the player walks through this door.")]
        [SerializeField] private RoomController roomToActivate;

        [Tooltip("Solid collider that physically blocks the doorway while locked.")]
        [SerializeField] private Collider2D blocker;

        [Tooltip("Optional visual shown only while locked.")]
        [SerializeField] private GameObject lockedVisual;

        [Tooltip("Optional visual shown only once unlocked.")]
        [SerializeField] private GameObject openVisual;

        [Header("Detection")]
        [Tooltip("Layers that count as 'the player walked through'. Normally just Player.")]
        [SerializeField] private LayerMask playerLayers;

        [Header("Animation")]
        [Tooltip("Seconds the locked visual takes to dissolve when the door opens. 0 = instant.")]
        [SerializeField, Min(0f)] private float openDuration = 0.35f;

        public bool IsOpen { get; private set; }
        public RoomController RoomToActivate => roomToActivate;

        /// <summary>Raised when the player passes through. StageController listens.</summary>
        public event Action<RoomExit> Entered;

        private Coroutine _openRoutine;

        private void Reset()
        {
            // Make the trigger behave sensibly the moment the component is added in the Editor.
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.isTrigger = true;
        }

        private void Awake() => Close();

        public void Open()
        {
            IsOpen = true;

            // The barrier drops at once - the room is cleared, the player may leave now. Only
            // the picture of it lingers for a moment, so the door visibly gives way.
            if (blocker != null) blocker.enabled = false;
            if (openVisual != null) openVisual.SetActive(true);

            if (lockedVisual == null) return;

            if (_openRoutine != null) StopCoroutine(_openRoutine);

            if (openDuration <= 0f || !isActiveAndEnabled) lockedVisual.SetActive(false);
            else _openRoutine = StartCoroutine(DissolveLockedVisual());
        }

        public void Close()
        {
            IsOpen = false;

            if (_openRoutine != null)
            {
                StopCoroutine(_openRoutine);
                _openRoutine = null;
            }

            if (blocker != null) blocker.enabled = true;
            if (lockedVisual != null)
            {
                lockedVisual.SetActive(true);
                RestoreLockedVisual();
            }
            if (openVisual != null) openVisual.SetActive(false);
        }

        // The locked visual's starting look, captured the first time it dissolves so that
        // Close() can put it back exactly.
        private bool _capturedLockedLook;
        private Vector3 _lockedScale;
        private Color _lockedColor;

        private System.Collections.IEnumerator DissolveLockedVisual()
        {
            var renderer = lockedVisual.GetComponent<SpriteRenderer>();
            Transform t = lockedVisual.transform;

            if (!_capturedLockedLook)
            {
                _capturedLockedLook = true;
                _lockedScale = t.localScale;
                _lockedColor = renderer != null ? renderer.color : Color.white;
            }

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / openDuration);
                float eased = p * p;

                // Shrinks toward the doorway's centre line and fades: the bar slides away.
                t.localScale = new Vector3(_lockedScale.x, _lockedScale.y * (1f - eased), _lockedScale.z);
                if (renderer != null)
                {
                    Color c = _lockedColor;
                    c.a = _lockedColor.a * (1f - eased);
                    renderer.color = c;
                }

                yield return null;
            }

            lockedVisual.SetActive(false);
            RestoreLockedVisual();
            _openRoutine = null;
        }

        private void RestoreLockedVisual()
        {
            if (!_capturedLockedLook || lockedVisual == null) return;

            lockedVisual.transform.localScale = _lockedScale;
            var renderer = lockedVisual.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = _lockedColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsOpen) return;
            if ((playerLayers.value & (1 << other.gameObject.layer)) == 0) return;

            Entered?.Invoke(this);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Application.isPlaying && IsOpen
                ? new Color(0.3f, 1f, 0.4f, 0.8f)
                : new Color(1f, 0.75f, 0.2f, 0.8f);

            Gizmos.DrawWireCube(transform.position, transform.lossyScale);

            if (roomToActivate == null) return;

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            Gizmos.DrawLine(transform.position, roomToActivate.transform.position);
        }
    }
}
