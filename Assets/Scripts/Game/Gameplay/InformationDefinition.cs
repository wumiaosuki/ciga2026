using System.Collections.Generic;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// NPC 提供的一条信息，以及这条信息可接受的多个评分档答案。
    /// </summary>
    [CreateAssetMenu(fileName = "InformationDefinition", menuName = "Ciga2026/Gameplay/Information Definition")]
    public sealed class InformationDefinition : ScriptableObject
    {
        [Header("信息基础内容")]
        [Tooltip("信息 ID。用于流程、存档或调试定位。")]
        [SerializeField]
        private string id;

        [Tooltip("NPC 提供给玩家的信息文本。")]
        [TextArea]
        [SerializeField]
        private string informationText;

        [Header("本轮信息可用词语")]
        [Tooltip("这条信息对应可供玩家排列的词语 ID。词语文本从全局词库读取。")]
        [SerializeField]
        private List<string> availableWordIds = new();

        [Header("评分档答案")]
        [Tooltip("同一条信息可配置多个答案组合，每个组合命中不同评分档。")]
        [SerializeField]
        private List<AnswerCombination> answerCombinations = new();

        /// <summary>
        /// 信息 ID。
        /// </summary>
        public string Id => id;

        /// <summary>
        /// NPC 提供给玩家的信息文本。
        /// </summary>
        public string InformationText => informationText;

        /// <summary>
        /// 本轮信息可供玩家排列的词语 ID。
        /// </summary>
        public IReadOnlyList<string> AvailableWordIds => availableWordIds;

        /// <summary>
        /// 这条信息可接受的评分档答案组合。
        /// </summary>
        public IReadOnlyList<AnswerCombination> AnswerCombinations => answerCombinations;
    }
}
