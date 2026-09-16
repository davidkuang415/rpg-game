using UnityEngine;

namespace RPG.Inventory
{
    /// <summary>
    /// Inventory sizing and expansion economics, kept as data so they can be tuned without
    /// touching the inventory code.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryConfig", menuName = "RPG/Inventory/Inventory Config")]
    public class InventoryConfig : ScriptableObject
    {
        [Header("Capacity")]
        [Tooltip("Slots a new player starts with. Equipped gear never counts against this.")]
        [SerializeField, Min(1)] private int initialCapacity = 10;

        [Tooltip("Hard ceiling, so expansion cannot grow without bound.")]
        [SerializeField, Min(1)] private int maxCapacity = 60;

        [Header("Expansion (paid in Gems)")]
        [Tooltip("Slots added per purchase.")]
        [SerializeField, Min(1)] private int slotsPerExpansion = 5;

        [Tooltip("Gem cost of the first expansion.")]
        [SerializeField, Min(0)] private int baseExpansionCost = 50;

        [Tooltip("Added to the cost for every expansion already bought, so each one costs more.")]
        [SerializeField, Min(0)] private int costIncreasePerExpansion = 25;

        public int InitialCapacity => initialCapacity;
        public int MaxCapacity => maxCapacity;
        public int SlotsPerExpansion => slotsPerExpansion;

        /// <summary>Cost of the next expansion, given how many have been bought already.</summary>
        public int GetExpansionCost(int expansionsBought) =>
            baseExpansionCost + costIncreasePerExpansion * Mathf.Max(0, expansionsBought);
    }
}
