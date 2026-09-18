using System;
using UnityEngine;
using UnityEngine.UI;
using RPG.Audio;
using RPG.Core.Events;
using RPG.Stages;

namespace RPG.UI
{
    /// <summary>
    /// The pause menu: resume, toggle sound, or leave the stage.
    ///
    /// Before this the only ways out of a fight were winning it or dying in it. On a phone,
    /// where a run gets interrupted by real life every few minutes, that is not a design
    /// choice, it is a missing feature. Leaving keeps everything earned so far, by the same
    /// rule a death does - the rewards are pending in the collector and nothing here touches
    /// them - and the next stage stays locked.
    ///
    /// Time stops while it is open (ModalPanel does that), so the pause is a real pause.
    /// </summary>
    public class PauseMenuScreen : ModalPanel
    {
        [Header("Systems")]
        [SerializeField] private StageManager stageManager;
        [SerializeField] private StageFailureHandler failureHandler;
        [SerializeField] private PlayerReference playerReference;

        [Tooltip("Optional. The sound toggle drives this.")]
        [SerializeField] private SfxPlayer sfx;

        [Header("UI")]
        [Tooltip("The button on the combat HUD that opens this menu.")]
        [SerializeField] private Button openButton;

        [SerializeField] private Button resumeButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Text soundLabel;
        [SerializeField] private Button leaveButton;

        /// <summary>Raised after the player leaves the stage from here. The game flow listens.</summary>
        public event Action Left;

        protected override void Awake()
        {
            base.Awake();

            if (openButton != null) openButton.onClick.AddListener(Open);
            if (resumeButton != null) resumeButton.onClick.AddListener(Hide);
            if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
            if (leaveButton != null) leaveButton.onClick.AddListener(OnLeave);

            Hide();
        }

        /// <summary>Opens the menu, if there is a stage to pause and a living player to pause it for.</summary>
        public void Open()
        {
            if (IsOpen) return;
            if (stageManager != null && !stageManager.IsStageActive) return;

            // Pausing during the death beat would let the defeat screen open over this one, and
            // the two would then fight over the time scale.
            if (playerReference != null && playerReference.Health != null && !playerReference.Health.IsAlive) return;

            Show();
        }

        protected override void BuildContent() => RefreshSoundLabel();

        private void ToggleSound()
        {
            if (sfx == null) return;
            sfx.Muted = !sfx.Muted;
            RefreshSoundLabel();
        }

        private void RefreshSoundLabel()
        {
            if (soundLabel == null) return;

            if (sfx == null)
            {
                soundLabel.text = "SOUND: N/A";
                return;
            }

            soundLabel.text = sfx.Muted ? "SOUND: OFF" : "SOUND: ON";
        }

        private void OnLeave()
        {
            Hide();
            if (failureHandler != null) failureHandler.Abandon();
            Left?.Invoke();
        }
    }
}
