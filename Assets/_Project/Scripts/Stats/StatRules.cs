using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Stats
{
    /// <summary>
    /// Central clamp rules for every stat: the caps and floors the design spec calls for
    /// (crit chance capped at 100%, dodge capped so permanent invulnerability is impossible,
    /// speeds never negative).
    ///
    /// Kept in one ScriptableObject so balance changes never require touching code, and so
    /// no individual system can quietly grant itself an uncapped stat.
    /// </summary>
    [CreateAssetMenu(fileName = "StatRules", menuName = "RPG/Stats/Stat Rules")]
    public class StatRules : ScriptableObject
    {
        [Serializable]
        public struct StatRule
        {
            public StatType Stat;
            public bool HasMin;
            public float Min;
            public bool HasMax;
            public float Max;
        }

        [Tooltip("One entry per stat that needs a floor or a cap. Stats not listed are unclamped.")]
        [SerializeField] private List<StatRule> rules = new List<StatRule>();

        /// <summary>Clamps every stat in the block in place. Called at the end of every recalculation.</summary>
        public void Apply(StatBlock block)
        {
            if (block == null) return;

            for (int i = 0; i < rules.Count; i++)
            {
                StatRule rule = rules[i];
                float value = block[rule.Stat];

                if (rule.HasMin) value = Mathf.Max(value, rule.Min);
                if (rule.HasMax) value = Mathf.Min(value, rule.Max);

                block[rule.Stat] = value;
            }
        }

        /// <summary>Fills in the project's default caps. Used by the setup tool; safe to re-tune by hand.</summary>
        public void ResetToDefaults()
        {
            rules = new List<StatRule>
            {
                Rule(StatType.MaxHealth, min: 1f),
                Rule(StatType.Attack, min: 0f),
                Rule(StatType.Defense, min: 0f),
                Rule(StatType.AttackSpeed, min: 0.05f, max: 10f),
                Rule(StatType.MoveSpeed, min: 0f, max: 20f),
                Rule(StatType.CritChance, min: 0f, max: 1f),      // spec: crit chance caps at 100%
                Rule(StatType.CritDamage, min: 1f),               // never worse than a normal hit
                Rule(StatType.DefensePenetration, min: 0f, max: 1f),
                Rule(StatType.DodgeChance, min: 0f, max: 0.6f),   // spec: dodge must not reach invulnerability
                Rule(StatType.LifeSteal, min: 0f, max: 1f)
            };
        }

        private static StatRule Rule(StatType stat, float? min = null, float? max = null) => new StatRule
        {
            Stat = stat,
            HasMin = min.HasValue,
            Min = min ?? 0f,
            HasMax = max.HasValue,
            Max = max ?? 0f
        };
    }
}
