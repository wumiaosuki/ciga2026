namespace Ciga2026.Game.States
{
    /// <summary>
    /// 游戏顶层流程状态。
    /// </summary>
    public enum GameStateType
    {
        /// <summary>
        /// 主菜单状态。
        /// </summary>
        MainMenu = 0,

        /// <summary>
        /// 正式游戏状态。
        /// </summary>
        Playing = 1,

        /// <summary>
        /// 退出游戏状态。
        /// </summary>
        Exiting = 2
    }
}
