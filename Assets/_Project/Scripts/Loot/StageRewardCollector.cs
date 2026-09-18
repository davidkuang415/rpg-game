using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Enemies;
using RPG.Items;
using RPG.Stages;

namespace RPG.Loot
{
    /// <summary>
    /// Watches enemies die and banks what they were worth.
    ///
    /// The key design rule this implements: equipment does NOT physically drop during combat.
    /// A successful loot roll goes straight into the pending reward list, so the player never
    /// stops fighting to pick things up or manage inventory mid-room. The completion screen
    /// reveals everything at the end.
    ///
    /// Rewards persist across stage restarts on purpose - dying must not cost loot already
    /// earned - so the list is only emptied by ClaimAll().
    /// </summary>
    public class StageRewardCollector : MonoBehaviour
    {
        [Header("Channels")]
        [SerializeField] private EnemyEventChannel enemyEvents;
        [SerializeField] private StageEventChannel stageEvents;

        [Header("Data")]
        [SerializeField] private RarityTable rarityTable;

        [Tooltip("Used when an enemy has no loot table of its own. Optional.")]
        [SerializeField] private LootTableData fallbackLootTable;

        [Tooltip("Rolled instead of the enemy's own table when it died as an elite. Optional; " +
                 "without it an elite drops from its normal table.")]
        [SerializeField] private LootTableData eliteLootTable;

        [SerializeField] private bool logDrops = true;

        private readonly List<EquipmentInstance> _rollBuffer = new List<EquipmentInstance>(4);
        private StageData _currentStage;

        public StageRewards Pending { get; } = new StageRewards();

        /// <summary>Raised for each item generated. Optional drop VFX would listen here.</summary>
        public event Action<EquipmentInstance, Vector3> ItemGenerated;

        /// <summary>Raised when a stage completes, with everything banked so far.</summary>
        public event Action<StageRewards> RewardsReady;

        private void OnEnable()
        {
            if (enemyEvents != null) enemyEvents.EnemyDied += OnEnemyDied;

            if (stageEvents != null)
            {
                stageEvents.StageStarted += OnStageStarted;
                stageEvents.StageCompleted += OnStageCompleted;
            }
        }

        private void OnDisable()
        {
            if (enemyEvents != null) enemyEvents.EnemyDied -= OnEnemyDied;

            if (stageEvents != null)
            {
                stageEvents.StageStarted -= OnStageStarted;
                stageEvents.StageCompleted -= OnStageCompleted;
            }
        }

        // Only records which stage is running - deliberately does NOT clear pending rewards.
        private void OnStageStarted(StageData stage) => _currentStage = stage;

        private void OnStageCompleted(StageData stage) => RewardsReady?.Invoke(Pending);

        private void OnEnemyDied(EnemyDeathInfo info)
        {
            Pending.CountKill();
            Pending.AddXp(info.XpReward);

            float goldModifier = _currentStage != null ? _currentStage.GoldModifier : 1f;
            Pending.AddGold(Mathf.RoundToInt(info.GoldReward * goldModifier));

            RollLoot(info);
        }

        private void RollLoot(EnemyDeathInfo info)
        {
            LootTableData table = info.Data != null && info.Data.LootTable != null
                ? info.Data.LootTable
                : fallbackLootTable;

            // A boss keeps its own table - it is already the best one - but any lesser enemy
            // promoted to elite rolls from the elite table.
            bool isBoss = info.Data != null && info.Data.IsBoss;
            if (info.IsElite && !isBoss && eliteLootTable != null) table = eliteLootTable;

            if (table == null) return;

            float lootModifier = _currentStage != null ? _currentStage.LootModifier : 1f;

            _rollBuffer.Clear();
            LootGenerator.TryRoll(table, rarityTable, info.Level, lootModifier, _rollBuffer);

            for (int i = 0; i < _rollBuffer.Count; i++)
            {
                EquipmentInstance item = _rollBuffer[i];
                Pending.AddItem(item);
                ItemGenerated?.Invoke(item, info.Position);

                if (logDrops) Debug.Log($"[Loot] {item}", this);
            }
        }

        /// <summary>
        /// Hands over everything banked and empties the list. Phase 8 will pass these to the
        /// inventory; Phase 9's completion screen calls it after the reveal animation.
        /// </summary>
        public List<EquipmentInstance> ClaimAll()
        {
            var claimed = new List<EquipmentInstance>(Pending.Items);
            Pending.Clear();
            return claimed;
        }

        /// <summary>Debug tool support: generates one item without needing a kill.</summary>
        public EquipmentInstance DebugGenerate(int enemyLevel)
        {
            if (fallbackLootTable == null) return null;

            EquipmentInstance item = LootGenerator.Generate(fallbackLootTable, rarityTable, enemyLevel, 1f);
            if (item == null) return null;

            Pending.AddItem(item);
            return item;
        }
    }
}
