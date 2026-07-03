using System;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 一个可被玩家排列的词语定义。
    /// </summary>
    [Serializable]
    public sealed class WordDefinition
    {
        [Header("词语基础信息")]
        [Tooltip("全局唯一词语 ID。建议使用稳定英文或数字，例如 npc_name_001。")]
        [SerializeField]
        private string id;

        [Tooltip("展示给玩家看到的词语文本。")]
        [SerializeField]
        private string text;

        /// <summary>
        /// 全局唯一词语 ID。
        /// </summary>
        public string Id => id;

        /// <summary>
        /// 展示给玩家看到的词语文本。
        /// </summary>
        public string Text => text;
    }
}
