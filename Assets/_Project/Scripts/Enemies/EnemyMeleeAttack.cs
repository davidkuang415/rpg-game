using System.Collections.Generic;
using UnityEngine;
using RPG.Core.Combat;

namespace RPG.Enemies
{
    /// <summary>
    /// A close-range swing. Used by both Melee and Tank archetypes - they differ in their
    /// numbers, not their behaviour.
    ///
    /// Like the Knight's swing, it respects walls: an enemy cannot hit the player through
    /// level geometry just because they are close on the other side of it.
    /// </summary>
    public class EnemyMeleeAttack : EnemyAttackBase
    {
        [Header("Melee")]
        [Tooltip("Arc width in degrees, centred on the attack direction.")]
        [SerializeField, Range(10f, 360f)] private float arcDegrees = 110f;

        [Tooltip("Extra reach at the moment of impact, so a player who barely backed off " +
                 "is still clipped rather than the attack whiffing on a rounding error.")]
        [SerializeField, Min(0f)] private float rangePadding = 0.35f;

        private readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();

        protected override void Execute(Vector2 direction, Vector2 targetPosition)
        {
            Vector2 origin = transform.position;
            float range = AttackRange + rangePadding;
            float halfArc = arcDegrees * 0.5f;

            _hitThisSwing.Clear();

            List<Collider2D> candidates = CombatQueries.OverlapCircle(origin, range, targetLayers);

            for (int i = 0; i < candidates.Count; i++)
            {
                Collider2D candidate = candidates[i];
                if (candidate == null) continue;

                var damageable = candidate.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;
                if (_hitThisSwing.Contains(damageable)) continue;

                Vector2 hitPoint = candidate.ClosestPoint(origin);
                if (!CombatQueries.IsInsideCone(origin, direction, hitPoint, halfArc)) continue;
                if (!CombatQueries.HasLineOfSight(origin, hitPoint, blockingLayers)) continue;

                _hitThisSwing.Add(damageable);
                damageable.TakeDamage(BuildDamage(hitPoint, direction));
            }
        }
    }
}
