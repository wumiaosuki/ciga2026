using System;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 累计扣分到评分档位的映射。
    /// </summary>
    [Serializable]
    public sealed class GradeThresholdEntry
    {
        [Header("评分阈值")]
        [Tooltip("评分档位。")]
        [SerializeField]
        private AnswerGrade grade = AnswerGrade.C;

        [Tooltip("本评分允许的最大累计扣分。累计扣分小于等于该值时命中。")]
        [Min(0)]
        [SerializeField]
        private int maxTotalPenalty = 30;

        /// <summary>
        /// 创建空评分阈值，供 Unity 序列化使用。
        /// </summary>
        public GradeThresholdEntry()
        {
        }

        /// <summary>
        /// 评分档位。
        /// </summary>
        public AnswerGrade Grade => grade;

        /// <summary>
        /// 本评分允许的最大累计扣分。
        /// </summary>
        public int MaxTotalPenalty => Mathf.Max(0, maxTotalPenalty);

        /// <summary>
        /// 创建评分阈值。
        /// </summary>
        /// <param name="grade">评分档位。</param>
        /// <param name="maxTotalPenalty">最大累计扣分。</param>
        public GradeThresholdEntry(AnswerGrade grade, int maxTotalPenalty)
        {
            this.grade = grade;
            this.maxTotalPenalty = maxTotalPenalty;
        }
    }
}
