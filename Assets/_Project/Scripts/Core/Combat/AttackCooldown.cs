using System;
using UnityEngine;

namespace RPG.Core.Combat
{
    /// <summary>
    /// Reusable attack-rate timer shared by every attacker (player classes, enemies, bosses).
    ///
    /// Attack speed is always expressed as ATTACKS PER SECOND; the cooldown is derived
    /// as 1 / AttacksPerSecond so no system ever stores a duration directly.
    /// Keeping this in one place means Knight, Archer and every enemy use identical timing rules.
    ///
    /// This is a plain [Serializable] class, not a MonoBehaviour, so it can be a field on
    /// anything and still be tuned in the Inspector.
    /// </summary>
    [Serializable]
    public class AttackCooldown
    {
        [Tooltip("Attacks per second. 2 = twice per second, 0.5 = once every two seconds.")]
        [SerializeField, Min(0f)] private float attacksPerSecond = 1f;

        private float _nextReadyTime;

        /// <summary>Attacks per second. Runtime stat systems overwrite this each time stats change.</summary>
        public float AttacksPerSecond
        {
            get => attacksPerSecond;
            set => attacksPerSecond = Mathf.Max(0f, value);
        }

        /// <summary>Seconds between attacks. Infinite when attack speed is zero (cannot attack).</summary>
        public float CooldownSeconds =>
            attacksPerSecond <= 0f ? float.PositiveInfinity : 1f / attacksPerSecond;

        public bool IsReady => Time.time >= _nextReadyTime;

        /// <summary>0 = just attacked, 1 = ready again. Useful for cooldown UI later.</summary>
        public float NormalizedProgress
        {
            get
            {
                float cooldown = CooldownSeconds;
                if (float.IsInfinity(cooldown)) return 0f;
                float remaining = _nextReadyTime - Time.time;
                return remaining <= 0f ? 1f : Mathf.Clamp01(1f - (remaining / cooldown));
            }
        }

        /// <summary>Returns true and starts the cooldown if an attack is allowed right now.</summary>
        public bool TryConsume()
        {
            if (!IsReady) return false;
            _nextReadyTime = Time.time + CooldownSeconds;
            return true;
        }

        /// <summary>Makes the next attack available immediately (respawn, stage restart, debug).</summary>
        public void Reset() => _nextReadyTime = 0f;
    }
}
