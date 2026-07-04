using Ciga2026.Framework.Audio;
using Ciga2026.Game.Gameplay;
using UnityEngine;

namespace Ciga2026.Game.Audio
{
    /// <summary>
    /// 游戏侧音频播放入口，负责把玩法配置中的音频映射到 AudioManager 频道。
    /// </summary>
    public static class GameAudioPlayer
    {
        /// <summary>
        /// 播放主菜单 BGM。
        /// </summary>
        /// <param name="config">玩法会话配置。</param>
        public static void PlayMainMenuBgm(GameplaySessionConfig config)
        {
            GetAudioManager().PlayBgm(config != null ? config.MainMenuBgm : null);
        }

        /// <summary>
        /// 播放游戏流程 BGM。
        /// </summary>
        /// <param name="config">玩法会话配置。</param>
        public static void PlayGameBgm(GameplaySessionConfig config)
        {
            GetAudioManager().PlayBgm(config != null ? config.GameBgm : null);
        }

        /// <summary>
        /// 播放 UI 点击音效。
        /// </summary>
        /// <param name="config">玩法会话配置。</param>
        public static void PlayUiClick(GameplaySessionConfig config)
        {
            GetAudioManager().PlaySfx(config != null ? config.UiClickSfx : null, AudioSfxChannel.UI);
        }

        /// <summary>
        /// 根据本次选词扣分播放警告音效。
        /// </summary>
        /// <param name="config">玩法会话配置。</param>
        /// <param name="penalty">本次选词扣分。</param>
        public static void PlayPenaltyWarningIfNeeded(GameplaySessionConfig config, int penalty)
        {
            if (config == null || penalty < config.WarningPenaltyThreshold)
            {
                return;
            }

            GetAudioManager().PlaySfx(config.WarningSfx, AudioSfxChannel.Other);
        }

        /// <summary>
        /// 随机播放一个说话音效。
        /// </summary>
        /// <param name="config">玩法会话配置。</param>
        public static void PlayRandomVoice(GameplaySessionConfig config)
        {
            if (config == null || config.VoiceSfxClips.Count == 0)
            {
                return;
            }

            var clip = config.VoiceSfxClips[Random.Range(0, config.VoiceSfxClips.Count)];
            GetAudioManager().PlaySfx(clip, AudioSfxChannel.Voice);
        }

        /// <summary>
        /// 播放胜利结算音效。
        /// </summary>
        /// <param name="config">玩法会话配置。</param>
        public static void PlayVictory(GameplaySessionConfig config)
        {
            GetAudioManager().PlaySfx(config != null ? config.VictorySfx : null, AudioSfxChannel.Other);
        }

        /// <summary>
        /// 播放失败结算音效。
        /// </summary>
        /// <param name="config">玩法会话配置。</param>
        public static void PlayFailure(GameplaySessionConfig config)
        {
            GetAudioManager().PlaySfx(config != null ? config.FailureSfx : null, AudioSfxChannel.Other);
        }

        private static AudioManager GetAudioManager()
        {
            if (AudioManager.TryGetInstance(out var audioManager))
            {
                return audioManager;
            }

            var audioManagerObject = new GameObject("AudioManager");
            return audioManagerObject.AddComponent<AudioManager>();
        }
    }
}
