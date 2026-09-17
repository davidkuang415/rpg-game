using UnityEngine;

namespace RPG.Vfx
{
    /// <summary>
    /// The flash of impact drawn where a hit lands: a sprite that expands and fades over a
    /// fraction of a second, then returns itself to the pool.
    ///
    /// Purely visual, and deliberately so. It is never told who hit whom, only where the hit
    /// happened and whether it crit, so no combat rule can ever end up hiding in an effect.
    /// </summary>
    public class HitSpark : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer sprite;

        [Header("Timing")]
        [Tooltip("Seconds from impact to fully faded.")]
        [SerializeField, Min(0.02f)] private float lifetime = 0.18f;

        [Header("Shape")]
        [SerializeField, Min(0f)] private float startScale = 0.45f;
        [SerializeField, Min(0f)] private float endScale = 1.15f;

        [Tooltip("How far the spark drifts along the hit direction over its lifetime.")]
        [SerializeField] private float driftDistance = 0.25f;

        [Header("Colour")]
        [SerializeField] private Color normalColor = new Color(1f, 0.95f, 0.75f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.55f, 0.2f);

        [Tooltip("Crits use a bigger spark, so a crit reads differently at a glance.")]
        [SerializeField, Min(1f)] private float criticalScaleMultiplier = 1.6f;

        private HitSparkPool _pool;
        private Vector2 _origin;
        private Vector2 _drift;
        private float _elapsed;
        private float _scaleFrom;
        private float _scaleTo;
        private Color _color;
        private bool _playing;

        public void BindPool(HitSparkPool pool) => _pool = pool;

        public void Play(Vector2 point, Vector2 direction, bool critical)
        {
            float sizeBoost = critical ? criticalScaleMultiplier : 1f;

            _origin = point;
            _drift = direction.sqrMagnitude > 0.0001f ? direction.normalized * driftDistance : Vector2.zero;
            _scaleFrom = startScale * sizeBoost;
            _scaleTo = endScale * sizeBoost;
            _color = critical ? criticalColor : normalColor;
            _elapsed = 0f;
            _playing = true;

            // A random spin stops repeated hits on the same enemy from looking like one
            // stuttering sprite.
            transform.position = _origin;
            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            transform.localScale = Vector3.one * _scaleFrom;

            if (sprite != null) sprite.color = _color;
        }

        private void Update()
        {
            if (!_playing) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / lifetime);

            transform.position = _origin + _drift * t;
            transform.localScale = Vector3.one * Mathf.Lerp(_scaleFrom, _scaleTo, t);

            if (sprite != null)
            {
                Color color = _color;
                // Squared falloff: bright for most of the life, then gone quickly, which reads
                // as a snap rather than a slow smear.
                color.a = _color.a * (1f - t) * (1f - t);
                sprite.color = color;
            }

            if (t >= 1f) Despawn();
        }

        private void Despawn()
        {
            _playing = false;

            if (_pool != null) _pool.Release(this);
            else gameObject.SetActive(false);
        }

        private void OnDisable() => _playing = false;
    }
}
