using System.Collections;
using Ciga2026.Framework.Singletons;
using UnityEngine;

namespace Ciga2026.Framework.Audio
{
    /// <summary>
    /// 全局音频管理器。
    /// 负责统一播放 BGM 和短音效，并提供主音量、BGM 音量、音效音量的运行时控制。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Ciga2026/Framework/Audio Manager")]
    public sealed class AudioManager : PersistentMonoSingleton<AudioManager>
    {
        [Header("音频源")]
        [SerializeField]
        [Tooltip("用于播放背景音乐的 AudioSource。未绑定时会在运行时自动创建。")]
        private AudioSource bgmSource;

        [SerializeField]
        [Tooltip("用于播放短音效的 AudioSource。未绑定时会在运行时自动创建。")]
        private AudioSource sfxSource;

        [Header("音量设置")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("全局主音量。最终 BGM 音量 = 主音量 x BGM 音量；最终音效音量 = 主音量 x 音效音量。")]
        private float masterVolume = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("背景音乐音量，会和主音量相乘后应用到 BGM AudioSource。")]
        private float bgmVolume = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("音效音量，会和主音量相乘后应用到音效 AudioSource。")]
        private float sfxVolume = 1f;

        [Header("启动播放")]
        [SerializeField]
        [Tooltip("是否在 Awake 时自动播放默认背景音乐。")]
        private bool playBgmOnAwake;

        [SerializeField]
        [Tooltip("默认背景音乐。仅在“启动时播放 BGM”开启时使用。")]
        private AudioClip initialBgmClip;

        [SerializeField]
        [Tooltip("默认背景音乐是否循环播放。")]
        private bool initialBgmLoop = true;

        private Coroutine bgmFadeRoutine;

        /// <summary>
        /// BGM 使用的 AudioSource。适合做更细的音频源配置，例如 Spatial Blend 或 Output Mixer。
        /// </summary>
        public AudioSource BgmSource => bgmSource;

        /// <summary>
        /// 音效使用的 AudioSource。短音效默认通过 PlayOneShot 在这个源上播放。
        /// </summary>
        public AudioSource SfxSource => sfxSource;

        /// <summary>
        /// 当前主音量，范围 0 到 1。
        /// </summary>
        public float MasterVolume => masterVolume;

        /// <summary>
        /// 当前 BGM 音量，范围 0 到 1。
        /// </summary>
        public float BgmVolume => bgmVolume;

        /// <summary>
        /// 当前音效音量，范围 0 到 1。
        /// </summary>
        public float SfxVolume => sfxVolume;

        /// <summary>
        /// 当前 BGM 是否正在播放。
        /// </summary>
        public bool IsBgmPlaying => bgmSource != null && bgmSource.isPlaying;

        /// <summary>
        /// 初始化单例并准备两个 AudioSource。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
            {
                return;
            }

            EnsureAudioSources();
            ApplyVolumes();

            if (playBgmOnAwake && initialBgmClip != null)
            {
                PlayBgm(initialBgmClip, initialBgmLoop);
            }
        }

        /// <summary>
        /// 播放背景音乐。
        /// </summary>
        /// <param name="clip">要播放的 BGM 音频片段。传入 null 时不会执行播放。</param>
        /// <param name="loop">是否循环播放。BGM 通常应为 true。</param>
        /// <param name="restartIfSameClip">当传入的 clip 和当前 BGM 相同时，是否从头重新播放。</param>
        public void PlayBgm(AudioClip clip, bool loop = true, bool restartIfSameClip = false)
        {
            if (clip == null)
            {
                return;
            }

            EnsureAudioSources();

            if (bgmSource.clip == clip && bgmSource.isPlaying && !restartIfSameClip)
            {
                return;
            }

            StopBgmFade();

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = GetFinalBgmVolume();
            bgmSource.Play();
        }

        /// <summary>
        /// 渐变播放背景音乐。
        /// </summary>
        /// <param name="clip">要播放的 BGM 音频片段。传入 null 时不会执行播放。</param>
        /// <param name="fadeDuration">淡入时长，单位秒。小于等于 0 时等同于立即播放。</param>
        /// <param name="loop">是否循环播放。</param>
        public void PlayBgmFadeIn(AudioClip clip, float fadeDuration = 0.5f, bool loop = true)
        {
            if (clip == null)
            {
                return;
            }

            if (fadeDuration <= 0f)
            {
                PlayBgm(clip, loop, true);
                return;
            }

            EnsureAudioSources();
            StopBgmFade();

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = 0f;
            bgmSource.Play();

            bgmFadeRoutine = StartCoroutine(FadeBgmVolume(GetFinalBgmVolume(), fadeDuration));
        }

        /// <summary>
        /// 停止背景音乐。
        /// </summary>
        /// <param name="clearClip">是否清空当前 BGM clip。为 true 时下次播放必须重新指定 clip。</param>
        public void StopBgm(bool clearClip = false)
        {
            EnsureAudioSources();
            StopBgmFade();

            bgmSource.Stop();

            if (clearClip)
            {
                bgmSource.clip = null;
            }
        }

        /// <summary>
        /// 渐变停止背景音乐。
        /// </summary>
        /// <param name="fadeDuration">淡出时长，单位秒。小于等于 0 时等同于立即停止。</param>
        /// <param name="clearClip">淡出结束后是否清空当前 BGM clip。</param>
        public void StopBgmFadeOut(float fadeDuration = 0.5f, bool clearClip = false)
        {
            if (fadeDuration <= 0f)
            {
                StopBgm(clearClip);
                return;
            }

            EnsureAudioSources();
            StopBgmFade();
            bgmFadeRoutine = StartCoroutine(FadeOutAndStopBgm(fadeDuration, clearClip));
        }

        /// <summary>
        /// 暂停背景音乐。
        /// </summary>
        public void PauseBgm()
        {
            EnsureAudioSources();
            bgmSource.Pause();
        }

        /// <summary>
        /// 恢复暂停中的背景音乐。
        /// </summary>
        public void ResumeBgm()
        {
            EnsureAudioSources();
            bgmSource.UnPause();
        }

        /// <summary>
        /// 播放一次短音效。
        /// </summary>
        /// <param name="clip">要播放的音效片段。传入 null 时不会执行播放。</param>
        /// <param name="volumeScale">单次播放音量倍率，最终音量会再乘以主音量和音效音量。</param>
        /// <param name="pitch">播放音高。1 为原始音高，小于 1 变低，大于 1 变高。</param>
        public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null)
            {
                return;
            }

            EnsureAudioSources();

            sfxSource.pitch = Mathf.Max(0.01f, pitch);
            sfxSource.volume = GetFinalSfxVolume();
            sfxSource.PlayOneShot(clip, Mathf.Max(0f, volumeScale));
        }

        /// <summary>
        /// 停止当前音效 AudioSource 上正在播放的所有声音。
        /// </summary>
        public void StopSfx()
        {
            EnsureAudioSources();
            sfxSource.Stop();
        }

        /// <summary>
        /// 设置全局主音量。
        /// </summary>
        /// <param name="volume">主音量，传入值会被限制在 0 到 1。</param>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        /// <summary>
        /// 设置背景音乐音量。
        /// </summary>
        /// <param name="volume">BGM 音量，传入值会被限制在 0 到 1。</param>
        public void SetBgmVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        /// <summary>
        /// 设置音效音量。
        /// </summary>
        /// <param name="volume">音效音量，传入值会被限制在 0 到 1。</param>
        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        /// <summary>
        /// 确保 BGM 和音效各自拥有一个可用的 AudioSource。
        /// </summary>
        private void EnsureAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = CreateChildAudioSource("BGM Source", true);
            }

            if (sfxSource == null)
            {
                sfxSource = CreateChildAudioSource("SFX Source", false);
            }

            bgmSource.playOnAwake = false;
            sfxSource.playOnAwake = false;
        }

        /// <summary>
        /// 创建挂在当前管理器下面的 AudioSource 子对象。
        /// </summary>
        /// <param name="name">子对象名称，用于在 Hierarchy 中区分 BGM 和 SFX。</param>
        /// <param name="loop">创建后 AudioSource 的默认循环状态。</param>
        /// <returns>创建完成并配置好的 AudioSource。</returns>
        private AudioSource CreateChildAudioSource(string name, bool loop)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);

            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;

            return source;
        }

        /// <summary>
        /// 将当前音量设置应用到两个 AudioSource。
        /// </summary>
        private void ApplyVolumes()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = GetFinalBgmVolume();
            }

            if (sfxSource != null)
            {
                sfxSource.volume = GetFinalSfxVolume();
            }
        }

        /// <summary>
        /// 计算最终 BGM 音量。
        /// </summary>
        private float GetFinalBgmVolume()
        {
            return masterVolume * bgmVolume;
        }

        /// <summary>
        /// 计算最终音效音量。
        /// </summary>
        private float GetFinalSfxVolume()
        {
            return masterVolume * sfxVolume;
        }

        /// <summary>
        /// 停止当前正在运行的 BGM 淡入淡出协程。
        /// </summary>
        private void StopBgmFade()
        {
            if (bgmFadeRoutine == null)
            {
                return;
            }

            StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = null;
        }

        /// <summary>
        /// 将 BGM 音量渐变到目标值。
        /// </summary>
        /// <param name="targetVolume">目标音量，通常为主音量 x BGM 音量。</param>
        /// <param name="duration">渐变时长，单位秒。</param>
        private IEnumerator FadeBgmVolume(float targetVolume, float duration)
        {
            var startVolume = bgmSource.volume;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            bgmSource.volume = targetVolume;
            bgmFadeRoutine = null;
        }

        /// <summary>
        /// 淡出并停止 BGM。
        /// </summary>
        /// <param name="duration">淡出时长，单位秒。</param>
        /// <param name="clearClip">停止后是否清空当前 BGM clip。</param>
        private IEnumerator FadeOutAndStopBgm(float duration, bool clearClip)
        {
            yield return FadeBgmVolume(0f, duration);

            bgmSource.Stop();
            bgmSource.volume = GetFinalBgmVolume();

            if (clearClip)
            {
                bgmSource.clip = null;
            }
        }

        /// <summary>
        /// 在 Inspector 修改序列化字段时限制音量范围，并在运行时立即应用。
        /// </summary>
        private void OnValidate()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            bgmVolume = Mathf.Clamp01(bgmVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);

            if (Application.isPlaying)
            {
                ApplyVolumes();
            }
        }
    }
}
