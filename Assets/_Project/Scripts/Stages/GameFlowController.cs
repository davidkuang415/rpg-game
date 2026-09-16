using System.Collections;
using UnityEngine;
using RPG.Classes;
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

        [Header("Systems")]
        [SerializeField] private StageManager stageManager;
        [SerializeField] private StageEventChannel stageEvents;

        [Header("Timing")]
        [Tooltip("Pause after a stage completes, before the stage list reappears. " +
                 "Phase 9's reward screen takes over this beat.")]
        [SerializeField, Min(0f)] private float stageCompleteDelay = 1.25f;

        private void OnEnable()
        {
            if (classSelectionPanel != null) classSelectionPanel.ClassSelected += OnClassSelected;
            if (stageSelectPanel != null) stageSelectPanel.StageChosen += OnStageChosen;
            if (stageEvents != null) stageEvents.StageCompleted += OnStageCompleted;
        }

        private void OnDisable()
        {
            if (classSelectionPanel != null) classSelectionPanel.ClassSelected -= OnClassSelected;
            if (stageSelectPanel != null) stageSelectPanel.StageChosen -= OnStageChosen;
            if (stageEvents != null) stageEvents.StageCompleted -= OnStageCompleted;
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
            StartCoroutine(ReturnToStageSelect(stage));
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
