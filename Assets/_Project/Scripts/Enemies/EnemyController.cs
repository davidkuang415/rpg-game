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

        [Header("Elite Presentation")]
        [Tooltip("Colour the Outline child takes while this enemy is an elite.")]
        [SerializeField] private Color eliteOutlineColor = new Color(1f, 0.78f, 0.2f);

        private Health _health;
        private PooledEnemy _pooled;
        private SpriteRenderer[] _renderers;
        private Color[] _rendererColors;
        private EnemyStats _stats;
        private EnemyPerception _perception;
        private EnemyBrain _brain;
        private EnemyMotor _motor;
        private EnemyAttackBase _attack;
        private bool _deathHandled;
        private Vector3 _authoredScale;
        private SpriteRenderer _outline;
        private Color _authoredOutlineColor;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _stats = GetComponent<EnemyStats>();
            _perception = GetComponent<EnemyPerception>();
            _brain = GetComponent<EnemyBrain>();
            _motor = GetComponent<EnemyMotor>();
            _attack = GetComponent<EnemyAttackBase>();
            _pooled = GetComponent<PooledEnemy>();

            // Captured before anything fades them, so a reused enemy can be restored to the
            // colours it was authored with rather than to whatever the death fade left behind.
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _rendererColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _rendererColors[i] = _renderers[i].color;

            _authoredScale = transform.localScale;

            Transform outline = transform.Find("Outline");
            _outline = outline != null ? outline.GetComponent<SpriteRenderer>() : null;
            if (_outline != null) _authoredOutlineColor = _outline.color;
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

        /// <summary>
        /// Puts a reused enemy back into a fightable state.
        ///
        /// Called by the spawn point AFTER stats have been configured, not from OnEnable, so the
        /// health reset sees the level this enemy is being spawned at rather than the one the
        /// previous occupant died with.
        ///
        /// Everything ShutDownBehaviour switched off is switched back on here. Anything missed
        /// would come back as an enemy that cannot move, cannot be hit, or is invisible - so the
        /// two methods are deliberately kept next to each other.
        /// </summary>
        public void ResetForReuse()
        {
            StopAllCoroutines();
            _deathHandled = false;

            if (_brain != null) _brain.enabled = true;
            if (_perception != null)
            {
                _perception.enabled = true;
                _perception.ResetMemory();
            }

            if (_motor != null)
            {
                _motor.enabled = true;
                _motor.Stop();
            }

            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>(true))
            {
                collider.enabled = true;
            }

            RestoreRendererColors();
            ApplyElitePresentation();

            if (_health != null) _health.ResetToFull();

            // Announced last, once the enemy is fully configured and at full health, so a
            // listener that binds a health bar to it reads the right numbers straight away.
            if (eventChannel != null && _stats != null) eventChannel.RaiseEnemySpawned(_stats);
        }

        /// <summary>
        /// Bigger, with a gold outline. Applied on every spawn because a pooled object can be
        /// an elite this time and a regular the next; the authored values are restored when
        /// it is not one.
        /// </summary>
        private void ApplyElitePresentation()
        {
            bool elite = _stats != null && _stats.IsElite;

            float scale = elite && _stats.DifficultyCurve != null ? _stats.DifficultyCurve.EliteScale : 1f;
            transform.localScale = _authoredScale * scale;

            if (_outline != null) _outline.color = elite ? eliteOutlineColor : _authoredOutlineColor;
        }

        /// <summary>Undoes the death fade, which leaves every sprite at zero alpha.</summary>
        private void RestoreRendererColors()
        {
            if (_renderers == null) return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                _renderers[i].color = _rendererColors[i];
            }
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

            // Every sprite on the corpse fades together: body, outline, shadow. The health bar
            // hides itself on death, so it is not in this list for long anyway.
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            var startColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) startColors[i] = renderers[i].color;

            while (elapsed < despawnDelay)
            {
                elapsed += Time.deltaTime;
                float t = despawnDelay > 0f ? elapsed / despawnDelay : 1f;

                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    Color c = startColors[i];
                    c.a = Mathf.Lerp(startColors[i].a, 0f, t);
                    renderers[i].color = c;
                }

                yield return null;
            }

            // Back to the pool if there is one, destroyed if there is not. Either way this
            // object stops existing as far as the stage is concerned.
            if (_pooled == null || !_pooled.ReturnToPool()) Destroy(gameObject);
        }
    }
}
