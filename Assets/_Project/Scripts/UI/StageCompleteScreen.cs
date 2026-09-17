using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPG.Economy;
using RPG.Items;
using RPG.Loot;
using RPG.Progression;
using RPG.Stages;

namespace RPG.UI
{
    /// <summary>
    /// The stage completion screen: what you earned, and the XP bar filling up.
    ///
    /// The design spec is emphatic that this is PRESENTATION ONLY. XP was already awarded the
    /// moment each enemy died, and gold and items are banked the instant this screen opens -
    /// before a single frame of animation plays. The bar then replays a result that is already
    /// permanent, so quitting mid-animation cannot cost the player anything.
    ///
    /// The bar shows two fills, per the spec: cyan for XP already held, a darker blue for the
    /// XP just earned, and the dark portion converts to cyan left-to-right as it animates.
    /// Multiple level-ups play in sequence.
    /// </summary>
    public class StageCompleteScreen : ModalPanel
    {
        [Header("Systems")]
        [SerializeField] private StageEventChannel stageEvents;
        [SerializeField] private PlayerLevel playerLevel;
        [SerializeField] private XpCurveData xpCurve;
        [SerializeField] private StageRewardCollector collector;
        [SerializeField] private RewardClaimer claimer;
        [SerializeField] private CurrencyWallet wallet;

        [Header("Item Display")]
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private RarityTable rarityTable;

        [Header("Labels")]
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text xpValueLabel;
        [SerializeField] private Text xpGainedLabel;
        [SerializeField] private Text goldLabel;
        [SerializeField] private Text levelUpLabel;
        [SerializeField] private Text itemsHeaderLabel;

        [Header("XP Bar")]
        [Tooltip("Filled image showing XP already earned before this stage. Cyan.")]
        [SerializeField] private Image xpFillCurrent;

        [Tooltip("Filled image drawn behind it showing the newly earned XP. Darker blue.")]
        [SerializeField] private Image xpFillPending;

        [Header("Items")]
        [SerializeField] private RectTransform itemsContainer;
        [SerializeField] private Button itemTileTemplate;

        [Header("Buttons")]
        [SerializeField] private Button continueButton;

        [Header("Timing (unscaled - the game is paused here)")]
        [Tooltip("Seconds to fill one whole level's worth of bar. Partial fills scale down.")]
        [SerializeField, Min(0.05f)] private float secondsPerFullBar = 1.1f;

        [SerializeField, Min(0f)] private float delayBeforeXp = 0.35f;
        [SerializeField, Min(0f)] private float levelUpPause = 0.45f;
        [SerializeField, Min(0f)] private float itemRevealStagger = 0.12f;

        private readonly List<GameObject> _spawnedTiles = new List<GameObject>();
        private readonly List<EquipmentInstance> _shownItems = new List<EquipmentInstance>();

        private Coroutine _animation;
        private StageData _completedStage;
        private int _goldGained;
        private float _xpGained;

        // Captured at stage start so "level before" is accurate even though XP was awarded live.
        private bool _hasSnapshot;
        private int _snapshotLevel = 1;
        private float _snapshotXp;

        /// <summary>Raised when the player presses Continue. The game flow listens.</summary>
        public event Action Continued;

        protected override void Awake()
        {
            base.Awake();
            if (itemTileTemplate != null) itemTileTemplate.gameObject.SetActive(false);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinuePressed);

            // Subscribing here rather than in OnEnable is essential, not stylistic: this
            // component lives ON the panel root, so Hide() below deactivates its own
            // GameObject. OnEnable would never run while hidden, and a screen that only
            // listens while already visible can never be the thing that opens itself.
            if (stageEvents != null)
            {
                stageEvents.StageStarted += OnStageStarted;
                stageEvents.StageCompleted += OnStageCompleted;
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (stageEvents == null) return;
            stageEvents.StageStarted -= OnStageStarted;
            stageEvents.StageCompleted -= OnStageCompleted;
        }

        /// <summary>
        /// Records where progression stood when this run began.
        ///
        /// Only snapshots when nothing is pending, because a death-and-retry keeps the rewards
        /// from the failed attempt - so the "before" state must stay the one from the ORIGINAL
        /// attempt, not the restart.
        /// </summary>
        private void OnStageStarted(StageData stage)
        {
            if (playerLevel == null) return;
            if (collector != null && !collector.Pending.IsEmpty && _hasSnapshot) return;

            _snapshotLevel = playerLevel.Level;
            _snapshotXp = playerLevel.CurrentXp;
            _hasSnapshot = true;
        }

        private void OnStageCompleted(StageData stage)
        {
            _completedStage = stage;
            CaptureAndBankRewards();
            Show();
        }

        /// <summary>
        /// Snapshots the rewards for display, then banks them immediately.
        ///
        /// Order matters: the display copy is taken first because claiming empties the pending
        /// list. Banking happens here rather than when the animation ends, so the animation's
        /// length can never affect what the player actually keeps.
        /// </summary>
        private void CaptureAndBankRewards()
        {
            _shownItems.Clear();
            _goldGained = 0;
            _xpGained = 0f;

            if (collector != null)
            {
                StageRewards pending = collector.Pending;
                _goldGained = pending.GoldEarned;
                _xpGained = pending.XpEarned;
                _shownItems.AddRange(pending.Items);
            }

            if (claimer != null) claimer.ClaimPending();
        }

        protected override void BuildContent()
        {
            if (titleLabel != null)
            {
                titleLabel.text = _completedStage != null
                    ? $"{_completedStage.DisplayName.ToUpperInvariant()} COMPLETE"
                    : "STAGE COMPLETE";
            }

            if (xpGainedLabel != null) xpGainedLabel.text = $"+{_xpGained:0} XP";

            if (goldLabel != null)
            {
                string gems = wallet != null ? $"      Gems  {wallet.Gems}" : string.Empty;
                goldLabel.text = $"+{_goldGained} Gold{gems}";
            }

            if (levelUpLabel != null) levelUpLabel.gameObject.SetActive(false);

            BuildItemTiles();
            SetBar(_snapshotLevel, _snapshotXp, _xpGained);
        }

        public override void Show()
        {
            base.Show();

            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(PlaySequence());
        }

        // ------------------------------------------------------------------ items

        private void BuildItemTiles()
        {
            for (int i = 0; i < _spawnedTiles.Count; i++)
            {
                if (_spawnedTiles[i] != null) Destroy(_spawnedTiles[i]);
            }
            _spawnedTiles.Clear();

            if (itemsHeaderLabel != null)
            {
                itemsHeaderLabel.text = _shownItems.Count > 0
                    ? $"EQUIPMENT OBTAINED ({_shownItems.Count})"
                    : "NO EQUIPMENT THIS RUN";
            }

            if (itemTileTemplate == null || itemsContainer == null) return;

            for (int i = 0; i < _shownItems.Count; i++)
            {
                EquipmentInstance item = _shownItems[i];

                Button tile = Instantiate(itemTileTemplate, itemsContainer);
                tile.gameObject.name = $"Item_{i}";
                tile.interactable = false;
                _spawnedTiles.Add(tile.gameObject);

                ItemDefinition definition = itemRegistry != null ? itemRegistry.GetDefinition(item) : null;

                var image = tile.GetComponent<Image>();
                if (image != null && rarityTable != null)
                {
                    image.color = Color.Lerp(new Color(0.16f, 0.17f, 0.2f),
                        rarityTable.GetColor(item.Rarity), 0.5f);
                }

                var label = tile.GetComponentInChildren<Text>();
                if (label != null)
                {
                    string itemName = definition != null ? definition.DisplayName : item.TemplateId;
                    label.text = $"{itemName}\nLv {item.ItemLevel}  {item.Rarity}";
                }

                // Revealed one by one by the sequence below.
                tile.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ animation

        private IEnumerator PlaySequence()
        {
            if (continueButton != null) continueButton.interactable = false;

            // WaitForSecondsRealtime throughout: ModalPanel pauses the game while this is open.
            yield return new WaitForSecondsRealtime(delayBeforeXp);

            yield return AnimateXp();

            for (int i = 0; i < _spawnedTiles.Count; i++)
            {
                if (_spawnedTiles[i] != null) _spawnedTiles[i].SetActive(true);
                if (itemRevealStagger > 0f) yield return new WaitForSecondsRealtime(itemRevealStagger);
            }

            if (continueButton != null) continueButton.interactable = true;
            _animation = null;
        }

        /// <summary>
        /// Fills the bar from the pre-stage state up to the current one, pausing on each
        /// level-up. Because XP was already banked, this is a replay - the numbers it lands on
        /// always match the player's real progression.
        /// </summary>
        private IEnumerator AnimateXp()
        {
            int level = _snapshotLevel;
            float xp = _snapshotXp;
            float remaining = _xpGained;

            while (remaining > 0.01f)
            {
                float required = RequirementFor(level);
                if (float.IsInfinity(required)) break;          // Max level: nothing left to fill.

                float room = Mathf.Max(0f, required - xp);
                float step = Mathf.Min(room, remaining);

                float from = xp;
                float to = xp + step;

                // FillTo scales its duration by the fraction of a bar covered, so a sliver
                // does not take as long as a whole level.
                yield return FillTo(level, from, to, required, remaining);

                xp = to;
                remaining -= step;

                bool leveled = xp >= required - 0.01f;
                if (!leveled) break;

                level++;
                xp = 0f;

                yield return ShowLevelUp(level);
                SetBar(level, 0f, remaining);
            }

            SetBar(level, xp, 0f);
        }

        private IEnumerator FillTo(int level, float from, float to, float required, float remainingTotal)
        {
            float duration = secondsPerFullBar * (required > 0f ? Mathf.Abs(to - from) / required : 0f);

            if (duration <= 0.01f)
            {
                SetBar(level, to, remainingTotal - (to - from));
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float shown = Mathf.Lerp(from, to, t);

                SetBar(level, shown, remainingTotal - (shown - from));
                yield return null;
            }

            SetBar(level, to, remainingTotal - (to - from));
        }

        private IEnumerator ShowLevelUp(int newLevel)
        {
            if (levelUpLabel != null)
            {
                levelUpLabel.text = $"LEVEL UP!   {newLevel}";
                levelUpLabel.gameObject.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(levelUpPause);

            if (levelUpLabel != null) levelUpLabel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Paints both fills. The pending (dark) fill always sits at or beyond the current
        /// (cyan) one, so the cyan visibly eats into it as it grows.
        /// </summary>
        private void SetBar(int level, float currentXp, float pendingXp)
        {
            float required = RequirementFor(level);
            bool maxed = float.IsInfinity(required);

            float currentFraction = maxed ? 1f : Mathf.Clamp01(currentXp / required);
            float pendingFraction = maxed ? 1f : Mathf.Clamp01((currentXp + Mathf.Max(0f, pendingXp)) / required);

            if (xpFillCurrent != null) xpFillCurrent.fillAmount = currentFraction;
            if (xpFillPending != null) xpFillPending.fillAmount = pendingFraction;

            if (levelLabel != null) levelLabel.text = $"LEVEL {level}";

            if (xpValueLabel != null)
            {
                xpValueLabel.text = maxed ? "MAX LEVEL" : $"{currentXp:0} / {required:0}";
            }
        }

        private float RequirementFor(int level) =>
            xpCurve != null ? xpCurve.GetRequirementForLevel(level) : float.PositiveInfinity;

        // ------------------------------------------------------------------ continue

        private void OnContinuePressed()
        {
            if (_animation != null)
            {
                // Second press skips the animation rather than being ignored.
                StopCoroutine(_animation);
                _animation = null;
            }

            _hasSnapshot = false;
            Hide();
            Continued?.Invoke();
        }
    }
}
