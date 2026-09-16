using UnityEngine;

namespace RPG.Player.Combat
{
    /// <summary>
    /// What PlayerController talks to when the attack button is pressed.
    ///
    /// Exactly one component on the player implements this - the PlayerAttackRouter - which
    /// then forwards to whichever weapon behaviour the selected class uses. The controller
    /// therefore never learns that Knights and Archers attack differently.
    /// </summary>
    public interface IPlayerAttack
    {
        /// <summary>False while on cooldown, or when no weapon behaviour is active.</summary>
        bool CanAttack { get; }

        /// <summary>Attempts an attack in the given world direction. Returns true if it happened.</summary>
        bool TryAttack(Vector2 direction);
    }
}
