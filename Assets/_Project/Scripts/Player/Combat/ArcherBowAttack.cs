using UnityEngine;
using RPG.Combat.Projectiles;
using RPG.Core.Combat;
using RPG.Items;

namespace RPG.Player.Combat
{
    /// <summary>
    /// The Archer's bow: fires a pooled arrow in the facing direction, with aim assistance.
    ///
    /// Aim assist, exactly as specified:
    ///   - look for enemies inside the bow's maximum range
    ///   - within +/- aimAssistAngle degrees of facing (default 15)
    ///   - with valid line of sight
    ///   - if any qualify, aim at the CLOSEST one; otherwise fire straight ahead
    ///
    /// Arrows are infinite - there is no ammunition - and walls stop them (handled by the
    /// projectile's sweep against the blocking layers).
    /// </summary>
    public class ArcherBowAttack : PlayerAttackBase
    {
        [Header("Bow")]
        [Tooltip("Direct reference, for objects placed in a scene by hand.")]
        [SerializeField] private ProjectilePool projectilePool;

        [Tooltip("Fallback used when this object is a spawned prefab and cannot reference a " +
                 "scene pool directly. The pool publishes itself into this asset at runtime.")]
        [SerializeField] private ProjectilePoolReference poolReference;

        [Tooltip("How far in front of the player the arrow spawns, so it clears the body collider.")]
        [SerializeField, Min(0f)] private float muzzleOffset = 0.55f;

        [Header("Aim Assist")]
        [Tooltip("Half-angle of the assist cone in degrees. The spec's +/-15 degrees.")]
        [SerializeField, Range(0f, 90f)] private float aimAssistAngle = 15f;

        [SerializeField] private bool aimAssistEnabled = true;

        [Header("Debug")]
        [SerializeField] private bool drawShotGizmo = true;
        [SerializeField, Min(0f)] private float gizmoDuration = 0.25f;

        public override WeaponType WeaponType => WeaponType.Bow;

        protected override void PerformAttack(Vector2 direction)
        {
            ProjectilePool pool = ResolvePool();
            if (pool == null)
            {
                Debug.LogError($"{nameof(ArcherBowAttack)} on '{name}' has no projectile pool " +
                               "(neither a direct reference nor a published pool reference).", this);
                return;
            }

            float range = AttackRange;
            Vector2 origin = (Vector2)transform.position + direction * muzzleOffset;
            Vector2 shotDirection = direction;

            if (aimAssistEnabled)
            {
                Collider2D target = CombatQueries.FindClosestInCone(
                    origin, direction, range, aimAssistAngle, targetLayers, blockingLayers);

                if (target != null)
                {
                    Vector2 toTarget = (Vector2)target.bounds.center - origin;
                    if (toTarget.sqrMagnitude > 0.0001f) shotDirection = toTarget.normalized;
                }
            }

            Projectile arrow = pool.Get();
            arrow.Launch(origin, shotDirection, range, BuildDamage(origin, shotDirection),
                targetLayers, blockingLayers, this);

            if (drawShotGizmo)
            {
                Debug.DrawRay(origin, shotDirection * range, Color.cyan, gizmoDuration);
            }

            if (logAttacks) Debug.Log($"[Archer] Fired along {shotDirection}.", this);
        }

        /// <summary>Direct scene reference wins; otherwise use whatever pool published itself.</summary>
        private ProjectilePool ResolvePool()
        {
            if (projectilePool != null) return projectilePool;
            return poolReference != null ? poolReference.Current : null;
        }
    }
}
