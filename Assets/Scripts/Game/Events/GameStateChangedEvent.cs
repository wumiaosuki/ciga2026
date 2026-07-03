using Ciga2026.Game.States;

namespace Ciga2026.Game.Events
{
    /// <summary>
    /// 游戏顶层状态切换事件，可由 UI、音频或系统模块订阅。
    /// </summary>
    public readonly struct GameStateChangedEvent
    {
        /// <summary>
        /// 创建游戏状态切换事件。
        /// </summary>
        /// <param name="previousState">切换前状态；首次进入时为 null。</param>
        /// <param name="currentState">切换后状态。</param>
        public GameStateChangedEvent(GameStateType? previousState, GameStateType currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }

        /// <summary>
        /// 切换前状态；首次进入时为 null。
        /// </summary>
        public GameStateType? PreviousState { get; }

        /// <summary>
        /// 当前状态。
        /// </summary>
        public GameStateType CurrentState { get; }
    }
}
