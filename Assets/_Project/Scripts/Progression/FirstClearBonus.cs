using UnityEngine;
using RPG.Economy;
using RPG.Stages;

namespace RPG.Progression
{
    /// <summary>
    /// Pays a one-off gem bonus the first time each stage is cleared.
    ///
    /// Gems previously had exactly one source in the whole game: selling a Legendary or better,
    /// which is a 1.8% weight drop. Meanwhile their only sink - bag expansion - becomes urgent
    /// within about three stages. A player could therefore need bag space for hours with no
    /// legitimate way to earn it. This puts gems on the critical path instead of the loot lottery.
    ///
    /// It pays on FIRST clear only, so replaying an easy stage cannot farm the premium currency.
    /// </summary>
    public class FirstClearBonus : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private StageProgressState progress;
        [SerializeField] private StageEventChannel stageEvents;

        [Header("Reward")]
        [Tooltip("Gems paid the first time a stage is cleared.")]
        [SerializeField, Min(0)] private int gemsPerFirstClear = 15;

        [Tooltip("Extra gems per stage number, so later first clears are worth more.")]
        [SerializeField, Min(0)] private int gemsPerStageNumber = 2;

        [SerializeField] private bool logAwards = true;

        // Snapshot taken when the stage begins, so the "was this new?" test cannot be spoiled
        // by whatever else is listening to StageCompleted.
        private int _highestUnlockedAtStageStart = int.MaxValue;

        private void OnEnable()
        {
            if (stageEvents == null) return;

            stageEvents.StageStarted += OnStageStarted;
            stageEvents.StageCompleted += OnStageCompleted;
        }

        private void OnDisable()
        {
            if (stageEvents == null) return;

            stageEvents.StageStarted -= OnStageStarted;
            stageEvents.StageCompleted -= OnStageCompleted;
        }

        private void OnStageStarted(StageData stage)
        {
            _highestUnlockedAtStageStart = progress != null ? progress.HighestUnlockedStage : int.MaxValue;
        }

        private void OnStageCompleted(StageData stage)
        {
            if (stage == null || wallet == null || progress == null) return;

            // Compared against the snapshot from when the stage STARTED, not the live value.
            // StageManager also listens to StageCompleted and unlocks the next stage there, and
            // Unity does not order event subscribers - reading the live value would award the
            // bonus or not depending on which handler happened to run first.
            bool isFirstClear = stage.StageNumber >= _highestUnlockedAtStageStart;
            _highestUnlockedAtStageStart = int.MaxValue;

            if (!isFirstClear) return;

            int gems = gemsPerFirstClear + gemsPerStageNumber * Mathf.Max(0, stage.StageNumber - 1);
            if (gems <= 0) return;

            wallet.Add(CurrencyType.Gems, gems);

            if (logAwards)
            {
                Debug.Log($"[Progression] First clear of {stage.DisplayName}: +{gems} gems.", this);
            }
        }
    }
}
