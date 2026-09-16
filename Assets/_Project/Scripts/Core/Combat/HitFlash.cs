using System.Collections;
using UnityEngine;

namespace RPG.Core.Combat
{
    /// <summary>
    /// Minimal hit feedback: briefly tints the sprite when damage lands.
    ///
    /// Deliberately tiny - proper hit reactions, damage numbers and screen shake belong to the
    /// polish phase. This exists only so hits are visible while testing combat.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.08f;

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
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            if (result.WasDodged) return;

            // Re-read the base colour in case something else (class tint) changed it.
            if (_routine != null) StopCoroutine(_routine);
            else _baseColor = _renderer.color;

            _routine = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            _renderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            _renderer.color = _baseColor;
            _routine = null;
        }
    }
}
