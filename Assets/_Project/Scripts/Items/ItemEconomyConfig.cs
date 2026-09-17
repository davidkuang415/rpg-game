using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// Every number behind spending gold on gear and selling gear back, in one asset.
    ///
    /// Two things gold is for:
    ///  - UPGRADING: +1, +2, ... on an item, each step multiplying its base stats. This is the
    ///    main gold sink, and it is why gold keeps mattering after the bag is full of gear.
    ///  - SELLING: turning gear you do not want into gold. Legendary and Mythic items also
    ///    return gems, because those are the only rarities rare enough that scrapping one
    ///    should feel like more than pocket change.
    ///
    /// The upgrade multiplier is deliberately applied to BASE stats only - see
    /// EquipmentStatCalculator - so it can never scale enchantment values (Phase 12+).
    /// </summary>
    [CreateAssetMenu(fileName = "ItemEconomyConfig", menuName = "RPG/Items/Item Economy Config")]
    public class ItemEconomyConfig : ScriptableObject
    {
        [Header("Upgrading (paid in gold)")]
        [Tooltip("Highest upgrade level any item can reach.")]
        [SerializeField, Min(1)] private int maxUpgradeLevel = 10;

        [Tooltip("Added to every base stat per upgrade level. 0.06 = +6% per level, so +10 is +60%.")]
        [SerializeField, Min(0f)] private float statBonusPerUpgrade = 0.06f;

        [Tooltip("Gold for the first upgrade of a level 1 Common item.")]
        [SerializeField, Min(0)] private int baseUpgradeCost = 25;

        [Tooltip("Added to the cost for every item level above 1.")]
        [SerializeField, Min(0f)] private float upgradeCostPerItemLevel = 6f;

        [Tooltip("Cost multiplier per upgrade level already on the item. 1.35 = each step is 35% dearer.")]
        [SerializeField, Min(1f)] private float upgradeCostGrowth = 1.35f;

        [Tooltip("Cost multiplier per rarity tier above Common. 1.25 = a Legendary (tier 4) costs 1.25^4.")]
        [SerializeField, Min(1f)] private float upgradeCostPerRarityTier = 1.25f;

        [Header("Selling (paid out in gold)")]
        [Tooltip("Sell value multiplier per item level above 1, on top of the item's Base Value.")]
        [SerializeField, Min(0f)] private float sellValuePerItemLevel = 0.12f;

        [Tooltip("Fraction of gold spent on upgrades that comes back when the item is sold.")]
        [SerializeField, Range(0f, 1f)] private float upgradeRefundFraction = 0.5f;

        [Header("Selling (gems, high rarities only)")]
        [Tooltip("Lowest rarity that also pays gems when sold.")]
        [SerializeField] private Rarity gemRarityThreshold = Rarity.Legendary;

        [Tooltip("Gems paid for an item of exactly the threshold rarity.")]
        [SerializeField, Min(0)] private int gemsAtThreshold = 5;

        [Tooltip("Gems paid per rarity tier above the threshold, added to the amount above.")]
        [SerializeField, Min(0)] private int gemsPerTierAboveThreshold = 10;

        public int MaxUpgradeLevel => maxUpgradeLevel;
        public float StatBonusPerUpgrade => statBonusPerUpgrade;
        public Rarity GemRarityThreshold => gemRarityThreshold;

        /// <summary>Multiplier applied to an item's base stats at a given upgrade level. +0 = 1.</summary>
        public float GetUpgradeMultiplier(int upgradeLevel) =>
            1f + statBonusPerUpgrade * Mathf.Max(0, upgradeLevel);

        /// <summary>Gold to go from the item's CURRENT upgrade level to the next. -1 when maxed.</summary>
        public int GetUpgradeCost(EquipmentInstance item)
        {
            if (item == null || item.UpgradeLevel >= maxUpgradeLevel) return -1;
            return GetUpgradeCost(item.ItemLevel, item.Rarity, item.UpgradeLevel);
        }

        public int GetUpgradeCost(int itemLevel, Rarity rarity, int currentUpgradeLevel)
        {
            float cost = baseUpgradeCost + upgradeCostPerItemLevel * Mathf.Max(0, itemLevel - 1);
            cost *= Mathf.Pow(upgradeCostGrowth, Mathf.Max(0, currentUpgradeLevel));
            cost *= Mathf.Pow(upgradeCostPerRarityTier, (int)rarity);
            return Mathf.Max(1, Mathf.RoundToInt(cost));
        }

        /// <summary>Every gold coin it took to bring this item from +0 to where it is now.</summary>
        public int GetTotalUpgradeSpend(EquipmentInstance item)
        {
            if (item == null) return 0;

            int total = 0;
            for (int level = 0; level < item.UpgradeLevel; level++)
            {
                total += GetUpgradeCost(item.ItemLevel, item.Rarity, level);
            }
            return total;
        }

        /// <summary>Gold received for selling. Base value, scaled by level and rarity, plus a partial upgrade refund.</summary>
        public int GetSellGold(EquipmentInstance item, ItemDefinition definition, RarityTable rarityTable)
        {
            if (item == null) return 0;

            float baseValue = definition != null ? definition.BaseValue : 10f;
            float levelScale = 1f + sellValuePerItemLevel * Mathf.Max(0, item.ItemLevel - 1);

            RarityTable.RarityEntry entry = rarityTable != null ? rarityTable.Get(item.Rarity) : null;
            float rarityScale = entry != null ? entry.RecycleValueMultiplier : 1f;

            float gold = baseValue * levelScale * rarityScale;
            gold += GetTotalUpgradeSpend(item) * upgradeRefundFraction;

            return Mathf.Max(1, Mathf.RoundToInt(gold));
        }

        /// <summary>Gems received for selling. Zero below the threshold rarity.</summary>
        public int GetSellGems(EquipmentInstance item)
        {
            if (item == null || item.Rarity < gemRarityThreshold) return 0;

            int tiersAbove = (int)item.Rarity - (int)gemRarityThreshold;
            return gemsAtThreshold + gemsPerTierAboveThreshold * tiersAbove;
        }
    }
}
