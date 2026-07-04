using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// NPC 提供的一条信息，以及本关可抽取的词组选项。
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

        [Header("本轮词组选项")]
        [Tooltip("本条信息同屏希望出现的关联词组数量。待选区实际互斥词组上限为该值 + 1；混淆词属于词组内部选项，不占用该上限。")]
        [Min(1)]
        [SerializeField]
        [FormerlySerializedAs("visibleChoiceSetCount")]
        private int relatedWordCount = 3;

        [Tooltip("按抽取顺序配置的互斥词组。每个词组内可以放正确词和多个混淆词。")]
        [SerializeField]
        private List<WordChoiceSet> choiceSets = new();

        /// <summary>
        /// 信息 ID。
        /// </summary>
        public string Id => id;

        /// <summary>
        /// NPC 提供给玩家的信息文本。
        /// </summary>
        public string InformationText => informationText;

        /// <summary>
        /// 本条信息同屏希望出现的关联词组数量。
        /// </summary>
        public int RelatedWordCount => Mathf.Max(1, relatedWordCount);

        /// <summary>
        /// 待选区实际互斥词组显示上限。
        /// </summary>
        public int VisibleChoiceSetLimit => RelatedWordCount + 1;

        /// <summary>
        /// 本轮按抽取顺序配置的互斥词组。
        /// </summary>
        public IReadOnlyList<WordChoiceSet> ChoiceSets => choiceSets;

    }
}
