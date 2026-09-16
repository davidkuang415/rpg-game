using System;
using System.Collections;
using UnityEngine;
using RPG.Core.Combat;
using RPG.Stats;

namespace RPG.Enemies
{
    /// <summary>
    /// Shared attack machinery for enemies: cooldown timing, the telegraph windup, and the
    /// damage roll. Subclasses implement only the moment of impact.
    ///
    /// The windup is a deliberate design rule, not a delay for its own sake: an attack the
    /// player can see coming is one they can dodge or interrupt, which is what separates a
    /// fair fight from an unavoidable one. Bosses will use the same pattern at a larger scale.
    /// </summary>
    public abstract class EnemyAttackBase : MonoBehaviour
    {
        [Header("Targeting")]
        [Tooltip("Layers this attack can damage. Normally just Player.")]
        [SerializeField] protected LayerMask targetLayers;

        [Tooltip("Layers that block the attack. Normally just Wall.")]
        [SerializeField] protected LayerMask blockingLayers;

        [Header("Tuning")]
        [SerializeField, Min(0f)] protected float damageMultiplier = 1f;

        [Tooltip("Optional: tint flashed during the windup so the attack reads clearly.")]
        [SerializeField] private SpriteRenderer telegraphRenderer;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.9f, 0.3f);

        private readonly AttackCooldown _cooldown = new AttackCooldown();

        protected EnemyStats Stats { get; private set; }

        /// <summary>True while a windup is running - the brain holds still during this.</summary>
        public bool IsAttacking { get; private set; }

        public bool IsReady => _cooldown.IsReady && !IsAttacking;

        /// <summary>Reach in world units, from the enemy definition.</summary>
        public float AttackRange => Stats != null && Stats.Data != null ? Stats.Data.AttackRange : 1f;

        /// <summary>Raised when a windup starts. Animation and audio hook in here.</summary>
        public event Action<Vector2> AttackTelegraphed;

        protected virtual void Awake() => Stats = GetComponent<EnemyStats>();

        /// <summary>
        /// Starts an attack toward a world position, if off cooldown.
        /// Returns true if the attack began (not that it hit anything).
        /// </summary>
        public bool TryAttack(Vector2 targetPosition)
        {
            if (!isActiveAndEnabled || IsAttacking) return false;

            _cooldown.AttacksPerSecond = Stats != null ? Stats.GetStat(StatType.AttackSpeed) : 1f;
            if (!_cooldown.TryConsume()) return false;

            Vector2 direction = targetPosition - (Vector2)transform.position;
            if (direction.sqrMagnitude <= 0.0001f) direction = Vector2.down;

            StartCoroutine(AttackRoutine(direction.normalized, targetPosition));
            return true;
        }

        private IEnumerator AttackRoutine(Vector2 direction, Vector2 targetPosition)
        {
            IsAttacking = true;
            AttackTelegraphed?.Invoke(direction);

            float windup = Stats != null && Stats.Data != null ? Stats.Data.AttackWindupSeconds : 0f;

            Color? originalColor = null;
            if (telegraphRenderer != null && windup > 0f)
            {
                originalColor = telegraphRenderer.color;
                telegraphRenderer.color = telegraphColor;
            }

            if (windup > 0f) yield return new WaitForSeconds(windup);

            if (originalColor.HasValue) telegraphRenderer.color = originalColor.Value;

            // Aim is locked in at the START of the windup, so committing to an attack is a real
            // decision the enemy can be punished for - stepping aside actually works.
            if (isActiveAndEnabled) Execute(direction, targetPosition);

            IsAttacking = false;
        }

        /// <summary>The moment of impact. Melee sweeps, ranged fires.</summary>
        protected abstract void Execute(Vector2 direction, Vector2 targetPosition);

        /// <summary>Rolls this hit's damage from the enemy's stats, including its crit chance.</summary>
        protected DamageInfo BuildDamage(Vector2 hitPoint, Vector2 direction)
        {
            float amount = DamageCalculator.RollOutgoingDamage(Stats, damageMultiplier, out bool isCritical);
            return new DamageInfo(amount, isCritical, gameObject, hitPoint, direction);
        }

        /// <summary>Cancels an in-flight windup, e.g. on death.</summary>
        public void CancelAttack()
        {
            StopAllCoroutines();
            IsAttacking = false;
        }
    }
}
