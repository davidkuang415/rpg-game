using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Items
{
    /// <summary>One rolled enchantment on one item. Values are rolled once, at drop time.</summary>
    [Serializable]
    public struct EnchantmentInstance
    {
        [Tooltip("Stable ID of the EnchantmentData that produced this. Resolved via the registry.")]
        public string EnchantmentId;

        [Tooltip("The rolled value, in the enchantment's own units (0.05 = 5%).")]
        public float Value;

        [Tooltip("False for enchantments the player added later, true for ones the item dropped with.")]
        public bool IsNatural;
    }

    /// <summary>
    /// A specific item that a specific player owns.
    ///
    /// This is the SAVE-SAFE half of the item system. It is a plain serializable class - not a
    /// ScriptableObject - because it is per-player mutable data. It references its template by
    /// stable string ID rather than by asset reference, which is what lets item assets be
    /// renamed, moved or rebuilt without corrupting saves.
    ///
    /// Every instance carries its own GUID, so two Iron Swords with different levels, rarities
    /// and enchantments are never confused for one another.
    /// </summary>
    [Serializable]
    public class EquipmentInstance
    {
        [SerializeField] private string instanceId;
        [SerializeField] private string templateId;
        [SerializeField] private int itemLevel = 1;
        [SerializeField] private Rarity rarity = Rarity.Common;
        [SerializeField] private int upgradeLevel;
        [SerializeField] private List<EnchantmentInstance> enchantments = new List<EnchantmentInstance>();

        /// <summary>Unique per item. Two identical swords still have different instance IDs.</summary>
        public string InstanceId => instanceId;

        /// <summary>Stable ID of the ItemDefinition this was made from.</summary>
        public string TemplateId => templateId;

        public int ItemLevel => itemLevel;
        public Rarity Rarity => rarity;

        /// <summary>Upgrade level (+0, +1, ...). Raised by spending gold, from Phase 11.</summary>
        public int UpgradeLevel => upgradeLevel;

        public IReadOnlyList<EnchantmentInstance> Enchantments => enchantments;

        /// <summary>Serialization constructor. Use the factory below in gameplay code.</summary>
        public EquipmentInstance() { }

        public EquipmentInstance(string templateId, int itemLevel, Rarity rarity)
        {
            instanceId = Guid.NewGuid().ToString("N");
            this.templateId = templateId;
            this.itemLevel = Mathf.Max(1, itemLevel);
            this.rarity = rarity;
            upgradeLevel = 0;
            enchantments = new List<EnchantmentInstance>();
        }

        public void AddEnchantment(EnchantmentInstance enchantment)
        {
            enchantments ??= new List<EnchantmentInstance>();
            enchantments.Add(enchantment);
        }

        /// <summary>Raises the upgrade level. Cost and limits are enforced by the upgrade system.</summary>
        public void SetUpgradeLevel(int level) => upgradeLevel = Mathf.Max(0, level);

        /// <summary>
        /// Repairs an instance loaded from an older save that has no GUID. Called by the save
        /// system, so a missing ID can never make two items collide.
        /// </summary>
        public void EnsureInstanceId()
        {
            if (string.IsNullOrEmpty(instanceId)) instanceId = Guid.NewGuid().ToString("N");
        }

        public override string ToString()
        {
            string shortId = string.IsNullOrEmpty(instanceId)
                ? "------"
                : instanceId[..Mathf.Min(6, instanceId.Length)];

            return $"{templateId} Lv{itemLevel} {rarity}+{upgradeLevel} [{shortId}]";
        }
    }
}
