using UnityEngine;
using UnityEngine.EventSystems;
using RPG.Items;

namespace RPG.UI
{
    /// <summary>
    /// Shows the shared tooltip while the pointer is over this tile.
    ///
    /// Added to tiles as they are built rather than authored on the template, because the item a
    /// tile represents changes every time the page is rebuilt.
    /// </summary>
    public class ItemTooltipTrigger : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        private ItemTooltip _tooltip;
        private EquipmentInstance _item;

        public void Bind(ItemTooltip tooltip, EquipmentInstance item)
        {
            _tooltip = tooltip;
            _item = item;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltip == null || _item == null) return;
            _tooltip.Show(_item, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip != null) _tooltip.Hide();
        }

        /// <summary>
        /// A touch reports an enter and an exit in the same frame, so without this a phone would
        /// never see the tooltip at all. Pressing shows it; the tile's own click still runs.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_tooltip == null || _item == null) return;
            _tooltip.Show(_item, eventData.position);
        }

        private void OnDisable()
        {
            if (_tooltip != null) _tooltip.Hide();
        }
    }
}
