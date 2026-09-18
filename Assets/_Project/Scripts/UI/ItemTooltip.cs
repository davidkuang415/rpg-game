using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RPG.Items;
using RPG.Stats;

namespace RPG.UI
{
    /// <summary>
    /// A floating panel describing whatever the pointer is over.
    ///
    /// One instance is shared by every tile in the hub - equipped slots and bag slots alike -
    /// rather than each tile owning a panel it almost never shows.
    ///
    /// Hover only exists with a mouse. On a phone there is no hover at all, which is why the
    /// details box under each page still shows the selected item: the tooltip is a convenience
    /// for playing in the editor, never the only way to read an item's stats.
    /// </summary>
    public class ItemTooltip : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text bodyLabel;

        [Header("Data")]
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private RarityTable rarityTable;
        [SerializeField] private ItemEconomyConfig economyConfig;

        [Header("Placement")]
        [Tooltip("Offset from the pointer, in canvas units.")]
        [SerializeField] private Vector2 pointerOffset = new Vector2(24f, -24f);

        private readonly StatBlock _scratch = new StatBlock();
        private readonly StringBuilder _builder = new StringBuilder(256);

        private RectTransform _canvasRect;
        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                _canvas = _canvas.rootCanvas;
                _canvasRect = _canvas.transform as RectTransform;
            }

            Hide();
        }

        /// <summary>Shows the panel for an item, positioned near the pointer.</summary>
        public void Show(EquipmentInstance item, Vector2 screenPosition)
        {
            if (panel == null || item == null)
            {
                Hide();
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = ItemTilePainter.DisplayName(item, itemRegistry, rarityTable);
                if (rarityTable != null) titleLabel.color = rarityTable.GetColor(item.Rarity);
            }

            if (bodyLabel != null)
            {
                string stats = ItemTilePainter.DescribeStats(item, itemRegistry, rarityTable,
                    _scratch, _builder, economyConfig);

                bodyLabel.text = string.IsNullOrWhiteSpace(stats) ? "No stats." : stats;
            }

            panel.gameObject.SetActive(true);
            Reposition(screenPosition);
        }

        public void Hide()
        {
            if (panel != null) panel.gameObject.SetActive(false);
        }

        private void Reposition(Vector2 screenPosition)
        {
            if (_canvasRect == null) return;

            Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screenPosition, camera, out Vector2 local))
            {
                return;
            }

            local += pointerOffset;

            // Kept inside the canvas, so an item near the right or bottom edge does not push its
            // own tooltip off the screen.
            Vector2 canvasSize = _canvasRect.rect.size;
            Vector2 panelSize = panel.rect.size;

            float maxX = canvasSize.x * 0.5f - panelSize.x;
            float minX = -canvasSize.x * 0.5f;
            float maxY = canvasSize.y * 0.5f;
            float minY = -canvasSize.y * 0.5f + panelSize.y;

            local.x = Mathf.Clamp(local.x, minX, maxX);
            local.y = Mathf.Clamp(local.y, minY, maxY);

            panel.anchoredPosition = local;
        }
    }
}
