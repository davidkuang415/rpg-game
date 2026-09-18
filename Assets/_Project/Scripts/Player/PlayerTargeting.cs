using System.Collections.Generic;
using UnityEngine;
using RPG.Core;
using RPG.Core.Combat;

namespace RPG.Player
{
    /// <summary>
    /// Picks what the player is shooting at.
    ///
    /// This exists to break the link between "where I am walking" and "where I am aiming". With
    /// aim welded to movement you cannot back away from a melee enemy while shooting it, and
    /// you cannot circle a ranged one - the only way to point at something is to walk into it.
    /// That removes the entire positioning game.
    ///
    /// The search is deliberately NOT run every frame. Targets are re-evaluated on an interval
    /// and cached, because a physics overlap per frame per player is exactly the per-frame cost
    /// the design rules out on mobile - and a target that updates ten times a second is
    /// indistinguishable from one that updates ninety.
    /// </summary>
    public class PlayerTargeting : MonoBehaviour
    {
        [Header("Search")]
        [Tooltip("Layers that can be targeted. Normally just Enemy.")]
        [SerializeField] private LayerMask targetLayers;

        [Tooltip("Layers that block line of sight. Normally just Wall.")]
        [SerializeField] private LayerMask blockingLayers;

        [Tooltip("How far to look for a target, in world units. This is aim assist range, not " +
                 "weapon range - a target further than the weapon reaches is still worth facing.")]
        [SerializeField, Min(1f)] private float searchRadius = 12f;

        [Tooltip("Seconds between target searches. Small enough to feel instant, large enough " +
                 "that the physics query is not a per-frame cost.")]
        [SerializeField, Min(0.02f)] private float searchInterval = 0.1f;

        [Header("Behaviour")]
        [Tooltip("Require a clear line of sight. Off means the player will aim through walls.")]
        [SerializeField] private bool requireLineOfSight = true;

        [Tooltip("Bias toward whatever is already targeted, in world units. Stops the aim " +
                 "flicking between two enemies standing at almost the same distance.")]
        [SerializeField, Min(0f)] private float stickiness = 1.5f;

        private Transform _target;
        private float _nextSearchTime;

        /// <summary>The current target, or null when nothing is in range.</summary>
        public Transform Target => _target;

        public bool HasTarget => _target != null && _target.gameObject.activeInHierarchy;

        private void Reset()
        {
            targetLayers = LayerMask.GetMask(GameLayers.Enemy);
            blockingLayers = LayerMask.GetMask(GameLayers.Wall);
        }

        /// <summary>
        /// Direction to aim, falling back to the given direction when there is nothing to shoot.
        /// The fallback is normally the movement facing, so aiming at nothing still points the
        /// way the player is walking.
        /// </summary>
        public Vector2 GetAimDirection(Vector2 fallback)
        {
            RefreshIfDue();

            if (!HasTarget) return fallback;

            Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
            return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : fallback;
        }

        private void RefreshIfDue()
        {
            // A target that died or was despawned is dropped immediately rather than waiting
            // for the next scheduled search, so the player never keeps aiming at a corpse.
            if (_target != null && !IsValid(_target)) _target = null;

            if (Time.time < _nextSearchTime) return;
            _nextSearchTime = Time.time + searchInterval;

            _target = FindBest();
        }

        private Transform FindBest()
        {
            Vector2 origin = transform.position;
            List<Collider2D> candidates = CombatQueries.OverlapCircle(origin, searchRadius, targetLayers);

            Transform best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                Collider2D candidate = candidates[i];
                if (candidate == null) continue;

                var damageable = candidate.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                Transform candidateTransform = damageable.Transform;
                Vector2 point = candidate.ClosestPoint(origin);

                if (requireLineOfSight && !CombatQueries.HasLineOfSight(origin, point, blockingLayers))
                {
                    continue;
                }

                float distance = Vector2.Distance(origin, point);

                // The incumbent is measured as closer than it is, so it keeps the lock unless
                // something is meaningfully nearer.
                if (candidateTransform == _target) distance -= stickiness;

                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = candidateTransform;
            }

            return best;
        }

        private bool IsValid(Transform candidate)
        {
            if (!candidate.gameObject.activeInHierarchy) return false;

            var damageable = candidate.GetComponentInParent<IDamageable>();
            return damageable != null && damageable.IsAlive;
        }
    }
}
