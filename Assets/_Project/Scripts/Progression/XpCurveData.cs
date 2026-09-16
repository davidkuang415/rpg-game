using UnityEngine;

namespace RPG.Progression
{
    /// <summary>
    /// How much XP each level costs.
    ///
    /// A formula rather than a hand-typed table, per the design spec: requirements grow
    /// compounding per level, with an optional list of overrides for the first few levels
    /// (early levels usually want to be faster than a clean curve would make them).
    /// </summary>
    [CreateAssetMenu(fileName = "XpCurve", menuName = "RPG/Progression/XP Curve")]
    public class XpCurveData : ScriptableObject
    {
        [Header("Bounds")]
        [SerializeField, Min(1)] private int maxLevel = 60;

        [Header("Curve")]
        [Tooltip("XP required to go from level 1 to level 2.")]
        [SerializeField, Min(1f)] private float baseRequirement = 60f;

        [Tooltip("Compounding growth per level. 0.18 = each level costs 18% more than the last.")]
        [SerializeField, Range(0f, 1f)] private float growthPerLevel = 0.18f;

        [Header("Early Game Overrides")]
        [Tooltip("Optional exact requirements for the first levels. Index 0 is level 1 -> 2. " +
                 "Leave empty to use the formula everywhere.")]
        [SerializeField] private float[] earlyLevelOverrides = new float[0];

        public int MaxLevel => maxLevel;

        /// <summary>XP needed to advance FROM the given level. Infinite at max level.</summary>
        public float GetRequirementForLevel(int level)
        {
            if (level >= maxLevel) return float.PositiveInfinity;

            int index = level - 1;
            if (index >= 0 && index < earlyLevelOverrides.Length && earlyLevelOverrides[index] > 0f)
            {
                return earlyLevelOverrides[index];
            }

            return baseRequirement * Mathf.Pow(1f + growthPerLevel, Mathf.Max(0, index));
        }

        /// <summary>
        /// Total XP earned across a whole run to reach a level from level 1.
        /// Used by save migration and by any UI that wants lifetime XP.
        /// </summary>
        public float GetCumulativeXpToReach(int level)
        {
            float total = 0f;
            for (int i = 1; i < Mathf.Min(level, maxLevel); i++) total += GetRequirementForLevel(i);
            return total;
        }
    }
}
