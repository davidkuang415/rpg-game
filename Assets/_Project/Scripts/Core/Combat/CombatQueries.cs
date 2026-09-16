using System.Collections.Generic;
using UnityEngine;

namespace RPG.Core.Combat
{
    /// <summary>
    /// Shared spatial queries for combat: line of sight and cone searches.
    ///
    /// The Knight's swing, the Archer's aim assist and (in the next phase) enemy vision all
    /// answer the same question - "can I actually see that, or is a wall in the way?" - so the
    /// answer lives in one place. Buffers are static and reused; these run every attack.
    /// </summary>
    public static class CombatQueries
    {
        private static readonly List<Collider2D> OverlapBuffer = new List<Collider2D>(32);

        /// <summary>True if nothing on <paramref name="blockingMask"/> sits between the two points.</summary>
        public static bool HasLineOfSight(Vector2 from, Vector2 to, int blockingMask)
        {
            Vector2 delta = to - from;
            float distance = delta.magnitude;
            if (distance <= 0.0001f) return true;

            return Physics2D.Raycast(from, delta / distance, distance, blockingMask).collider == null;
        }

        /// <summary>
        /// Collects colliders on <paramref name="targetMask"/> within radius.
        /// The returned list is a shared buffer - consume it before calling again.
        /// </summary>
        public static List<Collider2D> OverlapCircle(Vector2 origin, float radius, int targetMask)
        {
            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = targetMask,
                useTriggers = true
            };

            OverlapBuffer.Clear();
            Physics2D.OverlapCircle(origin, radius, filter, OverlapBuffer);
            return OverlapBuffer;
        }

        /// <summary>Is <paramref name="point"/> inside a cone of the given half-angle around facing?</summary>
        public static bool IsInsideCone(Vector2 origin, Vector2 facing, Vector2 point, float halfAngleDegrees)
        {
            Vector2 toPoint = point - origin;
            if (toPoint.sqrMagnitude <= 0.0001f) return true;
            return Vector2.Angle(facing, toPoint) <= halfAngleDegrees;
        }

        /// <summary>
        /// Closest collider inside a cone that also has line of sight. Used by the Archer's
        /// aim assist; returns null when nothing qualifies, so the caller fires straight ahead.
        /// </summary>
        public static Collider2D FindClosestInCone(Vector2 origin, Vector2 facing, float maxRange,
            float halfAngleDegrees, int targetMask, int blockingMask)
        {
            List<Collider2D> candidates = OverlapCircle(origin, maxRange, targetMask);

            Collider2D best = null;
            float bestDistanceSquared = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                Collider2D candidate = candidates[i];
                if (candidate == null) continue;

                // Aim at the collider's centre, but test reach and sight from its nearest point,
                // so a large enemy peeking around a corner is still a valid target.
                Vector2 targetPoint = candidate.bounds.center;
                if (!IsInsideCone(origin, facing, targetPoint, halfAngleDegrees)) continue;

                float distanceSquared = ((Vector2)candidate.bounds.center - origin).sqrMagnitude;
                if (distanceSquared >= bestDistanceSquared) continue;
                if (!HasLineOfSight(origin, targetPoint, blockingMask)) continue;

                best = candidate;
                bestDistanceSquared = distanceSquared;
            }

            return best;
        }
    }
}
