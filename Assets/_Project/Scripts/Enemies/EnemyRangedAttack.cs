using UnityEngine;
using RPG.Combat.Projectiles;

namespace RPG.Enemies
{
    /// <summary>
    /// Fires a pooled projectile at the player's position.
    ///
    /// No aim assist - that is a player convenience, not an enemy one. The shot is aimed where
    /// the player was when the windup started, so moving during the telegraph avoids it.
    ///
    /// Walls stop the projectile (the projectile sweeps against the blocking layers), and the
    /// brain refuses to start a shot without line of sight, so enemies never fire through walls.
    /// </summary>
    public class EnemyRangedAttack : EnemyAttackBase
    {
        [Header("Ranged")]
        [Tooltip("Direct reference, for objects placed in a scene by hand.")]
        [SerializeField] private ProjectilePool projectilePool;

        [Tooltip("Fallback used when this object is a spawned prefab and cannot reference a " +
                 "scene pool directly. The pool publishes itself into this asset at runtime.")]
        [SerializeField] private ProjectilePoolReference poolReference;

        [Tooltip("How far in front of the enemy the projectile spawns.")]
        [SerializeField, Min(0f)] private float muzzleOffset = 0.5f;

        [Tooltip("How far the projectile may travel. 0 = the enemy's attack range plus padding.")]
        [SerializeField, Min(0f)] private float projectileRangeOverride;

        private float ProjectileRange => projectileRangeOverride > 0f
            ? projectileRangeOverride
            : AttackRange * 1.4f;

        protected override void Execute(Vector2 direction, Vector2 targetPosition)
        {
            ProjectilePool pool = ResolvePool();
            if (pool == null)
            {
                Debug.LogError($"{nameof(EnemyRangedAttack)} on '{name}' has no projectile pool " +
                               "(neither a direct reference nor a published pool reference).", this);
                return;
            }

            Vector2 origin = (Vector2)transform.position + direction * muzzleOffset;

            Projectile projectile = pool.Get();
            projectile.Launch(origin, direction, ProjectileRange,
                BuildDamage(origin, direction), targetLayers, blockingLayers, null);
        }

        /// <summary>Direct scene reference wins; otherwise use whatever pool published itself.</summary>
        private ProjectilePool ResolvePool()
        {
            if (projectilePool != null) return projectilePool;
            return poolReference != null ? poolReference.Current : null;
        }
    }
}
