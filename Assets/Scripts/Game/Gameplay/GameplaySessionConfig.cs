using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ciga2026.Game.Gameplay
{
    /// <summary>
    /// 核心玩法运行时会话配置。
    /// </summary>
    [CreateAssetMenu(fileName = "GameplaySessionConfig", menuName = "Ciga2026/Gameplay/Session Config")]
    public sealed class GameplaySessionConfig : ScriptableObject
    {
        private const string DefaultRuntimeConfigFileName = "GameplaySessionConfig.json";

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

        [Header("运行时 JSON 覆盖")]
        [Tooltip("StreamingAssets 下的热更配置文件名。文件不存在或解析失败时使用本 SO 的默认值。")]
        [SerializeField]
        private string runtimeConfigFileName = DefaultRuntimeConfigFileName;

        private RuntimeGameplaySessionConfig runtimeConfig;
        private AnimationCurve runtimeLevelDurationMultiplierCurve;

        /// <summary>
        /// 一局开始时的全局容忍度。
        /// </summary>
        public int InitialTolerance => Mathf.Max(1, runtimeConfig != null ? runtimeConfig.initialTolerance : initialTolerance);

        /// <summary>
        /// 全局累计扣分到评分档的阈值配置。
        /// </summary>
        public IReadOnlyList<GradeThresholdEntry> GradeThresholds => gradeThresholds;

        /// <summary>
        /// 单次评分为 A 时回复的容忍度。
        /// </summary>
        public int GradeARecovery => Mathf.Max(0, runtimeConfig != null ? runtimeConfig.gradeARecovery : gradeARecovery);

        /// <summary>
        /// 单次评分为 B 时回复的容忍度。
        /// </summary>
        public int GradeBRecovery => Mathf.Max(0, runtimeConfig != null ? runtimeConfig.gradeBRecovery : gradeBRecovery);

        /// <summary>
        /// 触发额外回复所需的连续 A 次数。
        /// </summary>
        public int ConsecutiveAGradeThreshold => Mathf.Max(2, runtimeConfig != null ? runtimeConfig.consecutiveAGradeThreshold : consecutiveAGradeThreshold);

        /// <summary>
        /// 达到连续 A 阈值后，每次 A 额外回复的容忍度。
        /// </summary>
        public int ConsecutiveAGradeRecoveryBonus => Mathf.Max(0, runtimeConfig != null ? runtimeConfig.consecutiveAGradeRecoveryBonus : consecutiveAGradeRecoveryBonus);

        /// <summary>
        /// 基础选词倒计时时长，单位为秒。
        /// </summary>
        public float InitialLevelDuration => Mathf.Max(0.01f, runtimeConfig != null ? runtimeConfig.initialLevelDuration : initialLevelDuration);

        /// <summary>
        /// 单次选词倒计时清零时扣除的容忍度。
        /// </summary>
        public int SelectionTimeoutPenalty => Mathf.Max(0, runtimeConfig != null ? runtimeConfig.selectionTimeoutPenalty : selectionTimeoutPenalty);

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
        public int WarningPenaltyThreshold => Mathf.Max(0, runtimeConfig != null ? runtimeConfig.warningPenaltyThreshold : warningPenaltyThreshold);

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

        private void OnEnable()
        {
            ReloadRuntimeConfig();
        }

        /// <summary>
        /// 从 StreamingAssets 重新读取运行时 JSON 配置。
        /// </summary>
        public void ReloadRuntimeConfig()
        {
            runtimeConfig = null;
            runtimeLevelDurationMultiplierCurve = null;

            var path = GetRuntimeConfigPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                var parsedConfig = JsonUtility.FromJson<RuntimeGameplaySessionConfig>(json);
                if (parsedConfig == null)
                {
                    Debug.LogWarning($"玩法 JSON 配置解析为空，已使用 SO 默认值：{path}");
                    return;
                }

                runtimeConfig = parsedConfig;
                runtimeLevelDurationMultiplierCurve = CreateRuntimeDurationCurve(parsedConfig.levelDurationMultiplierCurve);
                Debug.Log($"已应用玩法 JSON 配置：{path}，初始容忍度={InitialTolerance}，基础选词时间={InitialLevelDuration:0.###}，超时扣分={SelectionTimeoutPenalty}");
            }
            catch (Exception exception)
            {
                runtimeConfig = null;
                runtimeLevelDurationMultiplierCurve = null;
                Debug.LogWarning($"读取玩法 JSON 配置失败，已使用 SO 默认值：{path}\n{exception.Message}");
            }
        }

        /// <summary>
        /// 按全局累计扣分阈值获取评分档位。
        /// </summary>
        /// <param name="totalPenalty">本关累计扣分。</param>
        /// <returns>评分档位。</returns>
        public AnswerGrade GetGradeByTotalPenalty(int totalPenalty)
        {
            var penalty = Mathf.Max(0, totalPenalty);
            var runtimeThresholds = runtimeConfig?.gradeThresholds;
            if (runtimeThresholds != null && runtimeThresholds.Count > 0)
            {
                var fallbackGrade = AnswerGrade.E;
                for (var i = 0; i < runtimeThresholds.Count; i++)
                {
                    var entry = runtimeThresholds[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    fallbackGrade = ToAnswerGrade(entry.grade);
                    if (penalty <= Mathf.Max(0, entry.maxTotalPenalty))
                    {
                        return fallbackGrade;
                    }
                }

                return fallbackGrade;
            }

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
            var durationCurve = runtimeLevelDurationMultiplierCurve ?? levelDurationMultiplierCurve;
            var multiplier = durationCurve != null
                ? Mathf.Max(0.01f, durationCurve.Evaluate(normalizedProgress))
                : 1f;

            return Mathf.Max(0.01f, InitialLevelDuration * multiplier);
        }

        private string GetRuntimeConfigPath()
        {
            var fileName = string.IsNullOrWhiteSpace(runtimeConfigFileName)
                ? DefaultRuntimeConfigFileName
                : runtimeConfigFileName.Trim();
            return Path.Combine(Application.streamingAssetsPath, fileName);
        }

        private static AnimationCurve CreateRuntimeDurationCurve(List<RuntimeCurveKey> keys)
        {
            if (keys == null || keys.Count == 0)
            {
                return null;
            }

            var keyframes = new Keyframe[keys.Count];
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                keyframes[i] = key != null
                    ? new Keyframe(Mathf.Clamp01(key.time), Mathf.Max(0.01f, key.value))
                    : new Keyframe(0f, 1f);
            }

            return new AnimationCurve(keyframes);
        }

        private static AnswerGrade ToAnswerGrade(int grade)
        {
            return Enum.IsDefined(typeof(AnswerGrade), grade) ? (AnswerGrade)grade : AnswerGrade.E;
        }

        [Serializable]
        private sealed class RuntimeGameplaySessionConfig
        {
            public int initialTolerance = 100;
            public List<RuntimeGradeThresholdEntry> gradeThresholds = new();
            public int gradeARecovery = 20;
            public int gradeBRecovery = 10;
            public int consecutiveAGradeThreshold = 2;
            public int consecutiveAGradeRecoveryBonus = 10;
            public float initialLevelDuration = 5f;
            public int selectionTimeoutPenalty = 10;
            public List<RuntimeCurveKey> levelDurationMultiplierCurve = new();
            public int warningPenaltyThreshold = 20;
        }

        [Serializable]
        private sealed class RuntimeGradeThresholdEntry
        {
            public int grade;
            public int maxTotalPenalty;
        }

        [Serializable]
        private sealed class RuntimeCurveKey
        {
            public float time;
            public float value;
        }
    }
}
