using UnityEngine;

namespace RPG.Player
{
    /// <summary>
    /// Pushes final stat values into the components that consume them.
    ///
    /// This exists so PlayerStats does not need to know that a PlayerMotor exists, and
    /// PlayerMotor does not need to know that stats exist. Each stays independently testable;
    /// this small adapter is the only thing that knows both.
    ///
    /// Only stats that must be PUSHED live here. Attack speed is deliberately absent: the
    /// attack components read it from the stat pipeline at the moment they swing, which is
    /// always correct and needs no wiring.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerStatsBinder : MonoBehaviour
    {
        [Tooltip("Optional: tint the placeholder sprite with the class colour, so the selected " +
                 "class is visible at a glance during development.")]
        [SerializeField] private SpriteRenderer bodyRenderer;

        [SerializeField] private bool applyClassTint = true;

        private PlayerStats _stats;
        private PlayerMotor _motor;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _motor = GetComponent<PlayerMotor>();

            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            _stats.StatsChanged += Apply;
            _stats.ClassChanged += ApplyClassVisuals;
        }

        private void OnDisable()
        {
            _stats.StatsChanged -= Apply;
            _stats.ClassChanged -= ApplyClassVisuals;
        }

        // Applied once on Start as well: PlayerStats may have raised its first event during
        // Awake, before this component subscribed.
        private void Start()
        {
            Apply(_stats);
            ApplyClassVisuals(_stats.CurrentClass);
        }

        private void Apply(PlayerStats stats)
        {
            if (_motor != null) _motor.MoveSpeed = stats.Current.MoveSpeed;
        }

        private void ApplyClassVisuals(Classes.ClassData classData)
        {
            if (!applyClassTint || classData == null || bodyRenderer == null) return;
            bodyRenderer.color = classData.BodyTint;
        }
    }
}
