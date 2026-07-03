using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 词库分组，用于把大量词语按主题折叠管理。
    /// </summary>
    [Serializable]
    public sealed class WordGroup
    {
        [Header("词语分组")]
        [Tooltip("分组标题，仅用于编辑器管理，例如人物、动作、地点。")]
        [SerializeField]
        private string title;

        [Tooltip("该分组下的词语列表。词语 ID 仍然需要全局唯一。")]
        [SerializeField]
        private List<WordDefinition> words = new();

        /// <summary>
        /// 分组标题。
        /// </summary>
        public string Title => title;

        /// <summary>
        /// 该分组下的词语列表。
        /// </summary>
        public IReadOnlyList<WordDefinition> Words => words;
    }
}
