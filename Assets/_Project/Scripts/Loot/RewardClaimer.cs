using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Economy;
using RPG.Inventory;
using RPG.Items;
using RPG.Stages;

namespace RPG.Loot
{
    /// <summary>Outcome of a claim, for the UI to report.</summary>
    public readonly struct ClaimResult
    {
        public readonly int GoldClaimed;
        public readonly int ItemsClaimed;
        public readonly int ItemsLeftPending;

        public ClaimResult(int goldClaimed, int itemsClaimed, int itemsLeftPending)
        {
            GoldClaimed = goldClaimed;
            ItemsClaimed = itemsClaimed;
            ItemsLeftPending = itemsLeftPending;
        }
    }

    /// <summary>
    /// Moves pending rewards into the player's account: gold to the wallet, items to the bag.
    ///
    /// XP is not handled here - it was awarded live, the moment each enemy died. Only the
    /// things that must wait for the end of the stage flow through this.
    ///
    /// Items that do not fit stay pending rather than being lost. Phase 14 replaces that with
    /// the timed Reward Storage from the design spec; until then nothing is ever discarded.
    /// </summary>
    public class RewardClaimer : MonoBehaviour
    {
        [SerializeField] private StageRewardCollector collector;
        [SerializeField] private InventoryManager inventory;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private StageEventChannel stageEvents;

        [Tooltip("Claim automatically when a stage completes. Phase 9's completion screen " +
                 "turns this off and claims after the reveal instead.")]
        [SerializeField] private bool claimOnStageComplete = true;

        [SerializeField] private bool logClaims = true;

        public event Action<ClaimResult> Claimed;

        private void OnEnable()
        {
            if (stageEvents != null) stageEvents.StageCompleted += OnStageCompleted;
        }

        private void OnDisable()
        {
            if (stageEvents != null) stageEvents.StageCompleted -= OnStageCompleted;
        }

        private void OnStageCompleted(StageData stage)
        {
            if (claimOnStageComplete) ClaimPending();
        }

        /// <summary>Banks everything that fits. Safe to call with nothing pending.</summary>
        public ClaimResult ClaimPending()
        {
            if (collector == null) return default;

            StageRewards pending = collector.Pending;

            int gold = pending.GoldEarned;
            if (gold > 0 && wallet != null) wallet.Add(CurrencyType.Gold, gold);

            List<EquipmentInstance> items = collector.ClaimAll();   // Also clears gold/xp counters.

            int added = 0;
            var leftover = new List<EquipmentInstance>();
            for (int i = 0; i < items.Count; i++)
            {
                if (inventory != null && inventory.TryAdd(items[i])) added++;
                else leftover.Add(items[i]);
            }

            // Anything that did not fit goes back to pending so it cannot be lost.
            for (int i = 0; i < leftover.Count; i++) pending.AddItem(leftover[i]);

            var result = new ClaimResult(gold, added, leftover.Count);

            if (logClaims)
            {
                Debug.Log($"[Rewards] Claimed {gold} gold and {added} item(s)" +
                          (leftover.Count > 0 ? $"; {leftover.Count} left pending (bag full)." : "."), this);
            }

            Claimed?.Invoke(result);
            return result;
        }
    }
}
