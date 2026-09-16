using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RPG.Economy;
using RPG.Inventory;
using RPG.Items;
using RPG.Stats;

namespace RPG.UI
{
    /// <summary>
    /// The bag and equipment screen.
    ///
    /// Interaction is deliberately minimal for the MVP: tap a bag item to equip it, tap an
    /// equipped slot to unequip it, read the result in the details box. Everything is rebuilt
    /// from the inventory and equipment managers on every change, so the screen can never
    /// drift out of sync with the data.
    ///
    /// Presentation only - every rule (slot, class, capacity) lives in EquipmentManager and
    /// InventoryManager, and this panel just reports what they decided.
    /// </summary>
    public class InventoryPanel : ModalPanel
    {
        [Header("Data")]
        [SerializeField] private InventoryManager inventory;
        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private RarityTable rarityTable;
        [SerializeField] private CurrencyWallet wallet;

        [Header("Equipped")]
        [SerializeField] private RectTransform slotContainer;
        [SerializeField] private Button slotButtonTemplate;

        [Header("Bag")]
        [SerializeField] private RectTransform bagContainer;
        [SerializeField] private Button bagButtonTemplate;

        [Header("Info")]
        [SerializeField] private Text detailsLabel;
        [SerializeField] private Text headerLabel;
        [SerializeField] private Button expandButton;
        [SerializeField] private Text expandLabel;

        [Header("Style")]
        [SerializeField] private Color emptySlotColor = new Color(0.16f, 0.17f, 0.2f);

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly StatBlock _scratch = new StatBlock();
        private readonly StringBuilder _builder = new StringBuilder(256);
        private string _lastMessage = string.Empty;

        protected override void Awake()
        {
            base.Awake();
            if (slotButtonTemplate != null) slotButtonTemplate.gameObject.SetActive(false);
            if (bagButtonTemplate != null) bagButtonTemplate.gameObject.SetActive(false);
            if (expandButton != null) expandButton.onClick.AddListener(OnExpandClicked);
            Hide();
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged += Refresh;
                inventory.CapacityChanged += OnCapacityChanged;
            }
            if (equipment != null) equipment.EquipmentChanged += OnEquipmentChanged;
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= Refresh;
                inventory.CapacityChanged -= OnCapacityChanged;
            }
            if (equipment != null) equipment.EquipmentChanged -= OnEquipmentChanged;
        }

        private void OnCapacityChanged(int capacity) => Refresh();
        private void OnEquipmentChanged(EquipmentSlot slot, EquipmentInstance item) => Refresh();

        private void Refresh()
        {
            if (IsOpen) BuildContent();
        }

        protected override void BuildContent()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Destroy(_spawned[i]);
            }
            _spawned.Clear();

            BuildEquippedSlots();
            BuildBag();
            BuildHeader();

            if (detailsLabel != null) detailsLabel.text = _lastMessage;
        }

        // ------------------------------------------------------------------ equipped

        private void BuildEquippedSlots()
        {
            if (equipment == null || slotButtonTemplate == null || slotContainer == null) return;

            IReadOnlyList<EquipmentSlot> slots = equipment.SupportedSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                EquipmentSlot slot = slots[i];
                EquipmentInstance item = equipment.GetEquipped(slot);

                Button button = Instantiate(slotButtonTemplate, slotContainer);
                button.gameObject.SetActive(true);
                button.gameObject.name = $"Slot_{slot}";
                _spawned.Add(button.gameObject);

                Paint(button, item, $"{slot}\n(empty)");

                EquipmentSlot captured = slot;
                button.onClick.AddListener(() => OnSlotTapped(captured));
            }
        }

        private void OnSlotTapped(EquipmentSlot slot)
        {
            EquipmentInstance item = equipment.GetEquipped(slot);
            if (item == null)
            {
                _lastMessage = $"{slot}: nothing equipped.";
                Refresh();
                return;
            }

            EquipResult result = equipment.TryUnequip(slot);
            _lastMessage = result == EquipResult.Success
                ? $"Unequipped {DisplayName(item)}."
                : $"Cannot unequip: {Describe(result)}";

            Refresh();
        }

        // ------------------------------------------------------------------ bag

        private void BuildBag()
        {
            if (inventory == null || bagButtonTemplate == null || bagContainer == null) return;

            IReadOnlyList<EquipmentInstance> items = inventory.Items;
            int slotsToDraw = Mathf.Max(inventory.Capacity, items.Count);

            for (int i = 0; i < slotsToDraw; i++)
            {
                EquipmentInstance item = i < items.Count ? items[i] : null;

                Button button = Instantiate(bagButtonTemplate, bagContainer);
                button.gameObject.SetActive(true);
                button.gameObject.name = item != null ? $"Bag_{i}" : $"Bag_{i}_Empty";
                _spawned.Add(button.gameObject);

                Paint(button, item, i >= inventory.Capacity ? "OVER" : "-");
                button.interactable = item != null;

                if (item == null) continue;

                EquipmentInstance captured = item;
                button.onClick.AddListener(() => OnBagItemTapped(captured));
            }
        }

        private void OnBagItemTapped(EquipmentInstance item)
        {
            EquipResult result = equipment != null ? equipment.TryEquip(item) : EquipResult.UnknownItem;

            _lastMessage = result == EquipResult.Success
                ? $"Equipped {DisplayName(item)}.\n\n{DescribeStats(item)}"
                : $"Cannot equip {DisplayName(item)}:\n{Describe(result)}\n\n{DescribeStats(item)}";

            Refresh();
        }

        // ------------------------------------------------------------------ header and expansion

        private void BuildHeader()
        {
            if (headerLabel != null && inventory != null)
            {
                string gold = wallet != null ? $"   Gold {wallet.Gold}   Gems {wallet.Gems}" : string.Empty;
                headerLabel.text = $"BAG  {inventory.Count} / {inventory.Capacity}{gold}";
            }

            if (expandButton == null || inventory == null) return;

            int cost = inventory.NextExpansionCost;
            bool canExpand = cost >= 0;
            expandButton.interactable = canExpand && wallet != null && wallet.CanAfford(CurrencyType.Gems, cost);

            if (expandLabel != null)
            {
                expandLabel.text = canExpand
                    ? $"+{inventory.SlotsPerExpansion} slots\n{cost} gems"
                    : "Max size";
            }
        }

        private void OnExpandClicked()
        {
            if (inventory == null) return;

            _lastMessage = inventory.TryExpand()
                ? $"Bag expanded to {inventory.Capacity} slots."
                : "Not enough gems.";

            Refresh();
        }

        // ------------------------------------------------------------------ helpers

        private void Paint(Button button, EquipmentInstance item, string emptyText)
        {
            var image = button.GetComponent<Image>();
            var label = button.GetComponentInChildren<Text>();

            if (item == null)
            {
                if (image != null) image.color = emptySlotColor;
                if (label != null) label.text = emptyText;
                return;
            }

            if (image != null && rarityTable != null)
            {
                Color rarityColor = rarityTable.GetColor(item.Rarity);
                image.color = Color.Lerp(emptySlotColor, rarityColor, 0.45f);
            }

            if (label != null)
            {
                ItemDefinition definition = itemRegistry != null ? itemRegistry.GetDefinition(item) : null;
                string itemName = definition != null ? definition.DisplayName : item.TemplateId;
                label.text = $"{itemName}\nLv {item.ItemLevel}  {item.Rarity}";
            }
        }

        private string DisplayName(EquipmentInstance item)
        {
            ItemDefinition definition = itemRegistry != null ? itemRegistry.GetDefinition(item) : null;
            return EquipmentStatCalculator.GetDisplayName(item, definition, rarityTable);
        }

        private string DescribeStats(EquipmentInstance item)
        {
            ItemDefinition definition = itemRegistry != null ? itemRegistry.GetDefinition(item) : null;
            if (definition == null) return "(unknown item)";

            EquipmentStatCalculator.ComputeStats(item, definition, rarityTable, _scratch);

            _builder.Clear();
            for (int i = 0; i < StatTypeInfo.Count; i++)
            {
                var stat = (StatType)i;
                float value = _scratch[stat];
                if (value == 0f) continue;

                _builder.Append(StatTypeInfo.DisplayName(stat)).Append(": ")
                        .Append(value > 0f ? "+" : string.Empty)
                        .Append(StatTypeInfo.Format(stat, value)).Append('\n');
            }

            if (definition is WeaponDefinition weapon)
            {
                _builder.Append("Range: ").Append(weapon.Range.ToString("0.0"));
                if (weapon.ArcDegrees > 0f) _builder.Append("   Arc: ").Append(weapon.ArcDegrees.ToString("0"));
            }

            return _builder.ToString();
        }

        private static string Describe(EquipResult result) => result switch
        {
            EquipResult.WrongClass => "your class cannot use this weapon type.",
            EquipResult.SlotNotSupported => "that slot is not available yet.",
            EquipResult.InventoryFull => "the bag is full.",
            EquipResult.NotInInventory => "item is not in the bag.",
            EquipResult.NothingEquipped => "nothing equipped there.",
            EquipResult.UnknownItem => "unknown item.",
            _ => result.ToString()
        };
    }
}
