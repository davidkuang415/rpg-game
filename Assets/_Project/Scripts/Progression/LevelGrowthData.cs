using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Stats;

namespace RPG.Progression
{
    /// <summary>
    /// What a character level is worth, in stats.
    ///
    /// The design spec is explicit that levelling should slightly raise Max HP and base ATK,
    /// and should NOT inflate every stat every level - the rest of a character's power is
    /// meant to come from equipment. That policy lives here as data, so it can be retuned
    /// without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelGrowth", menuName = "RPG/Progression/Level Growth")]
    public class LevelGrowthData : ScriptableObject
    {
        [Serializable]
        public struct StatGrowth
        {
            public StatType Stat;
            public StatModifierType Type;

            [Tooltip("Amount granted per level beyond level 1.")]
            public float PerLevel;
        }

        [SerializeField]
        private List<StatGrowth> growths = new List<StatGrowth>();

        /// <summary>
        /// Adds this level's cumulative growth to the modifier list.
        /// Level 1 grants nothing; the class's base stats already are level 1.
        /// </summary>
        public void Collect(int level, List<StatModifier> results)
        {
            int levelsGained = Mathf.Max(0, level - 1);
            if (levelsGained == 0) return;

            for (int i = 0; i < growths.Count; i++)
            {
                StatGrowth growth = growths[i];
                if (growth.PerLevel == 0f) continue;

                results.Add(new StatModifier(growth.Stat, growth.Type,
                    growth.PerLevel * levelsGained, StatLayer.LevelGrowth));
            }
        }

        /// <summary>Project defaults, used by the setup tool. Safe to retune afterwards.</summary>
        public void ResetToDefaults()
        {
            growths = new List<StatGrowth>
            {
                new StatGrowth { Stat = StatType.MaxHealth, Type = StatModifierType.Flat, PerLevel = 8f },
                new StatGrowth { Stat = StatType.Attack, Type = StatModifierType.Flat, PerLevel = 1.5f }
            };
        }
    }
}
