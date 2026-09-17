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
    ///   choose class -> hub -> fight -> stage complete -> back to the hub
    ///
    /// Each screen and system already works standalone; this is the only script that knows what
    /// order they happen in. That makes the flow easy to change later (a campaign map,
    /// auto-advance to the next stage) without touching any of them.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private ClassSelectionPanel classSelectionPanel;

        [Tooltip("The out-of-combat screen: stage list, gear and bag, one swipe apart.")]
        [SerializeField] private HubScreen hubScreen;

        [Tooltip("Optional. When present it owns the post-stage beat: it shows the rewards, " +
                 "and the hub appears only after the player presses Continue.")]
        [SerializeField] private StageCompleteScreen stageCompleteScreen;

        [Tooltip("Optional. Shown on death; its Leave button returns to the hub.")]
        [SerializeField] private StageFailedScreen stageFailedScreen;

        [Header("Systems")]
        [SerializeField] private StageManager stageManager;
        [SerializeField] private StageEventChannel stageEvents;

        [Tooltip("Optional. When present, the saved profile is loaded before the first screen " +
                 "is chosen, so a returning player skips class selection.")]
        [SerializeField] private SaveManager saveManager;

        [SerializeField] private PlayerStats playerStats;

        [Header("Timing")]
        [Tooltip("Pause after a stage completes, before the hub reappears. Only used when no " +
                 "completion screen is assigned.")]
        [SerializeField, Min(0f)] private float stageCompleteDelay = 1.25f;

        /// <summary>The stage list page, or null when the hub is not wired up.</summary>
        private StageSelectPanel Stages => hubScreen != null ? hubScreen.Stages : null;

        private void OnEnable()
        {
            if (classSelectionPanel != null) classSelectionPanel.ClassSelected += OnClassSelected;
            if (Stages != null) Stages.StageChosen += OnStageChosen;
            if (stageEvents != null) stageEvents.StageCompleted += OnStageCompleted;
            if (stageCompleteScreen != null) stageCompleteScreen.Continued += OnCompleteScreenContinued;
            if (stageFailedScreen != null) stageFailedScreen.Abandoned += OnRunAbandoned;
        }

        private void OnDisable()
        {
            if (classSelectionPanel != null) classSelectionPanel.ClassSelected -= OnClassSelected;
            if (Stages != null) Stages.StageChosen -= OnStageChosen;
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
                ShowHub();
                return;
            }

            if (classSelectionPanel != null) classSelectionPanel.Show();
        }

        private void OnClassSelected(ClassData classData) => ShowHub();

        private void OnStageChosen(StageData stage)
        {
            // The hub closes here rather than inside the page, so the page never has to know
            // that it lives in a hub at all.
            if (hubScreen != null) hubScreen.Hide();
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

            StartCoroutine(ReturnToHub(stage));
        }

        /// <summary>
        /// Deferred by a frame so every other StageCompleted listener has run before the stage
        /// object goes away.
        /// </summary>
        private IEnumerator UnloadStageNextFrame()
        {
            yield return null;
            if (stageManager != null) stageManager.UnloadStage();

            // The arena is gone at this point, so the completion screen is the only thing left
            // on screen. If it failed to open, fall back to the hub rather than leaving the
            // player staring at an empty scene with no way forward.
            if (stageCompleteScreen != null && !stageCompleteScreen.IsOpen)
            {
                Debug.LogWarning("[Flow] Completion screen did not open; showing the hub.", this);
                ShowHub();
            }
        }

        private void OnCompleteScreenContinued() => ShowHub();

        private void OnRunAbandoned() => ShowHub();

        private IEnumerator ReturnToHub(StageData stage)
        {
            if (stageCompleteDelay > 0f) yield return new WaitForSeconds(stageCompleteDelay);

            if (stageManager != null) stageManager.UnloadStage();

            if (Stages != null && stage != null)
            {
                Stages.SetHeadline($"{stage.DisplayName.ToUpperInvariant()} COMPLETE");
            }

            ShowHub();
        }

        private void ShowHub()
        {
            if (hubScreen != null) hubScreen.ShowStages();
        }
    }
}
