using UnityEngine;
using RPG.Core.Events;
using RPG.Enemies;

namespace RPG.Vfx
{
    /// <summary>
    /// Spawns a burst of XP motes wherever an enemy dies.
    ///
    /// Listens to the same enemy death event the XP system does, but is completely independent
    /// of it: this object can be deleted from the scene and the player still levels up
    /// normally. Feedback and rules are kept separate on purpose.
    /// </summary>
    public class XpParticleSpawner : MonoBehaviour
    {
        [SerializeField] private EnemyEventChannel enemyEvents;
        [SerializeField] private XpParticlePool particlePool;
        [SerializeField] private PlayerReference playerReference;

        [Header("Burst")]
        [Tooltip("Roughly how much XP each mote represents. Bigger kills throw more motes.")]
        [SerializeField, Min(0.1f)] private float xpPerParticle = 4f;

        [SerializeField, Min(1)] private int minParticles = 3;
        [SerializeField, Min(1)] private int maxParticles = 12;

        [Tooltip("Delay between motes in a burst, so they stream toward the player.")]
        [SerializeField, Min(0f)] private float spawnStagger = 0.035f;

        private void OnEnable()
        {
            if (enemyEvents == null || particlePool == null || playerReference == null)
            {
                Debug.LogError($"{nameof(XpParticleSpawner)} on '{name}' is missing references.", this);
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
            if (!playerReference.Exists) return;

            int count = Mathf.Clamp(
                Mathf.RoundToInt(info.XpReward / xpPerParticle), minParticles, maxParticles);

            for (int i = 0; i < count; i++)
            {
                XpParticle particle = particlePool.Get();
                particle.Launch(info.Position, playerReference.Transform, i * spawnStagger);
            }
        }
    }
}
