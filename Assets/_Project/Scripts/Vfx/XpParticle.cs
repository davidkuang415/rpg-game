using UnityEngine;
using RPG.Core.Pooling;

namespace RPG.Vfx
{
    /// <summary>
    /// A single XP mote that scatters outward from a kill and then homes in on the player.
    ///
    /// Purely cosmetic. The design spec is explicit that XP awarding must not depend on the
    /// particle arriving - the XP is already banked by the time this spawns, so a particle
    /// that is culled, interrupted or never seen costs the player nothing.
    /// </summary>
    public class XpParticle : MonoBehaviour
    {
        [Header("Flight")]
        [SerializeField, Min(0.05f)] private float travelDuration = 0.75f;

        [Tooltip("How far the mote flings outward before curving back to the player.")]
        [SerializeField, Min(0f)] private float scatterDistance = 1.2f;

        [Tooltip("Eased 0..1 progress along the flight path.")]
        [SerializeField]
        private AnimationCurve travelEase = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0.4f), new Keyframe(1f, 1f, 2.2f, 0f));

        [Header("Look")]
        [SerializeField] private float spinDegreesPerSecond = 180f;
        [SerializeField, Min(0f)] private float shrinkStartProgress = 0.75f;

        private XpParticlePool _pool;
        private Transform _target;
        private Vector3 _startPosition;
        private Vector3 _controlPoint;
        private Vector3 _baseScale;
        private float _elapsed;
        private float _startDelay;
        private bool _flying;

        public void BindPool(XpParticlePool pool) => _pool = pool;

        private void Awake() => _baseScale = transform.localScale;

        /// <summary>Starts the flight. <paramref name="startDelay"/> staggers a burst of motes.</summary>
        public void Launch(Vector3 origin, Transform target, float startDelay = 0f)
        {
            _target = target;
            _startPosition = origin;
            _elapsed = 0f;
            _startDelay = startDelay;
            _flying = true;

            transform.position = origin;
            transform.localScale = _baseScale;

            // Random outward fling, so a burst spreads instead of overlapping into one dot.
            Vector2 scatter = Random.insideUnitCircle.normalized * scatterDistance;
            _controlPoint = origin + new Vector3(scatter.x, scatter.y, 0f);

            // Nothing to fly to (player dead or not registered): do not linger on screen.
            if (_target == null) Release();
        }

        private void Update()
        {
            if (!_flying) return;

            if (_startDelay > 0f)
            {
                _startDelay -= Time.deltaTime;
                return;
            }

            if (_target == null)
            {
                Release();
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / travelDuration);
            float eased = travelEase.Evaluate(t);

            // Quadratic bezier, re-evaluated against the player's CURRENT position each frame,
            // so motes track a moving target instead of flying to where they were released.
            Vector3 destination = _target.position;
            Vector3 a = Vector3.Lerp(_startPosition, _controlPoint, eased);
            Vector3 b = Vector3.Lerp(_controlPoint, destination, eased);
            transform.position = Vector3.Lerp(a, b, eased);

            if (spinDegreesPerSecond != 0f)
            {
                transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
            }

            if (t >= shrinkStartProgress && shrinkStartProgress < 1f)
            {
                float shrink = Mathf.InverseLerp(shrinkStartProgress, 1f, t);
                transform.localScale = _baseScale * (1f - shrink);
            }

            if (t >= 1f) Release();
        }

        private void Release()
        {
            _flying = false;
            transform.localScale = _baseScale;

            if (_pool != null) _pool.Release(this);
            else gameObject.SetActive(false);
        }

        private void OnDisable() => _flying = false;
    }
}
