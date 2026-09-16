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
