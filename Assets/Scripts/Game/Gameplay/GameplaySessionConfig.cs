using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 核心玩法运行时会话配置。
    /// </summary>
    [CreateAssetMenu(fileName = "GameplaySessionConfig", menuName = "Ciga2026/Gameplay/Session Config")]
    public sealed class GameplaySessionConfig : ScriptableObject
    {
        [Header("容忍度")]
        [Tooltip("一局开始时的全局容忍度。降到 0 时游戏结束。")]
        [Min(1)]
        [SerializeField]
        private int initialTolerance = 100;

        [Tooltip("评分档位对应的容忍度扣分配置。为空时使用运行时默认值。")]
        [SerializeField]
        private GradePenaltyConfig gradePenaltyConfig;

        [Header("难度")]
        [Tooltip("第一关默认倒计时时长，单位为秒。")]
        [Min(0.01f)]
        [SerializeField]
        private float initialLevelDuration = 5f;

        [Tooltip("按关卡进度计算倒计时时长倍率。横轴 0 表示第一关，1 表示最后一关；纵轴乘以初始倒计时。")]
        [SerializeField]
        private AnimationCurve levelDurationMultiplierCurve = new(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.5f));

        /// <summary>
        /// 一局开始时的全局容忍度。
        /// </summary>
        public int InitialTolerance => initialTolerance;

        /// <summary>
        /// 评分档位对应的扣分配置。
        /// </summary>
        public GradePenaltyConfig GradePenaltyConfig => gradePenaltyConfig;

        /// <summary>
        /// 第一关默认倒计时时长，单位为秒。
        /// </summary>
        public float InitialLevelDuration => initialLevelDuration;

        /// <summary>
        /// 获取指定关卡的倒计时时长。
        /// </summary>
        /// <param name="levelIndex">当前关卡索引，从 0 开始。</param>
        /// <param name="levelCount">本轮总关卡数。</param>
        /// <returns>本关倒计时时长，单位为秒。</returns>
        public float GetLevelDuration(int levelIndex, int levelCount)
        {
            var normalizedProgress = levelCount <= 1 ? 0f : Mathf.Clamp01((float)levelIndex / (levelCount - 1));
            var multiplier = levelDurationMultiplierCurve != null
                ? Mathf.Max(0.01f, levelDurationMultiplierCurve.Evaluate(normalizedProgress))
                : 1f;

            return Mathf.Max(0.01f, initialLevelDuration * multiplier);
        }
    }
}
