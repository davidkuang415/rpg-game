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
    /// Tap an item to SELECT it. The details box shows what it does, and three buttons act
    /// on it: EQUIP, UPGRADE (gold) and SELL (gold, plus gems for Legendary and up). Tapping
    /// the same item again equips it, so the old one-tap flow still works for anyone used to
    /// it. What is currently equipped lives one swipe to the left, on the gear page.
    ///
    /// Selling is irreversible, so it is a two-tap action: the first tap arms the button
    /// ("SURE?"), the second sells. Picking anything else disarms it.
    ///
    /// Presentation only. Every rule (slot, class, capacity, prices) lives in
    /// EquipmentManager, InventoryManager and ItemEconomy; this page just reports what they
    /// decided, and rebuilds from scratch on every change so it can never drift out of sync.
    /// </summary>
    public class InventoryPanel : HubPage
    {
        [Header("Data")]
        [SerializeField] private InventoryManager inventory;
        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private RarityTable rarityTable;
        [SerializeField] private CurrencyWallet wallet;

        [Tooltip("Optional. Without it the upgrade and sell buttons stay hidden.")]
        [SerializeField] private ItemEconomy economy;

        [Header("Bag")]
        [SerializeField] private RectTransform bagContainer;
        [SerializeField] private Button bagButtonTemplate;

        [Header("Info")]
        [SerializeField] private Text detailsLabel;
        [SerializeField] private Text headerLabel;
        [SerializeField] private Button expandButton;
        [SerializeField] private Text expandLabel;

        [Header("Actions (optional)")]
        [SerializeField] private Button equipButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button sellButton;

        [Header("Style")]
        [SerializeField] private Color emptySlotColor = new Color(0.16f, 0.17f, 0.2f);

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly StatBlock _scratch = new StatBlock();
        private readonly StringBuilder _builder = new StringBuilder(256);

        private string _message = "Tap an item to see what it does.";
        private EquipmentInstance _selected;
        private bool _sellArmed;

        private void Awake()
        {
            if (bagButtonTemplate != null) bagButtonTemplate.gameObject.SetActive(false);
            if (expandButton != null) expandButton.onClick.AddListener(OnExpandClicked);
            if (equipButton != null) equipButton.onClick.AddListener(OnEquipClicked);
            if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeClicked);
            if (sellButton != null) sellButton.onClick.AddListener(OnSellClicked);
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged += Refresh;
                inventory.CapacityChanged += OnCapacityChanged;
            }
            if (equipment != null) equipment.EquipmentChanged += OnEquipmentChanged;
            if (wallet != null) wallet.CurrencyChanged += OnCurrencyChanged;
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= Refresh;
                inventory.CapacityChanged -= OnCapacityChanged;
            }
            if (equipment != null) equipment.EquipmentChanged -= OnEquipmentChanged;
            if (wallet != null) wallet.CurrencyChanged -= OnCurrencyChanged;
        }

        private void OnCapacityChanged(int capacity) => Refresh();
        private void OnEquipmentChanged(EquipmentSlot slot, EquipmentInstance item) => Refresh();
        private void OnCurrencyChanged(CurrencyType currency, int amount, int delta) => Refresh();

        protected override void BuildContent()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Destroy(_spawned[i]);
            }
            _spawned.Clear();

            // A selection that has left the bag (equipped, sold) must not linger as a ghost.
            if (_selected != null && (inventory == null || !inventory.Contains(_selected)))
            {
                _selected = null;
                _sellArmed = false;
            }

            BuildBag();
            BuildHeader();
            BuildActions();

            if (detailsLabel != null) detailsLabel.text = _message;
        }

        // ------------------------------------------------------------------ bag grid

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

                ItemTilePainter.SetSelected(button, item == _selected);

                EquipmentInstance captured = item;
                button.onClick.AddListener(() => OnBagItemTapped(captured));
            }
        }

        private void OnBagItemTapped(EquipmentInstance item)
        {
            // Second tap on the selected item equips it - the one-tap flow from before.
            if (item == _selected && item != null)
            {
                Equip(item);
                return;
            }

            _selected = item;
            _sellArmed = false;
            _message = DescribeSelected();
            Refresh();
        }

        // ------------------------------------------------------------------ actions

        private void BuildActions()
        {
            bool hasSelection = _selected != null;
            bool hasEconomy = economy != null && economy.Config != null;

            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(true);
                equipButton.interactable = hasSelection;
                SetLabel(equipButton, "EQUIP");
            }

            if (upgradeButton != null)
            {
                upgradeButton.gameObject.SetActive(hasEconomy);
                if (hasEconomy)
                {
                    int cost = hasSelection ? economy.GetUpgradeCost(_selected) : -1;
                    bool maxed = hasSelection && economy.IsMaxUpgrade(_selected);

                    upgradeButton.interactable = hasSelection && !maxed &&
                                                 wallet != null && wallet.CanAfford(CurrencyType.Gold, cost);

                    SetLabel(upgradeButton, !hasSelection ? "UPGRADE"
                        : maxed ? "UPGRADE\nMAX"
                        : $"UPGRADE\n{cost} gold");
                }
            }

            if (sellButton != null)
            {
                sellButton.gameObject.SetActive(hasEconomy);
                if (hasEconomy)
                {
                    sellButton.interactable = hasSelection;

                    if (!hasSelection) SetLabel(sellButton, "SELL");
                    else if (_sellArmed) SetLabel(sellButton, "SURE?\ntap again");
                    else
                    {
                        int gold = economy.GetSellGold(_selected);
                        int gems = economy.GetSellGems(_selected);
                        SetLabel(sellButton, gems > 0
                            ? $"SELL\n{gold} gold + {gems} gems"
                            : $"SELL\n{gold} gold");
                    }
                }
            }
        }

        private void OnEquipClicked()
        {
            if (_selected != null) Equip(_selected);
        }

        private void Equip(EquipmentInstance item)
        {
            string name = ItemTilePainter.DisplayName(item, itemRegistry, rarityTable);
            string stats = DescribeStats(item);

            EquipResult result = equipment != null ? equipment.TryEquip(item) : EquipResult.UnknownItem;

            _message = result == EquipResult.Success
                ? $"Equipped {name}.\n\n{stats}"
                : $"Cannot equip {name}:\n{ItemTilePainter.Describe(result)}\n\n{stats}";

            _sellArmed = false;
            Refresh();
        }

        private void OnUpgradeClicked()
        {
            if (_selected == null || economy == null) return;

            EquipmentInstance item = _selected;
            string name = ItemTilePainter.DisplayName(item, itemRegistry, rarityTable);
            int cost = economy.GetUpgradeCost(item);

            UpgradeResult result = economy.TryUpgrade(item);

            _message = result == UpgradeResult.Success
                ? $"Upgraded to +{item.UpgradeLevel} for {cost} gold.\n\n{DescribeStats(item)}"
                : $"Cannot upgrade {name}: {ItemEconomy.Describe(result)}\n\n{DescribeStats(item)}";

            _sellArmed = false;
            Refresh();
        }

        private void OnSellClicked()
        {
            if (_selected == null || economy == null) return;

            if (!_sellArmed)
            {
                _sellArmed = true;
                _message = $"Sell {ItemTilePainter.DisplayName(_selected, itemRegistry, rarityTable)}?\n" +
                           "This cannot be undone. Tap SELL again to confirm.";
                Refresh();
                return;
            }

            EquipmentInstance item = _selected;
            string name = ItemTilePainter.DisplayName(item, itemRegistry, rarityTable);

            SellResult result = economy.TrySell(item, out int gold, out int gems);

            if (result == SellResult.Success)
            {
                _message = gems > 0
                    ? $"Sold {name} for {gold} gold and {gems} gems."
                    : $"Sold {name} for {gold} gold.";
                _selected = null;
            }
            else
            {
                _message = $"Cannot sell {name}: {ItemEconomy.Describe(result)}";
            }

            _sellArmed = false;
            Refresh();
        }

        // ------------------------------------------------------------------ header

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

        // ------------------------------------------------------------------ helpers

        private string DescribeSelected()
        {
            if (_selected == null) return "Tap an item to see what it does.";

            string name = ItemTilePainter.DisplayName(_selected, itemRegistry, rarityTable);
            return $"{name}\n\n{DescribeStats(_selected)}";
        }

        private string DescribeStats(EquipmentInstance item) =>
            ItemTilePainter.DescribeStats(item, itemRegistry, rarityTable, _scratch, _builder,
                economy != null ? economy.Config : null);

        private static void SetLabel(Button button, string text)
        {
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }
    }
}
