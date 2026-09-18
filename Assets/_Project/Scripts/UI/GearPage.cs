using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RPG.Core.Combat;
using RPG.Economy;
using RPG.Inventory;
using RPG.Items;
using RPG.Player;
using RPG.Progression;
using RPG.Stats;

namespace RPG.UI
{
    /// <summary>
    /// The character sheet: what you are wearing, and what it adds up to.
    ///
    /// This replaces the IMGUI stat overlay that used to be drawn over the arena. Stats are
    /// something you read between fights, so they now live between fights, and the play area
    /// is left clear.
    ///
    /// Every number here is read from the same stat pipeline the combat code uses, never
    /// recomputed locally - a screen that does its own arithmetic is a screen that will one day
    /// disagree with the game.
    /// </summary>
    public class GearPage : HubPage
    {
        [Header("Data")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerLevel playerLevel;
        [SerializeField] private Health playerHealth;
        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private RarityTable rarityTable;
        [SerializeField] private CurrencyWallet wallet;

        [Tooltip("Optional. Without it the upgrade button stays hidden.")]
        [SerializeField] private ItemEconomy economy;

        [Header("Header")]
        [SerializeField] private Text classLabel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Image xpFill;
        [SerializeField] private Text currencyLabel;

        [Header("Equipped")]
        [SerializeField] private RectTransform slotContainer;
        [SerializeField] private Button slotButtonTemplate;

        [Tooltip("Optional shared hover panel showing an item's stats.")]
        [SerializeField] private ItemTooltip tooltip;

        [Header("Stats")]
        [SerializeField] private RectTransform statContainer;
        [SerializeField] private StatRowView statRowTemplate;

        [Header("Info")]
        [SerializeField] private Text detailsLabel;

        [Header("Actions (optional)")]
        [SerializeField] private Button unequipButton;
        [SerializeField] private Button upgradeButton;

        [Header("Style")]
        [SerializeField] private Color emptySlotColor = new Color(0.16f, 0.17f, 0.2f);

        [Tooltip("Value colour for a stat that gear or levels have raised above the class base.")]
        [SerializeField] private Color improvedStatColor = new Color(0.55f, 0.9f, 0.6f);

        [SerializeField] private Color neutralStatColor = Color.white;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly StatBlock _scratch = new StatBlock();
        private readonly StringBuilder _builder = new StringBuilder(256);
        private string _message = "Tap an equipped item to select it.";

        private bool _rebuildQueued;

        // The slot the action buttons refer to. Null = nothing selected.
        private EquipmentSlot? _selectedSlot;

        private void Awake()
        {
            if (unequipButton != null) unequipButton.onClick.AddListener(OnUnequipClicked);
            if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        private void OnEnable()
        {
            if (equipment != null) equipment.EquipmentChanged += OnEquipmentChanged;
            if (playerStats != null) playerStats.StatsChanged += OnStatsChanged;
            if (wallet != null) wallet.CurrencyChanged += OnCurrencyChanged;
        }

        private void OnDisable()
        {
            if (equipment != null) equipment.EquipmentChanged -= OnEquipmentChanged;
            if (playerStats != null) playerStats.StatsChanged -= OnStatsChanged;
            if (wallet != null) wallet.CurrencyChanged -= OnCurrencyChanged;
        }

        private void OnEquipmentChanged(EquipmentSlot slot, EquipmentInstance item) => QueueRebuild();
        private void OnStatsChanged(PlayerStats stats) => QueueRebuild();
        private void OnCurrencyChanged(CurrencyType currency, int amount, int delta) => QueueRebuild();

        /// <summary>
        /// One rebuild at the end of the frame instead of one per event. Upgrading an equipped
        /// item raises EquipmentChanged, StatsChanged and CurrencyChanged, each of which used to
        /// respawn every slot tile and every stat row - and doing that inside a Button's onClick
        /// destroys the Button that is still mid-dispatch.
        /// </summary>
        private void QueueRebuild() => _rebuildQueued = true;

        private void LateUpdate()
        {
            if (!_rebuildQueued) return;
            BuildContent();
        }

        protected override void BuildContent()
        {
            _rebuildQueued = false;

            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Destroy(_spawned[i]);
            }
            _spawned.Clear();

            // A selected slot that has since been emptied is no longer a selection.
            if (_selectedSlot.HasValue && (equipment == null || equipment.GetEquipped(_selectedSlot.Value) == null))
            {
                _selectedSlot = null;
            }

            BuildHeader();
            BuildSlots();
            BuildStats();
            BuildActions();

            if (detailsLabel != null) detailsLabel.text = _message;
        }

        // ------------------------------------------------------------------ header

        private void BuildHeader()
        {
            if (classLabel != null)
            {
                classLabel.text = playerStats != null && playerStats.CurrentClass != null
                    ? playerStats.CurrentClass.DisplayName.ToUpperInvariant()
                    : "NO CLASS";
            }

            if (levelLabel != null && playerLevel != null)
            {
                string xp = playerLevel.IsMaxLevel
                    ? "MAX"
                    : $"{playerLevel.CurrentXp:0} / {playerLevel.XpForNextLevel:0} XP";
                levelLabel.text = $"Level {playerLevel.Level}    {xp}";
            }

            if (xpFill != null && playerLevel != null)
            {
                xpFill.fillAmount = playerLevel.IsMaxLevel ? 1f : Mathf.Clamp01(playerLevel.XpProgress);
            }

            if (currencyLabel != null && wallet != null)
            {
                currencyLabel.text = $"Gold {wallet.Gold}     Gems {wallet.Gems}";
            }
        }

        // ------------------------------------------------------------------ equipped slots

        private void BuildSlots()
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

                ItemTilePainter.Paint(button, item, itemRegistry, rarityTable,
                    emptySlotColor, $"{slot}\n(empty)");
                ItemTilePainter.SetSelected(button, item != null && _selectedSlot == slot);

                if (tooltip != null && item != null)
                {
                    button.gameObject.AddComponent<ItemTooltipTrigger>().Bind(tooltip, item);
                }

                EquipmentSlot captured = slot;
                button.onClick.AddListener(() => OnSlotTapped(captured));
            }
        }

        private void OnSlotTapped(EquipmentSlot slot)
        {
            EquipmentInstance item = equipment.GetEquipped(slot);
            if (item == null)
            {
                _selectedSlot = null;
                _message = $"{slot}: nothing equipped. Swipe to the bag to find something.";
                QueueRebuild();
                return;
            }

            // Second tap on the selected slot takes the item off - the one-tap flow from before.
            if (_selectedSlot == slot)
            {
                Unequip(slot);
                return;
            }

            _selectedSlot = slot;
            _message = $"{ItemTilePainter.DisplayName(item, itemRegistry, rarityTable)}\n\n{DescribeItem(item)}";
            QueueRebuild();
        }

        // ------------------------------------------------------------------ actions

        private void BuildActions()
        {
            EquipmentInstance selected = _selectedSlot.HasValue && equipment != null
                ? equipment.GetEquipped(_selectedSlot.Value)
                : null;

            bool hasSelection = selected != null;
            bool hasEconomy = economy != null && economy.Config != null;

            if (unequipButton != null)
            {
                unequipButton.interactable = hasSelection;
                SetLabel(unequipButton, "UNEQUIP");
            }

            if (upgradeButton != null)
            {
                upgradeButton.gameObject.SetActive(hasEconomy);
                if (hasEconomy)
                {
                    int cost = hasSelection ? economy.GetUpgradeCost(selected) : -1;
                    bool maxed = hasSelection && economy.IsMaxUpgrade(selected);

                    upgradeButton.interactable = hasSelection && !maxed &&
                                                 wallet != null && wallet.CanAfford(CurrencyType.Gold, cost);

                    SetLabel(upgradeButton, !hasSelection ? "UPGRADE"
                        : maxed ? "UPGRADE\nMAX"
                        : $"UPGRADE\n{cost} gold");
                }
            }
        }

        private void OnUnequipClicked()
        {
            if (_selectedSlot.HasValue) Unequip(_selectedSlot.Value);
        }

        private void Unequip(EquipmentSlot slot)
        {
            EquipmentInstance item = equipment.GetEquipped(slot);
            if (item == null) return;

            string name = ItemTilePainter.DisplayName(item, itemRegistry, rarityTable);
            EquipResult result = equipment.TryUnequip(slot);

            _message = result == EquipResult.Success
                ? $"Unequipped {name}. It is in the bag now."
                : $"Cannot unequip: {ItemTilePainter.Describe(result)}";

            if (result == EquipResult.Success) _selectedSlot = null;
            QueueRebuild();
        }

        private void OnUpgradeClicked()
        {
            if (!_selectedSlot.HasValue || economy == null) return;

            EquipmentInstance item = equipment.GetEquipped(_selectedSlot.Value);
            if (item == null) return;

            string name = ItemTilePainter.DisplayName(item, itemRegistry, rarityTable);
            int cost = economy.GetUpgradeCost(item);

            UpgradeResult result = economy.TryUpgrade(item);

            _message = result == UpgradeResult.Success
                ? $"Upgraded to +{item.UpgradeLevel} for {cost} gold.\n\n{DescribeItem(item)}"
                : $"Cannot upgrade {name}: {ItemEconomy.Describe(result)}\n\n{DescribeItem(item)}";

            QueueRebuild();
        }

        private static void SetLabel(Button button, string text)
        {
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }

        // ------------------------------------------------------------------ stats

        private void BuildStats()
        {
            if (playerStats == null || statRowTemplate == null || statContainer == null) return;

            StatBlock current = playerStats.Current;
            StatBlock baseStats = playerStats.BaseStats;

            // Current HP is not a stat, but it is the first thing anyone opening this screen
            // wants to know, so it leads the list.
            if (playerHealth != null)
            {
                AddRow("Health", $"{playerHealth.CurrentHealth:0} / {playerHealth.MaxHealth:0}",
                    improved: false);
            }

            for (int i = 0; i < StatTypeInfo.Count; i++)
            {
                var stat = (StatType)i;
                float value = current[stat];
                bool improved = baseStats != null && value > baseStats[stat] + 0.0001f;

                AddRow(StatTypeInfo.DisplayName(stat), StatTypeInfo.Format(stat, value), improved);
            }
        }

        private void AddRow(string name, string value, bool improved)
        {
            StatRowView row = Instantiate(statRowTemplate, statContainer);
            row.gameObject.SetActive(true);
            row.gameObject.name = $"Stat_{name}";
            row.Set(name, value);
            row.SetValueColor(improved ? improvedStatColor : neutralStatColor);
            _spawned.Add(row.gameObject);
        }

        /// <summary>
        /// Full stat text for an item, shared with the bag page. Kept here so the gear page can
        /// answer "what would this change?" without a second copy of the formatting rules.
        /// </summary>
        public string DescribeItem(EquipmentInstance item) =>
            ItemTilePainter.DescribeStats(item, itemRegistry, rarityTable, _scratch, _builder,
                economy != null ? economy.Config : null);
    }
}
