using System;
using UnityEngine;

namespace RPG.Player
{
    /// <summary>
    /// Tracks which way the character is aiming.
    ///
    /// Rule from the design spec: facing follows the most recent NON-ZERO movement input.
    /// When the joystick returns to neutral the character keeps facing that direction, so
    /// the Knight's swing arc and the Archer's arrows stay where the player last pointed.
    ///
    /// Combat reads Facing; it never re-derives direction from input itself.
    /// </summary>
    public class PlayerFacing : MonoBehaviour
    {
        [Header("Visuals")]
        [Tooltip("Optional child transform that is rotated to match the facing direction " +
                 "(the aim indicator, and later the weapon pivot). The body sprite itself " +
                 "usually should NOT be assigned here.")]
        [SerializeField] private Transform aimPivot;

        [Tooltip("Degrees to add when rotating the aim pivot. 0 assumes the pivot's +X (right) " +
                 "axis points forward; use -90 if its art points up.")]
        [SerializeField] private float aimPivotAngleOffset;

        [Header("Behaviour")]
        [SerializeField] private Vector2 initialFacing = Vector2.down;

        [Tooltip("Input shorter than this is treated as neutral and does not change facing.")]
        [SerializeField, Range(0.01f, 0.9f)] private float minInputMagnitude = 0.15f;

        [Tooltip("Degrees per second the aim turns. 0 = instant snap.")]
        [SerializeField, Min(0f)] private float turnSpeedDegreesPerSecond;

        private Vector2 _facing;
        private float _targetAngle;
        private float _currentAngle;

        /// <summary>Normalized facing direction. Never zero.</summary>
        public Vector2 Facing => _facing;

        /// <summary>Facing as an angle in degrees (0 = right, 90 = up).</summary>
        public float FacingAngle => _currentAngle;

        /// <summary>Raised only when the facing direction actually changes.</summary>
        public event Action<Vector2> FacingChanged;

        private void Awake()
        {
            Vector2 start = initialFacing.sqrMagnitude > 0.0001f ? initialFacing.normalized : Vector2.down;
            _facing = start;
            _targetAngle = _currentAngle = Mathf.Atan2(start.y, start.x) * Mathf.Rad2Deg;
            ApplyPivotRotation();
        }

        /// <summary>Called once per frame with the raw move input; ignores neutral input.</summary>
        public void SetFromInput(Vector2 moveInput)
        {
            if (moveInput.magnitude < minInputMagnitude) return;
            SetFacing(moveInput);
        }

        /// <summary>Forces a facing direction (knockback, scripted events, boss stagger).</summary>
        public void SetFacing(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return;

            Vector2 normalized = direction.normalized;
            if (Vector2.Dot(normalized, _facing) > 0.9999f) return;

            _facing = normalized;
            _targetAngle = Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;

            if (turnSpeedDegreesPerSecond <= 0f)
            {
                _currentAngle = _targetAngle;
                ApplyPivotRotation();
            }

            FacingChanged?.Invoke(_facing);
        }

        private void Update()
        {
            if (turnSpeedDegreesPerSecond <= 0f) return;
            if (Mathf.Approximately(_currentAngle, _targetAngle)) return;

            _currentAngle = Mathf.MoveTowardsAngle(
                _currentAngle, _targetAngle, turnSpeedDegreesPerSecond * Time.deltaTime);
            ApplyPivotRotation();
        }

        private void ApplyPivotRotation()
        {
            if (aimPivot == null) return;
            aimPivot.localRotation = Quaternion.Euler(0f, 0f, _currentAngle + aimPivotAngleOffset);
        }
    }
}
