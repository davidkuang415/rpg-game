using UnityEngine;
using RPG.Player.Combat;
using RPG.Player.Input;

namespace RPG.Player
{
    /// <summary>
    /// The thin glue layer of the player: read input once per frame, hand it to the
    /// components that own each behaviour, and forward attack presses.
    ///
    /// Deliberately small. Movement physics live in PlayerMotor, aim state in PlayerFacing,
    /// and combat in an IPlayerAttack component. Stats, health and inventory arrive as
    /// their own components in later phases - this script should stay roughly this size.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerFacing))]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Shared input asset written by the on-screen joystick and buttons.")]
        [SerializeField] private PlayerInputChannel inputChannel;

        [Tooltip("Blocks movement and attacks (death, stage transitions, menus).")]
        [SerializeField] private bool inputEnabled = true;

        [Header("Attacking")]
        [Tooltip("Keep attacking while the button is held, at the rate Attack Speed allows. " +
                 "Off means one tap is one swing, which makes every point of Attack Speed on " +
                 "every item unspendable.")]
        [SerializeField] private bool autoRepeatWhileHeld = true;

        [Tooltip("Optional. Aims at the nearest enemy instead of the way the player is walking, " +
                 "so you can retreat from something while still hitting it.")]
        [SerializeField] private PlayerTargeting targeting;

        [Tooltip("Turn the body to face what is being attacked. Off keeps the body facing the " +
                 "movement direction while the weapon tracks the target.")]
        [SerializeField] private bool faceAttackTarget = true;

        private PlayerMotor _motor;
        private PlayerFacing _facing;
        private IPlayerAttack _attack;

        public PlayerMotor Motor => _motor;
        public PlayerFacing Facing => _facing;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _facing = GetComponent<PlayerFacing>();

            // The class-specific attack is a sibling component, so swapping Knight for
            // Archer later means swapping that component, not editing this script.
            _attack = GetComponent<IPlayerAttack>();
            if (targeting == null) targeting = GetComponent<PlayerTargeting>();

            if (inputChannel == null)
            {
                Debug.LogError($"{nameof(PlayerController)} on '{name}' has no Input Channel assigned. " +
                               "Assign the PlayerInputChannel asset in the Inspector.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!inputEnabled) return;

            Vector2 move = inputChannel.Move;

            _motor.SetMoveInput(move);
            _facing.SetFromInput(move);   // Neutral input keeps the previous facing.

            UpdateAttack();
        }

        /// <summary>
        /// Attacks while the button is held, as fast as Attack Speed allows.
        ///
        /// The order here is load-bearing. The press is checked but NOT consumed until an attack
        /// actually fires: consuming first - which is what this used to do - threw the press away
        /// whenever the weapon was still on cooldown, so the input buffer never once did the job
        /// it exists for.
        /// </summary>
        private void UpdateAttack()
        {
            if (_attack == null) return;

            bool wantsToAttack = inputChannel.HasBufferedPress ||
                                 (autoRepeatWhileHeld && inputChannel.AttackHeld);

            if (!wantsToAttack || !_attack.CanAttack) return;

            Vector2 aim = ResolveAimDirection();

            if (!_attack.TryAttack(aim)) return;

            inputChannel.ConsumeAttackPress();

            // Facing follows the shot rather than the walk, so retreating while firing does not
            // leave the character moonwalking away from its own attacks.
            if (faceAttackTarget) _facing.SetFacing(aim);
        }

        private Vector2 ResolveAimDirection()
        {
            Vector2 fallback = _facing.Facing;
            return targeting != null ? targeting.GetAimDirection(fallback) : fallback;
        }

        /// <summary>Enables/disables player control without disabling the GameObject.</summary>
        public void SetInputEnabled(bool value)
        {
            inputEnabled = value;
            if (!value)
            {
                _motor.Stop();
                inputChannel.ResetInput();
            }
        }
    }
}
