using UnityEngine;
using RPG.Core.Combat;
using RPG.Core.Events;

namespace RPG.Enemies
{
    /// <summary>
    /// What this enemy can currently see, and what it remembers.
    ///
    /// Design rules implemented here:
    ///  - Walls block sight. An enemy does not get free knowledge of the player's position
    ///    through level geometry.
    ///  - After losing sight, the enemy remembers the player's LAST KNOWN position for a
    ///    limited time and investigates it, rather than instantly forgetting or magically
    ///    tracking.
    ///
    /// Sight checks are throttled and randomly offset per instance, so twenty enemies do not
    /// all raycast on the same frame. That matters on mobile.
    /// </summary>
    public class EnemyPerception : MonoBehaviour
    {
        [SerializeField] private PlayerReference playerReference;

        [Tooltip("Layers that block sight. Normally just Wall.")]
        [SerializeField] private LayerMask blockingLayers;

        [Tooltip("Seconds between sight checks. Higher is cheaper and slightly less responsive.")]
        [SerializeField, Min(0.02f)] private float checkInterval = 0.15f;

        [Tooltip("Where sight is measured from - usually slightly above the feet. Optional.")]
        [SerializeField] private Transform eyes;

        private EnemyStats _stats;
        private float _nextCheckTime;
        private float _lastSeenTime = float.NegativeInfinity;

        /// <summary>True if the player is in range and in direct line of sight right now.</summary>
        public bool HasVisual { get; private set; }

        /// <summary>Where the player was last actually seen.</summary>
        public Vector2 LastKnownPosition { get; private set; }

        /// <summary>True while the enemy still remembers where the player went.</summary>
        public bool HasMemory
        {
            get
            {
                float memory = _stats != null && _stats.Data != null ? _stats.Data.MemorySeconds : 0f;
                return Time.time - _lastSeenTime <= memory;
            }
        }

        public Vector2 EyePosition => eyes != null ? (Vector2)eyes.position : (Vector2)transform.position;
        public bool PlayerExists => playerReference != null && playerReference.Exists;

        private void Awake()
        {
            _stats = GetComponent<EnemyStats>();

            // Spread the first check out so a wave of enemies spawned on the same frame does
            // not synchronise its raycasts forever after.
            _nextCheckTime = Time.time + Random.Range(0f, checkInterval);
        }

        private void Update()
        {
            if (Time.time < _nextCheckTime) return;
            _nextCheckTime = Time.time + checkInterval;

            HasVisual = CheckVisual();
            if (HasVisual)
            {
                LastKnownPosition = playerReference.Position;
                _lastSeenTime = Time.time;
            }
        }

        private bool CheckVisual()
        {
            if (!PlayerExists || _stats == null || _stats.Data == null) return false;

            Vector2 eyePosition = EyePosition;
            Vector2 playerPosition = playerReference.Position;

            float detectionRange = _stats.Data.DetectionRange;
            if ((playerPosition - eyePosition).sqrMagnitude > detectionRange * detectionRange) return false;

            return CombatQueries.HasLineOfSight(eyePosition, playerPosition, blockingLayers);
        }

        /// <summary>Lets a spawner or a damage reaction make an enemy instantly aware of the player.</summary>
        public void ForceAlert()
        {
            if (!PlayerExists) return;
            LastKnownPosition = playerReference.Position;
            _lastSeenTime = Time.time;
        }

        private void OnDrawGizmosSelected()
        {
            var stats = GetComponent<EnemyStats>();
            if (stats == null || stats.Data == null) return;

            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, stats.Data.DetectionRange);

            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, stats.Data.AttackRange);
        }
    }
}
