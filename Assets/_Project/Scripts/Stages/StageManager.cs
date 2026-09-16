using UnityEngine;
using RPG.CameraSystem;
using RPG.Core.Combat;
using RPG.Core.Events;

namespace RPG.Stages
{
    /// <summary>
    /// Loads and unloads stages, and owns progression unlocking.
    ///
    /// Stages are PREFABS instantiated into the running scene rather than separate Unity
    /// scenes. That keeps the player, HUD, pools and systems alive across stages - no reload,
    /// no re-wiring, no losing the character between fights - while still letting each stage
    /// be authored visually as its own asset.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private StageRegistry registry;
        [SerializeField] private StageProgressState progress;
        [SerializeField] private StageEventChannel stageEvents;
        [SerializeField] private PlayerReference playerReference;

        [Header("Scene")]
        [Tooltip("Parent for the instantiated stage. Optional; defaults to this object.")]
        [SerializeField] private Transform stageRoot;

        [Tooltip("Clamped to each stage's declared bounds as it loads. Optional.")]
        [SerializeField] private CameraFollow2D cameraFollow;

        [Header("Behaviour")]
        [Tooltip("Heal the player to full when a stage begins.")]
        [SerializeField] private bool healPlayerOnStageStart = true;

        private GameObject _currentInstance;
        private StageController _currentController;

        public StageData CurrentStage { get; private set; }
        public bool IsStageActive => _currentInstance != null;

        private void Awake()
        {
            if (stageRoot == null) stageRoot = transform;
        }

        public void LoadStage(int stageNumber)
        {
            StageData stage = registry != null ? registry.GetByNumber(stageNumber) : null;
            if (stage == null)
            {
                Debug.LogError($"[StageManager] No stage with number {stageNumber} in the registry.", this);
                return;
            }

            LoadStage(stage);
        }

        public void LoadStage(StageData stage)
        {
            if (stage == null) return;

            if (!progress.IsUnlocked(stage.StageNumber))
            {
                Debug.LogWarning($"[StageManager] Stage {stage.StageNumber} is locked.", this);
                return;
            }

            if (stage.StagePrefab == null)
            {
                Debug.LogError($"[StageManager] Stage '{stage.name}' has no prefab assigned.", this);
                return;
            }

            UnloadStage();

            CurrentStage = stage;
            _currentInstance = Instantiate(stage.StagePrefab, stageRoot);
            _currentInstance.name = $"Stage_{stage.StageNumber}_Instance";

            _currentController = _currentInstance.GetComponent<StageController>();
            if (_currentController == null)
            {
                Debug.LogError($"[StageManager] Stage prefab '{stage.StagePrefab.name}' has no " +
                               "StageController on its root.", this);
                return;
            }

            PlacePlayer(_currentController);
            ApplyCameraBounds(_currentController);

            _currentController.Completed += OnStageCompleted;
            _currentController.RoomCleared += OnRoomCleared;

            // The stage's enemy level comes from its data, NOT from its stage number.
            _currentController.Begin(stage.EnemyLevel);

            if (stageEvents != null) stageEvents.RaiseStageStarted(stage);
        }

        /// <summary>Reloads the current stage from scratch. Phase 10's death rule uses this.</summary>
        public void RestartCurrentStage()
        {
            if (CurrentStage != null) LoadStage(CurrentStage);
        }

        public void UnloadStage()
        {
            if (_currentController != null)
            {
                _currentController.Completed -= OnStageCompleted;
                _currentController.RoomCleared -= OnRoomCleared;
                _currentController = null;
            }

            if (_currentInstance != null)
            {
                Destroy(_currentInstance);
                _currentInstance = null;
            }

            CurrentStage = null;
        }

        private void PlacePlayer(StageController controller)
        {
            if (playerReference == null || !playerReference.Exists) return;

            if (controller.PlayerSpawnPoint != null)
            {
                playerReference.Transform.position = controller.PlayerSpawnPoint.position;
            }

            if (!healPlayerOnStageStart) return;

            Health health = playerReference.Health;
            if (health != null) health.ResetToFull();
        }

        private void ApplyCameraBounds(StageController controller)
        {
            if (cameraFollow == null) return;

            Vector2 center = (Vector2)controller.transform.position + controller.CameraBoundsCenter;
            cameraFollow.SetBounds(center, controller.CameraBoundsSize);
        }

        private void OnRoomCleared(RoomController room)
        {
            if (stageEvents != null) stageEvents.RaiseRoomCleared(room);
        }

        private void OnStageCompleted(StageController controller)
        {
            StageData completed = CurrentStage;

            // Completing a stage unlocks the next NUMBERED stage, whether or not it is built yet.
            if (progress != null && completed != null)
            {
                progress.UnlockUpTo(completed.StageNumber + 1);
            }

            if (stageEvents != null) stageEvents.RaiseStageCompleted(completed);
        }

        /// <summary>Debug tool support: instantly clears and completes the running stage.</summary>
        public void ForceCompleteCurrentStage()
        {
            if (_currentController != null) _currentController.ForceComplete();
        }
    }
}
