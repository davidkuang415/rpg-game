using UnityEngine;
using RPG.Core.Combat;

namespace RPG.UI.HUD
{
    /// <summary>
    /// A health bar drawn in the world, under a character.
    ///
    /// Built from three sprites rather than a world-space Canvas. A Canvas per enemy would
    /// rebuild its mesh every time the enemy moves, and a room full of enemies would mean a
    /// room full of canvas rebuilds every frame - the exact per-frame cost the design spec
    /// rules out on mobile. Three SpriteRenderers cost nothing to move.
    ///
    /// It reads Health and nothing else. It never caches max HP, so a level-up or an equipment
    /// swap that changes max HP is reflected the moment Health reports it.
    /// </summary>
    public class HealthBarView : MonoBehaviour
    {
        [Header("Source")]
        [Tooltip("Left empty, the Health component is found on a parent. That is the normal " +
                 "case: this sits on a child object of the character.")]
        [SerializeField] private Health health;

        [Header("Style")]
        [SerializeField] private HealthBarStyle style;

        [Header("Parts")]
        [SerializeField] private SpriteRenderer background;

        [Tooltip("The lighter bar that lags behind after a hit. Optional.")]
        [SerializeField] private SpriteRenderer trail;

        [SerializeField] private SpriteRenderer fill;

        [Header("Behaviour")]
        [Tooltip("Cancels the parent's rotation each frame so the bar stays level. Only needed " +
                 "if the character itself rotates; it costs a LateUpdate, so leave it off.")]
        [SerializeField] private bool keepUpright;

        [Tooltip("Divides out the character's own scale, so a big enemy gets a bar the same " +
                 "physical size as everyone else's rather than a proportionally huge one.")]
        [SerializeField] private bool compensateParentScale = true;

        private float _fraction = 1f;
        private float _trailFraction = 1f;
        private float _trailHoldRemaining;
        private float _visibleHoldRemaining;
        private bool _visible = true;

        private void Awake()
        {
            if (health == null) health = GetComponentInParent<Health>();

            if (health == null)
            {
                Debug.LogError($"{nameof(HealthBarView)} on '{name}' found no Health to track.", this);
                enabled = false;
                return;
            }

            ApplyStyle();
        }

        private void OnEnable()
        {
            if (health == null) return;

            health.HealthChanged += OnHealthChanged;
            health.Died += OnDied;

            // Snap rather than animate on enable: a pooled enemy reused for a new fight must
            // start with a full bar, not finish the previous occupant's drain animation.
            SetFractionImmediate(health.HealthFraction);
        }

        private void OnDisable()
        {
            if (health == null) return;

            health.HealthChanged -= OnHealthChanged;
            health.Died -= OnDied;
        }

        private void OnHealthChanged(float current, float max)
        {
            float fraction = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (fraction < _fraction && style != null)
            {
                // Damage: leave the trail where it was and let it catch up, which is what makes
                // a small hit on a large health pool visible at all.
                _trailHoldRemaining = style.TrailHoldSeconds;
            }
            else if (fraction > _fraction)
            {
                // Healing: the trail has nothing to show, so it jumps straight to the new value.
                _trailFraction = fraction;
            }

            _fraction = fraction;
            _visibleHoldRemaining = style != null ? style.HideDelaySeconds : 0f;

            Redraw();
        }

        private void OnDied(GameObject killer)
        {
            SetFractionImmediate(0f);
            if (style != null && style.HideWhenDead) SetVisible(false);
        }

        private void SetFractionImmediate(float fraction)
        {
            _fraction = _trailFraction = Mathf.Clamp01(fraction);
            _trailHoldRemaining = 0f;
            _visibleHoldRemaining = 0f;
            Redraw();
        }

        private void Update()
        {
            if (_trailFraction > _fraction)
            {
                if (_trailHoldRemaining > 0f)
                {
                    _trailHoldRemaining -= Time.deltaTime;
                }
                else
                {
                    float drain = (style != null ? style.TrailDrainPerSecond : 1.2f) * Time.deltaTime;
                    _trailFraction = Mathf.Max(_fraction, _trailFraction - drain);
                    Redraw();
                }
            }

            if (_visibleHoldRemaining > 0f) _visibleHoldRemaining -= Time.deltaTime;

            UpdateVisibility();
        }

        private void LateUpdate()
        {
            if (keepUpright) transform.rotation = Quaternion.identity;
        }

        private void UpdateVisibility()
        {
            if (style == null) return;

            if (health != null && !health.IsAlive && style.HideWhenDead)
            {
                SetVisible(false);
                return;
            }

            bool atFull = _fraction >= 0.999f && _trailFraction >= 0.999f;
            bool shouldHide = style.HideWhenFull && atFull && _visibleHoldRemaining <= 0f;

            SetVisible(!shouldHide);
        }

        private void SetVisible(bool visible)
        {
            if (_visible == visible) return;
            _visible = visible;

            if (background != null) background.enabled = visible;
            if (trail != null) trail.enabled = visible;
            if (fill != null) fill.enabled = visible;
        }

        /// <summary>
        /// Positions and colours every part from the style asset. Public so the setup tool - and
        /// the Inspector, via OnValidate - can lay a bar out without entering play mode.
        /// </summary>
        public void ApplyStyle()
        {
            if (style == null) return;

            // The offset stays in the parent's space on purpose: a larger enemy is taller, so
            // its bar should sit proportionally lower. Only the bar's SIZE is normalised.
            transform.localPosition = style.Offset;
            transform.localScale = ParentScaleCompensation();

            Vector2 size = style.Size;
            float border = style.Border;

            if (background != null)
            {
                background.transform.localPosition = Vector3.zero;
                background.transform.localScale =
                    new Vector3(size.x + border * 2f, size.y + border * 2f, 1f);
                background.color = style.BackgroundColor;
                background.sortingOrder = style.SortingOrder;
            }

            if (trail != null)
            {
                trail.color = style.TrailColor;
                trail.sortingOrder = style.SortingOrder + 1;
            }

            if (fill != null)
            {
                fill.color = style.FillColor;
                fill.sortingOrder = style.SortingOrder + 2;
            }

            Redraw();
        }

        private void Redraw()
        {
            if (style == null) return;

            Vector2 size = style.Size;
            Stretch(trail, _trailFraction, size);
            Stretch(fill, _fraction, size);

            if (fill != null) fill.color = style.FillColorFor(_fraction);
        }

        private Vector3 ParentScaleCompensation()
        {
            if (!compensateParentScale || transform.parent == null) return Vector3.one;

            Vector3 parentScale = transform.parent.lossyScale;
            if (Mathf.Approximately(parentScale.x, 0f) || Mathf.Approximately(parentScale.y, 0f))
            {
                return Vector3.one;
            }

            return new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f);
        }

        /// <summary>
        /// Scales a centre-pivoted sprite to a fraction of the bar and slides it left, so the bar
        /// empties from the right instead of shrinking towards its middle.
        /// </summary>
        private static void Stretch(SpriteRenderer part, float fraction, Vector2 size)
        {
            if (part == null) return;

            fraction = Mathf.Clamp01(fraction);
            part.transform.localScale = new Vector3(size.x * fraction, size.y, 1f);
            part.transform.localPosition = new Vector3(-size.x * (1f - fraction) * 0.5f, 0f, 0f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) ApplyStyle();
        }
#endif
    }
}
