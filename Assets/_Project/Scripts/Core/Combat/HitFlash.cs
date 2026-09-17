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
    /// It only ever changes colour. Movement - recoil, squash - is CharacterAnimator's job,
    /// and it works on the "Body" child so the collider is never touched.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        [Tooltip("The sprite to tint. Left empty, the root's renderer or the 'Body' child is used.")]
        [SerializeField] private SpriteRenderer bodyRenderer;

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
            _renderer = bodyRenderer != null ? bodyRenderer : CharacterBody.FindRenderer(gameObject);
            _health = GetComponent<Health>();

            if (_renderer == null)
            {
                Debug.LogWarning($"{nameof(HitFlash)} on '{name}' found no SpriteRenderer to flash.", this);
                enabled = false;
                return;
            }

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
            if (result.WasDodged || _renderer == null) return;

            // Re-read the base colour in case something else (class tint) changed it.
            if (_routine != null) StopCoroutine(_routine);
            else _baseColor = _renderer.color;

            Color color = info.IsCritical ? criticalFlashColor : flashColor;
            float duration = info.IsCritical ? flashDuration * criticalDurationMultiplier : flashDuration;

            _routine = StartCoroutine(Flash(color, duration));
        }

        private IEnumerator Flash(Color color, float duration)
        {
            // Alpha is left alone both ways, so a fatal hit's flash cannot snap a fading
            // corpse back to fully opaque when it ends.
            _renderer.color = WithAlpha(color, _renderer.color.a);
            yield return new WaitForSeconds(duration);
            _renderer.color = WithAlpha(_baseColor, _renderer.color.a);
            _routine = null;
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha);
    }
}
