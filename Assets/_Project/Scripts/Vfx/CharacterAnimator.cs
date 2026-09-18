using System.Collections;
using UnityEngine;
using RPG.Core;
using RPG.Core.Combat;
using RPG.Enemies;
using RPG.Player;
using RPG.Player.Combat;

namespace RPG.Vfx
{
    /// <summary>
    /// Procedural body animation for the placeholder characters: walk bob and lean, idle
    /// breathing, a lunge on attack, a recoil and squash on being hit, a pop on spawn and a
    /// collapse on death.
    ///
    /// Everything here moves the "Body" child, never the root. The root carries the collider
    /// and the rigidbody, so animating it would resize hitboxes and fight the physics step -
    /// the reason the sprite was moved off the root in the first place (see CharacterBody).
    ///
    /// One component serves the player and every enemy. It finds whichever movement, attack
    /// and health components the character happens to have and listens to those; anything
    /// missing is simply skipped. There is no per-frame allocation and no Animator asset.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [Header("Parts")]
        [Tooltip("The child holding the sprite. Left empty, the 'Body' child is used.")]
        [SerializeField] private Transform body;

        [Tooltip("Optional. Scaled with the body so the outline stays attached.")]
        [SerializeField] private Transform outline;

        [Tooltip("Optional. Squashes as the body rises, like a real drop shadow.")]
        [SerializeField] private Transform shadow;

        [Header("Walk")]
        [Tooltip("Bounces per second at full speed.")]
        [SerializeField, Min(0f)] private float bobFrequency = 5.5f;

        [Tooltip("How far the body rises on each bounce, in world units.")]
        [SerializeField, Min(0f)] private float bobHeight = 0.06f;

        [Tooltip("Degrees the body leans into its movement direction.")]
        [SerializeField, Min(0f)] private float leanDegrees = 9f;

        [Header("Idle")]
        [SerializeField, Min(0f)] private float breathFrequency = 1.2f;
        [SerializeField, Min(0f)] private float breathAmount = 0.025f;

        [Header("Hit")]
        [Tooltip("How far the body is knocked away from the hit, in world units.")]
        [SerializeField, Min(0f)] private float recoilDistance = 0.18f;

        [SerializeField, Min(0.01f)] private float recoilDuration = 0.14f;

        [Tooltip("Squash on impact: x stretches by this, y shrinks by it.")]
        [SerializeField, Range(0f, 0.5f)] private float squashAmount = 0.18f;

        [Header("Attack")]
        [Tooltip("How far the body lunges toward its target when an attack fires.")]
        [SerializeField, Min(0f)] private float lungeDistance = 0.22f;

        [SerializeField, Min(0.01f)] private float lungeDuration = 0.16f;

        [Tooltip("Enemies pull back by this fraction of the lunge during their windup, so the " +
                 "telegraph is a motion as well as a tint.")]
        [SerializeField, Range(0f, 1f)] private float windupPullback = 0.6f;

        [Header("Dash")]
        [Tooltip("Stretch along the dash direction: x grows by this, y shrinks by it.")]
        [SerializeField, Range(0f, 0.6f)] private float dashStretch = 0.26f;

        [SerializeField, Min(0.01f)] private float dashStretchDuration = 0.22f;

        [Header("Spawn and death")]
        [SerializeField, Min(0f)] private float spawnPopDuration = 0.22f;
        [SerializeField, Min(0f)] private float deathCollapseDuration = 0.4f;

        // Sources found on the character. Any may be null.
        private PlayerMotor _playerMotor;
        private EnemyMotor _enemyMotor;
        private Health _health;
        private PlayerAttackBase[] _playerAttacks;
        private PlayerDash _playerDash;
        private EnemyAttackBase _enemyAttack;
        private EnemyStats _enemyStats;

        private Vector3 _bodyRestPosition;
        private Vector3 _bodyRestScale;
        private Vector3 _outlineRestScale;
        private Vector3 _shadowRestScale;

        // Offsets layered onto the rest pose each frame. Kept separate so a hit during a lunge
        // does not have to know about the lunge - they just add.
        private Vector2 _impulseOffset;
        private Vector2 _impulseScale;      // additive: (0,0) = rest scale
        private float _bobPhase;
        private float _currentLean;
        private bool _dead;
        private bool _hasPlayedSpawn;

        private void Awake()
        {
            if (body == null) body = CharacterBody.FindAnimatable(gameObject);
            if (body == null)
            {
                Debug.LogWarning($"{nameof(CharacterAnimator)} on '{name}' has no Body child to animate.", this);
                enabled = false;
                return;
            }

            _bodyRestPosition = body.localPosition;
            _bodyRestScale = body.localScale;
            if (outline != null) _outlineRestScale = outline.localScale;
            if (shadow != null) _shadowRestScale = shadow.localScale;

            _playerMotor = GetComponent<PlayerMotor>();
            _enemyMotor = GetComponent<EnemyMotor>();
            _health = GetComponent<Health>();
            _playerAttacks = GetComponents<PlayerAttackBase>();
            _playerDash = GetComponent<PlayerDash>();
            _enemyAttack = GetComponent<EnemyAttackBase>();
            _enemyStats = GetComponent<EnemyStats>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.DamageTaken += OnDamageTaken;
                _health.Died += OnDied;
            }

            if (_playerAttacks != null)
            {
                for (int i = 0; i < _playerAttacks.Length; i++) _playerAttacks[i].Attacked += OnAttacked;
            }

            if (_playerDash != null) _playerDash.Dashed += OnDashed;

            if (_enemyAttack != null)
            {
                _enemyAttack.AttackTelegraphed += OnTelegraphed;
                _enemyAttack.AttackExecuted += OnAttacked;
            }

            _dead = false;
            _impulseOffset = Vector2.zero;
            _impulseScale = Vector2.zero;

            if (!_hasPlayedSpawn)
            {
                _hasPlayedSpawn = true;
                StartCoroutine(SpawnPop());
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.DamageTaken -= OnDamageTaken;
                _health.Died -= OnDied;
            }

            if (_playerAttacks != null)
            {
                for (int i = 0; i < _playerAttacks.Length; i++) _playerAttacks[i].Attacked -= OnAttacked;
            }

            if (_playerDash != null) _playerDash.Dashed -= OnDashed;

            if (_enemyAttack != null)
            {
                _enemyAttack.AttackTelegraphed -= OnTelegraphed;
                _enemyAttack.AttackExecuted -= OnAttacked;
            }

            StopAllCoroutines();
            ApplyPose(Vector2.zero, 0f, Vector2.zero, 0f);
        }

        // ------------------------------------------------------------------ per frame

        // Eight is far more than a character ever has running at once; the ninth simultaneous
        // reaction recycles the stalest slot rather than allocating.
        private readonly ActiveImpulse[] _impulses = new ActiveImpulse[8];

        private void LateUpdate()
        {
            if (_dead)
            {
                // The player is revived in place on a stage restart (Health.ResetToFull), with
                // no event to say so. Coming back to life is detected here instead.
                if (_health == null || !_health.IsAlive) return;

                StopAllCoroutines();
                ClearImpulses();
                _dead = false;
                _impulseOffset = Vector2.zero;
                _impulseScale = Vector2.zero;
                _currentLean = 0f;
                StartCoroutine(SpawnPop());
            }

            TickImpulses(Time.deltaTime);

            Vector2 velocity = CurrentVelocity();
            float speed = velocity.magnitude;
            float maxSpeed = MaxSpeed();
            float speedFraction = maxSpeed > 0f ? Mathf.Clamp01(speed / maxSpeed) : 0f;

            // Walk bob: a rectified sine so the body rises and lands rather than dipping below rest.
            float bob = 0f;
            if (speedFraction > 0.05f)
            {
                _bobPhase += Time.deltaTime * bobFrequency * Mathf.Lerp(0.6f, 1f, speedFraction) * Mathf.PI * 2f;
                bob = Mathf.Abs(Mathf.Sin(_bobPhase)) * bobHeight * speedFraction;
            }
            else
            {
                _bobPhase = 0f;
            }

            // Lean: tilt away from the direction of travel, like leaning into a run.
            float targetLean = speedFraction > 0.05f ? -Mathf.Sign(velocity.x) * leanDegrees * speedFraction : 0f;
            if (Mathf.Abs(velocity.x) < 0.05f) targetLean = 0f;
            _currentLean = Mathf.Lerp(_currentLean, targetLean, 1f - Mathf.Exp(-14f * Time.deltaTime));

            // Idle breathing: a gentle vertical scale that fades out as the character moves.
            float breath = Mathf.Sin(Time.time * breathFrequency * Mathf.PI * 2f) * breathAmount * (1f - speedFraction);

            ApplyPose(_impulseOffset + new Vector2(0f, bob), _currentLean,
                _impulseScale + new Vector2(-breath * 0.5f, breath), bob);
        }

        private Vector2 CurrentVelocity()
        {
            if (_playerMotor != null) return _playerMotor.CurrentVelocity;
            if (_enemyMotor != null) return _enemyMotor.CurrentVelocity;
            return Vector2.zero;
        }

        private float MaxSpeed()
        {
            if (_playerMotor != null) return _playerMotor.MoveSpeed;
            if (_enemyMotor != null) return _enemyMotor.MoveSpeed;
            return 1f;
        }

        private void ApplyPose(Vector2 offset, float leanDegreesNow, Vector2 scaleDelta, float lift)
        {
            if (body == null) return;

            body.localPosition = _bodyRestPosition + (Vector3)offset;
            body.localRotation = Quaternion.Euler(0f, 0f, leanDegreesNow);

            var scale = new Vector3(
                _bodyRestScale.x * (1f + scaleDelta.x),
                _bodyRestScale.y * (1f + scaleDelta.y),
                _bodyRestScale.z);
            body.localScale = scale;

            if (outline != null)
            {
                outline.localPosition = body.localPosition;
                outline.localRotation = body.localRotation;
                outline.localScale = new Vector3(
                    _outlineRestScale.x * (1f + scaleDelta.x),
                    _outlineRestScale.y * (1f + scaleDelta.y),
                    _outlineRestScale.z);
            }

            if (shadow != null)
            {
                // The shadow stays on the ground and shrinks slightly as the body lifts off it.
                float shrink = 1f - Mathf.Clamp01(lift / Mathf.Max(0.01f, bobHeight)) * 0.15f;
                shadow.localScale = new Vector3(_shadowRestScale.x * shrink, _shadowRestScale.y * shrink,
                    _shadowRestScale.z);
            }
        }

        // ------------------------------------------------------------------ events

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            if (result.WasDodged || _dead) return;

            Vector2 away = info.Direction.sqrMagnitude > 0.0001f ? info.Direction.normalized : Vector2.up;
            AddImpulse(away * recoilDistance, new Vector2(squashAmount, -squashAmount),
                recoilDuration);
        }

        private void OnAttacked(Vector2 direction)
        {
            if (_dead) return;

            Vector2 toward = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
            AddImpulse(toward * lungeDistance, new Vector2(0.08f, -0.06f), lungeDuration);
        }

        private void OnDashed(Vector2 direction)
        {
            if (_dead) return;

            // The body is scaled in its own x/y, not along the dash, so a vertical dash reads as
            // a tall stretch and a horizontal one as a long stretch - both read as "fast".
            bool horizontal = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);
            Vector2 stretch = horizontal
                ? new Vector2(dashStretch, -dashStretch * 0.6f)
                : new Vector2(-dashStretch * 0.6f, dashStretch);

            AddImpulse(direction.normalized * 0.08f, stretch, dashStretchDuration);
        }

        private void OnTelegraphed(Vector2 direction)
        {
            if (_dead || _enemyAttack == null) return;

            Vector2 away = direction.sqrMagnitude > 0.0001f ? -direction.normalized : Vector2.up;
            float windup = _enemyStats != null && _enemyStats.Data != null
                ? _enemyStats.Data.AttackWindupSeconds
                : 0.3f;

            AddImpulse(away * lungeDistance * windupPullback, new Vector2(-0.06f, 0.1f),
                Mathf.Max(0.05f, windup), holdAtPeak: true);
        }

        private void OnDied(GameObject killer)
        {
            if (_dead) return;
            _dead = true;

            StopAllCoroutines();
            StartCoroutine(DeathCollapse());
        }

        // ------------------------------------------------------------------ routines

        /// <summary>
        /// Out fast, back slow. With holdAtPeak the pose is reached and kept until the
        /// duration ends (a windup), otherwise it peaks a third of the way in and eases home.
        /// </summary>
        /// <summary>
        /// Starts a squash/recoil/lunge without allocating.
        ///
        /// This used to be a coroutine per event. A Knight swing lands on every enemy in a 120
        /// degree cone, so one attack into five enemies allocated five iterators plus Unity's
        /// five coroutine wrappers - sustained GC pressure on exactly the frames the player is
        /// watching. The state is a few floats, so it lives in a fixed array and is ticked from
        /// LateUpdate instead.
        /// </summary>
        private void AddImpulse(Vector2 offset, Vector2 scale, float duration, bool holdAtPeak = false)
        {
            if (duration <= 0f) return;

            int slot = -1;
            float oldest = -1f;

            for (int i = 0; i < _impulses.Length; i++)
            {
                if (!_impulses[i].Active)
                {
                    slot = i;
                    break;
                }

                // Every slot busy: the one closest to finishing is recycled, so a burst of hits
                // drops the stalest reaction rather than being ignored.
                float progress = _impulses[i].Elapsed / _impulses[i].Duration;
                if (progress <= oldest) continue;

                oldest = progress;
                slot = i;
            }

            if (slot < 0) return;

            // Reusing a live slot means backing out its current contribution first, or the pose
            // keeps an offset that nothing will ever remove.
            if (_impulses[slot].Active) RemoveContribution(ref _impulses[slot]);

            _impulses[slot] = new ActiveImpulse
            {
                Offset = offset,
                Scale = scale,
                Duration = duration,
                Elapsed = 0f,
                HoldAtPeak = holdAtPeak,
                Active = true
            };
        }

        private void TickImpulses(float deltaTime)
        {
            for (int i = 0; i < _impulses.Length; i++)
            {
                if (!_impulses[i].Active) continue;

                _impulses[i].Elapsed += deltaTime;
                float t = Mathf.Clamp01(_impulses[i].Elapsed / _impulses[i].Duration);

                float weight = _impulses[i].HoldAtPeak
                    ? Mathf.Clamp01(t * 4f)                                     // reach fast, then hold
                    : (t < 0.33f ? t / 0.33f : 1f - (t - 0.33f) / 0.67f);       // spike, then ease home
                if (!_impulses[i].HoldAtPeak) weight = Mathf.SmoothStep(0f, 1f, weight);

                Vector2 newOffset = _impulses[i].Offset * weight;
                Vector2 newScale = _impulses[i].Scale * weight;

                // Applied as deltas, so this composes with the other writers of these two
                // accumulators (the spawn pop) instead of overwriting them.
                _impulseOffset += newOffset - _impulses[i].LastOffset;
                _impulseScale += newScale - _impulses[i].LastScale;
                _impulses[i].LastOffset = newOffset;
                _impulses[i].LastScale = newScale;

                if (t < 1f) continue;

                RemoveContribution(ref _impulses[i]);
                _impulses[i].Active = false;
            }
        }

        private void RemoveContribution(ref ActiveImpulse impulse)
        {
            _impulseOffset -= impulse.LastOffset;
            _impulseScale -= impulse.LastScale;
            impulse.LastOffset = Vector2.zero;
            impulse.LastScale = Vector2.zero;
        }

        private void ClearImpulses()
        {
            for (int i = 0; i < _impulses.Length; i++) _impulses[i] = default;
        }

        /// <summary>One running squash/recoil/lunge. Plain data, deliberately not a coroutine.</summary>
        private struct ActiveImpulse
        {
            public Vector2 Offset;
            public Vector2 Scale;
            public Vector2 LastOffset;
            public Vector2 LastScale;
            public float Duration;
            public float Elapsed;
            public bool HoldAtPeak;
            public bool Active;
        }

        private IEnumerator SpawnPop()
        {
            if (spawnPopDuration <= 0f) yield break;

            float elapsed = 0f;
            Vector2 last = Vector2.zero;

            while (elapsed < spawnPopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / spawnPopDuration);

                // Overshoot: grows past full size then settles, which reads as "arrived".
                float s = 1f + 0.35f * Mathf.Sin(t * Mathf.PI) - (1f - t) * 0.9f;
                Vector2 delta = new Vector2(s - 1f, s - 1f);

                _impulseScale += delta - last;
                last = delta;
                yield return null;
            }

            _impulseScale -= last;
        }

        private IEnumerator DeathCollapse()
        {
            float elapsed = 0f;
            Vector2 startOffset = _impulseOffset;
            float startLean = _currentLean;

            while (elapsed < deathCollapseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / deathCollapseDuration);
                float eased = 1f - (1f - t) * (1f - t);

                // Flatten and sink: x widens a little, y collapses, the body tips over.
                var scale = new Vector2(0.25f * eased, -0.85f * eased);
                Vector2 offset = Vector2.Lerp(startOffset, new Vector2(0f, -0.12f), eased);
                float lean = Mathf.Lerp(startLean, 28f, eased);

                ApplyPose(offset, lean, scale, 0f);
                yield return null;
            }
        }
    }
}
