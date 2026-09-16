using System;
using UnityEngine;
using UnityEngine.UI;
using RPG.Loot;
using RPG.Stages;

namespace RPG.UI
{
    /// <summary>
    /// The death screen: retry the stage, or leave with what you earned.
    ///
    /// It deliberately shows what the failed attempt banked, because the design spec keeps
    /// those rewards - telling the player that up front is the difference between a setback
    /// and feeling robbed.
    /// </summary>
    public class StageFailedScreen : ModalPanel
    {
        [Header("Systems")]
        [SerializeField] private StageFailureHandler failureHandler;
        [SerializeField] private StageRewardCollector collector;

        [Header("UI")]
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text summaryLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button leaveButton;

        private StageData _failedStage;

        /// <summary>Raised when the player chooses to leave rather than retry.</summary>
        public event Action Abandoned;

        protected override void Awake()
        {
            base.Awake();

            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (leaveButton != null) leaveButton.onClick.AddListener(OnLeave);

            Hide();
        }

        private void OnEnable()
        {
            if (failureHandler != null) failureHandler.StageFailed += OnStageFailed;
        }

        private void OnDisable()
        {
            if (failureHandler != null) failureHandler.StageFailed -= OnStageFailed;
        }

        private void OnStageFailed(StageData stage)
        {
            _failedStage = stage;
            Show();
        }

        protected override void BuildContent()
        {
            if (titleLabel != null)
            {
                titleLabel.text = _failedStage != null
                    ? $"DEFEATED IN {_failedStage.DisplayName.ToUpperInvariant()}"
                    : "DEFEATED";
            }

            if (summaryLabel == null) return;

            if (collector == null)
            {
                summaryLabel.text = "Your progress is safe.";
                return;
            }

            StageRewards pending = collector.Pending;
            summaryLabel.text =
                $"You keep everything earned this attempt:\n\n" +
                $"{pending.XpEarned:0} XP   |   {pending.GoldEarned} Gold   |   " +
                $"{pending.Items.Count} item(s)\n\n" +
                "The next stage stays locked until you clear this one.";
        }

        private void OnRetry()
        {
            Hide();
            if (failureHandler != null) failureHandler.Retry();
        }

        private void OnLeave()
        {
            Hide();
            if (failureHandler != null) failureHandler.Abandon();
            Abandoned?.Invoke();
        }
    }
}
