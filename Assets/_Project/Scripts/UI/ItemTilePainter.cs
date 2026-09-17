using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RPG.Inventory;
using RPG.Items;
using RPG.Stats;

namespace RPG.UI
{
    /// <summary>
    /// How an item looks on a button, and how it reads in a details box.
    ///
    /// The gear page and the bag page draw the same items, and before this existed they drew
    /// them with two copies of the same code that had already started to disagree about
    /// rarity colours. One copy, both callers.
    /// </summary>
    public static class ItemTilePainter
    {
        /// <summary>Paints an item onto a tile button, or an empty slot when the item is null.</summary>
        public static void Paint(Button button, EquipmentInstance item, ItemRegistry registry,
            RarityTable rarityTable, Color emptyColor, string emptyText)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            var label = button.GetComponentInChildren<Text>();

            if (item == null)
            {
                if (image != null) image.color = emptyColor;
                if (label != null) label.text = emptyText;
                return;
            }

            if (image != null && rarityTable != null)
            {
                // Tinted towards the rarity rather than painted with it: a full-strength
                // Legendary orange behind white text is unreadable on a phone.
                Color rarityColor = rarityTable.GetColor(item.Rarity);
                image.color = Color.Lerp(emptyColor, rarityColor, 0.45f);
            }

            if (label != null)
            {
                ItemDefinition definition = registry != null ? registry.GetDefinition(item) : null;
                string itemName = definition != null ? definition.DisplayName : item.TemplateId;
                string upgrade = item.UpgradeLevel > 0 ? $" +{item.UpgradeLevel}" : string.Empty;
                label.text = $"{itemName}{upgrade}\nLv {item.ItemLevel}  {item.Rarity}";
            }
        }

        /// <summary>
        /// Marks a tile as the one currently selected: a brighter border-like tint and a
        /// slight scale, so the item the action buttons refer to is never ambiguous.
        /// </summary>
        public static void SetSelected(Button button, bool selected)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                Color c = image.color;
                c.a = selected ? 1f : 0.92f;
                image.color = c;
            }

            button.transform.localScale = selected ? Vector3.one * 1.06f : Vector3.one;
        }

        public static string DisplayName(EquipmentInstance item, ItemRegistry registry,
            RarityTable rarityTable)
        {
            if (item == null) return "(nothing)";

            ItemDefinition definition = registry != null ? registry.GetDefinition(item) : null;
            return EquipmentStatCalculator.GetDisplayName(item, definition, rarityTable);
        }

        /// <summary>
        /// The item's stat lines. The scratch block and builder are passed in so that opening a
        /// bag of sixty items does not allocate sixty of each.
        /// </summary>
        public static string DescribeStats(EquipmentInstance item, ItemRegistry registry,
            RarityTable rarityTable, StatBlock scratch, StringBuilder builder,
            ItemEconomyConfig economy = null)
        {
            if (item == null) return string.Empty;

            ItemDefinition definition = registry != null ? registry.GetDefinition(item) : null;
            if (definition == null) return "(unknown item)";

            // The multiplier the equipment manager would use, so the bag never promises
            // numbers that differ from what equipping actually gives.
            float upgradeMultiplier = economy != null
                ? economy.GetUpgradeMultiplier(item.UpgradeLevel)
                : 1f;

            EquipmentStatCalculator.ComputeStats(item, definition, rarityTable, scratch, upgradeMultiplier);

            builder.Clear();
            for (int i = 0; i < StatTypeInfo.Count; i++)
            {
                var stat = (StatType)i;
                float value = scratch[stat];
                if (value == 0f) continue;

                builder.Append(StatTypeInfo.DisplayName(stat)).Append(": ")
                       .Append(value > 0f ? "+" : string.Empty)
                       .Append(StatTypeInfo.Format(stat, value)).Append('\n');
            }

            if (definition is WeaponDefinition weapon)
            {
                builder.Append("Range: ").Append(weapon.Range.ToString("0.0"));
                if (weapon.ArcDegrees > 0f) builder.Append("   Arc: ").Append(weapon.ArcDegrees.ToString("0"));
            }

            return builder.ToString();
        }

        /// <summary>Turns an equip failure into something a player can act on.</summary>
        public static string Describe(EquipResult result) => result switch
        {
            EquipResult.WrongClass => "your class cannot use this weapon type.",
            EquipResult.SlotNotSupported => "that slot is not available yet.",
            EquipResult.InventoryFull => "the bag is full.",
            EquipResult.NotInInventory => "item is not in the bag.",
            EquipResult.NothingEquipped => "nothing equipped there.",
            EquipResult.UnknownItem => "unknown item.",
            _ => result.ToString()
        };
    }
}
