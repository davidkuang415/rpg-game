namespace RPG.Items
{
    /// <summary>
    /// Weapon categories. A class may only equip its own category (Knight = Sword,
    /// Archer = Bow), which is enforced by data on ClassData rather than by code.
    ///
    /// Armour is deliberately NOT restricted by class, so it has no equivalent enum.
    /// </summary>
    public enum WeaponType
    {
        Sword = 0,
        Bow = 1
    }
}
