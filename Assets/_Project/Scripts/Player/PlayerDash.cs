using System;
using UnityEngine;
using RPG.Core.Combat;

namespace RPG.Player
{
    /// <summary>
    /// The player's dodge roll: a short burst of speed with invulnerability for most of it.
    ///
    /// Every enemy attack in the game telegraphs itself with a windup, and until now the only
    /// answer to a telegraph was to walk away at walking speed - which against a Brute's reach
    /// or a Slinger's bolt often was not an answer at all. The dash is that answer. It is
    /// deliberately not an attack, not a damage source and not tied to any stat: the whole
    /// point is that it is always the same and always available on the same rhythm, so the
    /// player can learn it.
    ///
    /// Direction comes from the joystick if it is being pushed, otherwise from the facing, so
    /// a neutral-stick dash backs the character out along the line it was already committed to.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerFacing))]
    public class PlayerDash : MonoBehaviour
    {
        [Header("Tuning")]
        [Tooltip("World units covered by one dash.")]
        [SerializeField, Min(0.1f)] private float distance = 3.2f;

        [Tooltip("Seconds the burst lasts. Shorter = snappier.")]
        [SerializeField, Min(0.02f)] private float duration = 0.18f;

        [Tooltip("Seconds of invulnerability, from the moment the dash starts. Slightly longer " +
                 "than the burst so a hit landing on the last frame still misses.")]
        [SerializeField, Min(0f)] private float invulnerableSeconds = 0.24f;

        [Tooltip("Seconds between the end of one dash and the start of the next.")]
        [SerializeField, Min(0f)] private float cooldownSeconds = 0.9f;

        private PlayerMotor _motor;
        private PlayerFacing _facing;
        private Health _health;

        private float _readyAt;
        private float _invulnerableUntil;
        private bool _holdingInvulnerability;

        /// <summary>Raised when a dash starts, with its direction. Animation and audio hook in here.</summary>
        public event Action<Vector2> Dashed;

        public bool IsDashing => _motor != null && _motor.IsDashing;
        public bool IsReady => Time.time >= _readyAt && !IsDashing;

        /// <summary>0 = ready, 1 = just used. For a cooldown ring on the button.</summary>
        public float CooldownFraction
        {
            get
            {
                float total = duration + cooldownSeconds;
                if (total <= 0f) return 0f;
                return Mathf.Clamp01((_readyAt - Time.time) / total);
            }
        }

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _facing = GetComponent<PlayerFacing>();
            _health = GetComponent<Health>();
        }

        private void OnDisable() => ReleaseInvulnerability();

        private void Update()
        {
            if (_holdingInvulnerability && Time.time >= _invulnerableUntil) ReleaseInvulnerability();
        }

        /// <summary>
        /// Starts a dash if one is allowed. <paramref name="moveInput"/> is the raw stick; the
        /// facing is used when it is neutral.
        /// </summary>
        public bool TryDash(Vector2 moveInput)
        {
            if (!isActiveAndEnabled || !IsReady) return false;
            if (_health != null && !_health.IsAlive) return false;

            Vector2 direction = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : _facing.Facing;
            if (direction.sqrMagnitude <= 0.0001f) direction = Vector2.down;

            _motor.Dash(direction, distance, duration);
            _readyAt = Time.time + duration + cooldownSeconds;

            if (_health != null && invulnerableSeconds > 0f)
            {
                _invulnerableUntil = Time.time + invulnerableSeconds;
                if (!_holdingInvulnerability)
                {
                    _health.HoldInvulnerability();
                    _holdingInvulnerability = true;
                }
            }

            Dashed?.Invoke(direction);
            return true;
        }

        private void ReleaseInvulnerability()
        {
            if (!_holdingInvulnerability) return;
            _holdingInvulnerability = false;
            if (_health != null) _health.ReleaseInvulnerability();
        }
    }
}
