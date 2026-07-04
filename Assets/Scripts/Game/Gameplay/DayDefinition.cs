using System.Collections.Generic;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 一天的关卡配置。流程会按天顺序、再按当天关卡顺序执行。
    /// </summary>
    [CreateAssetMenu(fileName = "DayDefinition", menuName = "Ciga2026/Gameplay/Day Definition")]
    public sealed class DayDefinition : ScriptableObject
    {
        [Header("天数")]
        [Tooltip("天数标题，例如 Day 1 / 第一天。")]
        [SerializeField]
        private string title;

        [Tooltip("这一天内按顺序执行的关卡。")]
        [SerializeField]
        private List<InformationDefinition> levels = new();

        /// <summary>
        /// 天数标题。
        /// </summary>
        public string Title => title;

        /// <summary>
        /// 这一天内按顺序执行的关卡。
        /// </summary>
        public IReadOnlyList<InformationDefinition> Levels => levels;
    }
}
