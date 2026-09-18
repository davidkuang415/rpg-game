using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RPG.UI
{
    /// <summary>
    /// Lets a page scroll up and down inside a hub that swipes left and right.
    ///
    /// Two ScrollRects nested at right angles do not cooperate on their own: the inner one
    /// swallows every drag that starts inside it, including horizontal ones, so the page would
    /// scroll but the hub would stop swiping. uGUI has no built-in routing for this.
    ///
    /// The gesture's direction is judged once, when the drag begins, and the whole drag is then
    /// committed to one axis - which is also how a finger actually behaves. A horizontal gesture
    /// disables the inner ScrollRect for the duration and is replayed into the outer one.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class NestedPageScroll : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("The horizontal pager this page lives in. Found on a parent when left empty.")]
        [SerializeField] private SwipePageView pageView;

        private ScrollRect _inner;
        private GameObject _outerTarget;
        private bool _routingToOuter;

        private void Awake()
        {
            _inner = GetComponent<ScrollRect>();
            _inner.horizontal = false;
            _inner.vertical = true;

            if (pageView == null) pageView = GetComponentInParent<SwipePageView>();
            if (pageView != null) _outerTarget = pageView.gameObject;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Horizontal wins ties on purpose: swiping between pages is the more common gesture,
            // and a page that refuses to let go feels broken in a way a slightly twitchy scroll
            // does not.
            _routingToOuter = _outerTarget != null &&
                              Mathf.Abs(eventData.delta.x) >= Mathf.Abs(eventData.delta.y);

            if (!_routingToOuter) return;

            // A disabled MonoBehaviour receives no further events, so this is what stops the
            // inner view from also consuming the drag.
            _inner.enabled = false;
            _inner.velocity = Vector2.zero;

            ExecuteEvents.Execute(_outerTarget, eventData, ExecuteEvents.beginDragHandler);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_routingToOuter) return;
            ExecuteEvents.Execute(_outerTarget, eventData, ExecuteEvents.dragHandler);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_routingToOuter) return;

            ExecuteEvents.Execute(_outerTarget, eventData, ExecuteEvents.endDragHandler);

            _routingToOuter = false;
            _inner.enabled = true;
        }

        private void OnDisable()
        {
            // Never leave the inner view switched off because a drag was interrupted by the page
            // closing - it would come back unable to scroll at all.
            _routingToOuter = false;
            if (_inner != null) _inner.enabled = true;
        }
    }
}
