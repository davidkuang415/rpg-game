using System.Collections.Generic;

namespace RPG.Stats
{
    /// <summary>
    /// The single place where final stats are produced. Every character in the game
    /// (player now, enemies and bosses later) runs through this.
    ///
    /// Within each layer: all flat modifiers are added, then all percent modifiers are summed
    /// and applied once. Layers are processed in ascending order, so an equipment flat bonus
    /// is already included before enchantment percentages multiply it - which is exactly the
    /// pipeline the design spec describes.
    ///
    /// Percentages sum within a layer rather than multiplying (+10% and +10% gives +20%, not
    /// +21%). That keeps enchantment values predictable to read and to balance.
    /// </summary>
    public static class StatCalculator
    {
        private static readonly StatLayer[] LayerOrder =
        {
            StatLayer.LevelGrowth,
            StatLayer.Equipment,
            StatLayer.Enchantment,
            StatLayer.Temporary
        };

        // Reused between calls so recalculation allocates nothing.
        private static readonly float[] FlatBuffer = new float[StatTypeInfo.Count];
        private static readonly float[] PercentBuffer = new float[StatTypeInfo.Count];

        /// <summary>
        /// Writes final stats into <paramref name="result"/>.
        /// </summary>
        /// <param name="baseStats">Base class stats. Never modified.</param>
        /// <param name="modifiers">Every contribution from every registered source.</param>
        /// <param name="rules">Optional clamp rules applied at the very end.</param>
        /// <param name="result">Destination block, overwritten.</param>
        public static void Compute(StatBlock baseStats, List<StatModifier> modifiers,
            StatRules rules, StatBlock result)
        {
            result.CopyFrom(baseStats);

            if (modifiers != null && modifiers.Count > 0)
            {
                for (int layerIndex = 0; layerIndex < LayerOrder.Length; layerIndex++)
                {
                    ApplyLayer(LayerOrder[layerIndex], modifiers, result);
                }
            }

            rules?.Apply(result);
        }

        private static void ApplyLayer(StatLayer layer, List<StatModifier> modifiers, StatBlock result)
        {
            bool layerHasAnything = false;

            for (int i = 0; i < StatTypeInfo.Count; i++)
            {
                FlatBuffer[i] = 0f;
                PercentBuffer[i] = 0f;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier modifier = modifiers[i];
                if (modifier.Layer != layer) continue;

                layerHasAnything = true;
                int index = (int)modifier.Stat;

                if (modifier.Type == StatModifierType.Flat) FlatBuffer[index] += modifier.Value;
                else PercentBuffer[index] += modifier.Value;
            }

            if (!layerHasAnything) return;

            for (int i = 0; i < StatTypeInfo.Count; i++)
            {
                var stat = (StatType)i;
                float value = result[stat] + FlatBuffer[i];
                if (PercentBuffer[i] != 0f) value *= 1f + PercentBuffer[i];
                result[stat] = value;
            }
        }
    }
}
