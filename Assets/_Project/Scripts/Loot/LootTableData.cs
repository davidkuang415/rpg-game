using System.Collections.Generic;
using UnityEngine;
using RPG.Items;

namespace RPG.Loot
{
    /// <summary>
    /// What an enemy can drop, and how likely each outcome is.
    ///
    /// Item selection and rarity selection are independent rolls: WHICH item comes from this
    /// table's weighted list, and HOW GOOD it is comes from the rarity weights. That separation
    /// is what lets a common sword drop as a Legendary without needing a separate table entry
    /// for every item-rarity combination.
    /// </summary>
    [CreateAssetMenu(fileName = "LootTable", menuName = "RPG/Loot/Loot Table")]
    public class LootTableData : ScriptableObject
    {
        [Header("Drop Chance")]
        [Tooltip("Chance this table produces anything at all, per kill.")]
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.3f;

        [Tooltip("Always drops, ignoring Drop Chance. For bosses and guaranteed rewards.")]
        [SerializeField] private bool guaranteedDrop;

        [Tooltip("How many items to roll when this table drops. Bosses may roll several.")]
        [SerializeField, Min(1)] private int rollCount = 1;

        [Header("Possible Items")]
        [Tooltip("Candidates, weighted by each item's own Drop Weight.")]
        [SerializeField] private List<ItemDefinition> possibleItems = new List<ItemDefinition>();

        [Header("Item Level")]
        [Tooltip("Item level is the enemy's level plus a random offset in this range. " +
                 "Item level scales with difficulty; rarity does not replace it.")]
        [SerializeField] private int itemLevelOffsetMin;
        [SerializeField] private int itemLevelOffsetMax = 1;

        [Header("Rarity")]
        [Tooltip("Floor for rarity rolls. Boss tables raise this to guarantee Epic or better.")]
        [SerializeField] private Rarity minimumRarity = Rarity.Common;

        [Tooltip("Extra rarity luck per enemy level, so higher-level enemies drop better gear. " +
                 "0.01 = +1% luck weighting per level.")]
        [SerializeField, Min(0f)] private float luckPerEnemyLevel = 0.01f;

        public float DropChance => dropChance;
        public bool GuaranteedDrop => guaranteedDrop;
        public int RollCount => rollCount;
        public IReadOnlyList<ItemDefinition> PossibleItems => possibleItems;
        public Rarity MinimumRarity => minimumRarity;
        public float LuckPerEnemyLevel => luckPerEnemyLevel;

        public int RollItemLevel(int enemyLevel)
        {
            int min = Mathf.Min(itemLevelOffsetMin, itemLevelOffsetMax);
            int max = Mathf.Max(itemLevelOffsetMin, itemLevelOffsetMax);
            return Mathf.Max(1, enemyLevel + Random.Range(min, max + 1));
        }
    }
}
