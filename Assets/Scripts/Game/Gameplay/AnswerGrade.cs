namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 一次句子排列命中的评分档位。
    /// </summary>
    public enum AnswerGrade
    {
        /// <summary>
        /// 最优答案，默认扣除最少容忍度。
        /// </summary>
        A = 0,

        /// <summary>
        /// 中等答案。
        /// </summary>
        B = 1,

        /// <summary>
        /// 较差但仍可接受的答案。
        /// </summary>
        C = 2
    }
}
