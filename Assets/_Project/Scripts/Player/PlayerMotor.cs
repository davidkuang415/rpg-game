using UnityEngine;

namespace RPG.Player
{
    /// <summary>
    /// Turns a normalized input direction into physical 2D movement.
    ///
    /// It deliberately knows nothing about WHERE the input came from and nothing about
    /// WHY the speed is what it is. In Phase 2 the stat system will simply write
    /// MoveSpeed each time stats change; nothing else here has to change.
    ///
    /// Movement runs through Rigidbody2D.MovePosition in FixedUpdate so walls block the
    /// player properly instead of the transform teleporting through colliders.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Tuning")]
        [Tooltip("Fallback speed in world units per second, used until the stat system takes over.")]
        [SerializeField, Min(0f)] private float baseMoveSpeed = 5f;

        [Tooltip("How fast the character reaches full speed, in units/sec^2. 0 = instant, snappier arcade feel.")]
        [SerializeField, Min(0f)] private float acceleration = 60f;

        [Tooltip("How fast the character stops when input is released, in units/sec^2. 0 = instant.")]
        [SerializeField, Min(0f)] private float deceleration = 80f;

        private Rigidbody2D _rigidbody;
        private Vector2 _moveInput;
        private Vector2 _currentVelocity;

        // A dash overrides normal movement for a short burst. Kept here rather than in
        // PlayerDash so there is still exactly one thing that writes the rigidbody's position.
        private Vector2 _dashVelocity;
        private float _dashTimeLeft;

        /// <summary>Current movement speed in units/second. Phase 2 stats will drive this.</summary>
        public float MoveSpeed { get; set; }

        /// <summary>Actual velocity this frame - useful for animation and camera look-ahead later.</summary>
        public Vector2 CurrentVelocity => _currentVelocity;

        public bool IsMoving => _currentVelocity.sqrMagnitude > 0.0001f;

        /// <summary>True while a dash burst is overriding normal movement.</summary>
        public bool IsDashing => _dashTimeLeft > 0f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            MoveSpeed = baseMoveSpeed;

            // Enforce the settings a top-down 2D character needs, so a mis-set Inspector
            // value cannot make the player fall or spin on collision.
            _rigidbody.gravityScale = 0f;
            _rigidbody.freezeRotation = true;
        }

        /// <summary>Feed a direction with magnitude 0..1. Called once per frame by PlayerController.</summary>
        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input.sqrMagnitude > 1f ? input.normalized : input;
        }

        /// <summary>Immediately stops the character (death, cutscene, stage transition).</summary>
        public void Stop()
        {
            _moveInput = Vector2.zero;
            _currentVelocity = Vector2.zero;
            _dashTimeLeft = 0f;
        }

        /// <summary>
        /// Covers <paramref name="distance"/> in <paramref name="duration"/> seconds along a
        /// direction, ignoring move input for that long. Walls still stop it - the burst goes
        /// through MovePosition like everything else, so it cannot tunnel.
        /// </summary>
        public void Dash(Vector2 direction, float distance, float duration)
        {
            if (duration <= 0f || direction.sqrMagnitude <= 0.0001f) return;

            _dashVelocity = direction.normalized * (distance / duration);
            _dashTimeLeft = duration;
        }

        private void FixedUpdate()
        {
            if (_dashTimeLeft > 0f)
            {
                _dashTimeLeft -= Time.fixedDeltaTime;

                // Leaving the dash at dash speed would make the character skid; it hands over
                // to the normal decel curve from the walk speed instead.
                _currentVelocity = _dashVelocity.normalized * Mathf.Min(_dashVelocity.magnitude, MoveSpeed);

                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.MovePosition(_rigidbody.position + _dashVelocity * Time.fixedDeltaTime);
                return;
            }

            Vector2 targetVelocity = _moveInput * MoveSpeed;
            float rate = _moveInput.sqrMagnitude > 0.0001f ? acceleration : deceleration;

            _currentVelocity = rate <= 0f
                ? targetVelocity
                : Vector2.MoveTowards(_currentVelocity, targetVelocity, rate * Time.fixedDeltaTime);

            // The body stays dynamic, so walls and enemies still collide with it, but its
            // velocity is never allowed to persist between steps. MovePosition authors the
            // position outright; anything left in the velocity - a shove from an enemy, residue
            // from the previous move - would otherwise keep pushing the character, because a
            // top-down body has no gravity and no drag to bleed it off. That is what made the
            // character drift with no input at all.
            _rigidbody.linearVelocity = Vector2.zero;

            if (_currentVelocity.sqrMagnitude <= 0.0001f) return;

            _rigidbody.MovePosition(_rigidbody.position + _currentVelocity * Time.fixedDeltaTime);
        }
    }
}
