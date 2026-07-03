using UnityEngine;
using UnityEngine.EventSystems;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// 可接收词块拖拽投放的区域。
    /// </summary>
    public sealed class WordDropZone : MonoBehaviour, IDropHandler
    {
        [Header("投放区域")]
        [Tooltip("词块被投放后挂载到的内容根节点。为空时使用当前对象。")]
        [SerializeField]
        private RectTransform contentRoot;

        /// <summary>
        /// 词块被投放后实际挂载的内容根节点。
        /// </summary>
        public RectTransform ContentRoot => contentRoot != null ? contentRoot : (RectTransform)transform;

        /// <inheritdoc />
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
            {
                return;
            }

            var item = eventData.pointerDrag.GetComponent<DraggableWordItem>();
            if (item == null)
            {
                return;
            }

            item.AttachToZone(this);
        }
    }
}
