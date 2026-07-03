using Ciga2026.Framework.StateMachine;
using Ciga2026.Game;

namespace Ciga2026.Game.States
{
    /// <summary>
    /// 游戏顶层状态基类。
    /// </summary>
    public abstract class GameState : State
    {
        /// <summary>
        /// 创建游戏顶层状态。
        /// </summary>
        /// <param name="owner">持有并驱动状态机的管理器。</param>
        protected GameState(GameStateManager owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// 当前状态类型。
        /// </summary>
        public abstract GameStateType StateType { get; }

        /// <summary>
        /// 持有并驱动状态机的管理器。
        /// </summary>
        protected GameStateManager Owner { get; }

        /// <summary>
        /// 从任意状态请求退出游戏。
        /// </summary>
        protected void RequestExitGame()
        {
            Owner.ExitGame();
        }
    }
}
