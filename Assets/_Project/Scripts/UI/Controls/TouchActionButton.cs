using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RPG.Player.Input;

namespace RPG.UI.Controls
{
    /// <summary>
    /// A press-and-hold action button for the mobile HUD.
    ///
    /// Unity's built-in Button only fires on release, which feels wrong for combat, so this
    /// reports the press the instant the finger lands. It writes into the shared
    /// PlayerInputChannel; the player decides what (if anything) an attack press does.
    ///
    /// The 'action' field is what lets more buttons be added later (abilities, dash, potion)
    /// without a new script per button.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum ActionType
        {
            Attack
            // Ability1, Ability2, Dash ... added as those systems arrive.
        }

        [SerializeField] private PlayerInputChannel inputChannel;
        [SerializeField] private ActionType action = ActionType.Attack;

        [Header("Feedback")]
        [Tooltip("Optional: scale multiplier applied while held, for a bit of tactile feel.")]
        [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.92f;

        private Vector3 _baseScale;
        private bool _isHeld;

        public bool IsHeld => _isHeld;

        private void Awake()
        {
            _baseScale = transform.localScale;
            if (inputChannel == null)
                Debug.LogError($"{nameof(TouchActionButton)} on '{name}' has no Input Channel assigned.", this);
        }

        private void OnDisable()
        {
            if (_isHeld) Release();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isHeld = true;
            transform.localScale = _baseScale * pressedScale;

            if (inputChannel == null) return;
            switch (action)
            {
                case ActionType.Attack:
                    inputChannel.PressAttack();
                    break;
            }
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        private void Release()
        {
            _isHeld = false;
            transform.localScale = _baseScale;

            if (inputChannel == null) return;
            switch (action)
            {
                case ActionType.Attack:
                    inputChannel.ReleaseAttack();
                    break;
            }
        }
    }
}
