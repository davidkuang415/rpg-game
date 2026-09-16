using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Stats;

namespace RPG.Items
{
    /// <summary>
    /// A stat an item provides, expressed as a value at item level 1 plus growth per level.
    ///
    /// Storing growth per stat is what lets two swords of the same category feel different:
    /// one can scale hard on ATK while another scales on attack speed, rather than every
    /// weapon being the same item with a different number.
    /// </summary>
    [Serializable]
    public struct ItemStatLine
    {
        public StatType Stat;

        [Tooltip("Value at item level 1, before rarity and upgrades.")]
        public float BaseValue;

        [Tooltip("Added per item level beyond 1. This is what makes a high-level Rare beat a " +
                 "low-level Legendary.")]
        public float PerItemLevel;
    }

    /// <summary>
    /// The TEMPLATE for an item: "Iron Sword" as a concept, not a specific sword someone owns.
    ///
    /// Per the design spec there is no IronSword.cs - there is one Iron Sword asset, and every
    /// Iron Sword in the game is an EquipmentInstance pointing at it with its own level,
    /// rarity, upgrades and enchantments.
    /// </summary>
    public abstract class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable ID written into save data. NEVER change this once items exist; " +
                 "renaming the asset is safe, changing this orphans every saved copy.")]
        [SerializeField] private string itemId = "iron_sword";

        [SerializeField] private string displayName = "Iron Sword";
        [SerializeField] private Sprite icon;

        [TextArea(2, 3)]
        [SerializeField] private string description;

        [Header("Stats")]
        [Tooltip("What this item provides. Values are before rarity, upgrades and enchantments.")]
        [SerializeField] private List<ItemStatLine> stats = new List<ItemStatLine>();

        [Header("Drops")]
        [Tooltip("Relative weight when this item is picked from a loot table.")]
        [SerializeField, Min(0f)] private float dropWeight = 1f;

        [Tooltip("Base gold value at item level 1, before rarity's recycle multiplier.")]
        [SerializeField, Min(0f)] private float baseValue = 10f;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public string Description => description;
        public IReadOnlyList<ItemStatLine> Stats => stats;
        public float DropWeight => dropWeight;
        public float BaseValue => baseValue;

        /// <summary>Which slot this item occupies. Weapons answer Weapon; armour answers its piece.</summary>
        public abstract EquipmentSlot Slot { get; }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogWarning($"ItemDefinition '{name}' has an empty Item Id. Save data needs one.", this);
            }
        }
    }
}
