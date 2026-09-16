using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// An armour template.
    ///
    /// Armour is never class-restricted - any class may wear any piece - so unlike weapons
    /// this carries no class or type gate. Which stats a piece provides (HP and DEF, plus
    /// Movement Speed on boots) is authored in its stat list rather than hardcoded per slot,
    /// so a future "heavy boots" piece could trade speed for defence without a code change.
    /// </summary>
    [CreateAssetMenu(fileName = "Armor", menuName = "RPG/Items/Armor Definition")]
    public class ArmorDefinition : ItemDefinition
    {
        [Header("Armour")]
        [SerializeField] private EquipmentSlot slot = EquipmentSlot.Helmet;

        [Header("Sets (used from the Mythic phase)")]
        [Tooltip("Optional set this piece belongs to. Wearing four Mythic pieces of one set " +
                 "will activate its bonus. No set bonuses exist yet.")]
        [SerializeField] private string setId;

        public string SetId => setId;

        public override EquipmentSlot Slot => slot;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (slot == EquipmentSlot.Weapon)
            {
                Debug.LogError($"ArmorDefinition '{name}' is set to the Weapon slot.", this);
            }
        }
    }
}
