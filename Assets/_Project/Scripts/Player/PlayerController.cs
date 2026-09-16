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

            if (inputChannel.ConsumeAttackPress())
            {
                _attack?.TryAttack(_facing.Facing);
            }
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
