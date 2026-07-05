using System;
using System.Reflection;
using UnityEngine;

namespace Ciga2026.Game.UI
{
    /// <summary>
    /// Game 界面角色 Spine 动画控制器。
    /// </summary>
    public sealed class GameCharacterSpineAnimator : MonoBehaviour
    {
        [Header("玩家 Spine")]
        [Tooltip("玩家 Spine 组件。可绑定 SkeletonGraphic 或 SkeletonAnimation。")]
        [SerializeField]
        private Component playerSpine;

        [Tooltip("玩家默认循环动画。")]
        [SerializeField]
        private string playerDefaultAnimation = "idle";

        [Tooltip("每次点击词语时播放一次的玩家动画。播放结束后会回到默认动画。")]
        [SerializeField]
        private string playerSelectAnimation = "idle2";

        [Tooltip("玩家从默认动画切换到选词动画前播放一次的过渡动画。为空时直接切换。")]
        [SerializeField]
        private string playerDefaultToSelectTransitionAnimation = "idle_to_idle2";

        [Tooltip("玩家从选词动画回到默认动画前播放一次的过渡动画。为空时直接切换。")]
        [SerializeField]
        private string playerSelectToDefaultTransitionAnimation = "idle2_to_idle";

        [Tooltip("玩家选词动画的最小持续时间。持续期间再次选词会刷新这个时间。")]
        [SerializeField]
        [Min(0f)]
        private float playerSelectMinimumDuration = 0.6f;

        [Header("NPC Spine")]
        [Tooltip("NPC Spine 组件。可绑定 SkeletonGraphic 或 SkeletonAnimation。")]
        [SerializeField]
        private Component npcSpine;

        [Tooltip("NPC 默认循环动画。")]
        [SerializeField]
        private string npcDefaultAnimation = "Idle";

        [Tooltip("中段天数循环播放的 NPC 动画。")]
        [SerializeField]
        private string npcHalfClosedEyeAnimation = "Idle2";

        [Tooltip("后段天数循环播放的 NPC 动画。")]
        [SerializeField]
        private string npcSleepingAnimation = "Idle3";

        private string currentNpcAnimation;
        private PlayerAnimationState playerAnimationState;
        private float playerSelectHoldUntil;
        private float playerTransitionLockUntil;
        private string playerPendingLoopAfterTransition;
        private bool playerSelectRequestedAfterTransition;

        private static readonly Type[] SetAnimationParameterTypes =
        {
            typeof(int),
            typeof(string),
            typeof(bool)
        };

        private static readonly Type[] AddAnimationParameterTypes =
        {
            typeof(int),
            typeof(string),
            typeof(bool),
            typeof(float)
        };

        private enum PlayerAnimationState
        {
            Default,
            EnteringSelect,
            SelectLoop,
            ExitingSelect
        }

        private void OnEnable()
        {
            ResetForLevel();
        }

        private void Update()
        {
            RefreshPlayerSelectAnimation();
        }

        /// <summary>
        /// 重置一关开始时的角色动画。
        /// </summary>
        public void ResetForLevel()
        {
            playerAnimationState = PlayerAnimationState.Default;
            playerSelectHoldUntil = 0f;
            playerTransitionLockUntil = 0f;
            playerPendingLoopAfterTransition = null;
            playerSelectRequestedAfterTransition = false;
            PlayLoop(playerSpine, playerDefaultAnimation);
            PlayNpcLoop(npcDefaultAnimation);
        }

        /// <summary>
        /// 玩家选择词语时进入循环选词动画，并刷新最小持续时间。
        /// </summary>
        public void PlayPlayerSelectAnimation()
        {
            if (string.IsNullOrWhiteSpace(playerSelectAnimation))
            {
                return;
            }

            playerSelectHoldUntil = Time.time + Mathf.Max(0f, playerSelectMinimumDuration);
            if (playerAnimationState == PlayerAnimationState.EnteringSelect || playerAnimationState == PlayerAnimationState.SelectLoop)
            {
                return;
            }

            if (playerAnimationState == PlayerAnimationState.ExitingSelect)
            {
                playerSelectRequestedAfterTransition = true;
                return;
            }

            PlayPlayerSelectSequence();
        }

        private void PlayPlayerSelectSequence()
        {
            playerSelectRequestedAfterTransition = false;

            if (TryPlayTransitionThenLoop(playerSpine, playerDefaultToSelectTransitionAnimation, playerSelectAnimation))
            {
                playerAnimationState = string.IsNullOrWhiteSpace(playerDefaultToSelectTransitionAnimation)
                    ? PlayerAnimationState.SelectLoop
                    : PlayerAnimationState.EnteringSelect;

                return;
            }

            if (TrySetAnimation(playerSpine, playerSelectAnimation, loop: true))
            {
                playerAnimationState = PlayerAnimationState.SelectLoop;
            }
        }

        private void PlayPlayerDefaultSequence()
        {
            playerSelectRequestedAfterTransition = false;

            if (TryPlayTransitionThenLoop(playerSpine, playerSelectToDefaultTransitionAnimation, playerDefaultAnimation))
            {
                playerAnimationState = string.IsNullOrWhiteSpace(playerSelectToDefaultTransitionAnimation)
                    ? PlayerAnimationState.Default
                    : PlayerAnimationState.ExitingSelect;

                return;
            }

            if (PlayLoop(playerSpine, playerDefaultAnimation))
            {
                playerAnimationState = PlayerAnimationState.Default;
            }
        }

        /// <summary>
        /// 按天数档位刷新 NPC 循环动画。
        /// </summary>
        /// <param name="dayStageIndex">向下取整得到的天数档位。0 为默认，1 为半闭眼，2 为睡着。</param>
        public void RefreshNpcByDayStage(int dayStageIndex)
        {
            var targetAnimation = GetNpcAnimation(dayStageIndex);
            PlayNpcLoop(targetAnimation);
        }

        private string GetNpcAnimation(int dayStageIndex)
        {
            if (dayStageIndex >= 2 && !string.IsNullOrWhiteSpace(npcSleepingAnimation))
            {
                return npcSleepingAnimation;
            }

            if (dayStageIndex >= 1 && !string.IsNullOrWhiteSpace(npcHalfClosedEyeAnimation))
            {
                return npcHalfClosedEyeAnimation;
            }

            return npcDefaultAnimation;
        }

        private void PlayNpcLoop(string animationName)
        {
            if (string.IsNullOrWhiteSpace(animationName) || currentNpcAnimation == animationName)
            {
                return;
            }

            if (PlayLoop(npcSpine, animationName))
            {
                currentNpcAnimation = animationName;
            }
        }

        private static bool PlayLoop(Component spineComponent, string animationName)
        {
            return !string.IsNullOrWhiteSpace(animationName)
                && TrySetAnimation(spineComponent, animationName, loop: true);
        }

        private void RefreshPlayerSelectAnimation()
        {
            RefreshPlayerTransitionState();

            if (playerAnimationState != PlayerAnimationState.SelectLoop || Time.time < playerSelectHoldUntil)
            {
                return;
            }

            PlayPlayerDefaultSequence();
        }

        private void RefreshPlayerTransitionState()
        {
            if (playerAnimationState != PlayerAnimationState.EnteringSelect && playerAnimationState != PlayerAnimationState.ExitingSelect)
            {
                return;
            }

            if (Time.time < playerTransitionLockUntil)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(playerPendingLoopAfterTransition))
            {
                PlayLoop(playerSpine, playerPendingLoopAfterTransition);
                playerPendingLoopAfterTransition = null;
            }

            var finishedTransitionState = playerAnimationState;
            playerAnimationState = playerAnimationState == PlayerAnimationState.EnteringSelect
                ? PlayerAnimationState.SelectLoop
                : PlayerAnimationState.Default;

            if (finishedTransitionState == PlayerAnimationState.ExitingSelect && playerSelectRequestedAfterTransition)
            {
                playerSelectRequestedAfterTransition = false;
                PlayPlayerSelectSequence();
            }
        }

        private static bool TrySetAnimation(Component spineComponent, string animationName, bool loop)
        {
            return TrySetAnimation(spineComponent, animationName, loop, out _);
        }

        private static bool TrySetAnimation(Component spineComponent, string animationName, bool loop, out object trackEntry)
        {
            var animationState = GetAnimationState(spineComponent);
            var setAnimationMethod = animationState?.GetType().GetMethod(
                "SetAnimation",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: SetAnimationParameterTypes,
                modifiers: null);

            trackEntry = null;
            if (setAnimationMethod == null)
            {
                return false;
            }

            try
            {
                trackEntry = setAnimationMethod.Invoke(animationState, new object[] { 0, animationName, loop });
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GameCharacterSpineAnimator failed to play '{animationName}' on {spineComponent.name}: {exception.InnerException?.Message ?? exception.Message}", spineComponent);
                return false;
            }
        }

        private bool TryPlayTransitionThenLoop(Component spineComponent, string transitionAnimation, string loopAnimation)
        {
            if (string.IsNullOrWhiteSpace(loopAnimation))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(transitionAnimation))
            {
                playerPendingLoopAfterTransition = null;
                return TrySetAnimation(spineComponent, loopAnimation, loop: true);
            }

            if (!TrySetAnimation(spineComponent, transitionAnimation, loop: false, out var transitionEntry))
            {
                return false;
            }

            var transitionDuration = GetTrackEntryAnimationDuration(transitionEntry);
            playerTransitionLockUntil = Time.time + transitionDuration;
            playerPendingLoopAfterTransition = TryAddAnimation(spineComponent, loopAnimation, loop: true, delay: 0f)
                ? null
                : loopAnimation;

            return true;
        }

        private static bool TryAddAnimation(Component spineComponent, string animationName, bool loop, float delay)
        {
            var animationState = GetAnimationState(spineComponent);
            var addAnimationMethod = animationState?.GetType().GetMethod(
                "AddAnimation",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: AddAnimationParameterTypes,
                modifiers: null);

            if (addAnimationMethod == null)
            {
                return false;
            }

            try
            {
                addAnimationMethod.Invoke(animationState, new object[] { 0, animationName, loop, Mathf.Max(0f, delay) });
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GameCharacterSpineAnimator failed to queue '{animationName}' on {spineComponent.name}: {exception.InnerException?.Message ?? exception.Message}", spineComponent);
                return false;
            }
        }

        private static float GetTrackEntryAnimationDuration(object trackEntry)
        {
            var animation = trackEntry?.GetType()
                .GetProperty("Animation", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(trackEntry);
            if (animation == null)
            {
                return 0f;
            }

            var duration = animation.GetType()
                .GetProperty("Duration", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(animation);
            return duration is float floatDuration ? Mathf.Max(0f, floatDuration) : 0f;
        }

        private static object GetAnimationState(Component spineComponent)
        {
            if (spineComponent == null)
            {
                return null;
            }

            TryInitializeSpine(spineComponent);

            var directAnimationState = spineComponent.GetType()
                .GetProperty("AnimationState", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(spineComponent);
            if (directAnimationState != null)
            {
                return directAnimationState;
            }

            var animationComponent = spineComponent.GetType()
                .GetProperty("Animation", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(spineComponent) as Component;
            if (animationComponent == null)
            {
                return null;
            }

            TryInitializeSpine(animationComponent);
            return animationComponent.GetType()
                .GetProperty("AnimationState", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(animationComponent);
        }

        private static void TryInitializeSpine(Component spineComponent)
        {
            var initializeMethod = spineComponent.GetType().GetMethod(
                "Initialize",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(bool) },
                modifiers: null);

            try
            {
                initializeMethod?.Invoke(spineComponent, new object[] { false });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GameCharacterSpineAnimator failed to initialize {spineComponent.name}: {exception.InnerException?.Message ?? exception.Message}", spineComponent);
            }
        }
    }
}
