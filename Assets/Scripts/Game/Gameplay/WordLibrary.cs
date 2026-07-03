using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 全局词库，负责维护词语 ID 到展示文本的映射。
    /// </summary>
    [CreateAssetMenu(fileName = "WordLibrary", menuName = "Ciga2026/Gameplay/Word Library")]
    public sealed class WordLibrary : ScriptableObject
    {
        [Header("词语分组列表")]
        [Tooltip("按主题管理词语。词语 ID 仍然需要全局唯一，重复 ID 会以后出现的配置覆盖先出现的配置。")]
        [SerializeField]
        private List<WordGroup> wordGroups = new();

        private readonly Dictionary<string, WordDefinition> wordsById = new();
        private readonly List<WordDefinition> cachedWords = new();
        private bool isCacheDirty = true;

        /// <summary>
        /// 当前词库内配置的全部词语分组。
        /// </summary>
        public IReadOnlyList<WordGroup> WordGroups => wordGroups;

        /// <summary>
        /// 当前词库内配置的全部词语。
        /// </summary>
        public IReadOnlyList<WordDefinition> Words
        {
            get
            {
                RebuildCacheIfNeeded();
                return cachedWords;
            }
        }

        private void OnEnable()
        {
            isCacheDirty = true;
        }

        private void OnValidate()
        {
            isCacheDirty = true;
        }

        /// <summary>
        /// 尝试按全局 ID 获取词语。
        /// </summary>
        /// <param name="wordId">全局唯一词语 ID。</param>
        /// <param name="word">找到的词语定义。</param>
        /// <returns>找到返回 true，否则返回 false。</returns>
        public bool TryGetWord(string wordId, out WordDefinition word)
        {
            RebuildCacheIfNeeded();

            if (string.IsNullOrWhiteSpace(wordId))
            {
                word = null;
                return false;
            }

            return wordsById.TryGetValue(wordId, out word);
        }

        /// <summary>
        /// 获取词语展示文本，找不到时返回词语 ID 本身，方便调试缺失配置。
        /// </summary>
        /// <param name="wordId">全局唯一词语 ID。</param>
        /// <returns>词语展示文本，或原始 ID。</returns>
        public string GetDisplayText(string wordId)
        {
            return TryGetWord(wordId, out var word) ? word.Text : wordId;
        }

        private void RebuildCacheIfNeeded()
        {
            if (!isCacheDirty)
            {
                return;
            }

            wordsById.Clear();
            cachedWords.Clear();

            foreach (var word in wordGroups.Where(group => group != null).SelectMany(group => group.Words))
            {
                if (word == null || string.IsNullOrWhiteSpace(word.Id))
                {
                    continue;
                }

                wordsById[word.Id] = word;
                cachedWords.Add(word);
            }

            isCacheDirty = false;
        }
    }
}
