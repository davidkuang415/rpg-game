using System;

namespace RPG.Stats
{
    /// <summary>
    /// Every numeric stat in the game. Adding a stat here makes it flow automatically through
    /// the whole pipeline - class data, equipment, enchantments, buffs, UI - because nothing
    /// downstream hardcodes a list of stats.
    ///
    /// Values are stored in CONSISTENT units everywhere (see StatTypeInfo.IsFraction):
    ///   CritChance 0.05 = 5%      CritDamage 1.5 = 150%      LifeSteal 0.05 = 5%
    /// Percent signs exist only in the UI layer, never in the data.
    /// </summary>
    public enum StatType
    {
        MaxHealth = 0,
        Attack = 1,
        Defense = 2,
        AttackSpeed = 3,      // attacks per second
        MoveSpeed = 4,        // world units per second
        CritChance = 5,       // 0..1
        CritDamage = 6,       // multiplier, 1.5 = 150%

        // Declared now so the pipeline, save format and UI already understand them.
        // Not applied by any gameplay system yet - see the spec's "future stat" notes.
        DefensePenetration = 7, // 0..1, reduces effective DEF before the damage formula
        DodgeChance = 8,        // 0..1
        LifeSteal = 9           // 0..1 of damage dealt
    }

    public static class StatTypeInfo
    {
        public static readonly StatType[] All = (StatType[])Enum.GetValues(typeof(StatType));
        public static readonly int Count = All.Length;

        /// <summary>True for stats stored as 0..1 fractions that should display as percentages.</summary>
        public static bool IsFraction(StatType stat) => stat switch
        {
            StatType.CritChance => true,
            StatType.DefensePenetration => true,
            StatType.DodgeChance => true,
            StatType.LifeSteal => true,
            _ => false
        };

        public static string DisplayName(StatType stat) => stat switch
        {
            StatType.MaxHealth => "HP",
            StatType.Attack => "ATK",
            StatType.Defense => "DEF",
            StatType.AttackSpeed => "Attack Speed",
            StatType.MoveSpeed => "Move Speed",
            StatType.CritChance => "Crit Chance",
            StatType.CritDamage => "Crit Damage",
            StatType.DefensePenetration => "DEF Pen",
            StatType.DodgeChance => "Dodge",
            StatType.LifeSteal => "Life Steal",
            _ => stat.ToString()
        };

        /// <summary>Formats a stat for display, converting fractions and multipliers to percentages.</summary>
        public static string Format(StatType stat, float value)
        {
            if (IsFraction(stat)) return $"{value * 100f:0.#}%";
            if (stat == StatType.CritDamage) return $"{value * 100f:0}%";
            if (stat == StatType.AttackSpeed) return $"{value:0.00}/s";
            return $"{value:0.##}";
        }
    }
}
