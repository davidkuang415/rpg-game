using UnityEngine;
using RPG.Core.Combat;

namespace RPG.Vfx
{
    /// <summary>
    /// Turns "this thing was damaged" into visible feedback.
    ///
    /// It listens to the VICTIM's Health rather than living on the attacker, which is why one
    /// component covers every source of damage there will ever be - sword swings, arrows,
    /// enemy bolts, and later hazards and boss attacks - without any of them knowing that
    /// feedback exists. Nothing in the attack code had to change to get hit sparks.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HitFeedbackEmitter : MonoBehaviour
    {
        [SerializeField] private CombatFeedbackChannel feedback;

        [Header("What to show")]
        [SerializeField] private bool showSparks = true;
        [SerializeField] private bool showDamageNumbers = true;

        [Tooltip("Show a MISS number when an attack is dodged.")]
        [SerializeField] private bool showDodges = true;

        [Header("Placement")]
        [Tooltip("Where the damage number appears, relative to this object's centre.")]
        [SerializeField] private Vector2 numberOffset = new Vector2(0f, 0.6f);

        [Tooltip("A reported hit point further than this from the target is treated as bad data " +
                 "and replaced with the target's centre.")]
        [SerializeField, Min(0.5f)] private float maxHitPointDistance = 4f;

        private Health _health;

        private void Awake() => _health = GetComponent<Health>();

        private void OnEnable()
        {
            if (_health != null) _health.DamageTaken += OnDamageTaken;
        }

        private void OnDisable()
        {
            if (_health != null) _health.DamageTaken -= OnDamageTaken;
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            if (feedback == null) return;

            Vector2 center = transform.position;
            Vector2 point = ResolveHitPoint(info, center);

            if (result.WasDodged)
            {
                if (showDodges && showDamageNumbers)
                {
                    feedback.SpawnDamageNumber(center + numberOffset, 0f, false, dodged: true);
                }
                return;
            }

            if (showSparks) feedback.SpawnHitSpark(point, info.Direction, info.IsCritical);

            if (showDamageNumbers)
            {
                feedback.SpawnDamageNumber(center + numberOffset, result.DamageDealt,
                    info.IsCritical, dodged: false);
            }
        }

        /// <summary>
        /// Guards against damage sources that never filled in a hit point. A spark drawn at the
        /// world origin would be worse than no spark at all.
        /// </summary>
        private Vector2 ResolveHitPoint(in DamageInfo info, Vector2 center)
        {
            Vector2 reported = info.HitPoint;
            if (reported == Vector2.zero) return center;

            return Vector2.Distance(reported, center) > maxHitPointDistance ? center : reported;
        }
    }
}
