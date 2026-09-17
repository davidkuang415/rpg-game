using System.Collections;
using UnityEngine;

namespace RPG.Core.Combat
{
    /// <summary>
    /// Hit reaction on the sprite itself: a bright tint the moment damage lands, brighter and
    /// longer on a critical.
    ///
    /// The flash is on the VICTIM, which is what makes a hit readable - you see the thing you
    /// hit react, not your own weapon. Impact sparks and damage numbers are separate and live
    /// in RPG.Vfx; this is only the tint.
    ///
    /// Note it never touches transform.scale. The SpriteRenderer sits on the same object as the
    /// collider, so a "punch" scale here would resize the character's hitbox mid-fight.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.08f;

        [Header("Critical hits")]
        [Tooltip("Tint used when the hit was a critical, so crits read differently at a glance.")]
        [SerializeField] private Color criticalFlashColor = new Color(1f, 0.72f, 0.35f);

        [Tooltip("Multiplies the flash duration on a critical.")]
        [SerializeField, Min(1f)] private float criticalDurationMultiplier = 2f;

        private SpriteRenderer _renderer;
        private Health _health;
        private Color _baseColor;
        private Coroutine _routine;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<Health>();
            _baseColor = _renderer.color;
        }

        private void OnEnable()
        {
            if (_health != null) _health.DamageTaken += OnDamageTaken;
        }

        private void OnDisable()
        {
            if (_health != null) _health.DamageTaken -= OnDamageTaken;

            // A pooled or disabled object must not come back still wearing the flash tint.
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
                if (_renderer != null) _renderer.color = _baseColor;
            }
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            if (result.WasDodged) return;

            // Re-read the base colour in case something else (class tint) changed it.
            if (_routine != null) StopCoroutine(_routine);
            else _baseColor = _renderer.color;

            Color color = info.IsCritical ? criticalFlashColor : flashColor;
            float duration = info.IsCritical ? flashDuration * criticalDurationMultiplier : flashDuration;

            _routine = StartCoroutine(Flash(color, duration));
        }

        private IEnumerator Flash(Color color, float duration)
        {
            _renderer.color = color;
            yield return new WaitForSeconds(duration);
            _renderer.color = _baseColor;
            _routine = null;
        }
    }
}
