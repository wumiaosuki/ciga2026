using Ciga2026.Framework.UI;
using UnityEngine;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// StartMeau 界面交互逻辑。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StartMeauBind))]
    public sealed class StartMeauController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("StartMeau 的组件绑定。为空时会从当前对象自动获取。")]
        private StartMeauBind bind;

        private void Awake()
        {
            if (bind == null)
            {
                bind = GetComponent<StartMeauBind>();
            }
        }

        private void OnEnable()
        {
            if (bind == null)
            {
                return;
            }

            if (bind.startButton != null)
            {
                bind.startButton.onClick.AddListener(OnStartClicked);
            }

            if (bind.exitButton != null)
            {
                bind.exitButton.onClick.AddListener(OnExitClicked);
            }
        }

        private void OnDisable()
        {
            if (bind == null)
            {
                return;
            }

            if (bind.startButton != null)
            {
                bind.startButton.onClick.RemoveListener(OnStartClicked);
            }

            if (bind.exitButton != null)
            {
                bind.exitButton.onClick.RemoveListener(OnExitClicked);
            }
        }

        private void OnStartClicked()
        {
            if (GameStateManager.TryGetInstance(out var gameStateManager))
            {
                gameStateManager.EnterGame();
            }

            if (UIManager.TryGetInstance(out var uiManager))
            {
                uiManager.OpenPanel(UIResourceType.Game, layer: UILayer.Normal);
                uiManager.ShowPanel(UIResourceType.StartMeau, false);
            }
        }

        private void OnExitClicked()
        {
            if (GameStateManager.TryGetInstance(out var gameStateManager))
            {
                gameStateManager.ExitGame();
            }
        }
    }
}
