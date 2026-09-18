using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Core;
using RPG.Core.Combat;
using RPG.Stats;

namespace RPG.Enemies
{
    /// <summary>
    /// A second phase for a boss: below a health threshold it speeds up, hits harder and
    /// changes colour, so the last half of the fight is a different fight.
    ///
    /// Until now the Warlord was a Brute with bigger numbers, and a fight against bigger
    /// numbers is only longer, not harder. The phase change is what gives a boss fight a
    /// shape - the player learns its rhythm, then has to relearn it under pressure.
    ///
    /// Stats go through the same IStatModifierSource seam elites use, so nothing here touches
    /// the enemy's numbers directly, and a pooled boss is put back to phase one on every spawn.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemyStats))]
    public class BossEnrage : MonoBehaviour, IStatModifierSource
    {
        [Header("Trigger")]
        [Tooltip("Health fraction at or below which the boss enrages.")]
        [SerializeField, Range(0.05f, 0.95f)] private float threshold = 0.5f;

        [Header("Bonuses while enraged (fractions)")]
        [SerializeField, Range(0f, 2f)] private float attackBonus = 0.25f;
        [SerializeField, Range(0f, 2f)] private float attackSpeedBonus = 0.45f;
        [SerializeField, Range(0f, 2f)] private float moveSpeedBonus = 0.35f;

        [Header("Presentation")]
        [Tooltip("Tint the body takes while enraged. Multiplied onto the sprite.")]
        [SerializeField] private Color enragedTint = new Color(1f, 0.55f, 0.5f);

        [SerializeField] private bool logPhases = true;

        private Health _health;
        private EnemyStats _stats;
        private SpriteRenderer _body;
        private Color _bodyRestColor;
        private bool _hasBodyRestColor;

        public bool IsEnraged { get; private set; }

        /// <summary>Raised once when the phase changes. The boss bar and the sound layer listen.</summary>
        public event Action<BossEnrage> Enraged;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _stats = GetComponent<EnemyStats>();
            _body = CharacterBody.FindRenderer(gameObject);
        }

        private void OnEnable()
        {
            _health.HealthChanged += OnHealthChanged;
            _health.Died += OnDied;

            // A pooled boss re-enables in whatever state it died in. Start every life calm.
            ResetPhase();
        }

        private void OnDisable()
        {
            _health.HealthChanged -= OnHealthChanged;
            _health.Died -= OnDied;
            ResetPhase();
        }

        private void OnHealthChanged(float current, float max)
        {
            if (IsEnraged || !_health.IsAlive || max <= 0f) return;
            if (current / max > threshold) return;

            Enrage();
        }

        private void OnDied(GameObject killer) => ResetPhase();

        private void LateUpdate()
        {
            // HitFlash restores whatever colour it captured when the flash began. If a flash
            // was already running when the phase changed, that capture predates the enrage
            // tint, and the flash's end would quietly put the calm colour back. Re-assert it.
            if (!IsEnraged || _body == null || !_hasBodyRestColor) return;
            if (_body.color == _bodyRestColor) _body.color = WithAlpha(enragedTint, _body.color.a);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void Enrage()
        {
            IsEnraged = true;
            _stats.RegisterSource(this);

            if (_body != null)
            {
                // Captured now, not in Awake: HitFlash and the elite pass may have re-tinted the
                // body since, and the colour to return to is whatever it was just before this.
                _bodyRestColor = _body.color;
                _hasBodyRestColor = true;
                _body.color = WithAlpha(enragedTint, _body.color.a);
            }

            if (logPhases) Debug.Log($"[Boss] {name} is enraged.", this);
            Enraged?.Invoke(this);
        }

        private void ResetPhase()
        {
            if (!IsEnraged) return;

            IsEnraged = false;
            _stats.UnregisterSource(this);

            if (_body != null && _hasBodyRestColor) _body.color = _bodyRestColor;
        }

        public void CollectModifiers(List<StatModifier> results)
        {
            if (!IsEnraged) return;

            if (attackBonus > 0f) results.Add(StatModifier.Percent(StatType.Attack, attackBonus, StatLayer.Temporary));
            if (attackSpeedBonus > 0f) results.Add(StatModifier.Percent(StatType.AttackSpeed, attackSpeedBonus, StatLayer.Temporary));
            if (moveSpeedBonus > 0f) results.Add(StatModifier.Percent(StatType.MoveSpeed, moveSpeedBonus, StatLayer.Temporary));
        }
    }
}
