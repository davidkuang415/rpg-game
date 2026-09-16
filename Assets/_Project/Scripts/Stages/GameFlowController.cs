using System.Collections;
using UnityEngine;
using RPG.Classes;
using RPG.Player;
using RPG.Save;
using RPG.UI;

namespace RPG.Stages
{
    /// <summary>
    /// Sequences the top-level game flow:
    ///
    ///   choose class -> choose stage -> fight -> stage complete -> choose stage again
    ///
    /// Each screen and system already works standalone; this is the only script that knows
    /// what order they happen in. That makes the flow easy to change later (a hub screen, a
    /// campaign map, auto-advance to the next stage) without touching any of them.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private ClassSelectionPanel classSelectionPanel;
        [SerializeField] private StageSelectPanel stageSelectPanel;

        [Tooltip("Optional. When present it owns the post-stage beat: it shows the rewards, " +
                 "and the stage list appears only after the player presses Continue.")]
        [SerializeField] private StageCompleteScreen stageCompleteScreen;

        [Tooltip("Optional. Shown on death; its Leave button returns to the stage list.")]
        [SerializeField] private StageFailedScreen stageFailedScreen;

        [Header("Systems")]
        [SerializeField] private StageManager stageManager;
        [SerializeField] private StageEventChannel stageEvents;

        [Tooltip("Optional. When present, the saved profile is loaded before the first screen " +
                 "is chosen, so a returning player skips class selection.")]
        [SerializeField] private SaveManager saveManager;

        [SerializeField] private PlayerStats playerStats;

        [Header("Timing")]
        [Tooltip("Pause after a stage completes, before the stage list reappears. " +
                 "Phase 9's reward screen takes over this beat.")]
        [SerializeField, Min(0f)] private float stageCompleteDelay = 1.25f;

        private void OnEnable()
        {
            if (classSelectionPanel != null) classSelectionPanel.ClassSelected += OnClassSelected;
            if (stageSelectPanel != null) stageSelectPanel.StageChosen += OnStageChosen;
            if (stageEvents != null) stageEvents.StageCompleted += OnStageCompleted;
            if (stageCompleteScreen != null) stageCompleteScreen.Continued += OnCompleteScreenContinued;
            if (stageFailedScreen != null) stageFailedScreen.Abandoned += OnRunAbandoned;
        }

        private void OnDisable()
        {
            if (classSelectionPanel != null) classSelectionPanel.ClassSelected -= OnClassSelected;
            if (stageSelectPanel != null) stageSelectPanel.StageChosen -= OnStageChosen;
            if (stageEvents != null) stageEvents.StageCompleted -= OnStageCompleted;
            if (stageCompleteScreen != null) stageCompleteScreen.Continued -= OnCompleteScreenContinued;
            if (stageFailedScreen != null) stageFailedScreen.Abandoned -= OnRunAbandoned;
        }

        /// <summary>
        /// The startup sequence. Loading happens HERE rather than in SaveManager's own Start,
        /// because that guarantees the profile is applied before anything decides which screen
        /// to open - Unity does not order Start calls between components.
        /// </summary>
        private void Start()
        {
            bool hasProfile = saveManager != null && saveManager.LoadIfPresent();
            bool hasClass = playerStats != null && playerStats.CurrentClass != null;

            if (hasProfile && hasClass)
            {
                if (stageSelectPanel != null) stageSelectPanel.Show();
                return;
            }

            if (classSelectionPanel != null) classSelectionPanel.Show();
        }

        private void OnClassSelected(ClassData classData)
        {
            if (stageSelectPanel != null) stageSelectPanel.Show();
        }

        private void OnStageChosen(StageData stage)
        {
            if (stageManager != null) stageManager.LoadStage(stage);
        }

        private void OnStageCompleted(StageData stage)
        {
            if (stageCompleteScreen != null)
            {
                // The screen is already showing itself. Just clear the arena behind it and
                // wait for Continue.
                StartCoroutine(UnloadStageNextFrame());
                return;
            }

            StartCoroutine(ReturnToStageSelect(stage));
        }

        /// <summary>
        /// Deferred by a frame so every other StageCompleted listener has run before the stage
        /// object goes away.
        /// </summary>
        private IEnumerator UnloadStageNextFrame()
        {
            yield return null;
            if (stageManager != null) stageManager.UnloadStage();
        }

        private void OnCompleteScreenContinued()
        {
            if (stageSelectPanel != null) stageSelectPanel.Show();
        }

        private void OnRunAbandoned()
        {
            if (stageSelectPanel != null) stageSelectPanel.Show();
        }

        private IEnumerator ReturnToStageSelect(StageData stage)
        {
            if (stageCompleteDelay > 0f) yield return new WaitForSeconds(stageCompleteDelay);

            if (stageManager != null) stageManager.UnloadStage();

            if (stageSelectPanel != null)
            {
                string title = stage != null
                    ? $"{stage.DisplayName.ToUpperInvariant()} COMPLETE"
                    : "STAGE COMPLETE";
                stageSelectPanel.ShowWithTitle(title);
            }
        }
    }
}
