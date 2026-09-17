using UnityEngine;
using RPG.Player;

namespace RPG.UI
{
    /// <summary>
    /// Shared behaviour for full-screen panels that interrupt play: class select, stage select,
    /// and the stage completion screen in a later phase.
    ///
    /// The one rule they all share is that the character must not keep fighting behind an open
    /// menu, so freezing and restoring input lives here rather than being re-implemented (and
    /// eventually forgotten) in each panel.
    /// </summary>
    public abstract class ModalPanel : MonoBehaviour
    {
        [Header("Panel")]
        [Tooltip("Root object toggled on and off. Defaults to this GameObject.")]
        [SerializeField] protected GameObject panelRoot;

        [Tooltip("Frozen while the panel is open. Optional.")]
        [SerializeField] protected PlayerController playerController;

        [SerializeField] private bool freezePlayerInput = true;

        [Tooltip("Stop the simulation while open, so enemies cannot act behind a menu.")]
        [SerializeField] private bool pauseTime = true;

        [Header("Transition")]
        [Tooltip("Seconds to fade and scale in when shown. 0 = appear instantly. Runs on " +
                 "unscaled time, since the panel usually pauses the game.")]
        [SerializeField, Min(0f)] private float showDuration = 0.18f;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private CanvasGroup _group;
        private Coroutine _transition;

        protected virtual void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
        }

        public virtual void Show()
        {
            BuildContent();
            panelRoot.SetActive(true);
            SetPlayerFrozen(true);
            if (pauseTime) Time.timeScale = 0f;

            PlayShowTransition();
        }

        public virtual void Hide()
        {
            if (_transition != null)
            {
                StopCoroutine(_transition);
                _transition = null;
            }
            ResetTransition();

            panelRoot.SetActive(false);
            SetPlayerFrozen(false);
            if (pauseTime) Time.timeScale = 1f;
        }

        /// <summary>
        /// A quick fade-and-grow on open, so screens arrive rather than pop. The coroutine
        /// runs on this component, which must therefore be active - it is, because Show()
        /// just activated the root and every panel component lives on or under it.
        /// </summary>
        private void PlayShowTransition()
        {
            if (showDuration <= 0f || !isActiveAndEnabled) return;

            if (_group == null)
            {
                _group = panelRoot.GetComponent<CanvasGroup>();
                if (_group == null) _group = panelRoot.AddComponent<CanvasGroup>();
            }

            if (_transition != null) StopCoroutine(_transition);
            _transition = StartCoroutine(ShowTransition());
        }

        private System.Collections.IEnumerator ShowTransition()
        {
            Transform t = panelRoot.transform;
            float elapsed = 0f;

            // Input is blocked for the few frames of the fade, so a tap that lands while a
            // screen is still half-transparent does not hit a button the player cannot read.
            _group.blocksRaycasts = false;

            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(elapsed / showDuration);
                float eased = 1f - (1f - p) * (1f - p);

                _group.alpha = eased;
                t.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);
                yield return null;
            }

            ResetTransition();
            _transition = null;
        }

        private void ResetTransition()
        {
            if (_group != null)
            {
                _group.alpha = 1f;
                _group.blocksRaycasts = true;
            }
            if (panelRoot != null) panelRoot.transform.localScale = Vector3.one;
        }

        /// <summary>Rebuild any dynamic contents. Called every time the panel opens.</summary>
        protected virtual void BuildContent() { }

        private void SetPlayerFrozen(bool frozen)
        {
            if (!freezePlayerInput || playerController == null) return;
            playerController.SetInputEnabled(!frozen);
        }
    }
}
