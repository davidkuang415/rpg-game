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

        public bool IsOpen { get; private set; }
        public RoomController RoomToActivate => roomToActivate;

        /// <summary>Raised when the player passes through. StageController listens.</summary>
        public event Action<RoomExit> Entered;

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
            if (blocker != null) blocker.enabled = false;
            if (lockedVisual != null) lockedVisual.SetActive(false);
            if (openVisual != null) openVisual.SetActive(true);
        }

        public void Close()
        {
            IsOpen = false;
            if (blocker != null) blocker.enabled = true;
            if (lockedVisual != null) lockedVisual.SetActive(true);
            if (openVisual != null) openVisual.SetActive(false);
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
