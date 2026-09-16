using UnityEngine;

namespace RPG.Stages
{
    /// <summary>
    /// Definition of one handcrafted stage.
    ///
    /// The layout itself lives in a PREFAB (rooms, walls, spawn points, hazards), not in this
    /// asset and not in a scene. That means a stage can be opened in Prefab Mode and edited
    /// visually, and the StageManager can load any stage into the running game without scene
    /// loading - which keeps the player, HUD and systems alive across stages.
    /// </summary>
    [CreateAssetMenu(fileName = "Stage", menuName = "RPG/Stages/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Progression number. Stage 10, 20, 30... are boss stages.")]
        [SerializeField, Min(1)] private int stageNumber = 1;

        [SerializeField] private string displayName = "Stage 1";

        [Tooltip("The layout prefab. Its root must have a StageController.")]
        [SerializeField] private GameObject stagePrefab;

        [Header("Difficulty")]
        [Tooltip("Level the stage's enemies spawn at. Deliberately separate from Stage Number: " +
                 "they usually track each other but are never the same variable.")]
        [SerializeField, Min(1)] private int enemyLevel = 1;

        [Tooltip("Leave off to use the every-10th-stage rule; turn on to force a boss stage.")]
        [SerializeField] private bool forceBossStage;

        [Header("Rewards (used from Phase 7 onward)")]
        [Tooltip("Multiplies loot quality rolls for this stage. 1 = normal.")]
        [SerializeField, Min(0f)] private float lootModifier = 1f;

        [Tooltip("Multiplies gold earned in this stage. 1 = normal.")]
        [SerializeField, Min(0f)] private float goldModifier = 1f;

        public int StageNumber => stageNumber;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? $"Stage {stageNumber}" : displayName;
        public GameObject StagePrefab => stagePrefab;
        public int EnemyLevel => enemyLevel;
        public float LootModifier => lootModifier;
        public float GoldModifier => goldModifier;

        /// <summary>Every 10th stage is a boss stage, unless an asset forces it on.</summary>
        public bool IsBossStage => forceBossStage || (stageNumber > 0 && stageNumber % 10 == 0);

        private void OnValidate()
        {
            if (stagePrefab != null && stagePrefab.GetComponent<StageController>() == null)
            {
                Debug.LogError($"StageData '{name}': the assigned prefab has no StageController " +
                               "on its root.", this);
            }
        }
    }
}
