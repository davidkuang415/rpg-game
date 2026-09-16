namespace RPG.Items
{
    /// <summary>
    /// Where an item is worn.
    ///
    /// Ring and Amulet are declared but not used by the MVP - having them in the enum from the
    /// start means the save format, inventory and equipment UI already understand them, so
    /// adding them later is content work rather than a migration.
    /// </summary>
    public enum EquipmentSlot
    {
        Weapon = 0,
        Helmet = 1,
        Chestplate = 2,
        Leggings = 3,
        Boots = 4,

        // Not used yet. See the design spec's MVP slot list.
        Ring = 5,
        Amulet = 6
    }
}
