using UnityEngine;
using RPG.Enemies;

namespace RPG.Progression
{
    /// <summary>
    /// Turns enemy deaths into XP.
    ///
    /// This is the whole point of the enemy event channel: the enemy announces that it died,
    /// and this listens. Neither knows about the other, so XP rules can change - bonus XP for
    /// elites, party XP, rested XP - without editing a single line of enemy code.
    ///
    /// XP is awarded HERE, immediately. The flying XP particles are separate and purely
    /// visual; a dropped or skipped particle can never cost the player experience.
    /// </summary>
    public class ExperienceCollector : MonoBehaviour
    {
        [SerializeField] private EnemyEventChannel enemyEvents;
        [SerializeField] private PlayerLevel playerLevel;

        [Tooltip("Global XP multiplier. A tuning knob and a debug lever, not a game mechanic.")]
        [SerializeField, Min(0f)] private float xpMultiplier = 1f;

        private void OnEnable()
        {
            if (enemyEvents == null || playerLevel == null)
            {
                Debug.LogError($"{nameof(ExperienceCollector)} on '{name}' is missing references.", this);
                return;
            }

            enemyEvents.EnemyDied += OnEnemyDied;
        }

        private void OnDisable()
        {
            if (enemyEvents != null) enemyEvents.EnemyDied -= OnEnemyDied;
        }

        private void OnEnemyDied(EnemyDeathInfo info)
        {
            playerLevel.AddXp(info.XpReward * xpMultiplier);
        }
    }
}
