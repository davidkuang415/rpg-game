using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// A weapon template. Adds the things only weapons have: a category that gates which class
    /// may equip it, and a reach.
    ///
    /// Reach is deliberately NOT scaled by item level or rarity - a longer sword changes how
    /// the weapon plays, and that should be a property of the weapon itself, not something a
    /// lucky roll grants.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon", menuName = "RPG/Items/Weapon Definition")]
    public class WeaponDefinition : ItemDefinition
    {
        [Header("Weapon")]
        [Tooltip("Knights may equip Swords only; Archers, Bows only.")]
        [SerializeField] private WeaponType weaponType = WeaponType.Sword;

        [Tooltip("Melee reach for swords, maximum projectile range for bows, in world units.")]
        [SerializeField, Min(0.1f)] private float range = 1.6f;

        [Tooltip("Swing arc in degrees. Melee weapons only; ignored by bows.")]
        [SerializeField, Range(0f, 360f)] private float arcDegrees = 120f;

        public WeaponType WeaponType => weaponType;
        public float Range => range;
        public float ArcDegrees => arcDegrees;

        public override EquipmentSlot Slot => EquipmentSlot.Weapon;
    }
}
