using System.Collections;
using UnityEngine;
using RPG.Core.Combat;

namespace RPG.Enemies
{
    /// <summary>
    /// Owns an enemy's lifecycle: it dies, it announces the kill, it cleans itself up.
    ///
    /// The announcement goes through the EnemyEventChannel rather than to any specific system,
    /// which is what lets XP, loot, room-clear tracking and death VFX all be added later
    /// without this script ever learning they exist.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemyStats))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyEventChannel eventChannel;

        [Tooltip("How long the corpse lingers before being removed.")]
        [SerializeField, Min(0f)] private float despawnDelay = 0.5f;

        [Tooltip("Being hit alerts the enemy to the player, even if they were shot from " +
                 "somewhere they could not see.")]
        [SerializeField] private bool alertOnDamage = true;

        private Health _health;
        private EnemyStats _stats;
        private EnemyPerception _perception;
        private EnemyBrain _brain;
        private EnemyMotor _motor;
        private EnemyAttackBase _attack;
        private bool _deathHandled;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _stats = GetComponent<EnemyStats>();
            _perception = GetComponent<EnemyPerception>();
            _brain = GetComponent<EnemyBrain>();
            _motor = GetComponent<EnemyMotor>();
            _attack = GetComponent<EnemyAttackBase>();
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
            _health.DamageTaken += OnDamageTaken;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
            _health.DamageTaken -= OnDamageTaken;
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            if (!alertOnDamage || result.WasDodged || _perception == null) return;
            _perception.ForceAlert();
        }

        private void OnDied(GameObject killer)
        {
            if (_deathHandled) return;
            _deathHandled = true;

            var info = new EnemyDeathInfo(
                _stats.Data, _stats.Level, _stats.IsElite,
                _stats.XpReward, _stats.GoldReward,
                transform.position, killer);

            if (eventChannel != null) eventChannel.RaiseEnemyDied(info);

            ShutDownBehaviour();
            StartCoroutine(DespawnAfterDelay());
        }

        private void ShutDownBehaviour()
        {
            if (_attack != null) _attack.CancelAttack();
            if (_brain != null) _brain.enabled = false;
            if (_perception != null) _perception.enabled = false;

            if (_motor != null)
            {
                _motor.Stop();
                _motor.enabled = false;
            }

            // Corpses stop blocking movement and stop absorbing arrows immediately.
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            {
                collider.enabled = false;
            }
        }

        private IEnumerator DespawnAfterDelay()
        {
            float elapsed = 0f;
            var renderer = GetComponent<SpriteRenderer>();
            Color startColor = renderer != null ? renderer.color : Color.white;

            while (elapsed < despawnDelay)
            {
                elapsed += Time.deltaTime;

                if (renderer != null)
                {
                    float alpha = Mathf.Lerp(startColor.a, 0f, despawnDelay > 0f ? elapsed / despawnDelay : 1f);
                    renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                }

                yield return null;
            }

            // Phase 6 replaces this with a return to the enemy pool, once spawners own enemies.
            Destroy(gameObject);
        }
    }
}
