using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPG.Stages;

namespace RPG.UI
{
    /// <summary>
    /// The stage list, built from the StageRegistry at runtime and shown as the first page of
    /// the hub.
    ///
    /// Locked stages are shown but not clickable, so progression is visible rather than hidden -
    /// the player can always see what comes next. Adding a stage means adding an asset to the
    /// registry; this page needs no edits.
    ///
    /// It only announces the choice. Closing the hub and loading the stage is the game flow's
    /// job, because the page has no business knowing what happens after a stage is picked.
    /// </summary>
    public class StageSelectPanel : HubPage
    {
        [Header("Data")]
        [SerializeField] private StageRegistry registry;
        [SerializeField] private StageProgressState progress;

        [Header("Content")]
        [Tooltip("Optional sub-heading above the list, e.g. 'STAGE 1 COMPLETE'.")]
        [SerializeField] private Text headlineLabel;

        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private Button buttonTemplate;

        [Header("Style")]
        [SerializeField] private Color unlockedColor = new Color(0.18f, 0.28f, 0.22f);
        [SerializeField] private Color lockedColor = new Color(0.16f, 0.16f, 0.18f);
        [SerializeField] private Color bossColor = new Color(0.32f, 0.18f, 0.22f);

        private readonly List<Button> _spawnedButtons = new List<Button>();
        private string _headline = string.Empty;

        /// <summary>Raised when the player picks an unlocked stage.</summary>
        public event Action<StageData> StageChosen;

        private void Awake()
        {
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets the sub-heading shown above the list. Cleared once displayed, so a headline from
        /// a previous stage never lingers.
        /// </summary>
        public void SetHeadline(string headline)
        {
            _headline = headline ?? string.Empty;
            Refresh();
        }

        protected override void BuildContent()
        {
            if (headlineLabel != null)
            {
                headlineLabel.text = _headline;
                headlineLabel.gameObject.SetActive(!string.IsNullOrEmpty(_headline));
            }

            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null) Destroy(_spawnedButtons[i].gameObject);
            }
            _spawnedButtons.Clear();

            if (registry == null || progress == null)
            {
                Debug.LogError($"{nameof(StageSelectPanel)} on '{name}' is missing its Registry " +
                               "or Progress reference.", this);
                return;
            }

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

            _headline = string.Empty;
            StageChosen?.Invoke(stage);
        }
    }
}
