using System.Collections.Generic;
using UnityEngine;
using RPG.Core.Combat;
using RPG.Stats;

namespace RPG.Enemies
{
    /// <summary>
    /// An enemy's runtime stats: EnemyData run through the difficulty curve for its level.
    ///
    /// Implements the same ICombatStatProvider the player does, so the shared damage pipeline
    /// treats enemies and the player identically - one defense formula, one crit rule, one
    /// dodge roll, for everyone.
    ///
    /// Elite modifiers and debuffs will arrive as IStatModifierSource registrations; the
    /// plumbing for that is already here.
    /// </summary>
    public class EnemyStats : MonoBehaviour, ICombatStatProvider
    {
        [Header("Definition")]
        [SerializeField] private EnemyData data;
        [SerializeField] private DifficultyCurveData difficultyCurve;
        [SerializeField] private StatRules statRules;

        [Header("Instance")]
        [Tooltip("Enemy Level - power and reward scaling. Deliberately NOT the stage number.")]
        [SerializeField, Min(1)] private int level = 1;

        [Tooltip("Elites get the Difficulty Curve's elite bonuses registered as a stat source, " +
                 "and pay out its reward multiplier on death.")]
        [SerializeField] private bool isElite;

        private readonly StatBlock _baseStats = new StatBlock();
        private readonly StatBlock _current = new StatBlock();
        private readonly List<IStatModifierSource> _sources = new List<IStatModifierSource>();
        private readonly List<StatModifier> _modifierBuffer = new List<StatModifier>(8);

        public EnemyData Data => data;
        public DifficultyCurveData DifficultyCurve => difficultyCurve;
        public int Level => level;
        public bool IsElite => isElite;
        public StatBlock Current => _current;

        /// <summary>XP granted on death, scaled for this enemy's level and elite status.</summary>
        public float XpReward => (difficultyCurve != null
            ? difficultyCurve.ScaleXp(data != null ? data.XpReward : 0f, level)
            : (data != null ? data.XpReward : 0f)) * RewardMultiplier;

        /// <summary>Gold granted on death, scaled for this enemy's level and elite status.</summary>
        public int GoldReward => Mathf.RoundToInt((difficultyCurve != null
            ? difficultyCurve.ScaleGold(data != null ? data.GoldReward : 0f, level)
            : (data != null ? data.GoldReward : 0f)) * RewardMultiplier);

        private float RewardMultiplier =>
            isElite && difficultyCurve != null ? difficultyCurve.EliteRewardMultiplier : 1f;

        private bool _built;
        private EliteModifierSource _eliteSource;

        private void Awake()
        {
            SyncEliteSource();
            Rebuild();
        }

        /// <summary>
        /// Sets up a spawned enemy. Spawn points call this so one prefab can serve every
        /// level and elite variant of an enemy type.
        /// </summary>
        public void Configure(EnemyData enemyData, int enemyLevel, bool elite = false)
        {
            data = enemyData;
            level = Mathf.Max(1, enemyLevel);
            isElite = elite;
            SyncEliteSource();
            Rebuild();
        }

        /// <summary>
        /// Keeps exactly one elite source registered while elite, none otherwise. A pooled
        /// enemy can be an elite on one spawn and a regular on the next, so this is re-run on
        /// every Configure rather than once in Awake.
        /// </summary>
        private void SyncEliteSource()
        {
            if (isElite)
            {
                if (_eliteSource == null) _eliteSource = new EliteModifierSource(difficultyCurve);
                if (!_sources.Contains(_eliteSource)) _sources.Add(_eliteSource);
            }
            else if (_eliteSource != null)
            {
                _sources.Remove(_eliteSource);
            }
        }

        public void RegisterSource(IStatModifierSource source)
        {
            if (source == null || _sources.Contains(source)) return;
            _sources.Add(source);
            Rebuild();
        }

        public void UnregisterSource(IStatModifierSource source)
        {
            if (source == null || !_sources.Remove(source)) return;
            Rebuild();
        }

        public void Rebuild()
        {
            _built = true;

            _baseStats.Clear();

            if (data != null)
            {
                bool hasCurve = difficultyCurve != null;

                _baseStats[StatType.MaxHealth] = hasCurve
                    ? difficultyCurve.ScaleHealth(data.MaxHealth, level) : data.MaxHealth;
                _baseStats[StatType.Attack] = hasCurve
                    ? difficultyCurve.ScaleAttack(data.Attack, level) : data.Attack;
                _baseStats[StatType.Defense] = hasCurve
                    ? difficultyCurve.ScaleDefense(data.Defense, level) : data.Defense;

                // Speeds do not scale with level: a level 50 grunt should be deadlier, not
                // faster than the player can react to.
                _baseStats[StatType.MoveSpeed] = data.MoveSpeed;
                _baseStats[StatType.AttackSpeed] = data.AttackSpeed;
                _baseStats[StatType.CritDamage] = 1.5f;
            }

            _modifierBuffer.Clear();
            for (int i = 0; i < _sources.Count; i++) _sources[i].CollectModifiers(_modifierBuffer);

            StatCalculator.Compute(_baseStats, _modifierBuffer, statRules, _current);
        }

        /// <summary>
        /// Builds on first read if it has not happened yet. Component Awake order within a
        /// GameObject is not guaranteed, and Health asks for Max HP during its own Awake -
        /// without this, an enemy could be created already dead.
        /// </summary>
        public float GetStat(StatType stat)
        {
            if (!_built) Rebuild();
            return _current[stat];
        }
    }
}
