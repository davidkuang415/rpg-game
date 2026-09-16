using System.Collections.Generic;
using UnityEngine;

namespace RPG.Items
{
    /// <summary>
    /// Every item template in the game, and the lookup from a saved template ID back to its asset.
    ///
    /// This is what makes the save format durable: a save file stores "iron_sword", and this
    /// turns it back into an asset on load. Without it, saves would hold direct ScriptableObject
    /// references and break the moment an asset moved.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemRegistry", menuName = "RPG/Items/Item Registry")]
    public class ItemRegistry : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        private Dictionary<string, ItemDefinition> _lookup;

        public IReadOnlyList<ItemDefinition> Items => items;

        public ItemDefinition GetById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            // Built once and cached: item lookups happen per drop, per inventory redraw and
            // per save load, so a linear scan would show up quickly.
            if (_lookup == null || _lookup.Count != items.Count) BuildLookup();

            return _lookup.TryGetValue(itemId, out ItemDefinition definition) ? definition : null;
        }

        /// <summary>Resolves the template an instance was made from.</summary>
        public ItemDefinition GetDefinition(EquipmentInstance instance) =>
            instance != null ? GetById(instance.TemplateId) : null;

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, ItemDefinition>(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition item = items[i];
                if (item == null || string.IsNullOrEmpty(item.ItemId)) continue;

                if (!_lookup.TryAdd(item.ItemId, item))
                {
                    Debug.LogError($"ItemRegistry '{name}': duplicate item id '{item.ItemId}'.", this);
                }
            }
        }

        private void OnValidate() => _lookup = null;
    }
}
