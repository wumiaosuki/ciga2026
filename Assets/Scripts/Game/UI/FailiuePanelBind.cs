using Ciga2026.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// 失败弹窗组件绑定。
    /// </summary>
    public sealed class FailiuePanelBind : UIBind
    {
        [Header("图片")]
        [Tooltip("失败弹窗背景遮罩。")]
        public Image bgImage;

        [Tooltip("失败弹窗主体图片。")]
        public Image panelImage;

        [Header("文本")]
        [Tooltip("失败弹窗说明文本。")]
        public TextMeshProUGUI messageText;

        [Tooltip("返回按钮文本。")]
        public TextMeshProUGUI returnButtonText;

        [Header("按钮")]
        [Tooltip("返回主菜单按钮。")]
        public Button returnButton;
    }
}
