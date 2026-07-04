using System.Collections.Generic;
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

        [Header("累计扣分评分")]
        [Tooltip("全局评分阈值。按本关累计扣分从上到下匹配，建议保持 A=0，B=10，C=30，D=50，E=70。")]
        [SerializeField]
        private List<GradeThresholdEntry> gradeThresholds = new()
        {
            new GradeThresholdEntry(AnswerGrade.A, 0),
            new GradeThresholdEntry(AnswerGrade.B, 10),
            new GradeThresholdEntry(AnswerGrade.C, 30),
            new GradeThresholdEntry(AnswerGrade.D, 50),
            new GradeThresholdEntry(AnswerGrade.E, 70)
        };

        [Header("耐心回复")]
        [Tooltip("单次评分为 A 时回复的容忍度。")]
        [Min(0)]
        [SerializeField]
        private int gradeARecovery = 20;

        [Tooltip("单次评分为 B 时回复的容忍度。")]
        [Min(0)]
        [SerializeField]
        private int gradeBRecovery = 10;

        [Tooltip("连续多少次 A 后触发额外回复。小于 2 时按 2 处理。")]
        [Min(2)]
        [SerializeField]
        private int consecutiveAGradeThreshold = 2;

        [Tooltip("达到连续 A 阈值后，每次 A 额外回复的容忍度。")]
        [Min(0)]
        [SerializeField]
        private int consecutiveAGradeRecoveryBonus = 10;

        [Header("难度")]
        [Tooltip("基础选词倒计时时长，单位为秒。实际时长会乘以关卡进度曲线。")]
        [Min(0.01f)]
        [SerializeField]
        private float initialLevelDuration = 5f;

        [Tooltip("单次选词倒计时清零时扣除的容忍度，也会计入本关累计扣分。")]
        [Min(0)]
        [SerializeField]
        private int selectionTimeoutPenalty = 10;

        [Tooltip("按关卡进度计算选词倒计时时长倍率。横轴 0 表示第一关，1 表示最后一关；纵轴乘以基础选词倒计时。")]
        [SerializeField]
        private AnimationCurve levelDurationMultiplierCurve = new(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.5f));

        [Header("音频")]
        [Tooltip("主菜单背景音乐。")]
        [SerializeField]
        private AudioClip mainMenuBgm;

        [Tooltip("游戏流程背景音乐。")]
        [SerializeField]
        private AudioClip gameBgm;

        [Tooltip("按钮点击音效，使用 UI 音效频道。")]
        [SerializeField]
        private AudioClip uiClickSfx;

        [Tooltip("选词扣分达到该阈值时播放警告音效。")]
        [Min(0)]
        [SerializeField]
        private int warningPenaltyThreshold = 20;

        [Tooltip("高扣分警告音效，使用其它音效频道。")]
        [SerializeField]
        private AudioClip warningSfx;

        [Tooltip("胜利结算音效，使用其它音效频道。")]
        [SerializeField]
        private AudioClip victorySfx;

        [Tooltip("失败结算音效，使用其它音效频道。")]
        [SerializeField]
        private AudioClip failureSfx;

        [Tooltip("选词时随机播放的说话音效列表，使用说话音效频道。")]
        [SerializeField]
        private List<AudioClip> voiceSfxClips = new();

        /// <summary>
        /// 一局开始时的全局容忍度。
        /// </summary>
        public int InitialTolerance => initialTolerance;

        /// <summary>
        /// 全局累计扣分到评分档的阈值配置。
        /// </summary>
        public IReadOnlyList<GradeThresholdEntry> GradeThresholds => gradeThresholds;

        /// <summary>
        /// 单次评分为 A 时回复的容忍度。
        /// </summary>
        public int GradeARecovery => gradeARecovery;

        /// <summary>
        /// 单次评分为 B 时回复的容忍度。
        /// </summary>
        public int GradeBRecovery => gradeBRecovery;

        /// <summary>
        /// 触发额外回复所需的连续 A 次数。
        /// </summary>
        public int ConsecutiveAGradeThreshold => Mathf.Max(2, consecutiveAGradeThreshold);

        /// <summary>
        /// 达到连续 A 阈值后，每次 A 额外回复的容忍度。
        /// </summary>
        public int ConsecutiveAGradeRecoveryBonus => consecutiveAGradeRecoveryBonus;

        /// <summary>
        /// 基础选词倒计时时长，单位为秒。
        /// </summary>
        public float InitialLevelDuration => initialLevelDuration;

        /// <summary>
        /// 单次选词倒计时清零时扣除的容忍度。
        /// </summary>
        public int SelectionTimeoutPenalty => Mathf.Max(0, selectionTimeoutPenalty);

        /// <summary>
        /// 主菜单背景音乐。
        /// </summary>
        public AudioClip MainMenuBgm => mainMenuBgm;

        /// <summary>
        /// 游戏流程背景音乐。
        /// </summary>
        public AudioClip GameBgm => gameBgm;

        /// <summary>
        /// 按钮点击音效。
        /// </summary>
        public AudioClip UiClickSfx => uiClickSfx;

        /// <summary>
        /// 选词扣分达到该阈值时播放警告音效。
        /// </summary>
        public int WarningPenaltyThreshold => Mathf.Max(0, warningPenaltyThreshold);

        /// <summary>
        /// 高扣分警告音效。
        /// </summary>
        public AudioClip WarningSfx => warningSfx;

        /// <summary>
        /// 胜利结算音效。
        /// </summary>
        public AudioClip VictorySfx => victorySfx;

        /// <summary>
        /// 失败结算音效。
        /// </summary>
        public AudioClip FailureSfx => failureSfx;

        /// <summary>
        /// 选词时随机播放的说话音效列表。
        /// </summary>
        public IReadOnlyList<AudioClip> VoiceSfxClips => voiceSfxClips;

        /// <summary>
        /// 按全局累计扣分阈值获取评分档位。
        /// </summary>
        /// <param name="totalPenalty">本关累计扣分。</param>
        /// <returns>评分档位。</returns>
        public AnswerGrade GetGradeByTotalPenalty(int totalPenalty)
        {
            var penalty = Mathf.Max(0, totalPenalty);
            GradeThresholdEntry fallback = null;

            for (var i = 0; i < gradeThresholds.Count; i++)
            {
                var entry = gradeThresholds[i];
                if (entry == null)
                {
                    continue;
                }

                fallback = entry;
                if (penalty <= entry.MaxTotalPenalty)
                {
                    return entry.Grade;
                }
            }

            return fallback != null ? fallback.Grade : AnswerGrade.E;
        }

        /// <summary>
        /// 获取指定关卡的单次选词倒计时时长。
        /// </summary>
        /// <param name="levelIndex">当前关卡索引，从 0 开始。</param>
        /// <param name="levelCount">本轮总关卡数。</param>
        /// <returns>本关单次选词倒计时时长，单位为秒。</returns>
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
