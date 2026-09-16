using UnityEngine;
using RPG.Core.Combat;

namespace RPG.Combat.Projectiles
{
    /// <summary>
    /// A travelling arrow (and later, every enemy projectile).
    ///
    /// Movement is raycast-based rather than collider-based: each physics step it sweeps from
    /// its current position to the next one. That means a fast arrow can never tunnel through
    /// a thin wall, which a trigger collider would allow, and it needs no Rigidbody at all.
    ///
    /// Damage is rolled when the arrow is FIRED, not when it lands, so a stat change mid-flight
    /// cannot retroactively alter a shot already in the air.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float speed = 18f;

        [Tooltip("Sweep radius. A little forgiveness so arrows do not slip past thin targets.")]
        [SerializeField, Min(0f)] private float sweepRadius = 0.08f;

        [Header("Penetration")]
        [Tooltip("How many extra targets a shot may pass through. Always 0 today - this is the " +
                 "hook a future piercing enchantment would raise, and nothing sets it yet.")]
        [SerializeField, Min(0)] private int maxPierceCount;

        private DamageInfo _payload;
        private Vector2 _direction;
        private float _remainingRange;
        private int _targetMask;
        private int _blockingMask;
        private int _pierceRemaining;
        private IDamageDealtListener _listener;
        private ProjectilePool _pool;
        private bool _active;

        public float Speed => speed;

        /// <summary>Called by the pool when this instance is created.</summary>
        public void BindPool(ProjectilePool pool) => _pool = pool;

        /// <summary>
        /// Fires the projectile. <paramref name="payload"/> already contains the rolled damage
        /// and crit result; impact details are filled in on hit.
        /// </summary>
        public void Launch(Vector2 origin, Vector2 direction, float maxRange, in DamageInfo payload,
            int targetMask, int blockingMask, IDamageDealtListener listener)
        {
            transform.position = origin;
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);

            _payload = payload;
            _remainingRange = Mathf.Max(0f, maxRange);
            _targetMask = targetMask;
            _blockingMask = blockingMask;
            _pierceRemaining = maxPierceCount;
            _listener = listener;
            _active = true;
        }

        private void FixedUpdate()
        {
            if (!_active) return;

            float step = speed * Time.fixedDeltaTime;
            if (step >= _remainingRange)
            {
                // Reached the weapon's maximum range without hitting anything.
                Step(_remainingRange);
                Despawn();
                return;
            }

            if (Step(step)) return;
            _remainingRange -= step;
        }

        /// <summary>Moves one step, resolving anything in the way. Returns true if the arrow was consumed.</summary>
        private bool Step(float distance)
        {
            Vector2 origin = transform.position;
            int combinedMask = _targetMask | _blockingMask;

            RaycastHit2D hit = Physics2D.CircleCast(origin, sweepRadius, _direction, distance, combinedMask);
            if (hit.collider == null)
            {
                transform.position = origin + _direction * distance;
                return false;
            }

            // A wall stops the arrow dead - arrows never pass through level geometry.
            bool hitWall = (_blockingMask & (1 << hit.collider.gameObject.layer)) != 0;
            transform.position = hit.point;

            if (hitWall)
            {
                Despawn();
                return true;
            }

            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                DamageResult result = damageable.TakeDamage(_payload.AtPoint(hit.point, _direction));
                _listener?.OnDamageDealt(result);

                if (_pierceRemaining <= 0)
                {
                    Despawn();
                    return true;
                }

                _pierceRemaining--;
            }

            // Nudge past whatever we just hit so the next sweep does not re-detect it.
            transform.position = (Vector2)transform.position + _direction * (sweepRadius + 0.01f);
            return false;
        }

        private void Despawn()
        {
            _active = false;
            _listener = null;

            if (_pool != null) _pool.Release(this);
            else gameObject.SetActive(false);
        }

        private void OnDisable() => _active = false;
    }
}
