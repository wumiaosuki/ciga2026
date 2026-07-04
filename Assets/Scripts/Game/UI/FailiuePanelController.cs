using Ciga2026.Framework.UI;
using Ciga2026.Game.Audio;
using Ciga2026.Game.Gameplay;
using UnityEngine;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// 失败弹窗交互逻辑。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FailiuePanelBind))]
    public sealed class FailiuePanelController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("失败弹窗组件绑定。为空时会从当前对象自动获取。")]
        private FailiuePanelBind bind;

        [SerializeField]
        [Tooltip("玩法会话配置，用于读取 UI 点击音效。")]
        private GameplaySessionConfig sessionConfig;

        private void Awake()
        {
            if (bind == null)
            {
                bind = GetComponent<FailiuePanelBind>();
            }
        }

        private void OnEnable()
        {
            if (bind != null && bind.returnButton != null)
            {
                bind.returnButton.onClick.AddListener(OnReturnButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (bind != null && bind.returnButton != null)
            {
                bind.returnButton.onClick.RemoveListener(OnReturnButtonClicked);
            }
        }

        private void OnReturnButtonClicked()
        {
            GameAudioPlayer.PlayUiClick(sessionConfig);

            if (UIManager.TryGetInstance(out var uiManager))
            {
                uiManager.ReleasePanel("FailiuePanel");
                uiManager.ReleasePanel(UIResourceType.Game.ToString());
                uiManager.ShowPanel(UIResourceType.StartMeau, true);
            }

            if (GameStateManager.TryGetInstance(out var gameStateManager))
            {
                gameStateManager.GoToMainMenu();
            }
        }
    }
}
