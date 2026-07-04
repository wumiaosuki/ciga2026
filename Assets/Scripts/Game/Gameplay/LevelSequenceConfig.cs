using System.Collections.Generic;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 多天多关卡流程配置，按天顺序依次播放多个信息 SO。
    /// </summary>
    [CreateAssetMenu(fileName = "LevelSequenceConfig", menuName = "Ciga2026/Gameplay/Level Sequence Config")]
    public sealed class LevelSequenceConfig : ScriptableObject
    {
        [Header("天数列表")]
        [Tooltip("按天组织关卡。流程会按列表顺序执行每一天内的关卡。")]
        [SerializeField]
        private List<DayDefinition> days = new();

        /// <summary>
        /// 按天组织的关卡列表。
        /// </summary>
        public IReadOnlyList<DayDefinition> Days => days;

        /// <summary>
        /// 有效关卡数量。
        /// </summary>
        public int Count => GetDayLevelCount();

        /// <summary>
        /// 是否配置了按天组织的有效关卡。
        /// </summary>
        public bool HasDayLevels => Count > 0;

        /// <summary>
        /// 尝试获取指定索引的关卡信息。
        /// </summary>
        /// <param name="index">关卡索引，从 0 开始。</param>
        /// <param name="level">找到的关卡信息。</param>
        /// <returns>索引有效且关卡非空时返回 true。</returns>
        public bool TryGetLevel(int index, out InformationDefinition level)
        {
            return TryGetLevel(index, out level, out _, out _, out _);
        }

        /// <summary>
        /// 尝试获取指定全局索引的关卡信息和天数信息。
        /// </summary>
        /// <param name="index">全局关卡索引，从 0 开始。</param>
        /// <param name="level">找到的关卡信息。</param>
        /// <param name="dayIndex">天数索引，从 0 开始。旧关卡列表固定为 0。</param>
        /// <param name="dayLevelIndex">当天内关卡索引，从 0 开始。</param>
        /// <param name="isFirstLevelOfDay">是否是当天第一关。</param>
        /// <returns>索引有效且关卡非空时返回 true。</returns>
        public bool TryGetLevel(int index, out InformationDefinition level, out int dayIndex, out int dayLevelIndex, out bool isFirstLevelOfDay)
        {
            if (index < 0)
            {
                level = null;
                dayIndex = -1;
                dayLevelIndex = -1;
                isFirstLevelOfDay = false;
                return false;
            }

            if (HasDayLevels && TryGetDayLevel(index, out level, out dayIndex, out dayLevelIndex, out isFirstLevelOfDay))
            {
                return true;
            }
            
            level = null;
            dayIndex = -1;
            dayLevelIndex = -1;
            isFirstLevelOfDay = false;
            return false;
        }

        private bool TryGetDayLevel(int index, out InformationDefinition level, out int dayIndex, out int dayLevelIndex, out bool isFirstLevelOfDay)
        {
            var cursor = 0;
            for (var day = 0; day < days.Count; day++)
            {
                var group = days[day];
                if (group == null)
                {
                    continue;
                }

                for (var localIndex = 0; localIndex < group.Levels.Count; localIndex++)
                {
                    if (cursor == index)
                    {
                        level = group.Levels[localIndex];
                        dayIndex = day;
                        dayLevelIndex = localIndex;
                        isFirstLevelOfDay = localIndex == 0;
                        return level != null;
                    }

                    cursor++;
                }
            }

            level = null;
            dayIndex = -1;
            dayLevelIndex = -1;
            isFirstLevelOfDay = false;
            return false;
        }

        private int GetDayLevelCount()
        {
            var count = 0;
            for (var i = 0; i < days.Count; i++)
            {
                if (days[i] != null)
                {
                    count += days[i].Levels.Count;
                }
            }

            return count;
        }
    }
}
