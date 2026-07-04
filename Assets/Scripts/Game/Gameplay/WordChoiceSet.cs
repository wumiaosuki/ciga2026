using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 一组互斥词语选项。玩家选择其中一个后，同组其它混淆词会同时消失。
    /// </summary>
    [Serializable]
    public sealed class WordChoiceSet
    {
        [Header("互斥词组")]
        [Tooltip("词组标题，用于策划识别，例如“主语”“事件”“地点”。")]
        [SerializeField]
        private string title;

        [Tooltip("同一组互斥词。每个选项都需要配置选择时的即时扣分。")]
        [SerializeField]
        private List<WordChoiceEntry> choices = new();

        /// <summary>
        /// 创建空互斥词组，供 Unity 序列化使用。
        /// </summary>
        public WordChoiceSet()
        {
        }

        /// <summary>
        /// 创建互斥词组。
        /// </summary>
        /// <param name="title">词组标题。</param>
        /// <param name="choices">同组词语选项。</param>
        public WordChoiceSet(string title, IEnumerable<WordChoiceEntry> choices)
        {
            this.title = title;
            this.choices = choices != null ? choices.Where(choice => choice != null).ToList() : new List<WordChoiceEntry>();
        }

        /// <summary>
        /// 词组标题。
        /// </summary>
        public string Title => title;

        /// <summary>
        /// 同一组互斥词。
        /// </summary>
        public IReadOnlyList<WordChoiceEntry> Choices => choices;
    }
}
