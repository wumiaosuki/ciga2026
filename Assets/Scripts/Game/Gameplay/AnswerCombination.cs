using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 一条信息下的一个可接受答案组合。
    /// </summary>
    [Serializable]
    public sealed class AnswerCombination
    {
        [Header("答案组合")]
        [Tooltip("按顺序排列的词语 ID。顺序不同会被视为不同答案。")]
        [SerializeField]
        private List<string> wordIds = new();

        [Tooltip("使用 / 分隔的有序词语 ID，例如 npc/said/fire。填写后优先使用该字段判定。")]
        [SerializeField]
        private string wordIdPattern;

        [Tooltip("该组合命中后的评分档位。")]
        [SerializeField]
        private AnswerGrade grade = AnswerGrade.C;

        [Tooltip("策划备注，仅用于编辑器说明，不参与逻辑。")]
        [TextArea]
        [SerializeField]
        private string note;

        /// <summary>
        /// 该答案要求的有序词语 ID。
        /// </summary>
        public IReadOnlyList<string> WordIds => wordIds;

        /// <summary>
        /// 使用 / 分隔的有序词语 ID。非空时优先用于判定。
        /// </summary>
        public string WordIdPattern => wordIdPattern;

        /// <summary>
        /// 命中该答案后的评分档位。
        /// </summary>
        public AnswerGrade Grade => grade;

        /// <summary>
        /// 策划备注。
        /// </summary>
        public string Note => note;

        /// <summary>
        /// 检查玩家提交的词语 ID 顺序是否与该组合完全一致。
        /// </summary>
        /// <param name="submittedWordIds">玩家提交的有序词语 ID。</param>
        /// <returns>完全一致返回 true。</returns>
        public bool Matches(IReadOnlyList<string> submittedWordIds)
        {
            var expectedWordIds = GetExpectedWordIds();

            if (submittedWordIds == null || submittedWordIds.Count != expectedWordIds.Count)
            {
                return false;
            }

            for (var i = 0; i < expectedWordIds.Count; i++)
            {
                if (submittedWordIds[i] != expectedWordIds[i])
                {
                    return false;
                }
            }

            return true;
        }

        private IReadOnlyList<string> GetExpectedWordIds()
        {
            if (string.IsNullOrWhiteSpace(wordIdPattern))
            {
                return wordIds;
            }

            return wordIdPattern
                .Split('/')
                .Select(wordId => wordId.Trim())
                .Where(wordId => !string.IsNullOrEmpty(wordId))
                .ToArray();
        }
    }
}
