using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Economy;
using RPG.Items;

namespace RPG.Inventory
{
    /// <summary>
    /// The bag: unequipped equipment the player is carrying.
    ///
    /// Capacity rules from the design spec:
    ///  - equipped gear does NOT count (it lives in EquipmentManager, not here)
    ///  - currency and, later, materials never take slots
    ///  - capacity grows by spending Gems, using InventoryConfig's pricing
    ///
    /// The bag may temporarily hold more than its capacity when the GAME moves an item into it
    /// (a weapon forced off on class change). Only player-driven additions are blocked when
    /// full, so nothing the player owns can ever be silently destroyed by a capacity check.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        [SerializeField] private InventoryConfig config;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private bool logChanges;

        private readonly List<EquipmentInstance> _items = new List<EquipmentInstance>();

        public IReadOnlyList<EquipmentInstance> Items => _items;
        public int Count => _items.Count;
        public int Capacity { get; private set; }
        public int ExpansionsBought { get; private set; }

        public bool IsFull => _items.Count >= Capacity;
        public int SlotsPerExpansion => config != null ? config.SlotsPerExpansion : 0;
        public int FreeSlots => Mathf.Max(0, Capacity - _items.Count);

        /// <summary>Raised on any add or remove. UI rebuilds from Items rather than tracking deltas.</summary>
        public event Action InventoryChanged;

        public event Action<int> CapacityChanged;

        private void Awake()
        {
            Capacity = config != null ? config.InitialCapacity : 10;
        }

        /// <summary>Adds if there is room. Returns false, unchanged, when full.</summary>
        public bool TryAdd(EquipmentInstance item)
        {
            if (item == null || IsFull) return false;
            AddInternal(item);
            return true;
        }

        /// <summary>
        /// Adds regardless of capacity. For system moves only (class-change unequips, save
        /// loading) - never for pickups, claims or purchases.
        /// </summary>
        public void ForceAdd(EquipmentInstance item)
        {
            if (item == null) return;
            AddInternal(item);
        }

        public bool Remove(EquipmentInstance item)
        {
            if (item == null || !_items.Remove(item)) return false;

            if (logChanges) Debug.Log($"[Inventory] Removed {item}", this);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool Contains(EquipmentInstance item) => item != null && _items.Contains(item);

        public EquipmentInstance FindByInstanceId(string instanceId)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].InstanceId == instanceId) return _items[i];
            }
            return null;
        }

        /// <summary>Gem cost of the next expansion, or -1 if already at max capacity.</summary>
        public int NextExpansionCost
        {
            get
            {
                if (config == null || Capacity >= config.MaxCapacity) return -1;
                return config.GetExpansionCost(ExpansionsBought);
            }
        }

        /// <summary>Buys one expansion with Gems. Returns false if unaffordable or at max.</summary>
        public bool TryExpand()
        {
            int cost = NextExpansionCost;
            if (cost < 0 || wallet == null) return false;
            if (!wallet.TrySpend(CurrencyType.Gems, cost)) return false;

            Capacity = Mathf.Min(config.MaxCapacity, Capacity + config.SlotsPerExpansion);
            ExpansionsBought++;

            if (logChanges) Debug.Log($"[Inventory] Expanded to {Capacity} slots for {cost} gems.", this);
            CapacityChanged?.Invoke(Capacity);
            return true;
        }

        /// <summary>Used by the save system on load, and by debug tools.</summary>
        public void SetCapacity(int capacity, int expansionsBought)
        {
            Capacity = Mathf.Max(1, capacity);
            ExpansionsBought = Mathf.Max(0, expansionsBought);
            CapacityChanged?.Invoke(Capacity);
        }

        /// <summary>Replaces the whole contents. Used by the save system on load.</summary>
        public void SetItems(IEnumerable<EquipmentInstance> items)
        {
            _items.Clear();
            if (items != null) _items.AddRange(items);
            InventoryChanged?.Invoke();
        }

        /// <summary>Debug tool support.</summary>
        public void ClearAll()
        {
            _items.Clear();
            InventoryChanged?.Invoke();
        }

        private void AddInternal(EquipmentInstance item)
        {
            _items.Add(item);
            if (logChanges) Debug.Log($"[Inventory] Added {item} ({_items.Count}/{Capacity})", this);
            InventoryChanged?.Invoke();
        }
    }
}
