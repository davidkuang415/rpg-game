using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// Every tuning value for every rarity, in one asset.
    ///
    /// The design spec is explicit that rarity values must not be scattered across scripts, so
    /// stat multipliers, drop weights, enchantment rules, recycle values and colours all live
    /// here. Rebalancing rarity is editing one asset.
    /// </summary>
    [CreateAssetMenu(fileName = "RarityTable", menuName = "RPG/Items/Rarity Table")]
    public class RarityTable : ScriptableObject
    {
        [Serializable]
        public class RarityEntry
        {
            public Rarity Rarity;
            public string DisplayName;

            [Header("Power")]
            [Tooltip("Multiplies the item's base stats. Item Level still dominates long-term.")]
            [Min(0.1f)] public float StatMultiplier = 1f;

            [Header("Drops")]
            [Tooltip("Relative chance of rolling this rarity. Weights need not sum to anything.")]
            [Min(0f)] public float LootWeight = 1f;

            [Header("Enchantments (used from Phase 12)")]
            [Tooltip("Total enchantment slots this rarity supports.")]
            [Min(0)] public int MaxEnchantments;

            [Tooltip("Chance an item of this rarity drops with a natural enchantment already on it.")]
            [Range(0f, 1f)] public float NaturalEnchantChance;

            [Tooltip("Whether the player may add enchantments by hand. Epics may not, per the spec.")]
            public bool AllowsManualEnchanting;

            [Tooltip("Whether this rarity can belong to a set. Mythic only, for now.")]
            public bool SupportsSetBonus;

            [Header("Economy and Presentation")]
            [Tooltip("Multiplies gold received when recycling.")]
            [Min(0f)] public float RecycleValueMultiplier = 1f;

            public Color Color = Color.white;
        }

        [SerializeField] private List<RarityEntry> entries = new List<RarityEntry>();

        public IReadOnlyList<RarityEntry> Entries => entries;

        public RarityEntry Get(Rarity rarity)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].Rarity == rarity) return entries[i];
            }

            Debug.LogError($"RarityTable '{name}': no entry for {rarity}.", this);
            return null;
        }

        public float GetStatMultiplier(Rarity rarity)
        {
            RarityEntry entry = Get(rarity);
            return entry != null ? entry.StatMultiplier : 1f;
        }

        public Color GetColor(Rarity rarity)
        {
            RarityEntry entry = Get(rarity);
            return entry != null ? entry.Color : Color.white;
        }

        /// <summary>Project defaults, written by the setup tool. Retune freely afterwards.</summary>
        public void ResetToDefaults()
        {
            entries = new List<RarityEntry>
            {
                Entry(Rarity.Common, "Common", 1.00f, 55f, 0, 0f, false, false, 1.0f,
                    new Color(0.75f, 0.75f, 0.78f)),
                Entry(Rarity.Uncommon, "Uncommon", 1.15f, 25f, 0, 0f, false, false, 1.6f,
                    new Color(0.45f, 0.85f, 0.45f)),
                Entry(Rarity.Rare, "Rare", 1.35f, 13f, 0, 0f, false, false, 2.6f,
                    new Color(0.35f, 0.65f, 1f)),

                // Epic: may drop with an enchantment, but cannot be enchanted by hand.
                Entry(Rarity.Epic, "Epic", 1.60f, 5f, 1, 0.5f, false, false, 4.5f,
                    new Color(0.72f, 0.4f, 1f)),

                // Legendary: up to 3 enchantments, and the player may fill empty slots.
                Entry(Rarity.Legendary, "Legendary", 1.90f, 1.8f, 3, 0.7f, true, false, 9f,
                    new Color(1f, 0.62f, 0.2f)),

                // Mythic: as Legendary, plus set bonuses.
                Entry(Rarity.Mythic, "Mythic", 2.30f, 0.2f, 3, 1f, true, true, 18f,
                    new Color(1f, 0.35f, 0.45f))
            };
        }

        private static RarityEntry Entry(Rarity rarity, string displayName, float statMultiplier,
            float lootWeight, int maxEnchants, float naturalEnchantChance, bool allowsManual,
            bool supportsSet, float recycleMultiplier, Color color) => new RarityEntry
        {
            Rarity = rarity,
            DisplayName = displayName,
            StatMultiplier = statMultiplier,
            LootWeight = lootWeight,
            MaxEnchantments = maxEnchants,
            NaturalEnchantChance = naturalEnchantChance,
            AllowsManualEnchanting = allowsManual,
            SupportsSetBonus = supportsSet,
            RecycleValueMultiplier = recycleMultiplier,
            Color = color
        };
    }
}
