using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// 可拖拽的词语 UI 项。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DraggableWordItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("显示")]
        [Tooltip("词语显示文本。")]
        [SerializeField]
        private TextMeshProUGUI label;

        [Tooltip("拖拽时是否临时脱离布局，以便跟随指针移动。")]
        [SerializeField]
        private bool detachToRootCanvasOnDrag = true;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private LayoutElement layoutElement;
        private Canvas rootCanvas;
        private WordDropZone previousZone;
        private bool wasDroppedThisDrag;
        private Vector3 dragWorldOffset;

        /// <summary>
        /// 当前词语的全局 ID。
        /// </summary>
        public string WordId { get; private set; }

        /// <summary>
        /// 当前所在的投放区域。
        /// </summary>
        public WordDropZone CurrentZone { get; private set; }

        private void Awake()
        {
            EnsureCached();
        }

        /// <summary>
        /// 初始化词块显示和所在区域。
        /// </summary>
        /// <param name="wordId">全局词语 ID。</param>
        /// <param name="displayText">显示给玩家的词语文本。</param>
        /// <param name="initialZone">初始所在投放区域。</param>
        public void Initialize(string wordId, string displayText, WordDropZone initialZone)
        {
            EnsureCached();
            WordId = wordId;

            if (label != null)
            {
                label.text = displayText;
            }

            if (initialZone != null)
            {
                AttachToZone(initialZone);
            }
        }

        /// <summary>
        /// 把词块挂到指定投放区域。
        /// </summary>
        /// <param name="zone">目标投放区域。</param>
        public void AttachToZone(WordDropZone zone)
        {
            EnsureCached();

            if (zone == null)
            {
                return;
            }

            CurrentZone = zone;
            wasDroppedThisDrag = true;
            transform.SetParent(zone.ContentRoot, false);
            transform.SetAsLastSibling();
            rectTransform.anchoredPosition = Vector2.zero;

            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = false;
            }
        }

        /// <inheritdoc />
        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureCached();
            previousZone = CurrentZone;
            wasDroppedThisDrag = false;
            rootCanvas = rootCanvas != null ? rootCanvas : GetComponentInParent<Canvas>()?.rootCanvas;

            if (layoutElement != null)
            {
                layoutElement.ignoreLayout = true;
            }

            if (detachToRootCanvasOnDrag && rootCanvas != null)
            {
                transform.SetParent(rootCanvas.transform, true);
            }

            dragWorldOffset = GetDragWorldOffset(eventData);
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.85f;
            UpdateDragPosition(eventData);
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
            EnsureCached();
            UpdateDragPosition(eventData);
        }

        /// <inheritdoc />
        public void OnEndDrag(PointerEventData eventData)
        {
            EnsureCached();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            if (!wasDroppedThisDrag && previousZone != null)
            {
                AttachToZone(previousZone);
            }
        }

        private void UpdateDragPosition(PointerEventData eventData)
        {
            EnsureCached();

            if (rootCanvas == null)
            {
                rectTransform.position = (Vector3)eventData.position + dragWorldOffset;
                return;
            }

            var rootRect = (RectTransform)rootCanvas.transform;
            var eventCamera = GetEventCamera(eventData);
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rootRect, eventData.position, eventCamera, out var worldPoint))
            {
                rectTransform.position = ClampWorldPositionToRoot(worldPoint + dragWorldOffset, rootRect);
            }
        }

        private Vector3 ClampWorldPositionToRoot(Vector3 worldPosition, RectTransform rootRect)
        {
            var localPosition = rootRect.InverseTransformPoint(worldPosition);
            var rect = rootRect.rect;
            var itemSize = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
            var rootScale = rootRect.lossyScale;

            if (!Mathf.Approximately(rootScale.x, 0f))
            {
                itemSize.x /= Mathf.Abs(rootScale.x);
            }

            if (!Mathf.Approximately(rootScale.y, 0f))
            {
                itemSize.y /= Mathf.Abs(rootScale.y);
            }

            var pivotOffset = new Vector2(
                Mathf.Lerp(itemSize.x, -itemSize.x, rectTransform.pivot.x) * 0.5f,
                Mathf.Lerp(itemSize.y, -itemSize.y, rectTransform.pivot.y) * 0.5f);
            var halfSize = itemSize * 0.5f;

            localPosition.x = Mathf.Clamp(localPosition.x, rect.xMin + halfSize.x + pivotOffset.x, rect.xMax - halfSize.x + pivotOffset.x);
            localPosition.y = Mathf.Clamp(localPosition.y, rect.yMin + halfSize.y + pivotOffset.y, rect.yMax - halfSize.y + pivotOffset.y);

            return rootRect.TransformPoint(localPosition);
        }

        private Vector3 GetDragWorldOffset(PointerEventData eventData)
        {
            if (rootCanvas == null)
            {
                return rectTransform.position - (Vector3)eventData.position;
            }

            var rootRect = (RectTransform)rootCanvas.transform;
            var eventCamera = GetEventCamera(eventData);
            return RectTransformUtility.ScreenPointToWorldPointInRectangle(rootRect, eventData.position, eventCamera, out var worldPoint)
                ? rectTransform.position - worldPoint
                : Vector3.zero;
        }

        private Camera GetEventCamera(PointerEventData eventData)
        {
            if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : eventData.pressEventCamera;
        }

        private void EnsureCached()
        {
            rectTransform = rectTransform != null ? rectTransform : GetComponent<RectTransform>();
            canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
            layoutElement = layoutElement != null ? layoutElement : GetComponent<LayoutElement>();
            rootCanvas = rootCanvas != null ? rootCanvas : GetComponentInParent<Canvas>()?.rootCanvas;
        }
    }
}
