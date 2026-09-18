using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPG.UI
{
    /// <summary>
    /// Horizontal paging: swipe left or right to move one full page at a time.
    ///
    /// It rides on top of Unity's ScrollRect rather than handling touches itself. That is
    /// deliberate - ScrollRect already solves the hard part, which is telling a swipe apart
    /// from a tap on a button inside the page. A hand-rolled drag handler would equip an item
    /// every time you tried to swipe past it.
    ///
    /// This component adds the two things ScrollRect does not do: laying the pages out edge to
    /// edge, and snapping to the nearest page when the finger lifts.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class SwipePageView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Feel")]
        [Tooltip("Fraction of a page width that counts as a deliberate swipe rather than a nudge.")]
        [SerializeField, Range(0.05f, 0.9f)] private float swipeThreshold = 0.2f;

        [Tooltip("Flick speed, in page widths per second, that advances a page even on a short drag.")]
        [SerializeField, Min(0f)] private float flickVelocity = 1.4f;

        [Tooltip("Seconds the snap animation takes.")]
        [SerializeField, Min(0.01f)] private float snapDuration = 0.18f;

        private ScrollRect _scroll;
        private readonly List<RectTransform> _pages = new List<RectTransform>();

        private int _currentPage;
        private float _pageWidth;
        private float _lastViewportWidth;

        private bool _animating;
        private float _animFrom;
        private float _animTo;
        private float _animElapsed;
        private float _dragStartX;
        private float _dragVelocity;

        /// <summary>Raised when the visible page changes, with the new page index.</summary>
        public event Action<int> PageChanged;

        public int CurrentPage => _currentPage;
        public int PageCount => _pages.Count;

        private void Awake()
        {
            _scroll = GetComponent<ScrollRect>();

            // Paging and inertia fight each other: inertia would coast past the page you
            // released on, and the snap would then drag it back.
            _scroll.horizontal = true;
            _scroll.vertical = false;
            _scroll.inertia = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
        }

        /// <summary>
        /// Takes ownership of the pages. Called by the hub on open, so pages added by a setup
        /// tool need no extra registration step.
        /// </summary>
        public void SetPages(IReadOnlyList<RectTransform> pages)
        {
            _pages.Clear();
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] != null) _pages.Add(pages[i]);
            }

            _currentPage = Mathf.Clamp(_currentPage, 0, Mathf.Max(0, _pages.Count - 1));
            Layout();
        }

        /// <summary>Lays the pages out edge to edge at the current viewport width.</summary>
        public void Layout()
        {
            if (_scroll == null || _scroll.content == null) return;

            RectTransform viewport = _scroll.viewport != null ? _scroll.viewport : (RectTransform)transform;
            _pageWidth = viewport.rect.width;
            _lastViewportWidth = _pageWidth;

            if (_pageWidth <= 0f) return;   // Laid out before the canvas has a size; retried in Update.

            RectTransform content = _scroll.content;
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.sizeDelta = new Vector2(_pageWidth * _pages.Count, 0f);

            for (int i = 0; i < _pages.Count; i++)
            {
                RectTransform page = _pages[i];
                page.anchorMin = new Vector2(0f, 0f);
                page.anchorMax = new Vector2(0f, 1f);
                page.pivot = new Vector2(0.5f, 0.5f);
                page.sizeDelta = new Vector2(_pageWidth, 0f);
                page.anchoredPosition = new Vector2(_pageWidth * (i + 0.5f), 0f);
            }

            SnapImmediate(_currentPage);
        }

        /// <summary>Animates to a page. Used by the tab buttons at the top of the hub.</summary>
        public void GoToPage(int index)
        {
            index = Mathf.Clamp(index, 0, Mathf.Max(0, _pages.Count - 1));
            bool changed = index != _currentPage;
            _currentPage = index;

            StartSnap(TargetXFor(index));
            if (changed) PageChanged?.Invoke(_currentPage);
        }

        /// <summary>Jumps to a page with no animation. Used when the hub opens.</summary>
        public void SnapImmediate(int index)
        {
            index = Mathf.Clamp(index, 0, Mathf.Max(0, _pages.Count - 1));
            bool changed = index != _currentPage;
            _currentPage = index;
            _animating = false;

            if (_scroll != null && _scroll.content != null)
            {
                _scroll.velocity = Vector2.zero;
                Vector2 position = _scroll.content.anchoredPosition;
                position.x = TargetXFor(index);
                _scroll.content.anchoredPosition = position;
            }

            if (changed) PageChanged?.Invoke(_currentPage);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _animating = false;
            _dragStartX = _scroll.content != null ? _scroll.content.anchoredPosition.x : 0f;
            _dragVelocity = 0f;
        }

        /// <summary>
        /// Tracks the drag's own speed.
        ///
        /// ScrollRect.velocity cannot be trusted here: this component switches inertia OFF (it
        /// fights paging), and with inertia disabled ScrollRect has no reason to maintain a
        /// velocity for us to read. Measuring the pointer directly means a quick flick is
        /// detected whatever ScrollRect does internally.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            float instant = eventData.delta.x / dt;

            // Smoothed, so one stuttering frame at the end of a drag cannot read as a flick.
            _dragVelocity = Mathf.Lerp(_dragVelocity, instant, 0.6f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_pageWidth <= 0f || _scroll.content == null) return;

            float travelled = _scroll.content.anchoredPosition.x - _dragStartX;
            float pagesTravelled = travelled / _pageWidth;

            // Dragging content left (negative) moves forward through the pages.
            int target = _currentPage;

            bool flicked = Mathf.Abs(_dragVelocity) / _pageWidth > flickVelocity;

            // A flick is judged by the direction of the FLICK, not of the total travel: a drag
            // that wandered back and forth should follow the way the thumb was going when it left.
            if (pagesTravelled <= -swipeThreshold || (flicked && _dragVelocity < 0f)) target = _currentPage + 1;
            else if (pagesTravelled >= swipeThreshold || (flicked && _dragVelocity > 0f)) target = _currentPage - 1;

            GoToPage(target);
        }

        private void Update()
        {
            // The viewport width changes on rotation and on a resolution change in the editor.
            // Re-laying out only when it actually changed keeps this to one float compare.
            RectTransform viewport = _scroll != null && _scroll.viewport != null
                ? _scroll.viewport
                : (RectTransform)transform;

            if (!Mathf.Approximately(viewport.rect.width, _lastViewportWidth)) Layout();

            if (!_animating) return;

            // Unscaled: the hub pauses the game, so scaled time is frozen while it is open.
            _animElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_animElapsed / snapDuration);
            float eased = 1f - (1f - t) * (1f - t);

            Vector2 position = _scroll.content.anchoredPosition;
            position.x = Mathf.Lerp(_animFrom, _animTo, eased);
            _scroll.content.anchoredPosition = position;

            if (t >= 1f) _animating = false;
        }

        private void StartSnap(float targetX)
        {
            if (_scroll == null || _scroll.content == null) return;

            _scroll.velocity = Vector2.zero;
            _animFrom = _scroll.content.anchoredPosition.x;
            _animTo = targetX;
            _animElapsed = 0f;
            _animating = true;
        }

        private float TargetXFor(int index) => -_pageWidth * index;
    }
}
