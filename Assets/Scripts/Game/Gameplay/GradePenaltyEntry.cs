using System;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 一个评分档位对应的容忍度扣除规则。
    /// </summary>
    [Serializable]
    public sealed class GradePenaltyEntry
    {
        /// <summary>
        /// 创建一个空评分扣分项，供 Unity 序列化使用。
        /// </summary>
        public GradePenaltyEntry()
        {
        }

        /// <summary>
        /// 创建一个评分扣分项。
        /// </summary>
        /// <param name="grade">评分档位。</param>
        /// <param name="tolerancePenalty">该评分档位对应扣除的容忍度。</param>
        public GradePenaltyEntry(AnswerGrade grade, int tolerancePenalty)
        {
            this.grade = grade;
            this.tolerancePenalty = Mathf.Max(0, tolerancePenalty);
        }

        [Header("评分扣分")]
        [Tooltip("命中的答案评分档位。")]
        [SerializeField]
        private AnswerGrade grade;

        [Tooltip("命中该评分档位时扣除的容忍度。数值会被限制为不小于 0。")]
        [Min(0)]
        [SerializeField]
        private int tolerancePenalty;

        /// <summary>
        /// 评分档位。
        /// </summary>
        public AnswerGrade Grade => grade;

        /// <summary>
        /// 对应扣除的容忍度。
        /// </summary>
        public int TolerancePenalty => tolerancePenalty;
    }
}
