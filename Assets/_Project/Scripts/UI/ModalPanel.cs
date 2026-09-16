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

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

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
        }

        public virtual void Hide()
        {
            panelRoot.SetActive(false);
            SetPlayerFrozen(false);
            if (pauseTime) Time.timeScale = 1f;
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
