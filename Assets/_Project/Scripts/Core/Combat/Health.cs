using System;
using UnityEngine;
using RPG.Stats;

namespace RPG.Core.Combat
{
    /// <summary>
    /// Current hit points and the receiving end of every hit in the game - player, enemies,
    /// bosses and destructibles all use this same component.
    ///
    /// Max HP is never stored here; it is read from the attached ICombatStatProvider so that
    /// equipment, level-ups and buffs change max HP through the one stat pipeline. Only
    /// CURRENT hp lives in this component.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [Tooltip("Fallback max HP used only when no ICombatStatProvider is present.")]
        [SerializeField, Min(1f)] private float fallbackMaxHealth = 100f;

        [Tooltip("Log every hit to the Console. Development aid; turn off for real play.")]
        [SerializeField] private bool logDamage;

        private ICombatStatProvider _statProvider;
        private float _currentHealth;
        private float _lastKnownMaxHealth;
        private int _invulnerabilityHolds;

        /// <summary>
        /// While true every hit is reported as dodged. Counted rather than a bool, so two
        /// sources (a dash, a revive grace period) can overlap without one releasing the other's.
        /// </summary>
        public bool IsInvulnerable => _invulnerabilityHolds > 0;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _statProvider?.GetStat(StatType.MaxHealth) ?? fallbackMaxHealth;
        public float HealthFraction => MaxHealth > 0f ? Mathf.Clamp01(_currentHealth / MaxHealth) : 0f;
        public bool IsAlive => _currentHealth > 0f;
        public Transform Transform => transform;

        /// <summary>(current, max) after any change. UI listens here rather than polling.</summary>
        public event Action<float, float> HealthChanged;

        /// <summary>Raised on every hit that got through, including the details of the hit.</summary>
        public event Action<DamageInfo, DamageResult> DamageTaken;

        /// <summary>Raised once, when HP reaches zero. The killer is passed where known.</summary>
        public event Action<GameObject> Died;

        private void Awake()
        {
            _statProvider = GetComponent<ICombatStatProvider>();
            _lastKnownMaxHealth = MaxHealth;
            _currentHealth = _lastKnownMaxHealth;
        }

        private void Start()
        {
            // Safety net for component initialisation order: if the stat provider had not
            // produced a max HP yet during Awake, this object would have spawned at 0 HP.
            if (_currentHealth <= 0f && MaxHealth > 0f) ResetToFull();
            else HealthChanged?.Invoke(_currentHealth, MaxHealth);
        }

        private void Update()
        {
            // Max HP can change at any time (level-up, equipment swap). Gaining max HP grants
            // the same amount of current HP, so a level-up never leaves the player at a lower
            // percentage than before; losing it only clamps.
            float max = MaxHealth;
            if (Mathf.Approximately(max, _lastKnownMaxHealth)) return;

            float delta = max - _lastKnownMaxHealth;
            _lastKnownMaxHealth = max;

            _currentHealth = delta > 0f
                ? _currentHealth + delta
                : Mathf.Min(_currentHealth, max);

            HealthChanged?.Invoke(_currentHealth, max);
        }

        /// <summary>Adds one hold on invulnerability. Pair every call with ReleaseInvulnerability.</summary>
        public void HoldInvulnerability() => _invulnerabilityHolds++;

        public void ReleaseInvulnerability() => _invulnerabilityHolds = Mathf.Max(0, _invulnerabilityHolds - 1);

        public DamageResult TakeDamage(in DamageInfo info)
        {
            if (!IsAlive) return new DamageResult(0f, false, false);

            // Reported as a dodge on purpose: the MISS number and the absence of a recoil are
            // exactly the feedback a dodge roll through an attack should give.
            bool dodged = IsInvulnerable ||
                          (!info.IgnoresDodge && DamageCalculator.RollDodge(_statProvider));

            if (dodged)
            {
                var miss = new DamageResult(0f, true, false);
                DamageTaken?.Invoke(info, miss);
                if (logDamage) Debug.Log($"[Health] {name} dodged.", this);
                return miss;
            }

            float defense = _statProvider?.GetStat(StatType.Defense) ?? 0f;
            float mitigated = DamageCalculator.ApplyDefense(info.Amount, defense, info.DefensePenetration);

            // Overkill is trimmed so life steal cannot heal from damage that was never dealt.
            float applied = Mathf.Min(mitigated, _currentHealth);
            _currentHealth -= applied;

            bool fatal = _currentHealth <= 0f;
            var result = new DamageResult(applied, false, fatal);

            if (logDamage)
            {
                Debug.Log($"[Health] {name} took {applied:0.#} " +
                          $"(raw {info.Amount:0.#}, DEF {defense:0.#}{(info.IsCritical ? ", CRIT" : "")}). " +
                          $"HP {_currentHealth:0.#}/{MaxHealth:0.#}", this);
            }

            HealthChanged?.Invoke(_currentHealth, MaxHealth);
            DamageTaken?.Invoke(info, result);

            if (fatal) Died?.Invoke(info.Source);

            return result;
        }

        /// <summary>Restores HP, never above max. Used by life steal and potions later.</summary>
        public float Heal(float amount)
        {
            if (amount <= 0f || !IsAlive) return 0f;

            float max = MaxHealth;
            float healed = Mathf.Min(amount, max - _currentHealth);
            if (healed <= 0f) return 0f;

            _currentHealth += healed;
            HealthChanged?.Invoke(_currentHealth, max);
            return healed;
        }

        /// <summary>Full heal and revive. Used on respawn, stage restart and by debug tools.</summary>
        public void ResetToFull()
        {
            _lastKnownMaxHealth = MaxHealth;
            _currentHealth = _lastKnownMaxHealth;
            HealthChanged?.Invoke(_currentHealth, _lastKnownMaxHealth);
        }
    }
}
