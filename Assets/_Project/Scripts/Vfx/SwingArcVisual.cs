using System.Collections;
using UnityEngine;
using RPG.Player.Combat;

namespace RPG.Vfx
{
    /// <summary>
    /// The visible slash for the Knight's swing: a crescent that flashes in the attack
    /// direction, grows to the weapon's reach and fades in a few frames.
    ///
    /// Before this the only sign a swing had happened was the damage on whatever it hit - a
    /// whiff looked identical to standing still. The arc is scaled from the attack's real
    /// range so what the player sees is exactly the area that was checked.
    ///
    /// The Archer needs nothing here: an arrow is its own visual.
    /// </summary>
    public class SwingArcVisual : MonoBehaviour
    {
        [Header("Parts")]
        [Tooltip("The crescent sprite, a child of the player. Disabled between swings.")]
        [SerializeField] private SpriteRenderer arc;

        [Header("Timing")]
        [SerializeField, Min(0.02f)] private float duration = 0.16f;

        [Tooltip("Scale at the start of the swing, relative to the full reach.")]
        [SerializeField, Range(0.1f, 1f)] private float startScale = 0.55f;

        [Tooltip("Scale at the end, relative to the full reach. Slightly over 1 sells the follow-through.")]
        [SerializeField, Range(0.5f, 2f)] private float endScale = 1.15f;

        [Tooltip("Degrees swept during the swing. The crescent rotates through this so it reads " +
                 "as a swing, not a stamp.")]
        [SerializeField] private float sweepDegrees = 40f;

        [Tooltip("Alternates the sweep direction on each swing.")]
        [SerializeField] private bool alternateSweep = true;

        [Header("Size")]
        [Tooltip("Reach, in world units, that the sprite's natural size corresponds to.")]
        [SerializeField, Min(0.1f)] private float spriteReach = 1f;

        private KnightSwordAttack _attack;
        private Coroutine _routine;
        private Color _baseColor;
        private bool _flip;

        private void Awake()
        {
            _attack = GetComponent<KnightSwordAttack>();

            if (arc != null)
            {
                _baseColor = arc.color;
                arc.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_attack != null) _attack.Attacked += OnAttacked;
        }

        private void OnDisable()
        {
            if (_attack != null) _attack.Attacked -= OnAttacked;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            if (arc != null) arc.enabled = false;
        }

        private void OnAttacked(Vector2 direction)
        {
            if (arc == null || !_attack.isActiveAndEnabled) return;

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Play(direction));
        }

        private IEnumerator Play(Vector2 direction)
        {
            float reach = _attack.CurrentRange;
            float scale = reach / spriteReach;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            float sweep = alternateSweep && _flip ? -sweepDegrees : sweepDegrees;
            _flip = !_flip;

            Transform t = arc.transform;
            arc.enabled = true;
            arc.flipY = sweep < 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - p) * (1f - p);

                t.localRotation = Quaternion.Euler(0f, 0f, angle - sweep * 0.5f + sweep * eased);
                float s = Mathf.Lerp(startScale, endScale, eased) * scale;
                t.localScale = new Vector3(s, s, 1f);

                Color c = _baseColor;
                c.a = _baseColor.a * (1f - p * p);
                arc.color = c;

                yield return null;
            }

            arc.enabled = false;
            _routine = null;
        }
    }
}
