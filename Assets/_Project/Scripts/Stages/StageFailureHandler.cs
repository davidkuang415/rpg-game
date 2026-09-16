using System;
using UnityEngine;
using RPG.Core.Combat;
using RPG.Core.Events;
using RPG.Player;

namespace RPG.Stages
{
    /// <summary>
    /// What happens when the player dies.
    ///
    /// The design spec's rule, in full:
    ///   - restart the CURRENT stage
    ///   - the next stage does NOT unlock
    ///   - previously completed stages stay unlocked
    ///   - XP, gold and loot earned during the failed attempt are KEPT
    ///
    /// Most of that is already true by construction and worth stating plainly:
    /// unlocking only ever happens in StageManager's completion handler, XP is banked per kill,
    /// and pending rewards are only emptied by an explicit claim. So this handler adds the one
    /// missing piece - noticing the death and offering a retry - rather than special-casing
    /// progression anywhere.
    ///
    /// The whole rule lives in this one script, so changing it later (lose gold on death, a
    /// revive cost, a run-ending penalty) is a local edit.
    /// </summary>
    public class StageFailureHandler : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private PlayerReference playerReference;
        [SerializeField] private StageManager stageManager;
        [SerializeField] private StageEventChannel stageEvents;

        [Header("Behaviour")]
        [Tooltip("Seconds between dying and the failure screen appearing, so the death reads.")]
        [SerializeField, Min(0f)] private float delayBeforeScreen = 0.9f;

        [Tooltip("Off means a death restarts immediately with no screen. Useful while testing.")]
        [SerializeField] private bool showFailureScreen = true;

        private Health _playerHealth;
        private PlayerController _playerController;
        private float _screenDueAt;
        private bool _awaitingScreen;
        private bool _handlingDeath;

        /// <summary>Raised once the failure is confirmed. The failure screen listens.</summary>
        public event Action<StageData> StageFailed;

        private void Update()
        {
            TrackPlayerHealth();

            if (!_awaitingScreen || Time.unscaledTime < _screenDueAt) return;

            _awaitingScreen = false;
            StageData failedStage = stageManager != null ? stageManager.CurrentStage : null;

            if (stageEvents != null) stageEvents.RaiseStageFailed(failedStage);

            if (showFailureScreen) StageFailed?.Invoke(failedStage);
            else Retry();
        }

        /// <summary>
        /// The player is a persistent object, but its Health component is only findable once
        /// registered, and the reference can change. Re-subscribing here keeps it correct
        /// without a scene search.
        /// </summary>
        private void TrackPlayerHealth()
        {
            Health current = playerReference != null ? playerReference.Health : null;
            if (current == _playerHealth) return;

            if (_playerHealth != null) _playerHealth.Died -= OnPlayerDied;
            _playerHealth = current;
            if (_playerHealth != null) _playerHealth.Died += OnPlayerDied;

            _playerController = playerReference != null && playerReference.GameObject != null
                ? playerReference.GameObject.GetComponent<PlayerController>()
                : null;
        }

        private void OnDisable()
        {
            if (_playerHealth != null) _playerHealth.Died -= OnPlayerDied;
            _playerHealth = null;
        }

        private void OnPlayerDied(GameObject killer)
        {
            // Dying outside a stage (debug damage on the stage select screen) is not a failure.
            if (stageManager == null || !stageManager.IsStageActive) return;
            if (_handlingDeath) return;

            _handlingDeath = true;
            _awaitingScreen = true;

            // Stop the corpse from moving and swinging during the beat before the screen.
            if (_playerController != null) _playerController.SetInputEnabled(false);

            _screenDueAt = Time.unscaledTime + delayBeforeScreen;

            Debug.Log($"[Stage] Player died in '{stageManager.CurrentStage?.DisplayName}'. " +
                      "XP, gold and loot earned this attempt are kept; the next stage stays locked.", this);
        }

        /// <summary>Reloads the current stage from the start. Rewards already earned are untouched.</summary>
        public void Retry()
        {
            _handlingDeath = false;
            if (stageManager != null) stageManager.RestartCurrentStage();
            if (_playerController != null) _playerController.SetInputEnabled(true);
        }

        /// <summary>Abandons the attempt. Everything earned so far is still kept.</summary>
        public void Abandon()
        {
            _handlingDeath = false;
            if (stageManager != null) stageManager.UnloadStage();
            RevivePlayer();
            if (_playerController != null) _playerController.SetInputEnabled(true);
        }

        /// <summary>
        /// Brings the player back to full health. Called when leaving a stage, since the stage
        /// loader only heals on entry.
        /// </summary>
        private void RevivePlayer()
        {
            if (playerReference != null && playerReference.Health != null)
            {
                playerReference.Health.ResetToFull();
            }
        }
    }
}
