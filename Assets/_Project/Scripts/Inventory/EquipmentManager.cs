using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Classes;
using RPG.Items;
using RPG.Player;
using RPG.Stats;

namespace RPG.Inventory
{
    public enum EquipResult
    {
        Success,
        UnknownItem,        // template id not in the registry
        SlotNotSupported,   // e.g. Ring before rings exist
        WrongClass,         // a Knight holding a bow
        NotInInventory,
        NothingEquipped,
        InventoryFull       // unequipping needs a free bag slot
    }

    /// <summary>
    /// What the player is wearing, and how that turns into stats.
    ///
    /// It contributes to the stat pipeline as an IStatModifierSource on the Equipment layer -
    /// the same mechanism levelling uses - so equipment is never a special case in the stat
    /// system. Every equip, unequip and class change simply triggers a recalculation.
    ///
    /// Validation from the design spec is enforced here and nowhere else: slot support, the
    /// weapon-class restriction, item validity, and the inventory-space rule for unequipping.
    /// Save data cannot end up with a Knight holding a bow because there is no other door in.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class EquipmentManager : MonoBehaviour, IStatModifierSource
    {
        [Header("Data")]
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private RarityTable rarityTable;

        [Header("Wiring")]
        [SerializeField] private InventoryManager inventory;

        [Header("Slots")]
        [Tooltip("Slots the game currently uses. Add Ring and Amulet here when those systems ship.")]
        [SerializeField] private List<EquipmentSlot> supportedSlots = new List<EquipmentSlot>
        {
            EquipmentSlot.Weapon, EquipmentSlot.Helmet, EquipmentSlot.Chestplate,
            EquipmentSlot.Leggings, EquipmentSlot.Boots
        };

        [SerializeField] private bool logChanges;

        private readonly Dictionary<EquipmentSlot, EquipmentInstance> _equipped =
            new Dictionary<EquipmentSlot, EquipmentInstance>();

        // One cached stat block per slot; rebuilt only when that slot changes.
        private readonly Dictionary<EquipmentSlot, StatBlock> _slotStats =
            new Dictionary<EquipmentSlot, StatBlock>();

        private PlayerStats _stats;

        public IReadOnlyList<EquipmentSlot> SupportedSlots => supportedSlots;

        /// <summary>Raised after any slot changes: (slot, newItemOrNull).</summary>
        public event Action<EquipmentSlot, EquipmentInstance> EquipmentChanged;

        private void Awake() => _stats = GetComponent<PlayerStats>();

        private void OnEnable()
        {
            _stats.RegisterSource(this);
            _stats.ClassChanged += OnClassChanged;
        }

        private void OnDisable()
        {
            _stats.ClassChanged -= OnClassChanged;
            _stats.UnregisterSource(this);
        }

        // ------------------------------------------------------------------ queries

        public EquipmentInstance GetEquipped(EquipmentSlot slot) =>
            _equipped.TryGetValue(slot, out EquipmentInstance item) ? item : null;

        public bool IsSlotSupported(EquipmentSlot slot) => supportedSlots.Contains(slot);

        public ItemDefinition GetDefinition(EquipmentInstance item) =>
            itemRegistry != null ? itemRegistry.GetDefinition(item) : null;

        /// <summary>The equipped weapon's template, or null. Attacks read range and arc from it.</summary>
        public WeaponDefinition EquippedWeapon =>
            GetDefinition(GetEquipped(EquipmentSlot.Weapon)) as WeaponDefinition;

        /// <summary>Why an item could or could not be equipped right now, without doing it.</summary>
        public EquipResult CanEquip(EquipmentInstance item)
        {
            if (item == null) return EquipResult.UnknownItem;

            ItemDefinition definition = GetDefinition(item);
            if (definition == null) return EquipResult.UnknownItem;

            if (!IsSlotSupported(definition.Slot)) return EquipResult.SlotNotSupported;

            if (definition is WeaponDefinition weapon &&
                !IsWeaponAllowedForClass(weapon, _stats.CurrentClass))
            {
                return EquipResult.WrongClass;
            }

            if (inventory != null && !inventory.Contains(item)) return EquipResult.NotInInventory;

            return EquipResult.Success;
        }

        // ------------------------------------------------------------------ commands

        /// <summary>
        /// Moves an item from the bag into its slot. Whatever was in the slot goes back into
        /// the bag - which always fits, because the incoming item just vacated a space.
        /// </summary>
        public EquipResult TryEquip(EquipmentInstance item)
        {
            EquipResult check = CanEquip(item);
            if (check != EquipResult.Success) return check;

            EquipmentSlot slot = GetDefinition(item).Slot;
            EquipmentInstance previous = GetEquipped(slot);

            if (inventory != null) inventory.Remove(item);
            _equipped[slot] = item;
            if (previous != null && inventory != null) inventory.ForceAdd(previous);

            OnSlotChanged(slot);
            if (logChanges) Debug.Log($"[Equipment] Equipped {item} in {slot}.", this);

            return EquipResult.Success;
        }

        /// <summary>Moves a slot's item back to the bag. Refused if the bag is full.</summary>
        public EquipResult TryUnequip(EquipmentSlot slot)
        {
            EquipmentInstance item = GetEquipped(slot);
            if (item == null) return EquipResult.NothingEquipped;
            if (inventory != null && inventory.IsFull) return EquipResult.InventoryFull;

            _equipped.Remove(slot);
            if (inventory != null) inventory.TryAdd(item);

            OnSlotChanged(slot);
            if (logChanges) Debug.Log($"[Equipment] Unequipped {item} from {slot}.", this);

            return EquipResult.Success;
        }

        /// <summary>
        /// Restores saved loadout without validation side effects like bag moves. The save
        /// system validates separately; anything invalid is pushed to the bag instead.
        /// </summary>
        public void SetEquippedDirect(EquipmentSlot slot, EquipmentInstance item)
        {
            if (item == null) _equipped.Remove(slot);
            else _equipped[slot] = item;

            OnSlotChanged(slot);
        }

        // ------------------------------------------------------------------ rules

        public static bool IsWeaponAllowedForClass(WeaponDefinition weapon, ClassData classData)
        {
            if (weapon == null) return false;
            if (classData == null) return true;   // No class yet: nothing to violate.
            return classData.CanEquipWeapon(weapon.WeaponType);
        }

        /// <summary>
        /// Switching class can make the held weapon illegal (Knight -> Archer with a sword).
        /// It is forced into the bag - over capacity if necessary - rather than destroyed or
        /// left silently active.
        /// </summary>
        private void OnClassChanged(ClassData classData)
        {
            EquipmentInstance weapon = GetEquipped(EquipmentSlot.Weapon);
            if (weapon == null) return;

            if (IsWeaponAllowedForClass(GetDefinition(weapon) as WeaponDefinition, classData)) return;

            _equipped.Remove(EquipmentSlot.Weapon);
            if (inventory != null) inventory.ForceAdd(weapon);

            if (logChanges) Debug.Log($"[Equipment] {weapon} is not usable by {classData.DisplayName}; moved to bag.", this);
            OnSlotChanged(EquipmentSlot.Weapon);
        }

        // ------------------------------------------------------------------ stats

        private void OnSlotChanged(EquipmentSlot slot)
        {
            RebuildSlotStats(slot);
            _stats.Recalculate();
            EquipmentChanged?.Invoke(slot, GetEquipped(slot));
        }

        private void RebuildSlotStats(EquipmentSlot slot)
        {
            EquipmentInstance item = GetEquipped(slot);
            if (item == null)
            {
                _slotStats.Remove(slot);
                return;
            }

            if (!_slotStats.TryGetValue(slot, out StatBlock block))
            {
                block = new StatBlock();
                _slotStats[slot] = block;
            }

            // Upgrade multiplier stays 1 until the upgrade system (Phase 11) supplies it.
            EquipmentStatCalculator.ComputeStats(item, GetDefinition(item), rarityTable, block);
        }

        /// <summary>IStatModifierSource: every equipped item's stats, as flat Equipment-layer modifiers.</summary>
        public void CollectModifiers(List<StatModifier> results)
        {
            foreach (KeyValuePair<EquipmentSlot, StatBlock> pair in _slotStats)
            {
                StatBlock block = pair.Value;
                for (int i = 0; i < StatTypeInfo.Count; i++)
                {
                    var stat = (StatType)i;
                    float value = block[stat];
                    if (value != 0f) results.Add(StatModifier.Flat(stat, value, StatLayer.Equipment));
                }
            }
        }

        /// <summary>Every slot with something in it. Used by the save system and the UI.</summary>
        public IEnumerable<KeyValuePair<EquipmentSlot, EquipmentInstance>> AllEquipped => _equipped;
    }
}
