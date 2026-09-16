namespace RPG.Items
{
    /// <summary>
    /// Item quality tiers.
    ///
    /// Rarity and Item Level are SEPARATE systems: rarity multiplies an item's stats, item
    /// level determines the base those stats are built from. A level 50 Rare sword is meant to
    /// beat a level 5 Legendary - that is what keeps long-term progression meaningful instead
    /// of making one lucky early drop permanent.
    ///
    /// The enum order is the tier order, and is used as an index into the RarityTable.
    /// </summary>
    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythic = 5
    }
}
