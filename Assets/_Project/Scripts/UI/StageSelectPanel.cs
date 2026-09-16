using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPG.Stages;

namespace RPG.UI
{
    /// <summary>
    /// Stage selection screen, built from the StageRegistry at runtime.
    ///
    /// Locked stages are shown but not clickable, so progression is visible rather than hidden -
    /// the player can always see what comes next. Adding a stage means adding an asset to the
    /// registry; this screen needs no edits.
    ///
    /// Phase 9 replaces the "Stage Complete" headline here with the real reward screen.
    /// </summary>
    public class StageSelectPanel : ModalPanel
    {
        [Header("Data")]
        [SerializeField] private StageRegistry registry;
        [SerializeField] private StageProgressState progress;

        [Header("Content")]
        [SerializeField] private Text titleLabel;
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private Button buttonTemplate;

        [Header("Style")]
        [SerializeField] private Color unlockedColor = new Color(0.18f, 0.28f, 0.22f);
        [SerializeField] private Color lockedColor = new Color(0.16f, 0.16f, 0.18f);
        [SerializeField] private Color bossColor = new Color(0.32f, 0.18f, 0.22f);

        private readonly List<Button> _spawnedButtons = new List<Button>();
        private string _pendingTitle = "SELECT STAGE";

        /// <summary>Raised when the player picks an unlocked stage.</summary>
        public event Action<StageData> StageChosen;

        protected override void Awake()
        {
            base.Awake();
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
            Hide();
        }

        /// <summary>Opens with a custom headline, e.g. "STAGE 1 COMPLETE".</summary>
        public void ShowWithTitle(string title)
        {
            _pendingTitle = title;
            Show();
        }

        public override void Show()
        {
            if (registry == null || progress == null)
            {
                Debug.LogError($"{nameof(StageSelectPanel)} on '{name}' is missing its Registry " +
                               "or Progress reference.", this);
                return;
            }

            base.Show();
            _pendingTitle = "SELECT STAGE";   // Reset, so the next open is not stale.
        }

        protected override void BuildContent()
        {
            if (titleLabel != null) titleLabel.text = _pendingTitle;

            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null) Destroy(_spawnedButtons[i].gameObject);
            }
            _spawnedButtons.Clear();

            if (buttonTemplate == null || buttonContainer == null) return;

            IReadOnlyList<StageData> stages = registry.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                StageData stage = stages[i];
                if (stage == null) continue;

                bool unlocked = progress.IsUnlocked(stage.StageNumber);

                Button button = Instantiate(buttonTemplate, buttonContainer);
                button.gameObject.name = $"Stage_{stage.StageNumber}";
                button.gameObject.SetActive(true);
                button.interactable = unlocked;

                var background = button.GetComponent<Image>();
                if (background != null)
                {
                    background.color = !unlocked ? lockedColor
                        : (stage.IsBossStage ? bossColor : unlockedColor);
                }

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    string suffix = stage.IsBossStage ? "  [BOSS]" : string.Empty;
                    label.text = unlocked
                        ? $"{stage.DisplayName}{suffix}\nEnemy Level {stage.EnemyLevel}"
                        : $"{stage.DisplayName}{suffix}\nLOCKED";
                }

                StageData captured = stage;
                button.onClick.AddListener(() => Select(captured));

                _spawnedButtons.Add(button);
            }
        }

        private void Select(StageData stage)
        {
            if (!progress.IsUnlocked(stage.StageNumber)) return;

            Hide();
            StageChosen?.Invoke(stage);
        }
    }
}
