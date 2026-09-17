using System.Text;
using UnityEngine;
using RPG.Stats;

namespace RPG.Items
{
    /// <summary>
    /// Turns an item instance into actual stat numbers.
    ///
    ///     value = (BaseValue + PerItemLevel x (ItemLevel - 1)) x RarityMultiplier x UpgradeMultiplier
    ///
    /// Note what is NOT in that formula: enchantments. Per the design spec, upgrading raises an
    /// item's BASE stats only and must never scale its enchantment values, so enchantments are
    /// applied separately as their own modifier layer (Phase 12) rather than folded in here.
    ///
    /// This is also where the "item level beats rarity" rule actually lives: rarity is a flat
    /// multiplier while item level compounds through PerItemLevel, so a high-level Rare
    /// overtakes a low-level Legendary exactly as intended.
    /// </summary>
    public static class EquipmentStatCalculator
    {
        /// <summary>
        /// Writes an item's stats into a block.
        /// </summary>
        /// <param name="upgradeMultiplier">
        /// Bonus from the item's upgrade level. 1 = +0. Callers get it from
        /// ItemEconomyConfig.GetUpgradeMultiplier; it is passed in rather than looked up here
        /// so this class stays free of asset references.
        /// </param>
        public static void ComputeStats(EquipmentInstance instance, ItemDefinition definition,
            RarityTable rarityTable, StatBlock into, float upgradeMultiplier = 1f)
        {
            if (into == null) return;
            into.Clear();

            if (instance == null || definition == null) return;

            float rarityMultiplier = rarityTable != null
                ? rarityTable.GetStatMultiplier(instance.Rarity)
                : 1f;

            int levelsAboveOne = Mathf.Max(0, instance.ItemLevel - 1);

            for (int i = 0; i < definition.Stats.Count; i++)
            {
                ItemStatLine line = definition.Stats[i];
                float value = line.BaseValue + line.PerItemLevel * levelsAboveOne;
                into[line.Stat] = value * rarityMultiplier * upgradeMultiplier;
            }
        }

        /// <summary>"Legendary Iron Sword +2 (Lv 34)"</summary>
        public static string GetDisplayName(EquipmentInstance instance, ItemDefinition definition,
            RarityTable rarityTable)
        {
            if (instance == null) return "<empty>";

            string itemName = definition != null ? definition.DisplayName : instance.TemplateId;

            RarityTable.RarityEntry entry = rarityTable != null ? rarityTable.Get(instance.Rarity) : null;
            string rarityName = entry != null ? entry.DisplayName : instance.Rarity.ToString();

            var builder = new StringBuilder();
            builder.Append(rarityName).Append(' ').Append(itemName);
            if (instance.UpgradeLevel > 0) builder.Append(" +").Append(instance.UpgradeLevel);
            builder.Append(" (Lv ").Append(instance.ItemLevel).Append(')');
            return builder.ToString();
        }

        /// <summary>
        /// A single number for sorting and for "is this an upgrade?" hints. Deliberately crude -
        /// it is a UI convenience, never a balance input.
        /// </summary>
        public static float GetPowerScore(EquipmentInstance instance, ItemDefinition definition,
            RarityTable rarityTable, StatBlock scratch)
        {
            ComputeStats(instance, definition, rarityTable, scratch);

            float score = 0f;
            for (int i = 0; i < StatTypeInfo.Count; i++)
            {
                var stat = (StatType)i;
                float value = scratch[stat];
                if (value == 0f) continue;

                // Fractional stats (crit chance, dodge) are tiny numbers with large impact,
                // so they are weighted up to keep the score meaningful.
                score += StatTypeInfo.IsFraction(stat) ? value * 100f : value;
            }

            return score;
        }
    }
}
