using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 播报员核心玩法会话，负责评估玩家排列的词语并维护全局容忍度。
    /// </summary>
    public sealed class BroadcastGameplaySession
    {
        private const int DefaultInitialTolerance = 100;
        private const int DefaultUnmatchedPenalty = 30;
        private const int DefaultGradeARecovery = 5;
        private const int DefaultConsecutiveAGradeThreshold = 2;
        private const int DefaultConsecutiveAGradeRecoveryBonus = 10;

        private readonly GameplaySessionConfig sessionConfig;
        private int consecutiveAGradeCount;

        /// <summary>
        /// 使用配置创建玩法会话。
        /// </summary>
        /// <param name="sessionConfig">玩法会话配置。为空时使用默认初始容忍度。</param>
        public BroadcastGameplaySession(GameplaySessionConfig sessionConfig)
        {
            this.sessionConfig = sessionConfig;
            InitialTolerance = Mathf.Max(1, sessionConfig != null ? sessionConfig.InitialTolerance : DefaultInitialTolerance);
            CurrentTolerance = InitialTolerance;
        }

        /// <summary>
        /// 一局开始时的全局容忍度。
        /// </summary>
        public int InitialTolerance { get; }

        /// <summary>
        /// 当前全局容忍度。
        /// </summary>
        public int CurrentTolerance { get; private set; }

        /// <summary>
        /// 当前容忍度是否已经降到 0。
        /// </summary>
        public bool IsGameOver => CurrentTolerance <= 0;

        /// <summary>
        /// 重置当前会话容忍度。
        /// </summary>
        public void Reset()
        {
            CurrentTolerance = InitialTolerance;
            consecutiveAGradeCount = 0;
        }

        /// <summary>
        /// 提交一组有序词语 ID，并根据命中的评分档扣除全局容忍度。
        /// </summary>
        /// <param name="information">当前 NPC 信息定义。</param>
        /// <param name="submittedWordIds">玩家按顺序提交的词语 ID。</param>
        /// <returns>评估结果，包含评分、扣分和是否游戏结束。</returns>
        public SentenceEvaluationResult SubmitAnswer(InformationDefinition information, IReadOnlyList<string> submittedWordIds)
        {
            if (information == null)
            {
                throw new ArgumentNullException(nameof(information));
            }

            var matchedCombination = FindMatchedCombination(information, submittedWordIds);
            var isMatched = matchedCombination != null;
            var grade = isMatched ? matchedCombination.Grade : (AnswerGrade?)null;
            var penalty = isMatched ? GetPenalty(matchedCombination.Grade) : GetUnmatchedPenalty();

            ApplyTolerancePenalty(penalty);
            var recovery = ApplyToleranceRecovery(grade);

            return new SentenceEvaluationResult(
                isMatched,
                grade,
                penalty,
                recovery,
                consecutiveAGradeCount,
                CurrentTolerance,
                IsGameOver,
                matchedCombination);
        }

        private static AnswerCombination FindMatchedCombination(InformationDefinition information, IReadOnlyList<string> submittedWordIds)
        {
            var combinations = information.AnswerCombinations;

            for (var i = 0; i < combinations.Count; i++)
            {
                var combination = combinations[i];
                if (combination != null && combination.Matches(submittedWordIds))
                {
                    return combination;
                }
            }

            return null;
        }

        private int GetPenalty(AnswerGrade grade)
        {
            return sessionConfig != null && sessionConfig.GradePenaltyConfig != null
                ? sessionConfig.GradePenaltyConfig.GetPenalty(grade)
                : GetDefaultPenalty(grade);
        }

        private int GetUnmatchedPenalty()
        {
            return sessionConfig != null && sessionConfig.GradePenaltyConfig != null
                ? sessionConfig.GradePenaltyConfig.GetUnmatchedPenalty()
                : DefaultUnmatchedPenalty;
        }

        private int GetGradeARecovery()
        {
            return sessionConfig != null
                ? Mathf.Max(0, sessionConfig.GradeARecovery)
                : DefaultGradeARecovery;
        }

        private int GetConsecutiveAGradeThreshold()
        {
            return sessionConfig != null
                ? Mathf.Max(2, sessionConfig.ConsecutiveAGradeThreshold)
                : DefaultConsecutiveAGradeThreshold;
        }

        private int GetConsecutiveAGradeRecoveryBonus()
        {
            return sessionConfig != null
                ? Mathf.Max(0, sessionConfig.ConsecutiveAGradeRecoveryBonus)
                : DefaultConsecutiveAGradeRecoveryBonus;
        }

        private static int GetDefaultPenalty(AnswerGrade grade)
        {
            return grade switch
            {
                AnswerGrade.A => 0,
                AnswerGrade.B => 10,
                AnswerGrade.C => 20,
                _ => DefaultUnmatchedPenalty
            };
        }

        private void ApplyTolerancePenalty(int penalty)
        {
            CurrentTolerance = Mathf.Max(0, CurrentTolerance - Mathf.Max(0, penalty));
        }

        private int ApplyToleranceRecovery(AnswerGrade? grade)
        {
            if (grade != AnswerGrade.A || IsGameOver)
            {
                consecutiveAGradeCount = 0;
                return 0;
            }

            consecutiveAGradeCount++;

            var recovery = GetGradeARecovery();
            if (consecutiveAGradeCount >= GetConsecutiveAGradeThreshold())
            {
                recovery += GetConsecutiveAGradeRecoveryBonus();
            }

            CurrentTolerance = Mathf.Min(InitialTolerance, CurrentTolerance + recovery);
            return recovery;
        }
    }
}
