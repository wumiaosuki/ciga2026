using Ciga2026.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// Game 界面组件绑定。
    /// </summary>
    public sealed class GamePanelBind : UIBind
    {
        [Header("容忍度")]
        [Tooltip("左上角容忍度填充条。")]
        public Image toleranceFillImage;

        [Tooltip("左上角容忍度数字文本。")]
        public TextMeshProUGUI toleranceText;

        [Header("头像")]
        [Tooltip("Head 节点下用于显示当前容忍度档位头像的 Image。")]
        public Image headImage;

        [Header("倒计时")]
        [Tooltip("当前关卡剩余时间填充条。")]
        public Image timerFillImage;

        [Tooltip("当前关卡剩余时间文本，显示两位小数。")]
        public TextMeshProUGUI timerText;

        [Header("词语区域")]
        [Tooltip("左下角词库栏的内容根节点。")]
        public RectTransform wordLibraryContent;

        [Tooltip("输入框内容根节点，玩家点击词语后会移动到这里组成句子。")]
        public RectTransform inputContent;

        [Tooltip("输入框中用于显示已选择词语的 TMP 文本。")]
        public TextMeshProUGUI inputText;

        [Tooltip("词库栏点击移动目标区域。")]
        public WordDropZone wordLibraryDropZone;

        [Tooltip("输入框点击移动目标区域。")]
        public WordDropZone inputDropZone;

        [Tooltip("运行时生成词块使用的 prefab。")]
        public DraggableWordItem wordItemPrefab;

        [Header("信息与提交")]
        [Tooltip("右侧信息面板根节点。为空时会使用 InformationText 的父节点。")]
        public RectTransform infoPanel;

        [Tooltip("右侧显示 NPC 本局信息的 TMP 文本。")]
        public TextMeshProUGUI informationText;

        [Tooltip("TVShow 节点上的图片。信息文本以 & 开头时显示对应 Resources 图片。")]
        public Image tvShowImage;

        [Tooltip("提交按钮。")]
        public Button submitButton;

        [Tooltip("测试用 GM 按钮，点击后直接进入胜利结算。")]
        public Button gmButton;

        [Tooltip("游戏流程内的退出游戏按钮。")]
        public Button gameExitButton;

        [Tooltip("提交后显示评分、扣分或失败原因的 TMP 文本。")]
        public TextMeshProUGUI feedbackText;

        [Header("评分展示")]
        [Tooltip("显示本次评分图片的 Image，初始隐藏。")]
        public Image gradeImage;

        [Header("结算")]
        [Tooltip("胜利或失败时显示的结算面板根节点。")]
        public GameObject settlementPanel;

        [Tooltip("结算标题文本。")]
        public TextMeshProUGUI settlementTitleText;

        [Tooltip("结算说明文本。")]
        public TextMeshProUGUI settlementMessageText;

        [Tooltip("结算面板中的重新开始按钮。")]
        public Button settlementRestartButton;

        [Tooltip("结算面板中的退出游戏按钮。")]
        public Button settlementExitButton;
    }
}
