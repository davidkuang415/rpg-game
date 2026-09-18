using UnityEngine;
using RPG.CameraSystem;
using RPG.Core.Combat;
using RPG.Core.Events;

namespace RPG.Progression
{
    /// <summary>
    /// Makes a mid-fight level-up an event rather than a log line.
    ///
    /// XP is banked per kill, so most level-ups happen in the middle of a room - and until
    /// now nothing happened when they did. The completion screen replays them afterwards, but
    /// by then the moment has passed. This restores the player to full health on the spot (the
    /// classic reward, and a real tactical swing in a hard room) and kicks the camera. The HUD
    /// banner and the sound layer listen to the same PlayerLevel event on their own.
    /// </summary>
    public class LevelUpFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerReference playerReference;

        [Tooltip("Optional. Found on the main camera if left empty.")]
        [SerializeField] private CameraShake cameraShake;

        [Header("Effect")]
        [Tooltip("Fraction of max health restored on level-up. 1 = full heal.")]
        [SerializeField, Range(0f, 1f)] private float healFraction = 1f;

        [SerializeField, Range(0f, 1f)] private float cameraTrauma = 0.3f;

        private PlayerLevel _level;

        private void Start()
        {
            if (cameraShake == null && Camera.main != null) cameraShake = Camera.main.GetComponent<CameraShake>();
        }

        private void Update()
        {
            // The player registers itself at runtime; bind once it exists (same pattern as
            // CameraShake and StageFailureHandler).
            if (_level != null || playerReference == null || !playerReference.Exists) return;

            _level = playerReference.GameObject.GetComponent<PlayerLevel>();
            if (_level != null) _level.LeveledUp += OnLeveledUp;
        }

        private void OnDisable()
        {
            if (_level != null) _level.LeveledUp -= OnLeveledUp;
            _level = null;
        }

        private void OnLeveledUp(int previous, int next)
        {
            Health health = playerReference != null ? playerReference.Health : null;
            if (health != null && health.IsAlive && healFraction > 0f)
            {
                health.Heal(health.MaxHealth * healFraction);
            }

            if (cameraShake != null) cameraShake.AddTrauma(cameraTrauma);
        }
    }
}
