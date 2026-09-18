using System.Collections.Generic;
using RPG.Stats;

namespace RPG.Enemies
{
    /// <summary>
    /// The stat bonuses an elite carries, fed into EnemyStats through the same
    /// IStatModifierSource seam equipment uses on the player.
    ///
    /// The numbers live on the DifficultyCurveData so every enemy type shares one tuning
    /// knob; this class only turns them into modifiers. Percent modifiers on the Temporary
    /// layer, so they multiply the level-scaled base rather than adding a flat amount that a
    /// level 30 grunt would not notice.
    /// </summary>
    public sealed class EliteModifierSource : IStatModifierSource
    {
        private readonly DifficultyCurveData _curve;

        public EliteModifierSource(DifficultyCurveData curve) => _curve = curve;

        public void CollectModifiers(List<StatModifier> results)
        {
            if (_curve == null) return;

            Add(results, StatType.MaxHealth, _curve.EliteHealthBonus);
            Add(results, StatType.Attack, _curve.EliteAttackBonus);
            Add(results, StatType.Defense, _curve.EliteDefenseBonus);
            Add(results, StatType.AttackSpeed, _curve.EliteAttackSpeedBonus);
        }

        private static void Add(List<StatModifier> results, StatType stat, float fraction)
        {
            if (fraction > 0f) results.Add(StatModifier.Percent(stat, fraction, StatLayer.Temporary));
        }
    }
}
