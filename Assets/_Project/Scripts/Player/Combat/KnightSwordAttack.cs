using System.Collections.Generic;
using UnityEngine;
using RPG.Core.Combat;
using RPG.Items;

namespace RPG.Player.Combat
{
    /// <summary>
    /// The Knight's sword swing: a cone centred on the facing direction.
    ///
    /// Design rules implemented here:
    ///  - 120 degree arc (from the class/weapon data, never hardcoded)
    ///  - every valid enemy inside the arc AND inside melee range is hit
    ///  - each enemy takes damage at most once per swing, even with multiple colliders
    ///  - walls block the swing: an enemy geometrically inside the arc but behind a wall
    ///    takes no damage
    /// </summary>
    public class KnightSwordAttack : PlayerAttackBase
    {
        [Header("Sword")]
        [Tooltip("Overrides the class's arc width. 0 = use the class value (120 degrees).")]
        [SerializeField, Range(0f, 360f)] private float arcOverride;

        [Header("Debug")]
        [SerializeField] private bool drawSwingGizmo = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField, Min(0f)] private float gizmoDuration = 0.15f;

        // Reused every swing so a hit never allocates. Also enforces "once per enemy per swing".
        private readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();

        public override WeaponType WeaponType => WeaponType.Sword;

        /// <summary>Total arc width in degrees: override, then equipped weapon, then class fallback.</summary>
        private float ArcDegrees
        {
            get
            {
                if (arcOverride > 0f) return arcOverride;

                WeaponDefinition weapon = EquippedWeapon;
                if (weapon != null && weapon.ArcDegrees > 0f) return weapon.ArcDegrees;

                return Stats != null && Stats.CurrentClass != null
                    ? Stats.CurrentClass.BaseAttackArcDegrees
                    : 120f;
            }
        }

        protected override void PerformAttack(Vector2 direction)
        {
            Vector2 origin = transform.position;
            float range = AttackRange;
            float halfArc = ArcDegrees * 0.5f;

            _hitThisSwing.Clear();

            List<Collider2D> candidates = CombatQueries.OverlapCircle(origin, range, targetLayers);
            int hitCount = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                Collider2D candidate = candidates[i];
                if (candidate == null) continue;

                var damageable = candidate.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                // One damage event per enemy per swing, no matter how many colliders it has.
                if (_hitThisSwing.Contains(damageable)) continue;

                // Nearest point rather than centre: a large enemy leaning into the arc counts.
                Vector2 hitPoint = candidate.ClosestPoint(origin);

                if (!CombatQueries.IsInsideCone(origin, direction, hitPoint, halfArc)) continue;
                if (!CombatQueries.HasLineOfSight(origin, hitPoint, blockingLayers)) continue;

                // Recorded only after passing every check, so a collider that failed the cone
                // test does not lock out another collider on the same enemy.
                _hitThisSwing.Add(damageable);

                DamageResult result = damageable.TakeDamage(BuildDamage(hitPoint, direction));
                OnDamageDealt(result);
                hitCount++;
            }

            if (drawSwingGizmo) DrawArc(origin, direction, range, halfArc);
            if (logAttacks) Debug.Log($"[Knight] Swing hit {hitCount} target(s).", this);
        }

        private void DrawArc(Vector2 origin, Vector2 direction, float range, float halfArc)
        {
            const int segments = 14;
            float centerAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            Vector2 previous = origin + AngleToVector(centerAngle - halfArc) * range;
            Debug.DrawLine(origin, previous, gizmoColor, gizmoDuration);

            for (int i = 1; i <= segments; i++)
            {
                float angle = centerAngle - halfArc + (halfArc * 2f) * (i / (float)segments);
                Vector2 point = origin + AngleToVector(angle) * range;
                Debug.DrawLine(previous, point, gizmoColor, gizmoDuration);
                previous = point;
            }

            Debug.DrawLine(origin, previous, gizmoColor, gizmoDuration);
        }

        private static Vector2 AngleToVector(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }
    }
}
