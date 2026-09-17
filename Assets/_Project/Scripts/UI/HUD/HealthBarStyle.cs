using UnityEngine;

namespace RPG.UI.HUD
{
    /// <summary>
    /// The look of a world-space health bar, as data.
    ///
    /// Every bar in the game - the player's and every enemy's - reads its size and colours from
    /// one of these, so retuning how health bars look is an asset edit, not a hunt through five
    /// prefabs. Two assets exist by default: one for the player, one for enemies, differing
    /// mainly in whether the bar hides while the target is at full health.
    /// </summary>
    [CreateAssetMenu(fileName = "HealthBarStyle", menuName = "RPG/UI/Health Bar Style")]
    public class HealthBarStyle : ScriptableObject
    {
        [Header("Layout")]
        [Tooltip("Bar size in world units.")]
        [SerializeField] private Vector2 size = new Vector2(1f, 0.14f);

        [Tooltip("Offset from the character's centre. Negative Y puts the bar under the model.")]
        [SerializeField] private Vector2 offset = new Vector2(0f, -0.62f);

        [Tooltip("Thickness of the dark outline drawn behind the bar, in world units.")]
        [SerializeField, Min(0f)] private float border = 0.04f;

        [Header("Colours")]
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.07f, 0.85f);
        [SerializeField] private Color fillColor = new Color(0.35f, 0.85f, 0.4f);

        [Tooltip("Used once health drops below the low-health threshold.")]
        [SerializeField] private Color lowFillColor = new Color(0.9f, 0.3f, 0.28f);

        [Tooltip("Fraction of max HP below which the bar turns to the low-health colour.")]
        [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.3f;

        [Header("Damage trail")]
        [Tooltip("The lighter bar left behind after a hit, which then drains to meet the real " +
                 "value. It is what makes a single hit visible on a big health pool.")]
        [SerializeField] private Color trailColor = new Color(0.95f, 0.85f, 0.5f, 0.9f);

        [Tooltip("Seconds the trail holds still before it starts draining.")]
        [SerializeField, Min(0f)] private float trailHoldSeconds = 0.25f;

        [Tooltip("How fast the trail drains, as a fraction of the full bar per second.")]
        [SerializeField, Min(0.05f)] private float trailDrainPerSecond = 1.2f;

        [Header("Visibility")]
        [Tooltip("Hide the bar while the target is at full health. On for enemies, off for the " +
                 "player, whose health should always be readable.")]
        [SerializeField] private bool hideWhenFull = true;

        [Tooltip("Seconds the bar stays up after returning to full before it hides again.")]
        [SerializeField, Min(0f)] private float hideDelaySeconds = 1.5f;

        [Tooltip("Hide the bar when the target is dead.")]
        [SerializeField] private bool hideWhenDead = true;

        [Header("Rendering")]
        [Tooltip("Sorting order for the bar. Must be above the character sprite's order or the " +
                 "bar draws behind the model.")]
        [SerializeField] private int sortingOrder = 50;

        public Vector2 Size => size;
        public Vector2 Offset => offset;
        public float Border => border;
        public Color BackgroundColor => backgroundColor;
        public Color FillColor => fillColor;
        public Color LowFillColor => lowFillColor;
        public float LowHealthThreshold => lowHealthThreshold;
        public Color TrailColor => trailColor;
        public float TrailHoldSeconds => trailHoldSeconds;
        public float TrailDrainPerSecond => trailDrainPerSecond;
        public bool HideWhenFull => hideWhenFull;
        public float HideDelaySeconds => hideDelaySeconds;
        public bool HideWhenDead => hideWhenDead;
        public int SortingOrder => sortingOrder;

        /// <summary>Colour for a given fraction of health, so every bar agrees on "hurt".</summary>
        public Color FillColorFor(float fraction) =>
            fraction <= lowHealthThreshold ? lowFillColor : fillColor;
    }
}
