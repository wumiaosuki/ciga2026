using System.Collections.Generic;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 评分档位到容忍度扣分的配置表。
    /// </summary>
    [CreateAssetMenu(fileName = "GradePenaltyConfig", menuName = "Ciga2026/Gameplay/Grade Penalty Config")]
    public sealed class GradePenaltyConfig : ScriptableObject
    {
        [Header("默认扣分")]
        [Tooltip("没有找到对应评分配置时使用的默认扣分。")]
        [Min(0)]
        [SerializeField]
        private int fallbackPenalty = 20;

        [Tooltip("提交答案没有命中任何组合时扣除的容忍度。")]
        [Min(0)]
        [SerializeField]
        private int unmatchedPenalty = 30;

        [Header("评分档扣分配置")]
        [Tooltip("每个评分档位对应的容忍度扣除。建议配置 A/B/C 三档。")]
        [SerializeField]
        private List<GradePenaltyEntry> penalties = new()
        {
            new GradePenaltyEntry(AnswerGrade.A, 0),
            new GradePenaltyEntry(AnswerGrade.B, 10),
            new GradePenaltyEntry(AnswerGrade.C, 20)
        };

        private readonly Dictionary<AnswerGrade, int> penaltiesByGrade = new();
        private bool isCacheDirty = true;

        /// <summary>
        /// 当前配置的默认扣分。
        /// </summary>
        public int FallbackPenalty => fallbackPenalty;

        /// <summary>
        /// 未命中任何答案组合时扣除的容忍度。
        /// </summary>
        public int UnmatchedPenalty => unmatchedPenalty;

        /// <summary>
        /// 评分扣分配置列表。
        /// </summary>
        public IReadOnlyList<GradePenaltyEntry> Penalties => penalties;

        private void OnEnable()
        {
            isCacheDirty = true;
        }

        private void OnValidate()
        {
            isCacheDirty = true;
        }

        /// <summary>
        /// 获取指定评分档位对应的容忍度扣除。
        /// </summary>
        /// <param name="grade">评分档位。</param>
        /// <returns>容忍度扣除值。</returns>
        public int GetPenalty(AnswerGrade grade)
        {
            RebuildCacheIfNeeded();
            return penaltiesByGrade.TryGetValue(grade, out var penalty) ? penalty : fallbackPenalty;
        }

        /// <summary>
        /// 获取未命中答案时的容忍度扣除。
        /// </summary>
        /// <returns>未命中扣除值。</returns>
        public int GetUnmatchedPenalty()
        {
            return Mathf.Max(0, unmatchedPenalty);
        }

        private void RebuildCacheIfNeeded()
        {
            if (!isCacheDirty)
            {
                return;
            }

            penaltiesByGrade.Clear();

            foreach (var entry in penalties)
            {
                if (entry == null)
                {
                    continue;
                }

                penaltiesByGrade[entry.Grade] = Mathf.Max(0, entry.TolerancePenalty);
            }

            isCacheDirty = false;
        }
    }
}
