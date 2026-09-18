using UnityEngine;

namespace RPG.Player.Input
{
    /// <summary>
    /// A ScriptableObject that acts as the wire between INPUT DEVICES (the on-screen
    /// joystick, the attack button, a keyboard fallback in the Editor) and the PLAYER.
    ///
    /// Why a ScriptableObject instead of the player finding the UI in the scene?
    ///  - The Player prefab cannot hold a reference to a scene object, and the UI cannot
    ///    hold a reference to a prefab instance that doesn't exist yet.
    ///  - Both sides just reference this shared asset, so neither knows the other exists.
    ///  - Swapping the joystick for a gamepad later means writing a new writer script only.
    ///
    /// This asset holds RUNTIME state, which is fine for transient input, but it means the
    /// values survive exiting Play Mode in the Editor - hence the reset in OnEnable/OnDisable.
    /// Never store persistent player/save data this way.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerInputChannel", menuName = "RPG/Input/Player Input Channel")]
    public class PlayerInputChannel : ScriptableObject
    {
        [Tooltip("How long an attack press stays queued if the attack is still on cooldown. " +
                 "Prevents a press being swallowed when the player taps slightly too early.")]
        [SerializeField, Min(0f)] private float attackPressBufferSeconds = 0.2f;

        [Tooltip("How long a dash press stays queued while the dash is still on cooldown.")]
        [SerializeField, Min(0f)] private float dashPressBufferSeconds = 0.15f;

        private Vector2 _move;
        private bool _attackHeld;
        private float _attackPressTime = float.NegativeInfinity;
        private float _dashPressTime = float.NegativeInfinity;

        /// <summary>Movement input, magnitude 0..1. Direction only - speed comes from stats.</summary>
        public Vector2 Move => _move;

        /// <summary>True while the attack button is held down. The MVP does not auto-attack.</summary>
        public bool AttackHeld => _attackHeld;

        // ---- Called by input writers (joystick, buttons, keyboard) ----

        public void SetMove(Vector2 value)
        {
            _move = value.sqrMagnitude > 1f ? value.normalized : value;
        }

        public void PressAttack()
        {
            _attackHeld = true;
            _attackPressTime = Time.time;
        }

        public void ReleaseAttack() => _attackHeld = false;

        /// <summary>A dash is a tap, never a hold: there is no "dash held" state to release.</summary>
        public void PressDash() => _dashPressTime = Time.time;

        // ---- Called by the player ----

        /// <summary>
        /// True while a recent press is still inside the buffer window. Checking does NOT clear
        /// it - that is the whole point. The old code consumed the press before anything asked
        /// whether the attack could actually fire, so a press arriving during cooldown was
        /// eaten and the buffer never did its job once.
        /// </summary>
        public bool HasBufferedPress => Time.time - _attackPressTime <= attackPressBufferSeconds;

        /// <summary>
        /// Returns true once per attack press (inside the buffer window) and clears it.
        /// Consuming rather than polling a flag means a press is never dropped and never
        /// counted twice, regardless of script execution order.
        /// </summary>
        public bool ConsumeAttackPress()
        {
            if (Time.time - _attackPressTime > attackPressBufferSeconds) return false;
            _attackPressTime = float.NegativeInfinity;
            return true;
        }

        /// <summary>True while a dash press is inside its buffer window. Does not clear it.</summary>
        public bool HasBufferedDash => Time.time - _dashPressTime <= dashPressBufferSeconds;

        /// <summary>Returns true once per dash press inside the buffer window and clears it.</summary>
        public bool ConsumeDashPress()
        {
            if (Time.time - _dashPressTime > dashPressBufferSeconds) return false;
            _dashPressTime = float.NegativeInfinity;
            return true;
        }

        /// <summary>Clears all input. Called on scene changes, death, or when a writer is disabled.</summary>
        public void ResetInput()
        {
            _move = Vector2.zero;
            _attackHeld = false;
            _attackPressTime = float.NegativeInfinity;
            _dashPressTime = float.NegativeInfinity;
        }

        private void OnEnable() => ResetInput();
        private void OnDisable() => ResetInput();
    }
}
