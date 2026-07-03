using Ciga2026.Framework.Events;
using Ciga2026.Framework.Singletons;
using Ciga2026.Game.Events;
using Ciga2026.Game.States;
using UnityEngine;
using FrameworkStateMachine = Ciga2026.Framework.StateMachine.StateMachine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ciga2026.Game
{
    /// <summary>
    /// 游戏顶层状态管理器，负责主菜单、进入游戏和退出游戏流程。
    /// </summary>
    public sealed class GameStateManager : MonoSingleton<GameStateManager>
    {
        [Header("初始状态")]
        [Tooltip("游戏启动后进入的顶层状态。")]
        [SerializeField]
        private GameStateType initialState = GameStateType.MainMenu;

        private readonly FrameworkStateMachine stateMachine = new();

        /// <summary>
        /// 当前游戏顶层状态。
        /// </summary>
        public GameStateType CurrentState { get; private set; }

        /// <summary>
        /// 当前状态是否为退出中。
        /// </summary>
        public bool IsExiting => CurrentState == GameStateType.Exiting;

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
            {
                return;
            }

            RegisterStates();
        }

        private void Start()
        {
            if (Instance != this)
            {
                return;
            }

            ChangeState(initialState);
        }

        private void Update()
        {
            stateMachine.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            stateMachine.FixedTick(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            stateMachine.LateTick(Time.deltaTime);
        }

        /// <summary>
        /// 切换到主菜单状态。
        /// </summary>
        public void GoToMainMenu()
        {
            ChangeState(GameStateType.MainMenu);
        }

        /// <summary>
        /// 从主菜单或其它状态进入正式游戏。
        /// </summary>
        public void EnterGame()
        {
            ChangeState(GameStateType.Playing);
        }

        /// <summary>
        /// 从任意状态请求退出游戏。
        /// </summary>
        public void ExitGame()
        {
            ChangeState(GameStateType.Exiting);
        }

        /// <summary>
        /// 按枚举切换游戏顶层状态。
        /// </summary>
        /// <param name="stateType">目标状态。</param>
        public void ChangeState(GameStateType stateType)
        {
            if (IsExiting && stateType != GameStateType.Exiting)
            {
                return;
            }

            switch (stateType)
            {
                case GameStateType.MainMenu:
                    stateMachine.ChangeState<MainMenuState>();
                    break;
                case GameStateType.Playing:
                    stateMachine.ChangeState<PlayingState>();
                    break;
                case GameStateType.Exiting:
                    stateMachine.ChangeState<ExitGameState>();
                    break;
                default:
                    Debug.LogWarning($"未处理的游戏状态：{stateType}");
                    break;
            }
        }

        internal void RaiseStateChanged(GameStateType newState)
        {
            var previousState = CurrentState;
            var hasPreviousState = stateMachine.PreviousState is GameState;

            CurrentState = newState;
            EventBus.Global.Publish(new GameStateChangedEvent(hasPreviousState ? previousState : null, newState));
        }

        internal void QuitApplication()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void RegisterStates()
        {
            stateMachine.AddState(new MainMenuState(this));
            stateMachine.AddState(new PlayingState(this));
            stateMachine.AddState(new ExitGameState(this));
        }
    }
}
