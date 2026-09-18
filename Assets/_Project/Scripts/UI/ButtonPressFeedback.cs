using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPG.UI
{
    /// <summary>
    /// Makes every button feel pressed: it shrinks slightly on touch-down and springs back on
    /// release. Runs on unscaled time, because most of the UI is open while the game is paused.
    ///
    /// A uGUI Button's own colour transition is kept as well; this only adds motion. It is
    /// deliberately not a Selectable subclass so the setup tools can drop it onto any button
    /// after the fact, including instantiated templates.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.94f;

        [Tooltip("Roughly how long the spring back takes.")]
        [SerializeField, Min(0.01f)] private float springSeconds = 0.08f;

        private RectTransform _rect;
        private Button _button;
        private Vector3 _restScale;
        private float _target = 1f;
        private float _current = 1f;
        private bool _pressed;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            // Rest scale is read on enable, not Awake: item tiles are scaled up slightly when
            // selected, and that selection scale is the rest pose this presses from.
            _restScale = _rect.localScale;
            _current = _target = 1f;
            _pressed = false;
        }

        private void OnDisable()
        {
            _rect.localScale = _restScale;
        }

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;

            _current = Mathf.Lerp(_current, _target, 1f - Mathf.Exp(-Time.unscaledDeltaTime / springSeconds));
            if (Mathf.Abs(_current - _target) < 0.002f) _current = _target;

            _rect.localScale = _restScale * _current;
        }

        /// <summary>
        /// Raised on every press of every button carrying this component. The sound layer
        /// listens; a static event because buttons are created and destroyed constantly (item
        /// tiles, stage rows) and a per-instance subscription would never keep up.
        /// </summary>
        public static event System.Action Pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;

            Pressed?.Invoke();
            _pressed = true;
            _restScale = _rect.localScale / _current;   // In case something rescaled us while at rest.
            _target = pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_pressed) Release();
        }

        private void Release()
        {
            _pressed = false;
            _target = 1f;
        }
    }
}
