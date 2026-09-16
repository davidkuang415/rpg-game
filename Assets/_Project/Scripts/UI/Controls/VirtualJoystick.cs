using UnityEngine;
using UnityEngine.EventSystems;
using RPG.Player.Input;

namespace RPG.UI.Controls
{
    /// <summary>
    /// On-screen thumbstick. It reads touch/mouse drags over its own RectTransform and
    /// writes a normalized direction into the shared PlayerInputChannel.
    ///
    /// It knows nothing about the player, movement speed or combat - it is purely a device.
    ///
    /// Setup expectations:
    ///  - 'background' is a UI Image with a CENTER pivot.
    ///  - 'handle' is a child Image of the background, also center pivot/anchor.
    ///  - This component sits on the object that should RECEIVE the touch (usually a large
    ///    transparent "input area" Image so the player can grab the stick anywhere on the
    ///    left half of the screen). That Image must have Raycast Target enabled.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public enum JoystickMode
        {
            /// <summary>Stick stays where it is placed in the layout.</summary>
            Fixed,
            /// <summary>Stick jumps to wherever the finger first touches inside the input area.</summary>
            Floating
        }

        [Header("Wiring")]
        [SerializeField] private PlayerInputChannel inputChannel;
        [Tooltip("The ring/base graphic. Must use a centered pivot.")]
        [SerializeField] private RectTransform background;
        [Tooltip("The knob graphic, a child of the background.")]
        [SerializeField] private RectTransform handle;

        [Header("Behaviour")]
        [SerializeField] private JoystickMode mode = JoystickMode.Floating;
        [Tooltip("How far the handle may travel, as a fraction of the background radius.")]
        [SerializeField, Range(0.1f, 1.5f)] private float handleRange = 0.75f;
        [Tooltip("Input below this fraction of the radius reads as zero, so a resting thumb does not drift.")]
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.12f;
        [Tooltip("Hide the stick graphics until the player touches the input area (Floating mode).")]
        [SerializeField] private bool hideWhenIdle = true;

        private Canvas _canvas;
        private Camera _uiCamera;
        private Vector2 _value;
        private bool _isDragging;

        /// <summary>Current stick value, magnitude 0..1. Exposed for HUD/debug readouts.</summary>
        public Vector2 Value => _value;
        public bool IsDragging => _isDragging;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null) _canvas = _canvas.rootCanvas;

            if (inputChannel == null)
                Debug.LogError($"{nameof(VirtualJoystick)} on '{name}' has no Input Channel assigned.", this);
            if (background == null || handle == null)
                Debug.LogError($"{nameof(VirtualJoystick)} on '{name}' needs both Background and Handle assigned.", this);
        }

        private void OnEnable()
        {
            // Screen Space - Overlay canvases pass a null camera to the UI math helpers.
            _uiCamera = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _canvas.worldCamera
                : null;

            ResetStick();
        }

        private void OnDisable()
        {
            // If the HUD is torn down mid-drag the player would otherwise keep sliding forever.
            if (_isDragging && inputChannel != null) inputChannel.SetMove(Vector2.zero);
            _isDragging = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;

            if (mode == JoystickMode.Floating && background != null)
            {
                // Re-centre the ring under the finger.
                RectTransform area = (RectTransform)transform;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        area, eventData.position, _uiCamera, out Vector2 areaPoint))
                {
                    background.anchoredPosition = areaPoint;
                }
            }

            SetGraphicsVisible(true);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || handle == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background, eventData.position, _uiCamera, out Vector2 local))
            {
                return;
            }

            float radius = Mathf.Min(background.rect.width, background.rect.height) * 0.5f;
            if (radius <= 0f) return;

            float travel = radius * handleRange;
            Vector2 raw = local / travel;
            if (raw.sqrMagnitude > 1f) raw.Normalize();

            float magnitude = raw.magnitude;
            _value = magnitude < deadZone
                ? Vector2.zero
                : raw.normalized * Mathf.InverseLerp(deadZone, 1f, magnitude);

            handle.anchoredPosition = raw * travel;

            if (inputChannel != null) inputChannel.SetMove(_value);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            ResetStick();
            if (inputChannel != null) inputChannel.SetMove(Vector2.zero);
        }

        private void ResetStick()
        {
            _value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            SetGraphicsVisible(!hideWhenIdle || mode == JoystickMode.Fixed);
        }

        private void SetGraphicsVisible(bool visible)
        {
            if (background != null && background.gameObject.activeSelf != visible)
                background.gameObject.SetActive(visible);
        }
    }
}
