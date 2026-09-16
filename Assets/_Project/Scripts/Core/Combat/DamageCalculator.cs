using UnityEngine;
using RPG.Stats;

namespace RPG.Core.Combat
{
    /// <summary>
    /// All combat math, in one place, used by every attacker and defender in the game.
    ///
    /// Splitting it into outgoing (attacker) and incoming (defender) halves is what keeps the
    /// pipeline honest: crit is rolled once by the attacker, mitigation is applied once by the
    /// defender, and neither can accidentally be applied twice.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Rolls the attacker's outgoing damage: base ATK, then a crit roll.
        /// Crit damage is a multiplier (1.5 = 150%), matching the project-wide convention.
        /// </summary>
        public static float RollOutgoingDamage(ICombatStatProvider attacker, float damageMultiplier,
            out bool isCritical)
        {
            float attack = attacker?.GetStat(StatType.Attack) ?? 0f;
            float critChance = attacker?.GetStat(StatType.CritChance) ?? 0f;
            float critDamage = attacker?.GetStat(StatType.CritDamage) ?? 1f;

            float damage = attack * damageMultiplier;

            isCritical = critChance > 0f && Random.value < critChance;
            if (isCritical) damage *= Mathf.Max(1f, critDamage);

            return Mathf.Max(0f, damage);
        }

        /// <summary>
        /// Diminishing-returns mitigation, per the design spec:
        ///
        ///     DamageTaken = RawDamage * 100 / (100 + EffectiveDefense)
        ///
        /// 10 DEF means damage * 100/110. Defense never fully blocks damage and never scales
        /// into immunity, which is why this is used instead of flat subtraction.
        /// </summary>
        public static float ApplyDefense(float rawDamage, float defense, float defensePenetration)
        {
            // Defense penetration reduces EffectiveDefense BEFORE mitigation. Nothing grants
            // this stat yet; the term is here so adding it later needs no formula change.
            float effectiveDefense = Mathf.Max(0f, defense * (1f - Mathf.Clamp01(defensePenetration)));
            return rawDamage * 100f / (100f + effectiveDefense);
        }

        /// <summary>Rolls the defender's dodge. Dodge is capped centrally by StatRules.</summary>
        public static bool RollDodge(ICombatStatProvider defender)
        {
            float dodgeChance = defender?.GetStat(StatType.DodgeChance) ?? 0f;
            return dodgeChance > 0f && Random.value < dodgeChance;
        }

        /// <summary>HP restored from life steal, based on damage ACTUALLY dealt.</summary>
        public static float CalculateLifeSteal(ICombatStatProvider attacker, float damageDealt)
        {
            float lifeSteal = attacker?.GetStat(StatType.LifeSteal) ?? 0f;
            return lifeSteal <= 0f ? 0f : damageDealt * lifeSteal;
        }
    }
}
