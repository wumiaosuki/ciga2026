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
        private const int DefaultInitialTolerance = 50;
        private const int DefaultMaxTolerance = 100;
        private const int DefaultGradeARecovery = 20;
        private const int DefaultGradeBRecovery = 10;
        private const int DefaultConsecutiveAGradeThreshold = 2;
        private const int DefaultConsecutiveAGradeRecoveryBonus = 10;

        private readonly GameplaySessionConfig sessionConfig;
        private int consecutiveAGradeCount;
        private int currentRoundPenalty;

        /// <summary>
        /// 使用配置创建玩法会话。
        /// </summary>
        /// <param name="sessionConfig">玩法会话配置。为空时使用默认初始容忍度。</param>
        public BroadcastGameplaySession(GameplaySessionConfig sessionConfig)
        {
            this.sessionConfig = sessionConfig;
            MaxTolerance = Mathf.Max(1, sessionConfig != null ? sessionConfig.MaxTolerance : DefaultMaxTolerance);
            InitialTolerance = Mathf.Clamp(sessionConfig != null ? sessionConfig.InitialTolerance : DefaultInitialTolerance, 1, MaxTolerance);
            CurrentTolerance = InitialTolerance;
        }

        /// <summary>
        /// 一局开始时的全局容忍度。
        /// </summary>
        public int InitialTolerance { get; }

        /// <summary>
        /// 当前会话的全局容忍度上限。
        /// </summary>
        public int MaxTolerance { get; }

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
            currentRoundPenalty = 0;
        }

        /// <summary>
        /// 开始新的一关，清空本关累计扣分。
        /// </summary>
        public void BeginRound()
        {
            currentRoundPenalty = 0;
        }

        /// <summary>
        /// 选择词语时立即应用扣分。
        /// </summary>
        /// <param name="penalty">该词语对应的扣分。</param>
        public void ApplyWordSelectionPenalty(int penalty)
        {
            var clampedPenalty = Mathf.Max(0, penalty);
            currentRoundPenalty += clampedPenalty;
            ApplyTolerancePenalty(clampedPenalty);
        }

        /// <summary>
        /// 选词倒计时清零时立即扣除容忍度，但不影响本关评分扣分。
        /// </summary>
        /// <param name="penalty">超时对应的容忍度扣分。</param>
        public void ApplySelectionTimeoutPenalty(int penalty)
        {
            var clampedPenalty = Mathf.Max(0, penalty);
            ApplyTolerancePenalty(clampedPenalty);
        }

        /// <summary>
        /// 本关当前用于评分的累计选词扣分。
        /// </summary>
        public int CurrentRoundPenalty => currentRoundPenalty;

        /// <summary>
        /// 提交一组有序词语 ID，并根据本关累计扣分给出评分。提交本身不再扣分。
        /// </summary>
        /// <param name="information">当前 NPC 信息定义。</param>
        /// <param name="submittedWordIds">玩家按顺序提交的词语 ID。</param>
        /// <returns>评估结果，包含评分、累计扣分、回复和是否游戏结束。</returns>
        public SentenceEvaluationResult SubmitAnswer(InformationDefinition information, IReadOnlyList<string> submittedWordIds)
        {
            if (information == null)
            {
                throw new ArgumentNullException(nameof(information));
            }

            var grade = sessionConfig != null
                ? sessionConfig.GetGradeByTotalPenalty(currentRoundPenalty)
                : GetDefaultGradeByTotalPenalty(currentRoundPenalty);
            var recovery = ApplyToleranceRecovery(grade);

            return new SentenceEvaluationResult(
                true,
                grade,
                currentRoundPenalty,
                recovery,
                consecutiveAGradeCount,
                CurrentTolerance,
                IsGameOver,
                null);
        }

        private int GetGradeARecovery()
        {
            return sessionConfig != null
                ? Mathf.Max(0, sessionConfig.GradeARecovery)
                : DefaultGradeARecovery;
        }

        private static AnswerGrade GetDefaultGradeByTotalPenalty(int totalPenalty)
        {
            var penalty = Mathf.Max(0, totalPenalty);
            if (penalty <= 0)
            {
                return AnswerGrade.A;
            }

            if (penalty <= 10)
            {
                return AnswerGrade.B;
            }

            if (penalty <= 30)
            {
                return AnswerGrade.C;
            }

            if (penalty <= 50)
            {
                return AnswerGrade.D;
            }

            return AnswerGrade.E;
        }

        private int GetGradeBRecovery()
        {
            return sessionConfig != null
                ? Mathf.Max(0, sessionConfig.GradeBRecovery)
                : DefaultGradeBRecovery;
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

        private void ApplyTolerancePenalty(int penalty)
        {
            CurrentTolerance = Mathf.Max(0, CurrentTolerance - Mathf.Max(0, penalty));
        }

        private int ApplyToleranceRecovery(AnswerGrade grade)
        {
            if (IsGameOver)
            {
                consecutiveAGradeCount = 0;
                return 0;
            }

            consecutiveAGradeCount = grade == AnswerGrade.A ? consecutiveAGradeCount + 1 : 0;

            var recovery = grade switch
            {
                AnswerGrade.A => GetGradeARecovery()
                    + (consecutiveAGradeCount >= GetConsecutiveAGradeThreshold() ? GetConsecutiveAGradeRecoveryBonus() : 0),
                AnswerGrade.B => GetGradeBRecovery(),
                _ => 0
            };

            CurrentTolerance = Mathf.Min(MaxTolerance, CurrentTolerance + recovery);
            return recovery;
        }
    }
}
