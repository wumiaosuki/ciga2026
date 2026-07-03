using Ciga2026.Game;

namespace Ciga2026.Game.States
{
    /// <summary>
    /// 主菜单状态。
    /// </summary>
    public sealed class MainMenuState : GameState
    {
        /// <summary>
        /// 创建主菜单状态。
        /// </summary>
        /// <param name="owner">持有并驱动状态机的管理器。</param>
        public MainMenuState(GameStateManager owner) : base(owner)
        {
        }

        /// <inheritdoc />
        public override GameStateType StateType => GameStateType.MainMenu;

        /// <inheritdoc />
        public override void Enter()
        {
            Owner.RaiseStateChanged(StateType);
        }
    }
}
