using UnityEngine;

namespace RPG.UI
{
    /// <summary>
    /// One page inside the out-of-combat hub: stages, gear, or the bag.
    ///
    /// A page is NOT a ModalPanel. It never shows or hides itself, never freezes the player and
    /// never touches the time scale - the hub that owns it does all of that once, for all of
    /// them. That separation is what lets pages be added, removed or reordered without any of
    /// them knowing there are others.
    /// </summary>
    public abstract class HubPage : MonoBehaviour
    {
        [Header("Page")]
        [Tooltip("Shown on this page's tab at the top of the hub.")]
        [SerializeField] private string pageName = "PAGE";

        public string PageName => pageName;

        public RectTransform Rect => (RectTransform)transform;

        /// <summary>
        /// Rebuilds this page's contents. Called by the hub when it opens and whenever the page
        /// is scrolled into view, so a page can never show data from before the last stage.
        /// </summary>
        public void Refresh() => BuildContent();

        protected abstract void BuildContent();
    }
}
