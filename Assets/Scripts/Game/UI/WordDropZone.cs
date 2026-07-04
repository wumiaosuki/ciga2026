using UnityEngine;
namespace Ciga2026.Game.UI
{
    /// <summary>
    /// 词块点击切换时使用的目标区域。
    /// </summary>
    public sealed class WordDropZone : MonoBehaviour
    {
        [Header("投放区域")]
        [Tooltip("词块被投放后挂载到的内容根节点。为空时使用当前对象。")]
        [SerializeField]
        private RectTransform contentRoot;

        [Tooltip("词块停留在这个区域时是否允许播放随机抖动。词库区域开启，句子输入区域关闭。")]
        [SerializeField]
        private bool allowWordItemJitter = true;

        /// <summary>
        /// 词块被投放后实际挂载的内容根节点。
        /// </summary>
        public RectTransform ContentRoot => contentRoot != null ? contentRoot : (RectTransform)transform;

        /// <summary>
        /// 词块停留在这个区域时是否允许播放随机抖动。
        /// </summary>
        public bool AllowWordItemJitter => allowWordItemJitter;

    }
}
