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
    /// The bag: everything you own but are not wearing.
    ///
    /// Tap an item to equip it and read the result in the details box. What is currently
    /// equipped lives one swipe to the left, on the gear page - this screen is only the
    /// storage half.
    ///
    /// Presentation only. Every rule (slot, class, capacity) lives in EquipmentManager and
    /// InventoryManager; this page just reports what they decided, and rebuilds from scratch on
    /// every change so it can never drift out of sync with the data.
    /// </summary>
    public class InventoryPanel : HubPage
    {
        [Header("Data")]
        [SerializeField] private InventoryManager inventory;
        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private RarityTable rarityTable;
        [SerializeField] private CurrencyWallet wallet;

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
        private string _message = "Tap an item to equip it.";

        private void Awake()
        {
            if (bagButtonTemplate != null) bagButtonTemplate.gameObject.SetActive(false);
            if (expandButton != null) expandButton.onClick.AddListener(OnExpandClicked);
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

        protected override void BuildContent()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Destroy(_spawned[i]);
            }
            _spawned.Clear();

            BuildBag();
            BuildHeader();

            if (detailsLabel != null) detailsLabel.text = _message;
        }

        private void BuildBag()
        {
            if (inventory == null || bagButtonTemplate == null || bagContainer == null) return;

            IReadOnlyList<EquipmentInstance> items = inventory.Items;

            // Overflow is drawn rather than hidden: an item above capacity (a reward claimed
            // into a full bag) must be visible so the player can deal with it.
            int slotsToDraw = Mathf.Max(inventory.Capacity, items.Count);

            for (int i = 0; i < slotsToDraw; i++)
            {
                EquipmentInstance item = i < items.Count ? items[i] : null;

                Button button = Instantiate(bagButtonTemplate, bagContainer);
                button.gameObject.SetActive(true);
                button.gameObject.name = item != null ? $"Bag_{i}" : $"Bag_{i}_Empty";
                _spawned.Add(button.gameObject);

                ItemTilePainter.Paint(button, item, itemRegistry, rarityTable, emptySlotColor,
                    i >= inventory.Capacity ? "OVER" : "-");
                button.interactable = item != null;

                if (item == null) continue;

                EquipmentInstance captured = item;
                button.onClick.AddListener(() => OnBagItemTapped(captured));
            }
        }

        private void OnBagItemTapped(EquipmentInstance item)
        {
            string name = ItemTilePainter.DisplayName(item, itemRegistry, rarityTable);
            string stats = ItemTilePainter.DescribeStats(item, itemRegistry, rarityTable, _scratch, _builder);

            EquipResult result = equipment != null ? equipment.TryEquip(item) : EquipResult.UnknownItem;

            _message = result == EquipResult.Success
                ? $"Equipped {name}.\n\n{stats}"
                : $"Cannot equip {name}:\n{ItemTilePainter.Describe(result)}\n\n{stats}";

            Refresh();
        }

        private void BuildHeader()
        {
            if (headerLabel != null && inventory != null)
            {
                string gold = wallet != null ? $"     Gold {wallet.Gold}     Gems {wallet.Gems}" : string.Empty;
                headerLabel.text = $"{inventory.Count} / {inventory.Capacity}{gold}";
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

            _message = inventory.TryExpand()
                ? $"Bag expanded to {inventory.Capacity} slots."
                : "Not enough gems.";

            Refresh();
        }
    }
}
