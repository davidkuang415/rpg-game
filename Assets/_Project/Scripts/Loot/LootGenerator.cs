using System.Collections.Generic;
using UnityEngine;
using RPG.Items;

namespace RPG.Loot
{
    /// <summary>
    /// Creates item instances from loot tables.
    ///
    /// Every generated item is a brand new EquipmentInstance with its own GUID, so duplicates
    /// are genuinely separate objects that can carry different levels, rarities, upgrades and
    /// enchantments - the design spec's duplicate rule, enforced at the point of creation.
    ///
    /// Enchantment rolling is not here yet (Phase 12). When it arrives it plugs in at the end
    /// of Generate(), using the rarity's MaxEnchantments and NaturalEnchantChance which the
    /// RarityTable already carries.
    /// </summary>
    public static class LootGenerator
    {
        /// <summary>
        /// Rolls the drop chance and, if it succeeds, generates items into <paramref name="results"/>.
        /// Returns how many items were added.
        /// </summary>
        public static int TryRoll(LootTableData table, RarityTable rarityTable, int enemyLevel,
            float stageLootModifier, List<EquipmentInstance> results)
        {
            if (table == null || results == null) return 0;

            if (!table.GuaranteedDrop && Random.value > table.DropChance) return 0;

            int generated = 0;
            for (int i = 0; i < table.RollCount; i++)
            {
                EquipmentInstance item = Generate(table, rarityTable, enemyLevel, stageLootModifier);
                if (item == null) continue;

                results.Add(item);
                generated++;
            }

            return generated;
        }

        /// <summary>Generates one item, ignoring the drop chance.</summary>
        public static EquipmentInstance Generate(LootTableData table, RarityTable rarityTable,
            int enemyLevel, float stageLootModifier)
        {
            ItemDefinition definition = PickItem(table);
            if (definition == null) return null;

            int itemLevel = table.RollItemLevel(enemyLevel);
            Rarity rarity = RollRarity(table, rarityTable, enemyLevel, stageLootModifier);

            return new EquipmentInstance(definition.ItemId, itemLevel, rarity);
        }

        /// <summary>Weighted pick from the table's candidates, using each item's Drop Weight.</summary>
        private static ItemDefinition PickItem(LootTableData table)
        {
            IReadOnlyList<ItemDefinition> candidates = table.PossibleItems;

            float totalWeight = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null) totalWeight += Mathf.Max(0f, candidates[i].DropWeight);
            }

            if (totalWeight <= 0f) return null;

            float roll = Random.value * totalWeight;
            for (int i = 0; i < candidates.Count; i++)
            {
                ItemDefinition candidate = candidates[i];
                if (candidate == null) continue;

                roll -= Mathf.Max(0f, candidate.DropWeight);
                if (roll <= 0f) return candidate;
            }

            return candidates.Count > 0 ? candidates[candidates.Count - 1] : null;
        }

        /// <summary>
        /// Weighted rarity roll.
        ///
        /// Luck scales the weight of each tier by luck^tier, so a modifier above 1 lifts high
        /// rarities far more than low ones, and a modifier of exactly 1 changes nothing. Enemy
        /// level feeds in the same way, which is how "higher level enemies drop better gear"
        /// is expressed without a second set of tables.
        /// </summary>
        private static Rarity RollRarity(LootTableData table, RarityTable rarityTable,
            int enemyLevel, float stageLootModifier)
        {
            if (rarityTable == null) return table.MinimumRarity;

            float levelLuck = 1f + table.LuckPerEnemyLevel * Mathf.Max(0, enemyLevel - 1);
            float luck = Mathf.Max(0.01f, stageLootModifier * levelLuck);

            IReadOnlyList<RarityTable.RarityEntry> entries = rarityTable.Entries;

            float totalWeight = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                totalWeight += WeightOf(entries[i], table.MinimumRarity, luck);
            }

            if (totalWeight <= 0f) return table.MinimumRarity;

            float roll = Random.value * totalWeight;
            for (int i = 0; i < entries.Count; i++)
            {
                roll -= WeightOf(entries[i], table.MinimumRarity, luck);
                if (roll <= 0f) return entries[i].Rarity;
            }

            return table.MinimumRarity;
        }

        private static float WeightOf(RarityTable.RarityEntry entry, Rarity minimumRarity, float luck)
        {
            if (entry == null || entry.Rarity < minimumRarity) return 0f;

            int tier = (int)entry.Rarity;
            return entry.LootWeight * Mathf.Pow(luck, tier);
        }
    }
}
