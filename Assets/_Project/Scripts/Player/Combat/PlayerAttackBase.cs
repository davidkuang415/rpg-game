using System;
using UnityEngine;
using RPG.Core.Combat;
using RPG.Inventory;
using RPG.Items;
using RPG.Stats;

namespace RPG.Player.Combat
{
    /// <summary>
    /// Shared machinery for every class's basic attack: cooldown timing, damage rolls,
    /// life steal and the layer masks combat queries run against.
    ///
    /// Subclasses implement only what is actually different - the shape of the attack.
    /// KnightSwordAttack sweeps an arc; ArcherBowAttack fires a projectile. Neither repeats
    /// the timing or damage code, which is the "no duplicated attack speed logic" rule from
    /// the design spec.
    ///
    /// Deliberately NOT an IPlayerAttack: the router is the single entry point, so the player
    /// controller can never accidentally bind to one class's weapon directly.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerFacing))]
    public abstract class PlayerAttackBase : MonoBehaviour, IDamageDealtListener
    {
        [Header("Targeting")]
        [Tooltip("Layers this attack can damage. Normally just Enemy.")]
        [SerializeField] protected LayerMask targetLayers;

        [Tooltip("Layers that block the attack. Normally just Wall.")]
        [SerializeField] protected LayerMask blockingLayers;

        [Header("Tuning")]
        [Tooltip("Scales ATK for this weapon behaviour. 1 = exactly the player's ATK per hit.")]
        [SerializeField, Min(0f)] protected float damageMultiplier = 1f;

        [Tooltip("Overrides the class's base attack range. 0 = use the class value.")]
        [SerializeField, Min(0f)] protected float rangeOverride;

        [SerializeField] protected bool logAttacks;

        // Plain runtime state, not serialized: the rate is driven from the stat pipeline on
        // every attack, so there is nothing here for a designer to tune in the Inspector.
        private readonly AttackCooldown _cooldown = new AttackCooldown();

        protected PlayerStats Stats { get; private set; }
        protected PlayerFacing Facing { get; private set; }
        protected Health OwnHealth { get; private set; }
        protected EquipmentManager Equipment { get; private set; }

        /// <summary>The equipped weapon's template, or null when fighting unarmed.</summary>
        protected WeaponDefinition EquippedWeapon => Equipment != null ? Equipment.EquippedWeapon : null;

        /// <summary>Which weapon category this behaviour represents. The router matches it to the class.</summary>
        public abstract WeaponType WeaponType { get; }

        /// <summary>Raised when an attack actually fires. Animation, audio and VFX hook in here.</summary>
        public event Action<Vector2> Attacked;

        public bool CanAttack => isActiveAndEnabled && _cooldown.IsReady;

        /// <summary>The reach this attack currently uses, for visuals that must match it.</summary>
        public float CurrentRange => AttackRange;

        /// <summary>
        /// Reach in world units. Precedence: explicit override, then the equipped weapon, then
        /// the class's unarmed fallback.
        /// </summary>
        protected float AttackRange
        {
            get
            {
                if (rangeOverride > 0f) return rangeOverride;

                WeaponDefinition weapon = EquippedWeapon;
                if (weapon != null) return weapon.Range;

                return Stats != null && Stats.CurrentClass != null ? Stats.CurrentClass.BaseAttackRange : 1.5f;
            }
        }

        protected virtual void Awake()
        {
            Stats = GetComponent<PlayerStats>();
            Facing = GetComponent<PlayerFacing>();
            OwnHealth = GetComponent<Health>();
            Equipment = GetComponent<EquipmentManager>();
        }

        /// <summary>Runs the cooldown check, then hands off to the subclass.</summary>
        public bool TryAttack(Vector2 direction)
        {
            // Attack speed is read from the stat pipeline at the moment of the attack, so a
            // weapon swap or buff applies immediately without anything having to push a value.
            _cooldown.AttacksPerSecond = Stats.Current.AttackSpeed;

            if (!_cooldown.TryConsume()) return false;

            Vector2 resolved = direction.sqrMagnitude > 0.0001f ? direction.normalized : Facing.Facing;
            PerformAttack(resolved);
            Attacked?.Invoke(resolved);
            return true;
        }

        /// <summary>The part that differs per class.</summary>
        protected abstract void PerformAttack(Vector2 direction);

        /// <summary>Rolls this hit's damage (including crit) from the current stat block.</summary>
        protected DamageInfo BuildDamage(Vector2 hitPoint, Vector2 direction)
        {
            float amount = DamageCalculator.RollOutgoingDamage(Stats, damageMultiplier, out bool isCritical);
            float penetration = Stats.GetStat(StatType.DefensePenetration);

            return new DamageInfo(amount, isCritical, gameObject, hitPoint, direction, penetration);
        }

        /// <summary>Applies life steal from damage actually dealt. Called by attacks and projectiles.</summary>
        public void OnDamageDealt(in DamageResult result)
        {
            if (result.DamageDealt <= 0f || OwnHealth == null) return;

            float healing = DamageCalculator.CalculateLifeSteal(Stats, result.DamageDealt);
            if (healing > 0f) OwnHealth.Heal(healing);
        }

        protected void ResetCooldown() => _cooldown.Reset();
    }
}
