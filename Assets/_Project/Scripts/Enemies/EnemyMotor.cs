using UnityEngine;
using RPG.Stats;

namespace RPG.Enemies
{
    /// <summary>
    /// Moves an enemy toward a destination using physics, so walls stop it the same way they
    /// stop the player.
    ///
    /// Steering is deliberately simple - move toward the target point - because pathfinding is
    /// not part of the MVP. Handcrafted arenas are designed so direct approach works; if that
    /// stops being true, this is the one component that would gain a navigation query.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMotor : MonoBehaviour
    {
        [Tooltip("How close counts as arrived, to stop jittering on the spot.")]
        [SerializeField, Min(0.01f)] private float arriveThreshold = 0.15f;

        [Tooltip("Acceleration in units/sec^2. 0 = instant.")]
        [SerializeField, Min(0f)] private float acceleration = 24f;

        private Rigidbody2D _rigidbody;
        private EnemyStats _stats;
        private Vector2 _currentVelocity;
        private Vector2? _destination;

        public bool HasDestination => _destination.HasValue;
        public Vector2 CurrentVelocity => _currentVelocity;

        /// <summary>Speed comes from the stat pipeline, exactly like the player's.</summary>
        public float MoveSpeed => _stats != null ? _stats.GetStat(StatType.MoveSpeed) : 0f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _stats = GetComponent<EnemyStats>();

            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public void MoveTo(Vector2 worldPosition) => _destination = worldPosition;

        public void Stop()
        {
            _destination = null;
            _currentVelocity = Vector2.zero;
        }

        public bool HasArrived(Vector2 position) =>
            ((Vector2)transform.position - position).sqrMagnitude <= arriveThreshold * arriveThreshold;

        private void FixedUpdate()
        {
            Vector2 targetVelocity = Vector2.zero;

            if (_destination.HasValue && !HasArrived(_destination.Value))
            {
                Vector2 toTarget = _destination.Value - (Vector2)transform.position;
                targetVelocity = toTarget.normalized * MoveSpeed;
            }

            _currentVelocity = acceleration <= 0f
                ? targetVelocity
                : Vector2.MoveTowards(_currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

            // Same rule as PlayerMotor: MovePosition authors the position, so no velocity may
            // survive the step. Without this, an enemy shoved by another enemy keeps sliding,
            // since a top-down body has neither gravity nor drag to bleed the push off.
            _rigidbody.linearVelocity = Vector2.zero;

            if (_currentVelocity.sqrMagnitude <= 0.0001f) return;

            _rigidbody.MovePosition(_rigidbody.position + _currentVelocity * Time.fixedDeltaTime);
        }
    }
}
