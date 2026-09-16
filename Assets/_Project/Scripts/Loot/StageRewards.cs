using System.Collections.Generic;
using RPG.Items;

namespace RPG.Loot
{
    /// <summary>
    /// Everything the player has earned but not yet claimed.
    ///
    /// The design spec requires that dying does NOT cost the XP, gold and loot earned during
    /// the failed attempt, so this deliberately accumulates across attempts and is only emptied
    /// when the rewards are actually claimed - not when a stage starts or restarts.
    /// </summary>
    public class StageRewards
    {
        private readonly List<EquipmentInstance> _items = new List<EquipmentInstance>();

        public float XpEarned { get; private set; }
        public int GoldEarned { get; private set; }
        public int EnemiesDefeated { get; private set; }

        public IReadOnlyList<EquipmentInstance> Items => _items;
        public bool IsEmpty => XpEarned <= 0f && GoldEarned <= 0 && _items.Count == 0;

        public void AddXp(float amount) => XpEarned += amount;
        public void AddGold(int amount) => GoldEarned += amount;
        public void CountKill() => EnemiesDefeated++;

        public void AddItem(EquipmentInstance item)
        {
            if (item != null) _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
            XpEarned = 0f;
            GoldEarned = 0;
            EnemiesDefeated = 0;
        }
    }
}
