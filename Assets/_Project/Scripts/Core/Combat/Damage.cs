using UnityEngine;

namespace RPG.Core.Combat
{
    /// <summary>
    /// One incoming hit, fully described. Built by the attacker, consumed by the defender.
    ///
    /// Amount is damage AFTER the attacker's crit roll but BEFORE the defender's defense,
    /// because defense belongs to the defender and must not be applied twice.
    /// </summary>
    public readonly struct DamageInfo
    {
        /// <summary>Post-crit, pre-mitigation damage.</summary>
        public readonly float Amount;
        public readonly bool IsCritical;

        /// <summary>Fraction of the target's DEF ignored, 0..1. Always 0 until DEF Pen ships.</summary>
        public readonly float DefensePenetration;

        /// <summary>The attacker. Null for hazards and other world damage.</summary>
        public readonly GameObject Source;

        public readonly Vector2 HitPoint;
        public readonly Vector2 Direction;

        /// <summary>False for hazards and self-damage, so environmental damage never feeds life steal.</summary>
        public readonly bool CanLifeSteal;

        /// <summary>True for damage that cannot be dodged (currently unused; hooks up cleanly later).</summary>
        public readonly bool IgnoresDodge;

        public DamageInfo(float amount, bool isCritical, GameObject source,
            Vector2 hitPoint, Vector2 direction, float defensePenetration = 0f,
            bool canLifeSteal = true, bool ignoresDodge = false)
        {
            Amount = amount;
            IsCritical = isCritical;
            Source = source;
            HitPoint = hitPoint;
            Direction = direction;
            DefensePenetration = defensePenetration;
            CanLifeSteal = canLifeSteal;
            IgnoresDodge = ignoresDodge;
        }

        /// <summary>
        /// Copies this hit with impact details filled in. Projectiles roll their damage when
        /// fired but only learn where they landed later.
        /// </summary>
        public DamageInfo AtPoint(Vector2 hitPoint, Vector2 direction) => new DamageInfo(
            Amount, IsCritical, Source, hitPoint, direction,
            DefensePenetration, CanLifeSteal, IgnoresDodge);
    }

    /// <summary>What actually happened, reported back to the attacker (life steal needs this).</summary>
    public readonly struct DamageResult
    {
        /// <summary>Damage actually applied after defense, dodge and overkill trimming.</summary>
        public readonly float DamageDealt;
        public readonly bool WasDodged;
        public readonly bool WasFatal;

        public DamageResult(float damageDealt, bool wasDodged, bool wasFatal)
        {
            DamageDealt = damageDealt;
            WasDodged = wasDodged;
            WasFatal = wasFatal;
        }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        Transform Transform { get; }
        DamageResult TakeDamage(in DamageInfo info);
    }

    /// <summary>Implemented by attackers that care what their hit actually did (life steal, on-hit effects).</summary>
    public interface IDamageDealtListener
    {
        void OnDamageDealt(in DamageResult result);
    }
}
