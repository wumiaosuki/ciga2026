using System.Collections.Generic;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 多关卡流程配置，按列表顺序依次播放多个信息 SO。
    /// </summary>
    [CreateAssetMenu(fileName = "LevelSequenceConfig", menuName = "Ciga2026/Gameplay/Level Sequence Config")]
    public sealed class LevelSequenceConfig : ScriptableObject
    {
        [Header("关卡列表")]
        [Tooltip("按顺序拖入多个 InformationDefinition。每次提交后进入下一关，容忍度归零时结束。")]
        [SerializeField]
        private List<InformationDefinition> levels = new();

        /// <summary>
        /// 按顺序执行的关卡信息列表。
        /// </summary>
        public IReadOnlyList<InformationDefinition> Levels => levels;

        /// <summary>
        /// 有效关卡数量。
        /// </summary>
        public int Count => levels.Count;

        /// <summary>
        /// 尝试获取指定索引的关卡信息。
        /// </summary>
        /// <param name="index">关卡索引，从 0 开始。</param>
        /// <param name="level">找到的关卡信息。</param>
        /// <returns>索引有效且关卡非空时返回 true。</returns>
        public bool TryGetLevel(int index, out InformationDefinition level)
        {
            if (index < 0 || index >= levels.Count)
            {
                level = null;
                return false;
            }

            level = levels[index];
            return level != null;
        }
    }
}
