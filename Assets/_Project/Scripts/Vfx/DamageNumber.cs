using UnityEngine;

namespace RPG.Vfx
{
    /// <summary>
    /// A floating damage number.
    ///
    /// Drawn with TextMesh rather than a world-space Canvas: a Canvas per number would rebuild
    /// its mesh every frame it moves, which is exactly the per-frame cost the design spec asks
    /// us to avoid on mobile. TextMesh is a plain mesh renderer and costs nothing to move.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public class DamageNumber : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float lifetime = 0.7f;

        [Tooltip("Fraction of the lifetime spent fully opaque before the fade begins.")]
        [SerializeField, Range(0f, 1f)] private float holdFraction = 0.35f;

        [Header("Motion")]
        [SerializeField] private float riseDistance = 0.9f;

        [Tooltip("Sideways scatter, so several numbers at once do not stack into one blur.")]
        [SerializeField, Min(0f)] private float horizontalScatter = 0.35f;

        [Header("Appearance")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = new Color(1f, 0.6f, 0.15f);
        [SerializeField] private Color dodgeColor = new Color(0.7f, 0.8f, 0.95f);

        [SerializeField, Min(0.001f)] private float normalScale = 0.1f;
        [SerializeField, Min(1f)] private float criticalScaleMultiplier = 1.5f;

        [Tooltip("Extra size at the moment of impact, easing back to normal. Gives the number a pop.")]
        [SerializeField, Min(1f)] private float punchScale = 1.6f;

        [SerializeField] private string dodgeText = "MISS";

        private TextMesh _text;
        private DamageNumberPool _pool;
        private Vector3 _origin;
        private Vector3 _drift;
        private float _elapsed;
        private float _targetScale;
        private Color _color;
        private bool _playing;

        public void BindPool(DamageNumberPool pool) => _pool = pool;

        private void Awake() => _text = GetComponent<TextMesh>();

        public void Play(Vector2 point, float amount, bool critical, bool dodged)
        {
            if (_text == null) _text = GetComponent<TextMesh>();

            if (dodged)
            {
                _text.text = dodgeText;
                _color = dodgeColor;
                _targetScale = normalScale;
            }
            else
            {
                // Rounded up, never to zero: a hit that connected must never read as "0".
                _text.text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString();
                _color = critical ? criticalColor : normalColor;
                _targetScale = normalScale * (critical ? criticalScaleMultiplier : 1f);
            }

            _origin = point;
            _drift = new Vector3(Random.Range(-horizontalScatter, horizontalScatter), riseDistance, 0f);
            _elapsed = 0f;
            _playing = true;

            transform.position = _origin;
            transform.localScale = Vector3.one * (_targetScale * punchScale);
            _text.color = _color;
        }

        private void Update()
        {
            if (!_playing) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / lifetime);

            // Ease-out rise: fast off the target, slowing as it fades, which draws the eye to
            // the hit rather than to where the number ends up.
            float eased = 1f - (1f - t) * (1f - t);
            transform.position = _origin + _drift * eased;

            // The punch collapses over the first fifth of the life, then holds.
            float punchT = Mathf.Clamp01(_elapsed / (lifetime * 0.2f));
            transform.localScale = Vector3.one * Mathf.Lerp(_targetScale * punchScale, _targetScale, punchT);

            Color color = _color;
            color.a = t <= holdFraction
                ? 1f
                : 1f - Mathf.InverseLerp(holdFraction, 1f, t);
            _text.color = color;

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
