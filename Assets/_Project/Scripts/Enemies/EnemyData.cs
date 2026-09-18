using UnityEngine;

namespace RPG.Enemies
{
    /// <summary>
    /// Definition of one enemy type. Static configuration, so it is a ScriptableObject.
    ///
    /// These are LEVEL 1 values. Actual stats are these numbers run through the difficulty
    /// curve for the enemy's level, which is why there is no "hard version" of an enemy asset:
    /// a level 40 Grunt and a level 1 Grunt share this one definition.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "RPG/Enemies/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable ID for save data and analytics. Do not change once used.")]
        [SerializeField] private string enemyId = "melee_grunt";

        [SerializeField] private string displayName = "Grunt";
        [SerializeField] private EnemyArchetype archetype = EnemyArchetype.Melee;

        [Tooltip("A boss gets the big health bar at the top of the screen and its own intro " +
                 "banner. Behaviour still comes from the prefab's components.")]
        [SerializeField] private bool isBoss;

        [Header("Base Stats (level 1)")]
        [SerializeField, Min(1f)] private float maxHealth = 40f;
        [SerializeField, Min(0f)] private float attack = 8f;
        [SerializeField, Min(0f)] private float defense = 0f;

        [Tooltip("World units per second.")]
        [SerializeField, Min(0f)] private float moveSpeed = 2.6f;

        [Tooltip("Attacks per second, same units as the player's attack speed.")]
        [SerializeField, Min(0.05f)] private float attackSpeed = 0.8f;

        [Header("Behaviour Ranges")]
        [Tooltip("How close the enemy must be to attack, in world units.")]
        [SerializeField, Min(0.1f)] private float attackRange = 1.2f;

        [Tooltip("How far the enemy can notice the player. Line of sight is still required.")]
        [SerializeField, Min(0.1f)] private float detectionRange = 9f;

        [Tooltip("How long the enemy keeps hunting the player's last known position after " +
                 "losing sight of them.")]
        [SerializeField, Min(0f)] private float memorySeconds = 3f;

        [Tooltip("Telegraph time before the hit lands. Gives the player a window to react, " +
                 "which is what keeps attacks readable rather than unavoidable.")]
        [SerializeField, Min(0f)] private float attackWindupSeconds = 0.35f;

        [Header("Rewards (level 1)")]
        [Tooltip("What this enemy can drop. Leave empty to use the collector's fallback table.")]
        [SerializeField] private RPG.Loot.LootTableData lootTable;

        [SerializeField, Min(0f)] private float xpReward = 10f;
        [SerializeField, Min(0)] private int goldReward = 5;

        [Header("Prefab")]
        [Tooltip("The prefab spawned for this enemy type. Spawn points reference this asset " +
                 "and get the prefab from here, so a room never wires prefabs directly.")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("Placeholder Presentation")]
        [SerializeField] private Color bodyTint = new Color(0.9f, 0.35f, 0.35f);
        [SerializeField, Min(0.1f)] private float bodyScale = 0.9f;

        [Tooltip("Optional. A real body sprite for this enemy type. Without it the shared " +
                 "placeholder circle is used, tinted with Body Tint above.")]
        [SerializeField] private Sprite bodySprite;

        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public EnemyArchetype Archetype => archetype;
        public bool IsBoss => isBoss;

        public float MaxHealth => maxHealth;
        public float Attack => attack;
        public float Defense => defense;
        public float MoveSpeed => moveSpeed;
        public float AttackSpeed => attackSpeed;

        public float AttackRange => attackRange;
        public float DetectionRange => detectionRange;
        public float MemorySeconds => memorySeconds;
        public float AttackWindupSeconds => attackWindupSeconds;

        public RPG.Loot.LootTableData LootTable => lootTable;
        public float XpReward => xpReward;
        public int GoldReward => goldReward;

        public GameObject EnemyPrefab => enemyPrefab;
        public Color BodyTint => bodyTint;
        public float BodyScale => bodyScale;
        public Sprite BodySprite => bodySprite;
    }
}
