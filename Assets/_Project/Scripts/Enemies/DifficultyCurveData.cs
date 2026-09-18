using UnityEngine;

namespace RPG.Enemies
{
    /// <summary>
    /// Turns an enemy's level into a stat multiplier.
    ///
    /// Two design rules are enforced here:
    ///  - Enemy Level is a separate concept from Stage Number. A stage decides what level to
    ///    spawn its enemies at; this asset decides what a level is worth. They often move
    ///    together but are never the same variable.
    ///  - Difficulty must not come from HP alone. Each stat has its own growth rate, so
    ///    health can be tuned to grow slower than damage and avoid damage-sponge enemies.
    ///
    /// Growth is compounding: value * (1 + rate)^(level - 1).
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyCurve", menuName = "RPG/Enemies/Difficulty Curve")]
    public class DifficultyCurveData : ScriptableObject
    {
        [Header("Per-level growth rates (0.12 = +12% compounding per level)")]
        [SerializeField, Range(0f, 1f)] private float healthGrowth = 0.11f;
        [SerializeField, Range(0f, 1f)] private float attackGrowth = 0.08f;
        [SerializeField, Range(0f, 1f)] private float defenseGrowth = 0.07f;
        [SerializeField, Range(0f, 1f)] private float xpGrowth = 0.10f;
        [SerializeField, Range(0f, 1f)] private float goldGrowth = 0.08f;

        [Header("Optional shaping")]
        [Tooltip("Extra multiplier applied on top of the growth formula, sampled at " +
                 "(level / Max Shaping Level). Leave flat at 1 for pure exponential growth.")]
        [SerializeField] private AnimationCurve extraMultiplier = AnimationCurve.Constant(0f, 1f, 1f);

        [SerializeField, Min(1)] private int maxShapingLevel = 100;

        [Header("Elites (applied on top of level scaling)")]
        [Tooltip("Extra max HP as a fraction. 0.8 = +80%.")]
        [SerializeField, Range(0f, 5f)] private float eliteHealthBonus = 0.8f;

        [Tooltip("Extra attack as a fraction.")]
        [SerializeField, Range(0f, 5f)] private float eliteAttackBonus = 0.35f;

        [Tooltip("Extra defense as a fraction.")]
        [SerializeField, Range(0f, 5f)] private float eliteDefenseBonus = 0.25f;

        [Tooltip("Extra attack speed as a fraction. Kept small: faster swings shrink the " +
                 "reaction window the telegraph exists to give.")]
        [SerializeField, Range(0f, 2f)] private float eliteAttackSpeedBonus = 0.15f;

        [Tooltip("XP and gold multiplier for an elite kill. Higher than its stat bonus, so an " +
                 "elite is worth going for rather than something to walk around.")]
        [SerializeField, Min(1f)] private float eliteRewardMultiplier = 2.5f;

        [Tooltip("How much bigger an elite is drawn (and collides) than its normal version.")]
        [SerializeField, Range(1f, 2f)] private float eliteScale = 1.22f;

        public float EliteHealthBonus => eliteHealthBonus;
        public float EliteAttackBonus => eliteAttackBonus;
        public float EliteDefenseBonus => eliteDefenseBonus;
        public float EliteAttackSpeedBonus => eliteAttackSpeedBonus;
        public float EliteRewardMultiplier => eliteRewardMultiplier;
        public float EliteScale => eliteScale;

        public float ScaleHealth(float baseValue, int level) => Scale(baseValue, level, healthGrowth);
        public float ScaleAttack(float baseValue, int level) => Scale(baseValue, level, attackGrowth);
        public float ScaleDefense(float baseValue, int level) => Scale(baseValue, level, defenseGrowth);
        public float ScaleXp(float baseValue, int level) => Scale(baseValue, level, xpGrowth);
        public float ScaleGold(float baseValue, int level) => Scale(baseValue, level, goldGrowth);

        private float Scale(float baseValue, int level, float growthRate)
        {
            int steps = Mathf.Max(0, level - 1);
            float shaping = extraMultiplier.Evaluate(Mathf.Clamp01(level / (float)maxShapingLevel));
            return baseValue * Mathf.Pow(1f + growthRate, steps) * shaping;
        }
    }
}
