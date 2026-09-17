using System;
using UnityEngine;
using RPG.Inventory;
using RPG.Items;

namespace RPG.Economy
{
    public enum UpgradeResult
    {
        Success,
        UnknownItem,
        MaxLevel,
        NotEnoughGold,
        NotOwned          // neither in the bag nor equipped
    }

    public enum SellResult
    {
        Success,
        UnknownItem,
        NotInBag          // equipped items must be taken off first
    }

    /// <summary>
    /// The two things gold buys: upgrading gear, and (in reverse) selling gear for gold.
    ///
    /// All the arithmetic lives in ItemEconomyConfig; this is the transaction. It checks
    /// ownership, moves the currency and mutates the item in one place, so the UI can never
    /// upgrade something for free or sell an item the player is still wearing.
    ///
    /// Selling is bag-only on purpose. Unequipping first is one tap and it means the
    /// "are you sure?" question is asked about something already sitting in the bag, not
    /// about the sword in your hand.
    /// </summary>
    public class ItemEconomy : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ItemEconomyConfig config;
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private RarityTable rarityTable;

        [Header("Wiring")]
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private InventoryManager inventory;
        [SerializeField] private EquipmentManager equipment;

        [SerializeField] private bool logTransactions;

        public ItemEconomyConfig Config => config;

        /// <summary>Raised after a successful upgrade, with the item's new level.</summary>
        public event Action<EquipmentInstance> ItemUpgraded;

        /// <summary>Raised after a sale: (item, goldPaid, gemsPaid).</summary>
        public event Action<EquipmentInstance, int, int> ItemSold;

        // ------------------------------------------------------------------ upgrading

        public int GetUpgradeCost(EquipmentInstance item) =>
            config != null ? config.GetUpgradeCost(item) : -1;

        public bool IsMaxUpgrade(EquipmentInstance item) =>
            config == null || item == null || item.UpgradeLevel >= config.MaxUpgradeLevel;

        public UpgradeResult CanUpgrade(EquipmentInstance item)
        {
            if (item == null || config == null) return UpgradeResult.UnknownItem;
            if (itemRegistry != null && itemRegistry.GetDefinition(item) == null) return UpgradeResult.UnknownItem;
            if (!IsOwned(item)) return UpgradeResult.NotOwned;
            if (item.UpgradeLevel >= config.MaxUpgradeLevel) return UpgradeResult.MaxLevel;

            int cost = config.GetUpgradeCost(item);
            if (wallet == null || !wallet.CanAfford(CurrencyType.Gold, cost)) return UpgradeResult.NotEnoughGold;

            return UpgradeResult.Success;
        }

        /// <summary>Spends gold and raises the item one level. Equipped items update their stats immediately.</summary>
        public UpgradeResult TryUpgrade(EquipmentInstance item)
        {
            UpgradeResult check = CanUpgrade(item);
            if (check != UpgradeResult.Success) return check;

            int cost = config.GetUpgradeCost(item);
            if (!wallet.TrySpend(CurrencyType.Gold, cost)) return UpgradeResult.NotEnoughGold;

            item.SetUpgradeLevel(item.UpgradeLevel + 1);

            // An equipped item's stats are cached per slot; tell the manager so the player's
            // numbers change now rather than the next time the slot is touched.
            if (equipment != null) equipment.NotifyItemChanged(item);

            if (logTransactions) Debug.Log($"[Economy] Upgraded {item} for {cost} gold.", this);
            ItemUpgraded?.Invoke(item);

            return UpgradeResult.Success;
        }

        // ------------------------------------------------------------------ selling

        public int GetSellGold(EquipmentInstance item)
        {
            if (config == null || item == null) return 0;
            ItemDefinition definition = itemRegistry != null ? itemRegistry.GetDefinition(item) : null;
            return config.GetSellGold(item, definition, rarityTable);
        }

        public int GetSellGems(EquipmentInstance item) => config != null ? config.GetSellGems(item) : 0;

        public SellResult CanSell(EquipmentInstance item)
        {
            if (item == null || config == null) return SellResult.UnknownItem;
            if (inventory == null || !inventory.Contains(item)) return SellResult.NotInBag;
            return SellResult.Success;
        }

        /// <summary>Removes the item from the bag and pays out. Irreversible - the UI confirms first.</summary>
        public SellResult TrySell(EquipmentInstance item, out int goldPaid, out int gemsPaid)
        {
            goldPaid = 0;
            gemsPaid = 0;

            SellResult check = CanSell(item);
            if (check != SellResult.Success) return check;

            goldPaid = GetSellGold(item);
            gemsPaid = GetSellGems(item);

            if (!inventory.Remove(item)) return SellResult.NotInBag;

            if (wallet != null)
            {
                wallet.Add(CurrencyType.Gold, goldPaid);
                wallet.Add(CurrencyType.Gems, gemsPaid);
            }

            if (logTransactions)
            {
                Debug.Log($"[Economy] Sold {item} for {goldPaid} gold" +
                          (gemsPaid > 0 ? $" and {gemsPaid} gems." : "."), this);
            }

            ItemSold?.Invoke(item, goldPaid, gemsPaid);
            return SellResult.Success;
        }

        // ------------------------------------------------------------------ helpers

        private bool IsOwned(EquipmentInstance item)
        {
            if (inventory != null && inventory.Contains(item)) return true;
            if (equipment == null) return false;

            foreach (var pair in equipment.AllEquipped)
            {
                if (pair.Value == item) return true;
            }
            return false;
        }

        public static string Describe(UpgradeResult result) => result switch
        {
            UpgradeResult.MaxLevel => "already at max upgrade.",
            UpgradeResult.NotEnoughGold => "not enough gold.",
            UpgradeResult.NotOwned => "you do not own that item.",
            UpgradeResult.UnknownItem => "unknown item.",
            _ => result.ToString()
        };

        public static string Describe(SellResult result) => result switch
        {
            SellResult.NotInBag => "take it off first - only bag items can be sold.",
            SellResult.UnknownItem => "unknown item.",
            _ => result.ToString()
        };
    }
}
