using System;
using System.Collections.Generic;

namespace Ciga2026.Framework.StateMachine
{
    public sealed class StateMachine
    {
        private readonly Dictionary<Type, IState> statesByType = new();

        public IState CurrentState { get; private set; }
        public IState PreviousState { get; private set; }

        public TState AddState<TState>(TState state) where TState : IState
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            statesByType[typeof(TState)] = state;
            return state;
        }

        public bool TryGetState<TState>(out TState state) where TState : class, IState
        {
            if (statesByType.TryGetValue(typeof(TState), out var foundState))
            {
                state = foundState as TState;
                return state != null;
            }

            state = null;
            return false;
        }

        public void ChangeState<TState>() where TState : class, IState
        {
            if (!TryGetState<TState>(out var state))
            {
                throw new InvalidOperationException($"State '{typeof(TState).Name}' has not been added.");
            }

            ChangeState(state);
        }

        public void ChangeState(IState nextState)
        {
            if (nextState == null)
            {
                throw new ArgumentNullException(nameof(nextState));
            }

            if (ReferenceEquals(CurrentState, nextState))
            {
                return;
            }

            CurrentState?.Exit();
            PreviousState = CurrentState;
            CurrentState = nextState;
            CurrentState.Enter();
        }

        public void Tick(float deltaTime)
        {
            CurrentState?.Tick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            CurrentState?.FixedTick(fixedDeltaTime);
        }

        public void LateTick(float deltaTime)
        {
            CurrentState?.LateTick(deltaTime);
        }
    }
}
