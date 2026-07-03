using UnityEngine;

namespace Ciga2026.Framework.UI
{
    /// <summary>
    /// UI 组件绑定基类。
    /// 只负责承载 Inspector 中的组件引用，不放具体业务逻辑。
    /// </summary>
    public abstract class UIBind : MonoBehaviour
    {
        /// <summary>
        /// 当前绑定对象在 UIManager 中注册的面板 ID。
        /// </summary>
        public string PanelId { get; private set; }

        /// <summary>
        /// 当前绑定对象所在 UI 层级。
        /// </summary>
        public UILayer Layer { get; private set; }

        /// <summary>
        /// 由 UIManager 在注册面板时调用。
        /// </summary>
        /// <param name="panelId">面板唯一 ID。</param>
        /// <param name="layer">面板所在 UI 层级。</param>
        public void Initialize(string panelId, UILayer layer)
        {
            PanelId = panelId;
            Layer = layer;
        }

        /// <summary>
        /// 由 UIManager 在移动层级后调用。
        /// </summary>
        /// <param name="layer">新的 UI 层级。</param>
        public void SetLayer(UILayer layer)
        {
            Layer = layer;
        }
    }
}
