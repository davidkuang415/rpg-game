using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Player;
using RPG.Stats;

namespace RPG.Progression
{
    /// <summary>
    /// The player's character level and current XP.
    ///
    /// It contributes to stats the same way equipment eventually will - as an
    /// IStatModifierSource on the LevelGrowth layer - rather than writing to stats directly.
    /// That is why a level-up needs no special case anywhere in the stat system.
    ///
    /// Level and XP are shared across classes: switching from Knight to Archer keeps both,
    /// exactly as the design spec requires.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerLevel : MonoBehaviour, IStatModifierSource
    {
        [Header("Configuration")]
        [SerializeField] private XpCurveData xpCurve;
        [SerializeField] private LevelGrowthData levelGrowth;

        [Header("Starting State")]
        [SerializeField, Min(1)] private int startingLevel = 1;
        [SerializeField, Min(0f)] private float startingXp;

        [SerializeField] private bool logLevelUps = true;

        private PlayerStats _stats;

        public int Level { get; private set; } = 1;

        /// <summary>XP earned toward the NEXT level, not lifetime XP.</summary>
        public float CurrentXp { get; private set; }

        public float XpForNextLevel => xpCurve != null
            ? xpCurve.GetRequirementForLevel(Level)
            : float.PositiveInfinity;

        public bool IsMaxLevel => xpCurve != null && Level >= xpCurve.MaxLevel;

        public float XpProgress
        {
            get
            {
                float required = XpForNextLevel;
                return float.IsInfinity(required) || required <= 0f ? 1f : Mathf.Clamp01(CurrentXp / required);
            }
        }

        /// <summary>(currentXp, requiredXp, level) after any XP change. The XP bar listens here.</summary>
        public event Action<float, float, int> XpChanged;

        /// <summary>(previousLevel, newLevel). Fires once per level, so a double level-up fires twice.</summary>
        public event Action<int, int> LeveledUp;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            Level = Mathf.Max(1, startingLevel);
            CurrentXp = Mathf.Max(0f, startingXp);
        }

        private void OnEnable() => _stats.RegisterSource(this);
        private void OnDisable() => _stats.UnregisterSource(this);

        private void Start() => RaiseXpChanged();

        /// <summary>
        /// Awards XP and resolves any level-ups. A single large award can grant several
        /// levels; each one raises its own event so the completion screen can animate them
        /// in sequence.
        /// </summary>
        public void AddXp(float amount)
        {
            if (amount <= 0f || IsMaxLevel) return;

            CurrentXp += amount;

            bool leveled = false;
            while (!IsMaxLevel && CurrentXp >= XpForNextLevel)
            {
                float required = XpForNextLevel;
                CurrentXp -= required;

                int previousLevel = Level;
                Level++;
                leveled = true;

                if (logLevelUps) Debug.Log($"[PlayerLevel] Level {previousLevel} -> {Level}", this);
                LeveledUp?.Invoke(previousLevel, Level);
            }

            // At max level XP stops accumulating rather than sitting in a bar that cannot fill.
            if (IsMaxLevel) CurrentXp = 0f;

            // One recalculation for the whole award, not one per level gained.
            if (leveled) _stats.Recalculate();

            RaiseXpChanged();
        }

        /// <summary>Restores saved progress. Used by the save system and debug tools.</summary>
        public void SetProgress(int level, float currentXp)
        {
            Level = Mathf.Clamp(level, 1, xpCurve != null ? xpCurve.MaxLevel : level);
            CurrentXp = Mathf.Max(0f, currentXp);

            _stats.Recalculate();
            RaiseXpChanged();
        }

        /// <summary>IStatModifierSource: hands the level's stat growth to the central calculator.</summary>
        public void CollectModifiers(List<StatModifier> results)
        {
            if (levelGrowth != null) levelGrowth.Collect(Level, results);
        }

        private void RaiseXpChanged() => XpChanged?.Invoke(CurrentXp, XpForNextLevel, Level);
    }
}
