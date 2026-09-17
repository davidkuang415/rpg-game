using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RPG.UI
{
    /// <summary>
    /// Everything you do between fights, in one screen: pick a stage, manage your gear, read
    /// your stats, sort your bag.
    ///
    /// This exists because the alternative - a STATS tab and a BAG button floating over the
    /// arena - puts menu furniture on top of the part of the screen you are trying to aim at.
    /// Out-of-combat business now happens out of combat, and the combat HUD is left with only
    /// the joystick and the attack button.
    ///
    /// The hub is the only ModalPanel here. Its pages never freeze the player, pause time or
    /// show and hide themselves; it does all of that once, for all of them.
    /// </summary>
    public class HubScreen : ModalPanel
    {
        [Header("Pages")]
        [Tooltip("Left to right, in swipe order. The first page is the one the hub opens on.")]
        [SerializeField] private HubPage[] pages;

        [Tooltip("The stage list. Held separately only so the game flow can hear StageChosen.")]
        [SerializeField] private StageSelectPanel stagesPage;

        [Header("Widgets")]
        [SerializeField] private SwipePageView pageView;
        [SerializeField] private Text titleLabel;

        [Header("Tabs")]
        [Tooltip("Optional. Tabs are built from the pages, so swiping is discoverable rather " +
                 "than something the player has to guess at.")]
        [SerializeField] private RectTransform tabContainer;

        [SerializeField] private Button tabTemplate;
        [SerializeField] private Color activeTabColor = new Color(0.22f, 0.34f, 0.46f);
        [SerializeField] private Color inactiveTabColor = new Color(0.14f, 0.15f, 0.18f);

        private readonly List<RectTransform> _pageRects = new List<RectTransform>();
        private readonly List<Button> _tabs = new List<Button>();
        private int _openOnPage;

        /// <summary>The stage list page, so the game flow can subscribe to StageChosen.</summary>
        public StageSelectPanel Stages => stagesPage;

        protected override void Awake()
        {
            base.Awake();

            if (tabTemplate != null) tabTemplate.gameObject.SetActive(false);
            BuildTabs();

            if (pageView != null) pageView.PageChanged += OnPageChanged;

            // Hidden in Awake, never in Start: every Awake runs before any Start, so this cannot
            // close a hub that the game flow opens during its own Start.
            Hide();
        }

        private void OnDestroy()
        {
            if (pageView != null) pageView.PageChanged -= OnPageChanged;
        }

        /// <summary>Opens the hub on the stage list. The normal way back from a fight.</summary>
        public void ShowStages() => ShowOnPage(IndexOf(stagesPage));

        /// <summary>Opens the hub on a given page. Used by the tab shortcuts and debug tools.</summary>
        public void ShowOnPage(int pageIndex)
        {
            _openOnPage = Mathf.Max(0, pageIndex);
            Show();
        }

        public override void Show()
        {
            base.Show();

            if (pageView != null)
            {
                CollectPageRects();
                pageView.SetPages(_pageRects);
                pageView.SnapImmediate(_openOnPage);
            }

            UpdateChrome(pageView != null ? pageView.CurrentPage : _openOnPage);
            _openOnPage = 0;   // Reset, so the next open starts on the stage list.
        }

        /// <summary>Every page is rebuilt on open, so none can show data from before the stage.</summary>
        protected override void BuildContent()
        {
            if (pages == null) return;

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null) pages[i].Refresh();
            }
        }

        private void OnPageChanged(int index)
        {
            // Refreshed on arrival as well as on open: equipping something on the gear page
            // changes what the bag page should show.
            if (pages != null && index >= 0 && index < pages.Length && pages[index] != null)
            {
                pages[index].Refresh();
            }

            UpdateChrome(index);
        }

        private void UpdateChrome(int index)
        {
            if (titleLabel != null && pages != null && index >= 0 && index < pages.Length &&
                pages[index] != null)
            {
                titleLabel.text = pages[index].PageName.ToUpperInvariant();
            }

            for (int i = 0; i < _tabs.Count; i++)
            {
                var image = _tabs[i].GetComponent<Image>();
                if (image != null) image.color = i == index ? activeTabColor : inactiveTabColor;
            }
        }

        private void BuildTabs()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i] != null) Destroy(_tabs[i].gameObject);
            }
            _tabs.Clear();

            if (tabTemplate == null || tabContainer == null || pages == null) return;

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] == null) continue;

                Button tab = Instantiate(tabTemplate, tabContainer);
                tab.gameObject.SetActive(true);
                tab.gameObject.name = $"Tab_{pages[i].PageName}";

                var label = tab.GetComponentInChildren<Text>();
                if (label != null) label.text = pages[i].PageName.ToUpperInvariant();

                int captured = i;
                tab.onClick.AddListener(() =>
                {
                    if (pageView != null) pageView.GoToPage(captured);
                });

                _tabs.Add(tab);
            }
        }

        private void CollectPageRects()
        {
            _pageRects.Clear();
            if (pages == null) return;

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null) _pageRects.Add(pages[i].Rect);
            }
        }

        private int IndexOf(HubPage page)
        {
            if (page == null || pages == null) return 0;

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] == page) return i;
            }
            return 0;
        }
    }
}
