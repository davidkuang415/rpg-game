using System;
using UnityEngine;
using RPG.Core.Combat;
using RPG.Core.Events;

namespace RPG.Stages
{
    /// <summary>
    /// Restores a share of the player's health every time a room is cleared.
    ///
    /// Nothing healed between rooms before this, and no stat anyone actually owns grants
    /// life steal, so a multi-room stage was pure attrition: damage taken in room one was
    /// still missing in room three. Clearing a room is the natural breath in a stage, and a
    /// visible refill on it is what makes pushing on feel like a choice rather than a gamble.
    ///
    /// A fraction of MAX health, not a flat amount, so it stays worth the same at level 40 as
    /// at level 1. Deliberately not a full heal: a bad room should still cost something.
    /// </summary>
    public class RoomClearHeal : MonoBehaviour
    {
        [SerializeField] private StageEventChannel stageEvents;
        [SerializeField] private PlayerReference playerReference;

        [Tooltip("Fraction of max health restored per room cleared.")]
        [SerializeField, Range(0f, 1f)] private float healFraction = 0.3f;

        [Tooltip("Skip the heal on the final room - the stage is over and the next one heals on entry anyway.")]
        [SerializeField] private bool skipFinalRoom = true;

        /// <summary>Amount restored by the most recent room clear, for the HUD banner.</summary>
        public float LastHealed { get; private set; }

        /// <summary>
        /// (room, amountHealed) after every room clear, healed or not. The HUD banner listens
        /// here rather than to RoomCleared, because Unity does not order event subscribers and
        /// a banner that read LastHealed off the same event could run before the heal did.
        /// </summary>
        public event Action<RoomController, float> RoomHealed;

        private void OnEnable()
        {
            if (stageEvents != null) stageEvents.RoomCleared += OnRoomCleared;
        }

        private void OnDisable()
        {
            if (stageEvents != null) stageEvents.RoomCleared -= OnRoomCleared;
        }

        private void OnRoomCleared(RoomController room)
        {
            LastHealed = 0f;

            bool skip = skipFinalRoom && room != null && room.IsFinalRoom;
            Health health = playerReference != null ? playerReference.Health : null;

            if (!skip && health != null && health.IsAlive)
            {
                LastHealed = health.Heal(health.MaxHealth * healFraction);
            }

            RoomHealed?.Invoke(room, LastHealed);
        }
    }
}
