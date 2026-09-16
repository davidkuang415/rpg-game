using UnityEngine;
using RPG.Player.Input;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RPG.DebugTools
{
    /// <summary>
    /// Development convenience: drives the same PlayerInputChannel from WASD/arrow keys
    /// and Space/left-mouse so you can test in the Editor without dragging the joystick.
    ///
    /// It only writes when the keyboard is actually being used, so it composes with the
    /// touch controls instead of fighting them.
    ///
    /// Compiles against whichever input backend the project uses (old, new, or both).
    /// Put this on a dev-only GameObject and disable it for real builds.
    /// </summary>
    public class KeyboardInputWriter : MonoBehaviour
    {
        [SerializeField] private PlayerInputChannel inputChannel;
        [Tooltip("Turn off to force testing with the on-screen controls only.")]
        [SerializeField] private bool enableInEditorOnly = true;

        private bool _wasWritingMove;

        private void Awake()
        {
            if (inputChannel == null)
            {
                Debug.LogError($"{nameof(KeyboardInputWriter)} on '{name}' has no Input Channel assigned.", this);
                enabled = false;
                return;
            }

            if (enableInEditorOnly && !Application.isEditor)
            {
                enabled = false;
            }
        }

        private void Update()
        {
            Vector2 move = ReadMoveAxis();

            // Only take over movement while keys are actually pressed, so releasing the keys
            // hands control straight back to the joystick instead of pinning it to zero.
            if (move.sqrMagnitude > 0.0001f)
            {
                inputChannel.SetMove(Vector2.ClampMagnitude(move, 1f));
                _wasWritingMove = true;
            }
            else if (_wasWritingMove)
            {
                inputChannel.SetMove(Vector2.zero);
                _wasWritingMove = false;
            }

            if (ReadAttackPressed()) inputChannel.PressAttack();
            if (ReadAttackReleased()) inputChannel.ReleaseAttack();
        }

        private void OnDisable()
        {
            if (_wasWritingMove && inputChannel != null)
            {
                inputChannel.SetMove(Vector2.zero);
                _wasWritingMove = false;
            }
        }

        private static Vector2 ReadMoveAxis()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical"));
#else
            return Vector2.zero;
#endif
        }

        private static bool ReadAttackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            bool space = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            return space || click;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private static bool ReadAttackReleased()
        {
#if ENABLE_INPUT_SYSTEM
            bool space = Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame;
            bool click = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            return space || click;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKeyUp(KeyCode.Space) || UnityEngine.Input.GetMouseButtonUp(0);
#else
            return false;
#endif
        }
    }
}
