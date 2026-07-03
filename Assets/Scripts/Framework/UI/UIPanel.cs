using UnityEngine;

namespace Ciga2026.Framework.UI
{
    /// <summary>
    /// UI 面板可选基类。
    /// 不是所有 UI prefab 都必须继承它；继承后可以收到打开、关闭、释放等生命周期回调。
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        /// <summary>
        /// 当前面板在 UIManager 中注册的唯一 ID。
        /// </summary>
        public string PanelId { get; private set; }

        /// <summary>
        /// 当前面板所在 UI 层级。
        /// </summary>
        public UILayer Layer { get; private set; }

        /// <summary>
        /// 面板是否处于可见状态。
        /// </summary>
        public bool IsVisible => gameObject.activeSelf;

        /// <summary>
        /// 由 UIManager 在面板实例化或注册时调用。
        /// </summary>
        /// <param name="panelId">面板唯一 ID，通常来自 prefab 名称或调用方传入的 key。</param>
        /// <param name="layer">面板所在 UI 层级。</param>
        public void Initialize(string panelId, UILayer layer)
        {
            PanelId = panelId;
            Layer = layer;
            OnInitialized();
        }

        /// <summary>
        /// 由 UIManager 在移动层级后调用。
        /// </summary>
        /// <param name="layer">新的 UI 层级。</param>
        public void SetLayer(UILayer layer)
        {
            Layer = layer;
            OnLayerChanged(layer);
        }

        /// <summary>
        /// 面板初始化完成时调用。
        /// </summary>
        protected virtual void OnInitialized()
        {
        }

        /// <summary>
        /// 面板显示时调用。
        /// </summary>
        public virtual void OnShow()
        {
        }

        /// <summary>
        /// 面板隐藏时调用。
        /// </summary>
        public virtual void OnHide()
        {
        }

        /// <summary>
        /// 面板即将被 UIManager 释放时调用。
        /// </summary>
        public virtual void OnRelease()
        {
        }

        /// <summary>
        /// 面板层级改变后调用。
        /// </summary>
        /// <param name="layer">新的 UI 层级。</param>
        protected virtual void OnLayerChanged(UILayer layer)
        {
        }
    }
}
