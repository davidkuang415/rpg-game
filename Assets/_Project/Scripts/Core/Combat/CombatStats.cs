using RPG.Stats;

namespace RPG.Core.Combat
{
    /// <summary>
    /// Anything that can supply stats to the combat system: the player, an enemy, a boss.
    ///
    /// Combat code depends on this interface rather than on PlayerStats, so the exact same
    /// damage, defense, crit and dodge math runs for every character in the game.
    /// </summary>
    public interface ICombatStatProvider
    {
        float GetStat(StatType stat);
    }
}
