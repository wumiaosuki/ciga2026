using Ciga2026.Game;

namespace Ciga2026.Game.States
{
    /// <summary>
    /// 正式游戏状态。
    /// </summary>
    public sealed class PlayingState : GameState
    {
        /// <summary>
        /// 创建正式游戏状态。
        /// </summary>
        /// <param name="owner">持有并驱动状态机的管理器。</param>
        public PlayingState(GameStateManager owner) : base(owner)
        {
        }

        /// <inheritdoc />
        public override GameStateType StateType => GameStateType.Playing;

        /// <inheritdoc />
        public override void Enter()
        {
            Owner.RaiseStateChanged(StateType);
        }
    }
}
