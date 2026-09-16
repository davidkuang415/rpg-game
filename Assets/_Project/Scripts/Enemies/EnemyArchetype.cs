namespace RPG.Enemies
{
    /// <summary>
    /// Broad behaviour families. The archetype decides which behaviour components an enemy
    /// prefab carries; the numbers that make one melee enemy different from another live in
    /// EnemyData, not here.
    ///
    /// Tank is intentionally not its own behaviour - it is a melee enemy with tank numbers.
    /// Support/healer enemies are excluded for now, per the design spec.
    /// </summary>
    public enum EnemyArchetype
    {
        Melee = 0,
        Ranged = 1,
        Tank = 2,
        Charger = 3
    }
}
