using Ciga2026.Game;

namespace Ciga2026.Game.States
{
    /// <summary>
    /// 退出游戏状态。
    /// </summary>
    public sealed class ExitGameState : GameState
    {
        /// <summary>
        /// 创建退出游戏状态。
        /// </summary>
        /// <param name="owner">持有并驱动状态机的管理器。</param>
        public ExitGameState(GameStateManager owner) : base(owner)
        {
        }

        /// <inheritdoc />
        public override GameStateType StateType => GameStateType.Exiting;

        /// <inheritdoc />
        public override void Enter()
        {
            Owner.RaiseStateChanged(StateType);
            Owner.QuitApplication();
        }
    }
}
