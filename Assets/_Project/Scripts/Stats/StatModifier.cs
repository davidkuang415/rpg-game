using System;
using System.Collections.Generic;

namespace RPG.Stats
{
    /// <summary>
    /// The order stat sources are applied in. This IS the calculation pipeline from the design
    /// spec, expressed as data:
    ///
    ///   Base Class Stats -> Level Growth -> Equipment Base -> Enchantment % -> Temporary
    ///
    /// Lower values apply first. Gaps are left between the numbers so a new layer can be
    /// inserted later without renumbering the ones that already exist.
    /// </summary>
    public enum StatLayer
    {
        LevelGrowth = 100,
        Equipment = 200,
        Enchantment = 300,
        Temporary = 400
    }

    public enum StatModifierType
    {
        /// <summary>Added to the running value: +15 ATK.</summary>
        Flat,

        /// <summary>Fraction of the running value: 0.10 = +10%. Summed within a layer, then applied once.</summary>
        PercentAdd
    }

    /// <summary>A single stat change from a single source.</summary>
    [Serializable]
    public struct StatModifier
    {
        public StatType Stat;
        public StatModifierType Type;
        public float Value;
        public StatLayer Layer;

        public StatModifier(StatType stat, StatModifierType type, float value, StatLayer layer)
        {
            Stat = stat;
            Type = type;
            Value = value;
            Layer = layer;
        }

        public static StatModifier Flat(StatType stat, float value, StatLayer layer)
            => new StatModifier(stat, StatModifierType.Flat, value, layer);

        public static StatModifier Percent(StatType stat, float fraction, StatLayer layer)
            => new StatModifier(stat, StatModifierType.PercentAdd, fraction, layer);
    }

    /// <summary>
    /// Anything that contributes stats: the equipment manager, an active buff, a level curve.
    ///
    /// Sources never write to final stats themselves - they only describe their contribution
    /// and let the central calculator combine them. That is what keeps the "one stat pipeline"
    /// rule enforceable instead of aspirational.
    /// </summary>
    public interface IStatModifierSource
    {
        void CollectModifiers(List<StatModifier> results);
    }
}
