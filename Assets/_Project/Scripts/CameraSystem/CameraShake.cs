using UnityEngine;
using RPG.Core.Combat;
using RPG.Core.Events;
using RPG.Enemies;

namespace RPG.CameraSystem
{
    /// <summary>
    /// A short shake when the player is hit, a smaller one on a kill, and a bigger one when a
    /// hit is critical.
    ///
    /// It adds an offset in LateUpdate AFTER CameraFollow2D has positioned the camera, and
    /// removes it again before the next frame's follow runs, so the follow never sees the
    /// shake and the shake never fights the follow.
    ///
    /// Trauma-based: each event adds to a value that decays over time, and the shake strength
    /// is trauma squared. Several hits in a row build up rather than restarting, and small
    /// hits barely register while big ones are unmistakable.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)]   // After CameraFollow2D, which uses the default order.
    public class CameraShake : MonoBehaviour
    {
        [Header("Sources")]
        [Tooltip("Shakes when the player takes damage.")]
        [SerializeField] private PlayerReference playerReference;

        [Tooltip("Optional. A small kick on every kill.")]
        [SerializeField] private EnemyEventChannel enemyEvents;

        [Header("Amounts (trauma added, 0-1)")]
        [SerializeField, Range(0f, 1f)] private float playerHitTrauma = 0.35f;
        [SerializeField, Range(0f, 1f)] private float playerCriticalHitTrauma = 0.55f;
        [SerializeField, Range(0f, 1f)] private float killTrauma = 0.12f;

        [Header("Feel")]
        [Tooltip("Largest offset, in world units, at full trauma.")]
        [SerializeField, Min(0f)] private float maxOffset = 0.35f;

        [Tooltip("Largest roll, in degrees, at full trauma.")]
        [SerializeField, Min(0f)] private float maxRoll = 1.5f;

        [Tooltip("Trauma lost per second.")]
        [SerializeField, Min(0.1f)] private float decayPerSecond = 2.2f;

        [Tooltip("How fast the noise moves. Higher = jitter, lower = sway.")]
        [SerializeField, Min(1f)] private float frequency = 28f;

        private float _trauma;
        private Vector3 _appliedOffset;
        private float _appliedRoll;
        private Health _watchedHealth;
        private float _seed;

        private void Awake() => _seed = Random.value * 100f;

        private void OnEnable()
        {
            if (enemyEvents != null) enemyEvents.EnemyDied += OnEnemyDied;
        }

        private void OnDisable()
        {
            if (enemyEvents != null) enemyEvents.EnemyDied -= OnEnemyDied;
            Unwatch();
            RemoveApplied();
        }

        private void Update()
        {
            // The player registers itself during Awake; by our first Update it is there.
            if (_watchedHealth == null && playerReference != null && playerReference.Exists)
            {
                Watch(playerReference.Health);
            }
        }

        private void LateUpdate()
        {
            // Undo last frame's shake first, so the follow camera's position is the base.
            RemoveApplied();

            if (_trauma <= 0f) return;

            _trauma = Mathf.Max(0f, _trauma - decayPerSecond * Time.deltaTime);
            float strength = _trauma * _trauma;

            float t = Time.time * frequency;
            float x = (Mathf.PerlinNoise(_seed, t) * 2f - 1f) * maxOffset * strength;
            float y = (Mathf.PerlinNoise(_seed + 17f, t) * 2f - 1f) * maxOffset * strength;
            float roll = (Mathf.PerlinNoise(_seed + 31f, t) * 2f - 1f) * maxRoll * strength;

            _appliedOffset = new Vector3(x, y, 0f);
            _appliedRoll = roll;

            transform.position += _appliedOffset;
            transform.rotation *= Quaternion.Euler(0f, 0f, _appliedRoll);
        }

        private void RemoveApplied()
        {
            if (_appliedOffset == Vector3.zero && _appliedRoll == 0f) return;

            transform.position -= _appliedOffset;
            transform.rotation *= Quaternion.Euler(0f, 0f, -_appliedRoll);
            _appliedOffset = Vector3.zero;
            _appliedRoll = 0f;
        }

        /// <summary>Adds shake. Any system can call this: boss slams, explosions, level-ups.</summary>
        public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + Mathf.Max(0f, amount));

        private void Watch(Health health)
        {
            if (health == null) return;
            _watchedHealth = health;
            _watchedHealth.DamageTaken += OnPlayerDamaged;
        }

        private void Unwatch()
        {
            if (_watchedHealth == null) return;
            _watchedHealth.DamageTaken -= OnPlayerDamaged;
            _watchedHealth = null;
        }

        private void OnPlayerDamaged(DamageInfo info, DamageResult result)
        {
            if (result.WasDodged) return;
            AddTrauma(info.IsCritical ? playerCriticalHitTrauma : playerHitTrauma);
        }

        private void OnEnemyDied(EnemyDeathInfo info) => AddTrauma(killTrauma);
    }
}
