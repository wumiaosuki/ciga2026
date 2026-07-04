using System;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 关卡中一个可被抽出的词语选项。
    /// </summary>
    [Serializable]
    public sealed class WordChoiceEntry
    {
        [Header("词语选项")]
        [Tooltip("词语 ID。标准词使用全局词 ID；混淆词可使用导入器生成的内部 ID。")]
        [SerializeField]
        private string wordId;

        [Tooltip("展示给玩家看到的词语文本。为空时会回退到全局词库按 ID 查询。")]
        [SerializeField]
        private string displayText;

        [Tooltip("选择该词语时立即扣除的容忍度。正确词通常填 0，混淆词按错误程度填写。")]
        [Min(0)]
        [SerializeField]
        private int tolerancePenalty;

        [Tooltip("策划备注，仅用于说明。")]
        [TextArea]
        [SerializeField]
        private string note;

        /// <summary>
        /// 创建空词语选项，供 Unity 序列化使用。
        /// </summary>
        public WordChoiceEntry()
        {
        }

        /// <summary>
        /// 创建词语选项。
        /// </summary>
        /// <param name="wordId">词语 ID。</param>
        /// <param name="displayText">展示给玩家看到的文本。</param>
        /// <param name="tolerancePenalty">选择时即时扣分。</param>
        public WordChoiceEntry(string wordId, string displayText, int tolerancePenalty)
        {
            this.wordId = wordId;
            this.displayText = displayText;
            this.tolerancePenalty = tolerancePenalty;
        }

        /// <summary>
        /// 创建词语选项，展示文本回退到词库查询。
        /// </summary>
        /// <param name="wordId">词语 ID。</param>
        /// <param name="tolerancePenalty">选择时即时扣分。</param>
        public WordChoiceEntry(string wordId, int tolerancePenalty)
            : this(wordId, null, tolerancePenalty)
        {
        }

        /// <summary>
        /// 词语 ID。
        /// </summary>
        public string WordId => wordId;

        /// <summary>
        /// 展示给玩家看到的词语文本。
        /// </summary>
        public string DisplayText => displayText;

        /// <summary>
        /// 选择该词语时立即扣除的容忍度。
        /// </summary>
        public int TolerancePenalty => Mathf.Max(0, tolerancePenalty);

        /// <summary>
        /// 策划备注。
        /// </summary>
        public string Note => note;
    }
}
