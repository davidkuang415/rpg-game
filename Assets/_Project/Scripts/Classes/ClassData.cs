using UnityEngine;
using RPG.Items;
using RPG.Stats;

namespace RPG.Classes
{
    /// <summary>
    /// A playable class definition: starting stats, weapon restriction and presentation.
    ///
    /// This is static configuration, so it is a ScriptableObject. Adding a third class later
    /// means creating another asset and dropping it in the ClassRegistry - no code changes,
    /// which is why nothing anywhere else says the words "Knight" or "Archer".
    /// </summary>
    [CreateAssetMenu(fileName = "ClassData", menuName = "RPG/Classes/Class Data")]
    public class ClassData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable ID written to save files. NEVER change this once a save exists - " +
                 "renaming the asset is safe, changing this is not.")]
        [SerializeField] private string classId = "knight";

        [SerializeField] private string displayName = "Knight";

        [TextArea(2, 4)]
        [SerializeField] private string description = "Short-range melee fighter. High direct damage, larger health pool.";

        [Header("Equipment Restriction")]
        [Tooltip("The only weapon category this class may equip. Armour is never class-restricted.")]
        [SerializeField] private WeaponType allowedWeaponType = WeaponType.Sword;

        [Header("Base Stats (level 1, no equipment)")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float attack = 12f;
        [SerializeField, Min(0f)] private float defense = 10f;

        [Tooltip("Attacks per second before any weapon is equipped.")]
        [SerializeField, Min(0.05f)] private float attackSpeed = 1f;

        [Tooltip("World units per second.")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;

        [Tooltip("0.05 = 5%. Stored as a fraction everywhere in the project.")]
        [SerializeField, Range(0f, 1f)] private float critChance = 0.05f;

        [Tooltip("1.5 = 150% damage on a critical hit.")]
        [SerializeField, Min(1f)] private float critDamage = 1.5f;

        [Header("Unarmed Fallback (used until a weapon is equipped)")]
        [Tooltip("Reach in world units. The equipped weapon overrides this once equipment exists.")]
        [SerializeField, Min(0f)] private float baseAttackRange = 1.6f;

        [Tooltip("Melee arc width in degrees. Ignored by ranged classes.")]
        [SerializeField, Range(0f, 360f)] private float baseAttackArcDegrees = 120f;

        [Header("Placeholder Presentation")]
        [SerializeField] private Color bodyTint = new Color(0.35f, 0.75f, 1f);
        [SerializeField] private Sprite icon;

        [Tooltip("Optional. A real body sprite for this class. Without it the shared placeholder " +
                 "circle is used, tinted with Body Tint above.")]
        [SerializeField] private Sprite bodySprite;

        public string ClassId => classId;
        public string DisplayName => displayName;
        public string Description => description;
        public WeaponType AllowedWeaponType => allowedWeaponType;
        public float BaseAttackRange => baseAttackRange;
        public float BaseAttackArcDegrees => baseAttackArcDegrees;
        public Color BodyTint => bodyTint;
        public Sprite Icon => icon;
        public Sprite BodySprite => bodySprite;

        /// <summary>Writes this class's level-1 stats into an existing block (no allocation).</summary>
        public void WriteBaseStats(StatBlock target)
        {
            if (target == null) return;

            target.Clear();
            target[StatType.MaxHealth] = maxHealth;
            target[StatType.Attack] = attack;
            target[StatType.Defense] = defense;
            target[StatType.AttackSpeed] = attackSpeed;
            target[StatType.MoveSpeed] = moveSpeed;
            target[StatType.CritChance] = critChance;
            target[StatType.CritDamage] = critDamage;
            // DefensePenetration, DodgeChance and LifeSteal start at 0 and come from gear only.
        }

        /// <summary>Can this class equip that weapon category?</summary>
        public bool CanEquipWeapon(WeaponType weaponType) => weaponType == allowedWeaponType;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(classId))
            {
                Debug.LogWarning($"ClassData '{name}' has an empty Class Id. Save data needs a stable ID.", this);
            }
        }
    }
}
