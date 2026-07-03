namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 一次句子提交后的评估结果。
    /// </summary>
    public readonly struct SentenceEvaluationResult
    {
        /// <summary>
        /// 创建一次句子评估结果。
        /// </summary>
        /// <param name="isMatched">是否命中任意答案组合。</param>
        /// <param name="grade">命中的评分档位。未命中时为 null。</param>
        /// <param name="tolerancePenalty">本次扣除的容忍度。</param>
        /// <param name="remainingTolerance">扣除后的剩余容忍度。</param>
        /// <param name="isGameOver">容忍度是否已经降到 0。</param>
        /// <param name="matchedCombination">命中的答案组合。未命中时为 null。</param>
        public SentenceEvaluationResult(
            bool isMatched,
            AnswerGrade? grade,
            int tolerancePenalty,
            int remainingTolerance,
            bool isGameOver,
            AnswerCombination matchedCombination)
        {
            IsMatched = isMatched;
            Grade = grade;
            TolerancePenalty = tolerancePenalty;
            RemainingTolerance = remainingTolerance;
            IsGameOver = isGameOver;
            MatchedCombination = matchedCombination;
        }

        /// <summary>
        /// 是否命中任意答案组合。
        /// </summary>
        public bool IsMatched { get; }

        /// <summary>
        /// 命中的评分档位。未命中时为 null。
        /// </summary>
        public AnswerGrade? Grade { get; }

        /// <summary>
        /// 本次扣除的容忍度。
        /// </summary>
        public int TolerancePenalty { get; }

        /// <summary>
        /// 扣除后的剩余容忍度。
        /// </summary>
        public int RemainingTolerance { get; }

        /// <summary>
        /// 容忍度是否已经降到 0。
        /// </summary>
        public bool IsGameOver { get; }

        /// <summary>
        /// 命中的答案组合。未命中时为 null。
        /// </summary>
        public AnswerCombination MatchedCombination { get; }
    }
}
