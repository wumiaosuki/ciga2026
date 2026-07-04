namespace Ciga2026.Framework.Audio
{
    /// <summary>
    /// 可互相独立播放的短音效频道。同一频道的新声音会打断上一条声音。
    /// </summary>
    public enum AudioSfxChannel
    {
        /// <summary>
        /// UI 点击、按钮反馈等界面音效。
        /// </summary>
        UI,

        /// <summary>
        /// 角色说话、拟声等语音类音效。
        /// </summary>
        Voice,

        /// <summary>
        /// 警告、结算等其它音效。
        /// </summary>
        Other
    }
}
