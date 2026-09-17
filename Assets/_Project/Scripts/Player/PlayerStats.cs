using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Classes;
using RPG.Core.Combat;
using RPG.Stats;

namespace RPG.Player
{
    /// <summary>
    /// Owns the player's stats and is the ONLY thing allowed to produce final stat values.
    ///
    /// Other systems either:
    ///   - register as an IStatModifierSource (equipment, buffs, level growth), or
    ///   - read PlayerStats.Current.
    ///
    /// Nothing writes to Current directly. That single rule is what stops stat bugs from
    /// becoming untraceable later, when six systems all want to change ATK.
    /// </summary>
    public class PlayerStats : MonoBehaviour, ICombatStatProvider
    {
        [Header("Configuration")]
        [Tooltip("Class applied on Awake. Class selection overwrites this at runtime.")]
        [SerializeField] private ClassData startingClass;

        [Tooltip("Caps and floors applied after every recalculation.")]
        [SerializeField] private StatRules statRules;

        private readonly StatBlock _baseStats = new StatBlock();
        private readonly StatBlock _current = new StatBlock();
        private readonly List<IStatModifierSource> _sources = new List<IStatModifierSource>();
        private readonly List<StatModifier> _modifierBuffer = new List<StatModifier>(32);

        /// <summary>Final stats after the full pipeline. Read-only by convention - do not write to it.</summary>
        public StatBlock Current => _current;

        /// <summary>Class stats before any modifier. Useful for "base vs total" UI later.</summary>
        public StatBlock BaseStats => _baseStats;

        public ClassData CurrentClass { get; private set; }

        /// <summary>Raised whenever final stats change, for any reason.</summary>
        public event Action<PlayerStats> StatsChanged;

        /// <summary>Raised when the player switches class. Fires before StatsChanged.</summary>
        public event Action<ClassData> ClassChanged;

        private void Awake()
        {
            if (startingClass != null) SetClass(startingClass);
        }

        /// <summary>
        /// Switches class and rebuilds stats. Level, XP, currency and inventory are shared
        /// across classes, so nothing else is reset here.
        /// </summary>
        public void SetClass(ClassData classData)
        {
            if (classData == null)
            {
                Debug.LogError($"{nameof(PlayerStats)}: SetClass called with null.", this);
                return;
            }

            CurrentClass = classData;
            classData.WriteBaseStats(_baseStats);

            ClassChanged?.Invoke(classData);
            Recalculate();
        }

        /// <summary>
        /// Drops back to "no class chosen", the state a brand new profile is in.
        ///
        /// Separate from SetClass because SetClass treats null as a caller mistake - which it is
        /// everywhere except here, where clearing the class is the whole point.
        /// </summary>
        public void ClearClass()
        {
            CurrentClass = null;
            _baseStats.Clear();

            ClassChanged?.Invoke(null);
            Recalculate();
        }

        /// <summary>Registers a stat contributor (equipment manager, buff, level curve).</summary>
        public void RegisterSource(IStatModifierSource source)
        {
            if (source == null || _sources.Contains(source)) return;
            _sources.Add(source);
            Recalculate();
        }

        public void UnregisterSource(IStatModifierSource source)
        {
            if (source == null || !_sources.Remove(source)) return;
            Recalculate();
        }

        /// <summary>
        /// Rebuilds final stats from base + every registered source.
        /// Call this after a source's contribution changes (equip, level-up, buff expiry).
        /// </summary>
        public void Recalculate()
        {
            _modifierBuffer.Clear();

            for (int i = 0; i < _sources.Count; i++)
            {
                _sources[i].CollectModifiers(_modifierBuffer);
            }

            StatCalculator.Compute(_baseStats, _modifierBuffer, statRules, _current);
            StatsChanged?.Invoke(this);
        }

        /// <summary>
        /// ICombatStatProvider: how the shared combat system reads the player's stats.
        /// Enemies and bosses implement the same interface, so one damage pipeline serves all.
        /// </summary>
        public float GetStat(StatType stat) => _current[stat];
    }
}
