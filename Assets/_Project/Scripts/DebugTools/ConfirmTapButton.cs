using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RPG.DebugTools
{
    /// <summary>
    /// Makes a button ask before it does something irreversible.
    ///
    /// The first tap arms it, repaints it and changes its label; a second tap inside a short
    /// window fires. The arm times out on its own, so a button armed and then forgotten cannot
    /// be triggered by a stray thumb minutes later.
    ///
    /// Built for the playtest RESET button, which deletes the save profile outright and until
    /// now did it on a single unconfirmed tap, from a control floating above every other screen.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ConfirmTapButton : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private Text label;
        [SerializeField] private string idleText = "RESET";
        [SerializeField] private string armedText = "SURE?";

        [Header("Colours")]
        [SerializeField] private Color idleColor = new Color(0.42f, 0.16f, 0.18f, 0.85f);
        [SerializeField] private Color armedColor = new Color(0.75f, 0.18f, 0.2f, 0.95f);

        [Header("Timing")]
        [Tooltip("Seconds the armed state lasts before it gives up and disarms itself.")]
        [SerializeField, Min(0.5f)] private float armWindowSeconds = 3f;

        [Tooltip("Raised only on the confirming second tap.")]
        [SerializeField] private UnityEvent confirmed = new UnityEvent();

        private Button _button;
        private Image _image;
        private float _armedUntil;

        private bool IsArmed => Time.unscaledTime < _armedUntil;

        /// <summary>The confirmed action, so setup tools can add a persistent listener.</summary>
        public UnityEvent Confirmed => confirmed;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _image = GetComponent<Image>();

            if (label == null) label = GetComponentInChildren<Text>();

            _button.onClick.AddListener(OnClicked);
            Paint();
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (IsArmed)
            {
                _armedUntil = 0f;
                Paint();
                confirmed.Invoke();
                return;
            }

            _armedUntil = Time.unscaledTime + armWindowSeconds;
            Paint();
        }

        // Unscaled, because this button is most useful while a menu has frozen the game.
        private void Update()
        {
            // Repaints once, on the frame the arm expires, rather than every frame.
            if (_armedUntil > 0f && !IsArmed)
            {
                _armedUntil = 0f;
                Paint();
            }
        }

        private void Paint()
        {
            bool armed = IsArmed;
            if (label != null) label.text = armed ? armedText : idleText;
            if (_image != null) _image.color = armed ? armedColor : idleColor;
        }
    }
}
